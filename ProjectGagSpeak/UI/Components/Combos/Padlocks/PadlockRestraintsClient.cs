using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.StateManagers;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.UI.Components.Combos;
public class PadlockRestraintsClient : PadlockBase<RestraintSet>
{
    private readonly WardrobeHandler _handler;
    private readonly AppearanceManager _appearance;
    public PadlockRestraintsClient(WardrobeHandler handler, AppearanceManager appearance,
        ILogger log, UiSharedService ui, string label) : base(log, ui, label)
    {
        _handler = handler;
        _appearance = appearance;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks() => GsPadlockEx.ClientLocks;
    protected override Padlocks GetLatestPadlock() => GetLatestActiveItem().Padlock.ToPadlock();
    protected override RestraintSet GetLatestActiveItem() => _handler.GetActiveSet() ?? new RestraintSet() { RestraintId = Guid.Empty };
    protected override string ToActiveItemString(RestraintSet item) => item.Name;
    protected override bool DisableCondition() => GetLatestActiveItem().RestraintId == Guid.Empty;

    public void DrawPadlockComboSection(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // grab the latest padlock. If it is not none, we should draw the unlock base, otherwise, draw the lock base.
        if (GetLatestPadlock() is not Padlocks.None)
            DrawUnlockCombo(width, tt, btt, flags);
        else
            DrawLockCombo(width, tt, btt, flags);
    }

    protected override void OnLockButtonPress()
    {
        // fire off the appearance gagLocked for publication.
        if (!_appearance.LockRestraintSet(GetLatestActiveItem().RestraintId, SelectedLock, _password, _timer, MainHub.UID, true, false))
            ResetInputs();

        ResetSelection();
    }

    protected override void OnUnlockButtonPress()
    {
        // fire off the appearance gagUnlocked for publication.
        if (!_appearance.UnlockRestraintSet(GetLatestActiveItem().RestraintId, _password, MainHub.UID, true, false))
            ResetInputs();

        ResetSelection();
    }
}
