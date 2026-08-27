using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
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

        var refCode = k.OwnPerms.PiShockShareCode;
        var refreshWidth = CkGui.IconTextButtonSize(FAI.Sync, "Refresh");
        ImGui.SetNextItemWidth(width - refreshWidth - ImGui.GetStyle().ItemInnerSpacing.X);
        CkGui.IconInputText(FAI.ShareAlt, string.Empty, "Unique Share Code", ref refCode, 40, width - refreshWidth - ImGui.GetStyle().ItemInnerSpacing.X, true, false);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            if (refCode == k.OwnPerms.PiShockShareCode)
                return;

            UiService.SetUITask(async () =>
            {
                if (await PermHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, nameof(PairPerms.PiShockShareCode), refCode))
                    await SyncAllPairsAsync(k, refCode);
            });
        }
        CkGui.AttachTooltip($"Unique Share Code for --COL--{dispName}--COL--." +
            $"--NL--This code gives {dispName} permission to interact with your PiShock device.");

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Sync, "Refresh", disabled: string.IsNullOrEmpty(refCode) || UiService.DisableUI))
            UiService.SetUITask(async () => await SyncAllPairsAsync(k, k.OwnPerms.PiShockShareCode));
        CkGui.AttachTooltip("Refresh permissions for all pairs with a share code set.");

        if (_shockies.LastConnectState is not PiShockProvider.ConnectState.Success)
        {
            CkGui.ColorText("Not connected - click Save & Connect in Settings first.", ImGuiColors.DalamudRed);
            return;
        }

        if (!string.IsNullOrEmpty(refCode))
        {
            if (k.OwnPerms.MaxDuration <= 0)
            {
                CkGui.ColorText("Not synced - click Refresh", ImGuiColors.DalamudYellow);
            }
            else
            {
                var maxSecs = (float)k.OwnPerms.GetTimespanFromDuration().TotalSeconds;
                var shock = k.OwnPerms.AllowShocks;
                var vibe  = k.OwnPerms.AllowVibrations;
                var beep  = k.OwnPerms.AllowBeeps;

                CkGui.ColorText("Shock: ", ImGuiColors.DalamudGrey);
                ImGui.SameLine(0, 2);
                CkGui.ColorText(shock ? "Yes" : "No", shock ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
                ImGui.SameLine(0, 8);
                CkGui.ColorText("Vibrate: ", ImGuiColors.DalamudGrey);
                ImGui.SameLine(0, 2);
                CkGui.ColorText(vibe ? "Yes" : "No", vibe ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
                ImGui.SameLine(0, 8);
                CkGui.ColorText("Beep: ", ImGuiColors.DalamudGrey);
                ImGui.SameLine(0, 2);
                CkGui.ColorText(beep ? "Yes" : "No", beep ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);

                CkGui.ColorText($"Max Intensity: {k.OwnPerms.MaxIntensity}%  |  Max Duration: {maxSecs:0.#}s", ImGuiColors.DalamudGrey);
            }
        }

        var shockers = _shockies.CachedShockers;
        if (shockers.Count > 0)
        {
            ImGui.Spacing();
            var currentId = _shockies.GetPairShockerId(k.UserData.UID);
            var currentName = shockers.FirstOrDefault(s => s.Id == currentId).Name ?? "Select Device...";

            ImGui.SetNextItemWidth(width);
            using (var combo = ImRaii.Combo("##Dev_" + k.UserData.UID, currentName))
            {
                if (combo)
                {
                    foreach (var (id, name) in shockers)
                    {
                        if (ImGui.Selectable(name, id == currentId))
                            _shockies.SetPairShockerId(k.UserData.UID, id);
                    }
                }
            }
            CkGui.AttachTooltip($"Choose which PiShock device {dispName} controls.");
        }
    }

    public void DrawShockActions(KinksterInfoCache cache, Kinkster k, string dispName, float width)
    {
        ImGui.TextUnformatted("Shock Collar Actions");

        if (!k.PairPerms.HasValidShareCode())
        {
            CkGui.ColorText("No PiShock configured or online.", ImGuiColors.DalamudGrey);
            return;
        }

        var maxDuration = k.PairPerms.GetTimespanFromDuration();
        var maxSecs = (float)maxDuration.TotalSeconds;
        cache.ApplyDuration = Math.Clamp(cache.ApplyDuration, 0.1f, maxSecs);
        cache.ApplyVibeDur = Math.Clamp(cache.ApplyVibeDur, 0.0f, maxSecs);

        // Shock Expander
        var AllowShocks = k.PairPerms.AllowShocks;
        if (CkGui.IconTextButton(FAI.BoltLightning, $"Shock {dispName}'s Shock Collar", width, true, !AllowShocks))
            cache.ToggleInteraction(InteractionType.ShockAction);
        CkGui.AttachTooltip($"Perform a Shock action to {dispName}'s Shock Collar.");

        if (cache.OpenItem is InteractionType.ShockAction)
        {
            using (ImRaii.Child("SCA_Child", new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y)))
                ShockAct(cache, k, dispName, width, maxDuration);
            ImGui.Separator();
        }

        // Vibrate Expander
        var AllowVibrations = k.PairPerms.AllowVibrations;
        if (CkGui.IconTextButton(FAI.WaveSquare, $"Vibrate {dispName}'s Shock Collar", width, true, !AllowVibrations))
            cache.ToggleInteraction(InteractionType.VibrateAction);
        CkGui.AttachTooltip($"Perform a Vibrate action to {dispName}'s Shock Collar.");

        if (cache.OpenItem is InteractionType.VibrateAction)
        {
            using (ImRaii.Child("VCA_Child", new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y)))
                VibeAct(cache, k, dispName, width, maxDuration);
            ImGui.Separator();
        }

        // Beep Expander
        var AllowBeeps = k.PairPerms.AllowBeeps;
        if (CkGui.IconTextButton(FAI.LandMineOn, $"Beep {dispName}'s Shock Collar", width, true, !AllowBeeps))
            cache.ToggleInteraction(InteractionType.BeepAction);
        CkGui.AttachTooltip($"Beep {dispName}'s Shock Collar");

        if (cache.OpenItem is InteractionType.BeepAction)
        {
            using (ImRaii.Child("BCA_Child", new Vector2(width, ImGui.GetFrameHeight())))
                BeepAct(cache, k, dispName, width, maxDuration);
            ImGui.Separator();
        }
    }

    private async Task SyncAllPairsAsync(Kinkster currentK, string codeForCurrent)
    {
        await SyncPermissionsWithCode(codeForCurrent, currentK);
        foreach (var k in _kinksters.DirectPairs)
        {
            if (k.UserData.UID == currentK.UserData.UID) continue;
            var code = k.OwnPerms.PiShockShareCode;
            if (!string.IsNullOrEmpty(code))
                await SyncPermissionsWithCode(code, k);
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
        await _hub.UserBulkChangeUnique(new(k.UserData, newPerms, k.OwnPermAccess, UpdateDir.Own, MainHub.OwnUserData));
    }

    private void ShockAct(KinksterInfoCache cache, Kinkster k, string dispName, float width, TimeSpan maxDuration)
    {
        var maxIntensity = k.PairPerms.MaxIntensity;
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt($"##SCI-{k.UserData.UID}", ref cache.ApplyIntensity, 0, maxIntensity, " % d%%", ImGuiSliderFlags.None);

        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.BoltLightning, "Shock") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat($"##SCD-{k.UserData.UID}", ref cache.ApplyDuration, 0.1f, (float)maxDuration.TotalSeconds, "%.1fs", ImGuiSliderFlags.None);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.BoltLightning, "Send Shock", disabled: cache.ApplyDuration <= 0))
        {
            var durationMs = (int)(cache.ApplyDuration * 1000f);
            _logger.LogDebug($"Sending Shock with duration: {durationMs}ms");
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserShockKinkster(new(k.UserData, 0, cache.ApplyIntensity, durationMs));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                {
                    _logger.LogDebug($"Failed to send Shock to {dispName}'s Shock Collar. ({res})", LoggerType.StickyUI);
                    return;
                }
                _logger.LogDebug($"Sent Shock to {dispName}'s Shock Collar for: {durationMs}ms", LoggerType.StickyUI);
                GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockSent);
            });
        }
    }

    private void VibeAct(KinksterInfoCache cache, Kinkster k, string dispName, float width, TimeSpan maxDuration)
    {
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt($"##ISR-{k.UserData.UID}", ref cache.ApplyVibeIntensity, 0, 100, "%d%%", ImGuiSliderFlags.None);

        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.HeartCircleBolt, "Vibrate") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat($"##DSR-{k.UserData.UID}", ref cache.ApplyVibeDur, 0.0f, (float)maxDuration.TotalSeconds, "%.1fs", ImGuiSliderFlags.None);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.HeartCircleBolt, "Send Vibration", disabled: cache.ApplyVibeDur <= 0))
        {
            var durationMs = (int)(cache.ApplyVibeDur * 1000f);
            _logger.LogDebug($"Sending Vibration with duration: {durationMs}ms");
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserShockKinkster(new(k.UserData, 1, cache.ApplyVibeIntensity, durationMs));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    _logger.LogDebug($"Failed to send Vibration to {dispName}'s Shock Collar. ({res})", LoggerType.StickyUI);
                else
                    _logger.LogDebug($"Sent Vibration to {dispName}'s Shock Collar for: {durationMs}ms", LoggerType.StickyUI);
            });
        }
    }

    private void BeepAct(KinksterInfoCache cache, Kinkster k, string dispName, float width, TimeSpan maxDuration)
    {
        var max = (float)maxDuration.TotalSeconds;
        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.LandMineOn, "Beep") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat("##DurationSliderRef" + k.UserData.UID, ref cache.ApplyVibeDur, 0.1f, max, "%.1fs", ImGuiSliderFlags.None);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.LandMineOn, "Send Beep", disabled: cache.ApplyVibeDur <= 0))
        {
            var durationMs = (int)(cache.ApplyVibeDur * 1000f);
            _logger.LogDebug($"Sending Beep for: {durationMs}ms");
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserShockKinkster(new ShockCollarAction(k.UserData, 2, 0, durationMs));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    _logger.LogDebug($"Failed to send Beep to {dispName}'s Shock Collar. ({res})", LoggerType.StickyUI);
                else
                    _logger.LogDebug($"Sent Beep to {dispName}'s Shock Collar for: {durationMs}ms", LoggerType.StickyUI);
            });
        }
    }
}
