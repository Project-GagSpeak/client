using CkCommons;
using CkCommons.Gui;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.MainWindow;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;

namespace GagSpeak.Gui.Components;

public class MainMenuTabs : IconTabBar<MainMenuTabs.SelectedTab>
{
    public enum SelectedTab
    {
        Homepage,
        Requests,
        Whitelist,
        PatternSharehub,
        LociSharehub,
        GlobalChat,
    }

    public override SelectedTab TabSelection
    {
        get => base.TabSelection;
        set
        {
            _config.Data.MainUiTab = value;
            _config.Save();
            base.TabSelection = value;
        }
    }

    private readonly MainConfig _config;
    private readonly ChatConfig _chatConfig;
    private readonly GagspeakMediator _mediator;
    private readonly RequestsManager _requests;
    private readonly GlobalChatLog _globalChat;

    public MainMenuTabs(GagspeakMediator mediator, MainConfig config, ChatConfig chatConfig,
        RequestsManager requests, GlobalChatLog globalChat, TutorialService guides)
    {
        _config = config;
        _chatConfig = chatConfig;
        _mediator = mediator;
        _requests = requests;
        _globalChat = globalChat;

        AddDrawButton(FontAwesomeIcon.Home, SelectedTab.Homepage, "Homepage",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.Homepage, MainUI.LastPos, MainUI.LastSize));

        AddDrawButton(FontAwesomeIcon.Inbox, SelectedTab.Requests, "Kinkster Requests",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.RequestsPage, MainUI.LastPos, MainUI.LastSize, _ => TabSelection = SelectedTab.Whitelist));

        AddDrawButton(FontAwesomeIcon.PeopleArrows, SelectedTab.Whitelist, "Kinkster Whitelist", 
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.Whitelist, MainUI.LastPos, MainUI.LastSize, _ => TabSelection = SelectedTab.Homepage));

        AddDrawButton(FontAwesomeIcon.Compass, SelectedTab.PatternSharehub, "Discover Patterns from the community!", 
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHub, MainUI.LastPos, MainUI.LastSize));

        AddDrawButton(FontAwesomeIcon.WandMagicSparkles, SelectedTab.LociSharehub, "Browse LociData made by others in the community!",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.LociSharehub, MainUI.LastPos, MainUI.LastSize));

        AddDrawButton(FontAwesomeIcon.Comments, SelectedTab.GlobalChat, "Meet & Chat with others in a cross-region chat!",
            () => guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.GlobalChat, MainUI.LastPos, MainUI.LastSize));

        TabSelectionChanged += (oldTab, newTab) => _mediator.Publish(new MainWindowTabChangeMessage(newTab));
    }

    public override void Draw(float availableWidth)
    {
        if (_tabButtons.Count == 0)
            return;

        using var color = ImRaii.PushColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0)));
        var spacing = ImGui.GetStyle().ItemSpacing;
        var buttonX = (availableWidth - (spacing.X * (_tabButtons.Count - 1))) / _tabButtons.Count;
        var buttonY = CkGui.IconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);
        var drawList = ImGui.GetWindowDrawList();
        var underlineColor = ImGui.GetColorU32(ImGuiCol.Separator);

        ImGuiHelpers.ScaledDummy(spacing.Y / 2f);

        foreach (var tab in _tabButtons)
            DrawTabButton(tab, buttonSize, spacing, drawList);

        // advance to the new line and dispose of the button color.
        ImGui.NewLine();
        color.Dispose();

        ImGuiHelpers.ScaledDummy(3f);
        ImGui.Separator();
    }

    protected override void DrawTabButton(TabButtonDefinition tab, Vector2 buttonSize, Vector2 spacing, ImDrawListPtr drawList)
    {
        var x = ImGui.GetCursorScreenPos();

        var isDisabled = IsTabDisabled(tab.TargetTab);
        using (ImRaii.Disabled(isDisabled))
        {

            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button(tab.Icon.ToIconString(), buttonSize))
                    TabSelection = tab.TargetTab;
            }

            ImGui.SameLine();
            var xPost = ImGui.GetCursorScreenPos();

            if (EqualityComparer<SelectedTab>.Default.Equals(TabSelection, tab.TargetTab))
            {
                drawList.AddLine(
                    x with { Y = x.Y + buttonSize.Y + spacing.Y },
                    xPost with { Y = xPost.Y + buttonSize.Y + spacing.Y, X = xPost.X - spacing.X },
                    ImGui.GetColorU32(ImGuiCol.Separator), 2f);
            }

            if (tab.TargetTab is SelectedTab.Requests)
            {
                if (_requests.Incoming.Count > 0)
                {
                    var newMsgTxtPos = new Vector2(x.X + buttonSize.X * .65f, x.Y - spacing.Y);
                    var newMsgTxt = _requests.Incoming.Count > 99 ? "99+" : _requests.Incoming.Count.ToString();
                    drawList.OutlinedFont(newMsgTxt, newMsgTxtPos, ImGuiColors.ParsedPink.ToUint(), 0xFF000000, 1);
                }
            }
            else if (tab.TargetTab is SelectedTab.GlobalChat && _chatConfig.Data.UnreadBubble)
            {
                if (_globalChat.UnreadMentions > 0)
                {
                    var pos = new Vector2(x.X + buttonSize.X / 2, x.Y - spacing.Y);
                    var txt = _globalChat.UnreadMentions > 99 ? "99+" : _globalChat.UnreadMentions.ToString();
                    drawList.OutlinedFont(txt, pos, CkCol.TriStateCross.Uint(), 0xFF000000, 1);
                }
                else if (_globalChat.UnreadMessages > 0)
                {
                    var pos = new Vector2(x.X + buttonSize.X / 2, x.Y - spacing.Y);
                    var txt = _globalChat.UnreadMessages > 99 ? "99+" : _globalChat.UnreadMessages.ToString();
                    drawList.OutlinedFont(txt, pos, ImGuiColors.TankBlue.ToUint(), 0xFF000000, 1);
                }
            }
        }
        CkGui.AttachTooltip(tab.Tooltip);

        // invoke action if we should.
        tab.CustomAction?.Invoke();
    }

}
