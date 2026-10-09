using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;

namespace GagSpeak.Gui.Settings;

public class SettingsModulesHardcore
{
    private enum HardcoreTabs
    {
        HardcoreState,
        Arousal,
    }

    private readonly MainConfig _config;
    private readonly MainHub _hub;
    private readonly HardcoreEscapeService _escape;
    private readonly ArousalService _arousal;

    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<HardcoreTabs> _tabs;

    public SettingsModulesHardcore(GagspeakMediator mediator, MainConfig config, MainHub hub,
        HardcoreEscapeService escape, ArousalService arousal)
    {
        _config = config;
        _hub = hub;
        _escape = escape;
        _arousal = arousal;

        _tabs = new StylizedTabbarBuilder<HardcoreTabs>()
            .AddTab(HardcoreTabs.HardcoreState, "Hardcore State", FAI.Handcuffs)
            .AddTab(HardcoreTabs.Arousal, "Arousal", FAI.Heartbeat)
            .Build();
    }

    public int NavbarIdx => (int)_tabs.TabSelection;

    public void SetNavbarIdx(int idx)
    {
        if (idx < 0 || idx >= Enum.GetValues<HardcoreTabs>().Length)
            return;
        _tabs.TabSelection = (HardcoreTabs)idx;
    }

    // All Content
    public void Draw(ImGuiWindowPtr winPtr)
    {
        // Draw out the tabs using the custom backgrounds and whatever else.
        _tabs.DrawTabs(TabBarFlags.MinimalGlow);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2f * ImGuiHelpers.GlobalScale);
        ImGui.Separator();
        ImGui.Spacing();
        using var _ = ImRaii.Child("main-notifs-inner");
        // Draw out the content based on the selected tab.
        switch (_tabs.TabSelection)
        {
            case HardcoreTabs.HardcoreState:
                DrawHardcoreStateOptions();
                break;
            case HardcoreTabs.Arousal:
                DrawArousalOptions();
                break;
        }
    }

    private void DrawHardcoreStateOptions()
    {
        CkGui.FontText("Hardcore State", Fonts.SubtitleFont);

        if (ClientData.Globals is not { } globals)
        {
            ImGui.Text("Global Perms is null! Safely returning early");
            return;
        }

        var hcCursedLoot = _config.Data.CursedItemsApplyTraits;
        if (CkGui.Checkbox(GSLoc.Settings.Options.MimicsApplyTraits, ref hcCursedLoot, !globals.WardrobeEnabled))
        {
            _config.Data.CursedItemsApplyTraits = hcCursedLoot;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.MimicsApplyTraitsTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

        var hcCursedOverlays = _config.Data.CursedItemsApplyOverlays;
        if (CkGui.Checkbox(GSLoc.Settings.Options.MimicsApplyOverlays, ref hcCursedOverlays, !globals.WardrobeEnabled))
        {
            _config.Data.CursedItemsApplyOverlays = hcCursedOverlays;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.MimicsApplyOverlaysTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

        var blindfoldMaxOpacity = _config.Data.OverlayMaxOpacity;
        var hardcoreEscape = _config.Data.HardcoreEscape;

        using (ImRaii.PushIndent())
        {
            // show a prettier value for the end user
            blindfoldMaxOpacity *= 100;
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat(GSLoc.Settings.Options.OverlayMaxOpacity, ref blindfoldMaxOpacity, 0f, 100f, "%.1f%%", ImGuiSliderFlags.AlwaysClamp))
            {
                _config.Data.OverlayMaxOpacity = blindfoldMaxOpacity / 100;
                _config.Save();
            }
            CkGui.HelpTextFramed(GSLoc.Settings.Options.OverlayMaxOpacityTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);
        }
        
        if (CkGui.Checkbox(GSLoc.Settings.Options.HardcoreEscape, ref hardcoreEscape, hardcoreEscape && !_escape.CanDisable))
        {
            _config.Data.HardcoreEscape = hardcoreEscape;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.HardcoreEscapeTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);
    }

    private void DrawArousalOptions()
    {
        CkGui.FontText("Arousal", Fonts.SubtitleFont);
        if (ClientData.Globals is not { } globals)
            return;

        var globalArousal = globals.GlobalArousal;
        if (ImGui.Checkbox(GSLoc.Settings.Options.GlobalArousal, ref globalArousal))
            UiService.SetUITask(async () => await PermHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.GlobalArousal), globalArousal));
        CkGui.HelpTextFramed(GSLoc.Settings.Options.GlobalArousalTT);

        // Individual effects only matter while Arousal Effects is enabled.
        using (ImRaii.PushIndent())
        {
            DrawArousalToggle(GSLoc.Settings.Options.ArousalStutter, GSLoc.Settings.Options.ArousalStutterTT,
                _config.Data.ArousalStutter, v => _config.Data.ArousalStutter = v, !globalArousal);
            DrawArousalToggle(GSLoc.Settings.Options.ArousalWordLimit, GSLoc.Settings.Options.ArousalWordLimitTT,
                _config.Data.ArousalWordLimit, v => _config.Data.ArousalWordLimit = v, !globalArousal);
            DrawArousalToggle(GSLoc.Settings.Options.ArousalBlush, GSLoc.Settings.Options.ArousalBlushTT,
                _config.Data.ArousalBlush, v => _config.Data.ArousalBlush = v, !globalArousal);
            DrawArousalToggle(GSLoc.Settings.Options.ArousalGcdDelay, GSLoc.Settings.Options.ArousalGcdDelayTT,
                _config.Data.ArousalGcdDelay, v => _config.Data.ArousalGcdDelay = v, !globalArousal);
            // Blur is not implemented yet, so its toggle stays disabled.
            DrawArousalToggle(GSLoc.Settings.Options.ArousalBlur, GSLoc.Settings.Options.ArousalBlurTT, false, _ => { }, true);
        }

        // The meter runs regardless of Arousal Effects, so the timings are always editable.
        var buildMinutes = _config.Data.ArousalBuildMinutes;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderInt(GSLoc.Settings.Options.ArousalBuildTime, ref buildMinutes, 15, 480, "%d min", ImGuiSliderFlags.AlwaysClamp))
        {
            _config.Data.ArousalBuildMinutes = buildMinutes;
            _config.Save();
            _arousal.UpdateFinalCache();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.ArousalBuildTimeTT, true);

        var decayMinutes = _config.Data.ArousalDecayMinutes;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderInt(GSLoc.Settings.Options.ArousalDecayTime, ref decayMinutes, 5, 480, "%d min", ImGuiSliderFlags.AlwaysClamp))
        {
            _config.Data.ArousalDecayMinutes = decayMinutes;
            _config.Save();
            _arousal.UpdateFinalCache();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.ArousalDecayTimeTT, true);
    }

    private void DrawArousalToggle(string label, string tooltip, bool value, Action<bool> set, bool disabled)
    {
        if (CkGui.Checkbox(label, ref value, disabled))
        {
            set(value);
            _config.Save();
        }
        CkGui.HelpTextFramed(tooltip, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);
    }
}
