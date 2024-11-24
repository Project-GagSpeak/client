using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Handlers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.UserPair;
using ImGuiNET;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.Components.UserPairList;

/// <summary>
/// Class handling the draw function for a singular user pair that the client has. (one row)
/// </summary>
public class DrawUserPair
{
    protected readonly MainHub _apiHubMain;
    protected readonly IdDisplayHandler _displayHandler;
    protected readonly GagspeakMediator _mediator;
    protected Pair _pair;
    private readonly string _id;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;
    private Dictionary<byte, bool> IsHovered = new();
    public DrawUserPair(ILogger<DrawUserPair> logger, string id, Pair entry, MainHub apiHubMain,
        IdDisplayHandler uIDDisplayHandler, GagspeakMediator mediator,
        CosmeticService cosmetics, UiSharedService uiShared)
    {
        _id = id;
        _pair = entry;
        _apiHubMain = apiHubMain;
        _displayHandler = uIDDisplayHandler;
        _mediator = mediator;
        _cosmetics = cosmetics;
        _uiShared = uiShared;
    }

    public Pair Pair => _pair;
    public UserPairDto UserPair => _pair.UserPair!;

    public bool DrawPairedClient(byte ident, bool supporterIcon = true, bool icon = true, bool iconTT = true, bool displayToggles = true, 
        bool displayNameTT = true, bool showHovered = true, bool showRightButtons = true)
    {
        // if no key exist for the dictionary, add it with default value of false.
        if (!IsHovered.ContainsKey(ident))
        {
            IsHovered.Add(ident, false);
        }

        bool selected = false;
        // get the current screen cursor pos
        var cursorPos = ImGui.GetCursorPosX();
        using var id = ImRaii.PushId(GetType() + _id);
        using (ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), showHovered && IsHovered[ident]))
        {
            using (ImRaii.Child(GetType() + _id, new Vector2(UiSharedService.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImGui.GetFrameHeight())))
            {
                ImUtf8.SameLineInner();
                if (icon)
                {
                    DrawLeftSide(iconTT);
                }
                ImGui.SameLine();
                var posX = ImGui.GetCursorPosX();

                float rightSide = ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth()
                                  - (_uiShared.GetIconButtonSize(FontAwesomeIcon.EllipsisV).X);

                if (showRightButtons)
                {
                    rightSide = DrawRightSide();
                }

                selected = DrawName(posX, rightSide, displayToggles, displayNameTT);
            }

            IsHovered[ident] = ImGui.IsItemHovered();
        }
        // if they were a supporter, go back to the start and draw the image.
        if (supporterIcon && _pair.UserData.SupporterTier is not CkSupporterTier.NoRole)
        {
            DrawSupporterIcon(cursorPos);
        }
        return selected;
    }

    private void DrawSupporterIcon(float cursorPos)
    {
        var Image = _cosmetics.GetSupporterInfo(Pair.UserData);
        if (Image.SupporterWrap is { } wrap)
        {
            ImGui.SameLine(cursorPos);
            ImGui.SetCursorPosX(cursorPos - _uiShared.GetIconData(FontAwesomeIcon.EllipsisV).X - ImGui.GetStyle().ItemSpacing.X);
            ImGui.Image(wrap.ImGuiHandle, new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight()));
            UiSharedService.AttachToolTip(Image.Tooltip);
        }
        // return to the end of the line.
    }

    private void DrawLeftSide(bool showToolTip)
    {
        var userPairText = string.Empty;

        ImGui.AlignTextToFramePadding();

        if (!_pair.IsOnline)
        {
            using var _ = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            _uiShared.IconText(_pair.IndividualPairStatus == IndividualPairStatus.OneSided
                ? FontAwesomeIcon.ArrowsLeftRight
                : _pair.IndividualPairStatus == IndividualPairStatus.Bidirectional
                    ? FontAwesomeIcon.User : FontAwesomeIcon.Users);
            userPairText = _pair.UserData.AliasOrUID + " is offline";
        }
        else if (_pair.IsVisible)
        {
            _uiShared.IconText(FontAwesomeIcon.Eye, ImGuiColors.ParsedGreen);
            userPairText = _pair.UserData.AliasOrUID + " is visible: " + _pair.PlayerName + Environment.NewLine + "Click to target this player";
            if (ImGui.IsItemClicked())
            {
                _mediator.Publish(new TargetPairMessage(_pair));
            }
        }
        else
        {
            using var _ = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
            _uiShared.IconText(_pair.IndividualPairStatus == IndividualPairStatus.Bidirectional
                ? FontAwesomeIcon.User : FontAwesomeIcon.Users);
            userPairText = _pair.UserData.AliasOrUID + " is online";
        }

        if (_pair.IndividualPairStatus == IndividualPairStatus.OneSided)
        {
            userPairText += UiSharedService.TooltipSeparator + "User has not added you back";
        }
        else if (_pair.IndividualPairStatus == IndividualPairStatus.Bidirectional)
        {
            userPairText += UiSharedService.TooltipSeparator + "You are directly Paired";
        }

        if (showToolTip)
            UiSharedService.AttachToolTip(userPairText);

        ImGui.SameLine();
    }

    private bool DrawName(float leftSide, float rightSide, bool canTogglePairTextDisplay, bool displayNameTT)
    {
        return _displayHandler.DrawPairText(_id, _pair, leftSide, () => rightSide - leftSide, canTogglePairTextDisplay, displayNameTT);
    }

    private float DrawRightSide()
    {
        var permissionsButtonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Cog);
        var barButtonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.EllipsisV);
        var spacingX = ImGui.GetStyle().ItemSpacing.X / 2;
        var windowEndX = ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth();
        var currentRightSide = windowEndX - barButtonSize.X;

        ImGui.SameLine(currentRightSide);
        ImGui.AlignTextToFramePadding();
        if (_uiShared.IconButton(FontAwesomeIcon.EllipsisV))
        {
            // open the permission setting window
            _mediator.Publish(new OpenUserPairPermissions(_pair, StickyWindowType.PairActionFunctions, false));
        }

        currentRightSide -= permissionsButtonSize.X + spacingX;
        ImGui.SameLine(currentRightSide);
        if (_uiShared.IconButton(FontAwesomeIcon.Cog))
        {
            if (Pair != null) _mediator.Publish(new OpenUserPairPermissions(_pair, StickyWindowType.ClientPermsForPair, false));
        }
        UiSharedService.AttachToolTip("Set your Permissions for " + _pair.UserData.AliasOrUID);

        currentRightSide -= permissionsButtonSize.X + spacingX;
        ImGui.SameLine(currentRightSide);
        if (_uiShared.IconButton(FontAwesomeIcon.Search))
        {
            // if we press the cog, we should modify its appearance, and set that we are drawing for this pair to true
            _mediator.Publish(new OpenUserPairPermissions(_pair, StickyWindowType.PairPerms, false));
        }
        UiSharedService.AttachToolTip("Inspect " + _pair.UserData.AliasOrUID + "'s permissions");

        return currentRightSide;
    }
}
