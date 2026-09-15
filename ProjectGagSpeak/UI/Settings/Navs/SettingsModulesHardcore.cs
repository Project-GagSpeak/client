using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Gui.Settings;

public class SettingsModulesHardcore
{
    private enum HardcoreTabs
    {
        HardcoreState,
    }

    private readonly MainConfig _config;
    private readonly HardcoreEscapeService _escape;

    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<HardcoreTabs> _tabs;

    public SettingsModulesHardcore(GagspeakMediator mediator, MainConfig config, HardcoreEscapeService escape)
    {
        _config = config;
        _escape = escape;

        _tabs = new StylizedTabbarBuilder<HardcoreTabs>()
            .AddTab(HardcoreTabs.HardcoreState, "Hardcore State", FAI.Handcuffs)
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
}
