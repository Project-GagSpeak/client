using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Padlockable;

// These are displayed seperately so dont use a layer updater.
public class PadlockGagsClient : CkPadlockComboBase<ActiveGagSlot>
{
    private readonly GagspeakMediator _mediator;
    private readonly GagRestrictionManager _gagData;
    private static int GagSlotLayer;

    public PadlockGagsClient(int layer, GagspeakMediator mediator, GagRestrictionManager gagData, ILogger log)
        : base(() => gagData.ActiveGagsData?.GagSlots[GagSlotLayer] ?? new ActiveGagSlot(), log, "##ClientPadlockGag"+layer)
    {
        _mediator = mediator;
        _gagData = gagData;
        GagSlotLayer = layer;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => GsPadlockEx.ClientLocks;

    protected override string ItemName(ActiveGagSlot item)
        => item.GagItem.GagName();

    public void DrawPadlockComboSection(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // grab the latest padlock. If it is not none, we should draw the unlock base, otherwise, draw the lock base.
        if (MonitoredItem.IsLocked())
            DrawUnlockCombo(width, tt, btt, flags);
        else
            DrawLockCombo(width, tt, btt, flags);
    }

    protected override bool DisableCondition() =>
        _gagData.ActiveGagsData is null || _gagData.ActiveGagsData.GagSlots[GagSlotLayer].GagItem is GagType.None;

    protected override void OnLockButtonPress()
    {
        // make a general common sense assumption logic check here, the rest can be handled across the server.
        if (MonitoredItem.CanLock())
        {
            var newData = new ActiveGagSlot()
            {
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID // use the same assigner. (To remove devotional timers)
            };
            _mediator.Publish(new GagDataChangedMessage(DataUpdateType.Locked, (GagLayer)GagSlotLayer, newData));
            ResetSelection();
        }
        else
        {
            ResetInputs();
        }
    }

    protected override void OnUnlockButtonPress()
    {
        // make a general common sense assumption logic check here, the rest can be handled across the server.
        if (MonitoredItem.CanUnlock())
        {
            var newData = new ActiveGagSlot()
            {
                Padlock = MonitoredItem.Padlock,
                Password = MonitoredItem.Password,
                PadlockAssigner = MainHub.UID
            };
            _mediator.Publish(new GagDataChangedMessage(DataUpdateType.Unlocked, (GagLayer)GagSlotLayer, newData));
            ResetSelection();
        }
        else
        {
            ResetInputs();
        }
    }
}
