using Dalamud.Plugin;
using GagSpeak.Services.Configs;
using System.Text.RegularExpressions;
// This file has no current use, but is here for any potential future implementations of the IPA parser.

namespace GagSpeak.MufflerCore.Handler;

/// <summary>
///     Class to convert Cantonese text to International Phonetic Alphabet (IPA) notation
/// </summary>
public class Ipa_Cantonese_Handler
{
    private Dictionary<string, string> obj; // Dictionary to store the conversion rules in JSON
    private readonly ILogger<Ipa_Cantonese_Handler> _logger; // Logger
    private readonly MainConfig _config; // The GagSpeak configuration
    private readonly IDalamudPluginInterface _pluginInterface; // Plugin interface for file access

    public Ipa_Cantonese_Handler(ILogger<Ipa_Cantonese_Handler> logger, MainConfig config, IDalamudPluginInterface pi)
    {
        _logger = logger;
        _config = config;
        _pluginInterface = pi;
        LoadConversionRules();
    }

    private void LoadConversionRules()
    {
        var data_file = "MufflerCore\\StoredDictionaries\\yue.json";
        try
        {
            var jsonFilePath = Path.Combine(_pluginInterface.AssemblyLocation.Directory?.FullName!, data_file);
            var json = File.ReadAllText(jsonFilePath);
            obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            _logger.LogInformation($"[IPA Parser] File read: {data_file}", LoggerType.GarblerCore);
        }
        catch (FileNotFoundException)
        {
            _logger.LogDebug($"[IPA Parser] File does not exist: {data_file}", LoggerType.GarblerCore);
            obj = new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"[IPA Parser] An error occurred while reading the file: {ex.Message}", LoggerType.GarblerCore);
            obj = new Dictionary<string, string>();
        }
    }

/*    /// <summary> Function for converting an input string to IPA notation.
    /// <list type="Bullet"><item><c>input</c><param name="input"> - string to convert</param></item></list>
    /// </summary><returns> The input string converted to IPA notation</returns>
    public string UpdateResult(string input)
    { // remember it's splitting by chars, but they are chinese chars, which are words.
        var c_w = input;
        var str = "";
        // Iterate over each character in the input string
        for (var i = 0; i < c_w.Length; i++)
        {
            // If the character exists in the dictionary
            if (obj.ContainsKey(c_w[i].ToString()))
            {
                // Initialize an array to store potential multi-word entries
                var s_words = new string[6];
                // Assign the first word
                s_words[0] = c_w[i].ToString();
                // Iterate through the next 5 words
                for (var j = 1; j < 6; j++)
                {
                    // If index is within the bounds of the array
                    if (i + j < c_w.Length)
                    {
                        // Add the next word to the array
                        s_words[j] = s_words[j - 1] + c_w[i + j];
                    }
                }
                // Find the last index of the array that exists in the dictionary
                var words_index = Array.FindLastIndex(s_words, sw => obj.ContainsKey(sw));
                // If the index is not -1, append the word to the result
                var search_words = s_words[words_index];
                // Adding word and its phonetic to the result
                str += "(" + search_words + " /" + obj[search_words] + "/ )";
                // Increment the index by the number of words in the array
                i += words_index;
            }
            // If the character does not exist in the obj dictionary
            else
            {
                // Add the character to the result
                str += c_w[i] + " ";
            }
        }
        // return the formatted string relative to the language dialect.
        return FormatMain(str);
    }

    /// <summary> Function for formatting the output string based on the selected dialect.
    /// <list type="Bullet"><item><c>t_str</c><param name="t_str"> - String to format</param></item></list>
    /// </summary><returns> The formatted output string</returns>
    private string FormatMain(string t_str)
    {
        var f_str = t_str;

        if (_config.Current.LanguageDialect == "IPA_nei5") f_str = FormatIPANum(t_str);         // nei13
        else if (_config.Current.LanguageDialect == "IPA_org") f_str = FormatIPAOrg(t_str);     // nei˩˧
        else if (_config.Current.LanguageDialect == "Jyutping") f_str = FormatJyutping(t_str);  // nei5

        return f_str;
    }*/

    // Below are all the language dialect formatters
    private string FormatIPANum(string x)
    { // nei13
        x = Regex.Replace(x, "˥", "5");
        x = Regex.Replace(x, "˧", "3");
        x = Regex.Replace(x, "˨", "2");
        x = Regex.Replace(x, "˩", "1");
        x = Regex.Replace(x, ":", "");
        return x;
    }

    private string FormatIPAOrg(string x)
    { // nei˩˧
        return x;
    }
    private string FormatJyutping(string x)
    { // nei5
        x = Regex.Replace(x, "˥˧|˥˥", "1");
        x = Regex.Replace(x, "˧˥", "2");
        x = Regex.Replace(x, "˧˧", "3");
        x = Regex.Replace(x, "˨˩|˩˩", "4");
        x = Regex.Replace(x, "˩˧|˨˧", "5");
        x = Regex.Replace(x, "˨˨", "6");

        x = Regex.Replace(x, "k˥", "k7");
        x = Regex.Replace(x, "k˧", "k8");
        x = Regex.Replace(x, "k˨", "k9");

        x = Regex.Replace(x, "t˥", "t7");
        x = Regex.Replace(x, "t˧", "t8");
        x = Regex.Replace(x, "t˨", "t9");

        x = Regex.Replace(x, "p˥", "p7");
        x = Regex.Replace(x, "p˧", "p8");
        x = Regex.Replace(x, "p˨", "p9");

        x = Regex.Replace(x, "˥", "1");
        x = Regex.Replace(x, "˧", "3");
        x = Regex.Replace(x, "˨", "6");

        x = Regex.Replace(x, ":", "");
        return x;
    }
}
