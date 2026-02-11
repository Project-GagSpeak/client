using CkCommons;
using CkCommons.DrawSystem;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.DrawSystem;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagspeakAPI.Data;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class MarionettesTab : IFancyTab
{
    private readonly ILogger<MarionettesTab> _logger;
    private readonly MarionetteDrawer _drawer;
    private readonly MarionetteDrawSystem _dds;
    private readonly PuppeteerManager _manager;
    private readonly TutorialService _guides;

    private AliasTrigger? _selected => _drawer.Selected;

    public MarionettesTab(ILogger<MarionettesTab> logger, GagspeakMediator mediator,
        MarionetteDrawer drawer, MarionetteDrawSystem dds, PuppeteerManager manager, 
        TutorialService guides)
    {
        _logger = logger;
        _drawer = drawer;
        _dds = dds;
        _manager = manager;
        _guides = guides;
    }

    public string   Label       => "Marionettes";
    public string   Tooltip     => "See what control kinksters surrendered to you, and their shared aliases.";
    public bool     Disabled    => false;

    // should be very similar to drawing out the list of items, except this will have a unique flavor to it.
    public void DrawContents(float width)
    {
        using var _ = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, ImUtf8.FramePadding - new Vector2(0, 1));

        var halfW = width / 2 - ImUtf8.ItemInnerSpacing.X;
        var rounding = FancyTabBar.BarHeight * .4f;

        using (ImRaii.Group())
        {
            DrawMarionetteCombo(halfW, rounding);
            DrawMarionetteAliases(halfW, rounding);
        }
        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            DrawMarionettesPerms(CkStyle.GetFrameRowsHeight(5), rounding);
            DrawAliasPreview(rounding);
        }
    }

    private void DrawMarionetteCombo(float width, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedW("Combo", width, ImUtf8.FrameHeight, 0, GsCol.VibrantPink.Uint(), rounding);
        if (_dds.DrawMarionetteCombo(_.InnerRegion.X))
        {
            _logger.LogInformation("We selected a Marionette!");
        }
    }

    private void DrawMarionetteAliases(float leftWidth, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedWH("marionette_aliases", new Vector2(leftWidth, ImGui.GetContentRegionAvail().Y), 0, GsCol.VibrantPink.Uint(), rounding);

        _drawer.DrawFilterRow(_.InnerRegion.X, 40);
        _drawer.DrawContents(flags: DynamicFlags.SelectableLeaves);
    }

    private void DrawMarionettesPerms(float height, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedW("Perms", ImGui.GetContentRegionAvail().X, height, 0, GsCol.VibrantPink.Uint(), rounding);
        // Only ever one view, that would be here.
        if (_dds.SelectedMarionette is not { } marionette)
        {
            CkGui.ColorTextCentered("Select a Marionette to view permissions.", ImGuiColors.DalamudRed);
            return;
        }
        
        ImGui.Text($"WAH! {marionette.GetDisplayName()} exists!");
        //// The non-editor variant of the topright segment.
        //using (ImRaii.Group())
        //{
        //    CkGui.ColorTextFrameAligned(selected.InPool ? "In Pool" : "Not In Pool", selected.InPool ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
        //    ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImUtf8.FrameHeight);
        //    if (CkGui.IconButton(FAI.Edit, inPopup: true))
        //        _manager.StartEditing(selected);

        //    // Label field.
        //    CkGui.TextFrameAligned(selected.Label);
        //}

        //// Item Label.
        //if (selected is CursedGagItem item)
        //    ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, iconSize);
        //else if (selected is CursedRestrictionItem item2)
        //    ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Restrained].Handle, iconSize);
        //else
        //    ImGui.Dummy(iconSize);

        //CkGui.TextFrameAlignedInline(selected.RefLabel);

        //// Precedence.
        //CkGui.FramedIconText(FAI.SortAmountUp);
        //CkGui.TextFrameAlignedInline(selected.Precedence.ToName());

        //// Trait Application.
        //CkGui.BooleanToColoredIcon(selected.ApplyTraits, false);
        //CkGui.TextFrameAlignedInline(selected.ApplyTraits ? "Applies traits" : "Ignores traits");
    }

    private void DrawAliasPreview(float rounding)
    {
        // This will cover the remaining content region
        using var _ = CkRaii.FramedChildPaddedWH("Preview", ImGui.GetContentRegionAvail(), 0, GsCol.VibrantPink.Uint(), rounding);
        if (_selected is not { } alias)
        {
            CkGui.ColorTextCentered("Select an Alias to preview it here!", ImGuiColors.DalamudRed);
            return;
        }

        ImGui.Text("Drawing Alias Preview here!");
    }
}
