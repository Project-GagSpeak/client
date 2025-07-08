using CkCommons.Gui;
using CkCommons.Helpers;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
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
    private readonly MainHub _hub;
    private readonly GlobalChatLog _globalChat;
    private readonly GlobalPermissions _globals;
    private readonly MufflerService _garbler;
    private readonly GagRestrictionManager _gagManager;
    private readonly MainConfig _mainConfig;
    private readonly KinkPlateService _plateManager;
    private readonly TutorialService _guides;

    public GlobalChatTab(
        ILogger<GlobalChatTab> logger,
        GagspeakMediator mediator,
        MainHub hub,
        GlobalChatLog globalChat,
        GlobalPermissions globals,
        MufflerService garbler,
        GagRestrictionManager gagManager,
        MainConfig mainConfig,
        KinkPlateService plateManager,
        TutorialService guides) : base(logger, mediator)
    {
        _hub = hub;
        _globalChat = globalChat;
        _globals = globals;
        _garbler = garbler;
        _gagManager = gagManager;
        _mainConfig = mainConfig;
        _plateManager = plateManager;
        _guides = guides;
    }

    public void DrawDiscoverySection()
    {
        ImGuiUtil.Center("Global GagSpeak Chat");
        ImGui.Separator();
        DrawGlobalChatlog("GS_MainGlobal");
    }

    private bool showMessagePreview = false;

    public void DrawGlobalChatlog(string windowId)
    {
        using var scrollbar = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        // grab the content region of the current section
        var CurrentRegion = ImGui.GetContentRegionAvail();

        // grab the profile object from the profile service.
        var profile = _plateManager.GetKinkPlate(MainHub.PlayerUserData);
        if (profile.KinkPlateInfo.Disabled)
        {
            ImGui.Spacing();
            CkGui.ColorTextCentered("Social Features have been Restricted", ImGuiColors.DalamudRed);
            ImGui.Spacing();
            CkGui.ColorTextCentered("Cannot View Global Chat because of this.", ImGuiColors.DalamudRed);
            return;
        }

        _globalChat.DrawChat(ImGui.GetContentRegionAvail(), ref showMessagePreview);
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.GlobalChat, ImGui.GetWindowPos(), ImGui.GetWindowSize());
    }
}

