using GagSpeak.GagspeakConfiguration;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerState.Models;

namespace GagSpeak.PlayerState.Toybox;

public sealed class TriggerManager(ILogger<TriggerManager> logger, GagspeakConfigService mainConfig, 
    TriggerConfigService triggers) : TriggerEditor(logger, mainConfig, triggers)
{
    public TriggerStorage Storage => _triggers.Current.Storage;

    public void OnLogin() { }

    public void OnLogout() { }

    public Trigger CreateNew(string triggerName)
    {
        return new GagTrigger() { Label = triggerName };
    }

    public Trigger CreateClone(Trigger other, string newName)
    {
        return other switch
        {
            SpellActionTrigger   t => new SpellActionTrigger() { Label = newName },
            HealthPercentTrigger t => new HealthPercentTrigger() { Label = newName },
            RestraintTrigger     t => new RestraintTrigger() { Label = newName },
            GagTrigger           t => new GagTrigger() { Label = newName },
            SocialTrigger        t => new SocialTrigger() { Label = newName },
            EmoteTrigger         t => new EmoteTrigger() { Label = newName },
            _ => throw new InvalidOperationException("Unknown trigger type.")
        };
    }

    public void Delete(Trigger trigger)
    {

    }

    public void StartEditing(Trigger trigger)
    {

    }

    public void StopEditing()
    {

    }

    public void AddFavorite(Trigger trigger)
    {

    }

    public void RemoveFavorite(Trigger trigger)
    {

    }

    public void ToggleTrigger(Guid triggerId, string enactor)
    {
        if (Storage.TryGetTrigger(triggerId, out var trigger))
        {
            trigger.Enabled = !trigger.Enabled;
            _triggers.Save();
        }
    }

    public void EnableTrigger(Guid triggerId, string enactor)
    {
        if (Storage.TryGetTrigger(triggerId, out var trigger))
        {
            trigger.Enabled = true;
            _triggers.Save();
        }
    }

    public void DisableTrigger(Guid triggerId, string enactor)
    {
        // if this is false it means one is active for us to disable.
        if (Storage.TryGetTrigger(triggerId, out var trigger))
        {
            if(!trigger.Enabled) 
                return;

            trigger.Enabled = false;
            _triggers.Save();
        }
    }
}

public class TriggerEditor(ILogger logger, GagspeakConfigService mainConfig, TriggerConfigService triggers)
{
    protected readonly ILogger _logger = logger;
    protected readonly GagspeakConfigService _mainConfig = mainConfig;
    protected readonly TriggerConfigService _triggers = triggers;
}
