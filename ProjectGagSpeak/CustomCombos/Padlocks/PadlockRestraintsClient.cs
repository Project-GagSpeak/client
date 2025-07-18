using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Padlock;

public class PadlockRestraintsClient : CkPadlockComboBase<CharaActiveRestraint>
{
    private readonly GagspeakMediator _mediator;
    private readonly RestraintManager _manager;
    public PadlockRestraintsClient(ILogger log, GagspeakMediator mediator, RestraintManager manager)
        : base(() => [ manager.ServerData ?? new CharaActiveRestraint() ], PadlockEx.ClientLocks, log)
    {
        _mediator = mediator;
        _manager = manager;
    }

    protected override string ItemName(CharaActiveRestraint item)
        => _manager.Storage.TryGetRestraint(item.Identifier, out var restraint) ? restraint.Label : "None";

    protected override bool DisableCondition(int _)
        => Items[0].Identifier == Guid.Empty;

    public void DrawLockCombo(float width, string tooltip)
        => DrawLockCombo("##ClientRestraintLock", width, 0, string.Empty, tooltip, true);

    public void DrawUnlockCombo(float width, string tooltip)
        => DrawUnlockCombo("##ClientRestraintUnlock", width, 0, string.Empty, tooltip);

    protected override Task<bool> OnLockButtonPress(int _)
    {
        if (Items[0].CanLock())
        {
            var newData = new CharaActiveRestraint()
            {
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID
            };
            _mediator.Publish(new RestraintDataChangedMessage(DataUpdateType.Locked, newData));
            ResetSelection();
            ResetInputs();
            return Task.FromResult(true);
        }

        ResetInputs();
        return Task.FromResult(false);
    }
    protected override Task<bool> OnUnlockButtonPress(int _)
    {
        if (Items[0].CanUnlock())
        {
            var newData = new CharaActiveRestraint()
            {
                Padlock = Items[0].Padlock,
                Password = Items[0].Password,
                PadlockAssigner = MainHub.UID
            };

            _mediator.Publish(new RestraintDataChangedMessage(DataUpdateType.Unlocked, newData));
            ResetSelection();
            ResetInputs();
            return Task.FromResult(true);
        }
        ResetInputs();
        return Task.FromResult(false);
    }
}
