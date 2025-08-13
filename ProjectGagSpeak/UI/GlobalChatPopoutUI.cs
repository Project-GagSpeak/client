using CkCommons;
using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using Dalamud.Bindings.ImGui;

namespace GagSpeak.Gui;

public class GlobalChatPopoutUI : WindowMediatorSubscriberBase
{
    private readonly KinkPlateService _plateService;
    private readonly PopoutGlobalChatlog _popoutGlobalChat;
    private bool _themePushed = false;

    public GlobalChatPopoutUI(ILogger<GlobalChatPopoutUI> logger, GagspeakMediator mediator,
        KinkPlateService plateService, PopoutGlobalChatlog popoutGlobalChat) 
        : base(logger, mediator, "Global Chat Popout UI")
    {
        _plateService = plateService;
        _popoutGlobalChat = popoutGlobalChat;

        IsOpen = false;
        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(380, 500), new Vector2(700, 2000));
    }
    protected override void PreDrawInternal()
    {
        if (!_themePushed)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));

            _themePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (_themePushed)
        {
            ImGui.PopStyleColor(2);
            _themePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        using var font = UiFontService.Default150Percent.Push();
        using var col = ImRaii.PushColor(ImGuiCol.ScrollbarBg, CkColor.LushPinkButton.Uint())
            .Push(ImGuiCol.ScrollbarGrab, CkColor.VibrantPink.Uint())
            .Push(ImGuiCol.ScrollbarGrabHovered, CkColor.VibrantPinkHovered.Uint());
        // grab the profile object from the profile service.
        var profile = _plateService.GetKinkPlate(MainHub.PlayerUserData);
        if (profile.KinkPlateInfo.Disabled || !MainHub.IsVerifiedUser)
        {
            ImGui.Spacing();
            CkGui.ColorTextCentered("Social Features have been Restricted", ImGuiColors.DalamudRed);
            ImGui.Spacing();
            CkGui.ColorTextCentered("Cannot View Global Chat because of this.", ImGuiColors.DalamudRed);
            return;
        }
        
        _popoutGlobalChat.DrawChat(ImGui.GetContentRegionAvail());
    }
}
