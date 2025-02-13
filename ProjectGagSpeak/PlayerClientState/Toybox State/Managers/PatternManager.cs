using GagSpeak.GagspeakConfiguration;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerState.Models;
using GagSpeak.Restrictions;
using GagSpeak.UI.Components;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;

namespace GagSpeak.PlayerState.Toybox;
public sealed class PatternManager(
    ILogger<PatternManager> logger,
    GagspeakConfigService mainConfig,
    PatternConfigService patterns,
    PatternApplier applier) : PatternEditor(logger, mainConfig, patterns)
{
    private readonly PatternApplier _applier = applier;
    public PatternStorage Storage => _patterns.Current.Storage;
    private LightPattern? ActivePattern => _applier.ActivePatternInfo;
    public void OnLogin() { }

    public void OnLogout() { }

    public Pattern CreateNew(string patternName)
    {
        return new Pattern() { Label = patternName };
    }

    public Pattern CreateClone(Pattern other, string newName)
    {
        return new Pattern() { Label = newName };
    }

    public void Delete(Pattern pattern)
    {

    }

    public void Rename(Pattern pattern, string newName)
    {
        var oldName = pattern.Label;
        if(oldName == newName || string.IsNullOrWhiteSpace(newName))
            return;

        pattern.Label = newName;
    }

    public void StartEditing(Pattern pattern)
    {

    }

    public void StopEditing()
    {

    }

    public void AddFavorite(RestraintSet pattern)
    {

    }

    public void RemoveFavorite(RestraintSet restriction)
    {

    }

    public bool CanEnable(Guid patternId)
    {
        if (ActivePattern is not null)
            return false;
        // currently cannot think of any case where this would not be allowed, so mark as true.
        return true;
    }

    public bool CanDisable(Guid patternId)
    {
        if(ActivePattern is null)
            return false;
        // a pattern is running that we can disable.
        return true;
    }

    /// <summary> Switches from a currently active pattern to a new one. </summary>
    /// <remarks> If no pattern is currently active, it will simply start one. </remarks>
    public void SwitchPattern(Guid patternId, string enactor)
    {
        // This only actually fires if a pattern is active, and is skipped otherwise.
        if(ActivePattern is not null)
            DisablePattern(ActivePattern.Id, enactor);
        // now enable it.
        EnablePattern(patternId, enactor);
    }

    /// <summary> Enables a pattern, beginning the execution to the simulated, or connected sex toy. </summary>
    /// <remarks> If no pattern in the storage is found, no pattern will activate. </remarks>
    public void EnablePattern(Guid patternId, string enactor)
    {
        if(Storage.TryGetPattern(patternId, out var pattern))
            _applier.StartPlayback(pattern);
    }

    public void DisablePattern(Guid patternId, string enactor)
    {
        if(ActivePattern is not null && ActivePattern.Id == patternId)
            _applier.StopPlayback();
    }
}

public class PatternEditor(ILogger logger, GagspeakConfigService mainConfig, 
    PatternConfigService patterns)
{
    protected readonly ILogger _logger = logger;
    protected readonly GagspeakConfigService _mainConfig = mainConfig;
    protected readonly PatternConfigService _patterns = patterns;

    // methods for the editor and stuff.
}
