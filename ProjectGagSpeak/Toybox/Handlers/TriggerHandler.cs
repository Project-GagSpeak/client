using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.WebAPI;

namespace GagSpeak.PlayerData.Handlers;

public class TriggerHandler
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ToyboxManager _toyboxStateManager;
    public TriggerHandler(ClientConfigurationManager clientConfigs, ToyboxManager toyboxManager)
    {
        _clientConfigs = clientConfigs;
        _toyboxStateManager = toyboxManager;
    }

    public List<Trigger> Triggers => _clientConfigs.TriggerConfig.TriggerStorage.Triggers;
    public int TriggerCount => _clientConfigs.TriggerConfig.TriggerStorage.Triggers.Count;

    public Trigger? ClonedTriggerForEdit { get; private set; } = null;

    public void StartEditingTrigger(Trigger trigger)
    {
        ClonedTriggerForEdit = trigger.DeepClone();
        Guid originalID = trigger.TriggerIdentifier; // Prevent storing the trigger ID by reference.
        ClonedTriggerForEdit.TriggerIdentifier = originalID; // Ensure the ID remains the same here.
    }

    public void CancelEditingTrigger() => ClonedTriggerForEdit = null;

    public void SaveEditedTrigger()
    {
        if (ClonedTriggerForEdit is null)
            return;
        // locate the restraint set that contains the matching guid.
        var triggerIdx = Triggers.FindIndex(x => x.TriggerIdentifier == ClonedTriggerForEdit.TriggerIdentifier);
        if (triggerIdx == -1)
            return;
        // update that set with the new cloned set.
        _clientConfigs.UpdateTrigger(ClonedTriggerForEdit, triggerIdx);
        // make the cloned set null again.
        ClonedTriggerForEdit = null;
    }

    public void AddNewTrigger(Trigger newPattern) => _clientConfigs.AddNewTrigger(newPattern);
    public void RemoveTrigger(Trigger triggerToRemove)
    {
        _clientConfigs.RemoveTrigger(triggerToRemove);
        CancelEditingTrigger();
    }

    public void EnableTrigger(Trigger trigger)
    => _toyboxStateManager.EnableTrigger(trigger.TriggerIdentifier, MainHub.UID);

    public void DisableTrigger(Trigger trigger)
        => _toyboxStateManager.DisableTrigger(trigger.TriggerIdentifier);
}
