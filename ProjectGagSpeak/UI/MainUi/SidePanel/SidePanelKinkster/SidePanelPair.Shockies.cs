using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

// Helper methods for drawing out the hardcore actions.
public partial class SidePanelPair
{
    private void UniqueShareCode(Kinkster k, string dispName, float width)
    {
        using var _ = ImRaii.Group();

        var length = width - CkGui.IconTextButtonSize(FAI.Sync, "Refresh") + ImGui.GetFrameHeight();
        var refCode = k.OwnPerms.PiShockShareCode;
        // the bad way.
        CkGui.IconInputText(FAI.ShareAlt, string.Empty, "Unique Share Code", ref refCode, 40, width, true, false);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            // No action if the code is the same.
            if (refCode == k.OwnPerms.PiShockShareCode)
                return;

            UiService.SetUITask(async () =>
            {
                if (await PermHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, nameof(PairPerms.PiShockShareCode), refCode))
                    await SyncPermissionsWithCode(refCode, k);
            });
        }
        CkGui.AttachToolTip($"Unique Share Code for --COL--{dispName}--COL--." +
            $"--NL--Permissions here are prioritized over --COL--Global Share Code--COL--, unique to {dispName}.");

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Sync, "Refresh", disabled: string.IsNullOrEmpty(refCode) || UiService.DisableUI))
            UiService.SetUITask(async () => await SyncPermissionsWithCode(k.OwnPerms.PiShockShareCode, k));
    }

    public void DrawShockActions(KinksterInfoCache cache, Kinkster k, string dispName, float width)
    {
        ImGui.TextUnformatted("Shock Collar Actions");
        var preferPairCode = k.PairPerms.HasValidShareCode();
        var maxDuration = preferPairCode ? k.PairPerms.GetTimespanFromDuration() : k.PairGlobals.GetTimespanFromDuration();

        // Shock Expander
        var AllowShocks = preferPairCode ? k.PairPerms.AllowShocks : k.PairGlobals.AllowShocks;
        if (CkGui.IconTextButton(FAI.BoltLightning, $"Shock {dispName}'s Shock Collar", width, true, !AllowShocks))
            cache.ToggleInteraction(InteractionType.ShockAction);
        CkGui.AttachToolTip($"Perform a Shock action to {dispName}'s Shock Collar.");

        if (cache.OpenItem is InteractionType.ShockAction)
        {
            using (ImRaii.Child("SCA_Child", new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y)))
                ShockAct(cache, k, dispName, width, preferPairCode, maxDuration);
            ImGui.Separator();
        }

        // Vibrate Expander
        var AllowVibrations = preferPairCode ? k.PairPerms.AllowVibrations : k.PairGlobals.AllowVibrations;
        if (CkGui.IconTextButton(FAI.WaveSquare, $"Vibrate {dispName}'s Shock Collar", width, true, !AllowVibrations))
            cache.ToggleInteraction(InteractionType.VibrateAction);
        CkGui.AttachToolTip($"Perform a Vibrate action to {dispName}'s Shock Collar.");

        if (cache.OpenItem is InteractionType.VibrateAction)
        {
            using (ImRaii.Child("VCA_Child", new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y)))
                VibeAct(cache, k, dispName, width, preferPairCode, maxDuration);
            ImGui.Separator();
        }


        // Beep Expander
        var AllowBeeps = preferPairCode ? k.PairPerms.AllowBeeps : k.PairGlobals.AllowBeeps;
        if (CkGui.IconTextButton(FAI.LandMineOn, $"Beep {dispName}'s Shock Collar", width, true, !AllowBeeps))
            cache.ToggleInteraction(InteractionType.BeepAction);
        CkGui.AttachToolTip($"Beep {dispName}'s Shock Collar");

        if (cache.OpenItem is InteractionType.BeepAction)
        {
            using (ImRaii.Child("BCA_Child", new Vector2(width, ImGui.GetFrameHeight())))
                BeepAct(cache, k, dispName, width, preferPairCode, maxDuration);
            ImGui.Separator();
        }
    }

    private async Task SyncPermissionsWithCode(string code, Kinkster k)
    {
        var newShockPerms = await _shockies.GetPermissionsFromCode(code);
        var newPerms = k.OwnPerms with
        {
            PiShockShareCode = code,
            AllowShocks = newShockPerms.AllowShocks,
            AllowVibrations = newShockPerms.AllowVibrations,
            AllowBeeps = newShockPerms.AllowBeeps,
            MaxDuration = newShockPerms.MaxDuration,
            MaxIntensity = newShockPerms.MaxIntensity
        };
        // push update
        await _hub.UserBulkChangeUnique(new(k.UserData, newPerms, k.OwnPermAccess, UpdateDir.Own, MainHub.OwnUserData));
    }

    private void ShockAct(KinksterInfoCache cache, Kinkster k, string dispName, float width, bool usePairCode, TimeSpan maxDuration)
    {
        var maxIntensity = usePairCode ? k.PairPerms.MaxIntensity : k.PairGlobals.MaxIntensity;
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt($"##SCI-{k.UserData.UID}", ref cache.ApplyIntensity, 0, maxIntensity, " % d%%", ImGuiSliderFlags.None);

        // ensure we cant fall below 100ms to rely on millisecond conversion.
        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.BoltLightning, "Shock") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat($"##SCD-{k.UserData.UID}", ref cache.ApplyDuration, 0.1f, (float)maxDuration.TotalMilliseconds / 1000f, "%.1fs", ImGuiSliderFlags.None);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.BoltLightning, "Send Shock", disabled: cache.ApplyDuration <= 100))
        {
            var finalVal = TimeSpan.FromMilliseconds(cache.ApplyDuration);
            _logger.LogDebug($"Sending Shock with duration: {finalVal}ms");
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserShockKinkster(new(k.UserData, 0, cache.ApplyIntensity, finalVal.Milliseconds));
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

    private void VibeAct(KinksterInfoCache cache, Kinkster k, string dispName, float width, bool usePairCode, TimeSpan maxDuration)
    {
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt($"##ISR-{k.UserData.UID}", ref cache.ApplyVibeIntensity, 0, 100, "%d%%", ImGuiSliderFlags.None);

        // ensure we cant fall below 100ms to rely on millisecond conversion.
        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.HeartCircleBolt, "Vibrate") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat($"##DSR-{k.UserData.UID}", ref cache.ApplyVibeDur, 0.0f, (float)maxDuration.TotalMilliseconds / 1000f, "%.1fs", ImGuiSliderFlags.None);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.HeartCircleBolt, "Send Vibration", disabled: cache.ApplyDuration <= 100))
        {
            var finalVal = TimeSpan.FromMilliseconds(cache.ApplyVibeDur);
            _logger.LogDebug($"Sending Vibration with duration: {finalVal}ms");
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserShockKinkster(new(k.UserData, 1, cache.ApplyVibeIntensity, finalVal.Milliseconds));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    _logger.LogDebug($"Failed to send Vibration to {dispName}'s Shock Collar. ({res})", LoggerType.StickyUI);
                else
                    _logger.LogDebug($"Sent Vibration to {dispName}'s Shock Collar for: {finalVal}ms", LoggerType.StickyUI);
            });
        }
    }

    private void BeepAct(KinksterInfoCache cache, Kinkster k, string dispName, float width, bool usePairCode, TimeSpan maxDuration)
    {
        var max = (float)maxDuration.TotalMilliseconds / 1000f;
        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.LandMineOn, "Beep") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat("##DurationSliderRef" + k.UserData.UID, ref cache.ApplyVibeDur, 0.1f, max, "%.1fs", ImGuiSliderFlags.None);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.LandMineOn, "Send Beep", disabled: cache.ApplyVibeDur <= 100))
        {
            _logger.LogDebug($"Sending Beep foir {cache.ApplyVibeDur}ms!");
            UiService.SetUITask(async () =>
            {
                var finalVal = TimeSpan.FromMilliseconds(cache.ApplyVibeDur);
                _logger.LogDebug($"Sending Beep for: {finalVal}ms");
                var res = await _hub.UserShockKinkster(new ShockCollarAction(k.UserData, 2, cache.ApplyIntensity, finalVal.Milliseconds));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    _logger.LogDebug($"Failed to send Beep to {dispName}'s Shock Collar. ({res})", LoggerType.StickyUI);
                else
                    _logger.LogDebug($"Sent Beep to {dispName}'s Shock Collar for: {finalVal}ms", LoggerType.StickyUI);
            });
        }
    }
}
