using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.MainWindow;
using ImGuiNET;

namespace GagSpeak.UI;

public class GlobalChatPopoutUI : WindowMediatorSubscriberBase
{
    private readonly GlobalChatTab _globalChat;
    private readonly CosmeticService _cosmetics;
    public GlobalChatPopoutUI(ILogger<GlobalChatPopoutUI> logger, GagspeakMediator mediator,
        GlobalChatTab globalChat, CosmeticService cosmetics) : base(logger, mediator, "Global Chat Popout UI")
    {
        _globalChat = globalChat;
        _cosmetics = cosmetics;

        IsOpen = false;
        RespectCloseHotkey = false;
        AllowPinning = false;
        AllowClickthrough = false;

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(380, 500),
            MaximumSize = new Vector2(700, 2000),
        };
    }

    private bool HoveringCloseButton { get; set; } = false;

    private bool ThemePushed = false;
    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));

            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        CkGui.AttachToolTip("Right-Click this area to close Global Chat Popout!");
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            IsOpen = false;
            return;
        }
        // draw out global chat here.
        _globalChat.DrawGlobalChatlog("GS_MainGlobal_Popout");
    }

/*    private void CloseButton(ImDrawListPtr drawList)
    {
        var btnPos = CloseButtonPos;
        var btnSize = CloseButtonSize;

        var closeButtonColor = HoveringCloseButton ? ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)) : ImGui.GetColorU32(ImGuiColors.ParsedPink);

        drawList.AddLine(btnPos, btnPos + btnSize, closeButtonColor, 3);
        drawList.AddLine(new Vector2(btnPos.X + btnSize.X, btnPos.Y), new Vector2(btnPos.X, btnPos.Y + btnSize.Y), closeButtonColor, 3);


        ImGui.SetCursorScreenPos(btnPos);
        if (ImGui.InvisibleButton($"CloseButton##KinkPlateClosePreview" + MainHub.UID, btnSize))
        {
            this.IsOpen = false;
        }
        HoveringCloseButton = ImGui.IsItemHovered();
    }*/


}
