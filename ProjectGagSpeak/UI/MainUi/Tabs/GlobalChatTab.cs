using CkCommons;
using CkCommons.Gui;
using CkCommons.Helpers;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class GlobalChatTab : DisposableMediatorSubscriberBase
{
    private readonly GlobalChatLog _globalChat;
    private readonly KinkPlateService _plateManager;
    private readonly TutorialService _guides;
    private readonly MainMenuTabs _tabmenu;

    public GlobalChatTab(ILogger<GlobalChatTab> logger, GagspeakMediator mediator,
        GlobalChatLog globalChat, KinkPlateService plateManager, TutorialService guides, MainMenuTabs tabMenu)
        : base(logger, mediator)
    {
        _globalChat = globalChat;
        _plateManager = plateManager;
        _tabmenu = tabMenu;
        _guides = guides;
    }

    public void DrawDiscoverySection()
    {
        ImGuiUtil.Center("Global GagSpeak Chat");
        ImGui.Separator();
        DrawGlobalChatlog("GS_MainGlobal");
    }

    public void DrawGlobalChatlog(string windowId)
    {
        // grab the profile object from the profile service.
        var profile = _plateManager.GetKinkPlate(MainHub.PlayerUserData);
        if (profile.KinkPlateInfo.Disabled || !MainHub.IsVerifiedUser)
        {
            ImGui.Spacing();
            CkGui.ColorTextCentered("Social Features have been Restricted", ImGuiColors.DalamudRed);
            ImGui.Spacing();
            CkGui.ColorTextCentered("Cannot View Global Chat because of this.", ImGuiColors.DalamudRed);
            return;
        }

        _globalChat.DrawChat(ImGui.GetContentRegionAvail());
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.UsingGlobalChat, ImGui.GetWindowPos(), ImGui.GetWindowSize());
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ChatMessageExamine, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
            () => _tabmenu.TabSelection = MainMenuTabs.SelectedTab.MySettings);
    }
}

