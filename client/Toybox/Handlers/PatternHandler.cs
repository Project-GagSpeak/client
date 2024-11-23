using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;

namespace GagSpeak.PlayerData.Handlers;

/// <summary>
/// A Handler that manages how we modify our pattern information for edits.
/// For Toggling states and updates via non-direct edits, see ToyboxManager.
/// </summary>
public class PatternHandler : MediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PlayerCharacterData _playerManager;
    private readonly ToyboxManager _toyboxStateManager;

    public PatternHandler(ILogger<PatternHandler> logger,
        GagspeakMediator mediator, ClientConfigurationManager clientConfigs,
        PlayerCharacterData playerManager, ToyboxManager toyboxStateManager)
        : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _playerManager = playerManager;
        _toyboxStateManager = toyboxStateManager;
    }

    public List<PatternData> Patterns => _clientConfigs.PatternConfig.PatternStorage.Patterns;
    public int PatternCount => _clientConfigs.PatternConfig.PatternStorage.Patterns.Count;

    public PatternData? ClonedPatternForEdit { get; private set; } = null;

    public void StartEditingPattern(PatternData pattern)
    {
        Logger.LogDebug("Editing Pattern.", LoggerType.ToyboxPatterns);
        ClonedPatternForEdit = pattern.DeepCloneData();
        Guid originalID = pattern.UniqueIdentifier; // Prevent storing the pattern ID by reference.
        ClonedPatternForEdit.UniqueIdentifier = originalID; // Ensure the ID remains the same here.
    }

    public void CancelEditingPattern()
    {
        ClonedPatternForEdit = null;
        Logger.LogDebug("Pattern Editing Cancelled.", LoggerType.ToyboxPatterns);
    }

    public void SaveEditedPattern()
    {
        if (ClonedPatternForEdit is null)
            return;

        Logger.LogDebug("Saving Edited Pattern.", LoggerType.ToyboxPatterns);
        // locate the restraint set that contains the matching guid.
        var setIdx = Patterns.FindIndex(x => x.UniqueIdentifier == ClonedPatternForEdit.UniqueIdentifier);
        if (setIdx == -1)
            return;

        // update that set with the new cloned set.
        _clientConfigs.UpdatePattern(ClonedPatternForEdit, setIdx);

        // make the cloned set null again.
        ClonedPatternForEdit = null;
    }

    public void AddNewPattern(PatternData newPattern) => _clientConfigs.AddNewPattern(newPattern);
    public void RemovePattern(Guid idToRemove)
    {
        _clientConfigs.RemovePattern(idToRemove);
        CancelEditingPattern();
    }

    public void EnablePattern(PatternData pattern)
        => _toyboxStateManager.EnablePattern(pattern.UniqueIdentifier, MainHub.UID);

    public void DisablePattern(PatternData pattern)
        => _toyboxStateManager.DisablePattern(pattern.UniqueIdentifier);
}

