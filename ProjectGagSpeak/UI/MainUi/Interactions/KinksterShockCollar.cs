using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

// Helper methods for drawing out the hardcore actions.
public class KinksterShockCollar
{
    private readonly ILogger<KinksterShockCollar> _logger;
    private readonly MainHub _hub;
    private readonly PiShockProvider _shockies;
    private readonly InteractionsService _service;

    public KinksterShockCollar(ILogger<KinksterShockCollar> logger, MainHub hub, 
        PiShockProvider shockies, InteractionsService service)
    {
        _logger = logger;
        _hub = hub;
        _shockies = shockies;
        _service = service;
    }

    public void KinksterPermsForClient(float width, Kinkster k, string dispName)
    {

    }

    public void DrawClientPermsForKinkster(float width, Kinkster k, string dispName)
    {
        UniqueShareCode(width, k, dispName);
        MaxVibrateDuration(width, k, dispName);
    }

    public void DrawShockActions(float width, Kinkster k, string dispName)
    {
        ImGui.TextUnformatted("Shock Collar Actions");
        var preferPairCode = k.PairPerms.HasValidShareCode();
        var maxDuration = preferPairCode ? k.PairPerms.GetTimespanFromDuration() : k.PairGlobals.GetTimespanFromDuration();

        // Shock Expander
        var AllowShocks = preferPairCode ? k.PairPerms.AllowShocks : k.PairGlobals.AllowShocks;
        if (CkGui.IconTextButton(FAI.BoltLightning, $"Shock {dispName}'s Shock Collar", width, true, !AllowShocks))
            _service.ToggleInteraction(InteractionType.ShockAction);
        CkGui.AttachToolTip($"Perform a Shock action to {dispName}'s Shock Collar.");

        if (_service.OpenItem is InteractionType.ShockAction)
        {
            using (ImRaii.Child("SCA_Child", new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y)))
                ShockAct(width, k, dispName, preferPairCode, maxDuration);
            ImGui.Separator();
        }

        // Vibrate Expander
        var AllowVibrations = preferPairCode ? k.PairPerms.AllowVibrations : k.PairGlobals.AllowVibrations;
        if (CkGui.IconTextButton(FAI.WaveSquare, $"Vibrate {dispName}'s Shock Collar", width, true, !AllowVibrations))
            _service.ToggleInteraction(InteractionType.VibrateAction);
        CkGui.AttachToolTip($"Perform a Vibrate action to {dispName}'s Shock Collar.");

        if (_service.OpenItem is InteractionType.VibrateAction)
        {
            using (ImRaii.Child("VCA_Child", new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y)))
                VibeAct(width, k, dispName, preferPairCode, maxDuration);
            ImGui.Separator();
        }


        // Beep Expander
        var AllowBeeps = preferPairCode ? k.PairPerms.AllowBeeps : k.PairGlobals.AllowBeeps;
        if (CkGui.IconTextButton(FAI.LandMineOn, $"Beep {dispName}'s Shock Collar", width, true, !AllowBeeps))
            _service.ToggleInteraction(InteractionType.BeepAction);
        CkGui.AttachToolTip($"Beep {dispName}'s Shock Collar");

        if (_service.OpenItem is InteractionType.BeepAction)
        {
            using (ImRaii.Child("BCA_Child", new Vector2(width, ImGui.GetFrameHeight())))
                BeepAct(width, k, dispName, preferPairCode, maxDuration);
            ImGui.Separator();
        }
    }

    private void UniqueShareCode(float width, Kinkster k, string dispName)
    {
        using var _ = ImRaii.Group();

        var length = width - CkGui.IconTextButtonSize(FAI.Sync, "Refresh") + ImGui.GetFrameHeight();
        var refCode = k.OwnPerms.PiShockShareCode;
        // the bad way.
        CkGui.IconInputText(FAI.ShareAlt, string.Empty, "Unique Share Code", ref refCode, 40, width, true, false);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            UiService.SetUITask(async () =>
            {
                if (await PermissionHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, nameof(PairPerms.PiShockShareCode), refCode))
                    await SyncPermissionsWithCode(refCode, k);
            });
        }
        CkGui.AttachToolTip($"Unique Share Code for --COL--{dispName}--COL--." +
            $"--NL--Permissions here are prioritized over --COL--Global Share Code--COL--, unique to {dispName}.");

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Sync, "Refresh", disabled: string.IsNullOrEmpty(refCode) || UiService.DisableUI))
            UiService.SetUITask(async () => await SyncPermissionsWithCode(k.OwnPerms.PiShockShareCode, k));
    }

    private void MaxVibrateDuration(float width, Kinkster k, string dispName)
    {
        using var _ = ImRaii.Group();

        // grab seconds from the service cache or our permissions.
        var seconds = _service.TmpVibeDur == -1
            ? (float)k.OwnPerms.MaxVibrateDuration.TotalMilliseconds / 1000
            : _service.TmpVibeDur;
        var disableDur = k.OwnPerms.HasValidShareCode();
        if (CkGui.IconSliderFloat($"##mvt-{k.UserData.UID}", FAI.Stopwatch, "Max Vibe Time", ref seconds, 0.1f, 15f, width * .65f, true, disableDur))
            _service.TmpVibeDur = seconds;
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            // if the max duration is under 15, parse to seconds.
            var newVal = k.OwnPerms.MaxDuration < 15 ? TimeSpan.FromSeconds(_service.TmpVibeDur) : TimeSpan.FromMilliseconds(_service.TmpVibeDur);

            // make sure its different from the stored duration.
            if (newVal.Milliseconds == k.OwnPerms.MaxVibrateDuration.Milliseconds)
            {
                _service.TmpVibeDur = -1;
                return;
            }
            // It was valid, so set it.
            var newTicks = (ulong)newVal.Ticks;
            UiService.SetUITask(async () =>
            {
                if (await PermissionHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, nameof(PairPerms.MaxVibrateDuration), newTicks))
                    _service.TmpVibeDur = -1;
            });
        }
        CkGui.AttachToolTip("Max duration you allow this pair to vibrate your Shock Collar for");
    }

    private async Task SyncPermissionsWithCode(string code, Kinkster k)
    {
        var newShockPerms = await _shockies.GetPermissionsFromCode(code);
        var newPerms = k.PairPerms with
        {
            PiShockShareCode = code,
            AllowShocks = newShockPerms.AllowShocks,
            AllowVibrations = newShockPerms.AllowVibrations,
            AllowBeeps = newShockPerms.AllowBeeps,
            MaxDuration = newShockPerms.MaxDuration,
            MaxIntensity = newShockPerms.MaxIntensity
        };
        // push update
        await _hub.UserBulkChangeUnique(new(k.UserData, newPerms, k.OwnPermAccess, UpdateDir.Own, MainHub.PlayerUserData));
    }

    private void ShockAct(float width, Kinkster k, string dispName, bool usePairCode, TimeSpan maxDuration)
    {
        var maxIntensity = usePairCode ? k.PairPerms.MaxIntensity : k.PairGlobals.MaxIntensity;
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt($"##SCI-{k.UserData.UID}", ref _service.ApplyIntensity, 0, maxIntensity, " % d%%", ImGuiSliderFlags.None);

        // ensure we cant fall below 100ms to rely on millisecond conversion.
        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.BoltLightning, "Shock") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat($"##SCD-{k.UserData.UID}", ref _service.ApplyDuration, 0.1f, (float)maxDuration.TotalMilliseconds / 1000f, "%.1fs", ImGuiSliderFlags.None);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.BoltLightning, "Send Shock", disabled: _service.ApplyDuration <= 100))
        {
            var finalVal = TimeSpan.FromMilliseconds(_service.ApplyDuration);
            _logger.LogDebug($"Sending Shock with duration: {finalVal}ms");
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserShockKinkster(new(k.UserData, 0, _service.ApplyIntensity, finalVal.Milliseconds));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                {
                    _logger.LogDebug($"Failed to send Shock to {dispName}'s Shock Collar. ({res})", LoggerType.StickyUI);
                    return;
                }
                _logger.LogDebug($"Sent Shock to {dispName}'s Shock Collar for: {finalVal}ms", LoggerType.StickyUI);
                GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockSent);
            });
        }
    }

    private void VibeAct(float width, Kinkster k, string dispName, bool usePairCode, TimeSpan maxDuration)
    {
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt($"##ISR-{k.UserData.UID}", ref _service.ApplyVibeIntensity, 0, 100, "%d%%", ImGuiSliderFlags.None);

        // ensure we cant fall below 100ms to rely on millisecond conversion.
        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.HeartCircleBolt, "Vibrate") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat($"##DSR-{k.UserData.UID}", ref _service.ApplyVibeDur, 0.0f, (float)maxDuration.TotalMilliseconds / 1000f, "%.1fs", ImGuiSliderFlags.None);
        
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.HeartCircleBolt, "Send Vibration", disabled: _service.ApplyDuration <= 100))
        {
            var finalVal = TimeSpan.FromMilliseconds(_service.ApplyVibeDur);
            _logger.LogDebug($"Sending Vibration with duration: {finalVal}ms");
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserShockKinkster(new(k.UserData, 1, _service.ApplyVibeIntensity, finalVal.Milliseconds));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    _logger.LogDebug($"Failed to send Vibration to {dispName}'s Shock Collar. ({res})", LoggerType.StickyUI);
                else
                    _logger.LogDebug($"Sent Vibration to {dispName}'s Shock Collar for: {finalVal}ms", LoggerType.StickyUI);
            });
        }
    }

    private void BeepAct(float width, Kinkster k, string dispName, bool usePairCode, TimeSpan maxDuration)
    {
        var max = (float)maxDuration.TotalMilliseconds / 1000f;
        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.LandMineOn, "Beep") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat("##DurationSliderRef" + k.UserData.UID, ref _service.ApplyVibeDur, 0.1f, max, "%.1fs", ImGuiSliderFlags.None);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.LandMineOn, "Send Beep", disabled: _service.ApplyVibeDur <= 100))
        {
            _logger.LogDebug($"Sending Beep foir {_service.ApplyVibeDur}ms!");
            UiService.SetUITask(async () =>
            {
                var finalVal = TimeSpan.FromMilliseconds(_service.ApplyVibeDur);
                _logger.LogDebug($"Sending Beep for: {finalVal}ms");
                var res = await _hub.UserShockKinkster(new ShockCollarAction(k.UserData, 2, _service.ApplyIntensity, finalVal.Milliseconds));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    _logger.LogDebug($"Failed to send Beep to {dispName}'s Shock Collar. ({res})", LoggerType.StickyUI);
                else
                    _logger.LogDebug($"Sent Beep to {dispName}'s Shock Collar for: {finalVal}ms", LoggerType.StickyUI);
            });
        }
    }
}
