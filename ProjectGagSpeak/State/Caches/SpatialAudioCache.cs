using GagSpeak.GameInternals.Structs;
using GagSpeak.Services.Configs;
using GagSpeak.State.Models;
using OtterGui.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.State.Caches;

/// <summary>
///     Internal Cache to help identify all custom Spatial Audio Resource paths for the Spatial Audio System. <para />
///     Can also cache the static pointers or spawned vfx items to help with cleanup and references.
/// </summary>
public unsafe class SpatialAudioCache : IDisposable
{
    private readonly ILogger<SpatialAudioCache> _logger;
    public SpatialAudioCache(ILogger<SpatialAudioCache> logger)
    {
        _logger = logger;
    }

    private readonly ConcurrentDictionary<ActorVfx, VfxSpawnItem> _trackedVfxs = [];
    private readonly List<VfxLoopItem> _trackedVfxsToLoop = [];

    // make absolutely sure nothing external can manipulate these.
    public IReadOnlyDictionary<ActorVfx, VfxSpawnItem> Vfxs => _trackedVfxs;
    public IReadOnlyList<VfxLoopItem> VfxsToLoop => _trackedVfxsToLoop;
    public bool CustomVfxActive => Vfxs.Count > 0;

    public void Dispose()
        => ClearCachedVfxs();

    public void AddTrackedVfx(VfxStruct* vfx, string path, SpawnType type, bool canLoop)
    {
        if (vfx is null)
            return;
        // create the key.
        var key = new ActorVfx(vfx, path);
        // do not track what is already tracked.
        if (_trackedVfxs.ContainsKey(key)) 
            return;
        // create the item.
        _trackedVfxs.TryAdd(key, new(path, type, canLoop));
    }

    public void RemoveTrackedVfx(nint data)
    {
        if (!GetVfx(data, out var vfx)) return;
        var item = Vfxs[vfx];

        // Do not include looping vfx spawns, we do that ourselves. (was here before)

        // Remove the vfx
        _trackedVfxs.Remove(vfx, out _); // Simply removes it from the list, not calls the remove actorVfx
    }

    // Remove all passed in vfxloopitems from the tracked vfx loops.
    public void RemoveLoopedVfxs(IEnumerable<VfxLoopItem> items)
        => _trackedVfxsToLoop.RemoveAll(items.Contains);

    public void ClearCachedVfxs()
    {
        foreach (var vfx in _trackedVfxs)
        {
            // calls the actual remove function sig, which then calls the interop removed.
            if (vfx.Key == null) continue;
            // Remove the vfx
            _trackedVfxs.Remove(vfx.Key, out _); // Simply removes it from the list, not calls the remove actorVfx
        }
        _trackedVfxs.Clear();
        _trackedVfxsToLoop.Clear();
    }

    public bool GetVfx(nint data, out ActorVfx vfx)
    {
        vfx = null!;
        if (data == nint.Zero || Vfxs.Count == 0) return false;
        return Vfxs.Keys.FindFirst(x => data == (nint)x.Vfx, out vfx!);
    }

    public List<string> GetCustomScdPaths()
    => CustomScdPaths.Keys.ToList();
    public List<string> GetCustomAvfxPaths()
        => CustomAvfxPaths.Keys.ToList();
    public List<string> GetValidAvfxPaths()
        => CustomAvfxPaths
            .Where(kvp => Path.Exists(Path.Combine(ConfigFileProvider.SpatialDirectory, "effects", kvp.Value)))
            .Select(kvp => kvp.Key)
            .ToList();

    public List<string> GetValidScdPaths()
        => CustomScdPaths
            .Where(kvp => Path.Exists(Path.Combine(ConfigFileProvider.SpatialDirectory, "sounds", kvp.Value)))
            .Select(kvp => kvp.Key)
            .ToList();

    public bool FileExists(string path)
        => Svc.Data.FileExists(path) // this would mean the file is a valid gamepath. (can throw unhandled exception for some damn reason).
        || TryGetReplacedPath(path, out var _); // this would mean it was a valid custom path.

    public bool TryGetReplacedPath(string path, [NotNullWhen(true)] out string? replacedPath)
    {
        replacedPath = null;
        // locate the custom sound path mappings.
        if (path.Contains("gagspeak/sound/"))
        {
            if (CustomScdPaths.TryGetValue(path, out var mappedSoundPath))
            {
                var fullPath = Path.Combine(ConfigFileProvider.SpatialDirectory, "sounds", mappedSoundPath);
                // if the path does not exist, return false.
                if (!Path.Exists(fullPath))
                {
                    _logger.LogWarning($"Mapped path '{mappedSoundPath}' does not exist in the spatial audio sound directory");
                    return false;
                }
                replacedPath = fullPath.Replace('\\', '/');
                return true;
            }
            return false;
        }
        // or locate any avfx path.
        else if (path.Contains("gagspeak/vfx/"))
        {
            if (CustomAvfxPaths.TryGetValue(path, out var mappedEffectPath))
            {
                var fullPath = Path.Combine(ConfigFileProvider.SpatialDirectory, "effects", mappedEffectPath);
                // if the path does not exist, return false.
                if (!Path.Exists(fullPath))
                {
                    _logger.LogWarning($"Mapped path '{mappedEffectPath}' does not exist in the spatial audio effect directory");
                    return false;
                }
                replacedPath = fullPath.Replace('\\', '/');
                return true;
            }
            return false;
        }
        // reject if not in mappings.
        return false;
    }

    // LEFT = GamePath, RIGHT = ReplacementPath
    public static readonly Dictionary<string, string> CustomScdPaths = new()
    {
        { "gagspeak/sound/gagged_idle.scd",             "gagged_idle.scd" },
        { "gagspeak/sound/gagged_talking.scd",          "gagged_talking.scd" },
        { "gagspeak/sound/ropes_struggle.scd",          "ropes_struggle.scd" },
        { "gagspeak/sound/chains_struggle.scd",         "chains_struggle.scd" },
        { "gagspeak/sound/leather_struggle.scd",        "leather_struggle.scd" },
        { "gagspeak/sound/latex_struggle.scd",          "latex_struggle.scd" },
        { "gagspeak/sound/arousal_feeble.scd",          "arousal_feeble.scd" },
        { "gagspeak/sound/arousal_weak.scd",            "arousal_weak.scd" },
        { "gagspeak/sound/arousal_light.scd",           "arousal_light.scd" },
        { "gagspeak/sound/arousal_mild.scd",            "arousal_mild.scd" },
        { "gagspeak/sound/arousal_strong.scd",          "arousal_strong.scd" },
        { "gagspeak/sound/arousal_intense.scd",         "arousal_intense.scd" },
        { "gagspeak/sound/arousal_overwhelming.scd",    "arousal_overwhelming.scd" },
        { "gagspeak/sound/arousal_unbearable.scd",      "arousal_unbearable.scd" },
    };

    public static readonly Dictionary<string, string> CustomAvfxPaths = new()
    {
        // Can play a variety, perhaps 5-10 variants, at random, of passive idle gagged sounds.
        // Note we only need 1 avfx file, as an scd can randomize which soundbyte plays, adding for
        // variety, and not 'repedative, annoying sounds'. Should be barely audible, mostly subtle drools.
        // Act as a passive ambiance.
        { "gagspeak/vfx/gag_ambiance.avfx",  "gag_ambiance.avfx" },
        
        // When a player speaks while gagged. Should contain different variants for different lengths
        // need seperate ones for these as they are used depending on text format and length.
        { "gagspeak/vfx/gag_speak_brief.avfx",         "gag_speak_brief.avfx" },
        { "gagspeak/vfx/gag_speak_brief_shy.avfx",     "gag_speak_brief_shy.avfx" },
        { "gagspeak/vfx/gag_speak_brief_loud.avfx",    "gag_speak_brief_loud.avfx" },
        { "gagspeak/vfx/gag_speak_short.avfx",         "gag_speak_short.avfx" },
        { "gagspeak/vfx/gag_speak_short_shy.avfx",     "gag_speak_short_shy.avfx" },
        { "gagspeak/vfx/gag_speak_short_loud.avfx",    "gag_speak_short_loud.avfx" },
        { "gagspeak/vfx/gag_speak_medium.avfx",        "gag_speak_medium.avfx" },
        { "gagspeak/vfx/gag_speak_medium_shy.avfx",    "gag_speak_medium_shy.avfx" },
        { "gagspeak/vfx/gag_speak_medium_loud.avfx",   "gag_speak_medium_loud.avfx" },
        { "gagspeak/vfx/gag_speak_long.avfx",          "gag_speak_long.avfx" },
        { "gagspeak/vfx/gag_speak_long_shy.avfx",      "gag_speak_long_shy.avfx" },
        { "gagspeak/vfx/gag_speak_long_loud.avfx",     "gag_speak_long_loud.avfx" },

        // Randomly plays a variety of sounds for rope-bondage.
        { "gagspeak/vfx/ropes_idle.avfx",       "bound_ropes_idle.avfx" },
        { "gagspeak/vfx/ropes_struggle.avfx",   "bound_ropes_struggle.avfx" },
        { "gagspeak/vfx/ropes_walk.avfx",       "bound_ropes_walk.avfx" },
        { "gagspeak/vfx/ropes_run.avfx",        "bound_ropes_run.avfx" },

        // Randomly plays a variety of sounds for chain-bondage.
        { "gagspeak/vfx/chains_idle.avfx",      "bound_chains_idle.avfx" },
        { "gagspeak/vfx/chains_struggle.avfx",  "bound_chains_struggle.avfx" },
        { "gagspeak/vfx/chains_walk.avfx",      "bound_chains_walk.avfx" },
        { "gagspeak/vfx/chains_run.avfx",       "bound_chains_run.avfx" },

        // Randomly plays a variety of sounds for leather-bondage.
        { "gagspeak/vfx/leather_idle.avfx",     "bound_leather_idle.avfx" },
        { "gagspeak/vfx/leather_struggle.avfx", "bound_leather_struggle.avfx" },
        { "gagspeak/vfx/leather_walk.avfx",     "bound_leather_walk.avfx" },
        { "gagspeak/vfx/leather_run.avfx",      "bound_leather_run.avfx" },

        // Randomly plays a variety of sounds for latex-bondage.
        { "gagspeak/vfx/latex_idle.avfx",       "bound_latex_idle.avfx" },
        { "gagspeak/vfx/latex_struggle.avfx",   "bound_latex_struggle.avfx" },
        { "gagspeak/vfx/latex_walk.avfx",       "bound_latex_walk.avfx" },
        { "gagspeak/vfx/latex_run.avfx",        "bound_latex_run.avfx" },

        // A quiet ambient sound for other players while a vibrator is active on them.
        { "gagspeak/vfx/vibrator_active.avfx",  "vibrator_active.avfx" },


        // --------------------- Arousal Effects ---------------------
        // Not sure how to determine other players arousal, but also think it only madders if for us.
        // furthermore, due to us not knowing how to directly impact or change an effect without re-summoning,
        // we would need different effects at different states!

        // From 80% to 100% arousal, the player will start to blush, escalate each 5% of arousal.
        { "gagspeak/vfx/arousal_blur_0_feeble", "arousal_blur_0_feeble.avfx" }, // 80% - 85%
        { "gagspeak/vfx/arousal_blur_1_weak",   "arousal_blur_1_weak.avfx" }, // 86% - 90%
        { "gagspeak/vfx/arousal_blur_2_light",  "arousal_blur_2_light.avfx" }, // 91% - 95%
        { "gagspeak/vfx/arousal_blur_3_mild",   "arousal_blur_3_mild.avfx" }, // 96% - 100%

        // Starting at 40% and going to 100%.
        { "gagspeak/vfx/arousal_blush_0_feeble","arousal_blush_0_feeble.avfx" }, // 40% - 52%
        { "gagspeak/vfx/arousal_blush_1_weak",  "arousal_blush_1_weak.avfx" }, // 52% - 64%
        { "gagspeak/vfx/arousal_blush_2_light", "arousal_blush_2_light.avfx" }, // 64% - 76%
        { "gagspeak/vfx/arousal_blush_3_mild",  "arousal_blush_3_mild.avfx" }, // 76% - 88%
        { "gagspeak/vfx/arousal_blush_4_full",  "arousal_blush_4_full.avfx" }, // 88% - 100%

        // do not overuse the pulse, it can become disorienting and distracting,
        // make it very mild, and very drawn out. Starts at 85%, goes to 100%.
        { "gagspeak/vfx/arousal_pulse_0_feeble","arousal_pulse_0_feeble.avfx" }, // 85-90%
        { "gagspeak/vfx/arousal_pulse_1_weak",  "arousal_pulse_1_weak.avfx" }, // 90-95%
        { "gagspeak/vfx/arousal_pulse_2_light", "arousal_pulse_2_light.avfx" }, // 95-100%
        { "gagspeak/vfx/arousal_pulse_3_max",   "arousal_pulse_3_max.avfx" }, // 100%
    };
}
