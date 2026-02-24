using CkCommons.GarblerCore;
using System.Text.RegularExpressions;
using CkCommons.FileSystem;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using Lumina.Excel.Sheets;

namespace GagSpeak.MufflerCore.Handler;

/// <summary>
///     Class to convert English, French, Japanese, and Spanish text to
///     International Phonetic Alphabet (IPA) notation
/// </summary>
public class Ipa_EN_FR_JP_SP_Handler
{
    private readonly ILogger<Ipa_EN_FR_JP_SP_Handler> _logger;
    private readonly MainConfig _config; // The GagSpeak configuration
    private Dictionary<string, string> obj; // Dictionary to store the conversion rules in JSON
    private readonly char[] _trimmingPunctuation = ['.', '-'];

    /* FOR DEBUGGING: If you ever need to aquire new unique symbols please reference the outdated private gagspeak repo. */

    public Ipa_EN_FR_JP_SP_Handler(ILogger<Ipa_EN_FR_JP_SP_Handler> logger, MainConfig config)
    {
        _logger = logger;
        _config = config;
        LoadConversionRules();
    }

    private void LoadConversionRules()
    {
        var data_file = GetDataFilePath();
        try
        {
            var jsonFilePath = Path.Combine(ConfigFileProvider.AssemblyDirectory, data_file);
            var json = File.ReadAllText(jsonFilePath);
            obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            _logger.LogInformation($"File read: {data_file}", LoggerType.GarblerCore);
        }
        catch (FileNotFoundException)
        {
            _logger.LogDebug($"File does not exist: {data_file}", LoggerType.GarblerCore);
        }
        catch (Bagagwa ex)
        {
            _logger.LogDebug($"An error occurred while reading the file: {ex.Message}", LoggerType.GarblerCore);
        }
    }

    /// <summary>
    ///     Function for converting an input string to IPA notation. <br />
    ///     <b>FOR UI DISPLAY PURPOSES, Hince the DASHED SPACE BETWEEN PHONEMES </b>
    /// </summary>
    /// <returns> The input string converted to IPA notation</returns>
    public string ToIPAStringDisplay(string input)
    {
        // split the string by the spaces between words
        var sanitizedInput = input.ReplaceLineEndings(" ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // the new string to output
        var str = "";
        // iterate over each word in the input string
        foreach (var word in sanitizedInput)
        {
            // if the word is not empty
            if (!string.IsNullOrEmpty(word))
            {
                // remove punctuation from the word
                var wordWithoutPunctuation = Regex.Replace(word, @"(?![.'-])\p{P}", "");
                wordWithoutPunctuation = wordWithoutPunctuation.ToLower();
                // if the word exists in the dictionary
                if (obj.ContainsKey(wordWithoutPunctuation))
                {
                    // append the word and its phonetic to the string
                    str += $"( {word} : {obj[wordWithoutPunctuation]} ) ";
                }
                // if not, append the word by itself
                else
                {
                    str += $"{word} ";
                }
            }
        }
        _logger.LogTrace($"Parsed IPA string: {str}", LoggerType.GarblerCore);
        //str = ConvertToSpacedPhonetics(str);
        return str;
    }

    /// <summary>
    ///     The same as ToIPAStringDisp but shows the next step where its split by dashes
    /// </summary>
    public string ToIPAStringSpacedDisplay(string input)
    {
        var str = input;
        var parsedStr = ToIPAList(str);
        str = ConvertDictionaryToSpacedPhonetics(parsedStr);
        return str;
    }

    /// <summary>
    ///     Converts a string to a list corrilating their words to their phonetic symbols.
    /// </summary>
    /// <returns> The list of (word, phonetic symbols, found in lookup) </returns>
    public List<(string Word, List<string> Phonetics, bool Found)> ToIPAList(string input)
    {
        _logger.LogTrace($"Parsing input to Phonetics...", LoggerType.GarblerCore);
        // Clear line endings and split by spaces, removing empty entries.
        var sanitizedInput = input.ReplaceLineEndings(" ").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Init the return list, over dictionary, to preserve order.
        var parsedResult = new List<(string, List<string>, bool)>();

        // Iterate over each word in the input string
        foreach (var word in sanitizedInput)
        {
            // Remove all punctuation from the word except . ' and -
            // Then convert to lowercase and trim . and - from the start of the world.
            var sanitized = Regex.Replace(word, @"(?![.'-])\p{P}", "").TrimStart(_trimmingPunctuation).ToLower();

            // If we cannot find the word in the dictionary..
            if (!obj.TryGetValue(sanitized, out var phonetics))
            {
                // no word was found, make sure it wasn't because of errant trailing punctuation we allow.
                if (!(sanitized.EndsWith('.') || sanitized.EndsWith('-')))
                {
                    parsedResult.Add((word, [], false));
                    continue;
                }

                // Attempt second lookup without trailing punctuation.
                if (!obj.TryGetValue(sanitized[..^1], out phonetics))
                {
                    // one last check, removing the last character didn't help,
                    sanitized = sanitized.TrimEnd(_trimmingPunctuation);
                    if (!obj.TryGetValue(sanitized, out phonetics))
                    {
                        parsedResult.Add((word, [], false));
                        continue;
                    }
                }
            }

            // strip unwanted characters out of the phonetics data
            phonetics = phonetics.Replace("/", "");

            // this word is a homograph, so take the first result.
            if (phonetics.Contains(','))
                phonetics = phonetics.Split(',')[0].Trim();

            // remove IPA symbols we aren't using.
            phonetics = phonetics.Replace("ˈ", "").Replace("ˌ", "");

            // create and iterate over our phonetic data
            var phoneticSymbols = new List<string>();
            var dialectMasterList = GetMasterListBasedOnDialect();
            for (var i = 0; i < phonetics.Length; i++)
            {
                // can't look ahead at characters if there's no characters to look ahead for.
                if (i >= phonetics.Length - 1)
                {
                    phoneticSymbols.Add(phonetics[i].ToString());
                    continue;
                }

                // check if the next character makes a valid IPA combination w/ this character.
                var candidate = phonetics.Substring(i, 2);
                var idx = dialectMasterList.FindIndex(s => s == candidate);
                if (idx != -1)
                {
                    // valid pair was found, add to list, and consume this and next char in loop.
                    phoneticSymbols.Add(dialectMasterList[idx]);
                    i++;
                }
                else
                {
                    // just add the character normally
                    phoneticSymbols.Add(phonetics[i].ToString());
                }
            }

            // add the final result to the dictionary.
            parsedResult.Add((word, phoneticSymbols, true));
        }

        _logger.LogTrace($"Parsed \"{input}\" to final list:\n{string.Join(',', parsedResult.Select(t => $"{t.Item1}:[{string.Join(',',t.Item2)}]"))}", LoggerType.GarblerCore);
        return parsedResult;
    }

    /// <summary>
    ///     Converts a dictionary of words and their phonetic symbols to a string of spaced phonetics
    /// </summary>
    public string ConvertDictionaryToSpacedPhonetics(List<(string Word, List<string> Phonetics, bool Found)> input)
    {
        var result = "";
        foreach (var entry in input)
        {
            // If the list has content, join the phonetic symbols with a dash
            var phonetics = entry.Item2.Any() ? string.Join("-", entry.Item2) : entry.Item1;
            // Add the phonetics to the result string
            result += $"{phonetics} ";
        }
        // Return the result string
        return result.Trim();
    }

    /// <summary>
    /// Returns the JSON file path based on the selected language
    /// </summary>
    public string GetDataFilePath()
    {
        switch (_config.Current.LanguageDialect)
        {
            case GarbleCoreDialect.UK:
                return "MufflerCore\\StoredDictionaries\\en_UK.json";
            case GarbleCoreDialect.US:
                return "MufflerCore\\StoredDictionaries\\en_US.json";
            case GarbleCoreDialect.Spain:
                return "MufflerCore\\StoredDictionaries\\es_ES.json";
            case GarbleCoreDialect.Mexico:
                return "MufflerCore\\StoredDictionaries\\es_MX.json";
            case GarbleCoreDialect.France:
                return "MufflerCore\\StoredDictionaries\\fr_FR.json";
            case GarbleCoreDialect.Quebec:
                return "MufflerCore\\StoredDictionaries\\fr_QC.json";
            case GarbleCoreDialect.Japan:
                return "MufflerCore\\StoredDictionaries\\ja.json";
            default: throw new Exception("Invalid language Dialect");
        }
    }

    /// <summary>
    /// Returns the master list of phonemes for the selected language
    /// </summary>
    public List<string> GetMasterListBasedOnDialect()
    {
        switch (_config.Current.LanguageDialect)
        {
            case GarbleCoreDialect.UK:
                return GagPhonetics.MasterListEN_UK;
            case GarbleCoreDialect.US:
                return GagPhonetics.MasterListEN_US;
            case GarbleCoreDialect.Spain:
                return GagPhonetics.MasterListSP_SPAIN;
            case GarbleCoreDialect.Mexico:
                return GagPhonetics.MasterListSP_MEXICO;
            case GarbleCoreDialect.France:
                return GagPhonetics.MasterListFR_FRENCH;
            case GarbleCoreDialect.Quebec:
                return GagPhonetics.MasterListFR_QUEBEC;
            case GarbleCoreDialect.Japan:
                return GagPhonetics.MasterListJP;
            default:
                throw new Exception("Invalid language dialect");
        }
    }
}
