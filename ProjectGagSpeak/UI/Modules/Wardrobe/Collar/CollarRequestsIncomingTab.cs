using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using OtterGui;
using OtterGui.Extensions;
using OtterGui.Text;
using TerraFX.Interop.Windows;

namespace GagSpeak.Gui.Wardrobe;

public class CollarRequestsIncomingTab : IFancyTab
{
    private readonly CollarManager _manager;
    private readonly TutorialService _guides;
    public CollarRequestsIncomingTab(CollarManager manager, TutorialService guides)
    {
        _manager = manager;
        _guides = guides;
    }

    public string   Label       => "Incoming Requests";
    public string   Tooltip     => string.Empty;
    public bool     Disabled    => _manager.RequestsIncoming.Count is 0;

    public void DrawContents(float width)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(6));
        using var _ = CkRaii.FramedChildPaddedWH("List", ImGui.GetContentRegionAvail(), 0, CkColor.VibrantPink.Uint(), FancyTabBar.RoundingInner);

        using var s = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 8f);

        // need to draw out the list and handle each of them accordingly.
        // We should also return if any had operations performed on them.
        var bgCol = new Vector4(0.25f, 0.2f, 0.2f, 0.4f).ToUint();
        foreach (var (request, idx) in _manager.RequestsIncoming.WithIndex())
        {
            using (var __ = CkRaii.FramedChildPaddedW($"Req-{idx}", _.InnerRegion.X, CkStyle.TwoRowHeight(), bgCol, 0, FancyTabBar.RoundingInner))
                DrawRequestBox(request, idx, __.InnerRegion.X);
        }
    }

    private void DrawRequestBox(CollarRequest request, int idx, float width)
    {
        // Confine all of this into a group, because for some wierd reason, if you don't, then ImGui.SameLine
        // refuses to properly right-align anything if a group is involved within. To fix this, encapsulate
        // everything in a group.
        // Why does this work? At this point ive given up trying to find a reason.
        using var _ = ImRaii.Group();

        var buttonW = CkGui.IconTextButtonSize(FAI.Check, "Accept");
        var splitW = CkGui.GetSeparatorVWidth();
        var permW = ImGui.GetFrameHeight() * 5 + ImGui.GetStyle().ItemInnerSpacing.X * 4;
        var rightWidth = buttonW + splitW + permW;
        var regionAvail = ImGui.GetContentRegionAvail().X;

        using (ImRaii.Group())
        {
            CkGui.FramedIconText(FAI.UserCircle);
            CkGui.TextFrameAlignedInline($"From: {request.User.AliasOrUID}");
            CkGui.AttachToolTip($"The kinkster that sent this request");

            // Time Remaining.
            ImGui.SameLine();
            CkGui.AnimatedHourglass(3000);
            CkGui.ColorTextFrameAlignedInline(request.ExpireTime().ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
            CkGui.AttachToolTip("Time remaining to accept this request.");

            //// Desired Writing.
            CkGui.FramedIconText(FAI.PenFancy);
            ImGui.SameLine(0, 0);
            CkGui.TextFrameAligned(request.Writing ?? "<No Writing>");
            CkGui.AttachToolTip("The writing the requester wants on your collar.");
        }

        ImGui.SameLine(regionAvail - rightWidth);
        using (ImRaii.Group())
        {
            // Owner Access.
            ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
            ImUtf8.SameLineInner();
            AccessPerm(FAI.Eye, request.OwnerAccess.HasAny(CollarAccess.Visuals), "Owners can toggle visuals", "Owners cannot toggle visuals.");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.FillDrip, request.OwnerAccess.HasAny(CollarAccess.Dyes), "Owners can dye your collar", "Owners cannot dye your collar.");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.TheaterMasks, request.OwnerAccess.HasAny(CollarAccess.Moodle), "Owners can change the collar moodle", "Owners cannot change the collar moodle.");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.PenFancy, request.OwnerAccess.HasAny(CollarAccess.Writing), "Owners can change the collar writing", "Owners cannot change the collar writing.");

            // Own Access.
            AccessPerm(FAI.Vest, request.TargetAccess.HasAny(CollarAccess.GlamMod), "You can change your collar glamour & mod.", "You cannot change your collar glamour & mod.");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.Eye, request.TargetAccess.HasAny(CollarAccess.Visuals), "You can toggle visuals", "You cannot toggle visuals.");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.FillDrip, request.TargetAccess.HasAny(CollarAccess.Dyes), "You can dye your collar", "You cannot dye your collar.");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.TheaterMasks, request.TargetAccess.HasAny(CollarAccess.Moodle), "You can change your collar moodle", "You cannot change your collar moodle.");
            ImUtf8.SameLineInner();
            AccessPerm(FAI.PenFancy, request.TargetAccess.HasAny(CollarAccess.Writing), "You can change your collar writing", "You cannot change your collar writing.");
        }

        CkGui.SeparatorV();

        // Access and Reject Buttons.
        using (ImRaii.Group())
        {
            if (CkGui.IconTextButton(FAI.Check, "Accept", buttonW))
            {
                // I dont actually think i have anything for accepting a request lol.
            }
            CkGui.AttachToolTip("Accept this collar request.");

            if (CkGui.IconTextButton(FAI.Times, "Reject", buttonW))
            {
                // I dont actually think i have anything for rejecting a request lol.
            }
            CkGui.AttachToolTip("Reject this collar request.");
        }

        void AccessPerm(FAI icon, bool state, string tooltipTrue, string tooltipFalse)
        {
            CkGui.BooleanToColoredIcon(state, false, icon, icon, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
            CkGui.AttachToolTip(state ? tooltipTrue : tooltipFalse);
        }      
    }
}
