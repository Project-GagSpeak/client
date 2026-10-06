using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using GagspeakAPI.User;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

// Helper methods for drawing out the hardcore actions.
public partial class SidePanelPair
{
    private int? _maxIntensityEdit;
    private int? _maxDurationEdit;

    private void UniqueShareCode(Kinkster k, string dispName, float width)
    {
        using var _ = ImRaii.Group();

        var refCode = k.OwnPerms.PiShockShareCode;
        CkGui.IconInputText(FAI.ShareAlt, string.Empty, "Unique Share Code", ref refCode, 40, width, true, false);
        if (ImGui.IsItemDeactivatedAfterEdit() && refCode != k.OwnPerms.PiShockShareCode)
            ChangeShockPerm(k, nameof(PairPerms.PiShockShareCode), refCode);
        CkGui.AttachTooltip($"Unique Share Code for --COL--{dispName}--COL--." +
            $"--NL--This code gives {dispName} permission to interact with your PiShock device." +
            "--NL--PiShock also enforces the limits set on the code itself.");

        ShockToggle(k, "Shock", k.OwnPerms.AllowShocks, nameof(PairPerms.AllowShocks));
        ImGui.SameLine();
        ShockToggle(k, "Vibrate", k.OwnPerms.AllowVibrations, nameof(PairPerms.AllowVibrations));
        ImGui.SameLine();
        ShockToggle(k, "Beep", k.OwnPerms.AllowBeeps, nameof(PairPerms.AllowBeeps));

        var curIntensity = Math.Clamp(k.OwnPerms.MaxIntensity, 1, 100);
        ShockLimitSlider(k, ref _maxIntensityEdit, curIntensity, 1, 100, "Max Intensity: %d%%", nameof(PairPerms.MaxIntensity), width);
        var curDuration = (int)Math.Clamp(k.OwnPerms.GetTimespanFromDuration().TotalSeconds, 1, 15);
        ShockLimitSlider(k, ref _maxDurationEdit, curDuration, 1, 15, "Max Duration: %ds", nameof(PairPerms.MaxDuration), width);

        if (!string.IsNullOrWhiteSpace(k.OwnPerms.PiShockShareCode) && k.OwnPerms.MaxDuration <= 0)
            CkGui.ColorText("Set a Max Duration to enable shock actions.", ImGuiColors.DalamudYellow);
    }

    private void ShockToggle(Kinkster k, string label, bool current, string propertyName)
    {
        if (ImGui.Checkbox($"{label}##{k.User.UID}", ref current))
            ChangeShockPerm(k, propertyName, current);
    }

    // Holds the dragged value while active so the slider doesn't snap back to the stored perm each frame.
    private void ShockLimitSlider(Kinkster k, ref int? editValue, int current, int min, int max, string format, string propertyName, float width)
    {
        var value = editValue ?? current;
        ImGui.SetNextItemWidth(width);
        if (ImGui.SliderInt($"##{propertyName}{k.User.UID}", ref value, min, max, format))
            editValue = value;
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            ChangeShockPerm(k, propertyName, value);
            editValue = null;
        }
    }

    private void ChangeShockPerm(Kinkster k, string propertyName, object newValue)
        => UiService.SetUITask(async () => await PermHelper.ChangeOwnUnique(_hub, k.User, k.OwnPerms, propertyName, newValue));

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

    private void ShockAct(KinksterInfoCache cache, Kinkster k, string dispName, float width, TimeSpan maxDuration)
    {
        var maxIntensity = k.PairPerms.MaxIntensity;
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt($"##SCI-{k.User.UID}", ref cache.ApplyIntensity, 0, maxIntensity, " % d%%", ImGuiSliderFlags.None);

        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.BoltLightning, "Shock") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat($"##SCD-{k.User.UID}", ref cache.ApplyDuration, 0.1f, (float)maxDuration.TotalSeconds, "%.1fs", ImGuiSliderFlags.None);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.BoltLightning, "Send Shock", disabled: cache.ApplyDuration <= 0))
        {
            var durationMs = (int)(cache.ApplyDuration * 1000f);
            _logger.LogDebug($"Sending Shock with duration: {durationMs}ms");
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserShockKinkster(new(k.User, 0, cache.ApplyIntensity, durationMs));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                {
                    _logger.LogDebug($"Failed to send Shock to {dispName}'s Shock Collar. ({res})", LogFilter.StickyUI);
                    return;
                }
                _logger.LogDebug($"Sent Shock to {dispName}'s Shock Collar for: {durationMs}ms", LogFilter.StickyUI);
                GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockSent);
            });
        }
    }

    private void VibeAct(KinksterInfoCache cache, Kinkster k, string dispName, float width, TimeSpan maxDuration)
    {
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt($"##ISR-{k.User.UID}", ref cache.ApplyVibeIntensity, 0, 100, "%d%%", ImGuiSliderFlags.None);

        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.HeartCircleBolt, "Vibrate") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat($"##DSR-{k.User.UID}", ref cache.ApplyVibeDur, 0.0f, (float)maxDuration.TotalSeconds, "%.1fs", ImGuiSliderFlags.None);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.HeartCircleBolt, "Send Vibration", disabled: cache.ApplyVibeDur <= 0))
        {
            var durationMs = (int)(cache.ApplyVibeDur * 1000f);
            _logger.LogDebug($"Sending Vibration with duration: {durationMs}ms");
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserShockKinkster(new(k.User, 1, cache.ApplyVibeIntensity, durationMs));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    _logger.LogDebug($"Failed to send Vibration to {dispName}'s Shock Collar. ({res})", LogFilter.StickyUI);
                else
                    _logger.LogDebug($"Sent Vibration to {dispName}'s Shock Collar for: {durationMs}ms", LogFilter.StickyUI);
            });
        }
    }

    private void BeepAct(KinksterInfoCache cache, Kinkster k, string dispName, float width, TimeSpan maxDuration)
    {
        var max = (float)maxDuration.TotalSeconds;
        ImGui.SetNextItemWidth(width - CkGui.IconTextButtonSize(FAI.LandMineOn, "Beep") - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SliderFloat("##DurationSliderRef" + k.User.UID, ref cache.ApplyVibeDur, 0.1f, max, "%.1fs", ImGuiSliderFlags.None);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.LandMineOn, "Send Beep", disabled: cache.ApplyVibeDur <= 0))
        {
            var durationMs = (int)(cache.ApplyVibeDur * 1000f);
            _logger.LogDebug($"Sending Beep for: {durationMs}ms");
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserShockKinkster(new ShockCollarAction(k.User, 2, 0, durationMs));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    _logger.LogDebug($"Failed to send Beep to {dispName}'s Shock Collar. ({res})", LogFilter.StickyUI);
                else
                    _logger.LogDebug($"Sent Beep to {dispName}'s Shock Collar for: {durationMs}ms", LogFilter.StickyUI);
            });
        }
    }
}
