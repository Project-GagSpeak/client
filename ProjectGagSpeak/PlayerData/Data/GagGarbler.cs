using GagSpeak.MufflerCore.Handler;
using GagSpeak.PlayerData.Handlers;
using GagspeakAPI.Extensions;

namespace GagSpeak.PlayerData.Data;

public class GagGarbler
{
    private readonly ILogger<GagGarbler> _logger;
    private readonly Ipa_EN_FR_JP_SP_Handler _IPAParser;
    private readonly GagDataHandler _gagDataHandler;

    public List<GagData> _activeGags;

    public GagGarbler(ILogger<GagGarbler> logger, GagDataHandler gagDataHandler, Ipa_EN_FR_JP_SP_Handler IPAParser)
    {
        _logger = logger;
        _IPAParser = IPAParser;
        _gagDataHandler = gagDataHandler;
    }

    public void UpdateGarblerLogic(GagType gagOne, GagType gagTwo, GagType gagThree)
    => UpdateGarblerLogic([gagOne.GagName(), gagTwo.GagName(), gagThree.GagName()]);

    public void UpdateGarblerLogic(List<string> newGagListNames)
    {
        _activeGags = newGagListNames
            .Where(gagType => _gagDataHandler.AllGarblerData.Any(gag => gag.Name == gagType))
            .Select(gagType => _gagDataHandler.AllGarblerData.First(gag => gag.Name == gagType))
            .ToList();
    }

    /// <summary> Processes the input message by converting it to GagSpeak format </summary> 
    public string ProcessMessage(string inputMessage)
    {
        if (_activeGags == null || _activeGags.All(gag => gag.Name == "None")) return inputMessage;
        var outputStr = "";
        try
        {
            outputStr = ConvertToGagSpeak(inputMessage);
            _logger.LogTrace($"Converted message to GagSpeak: {outputStr}", LoggerType.GarblerCore);
        }
        catch (Exception e)
        {
            _logger.LogError($"Error processing message: {e}");
        }
        return outputStr;
    }

    /// <summary>
    /// Internal convert for gagspeak
    public string ConvertToGagSpeak(string inputMessage)
    {


        // If all gags are None, return the input message as is
        if (_activeGags.All(gag => gag.Name == "None"))
        {
            return inputMessage;
        }

        // Initialize the algorithm scoped variables 
        _logger.LogDebug($"Converting message to GagSpeak, at least one gag is not None.", LoggerType.GarblerCore);
        var finalMessage = new StringBuilder(); // initialize a stringbuilder object so we dont need to make a new string each time
        var skipTranslation = false;
        try
        {
            // Convert the message to a list of phonetics for each word
            var wordsAndPhonetics = _IPAParser.ToIPAList(inputMessage);
            // Iterate over each word and its phonetics
            foreach (var entry in wordsAndPhonetics)
            {
                var word = entry.Item1; // create a variable to store the word (which includes its puncuation)
                // If the word is "*", then toggle skip translations
                if (word == "*")
                {
                    skipTranslation = !skipTranslation;
                    finalMessage.Append(word + " "); // append the word to the string
                    continue; // Skip the rest of the loop for this word
                }
                // If the word starts with "*", toggle skip translations and remove the "*"
                if (word.StartsWith("*"))
                {
                    skipTranslation = !skipTranslation;
                }
                // If the word ends with "*", remove the "*" and set a flag to toggle skip translations after processing the word
                var toggleAfter = false;
                if (word.EndsWith("*"))
                {
                    toggleAfter = true;
                }
                // If the word is not to be translated, just add the word to the final message and continue
                if (!skipTranslation && word.Any(char.IsLetter))
                {
                    // do checks for punctuation stuff
                    var isAllCaps = word.All(c => !char.IsLetter(c) || char.IsUpper(c));       // Set to true if the full letter is in caps
                    var isFirstLetterCaps = char.IsUpper(word[0]);
                    // Extract all leading and trailing punctuation
                    var leadingPunctuation = new string(word.TakeWhile(char.IsPunctuation).ToArray());
                    var trailingPunctuation = new string(word.Reverse().TakeWhile(char.IsPunctuation).Reverse().ToArray());
                    // Remove leading and trailing punctuation from the word
                    var wordWithoutPunctuation = word.Substring(leadingPunctuation.Length, word.Length - leadingPunctuation.Length - trailingPunctuation.Length);
                    // Convert the phonetics to GagSpeak if the list is not empty, otherwise use the original word
                    var gaggedSpeak = entry.Item2.Any() ? ConvertPhoneticsToGagSpeak(entry.Item2, isAllCaps, isFirstLetterCaps) : wordWithoutPunctuation;
                    // Add the GagSpeak to the final message

                    /* ---- THE BELOW LINE WILL CAUSE LOTS OF SPAM, ONLY FOR USE WHEN DEVELOPER DEBUGGING ---- */
                    //_logger.LogTrace($"[GagGarbleManager] Converted [{leadingPunctuation}] + [{word}] + [{trailingPunctuation}]");
                    finalMessage.Append(leadingPunctuation + gaggedSpeak + trailingPunctuation + " ");
                }
                else
                {
                    finalMessage.Append(word + " "); // append the word to the string
                }
                // If the word ended with "*", toggle skip translations now
                if (toggleAfter)
                {
                    skipTranslation = !skipTranslation;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"[GagGarbleManager] Error converting from IPA Spaced to final output. Puncutation error or other type possible : {e.Message}");
        }
        return finalMessage.ToString().Trim();
    }

    /// <summary>
    /// Phonetic IPA -> Garbled sound equivalent in selected language
    /// </summary>
    public string ConvertPhoneticsToGagSpeak(List<string> phonetics, bool isAllCaps, bool isFirstLetterCapitalized)
    {
        var outputString = new StringBuilder();
        foreach (var phonetic in phonetics)
        {
            try
            {
                var gagWithMaxMuffle = _activeGags
                    .Where(gag => gag.Phonemes.ContainsKey(phonetic) && !string.IsNullOrEmpty(gag.Phonemes[phonetic].Sound))
                    .OrderByDescending(gag => gag.Phonemes[phonetic].Muffle)
                    .FirstOrDefault();
                if (gagWithMaxMuffle != null)
                {
                    var translationSound = gagWithMaxMuffle.Phonemes[phonetic].Sound;
                    outputString.Append(translationSound);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error converting phonetic {phonetic} to GagSpeak: {e.Message}");
            }
        }
        var result = outputString.ToString();
        if (isAllCaps) result = result.ToUpper();
        if (isFirstLetterCapitalized && result.Length > 0)
        {
            result = char.ToUpper(result[0]) + result.Substring(1);
        }
        return result;
    }
}
