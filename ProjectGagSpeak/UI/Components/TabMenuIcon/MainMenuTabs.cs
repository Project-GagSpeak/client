using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Numerics;

namespace GagSpeak.UI.Components;

public class MainMenuTabs : IconTabBarBase<MainMenuTabs.SelectedTab>
{
    public enum SelectedTab
    {
        Homepage,
        Whitelist,
        PatternHub,
        GlobalChat,
        MySettings
    }

    private readonly GagspeakMediator _mediator;
    private readonly TutorialService _guides;

    public MainMenuTabs(UiSharedService uiSharedService, GagspeakMediator mediator,
        TutorialService guides) : base(uiSharedService)
    {
        _mediator = mediator;
        _guides = guides;

        AddDrawButton(FontAwesomeIcon.Home, SelectedTab.Homepage, "Homepage",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.Homepage, ImGui.GetWindowPos(), ImGui.GetWindowSize()));

        AddDrawButton(FontAwesomeIcon.PeopleArrows, SelectedTab.Whitelist, "Kinkster Whitelist", () =>
        {
            guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ToWhitelistPage, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => TabSelection = SelectedTab.Whitelist);
            guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.Whitelist, ImGui.GetWindowPos(), ImGui.GetWindowSize());
        });

        AddDrawButton(FontAwesomeIcon.Compass, SelectedTab.PatternHub, "Discover Patterns from the community!", 
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ToPatternHub, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => TabSelection = SelectedTab.PatternHub));

        AddDrawButton(FontAwesomeIcon.Comments, SelectedTab.GlobalChat, "Meet & Chat with others in a cross-region chat!",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ToGlobalChat, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => TabSelection = SelectedTab.GlobalChat));

        AddDrawButton(FontAwesomeIcon.UserCircle, SelectedTab.MySettings, "Account User Settings",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ToAccountPage, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => TabSelection = SelectedTab.MySettings));
    }

    protected override void OnTabSelectionChanged(MainMenuTabs.SelectedTab newTab)
    {
        _mediator.Publish(new MainWindowTabChangeMessage(newTab));
    }

    public override void Draw()
    {
        if (_tabButtons.Count == 0) return;

        var availableWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
        var spacing = ImGui.GetStyle().ItemSpacing;
        var buttonX = (availableWidth - (spacing.X * (_tabButtons.Count - 1))) / _tabButtons.Count;
        var buttonY = UiSharedService.GetIconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);
        var drawList = ImGui.GetWindowDrawList();
        var underlineColor = ImGui.GetColorU32(ImGuiCol.Separator);
        var btncolor = ImRaii.PushColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0)));

        ImGuiHelpers.ScaledDummy(spacing.Y / 2f);

        foreach (var tab in _tabButtons)
        {
            DrawTabButton(tab, buttonSize, spacing, drawList);
        }

        // advance to the new line and dispose of the button color.
        ImGui.NewLine();
        btncolor.Dispose();

        ImGuiHelpers.ScaledDummy(3f);
        ImGui.Separator();
    }

}
