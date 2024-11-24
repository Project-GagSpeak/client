using Dalamud.Plugin.Services;

namespace GagSpeak.UpdateMonitoring.SpatialAudio.Managers;

public class AvfxManager
{
    private readonly ILogger<AvfxManager> _logger;
    private readonly IDataManager _dataManager;
    private string _configDirectory;
    public string AvfxFilesFolder => Path.Combine(_configDirectory, "audiofiles");
    public string Extension { get; } = "avfx";

    public AvfxManager(ILogger<AvfxManager> logger, IDataManager dataManager, string configDirectory)
    {
        _logger = logger;
        _dataManager = dataManager;
        _configDirectory = configDirectory;
    }

    public List<string> GetAvfxFiles()
    {
        var files = new List<string>();
        foreach (var mapping in PathMappings)
        {
            files.Add(mapping.Key);
        }
        return files;
    }

    public bool FileExists(string path) => _dataManager.FileExists(path) || GetReplacePath(path, out var _);
    public bool GetReplacePath(string path, out string replacePath)
    {
        // Assume not found
        replacePath = null!;
        // Check if path is in our PathMappings
        if (PathMappings.TryGetValue(path, out var mappedPath))
        {
            // if so, construct the new path to use for replacement
            replacePath = Path.Combine(AvfxFilesFolder, mappedPath).Replace('\\', '/');
            // validate its existance before returning true.
            if (File.Exists(replacePath))
            {
                _logger.LogInformation("Replacement VFX Path exists, playing on target!");
                return true;
            }
        }
        return false;
    }

    // =================== GagSpeak Immersive Dynamic Audio Sound System ===================
    // LEFT = GamePath, RIGHT = ReplacementPath
    public static readonly Dictionary<string, string> PathMappings = new()
    {
        // Can play a variety, perhaps 5-10 variants, at random, of passive idle gagged sounds.
        // This should include very minimal, quiet sounds barely audible one that is gagged
        // would make while not struggling or complaining.
        { "gagspeak/vfx/gagged_idle.avfx",                "AvfxGaggedIdle.avfx" },
        
        // Plays a variety of audio clips for a person talking in chat will gagged.
        // Would need 2-3 variants or normal talking, mumbled/shy talking, upset talking, or shouting.
        // Each variant should be of a separate length of talking as well, to reflect the length of the text.
        { "gagspeak/vfx/gagged_talking.avfx",             "AvfxGaggedTalking.avfx" }, 

        // Plays a variety of short and subtle audio sounds for someone restrained in ropes.
        // included occasional, very subtle sounds of moving around in ropes, light rope stretches and things.
        { "gagspeak/vfx/restrained_ropes_idle.avfx",  "AvfxRopesStruggle.avfx" },
        // Plays a variety of sounds when the player tries to struggle out with the
        // struggle simulator, or when they move while restrained.
        // About 1-3 passive subtle struggle rope sounds (no voice)
        { "gagspeak/vfx/restrained_ropes_struggle.avfx",  "AvfxRopesStruggle.avfx" },

        // Plays a variety of short and subtle audio sounds for someone restrained in metal.
        // included occasional, very subtle sounds of chain movement, light clinks and such.
        { "gagspeak/vfx/restrained_chains_idle.avfx", "AvfxChainsStruggle.avfx" },
        // Plays a variety of sounds when the player tries to struggle out with the
        // struggle simulator, or when they move while restrained.
        // About 1-2 passive chain movement sounds(no voice)
        { "gagspeak/vfx/restrained_chains_struggle.avfx", "AvfxChainsStruggle.avfx" },

        // Plays a variety of short and subtle audio sounds for someone restrained in leather.
        // included occasional, very subtle sounds of leather stretching / tugging.
        { "gagspeak/vfx/restrained_leather_idle.avfx","AvfxLeatherStruggle.avfx" },
        // Plays a variety of sounds when the player tries to struggle out with the
        // struggle simulator, or when they move while restrained.
        // About 1-3 passive subtle leather sounds (no voice)
        { "gagspeak/vfx/restrained_leather_struggle.avfx","AvfxLeatherStruggle.avfx" },

        // Plays a variety of short and subtle audio sounds for someone restrained in latex.
        // included occasional, very subtle sounds of latex movement, squeaks, friction, stretching.
        { "gagspeak/vfx/restrained_latex_idle.avfx",  "AvfxLatexStruggle.avfx" },
        // Plays a variety of sounds when the player tries to struggle out with the
        // struggle simulator, or when they move while restrained.
        // About 1-3 more noticeable latex audio, potentially variants of the former that are more noticeable
        { "gagspeak/vfx/restrained_latex_struggle.avfx",  "AvfxLatexStruggle.avfx" },

        // No Audio requires, but overlays a pumping visual effect with a blue/purple/red overlay.
        // This overlay also plays the sound of a heartbeat, getting faster as the stimulation rises.
        { "gagspeak/vfx/stimulated_light.avfx",           "AvfxStimulatedLight.avfx" },
        { "gagspeak/vfx/stimulated_medium.avfx",          "AvfxStimulatedMedium.avfx" },
        { "gagspeak/vfx/stimulated_heavy.avfx",           "AvfxStimulatedHeavy.avfx" },

        // vibrator sound the plays while vibrated. In its own category entirely. 
        { "gagspeak/vfx/vibrator_active.avfx",            "AvfxVibeActive.avfx" },
    };
}
