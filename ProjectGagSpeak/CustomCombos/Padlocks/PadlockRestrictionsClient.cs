using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlock;

public class PadlockRestrictionsClient : CkPadlockComboBase<ActiveRestriction>
{
    private readonly GagspeakMediator _mediator;
    private readonly RestrictionManager _manager;
    public PadlockRestrictionsClient(ILogger log, GagspeakMediator mediator, RestrictionManager manager)
        : base(() => manager.ServerRestrictionData?.Restrictions ?? [], log)
    {
        _mediator = mediator;
        _manager = manager;
    }

    public int ItemCount => Items.Count;

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.ClientLocks;

    protected override string ItemName(ActiveRestriction item)
        => _manager.Storage.TryGetRestriction(item.Identifier, out var restriction) ? restriction.Label : "None";


    protected override bool DisableCondition(int layerIdx)
        => Items[layerIdx].CanLock() is false || Items[layerIdx].Padlock == SelectedLock;

    public void DrawLockCombo(float width, int layerIdx, string tooltip)
        => DrawLockCombo($"##ClientUnlock-{layerIdx}", width, layerIdx, string.Empty, tooltip, true);

    public void DrawUnlockCombo(float width, int layerIdx, string tooltip)
        => DrawUnlockCombo($"##ClientUnlock-{layerIdx}", width, layerIdx, string.Empty, tooltip);

    protected override Task<bool> OnLockButtonPress(int layerIdx)
    {
        if (Items[layerIdx].CanLock())
        {
            var newData = new ActiveRestriction()
            {
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID
            };
            _mediator.Publish(new RestrictionDataChangedMessage(DataUpdateType.Locked, layerIdx, newData));
            ResetSelection();
        }
        else
        {
            ResetInputs();
        }
        return Task.FromResult(true);
    }

    protected override Task<bool> OnUnlockButtonPress(int layerIdx)
    {
        if (Items[layerIdx].CanUnlock())
        {
            var newData = new ActiveRestriction()
            {
                Padlock = Items[layerIdx].Padlock,
                Password = Items[layerIdx].Password,
                PadlockAssigner = MainHub.UID
            };

            _mediator.Publish(new RestrictionDataChangedMessage(DataUpdateType.Unlocked, layerIdx, newData));
            ResetSelection();
        }
        else
        {
            ResetInputs();
        }
        return Task.FromResult(true);
    }
}
