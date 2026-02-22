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
    public string uniqueSymbolsString = "";
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

    /// <summary> Preprocess input string by converting it to lower case and removing certain characters.
    /// <list type="Bullet"><item><c>x</c><param name="x"> - String to preprocess</param></item></list>
    /// </summary> <returns> The preprocessed input string</returns>
    private string Preprocess(string x)
    {
        x = Regex.Replace(x, @"\n", "");
        return x;
    }

    /// <summary> Function for converting an input string to IPA notation.
    /// <para> THIS IS FOR UI DISPLAY PURPOSES, Hince the DASHED SPACE BETWEEN PHONEMES </para>
    /// <list type="Bullet"><item><c>input</c><param name="input"> - String to convert</param></item></list>
    /// </summary><returns> The input string converted to IPA notation</returns>
    public string ToIPAStringDisplay(string input)
    {
        // split the string by the spaces between words
        var c_w = (Preprocess(input) + " ").Split(" ");
        // the new string to output
        var str = "";
        // iterate over each word in the input string
        foreach (var word in c_w)
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
    /// The same as ToIPAStringDisp but shows the next step where its split by dashes
    /// </summary>
    public string ToIPAStringSpacedDisplay(string input)
    {
        var str = input;
        var parsedStr = ToIPAList(str);
        str = ConvertDictionaryToSpacedPhonetics(parsedStr);
        return str;
    }

    /// <summary> Converts an input string to a dictionary where each word maps to a list of its phonetic symbols.
    /// <param name="input">The input string to convert.</param>
    /// <returns>A dictionary where each word from the input string maps to a list of its phonetic symbols.</returns></summary>
    public List<Tuple<string, List<string>>> ToIPAList(string input)
    {
        // Log the input string
        _logger.LogTrace($"Parsing IPA string from original message:", LoggerType.GarblerCore);

        var c_w = (Preprocess(input) + " ").Split(" ");

        // Initialize the result dictionary
        var result = new List<Tuple<string, List<string>>>();

        // Iterate over each word in the input string
        foreach (var word in c_w)
        {
            // skip empty words
            if (string.IsNullOrEmpty(word)) continue;

            // remove punctuation from the word and convert to lower case
            var strippedWord = Regex.Replace(word, @"(?![.'-])\p{P}", "").ToLower();

            // attempt to retrieve the phonetic representation of the word.
            if (!obj.TryGetValue(strippedWord, out var phonetics))
            {
                // no word was found, make sure it wasn't because of errant trailing punctuation we allow.
                if (!strippedWord.EndsWith('.') || strippedWord.EndsWith('-'))
                {
                    result.Add(Tuple.Create(word, new List<string>()));
                    continue;
                }

                // remove last character for a second lookup
                if (!obj.TryGetValue(strippedWord[..^1], out phonetics))
                {
                    // one last check, removing the last character didn't help,
                    strippedWord = strippedWord.TrimEnd(_trimmingPunctuation);
                    if (!obj.TryGetValue(strippedWord, out phonetics))
                    {
                        result.Add(Tuple.Create(word, new List<string>()));
                        continue;
                    }
                }
            }

            // strip unwanted characters out of the phonetics data
            phonetics = phonetics.Replace("/", "");

            // this word is a homograph
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
            result.Add(Tuple.Create(word, phoneticSymbols));
        }

        _logger.LogTrace($"Parsed \"{input}\" to final list:\n{string.Join(',', result.Select(t => $"{t.Item1}:[{string.Join(',',t.Item2)}]"))}", LoggerType.GarblerCore);
        return result;
    }

    /// <summary>
    /// Converts a dictionary of words and their phonetic symbols to a string of spaced phonetics
    /// </summary>
    public string ConvertDictionaryToSpacedPhonetics(List<Tuple<string, List<string>>> inputTupleList)
    {
        // Initialize a string to hold the result
        var result = "";

        // Iterate over each entry in the dictionary
        foreach (var entry in inputTupleList)
        {
            // If the list has content, join the phonetic symbols with a dash
            // Otherwise, just use the normal word
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


    /// <summary>
    /// Sets the uniqueSymbolsString to the master list of phonemes for the selected language
    /// </summary>
    /// <summary>
    /// Sets the uniqueSymbolsString to the master list of phonemes for the selected language
    /// </summary>
    public void SetUniqueSymbolsString()
    {
        switch (_config.Current.LanguageDialect)
        {
            case GarbleCoreDialect.UK:
                uniqueSymbolsString = string.Join(",", GagPhonetics.MasterListEN_UK);
                break;
            case GarbleCoreDialect.US:
                uniqueSymbolsString = string.Join(",", GagPhonetics.MasterListEN_US);
                break;
            case GarbleCoreDialect.Spain:
                uniqueSymbolsString = string.Join(",", GagPhonetics.MasterListSP_SPAIN);
                break;
            case GarbleCoreDialect.Mexico:
                uniqueSymbolsString = string.Join(",", GagPhonetics.MasterListSP_MEXICO);
                break;
            case GarbleCoreDialect.France:
                uniqueSymbolsString = string.Join(",", GagPhonetics.MasterListFR_FRENCH);
                break;
            case GarbleCoreDialect.Quebec:
                uniqueSymbolsString = string.Join(",", GagPhonetics.MasterListFR_QUEBEC);
                break;
            case GarbleCoreDialect.Japan:
                uniqueSymbolsString = string.Join(",", GagPhonetics.MasterListJP);
                break;
            default:
                throw new Exception("Invalid language dialect");
        }
    }

}
