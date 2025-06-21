using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.FileSystems;
using GagSpeak.Kinksters;
using GagSpeak.State;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Wardrobe;
public partial class RestrictionsPanel : DisposableMediatorSubscriberBase
{
    private readonly RestrictionFileSelector _selector;
    private readonly ActiveItemsDrawer _activeItemDrawer;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly AttributeDrawer _attributeDrawer;
    private readonly RestrictionManager _manager;
    private readonly CosmeticService _textures;
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
        PairManager pairs,
        CosmeticService textures,
        TutorialService guides) : base(logger, mediator)
    {
        _selector = selector;
        _attributeDrawer = traitsDrawer;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _activeItemDrawer = activeItemDrawer;
        _manager = manager;
        _textures = textures;
        _guides = guides;

        Mediator.Subscribe<ThumbnailImageSelected>(this, (msg) =>
        {
            if (msg.MetaData.Kind is ImageDataType.Restrictions)
            {
                if (manager.Storage.TryGetRestriction(msg.MetaData.SourceId, out var match))
                {
                    Logger.LogDebug($"Thumbnail updated for {match.Label} to {msg.Name}");
                    manager.UpdateThumbnail(match, msg.Name);
                }
            }
            else if (msg.MetaData.Kind is ImageDataType.Blindfolds && manager.ItemInEditor is BlindfoldRestriction blindfold)
            {
                Logger.LogDebug($"Thumbnail updated for {blindfold.Label} to {blindfold.Properties.OverlayPath}");
                blindfold.Properties.OverlayPath = msg.Name;
            }
            else if (msg.MetaData.Kind is ImageDataType.Hypnosis && manager.ItemInEditor is HypnoticRestriction hypnoItem)
            {
                Logger.LogDebug($"Thumbnail updated for {hypnoItem.Label} to {hypnoItem.Properties.OverlayPath}");
                hypnoItem.Properties.OverlayPath = msg.Name;
            }
        });
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
        var height = ImGui.GetFrameHeight() * 2 + MoodleDrawer.IconSize.Y + ImGui.GetStyle().ItemSpacing.Y * 2;
        var region = new Vector2(drawRegion.Size.X, height.AddWinPadY());
        var tooltipAct = "Double Click me to begin editing!";

        using var inner = CkRaii.LabelChildAction("SelItem", region, DrawLabel, ImGui.GetFrameHeight(), BeginEdits, tt: tooltipAct, dFlag: ImDrawFlags.RoundCornersRight);

        var pos = ImGui.GetItemRectMin();
        var imgSize = new Vector2(inner.InnerRegion.Y);
        var imgDrawPos = pos with { X = pos.X + inner.InnerRegion.X - imgSize.X };
        // Draw the left items.
        if (_selector.Selected is not null) 
            DrawSelectedInner(imgSize.X);
        
        // Draw the right image item.
        ImGui.GetWindowDrawList().AddRectFilled(imgDrawPos, imgDrawPos + imgSize, CkColor.FancyHeaderContrast.Uint(), rounding);
        ImGui.SetCursorScreenPos(imgDrawPos);
        if (_selector.Selected is not null)
        {
            _activeItemDrawer.DrawRestrictionImage(_selector.Selected!, imgSize.Y, rounding, true);
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                var metaData = new ImageMetadataGS(ImageDataType.Restrictions, new Vector2(120, 120f), _selector.Selected!.Identifier);
                Mediator.Publish(new OpenThumbnailBrowser(metaData));
            }
            CkGui.AttachToolTip("The Thumbnail for this item.--SEP--Double Click to change the image.");
        }

        void DrawLabel()
        {
            using var _ = ImRaii.Child("LabelChild", new Vector2(region.X * .6f, ImGui.GetFrameHeight()));
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().WindowPadding.X);
            ImUtf8.TextFrameAligned(_selector.Selected?.Label ?? "No Item Selected!");
            ImGui.SameLine(region.WithoutWinPadding().X * .6f - ImGui.GetFrameHeightWithSpacing());
            var imgPos = ImGui.GetCursorScreenPos();

            // Draw the type of restriction item as an image path here.
            if (_selector.Selected is not null)
            {
                (var image, var tooltip) = _selector.Selected?.Type switch
                {
                    RestrictionType.Gag => (_textures.CoreTextures[CoreTexture.Gagged], "This is a Gag Restriction!"),
                    RestrictionType.Collar => (_textures.CoreTextures[CoreTexture.Collar], "This is a Collar Restriction!"),
                    RestrictionType.Blindfold => (_textures.CoreTextures[CoreTexture.Blindfolded], "This is a Blindfold Restriction!"),
                    _ => (_textures.CoreTextures[CoreTexture.Restrained], "This is a generic Restriction.")
                };
                ImGui.GetWindowDrawList().AddDalamudImage(image, imgPos, new Vector2(ImGui.GetFrameHeight()), tooltip);
            }
        }

        void BeginEdits() { if (_selector.Selected is not null) _manager.StartEditing(_selector.Selected!); }
    }

    private void DrawSelectedInner(float rightOffset)
    {
        using var innerGroup = ImRaii.Group();
        // Next row we need to draw the Glamour Icon, Mod Icon, and hardcore Traits.
        var hasGlamour = ItemService.NothingItem(_selector.Selected!.Glamour.Slot).Id != _selector.Selected!.Glamour.GameItem.Id;
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
        using var child = CkRaii.Child("ActiveItems", region, WFlags.NoScrollbar | WFlags.AlwaysUseWindowPadding);

        if (_manager.ServerRestrictionData is not { } activeRestrictionSlots)
            return;

        // get the current content height.
        var height = ImGui.GetContentRegionAvail().Y;
        // calculate the Y spacing for the items.
        var groupH = ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;
        var groupSpacing = (height - 5 * groupH) / 7;

        foreach (var (data, index) in activeRestrictionSlots.Restrictions.WithIndex())
        {
            // Spacing.
            if(index > 0) ImGui.SetCursorPosY(ImGui.GetCursorPosY() + groupSpacing);

            // Draw the framed Item.
            if (data.Identifier == Guid.Empty)
                _activeItemDrawer.ApplyItemGroup(index, data);
            else
            {
                // Lock Display. For here we want the thumbnail we provide for the restriction item, so find it.
                if (_manager.ActiveItems.TryGetValue(index, out var item) && item.Identifier != Guid.Empty)
                {
                    if (data.IsLocked())
                        _activeItemDrawer.UnlockItemGroup(index, data, item);
                    else
                        _activeItemDrawer.LockItemGroup(index, data, item);
                }
            }
        }
    }
}
