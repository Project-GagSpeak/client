using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using Dalamud.Bindings.ImGui;
using OtterGui.Extensions;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;
public partial class RestrictionsPanel : DisposableMediatorSubscriberBase
{
    private readonly RestrictionFileSelector _selector;
    private readonly ActiveItemsDrawer _activeItemDrawer;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly AttributeDrawer _attributeDrawer;
    private readonly RestrictionManager _manager;
    private readonly UiThumbnailService _thumbnails;
    private readonly TutorialService _guides;
    public bool IsEditing => _manager.ItemInEditor != null;
    public RestrictionsPanel(
        ILogger<RestrictionsPanel> logger,
        GagspeakMediator mediator,
        RestrictionFileSelector selector,
        ActiveItemsDrawer activeItemDrawer,
        EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        AttributeDrawer traitsDrawer,
        RestrictionManager manager,
        HypnoEffectManager effectPresets,
        KinksterManager pairs,
        UiThumbnailService thumbnails,
        TutorialService guides) : base(logger, mediator)
    {
        _selector = selector;
        _thumbnails = thumbnails;
        _attributeDrawer = traitsDrawer;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _activeItemDrawer = activeItemDrawer;
        _manager = manager;
        _guides = guides;

        _hypnoEditor = new HypnoEffectEditor("RestrictionEditor", effectPresets);

        Mediator.Subscribe<ThumbnailImageSelected>(this, (msg) =>
        {
            if (msg.Folder is ImageDataType.Restrictions)
            {
                if (manager.Storage.TryGetRestriction(msg.SourceId, out var match))
                {
                    Logger.LogDebug($"Thumbnail updated for {match.Label} to {msg.FileName}");
                    manager.UpdateThumbnail(match, msg.FileName);
                }
            }
            else if (msg.Folder is ImageDataType.Blindfolds && manager.ItemInEditor is BlindfoldRestriction blindfold)
            {
                Logger.LogDebug($"Thumbnail updated for {blindfold.Label} to {blindfold.Properties.OverlayPath}");
                blindfold.Properties.OverlayPath = msg.FileName;
            }
            else if (msg.Folder is ImageDataType.Hypnosis && manager.ItemInEditor is HypnoticRestriction hypnoItem)
            {
                Logger.LogDebug($"Thumbnail updated for {hypnoItem.Label} to {hypnoItem.Properties.OverlayPath}");
                hypnoItem.Properties.OverlayPath = msg.FileName;
            }
        });
    }

    private HypnoEffectEditor _hypnoEditor;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _hypnoEditor.Dispose();
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, WardrobeTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("RestrictionsTopLeft", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("RestrictionsBottomLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("RestrictionsTopRight", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        // Draw the selected Item
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawSelectedItemInfo(drawRegions.BotRight, curveSize);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));

        // Shift down and draw the Active items
        var verticalShift = new Vector2(0, ImGui.GetItemRectSize().Y + ImGui.GetStyle().WindowPadding.Y * 3);
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos + verticalShift);
        DrawActiveItemInfo(drawRegions.BotRight.Size - verticalShift);
    }

    public void DrawEditorContents(CkHeader.QuadDrawRegions drawRegions, float curveSize)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("RestrictionsTopLeft", drawRegions.TopLeft.Size))
            DrawEditorHeaderLeft(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("RestrictionsBottomLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            DrawEditorLeft(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("RestrictionsTopRight", drawRegions.TopRight.Size))
            DrawEditorHeaderRight(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("RestrictionsBottomRight", drawRegions.BotRight.Size))
            DrawEditorRight(drawRegions.BotRight.SizeX);
    }


    private void DrawSelectedItemInfo(CkHeader.DrawRegion drawRegion, float rounding)
    {
        var wdl = ImGui.GetWindowDrawList();
        var height = ImGui.GetFrameHeightWithSpacing() + MoodleDrawer.IconSize.Y;
        var region = new Vector2(drawRegion.Size.X, height);
        var nothingSelected = _selector.Selected is null;
        var isActive = _manager.ActiveItemsAll.ContainsKey(_selector.Selected?.Identifier ?? Guid.Empty);
        var tooltip = nothingSelected ? "No item selected!" : isActive ? "Cannot edit Active Item!" : "Double Click to begin editing!";

        using var c = CkRaii.ChildLabelCustomButton("SelItem", region, ImGui.GetFrameHeight(), LabelButton, BeginEdits, tooltip, DFlags.RoundCornersRight, LabelFlags.AddPaddingToHeight);

        var pos = ImGui.GetItemRectMin();
        var imgSize = new Vector2(c.InnerRegion.Y);
        var imgDrawPos = pos with { X = pos.X + c.InnerRegion.X - imgSize.X };
        // Draw the left items.
        if (_selector.Selected is not null) 
            DrawSelectedInner(imgSize.X);
        
        // Draw the right image item.
        ImGui.GetWindowDrawList().AddRectFilled(imgDrawPos, imgDrawPos + imgSize, CkColor.FancyHeaderContrast.Uint(), rounding);
        ImGui.SetCursorScreenPos(imgDrawPos);
        if (_selector.Selected is not null)
        {
            _activeItemDrawer.DrawRestrictionImage(_selector.Selected!, imgSize.Y, rounding, false);
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                _thumbnails.SetThumbnailSource(_selector.Selected!.Identifier, new Vector2(120), ImageDataType.Restrictions);
            CkGui.AttachToolTip("The Thumbnail for this item.--SEP--Double Click to change the image.");
        }

        void LabelButton()
        {
            using (var c = CkRaii.Child("##RestrictionSelectorLabel", new Vector2(region.X * .6f, ImGui.GetFrameHeight())))
            {
                var imgSize = new Vector2(c.InnerRegion.Y);
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().WindowPadding.X);
                ImUtf8.TextFrameAligned(_selector.Selected?.Label ?? "No Item Selected!");
                ImGui.SameLine(c.InnerRegion.X - imgSize.X * 1.5f);
                if (_selector.Selected is not null)
                {
                    (var image, var tooltip) = _selector.Selected?.Type switch
                    {
                        RestrictionType.Hypnotic => (CosmeticService.CoreTextures.Cache[CoreTexture.HypnoSpiral], "This is a Hypnotic Restriction!"),
                        RestrictionType.Blindfold => (CosmeticService.CoreTextures.Cache[CoreTexture.Blindfolded], "This is a Blindfold Restriction!"),
                        _ => (CosmeticService.CoreTextures.Cache[CoreTexture.Restrained], "This is a generic Restriction.")
                    };
                    ImGui.GetWindowDrawList().AddDalamudImage(image, ImGui.GetCursorScreenPos(), imgSize, tooltip);
                }
            }
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is not ImGuiMouseButton.Left || nothingSelected)
                return;
            
            if(_selector.Selected is not null)
                _manager.StartEditing(_selector.Selected);
        }
    }

    private void DrawSelectedInner(float rightOffset)
    {
        using var innerGroup = ImRaii.Group();
        // Next row we need to draw the Glamour Icon, Mod Icon, and hardcore Traits.
        var hasGlamour = ItemSvc.NothingItem(_selector.Selected!.Glamour.Slot).Id != _selector.Selected!.Glamour.GameItem.Id;
        CkGui.FramedIconText(FAI.Vest);
        CkGui.AttachToolTip(hasGlamour
            ? $"A --COL--{_selector.Selected!.Glamour.GameItem.Name}--COL-- is attached to the --COL--{_selector.Selected!.Label}--COL--."
            : $"There is no Glamour Item attached to the {_selector.Selected!.Label}.", color: ImGuiColors.ParsedGold);

        ImUtf8.SameLineInner();
        var hasMod = !(_selector.Selected!.Mod.Label.IsNullOrEmpty());
        CkGui.FramedIconText(FAI.FileDownload);
        CkGui.AttachToolTip(hasMod
            ? "Using Preset for Mod: " + _selector.Selected!.Mod.Label
            : "This Restriction Item has no associated Mod Preset.");

        ImUtf8.SameLineInner();
        _attributeDrawer.DrawTraitPreview(_selector.Selected!.Traits);

        _moodleDrawer.ShowStatusIcons(_selector.Selected!.Moodle, ImGui.GetContentRegionAvail().X);
    }

    private void DrawActiveItemInfo(Vector2 region)
    {
        using var child = CkRaii.Child("ActiveItems", region, wFlags: WFlags.NoScrollbar | WFlags.AlwaysUseWindowPadding);

        if (_manager.ServerRestrictionData is not { } activeSlots)
            return;

        var height = ImGui.GetContentRegionAvail().Y;
        var groupH = ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;
        var groupSpacing = (height - 5 * groupH) / 7;

        foreach (var (data, index) in activeSlots.Restrictions.WithIndex())
        {
            // Spacing.
            if(index > 0) ImGui.SetCursorPosY(ImGui.GetCursorPosY() + groupSpacing);

            // if no item is selected, display the unique 'Applier' group.
            if (data.Identifier == Guid.Empty)
            {
                _activeItemDrawer.ApplyItemGroup(index, data);
                continue;
            }

            // Otherwise, if the item is sucessfully applied, display the locked states, based on what is active.
            if (_manager.ActiveItems.TryGetValue(index, out var item))
            {
                // If the padlock is currently locked, show the 'Unlocking' group.
                if (data.IsLocked())
                    _activeItemDrawer.UnlockItemGroup(index, data, item);
                // Otherwise, show the 'Locking' group. Locking group can still change applied items.
                else
                    _activeItemDrawer.LockItemGroup(index, data, item);
            }
        }
    }
}
