using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Handlers;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.UserPair;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.UI.Components;

/// <summary>
/// Class handling the draw function for a singular user pair that the client has. (one row)
/// </summary>
public class DrawUserPair
{
    protected readonly MainHub _hub;
    protected readonly IdDisplayHandler _displayHandler;
    protected readonly GagspeakMediator _mediator;
    protected Pair _pair;
    private readonly string _id;
    private readonly CosmeticService _cosmetics;

    private Dictionary<byte, bool> IsHovered = new();
    public DrawUserPair(ILogger<DrawUserPair> logger, string id, Pair entry, MainHub hub,
        IdDisplayHandler uIDDisplayHandler, GagspeakMediator mediator, CosmeticService cosmetics)
    {
        _id = id;
        _pair = entry;
        _hub = hub;
        _displayHandler = uIDDisplayHandler;
        _mediator = mediator;
        _cosmetics = cosmetics;
    }

    public Pair Pair => _pair;

    public bool DrawPairedClient(byte ident, bool supporterIcon = true, bool icon = true, bool iconTT = true, bool displayToggles = true, 
        bool displayNameTT = true, bool showHovered = true, bool showRightButtons = true)
    {
        // if no key exist for the dictionary, add it with default value of false.
        if (!IsHovered.ContainsKey(ident))
        {
            IsHovered.Add(ident, false);
        }

        var selected = false;
        // get the current cursor pos
        var cursorPos = ImGui.GetCursorPosX();
        using var id = ImRaii.PushId(GetType() + _id);
        using (ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), showHovered && IsHovered[ident]))
        {
            using (ImRaii.Child(GetType() + _id, new Vector2(CkGui.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImGui.GetFrameHeight())))
            {
                ImUtf8.SameLineInner();
                if (icon)
                {
                    DrawLeftSide(iconTT);
                }
                ImGui.SameLine();
                var posX = ImGui.GetCursorPosX();

                var rightSide = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - CkGui.IconButtonSize(FAI.EllipsisV).X;

                if (showRightButtons)
                {
                    rightSide = DrawRightSide();
                }

                selected = DrawName(posX, rightSide, displayToggles, displayNameTT);
            }

            IsHovered[ident] = ImGui.IsItemHovered();
        }
        // if they were a supporter, go back to the start and draw the image.
        if (supporterIcon && _pair.UserData.Tier is not CkSupporterTier.NoRole)
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
            ImGui.SetCursorPosX(cursorPos - CkGui.IconSize(FAI.EllipsisV).X - ImGui.GetStyle().ItemSpacing.X);
            ImGui.Image(wrap.ImGuiHandle, new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight()));
            CkGui.AttachToolTip(Image.Tooltip);
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
            CkGui.IconText(FAI.User);
            userPairText = _pair.UserData.AliasOrUID + " is offline";
        }
        else if (_pair.IsVisible)
        {
            CkGui.IconText(FAI.Eye, ImGuiColors.ParsedGreen);
            userPairText = _pair.UserData.AliasOrUID + " is visible: " + _pair.PlayerName + Environment.NewLine + "Click to target this player";
            if (ImGui.IsItemClicked())
            {
                _mediator.Publish(new TargetPairMessage(_pair));
            }
        }
        else
        {
            using var _ = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
            CkGui.IconText(FAI.User);
            userPairText = _pair.UserData.AliasOrUID + " is online";
        }
        if (showToolTip)
            CkGui.AttachToolTip(userPairText);

        ImGui.SameLine();
    }

    private bool DrawName(float leftSide, float rightSide, bool canTogglePairTextDisplay, bool displayNameTT)
    {
        return _displayHandler.DrawPairText(_id, _pair, leftSide, () => rightSide - leftSide, canTogglePairTextDisplay, displayNameTT);
    }

    private float DrawRightSide()
    {
        var permissionsButtonSize = CkGui.IconButtonSize(FAI.Cog);
        var barButtonSize = CkGui.IconButtonSize(FAI.EllipsisV);
        var spacingX = ImGui.GetStyle().ItemSpacing.X / 2;
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var currentRightSide = windowEndX - barButtonSize.X;

        ImGui.SameLine(currentRightSide);
        ImGui.AlignTextToFramePadding();
        if (CkGui.IconButton(FAI.EllipsisV))
        {
            // open the permission setting window
            _mediator.Publish(new OpenUserPairPermissions(_pair, StickyWindowType.PairActionFunctions, false));
        }

        currentRightSide -= permissionsButtonSize.X + spacingX;
        ImGui.SameLine(currentRightSide);
        if (CkGui.IconButton(FAI.Cog))
        {
            if (Pair != null) _mediator.Publish(new OpenUserPairPermissions(_pair, StickyWindowType.ClientPermsForPair, false));
        }
        CkGui.AttachToolTip("Set your Permissions for " + _pair.UserData.AliasOrUID);

        currentRightSide -= permissionsButtonSize.X + spacingX;
        ImGui.SameLine(currentRightSide);
        if (CkGui.IconButton(FAI.Search))
        {
            // if we press the cog, we should modify its appearance, and set that we are drawing for this pair to true
            _mediator.Publish(new OpenUserPairPermissions(_pair, StickyWindowType.PairPerms, false));
        }
        CkGui.AttachToolTip("Inspect " + _pair.UserData.AliasOrUID + "'s permissions");

        return currentRightSide;
    }
}
