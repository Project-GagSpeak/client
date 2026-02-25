using CkCommons.GarblerCore;
using GagSpeak.MufflerCore;
using GagSpeak.MufflerCore.Handler;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagspeakAPI.Util;
using OtterGui.Extensions;
using System.Text.RegularExpressions;
using static FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkHistory.Delegates;

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
    private static Dictionary<string, Dictionary<string, PhonemeProperties>> _garbleData;

    /// <summary>
    ///     The collected GarblerData for all Gags.
    /// </summary>
    private static List<GarbleData> _allGarblerData = new List<GarbleData>();

    /// <summary>
    ///     The Muffler GagData for the currently active Gags worn.
    /// </summary>
    private static List<GarbleData> _activeGags;

    private static GagMuffleType _activeMuffleType = GagMuffleType.None;

    public MufflerService(ILogger<MufflerService> logger, GagspeakMediator mediator, MainConfig mainConfig,
        Ipa_EN_FR_JP_SP_Handler ipaParser, ConfigFileProvider fileprovider)
        : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _ipaParser = ipaParser;

        // Try to read the JSON file and de-serialize it into the obj dictionary
        try
        {
            var json = File.ReadAllText(fileprovider.GagDataJson);
            _garbleData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, PhonemeProperties>>>(json)
                ?? new Dictionary<string, Dictionary<string, PhonemeProperties>>();
        }
        catch (FileNotFoundException)
        {
            Logger.LogError($"[IPA Parser] File does not exist");
            _garbleData = new Dictionary<string, Dictionary<string, PhonemeProperties>>();
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"[IPA Parser] An error occurred while reading the file: {ex.Message}");
            _garbleData = new Dictionary<string, Dictionary<string, PhonemeProperties>>();
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
        foreach (var (gagName, phonemes) in _garbleData)
            _allGarblerData.Add(new(gagName, phonemes));
    }

    // Change to GagTypes soon.
    public void UpdateGarblerLogic(List<string> newGagListNames, GagMuffleType newMuffleType)
    {
        _activeGags = newGagListNames
            .Where(gagType => _allGarblerData.Any(gag => gag.Name == gagType))
            .Select(gagType => _allGarblerData.First(gag => gag.Name == gagType))
            .ToList();

        _activeMuffleType = newMuffleType;
    }

    /// <summary>
    ///     Processes the input message by converting it to GagSpeak format <br />
    ///     (we should probably consider using a ref string for this. Idk.
    /// </summary>
    public string GarbleMessage(string inputMessage, bool allowEmotes = false)
    {
        // Return the normal message if no active gags are present.
        if (_activeGags is null || _activeGags.All(gag => gag.Name == "None")) 
            return inputMessage;

        // Otherwise, assume initially a blank message. If errors occur, output a blank message.
        var outputStr = "";   
        try
        {
            outputStr = GarbleMessageInternal(inputMessage, allowEmotes);
            Logger.LogTrace($"Garlbed msg: {outputStr}", LoggerType.GarblerCore);
        }
        catch (Bagagwa e)
        {
            Logger.LogError($"Error garbling message: {e}");
        }
        return outputStr; 
    }

    /// <summary>
    ///     Internal convert for gagspeak
    /// </summary>
    private string GarbleMessageInternal(string inputMessage, bool allowEmotes)
    {
        // If all gags are None, return the input message as is
        if (_activeGags.All(gag => gag.Name == "None"))
            return inputMessage;

        // Initialize the algorithm scoped variables
        Logger.LogDebug($"Converting message to GagSpeak, at least one gag is not None.", LoggerType.GarblerCore);
        var finalMessage = new StringBuilder();
        var skipTranslation = false;
        try
        {
            // Convert the message to a list of phonetics for each word
            var wordsAndPhonetics = _ipaParser.ToIPAList(inputMessage);
            // Iterate over each word and its phonetics
            foreach (var (parsed, idx) in wordsAndPhonetics.WithIndex())
            {
                // Toggle skipping translation if an RP post (*)
                if (parsed.Word == "*")
                {
                    skipTranslation = !skipTranslation;
                    finalMessage.Append($"{parsed.Word} ");
                    continue;
                }
                // If the word starts with "*", toggle skip translations and remove the "*"
                if (parsed.Word.StartsWith('*'))
                    skipTranslation = !skipTranslation;

                // Init a 'toggleAfter' variable, that dictates if we should toggle translation after, treating this as the last word before toggling.
                var toggleAfter = false;

                // Immidiately assume true if it ends with a *
                if (parsed.Word.EndsWith('*'))
                    toggleAfter = true;

                // If we are allowing emotes, we can mark toggle afer as inverse of translation, then set it to skip.
                if (allowEmotes && parsed.Word.Length > 2)
                {
                    // Only validate if a valid emote.
                    if (parsed.Word.StartsWith(':') && parsed.Word.EndsWith(':') && CosmeticLabels.NameToEmote.ContainsKey(parsed.Word[1..^1]))
                    {
                        toggleAfter = !skipTranslation;
                        skipTranslation = true;
                    }
                }

                // If we should not skip translation, and any letters are present, attempt the parse
                if (!skipTranslation && parsed.Word.Any(char.IsLetter))
                {
                    // do checks for punctuation stuff
                    var isAllCaps = parsed.Word.All(c => !char.IsLetter(c) || char.IsUpper(c));       // Set to true if the full letter is in caps
                    var isFirstLetterCaps = char.IsUpper(parsed.Word[0]);
                    // Cache the leading and trailing punctuation to inject back in after.
                    var leadingPunctuation = new string([.. parsed.Word.TakeWhile(char.IsPunctuation)]);
                    var trailingPunctuation = new string([.. parsed.Word.Reverse().TakeWhile(char.IsPunctuation).Reverse()]);

                    // Sanitize to the word we will garble
                    var sanitizedWord = parsed.Word.Substring(leadingPunctuation.Length, parsed.Word.Length - leadingPunctuation.Length - trailingPunctuation.Length);
                    // Convert based on phonetics.
                    var converted = parsed.Found
                        ? GarbleWithPhonetics(sanitizedWord, parsed.Phonetics, isAllCaps, isFirstLetterCaps)
                        : GarbleWithFallback(sanitizedWord, isAllCaps, isFirstLetterCaps);

                    if (converted.Length is not 0 || idx == wordsAndPhonetics.Count - 1)
                    {
                        // Append to the final message
                        finalMessage.Append($"{leadingPunctuation}{converted}{trailingPunctuation} ");

                        /* ---- THE BELOW LINE WILL CAUSE LOTS OF SPAM, ONLY FOR USE WHEN DEVELOPER DEBUGGING ---- */
                        Logger.LogTrace($"Converting word [{parsed.Word}] with phonetics [{string.Join(", ", parsed.Phonetics)}]", LoggerType.GarblerCore);
                    }
                }
                else
                {
                    finalMessage.Append($"{parsed.Word} ");
                }

                // If the word ended with "*", toggle skip translations now
                if (toggleAfter)
                    skipTranslation = !skipTranslation;
            }
        }
        catch (Bagagwa e)
        {
            Logger.LogError($"Error converting to final output: {e.Message}");
        }

        return finalMessage.ToString().Trim();
    }

    /// <summary>
    ///     Phonetic IPA -> Garbled sound equivalent in selected language
    /// </summary>
    private string GarbleWithPhonetics(string word, List<string> phonetics, bool isAllCaps, bool isFirstLetterCapitalized)
    {
        Logger.LogTrace($"Garbling word [{word}] with phonetics [{string.Join(", ", phonetics)}]", LoggerType.GarblerCore);
        // Otherwise, parse it out normally.
        var outputString = new StringBuilder();
        foreach (var phonetic in phonetics)
        {
            try
            {
                var gagWithMaxMuffle = _activeGags
                    .Where(gag => gag.Phonemes.ContainsKey(phonetic))
                    .OrderByDescending(gag => gag.Phonemes[phonetic].Muffle)
                    .FirstOrDefault();
                if (gagWithMaxMuffle != null)
                {
                    var translationSound = gagWithMaxMuffle.Phonemes[phonetic].Sound;
                    outputString.Append(translationSound);
                }
            }
            catch (Bagagwa e)
            {
                Logger.LogError($"Error converting phonetic {phonetic} to GagSpeak: {e.Message}");
            }
        }

        var result = outputString.ToString();

        // If the combined result is empty, return the GetNoSoundWord instead (No Phonetic Matches)
        if (string.IsNullOrWhiteSpace(result))
            return GetNoSoundWord(word, isAllCaps, isFirstLetterCapitalized);

        // Ensure the output is uppercase.
        if (isAllCaps)
            result = result.ToUpper();

        // Capitalize the first letter if the original one was.
        if (isFirstLetterCapitalized && result.Length > 0)
            result = char.ToUpper(result[0]) + result[1..];

        return result;
    }

    /// <summary>
    ///    Handles conversion of speech that does not contain recognized words (e.g. misspellings, names, etc.)
    /// </summary>
    private string GarbleWithFallback(string word, bool isAllCaps, bool isFirstLetterCapitalized)
    {
        // Return the no sound converter for no sound words
        if (_activeMuffleType.HasFlag(GagMuffleType.NoSound))
            return GetNoSoundWord(word, isAllCaps, isFirstLetterCapitalized);

        // If this fallback word only contains classic muffle characters, then return it as is.
        // (Allows mhm, mmph, mnph ext..)
        if (Regex.IsMatch(word, @"^[mpgfh\p{P}]+$"))
            return word;

        // Get the prioritized muffle type.
        var toMuffle = _activeMuffleType.ToPrioritizedType();
        // If none, return the word
        if (!FallbackGarbleData.GagDataMap.TryGetValue(toMuffle, out var garbleStrings))
            return word;

        // Iterate through the word, applying translations until we reach the word length.
        var muffledWord = string.Empty;
        while (muffledWord.Length < word.Length)
        {
            muffledWord += garbleStrings[Random.Shared.Next(garbleStrings.Count)];
        }
        // Trim to match word length.
        muffledWord = muffledWord.Substring(0, word.Length);
        
        // Capitalize if we should
        if (isAllCaps)
            muffledWord = muffledWord.ToUpper();
        // Capitalize the first letter if the original one was.
        if (isFirstLetterCapitalized && muffledWord.Length > 0)
            muffledWord = char.ToUpper(muffledWord[0]) + muffledWord[1..];
        
        // Return the muffled word.
        return muffledWord;
    }

    private string GetNoSoundWord(string word, bool isAllCaps, bool isFirstLetterCapitalized)
    {
        // Always empty if less than 7 letters.
        if (string.IsNullOrEmpty(word) || word.Length < 3)
            return string.Empty;

        // 25% chance to generate periods
        if (Random.Shared.NextDouble() >= (isAllCaps ? 0.5 : 0.25))
            return string.Empty;

        var sb = new StringBuilder();
        int dots = Random.Shared.Next(0, 3);

        // 50% chance to prepend letters. Case dependant on isAllCaps || isFirstLetterCapitalized.
        if (Random.Shared.NextDouble() < 0.50)
        {
            char[] choices = { 'm', 'n', 'h' };
            // Ensure letters always follow with more than one dot.
            dots = Math.Clamp(dots, 2, 3);
            // get sound cluster length.
            var soundLen = Random.Shared.Next(1, 3);
            for (int i = 0; i < soundLen; i++)
            {
                if (i == 0 && (isAllCaps || isFirstLetterCapitalized))
                    sb.Insert(0, isAllCaps ? char.ToUpper(choices[Random.Shared.Next(choices.Length)]) : choices[Random.Shared.Next(choices.Length)]);
                else
                    sb.Insert(0, choices[Random.Shared.Next(choices.Length)]);
            }
        }

        // Then the dots after.
        sb.Append('.', dots);

        return sb.ToString();
    }

    public static GagMuffleType MuffleType(IEnumerable<GagType> gagTypes)
    {
        var combinedMuffleType = GagMuffleType.None;
        foreach (var gagType in gagTypes)
            combinedMuffleType |= MuffleType(gagType);
        return combinedMuffleType;
    }

    public static GagMuffleType MuffleType(GagType gagType)
    {
        return gagType switch
        {
            GagType.None => GagMuffleType.None,
            GagType.BallGag => GagMuffleType.MouthFull,
            GagType.BallGagMask => GagMuffleType.MouthFull,
            GagType.BambooGag => GagMuffleType.MouthFull,
            GagType.BeltStrapGag => GagMuffleType.MouthClosed,
            GagType.BitGag => GagMuffleType.MouthFull,
            GagType.BitGagPadded => GagMuffleType.MouthFull,
            GagType.BoneGag => GagMuffleType.MouthFull,
            GagType.BoneGagXL => GagMuffleType.MouthFull,
            GagType.CandleGag => GagMuffleType.MouthFull,
            GagType.CageMuzzle => GagMuffleType.None,
            GagType.CleaveGag => GagMuffleType.MouthClosed,
            GagType.ChloroformGag => GagMuffleType.MouthClosed,
            GagType.ChopStickGag => GagMuffleType.MouthOpen,
            GagType.ClothWrapGag => GagMuffleType.MouthClosed,
            GagType.ClothStuffingGag => GagMuffleType.MouthFull,
            GagType.CropGag => GagMuffleType.MouthClosed,
            GagType.CupHolderGag => GagMuffleType.MouthClosed,
            GagType.DeepthroatPenisGag => GagMuffleType.MouthFull,
            GagType.DentalGag => GagMuffleType.MouthOpen,
            GagType.DildoGag => GagMuffleType.MouthFull,
            GagType.DuctTapeGag => GagMuffleType.MouthClosed,
            GagType.DusterGag => GagMuffleType.MouthClosed,
            GagType.FunnelGag => GagMuffleType.MouthOpen,
            GagType.FuturisticHarnessBallGag => GagMuffleType.MouthFull,
            GagType.FuturisticHarnessPanelGag => GagMuffleType.MouthFull,
            GagType.GasMask => GagMuffleType.None,
            GagType.HarnessBallGag => GagMuffleType.MouthFull,
            GagType.HarnessBallGagXL => GagMuffleType.MouthFull,
            GagType.HarnessPanelGag => GagMuffleType.MouthFull,
            GagType.HookGagMask => GagMuffleType.MouthOpen,
            GagType.InflatableHood => GagMuffleType.MouthFull,
            GagType.LargeDildoGag => GagMuffleType.MouthFull,
            GagType.LatexHood => GagMuffleType.MouthClosed,
            GagType.LatexBallMuzzleGag => GagMuffleType.MouthFull,
            GagType.LatexPostureCollarGag => GagMuffleType.MouthClosed,
            GagType.LeatherCorsetCollarGag => GagMuffleType.MouthClosed,
            GagType.LeatherHood => GagMuffleType.MouthClosed,
            GagType.LipGag => GagMuffleType.MouthOpen,
            GagType.MedicalMask => GagMuffleType.None,
            GagType.MuzzleGag => GagMuffleType.MouthClosed,
            GagType.PantyStuffingGag => GagMuffleType.MouthFull,
            GagType.PlasticWrapGag => GagMuffleType.MouthClosed,
            GagType.PlugGag => GagMuffleType.MouthOpen,
            GagType.PonyGag => GagMuffleType.MouthOpen,
            GagType.PumpGaglv1 => GagMuffleType.MouthFull,
            GagType.PumpGaglv2 => GagMuffleType.MouthFull,
            GagType.PumpGaglv3 => GagMuffleType.MouthFull,
            GagType.PumpGaglv4 => GagMuffleType.NoSound,
            GagType.RibbonGag => GagMuffleType.MouthFull,
            GagType.RingGag => GagMuffleType.MouthOpen,
            GagType.RopeGag => GagMuffleType.MouthClosed,
            GagType.ScarfGag => GagMuffleType.MouthClosed,
            GagType.SensoryDeprivationHood => GagMuffleType.MouthFull,
            GagType.SockStuffingGag => GagMuffleType.MouthFull,
            GagType.SpiderGag => GagMuffleType.MouthOpen,
            GagType.TentacleGag => GagMuffleType.MouthFull,
            GagType.WebGag => GagMuffleType.MouthClosed,
            GagType.WhiffleGag => GagMuffleType.MouthFull,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
