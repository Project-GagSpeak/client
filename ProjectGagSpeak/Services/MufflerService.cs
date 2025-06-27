using CkCommons.GarblerCore;
using GagSpeak.MufflerCore;
using GagSpeak.MufflerCore.Handler;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Util;

namespace GagSpeak.Services;

/// <summary> Service for managing the gags. </summary>
public class MufflerService : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _mainConfig;
    private readonly Ipa_EN_FR_JP_SP_Handler _ipaParser;

    /// <summary>
    ///    The collected GagData for all Gags, indexed by gag name and phoneme,
    ///    taken from the dictionary of the selected dialect.
    /// </summary>
    private Dictionary<string, Dictionary<string, PhonemeProperties>> _gagData;

    /// <summary>
    ///     The collected GarblerData for all Gags.
    /// </summary>
    private List<GagData> _allGarblerData = new List<GagData>();

    /// <summary>
    ///     The Muffler GagData for the currently active Gags worn.
    /// </summary>
    private List<GagData> _activeGags;

    public MufflerService(
        ILogger<MufflerService> logger,
        GagspeakMediator mediator,
        MainConfig mainConfig,
        Ipa_EN_FR_JP_SP_Handler ipaParser, 
        ConfigFileProvider fileprovider)
        : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _ipaParser = ipaParser;

        // Try to read the JSON file and de-serialize it into the obj dictionary
        try
        {
            var json = File.ReadAllText(fileprovider.GagDataJson);
            _gagData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, PhonemeProperties>>>(json) 
                ?? new Dictionary<string, Dictionary<string, PhonemeProperties>>();
        }
        catch (FileNotFoundException)
        {
            Logger.LogError($"[IPA Parser] File does not exist");
            _gagData = new Dictionary<string, Dictionary<string, PhonemeProperties>>();
        }
        catch (Exception ex)
        {
            Logger.LogError($"[IPA Parser] An error occurred while reading the file: {ex.Message}");
            _gagData = new Dictionary<string, Dictionary<string, PhonemeProperties>>();
        }

        CreateGags();
        Mediator.Subscribe<MufflerLanguageChanged>(this, _ => RecreateGags());
    }

    private void RecreateGags()
    {
        _allGarblerData.Clear();
        CreateGags();
        Logger.LogDebug("Recreated Gags", LoggerType.GarblerCore);
    }

    private void CreateGags()
    {
        var masterList = _mainConfig.Current.LanguageDialect switch
        {
            GarbleCoreDialect.UK => GagPhonetics.MasterListEN_UK,
            GarbleCoreDialect.US => GagPhonetics.MasterListEN_US,
            GarbleCoreDialect.Spain => GagPhonetics.MasterListSP_SPAIN,
            GarbleCoreDialect.Mexico => GagPhonetics.MasterListSP_MEXICO,
            GarbleCoreDialect.France => GagPhonetics.MasterListFR_FRENCH,
            GarbleCoreDialect.Quebec => GagPhonetics.MasterListFR_QUEBEC,
            GarbleCoreDialect.Japan => GagPhonetics.MasterListJP,
            _ => throw new Exception("Invalid language")
        };

        // Assuming you want to reset the list each time you create gags
        foreach (var (gagName, phonemes) in _gagData)
            _allGarblerData.Add(new(gagName, phonemes));
    }

    public void UpdateGarblerLogic(GagType gagOne, GagType gagTwo, GagType gagThree)
        => UpdateGarblerLogic([gagOne.GagName(), gagTwo.GagName(), gagThree.GagName()]);

    public void UpdateGarblerLogic(List<string> newGagListNames)
    {
        _activeGags = newGagListNames
            .Where(gagType => _allGarblerData.Any(gag => gag.Name == gagType))
            .Select(gagType => _allGarblerData.First(gag => gag.Name == gagType))
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
            Logger.LogTrace($"Converted message to GagSpeak: {outputStr}", LoggerType.GarblerCore);
        }
        catch (Exception e)
        {
            Logger.LogError($"Error processing message: {e}");
        }
        return outputStr;
    }

    /// <summary>
    ///     Internal convert for gagspeak
    /// </summary>
    private string ConvertToGagSpeak(string inputMessage)
    {
        // If all gags are None, return the input message as is
        if (_activeGags.All(gag => gag.Name == "None"))
        {
            return inputMessage;
        }

        // Initialize the algorithm scoped variables 
        Logger.LogDebug($"Converting message to GagSpeak, at least one gag is not None.", LoggerType.GarblerCore);
        var finalMessage = new StringBuilder(); // initialize a stringbuilder object so we dont need to make a new string each time
        var skipTranslation = false;
        try
        {
            // Convert the message to a list of phonetics for each word
            var wordsAndPhonetics = _ipaParser.ToIPAList(inputMessage);
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
            Logger.LogError($"[GagGarbleManager] Error converting from IPA Spaced to final output. Puncutation error or other type possible : {e.Message}");
        }
        return finalMessage.ToString().Trim();
    }

    /// <summary>
    ///     Phonetic IPA -> Garbled sound equivalent in selected language
    /// </summary>
    private string ConvertPhoneticsToGagSpeak(List<string> phonetics, bool isAllCaps, bool isFirstLetterCapitalized)
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
                Logger.LogError($"Error converting phonetic {phonetic} to GagSpeak: {e.Message}");
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
