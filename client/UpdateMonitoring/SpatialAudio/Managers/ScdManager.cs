using Dalamud.Plugin.Services;

namespace GagSpeak.UpdateMonitoring.SpatialAudio.Managers;
public class ScdManager
{
    private readonly ILogger<ScdManager> _logger;
    private readonly IDataManager _dataManager;
    private string _configDirectory;
    public string AudioFilesFolder => Path.Combine(_configDirectory, "audiofiles");
    public string Extension { get; } = "scd";

    public ScdManager(ILogger<ScdManager> logger, IDataManager dataManager, string configDirectory)
    {
        _logger = logger;
        _dataManager = dataManager;
        _configDirectory = configDirectory;
    }

    public bool FileExists(string path) => _dataManager.FileExists(path) || GetReplacePath(path, out var _);
    public bool DoDebug(string path) => path.Contains($".{Extension}");

    public bool GetReplacePath(string path, out string replacePath)
    {
        // always return false for now until we figure more out.
        replacePath = null!;
        // check if we are being vibrated, and if so to dynamically update the volume
        if (path == "gagspeak/sound/vibrator_active.scd")
        {
            replacePath = Path.Combine(AudioFilesFolder, "ScdVibeActive.scd").Replace('\\', '/');
            return true;
            // Handle the special case
            /*            if (VibeMappings.TryGetValue(NextVibrationToPlay, out var dynamicVibePath))
                        {
                            replacePath = Path.Combine(AudioFilesFolder, dynamicVibePath).Replace('\\', '/');
                            return true;
                        }*/
        }
        // otherwise, check remaining mappings.
        if (PathMappings.TryGetValue(path, out var mappedPath))
        {
            replacePath = Path.Combine(AudioFilesFolder, mappedPath).Replace('\\', '/');
            return true;
        }
        return false;
    }

    // The next VibrationAudioPath to use
    CurrentVibeAudio NextVibrationToPlay { get; set; } = CurrentVibeAudio.Off;
    // Dynamically update the replaced Vibrator Audio Path
    public void SetVibrationSoundType(CurrentVibeAudio type) => NextVibrationToPlay = type;

    public static readonly Dictionary<string, string> PathMappings = new()
    {
        { "gagspeak/sound/gagged_idle.scd",                "ScdGaggedIdle.scd" },
        { "gagspeak/sound/gagged_talking.scd",             "ScdGaggedTalking.scd" },
        { "gagspeak/sound/restrained_ropes_struggle.scd",  "ScdRopesStruggle.scd" },
        { "gagspeak/sound/restrained_chains_struggle.scd", "ScdChainsStruggle.scd" },
        { "gagspeak/sound/restrained_leather_struggle.scd","ScdLeatherStruggle.scd" },
        { "gagspeak/sound/restrained_latex_struggle.scd",  "ScdLatexStruggle.scd" },
        { "gagspeak/sound/stimulated_light.scd",           "ScdStimulatedLight.scd" },
        { "gagspeak/sound/stimulated_medium.scd",          "ScdStimulatedMedium.scd" },
        { "gagspeak/sound/stimulated_heavy.scd",           "ScdStimulatedHeavy.scd" },
        { "gagspeak/sound/vibrator_active.scd",            "ScdVibeActive.scd" },
    };

    // List of Vibration Audios to play based on vibrator current intensity.
    public static readonly Dictionary<CurrentVibeAudio, string> VibeMappings = new()
    {
        { CurrentVibeAudio.Off, "ScdVibeOff.scd" },
        { CurrentVibeAudio.StepOne, "ScdVibeStepOne.scd" },
        { CurrentVibeAudio.StepTwo, "ScdVibeStepTwo.scd" },
        { CurrentVibeAudio.StepThree, "ScdVibeStepThree.scd" },
        { CurrentVibeAudio.StepFour, "ScdVibeStepFour.scd" },
        { CurrentVibeAudio.StepFive, "ScdVibeStepFive.scd" },
        { CurrentVibeAudio.StepSix, "ScdVibeStepSix.scd" },
        { CurrentVibeAudio.StepSeven, "ScdVibeStepSeven.scd" },
        { CurrentVibeAudio.StepEight, "ScdVibeStepEight.scd" },
        { CurrentVibeAudio.StepNine, "ScdVibeStepNine.scd" },
        { CurrentVibeAudio.StepTen, "ScdVibeStepTen.scd" },
        { CurrentVibeAudio.StepEleven, "ScdVibeStepEleven.scd" },
        { CurrentVibeAudio.StepTwelve, "ScdVibeStepTwelve.scd" },
        { CurrentVibeAudio.StepThirteen, "ScdVibeStepThirteen.scd" },
        { CurrentVibeAudio.StepFourteen, "ScdVibeStepFourteen.scd" },
        { CurrentVibeAudio.StepFifteen, "ScdVibeStepFifteen.scd" },
        { CurrentVibeAudio.StepSixteen, "ScdVibeStepSixteen.scd" },
        { CurrentVibeAudio.StepSeventeen, "ScdVibeStepSeventeen.scd" },
        { CurrentVibeAudio.StepEighteen, "ScdVibeStepEighteen.scd" },
        { CurrentVibeAudio.StepNineteen, "ScdVibeStepNineteen.scd" },
        { CurrentVibeAudio.Max, "ScdVibeMax.scd" },
        { CurrentVibeAudio.RampUp, "ScdVibeRampUp.scd" },
        { CurrentVibeAudio.RampDown, "ScdVibeRampDown.scd" },
    };

    public enum CurrentVibeAudio
    {
        Off,
        StepOne,
        StepTwo,
        StepThree,
        StepFour,
        StepFive,
        StepSix,
        StepSeven,
        StepEight,
        StepNine,
        StepTen,
        StepEleven,
        StepTwelve,
        StepThirteen,
        StepFourteen,
        StepFifteen,
        StepSixteen,
        StepSeventeen,
        StepEighteen,
        StepNineteen,
        Max,
        RampUp,
        RampDown,
    }
}
