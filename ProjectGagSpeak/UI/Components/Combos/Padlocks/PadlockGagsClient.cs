using GagSpeak.PlayerData.Data;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.UI.Components.Combos;
public class PadlockGagsClient : PadlockBase<GagSlot>
{
    private readonly ClientData _gagData;
    private readonly AppearanceManager _appearance;
    public int GagSlotLayer { get; init; }

    public PadlockGagsClient(int layer, ClientData gagData, AppearanceManager appearance, ILogger log, 
        UiSharedService uiShared, string label) : base(log, uiShared, label)
    {
        _gagData = gagData;
        _appearance = appearance;
        GagSlotLayer = layer;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks() => GsPadlockEx.ClientLocks;
    protected override Padlocks GetLatestPadlock() => _gagData.AppearanceData?.GagSlots[GagSlotLayer].Padlock.ToPadlock() ?? Padlocks.None;
    protected override GagSlot GetLatestActiveItem() => _gagData.AppearanceData?.GagSlots[GagSlotLayer] ?? new GagSlot();
    protected override string ToActiveItemString(GagSlot item) => item.GagType;

    public void DrawPadlockComboSection(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // grab the latest padlock. If it is not none, we should draw the unlock base, otherwise, draw the lock base.
        if (GetLatestPadlock() is not Padlocks.None)
            DrawUnlockCombo(width, tt, btt, flags);
        else
            DrawLockCombo(width, tt, btt, flags);
    }

    protected override bool DisableCondition() => 
        _gagData.AppearanceData is null || _gagData.AppearanceData.GagSlots[GagSlotLayer].GagType.ToGagType() is GagType.None;

    protected override void OnLockButtonPress()
    {
        // fire off the appearance gagLocked for publication.
        if (!_appearance.GagLocked((GagLayer)GagSlotLayer, SelectedLock, _password, _timer, MainHub.UID, true, false))
            ResetInputs();

        ResetSelection();
    }

    protected override void OnUnlockButtonPress()
    {
        if (!_appearance.GagUnlocked((GagLayer)GagSlotLayer, _password, MainHub.UID, true, false))
            ResetInputs();

        ResetSelection();
    }
}
