using CkCommons.DrawSystem;
using CkCommons.DrawSystem.Selector;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using OtterGui.Text;

namespace GagSpeak.DrawSystem;

public sealed class MarionetteDrawer : DynamicDrawer<AliasTrigger>
{
    public MarionetteDrawer(GagspeakMediator mediator, MarionetteDrawSystem ds)
        : base("##GSMarionette", Svc.Logger.Logger, ds, new MarionetteCache(ds))
    { 
    }

    public AliasTrigger? Selected => Selector.SelectedLeaf?.Data;

    protected override void DrawFolderBannerInner(IDynamicFolder<AliasTrigger> folder, Vector2 region, DynamicFlags flags)
        => DrawFolderInner((AliasFolder)folder, region, flags);

    private void DrawFolderInner(AliasFolder folder, Vector2 region, DynamicFlags flags)
    {
        var pos = ImGui.GetCursorPos();
        if (ImGui.InvisibleButton($"{Label}_node_{folder.ID}", region))
            HandleLeftClick(folder, flags);
        HandleDetections(folder, flags);

        // Back to the start, then draw.
        ImGui.SameLine(pos.X);
        CkGui.FramedIconText(folder.IsOpen ? FAI.FolderOpen : FAI.FolderClosed);
        CkGui.ColorTextFrameAlignedInline(folder.Name, folder.NameColor);
        // Total Context.
        CkGui.ColorTextFrameAlignedInline(folder.BracketText, ImGuiColors.DalamudGrey2);
        CkGui.AttachToolTip(folder.BracketTooltip);
    }

    // This override intentionally prevents the inner method from being called so that we can call our own inner method.
    protected override void DrawLeaf(IDynamicLeaf<AliasTrigger> leaf, DynamicFlags flags, bool selected)
    {
        var cursorPos = ImGui.GetCursorPos();
        var size = new Vector2(CkGui.GetWindowContentRegionWidth() - cursorPos.X, ImUtf8.FrameHeight);
        var bgCol = selected ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : 0;
        using (var _ = CkRaii.Child(Label + leaf.Name, size, bgCol, 5f))
            DrawAliasLeaf(leaf, _.InnerRegion, flags);
    }

    private void DrawAliasLeaf(IDynamicLeaf<AliasTrigger> leaf, Vector2 region, DynamicFlags flags)
    {
        ImUtf8.SameLineInner();
        var posX = ImGui.GetCursorPosX();
        if (ImGui.InvisibleButton($"{leaf.FullPath}-name-area", region))
            HandleLeftClick(leaf, flags);
        HandleDetections(leaf, flags);
        // Then return to the start position and draw out the text.
        ImGui.SameLine(posX);
        // Display the alias label here
        CkGui.TextFrameAligned(leaf.Data.Label);
        CkGui.AttachToolTip("Display name for the Alias. Used for UI Only.");
        // Inline, draw the detection in coded text.
        ImUtf8.SameLineInner();
        ImGui.AlignTextToFramePadding();
        using (ImRaii.Group())
        {
            CkGui.ColorText("\"", ImGuiColors.DalamudGrey2);
            ImGui.SameLine(0, 0);
            CkGui.ColorText(leaf.Data.InputCommand, ImGuiColors.DalamudGrey);
            ImGui.SameLine(0, 0);
            CkGui.ColorText("\"", ImGuiColors.DalamudGrey2);
        }
        CkGui.AttachToolTip("The scanned text that, when said after any trigger phrase, is reacted to.");
    }
}

