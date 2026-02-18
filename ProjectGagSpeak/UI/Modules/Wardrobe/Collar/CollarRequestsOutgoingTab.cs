using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using OtterGui.Extensions;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class CollarRequestsOutgoingTab : IFancyTab
{
    private readonly ILogger<CollarRequestsOutgoingTab> _logger;
    private readonly CollarManager _manager;
    private readonly TutorialService _guides;

    private PairCombo _combo;
    private Kinkster? _selectedKinkster;
    private string _desiredWriting;
    public CollarRequestsOutgoingTab(ILogger<CollarRequestsOutgoingTab> log, GagspeakMediator mediator,
        MainConfig config, KinksterManager kinksters, FavoritesConfig favorites, CollarManager manager, 
        TutorialService guides)
    {
        _logger = log;
        _manager = manager;
        _guides = guides;

        _combo = new PairCombo(log, mediator, kinksters, favorites);
        _desiredWriting = string.Empty;
    }

    public string   Label       => "Outgoing Requests";
    public string   Tooltip     => string.Empty;
    public bool     Disabled    => false;


    public void DrawContents(float width)
    {
        DrawRequestCreator(width);

        using var _ = ImRaii.Disabled(_manager.RequestsOutgoing.Count is 0);
        DrawSentRequests(width);
    }

    private void DrawRequestCreator(float width)
    {
        var createorH = CkStyle.HeaderHeight() + CkStyle.GetFrameRowsHeight(5);
        using var _ = CkRaii.FramedChildPaddedW("Creator", width, createorH.AddWinPadY(), 0, GsCol.VibrantPink.Uint(), FancyTabBar.RoundingInner);

        var spacing = ImUtf8.ItemInnerSpacing.X;
        var titleSize = CkGui.CalcFontTextSize("Create Request", Fonts.UidFont);
        var sendWidth = CkGui.IconTextButtonSize(FAI.CloudUploadAlt, "Send");
        var lineSize = titleSize.X + sendWidth + ImUtf8.FrameHeight;
        
        using (ImRaii.Group())
        {

            CkGui.FontText("Create Request", Fonts.UidFont);
            CkGui.Separator(GsCol.VibrantPink.Uint(), lineSize);

            // the Kinkster Selection.
            CkGui.FramedIconText(FAI.UserCircle);
            ImUtf8.SameLineInner();
            var comboW = titleSize.X - ImUtf8.FrameHeight - spacing;
            if (_combo.Draw(_selectedKinkster, comboW, 1.5f))
            {
                _logger.LogInformation($"Selected kinkster: {_combo.Current?.GetNickAliasOrUid() ?? "None"}");
                _selectedKinkster = _combo.Current;
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _logger.LogInformation("Clearing selected kinkster.");
                _selectedKinkster = null;
            }

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.CloudUploadAlt, "Send"))
            {
                // Something here to sent off the request!
            }

            // Desired Writing field.
            CkGui.FramedIconText(FAI.PenFancy);
            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(comboW + spacing + sendWidth);
            ImGui.InputTextWithHint("##DesiredWriting", "Desired Writing...", ref _desiredWriting, 100);
            CkGui.AttachToolTip("The initial writing you want on your collar.");
        }

        // Beside this group draw out the permission areas.
        ImGui.SameLine();

        var permBoxWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X) / 2;
        var permBoxSize = new Vector2(permBoxWidth, CkStyle.GetFrameRowsHeight(5));
        using (CkRaii.HeaderChild("Your Access", permBoxSize, FancyTabBar.RoundingInner, HeaderFlags.AddPaddingToHeight))
        {
            var refVar2 = false;
            var refVar3 = true;
            var refVar4 = false;
            var refVar5 = false;
            ImGui.Checkbox("Toggle Visibility", ref refVar2);
            ImGui.Checkbox("Glamour Dyes", ref refVar3);
            ImGui.Checkbox("Moodle", ref refVar4);
            ImGui.Checkbox("Collar Writing", ref refVar5);
        }
        CkGui.AttachToolTip("This is the access you will have if the request is accepted.");

        ImUtf8.SameLineInner();
        using (CkRaii.HeaderChild("Their Access", permBoxSize, FancyTabBar.RoundingInner, HeaderFlags.AddPaddingToHeight))
        {
            var refVar1 = true;
            var refVar2 = false;
            var refVar3 = true;
            var refVar4 = false;
            var refVar5 = false;
            ImGui.Checkbox("Toggle Visibility", ref refVar2);
            ImGui.Checkbox("Glamour Dyes", ref refVar3);
            ImGui.Checkbox("Moodle", ref refVar4);
            ImGui.Checkbox("Collar Writing", ref refVar5);
            ImGui.Checkbox("Glam/Mod Access", ref refVar1);
        }
        CkGui.AttachToolTip("This is the access the person you are sending the request to will have.");
    }

    private void DrawSentRequests(float width)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(6));
        using var _ = CkRaii.FramedChildPaddedWH("List", ImGui.GetContentRegionAvail(), 0, GsCol.VibrantPink.Uint(), FancyTabBar.RoundingInner);

        using var s = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 8f);

        var bgCol = new Vector4(0.25f, 0.2f, 0.2f, 0.4f).ToUint();
        foreach (var (request, idx) in _manager.RequestsIncoming.WithIndex())
        {
            using (var __ = CkRaii.FramedChildPaddedW($"Req-{idx}", _.InnerRegion.X, CkStyle.TwoRowHeight(), bgCol, 0, FancyTabBar.RoundingInner))
                DrawRequestBox(request, idx, __.InnerRegion);
        }
    }

    private void DrawRequestBox(CollarRequest request, int idx, Vector2 region)
    {
        // Confine all of this into a group, because for some wierd reason, if you don't, then ImGui.SameLine
        // refuses to properly right-align anything if a group is involved within. To fix this, encapsulate
        // everything in a group.
        // Why does this work? At this point ive given up trying to find a reason.
        using var _ = ImRaii.Group();

        var buttonW = CkGui.IconTextButtonSize(FAI.Times, "Cancel");
        var splitW = CkGui.GetSeparatorVWidth();
        var permW = ImGui.GetFrameHeight() * 5 + ImGui.GetStyle().ItemInnerSpacing.X * 4;
        var rightWidth = buttonW + splitW + permW;
        var regionAvail = ImGui.GetContentRegionAvail().X;

        using (ImRaii.Group())
        {
            CkGui.FramedIconText(FAI.UserCircle);
            CkGui.TextFrameAlignedInline($"Sent to: {request.User.AliasOrUID}");
            CkGui.AttachToolTip($"The kinkster you sent this request to.");

            // Time Remaining.
            ImGui.SameLine();
            CkGui.AnimatedHourglass(3000);
            CkGui.ColorTextFrameAlignedInline(request.ExpireTime().ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
            CkGui.AttachToolTip("Time remaining until this requerst expires.");

            //// Desired Writing.
            CkGui.FramedIconText(FAI.PenFancy);
            ImGui.SameLine(0, 0);
            CkGui.TextFrameAligned(request.Writing ?? "<No Writing>");
            CkGui.AttachToolTip("The initial writing you wish to be assigned to this collar.");
        }

        ImGui.SameLine(regionAvail - rightWidth);
        using (ImRaii.Group())
        {
            // Owner Access.
            ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
            ImUtf8.SameLineInner();
            AccessPerm(FAI.Eye, request.OwnerAccess.HasAny(CollarAccess.Visuals), $"You can toggle {request.Target.AliasOrUID}'s Visuals", $"You cannot toggle {request.Target.AliasOrUID}'s Visuals");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.FillDrip, request.OwnerAccess.HasAny(CollarAccess.Dyes), $"You can dye {request.Target.AliasOrUID}'s collar", $"You cannot dye {request.Target.AliasOrUID}'s collar");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.TheaterMasks, request.OwnerAccess.HasAny(CollarAccess.Moodle), $"You can change {request.Target.AliasOrUID}'s collar moodle", $"You cannot change {request.Target.AliasOrUID}'s collar moodle");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.PenFancy, request.OwnerAccess.HasAny(CollarAccess.Writing), $"You can change {request.Target.AliasOrUID}'s collar writing", $"You cannot change {request.Target.AliasOrUID}'s collar writing");

            // Recipient's Access.
            AccessPerm(FAI.Vest, request.TargetAccess.HasAny(CollarAccess.GlamMod), $"{request.Target.AliasOrUID} can change their collar glamour & mod.", $"{request.Target.AliasOrUID} cannot change their collar glamour & mod.");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.Eye, request.TargetAccess.HasAny(CollarAccess.Visuals), $"{request.Target.AliasOrUID} can toggle their visuals", $"{request.Target.AliasOrUID} cannot toggle their visuals.");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.FillDrip, request.TargetAccess.HasAny(CollarAccess.Dyes), $"{request.Target.AliasOrUID} can dye their collar", $"{request.Target.AliasOrUID} cannot dye their collar.");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.TheaterMasks, request.TargetAccess.HasAny(CollarAccess.Moodle), $"{request.Target.AliasOrUID} can change their collar moodle", $"{request.Target.AliasOrUID} cannot change their collar moodle.");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.PenFancy, request.TargetAccess.HasAny(CollarAccess.Writing), $"{request.Target.AliasOrUID} can change their collar writing", $"{request.Target.AliasOrUID} cannot change their collar writing.");
        }

        CkGui.SeparatorV();

        // Draw the cancel button.
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((region.Y - ImGui.GetFrameHeight()) / 2));
        if (CkGui.IconTextButton(FAI.Times, "Cancel"))
        {
            // I dont actually think i have anything for cancelling a request lol.
        }

        void AccessPerm(FAI icon, bool state, string tooltipTrue, string tooltipFalse)
        {
            CkGui.BooleanToColoredIcon(state, false, icon, icon, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
            CkGui.AttachToolTip(state ? tooltipTrue : tooltipFalse);
        }
    }
}
