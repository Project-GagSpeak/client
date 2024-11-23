using Dalamud.Plugin;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Utils;
using System.Text.RegularExpressions;


// This file has no current use, but is here for any potential future implementations of the IPA parser.

namespace GagSpeak.MufflerCore.Handler;
// Class to convert Mandarian text to International Phonetic Alphabet (IPA) notation
public class Ipa_Mandarian_Handler
{
    private string data_file; // Path to the JSON file containing the conversion rules
    private Dictionary<string, string> obj; // Dictionary to store the conversion rules in JSON
    private readonly ILogger<Ipa_Mandarian_Handler> _logger; // Logger
    private readonly ClientConfigurationManager _clientConfig; // The GagSpeak configuration
    private readonly IDalamudPluginInterface _pi; // Plugin interface for file access
    private List<string> CombinationsEng = new List<string> { "ɒː", "e", "iː", "uː", "eː", "ej", "ɒːj", "aw", "t͡ʃ", "d͡ʒ", "ts" };

    // List to store unique phonetic symbols
    private HashSet<string> uniqueSymbols = new HashSet<string>();
    public string uniqueSymbolsString = "";

    public Ipa_Mandarian_Handler(ILogger<Ipa_Mandarian_Handler> logger, ClientConfigurationManager clientConfig, IDalamudPluginInterface pi)
    {
        _logger = logger;
        _clientConfig = clientConfig;
        _pi = pi;
        // Set the path to the JSON file based on the language dialect
        data_file = DetermineDataFilePath(_clientConfig.GagspeakConfig.LanguageDialect);
        LoadConversionRules();
    }

    private string DetermineDataFilePath(string dialect)
    {
        switch (dialect)
        {
            case "IPA_zu_Hans":
                return "MufflerCore\\StoredDictionaries\\zu_hans.json";
            case "IPA_zu_Hant":
                return "MufflerCore\\StoredDictionaries\\zu_hant.json";
            default:
                return "MufflerCore\\StoredDictionaries\\zu_hans.json";
        }
    }

    private void LoadConversionRules()
    {
        try
        {
            string jsonFilePath = Path.Combine(_pi.AssemblyLocation.Directory?.FullName!, data_file);
            string json = File.ReadAllText(jsonFilePath);
            obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            _logger.LogInformation($"File read: {data_file}", LoggerType.GarblerCore);
            ExtractUniquePhonetics();
            uniqueSymbolsString = string.Join(",", uniqueSymbols);
        }
        catch (FileNotFoundException)
        {
            _logger.LogDebug($"File does not exist: {data_file}", LoggerType.GarblerCore);
            obj = new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"An error occurred while reading the file: {ex.Message}", LoggerType.GarblerCore);
            obj = new Dictionary<string, string>();
        }
    }

    /// <summary> Function for converting an input string to IPA notation.
    /// <list type="Bullet"><item><c>input</c><param name="input"> - string to convert</param></item></list>
    /// </summary><returns> The input string converted to IPA notation</returns>
    public string UpdateResult(string input)
    {
        var c_w = input;
        var str = "";
        // Iterate over each character in the input string
        for (var i = 0; i < c_w.Length; i++)
        {
            // If the character exists in the dictionary
            if (obj.ContainsKey(c_w[i].ToString()))
            {
                // Initialize an array to store the words
                var s_words = new string[6];
                // Assign the first word
                s_words[0] = c_w[i].ToString();
                // Iterate over the next 5 characters
                for (var j = 1; j < 6; j++)
                {
                    // If the index is within the string length
                    if (i + j < c_w.Length)
                    {
                        // Add the character to the word
                        s_words[j] = s_words[j - 1] + c_w[i + j];
                    }
                }
                // Find the last index of a word that exists in the dictionary
                var words_index = Array.FindLastIndex(s_words, sw => obj.ContainsKey(sw));
                // Get the word at the found index
                var search_words = s_words[words_index];
                // Add the word and its IPA notation to the result string
                str += $"( {search_words} : {obj[search_words]} ) ";
                //str += "(" + search_words + " " + obj[search_words] + " )";
                // Increment the index by the found index
                i += words_index;
            }
            // If the character does not exist in the dictionary
            else
            {
                // Add the character to the result string
                str += $"{c_w[i]} ";
                //str += c_w[i] + " ";
            }
        }

        // Return the formatted result string
        str = FormatMain(str);
        str = ConvertToSpacedPhonetics(str);
        return str;
    }

    /// <summary> Function for formatting the output string based on the selected dialect.
    /// <list type="Bullet">
    /// <item><c>t_str</c><param name="t_str"> - The input string to format</param></item>
    /// </list> </summary>
    /// <returns> The formatted output string</returns>
    private string FormatMain(string t_str)
    {
        var f_str = t_str;

        if (_clientConfig.GagspeakConfig.LanguageDialect == "IPA_num") f_str = FormatIPANum(t_str);               // kuɔ35
        else if (_clientConfig.GagspeakConfig.LanguageDialect == "IPA_org") f_str = FormatIPAOrg(t_str);          // kuɔ˧˥
        else if (_clientConfig.GagspeakConfig.LanguageDialect == "Jyutping_num") f_str = FormatJyutpingNum(t_str);// kuɔ2
        else if (_clientConfig.GagspeakConfig.LanguageDialect == "Jyutping") f_str = FormatJyutping(t_str);       // kuɔˊ

        return f_str;
    }

    private string FormatIPANum(string x)
    {         // kuɔ35
        x = x.Replace("˥", "5");
        x = x.Replace("˧", "3");
        x = x.Replace("˨", "2");
        x = x.Replace("˩", "1");
        x = x.Replace(":", "");
        return x;
    }

    private string FormatIPAOrg(string x)
    {         // kuɔ˧˥
        return x;
    }
    private string FormatJyutpingNum(string x)
    {    // kuɔ2
        x = FormatJyutping(x);

        x = x.Replace("ˉ", "1");
        x = x.Replace("ˊ", "2");
        x = x.Replace("ˇ", "3");
        x = x.Replace("ˋ", "4");
        x = x.Replace("˙", "˙");
        return x;
    }

    private string FormatJyutping(string x)
    {       // kuɔˊ
        x = x.Replace("˥˥", "ˉ");
        x = x.Replace("˧˥", "ˊ");
        x = x.Replace("˨˩˦", "ˇ");
        x = x.Replace("˨˩˩", "ˇ");
        x = x.Replace("˧˥", "ˇ");
        x = x.Replace("˥˩", "ˋ");
        x = x.Replace("˥˧", "ˋ");
        x = x.Replace("˨˩", "˙");
        x = x.Replace("˧˩", "˙");
        x = x.Replace("˦˩", "˙");
        x = x.Replace("˩˩", "˙");
        x = x.Replace("˧", "˙");
        x = x.Replace(":", "");
        return x;
    }


    public List<string> ExtractUniquePhonetics()
    {
        // Iterate over each word in the dictionary
        foreach (var entry in obj)
        {
            // Extract the phonetic symbols between the slashes
            var phonetics = entry.Value.Replace("/", "").Replace(",", "");

            // Check for known combinations first
            for (var i = 0; i < phonetics.Length - 1; i++)
            {
                var possibleCombination = phonetics.Substring(i, 2);
                if (CombinationsEng.Contains(possibleCombination))
                {
                    uniqueSymbols.Add(possibleCombination);
                    i++; // Skip next character as it's part of the combination
                }
                else
                {
                    // Skip commas
                    if (phonetics[i] != ',')
                    {
                        uniqueSymbols.Add(phonetics[i].ToString());
                    }
                }
            }

            // Check the last character if it wasn't part of a combination
            if (!uniqueSymbols.Contains(phonetics[^1].ToString()) && phonetics[^1] != ',')
            {
                uniqueSymbols.Add(phonetics[^1].ToString());
            }
        }

        return uniqueSymbols.ToList();
    }

    public string ConvertToSpacedPhonetics(string input)
    {
        _logger.LogDebug($"[IPA Parser] Converting phonetics to spaced phonetics: {input}", LoggerType.GarblerCore);
        var output = "";
        // Add a placeholder at the start and end of the input string
        input = " " + input + " ";
        // Split the input into phonetic representations
        var phoneticRepresentations = Regex.Split(input, @"(?<=\))\s*(?=\()");
        // Iterate over the phonetic representations
        foreach (var representation in phoneticRepresentations)
        {
            _logger.LogDebug($"[IPA Parser] Phonetic representation: {representation}", LoggerType.GarblerCore);
            // Remove the placeholders
            var phonetics = representation.Trim();
            // Check if the representation has a phonetic representation
            if (phonetics.StartsWith("(") && phonetics.EndsWith(")"))
            {
                // Extract the phonetic representation
                phonetics = phonetics.Trim('(', ')').Split(':')[1].Trim().Trim('/');
                // If there are multiple phonetic representations, only take the first one
                if (phonetics.Contains(","))
                {
                    phonetics = phonetics.Split(',')[0].Trim();
                }
                // Remove the primary and secondary stress symbols (delete this later if we find a use for them)
                phonetics = phonetics.Replace("ˈ", "").Replace("ˌ", "");
                // Initialize an empty string to hold the spaced out phonetics
                var spacedPhonetics = "";
                // Iterate over the phonetic symbols
                for (var i = 0; i < phonetics.Length; i++)
                {
                    // Check for known combinations first
                    if (i < phonetics.Length - 1)
                    {
                        var possibleCombination = phonetics.Substring(i, 2);
                        int index = GagPhonetics.MasterListEN_US.FindIndex(t => t == possibleCombination);
                        if (index != -1)
                        {
                            spacedPhonetics += GagPhonetics.MasterListEN_US[index] + "-"; // Use the phoneme from the Translator object
                            i++; // Skip next character as it's part of the combination
                        }
                        else
                        {
                            spacedPhonetics += phonetics[i] + "-";
                        }
                    }
                    else
                    {
                        spacedPhonetics += phonetics[i] + "-";
                    }
                }
                // Remove the trailing "-" and add the spaced out phonetics to the output
                output += spacedPhonetics.TrimEnd('-') + " ";
            }
            else
            {
                // If the representation doesn't have a phonetic representation, add it to the output as is
                output += phonetics + " ";
            }
        }
        _logger.LogDebug($"[IPA Parser] Converted phonetics to spaced phonetics: {output}", LoggerType.GarblerCore);
        // Remove the trailing space and return the output
        return output.TrimEnd();
    }
}
