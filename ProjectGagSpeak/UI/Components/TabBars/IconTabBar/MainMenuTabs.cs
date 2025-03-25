using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using ImGuiNET;

namespace GagSpeak.UI.Components;

public class MainMenuTabs : IconTabBar<MainMenuTabs.SelectedTab>
{
    public enum SelectedTab
    {
        Homepage,
        Whitelist,
        PatternHub,
        MoodlesHub,
        GlobalChat,
        MySettings
    }

    private readonly GagspeakMediator _mediator;
    private readonly TutorialService _guides;
    public MainMenuTabs(GagspeakMediator mediator, TutorialService guides)
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

        AddDrawButton(FontAwesomeIcon.WandMagicSparkles, SelectedTab.MoodlesHub, "Browse Moodles made by others in the community!");
            /* maybe add some tutorial for this later */

        AddDrawButton(FontAwesomeIcon.Comments, SelectedTab.GlobalChat, "Meet & Chat with others in a cross-region chat!",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ToGlobalChat, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => TabSelection = SelectedTab.GlobalChat));

        AddDrawButton(FontAwesomeIcon.UserCircle, SelectedTab.MySettings, "Account User Settings",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ToAccountPage, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => TabSelection = SelectedTab.MySettings));

        TabSelectionChanged += (oldTab, newTab) => _mediator.Publish(new MainWindowTabChangeMessage(newTab));
    }

    public override void Draw(float availableWidth)
    {
        if (_tabButtons.Count == 0)
            return;

        using var btncolor = ImRaii.PushColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0)));
        var spacing = ImGui.GetStyle().ItemSpacing;
        var buttonX = (availableWidth - (spacing.X * (_tabButtons.Count - 1))) / _tabButtons.Count;
        var buttonY = CkGui.IconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);
        var drawList = ImGui.GetWindowDrawList();
        var underlineColor = ImGui.GetColorU32(ImGuiCol.Separator);

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
