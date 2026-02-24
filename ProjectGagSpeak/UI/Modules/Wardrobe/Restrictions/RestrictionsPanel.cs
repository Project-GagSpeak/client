using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
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
    private readonly SelfBondageService _selfBondage;

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
        SelfBondageService selfBondage,
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
        _selfBondage = selfBondage;
        _guides = guides;

        _hypnoEditor = new HypnoEffectEditor("RestrictionEditor", effectPresets, guides);

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
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.Searching, WardrobeUI.LastPos, WardrobeUI.LastSize);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("RestrictionsBottomLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.RestrictionList, WardrobeUI.LastPos, WardrobeUI.LastSize,
            () => _selector.SelectByValue(_selector.TutorialHypnoRestriction));

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("RestrictionsTopRight", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        // Draw the selected Item
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Group())
        {
            DrawSelectedItemInfo(drawRegions.BotRight, curveSize);
            var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
            var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
            ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
        }

        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.SelectedRestriction, WardrobeUI.LastPos, WardrobeUI.LastSize);

        // Shift down and draw the Active items
        var verticalShift = new Vector2(0, ImGui.GetItemRectSize().Y + ImGui.GetStyle().WindowPadding.Y * 3);
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos + verticalShift);
        DrawActiveItemInfo(drawRegions.BotRight.Size - verticalShift);
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.Applying, WardrobeUI.LastPos, WardrobeUI.LastSize);
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.NoFreeSlots, WardrobeUI.LastPos, WardrobeUI.LastSize, () =>
            _guides.JumpToStep(TutorialType.Restrictions, StepsRestrictions.Removing));
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
        var notSelected = _selector.Selected is null;
        var isActive = !notSelected && _manager.IsItemApplied(_selector.Selected!.Identifier);
        var tooltip = notSelected ? "No item selected!" : isActive ? "Item is Active!" : "Double Click to begin editing!";

        using var c = CkRaii.ChildLabelCustomButton("SelItem", region, ImGui.GetFrameHeight(), LabelButton, BeginEdits, tooltip,
            DFlags.RoundCornersRight, LabelFlags.AddPaddingToHeight);

        var pos = ImGui.GetItemRectMin();
        var imgSize = new Vector2(c.InnerRegion.Y);
        var imgDrawPos = pos with { X = pos.X + c.InnerRegion.X - imgSize.X };
        // Draw the left items.
        if (_selector.Selected is not null)
            DrawSelectedInner(imgSize.X, isActive);

        // Draw the right image item.
        ImGui.GetWindowDrawList().AddRectFilled(imgDrawPos, imgDrawPos + imgSize, CkCol.CurvedHeaderFade.Uint(), rounding);
        ImGui.SetCursorScreenPos(imgDrawPos);
        if (_selector.Selected is not null)
        {
            _activeItemDrawer.DrawRestrictionImage(_selector.Selected!, imgSize.Y, rounding, false);
            if (!isActive && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                _thumbnails.SetThumbnailSource(_selector.Selected!.Identifier, new Vector2(120), ImageDataType.Restrictions);
            CkGui.AttachToolTip("The Thumbnail for this item.--SEP--Double Click to change the image.");
            _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.SelectingThumbnails, WardrobeUI.LastPos, WardrobeUI.LastSize);
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

            _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.EnteringEditor, WardrobeUI.LastPos, WardrobeUI.LastSize,
                () => _manager.StartEditing(_selector.Selected!));
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is ImGuiMouseButton.Left && !notSelected && !isActive)
                _manager.StartEditing(_selector.Selected!);
        }
    }

    private void DrawSelectedInner(float rightOffset, bool isActive)
    {
        using var innerGroup = ImRaii.Group();

        using (CkRaii.Group(CkCol.CurvedHeaderFade.Uint()))
        {
            CkGui.BooleanToColoredIcon(_selector.Selected!.IsEnabled, false);
            CkGui.TextFrameAlignedInline($"Visuals  ");
        }

        if (!isActive && ImGui.IsItemHovered() && ImGui.IsItemClicked())
            _manager.ToggleVisibility(_selector.Selected!.Identifier);
        CkGui.AttachToolTip($"Visuals {(_selector.Selected!.IsEnabled ? "will" : "will not")} be applied.");

        // Next row we need to draw the Glamour Icon, Mod Icon, and hardcore Traits.
        if (ItemSvc.NothingItem(_selector.Selected!.Glamour.Slot).Id != _selector.Selected!.Glamour.GameItem.Id)
        {
            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.Vest);
            CkGui.AttachToolTip($"A --COL--{_selector.Selected!.Glamour.GameItem.Name}--COL-- is attached to the " +
                $"--COL--{_selector.Selected!.Label}--COL--.", color: ImGuiColors.ParsedGold);
        }

        if (_selector.Selected!.Mod.HasData)
        {
            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.FileDownload);
            CkGui.AttachToolTip($"Mod Preset ({_selector.Selected.Mod.Label}) is applied." +
                $"--SEP--Source Mod: {_selector.Selected!.Mod.Container.ModName}");
        }

        if (_selector.Selected!.Traits > 0)
        {
            ImUtf8.SameLineInner();
            _attributeDrawer.DrawTraitPreview(_selector.Selected!.Traits);
        }

        _moodleDrawer.ShowStatusIcons(_selector.Selected!.Moodle, ImGui.GetContentRegionAvail().X);
    }

    // tutorial item storage, so we don't have to reinitialize data every frame.
    private (int, ActiveRestriction?) _tActiveRestriction = (-1, null);

    private void DrawActiveItemInfo(Vector2 region)
    {
        using var child = CkRaii.Child("ActiveItems", region, wFlags: WFlags.NoScrollbar | WFlags.AlwaysUseWindowPadding);

        if (_manager.ServerRestrictionData is not { } activeSlots)
            return;

        // clear the tutorial object if it has stale data, in case tutorial needs to run again.
        if (!_guides.IsTutorialActive(TutorialType.Restrictions) && _tActiveRestriction.Item1 != -1) _tActiveRestriction = (-1, null);

        var height = ImGui.GetContentRegionAvail().Y;
        var groupH = ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;
        var groupSpacing = (height - 5 * groupH) / 6;

        foreach (var (data, index) in activeSlots.Restrictions.WithIndex())
        {
            // Spacing.
            ImGui.SetCursorPosY((groupH + groupSpacing) * index);

            // if no item is selected, display the unique 'Applier' group.
            if (data.Identifier == Guid.Empty)
            {
                if (_tActiveRestriction.Item1 == -1) _tActiveRestriction.Item1 = index; // a slot for the tutorial item is found

                _activeItemDrawer.ApplyItemGroup(index, data);
                if (index != _tActiveRestriction.Item1) continue;
                // skip tutorial step for subsequent runs
                _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.Selecting, WardrobeUI.LastPos, WardrobeUI.LastSize, () =>
                {
                    _tActiveRestriction.Item2 = new ActiveRestriction { Enabler = MainHub.UID, Identifier = _selector.TutorialBasicRestriction.Identifier };
                    _selfBondage.DoSelfBind(_tActiveRestriction.Item1, _tActiveRestriction.Item2, DataUpdateType.Applied);
                    // because we found a free slot, we need to skip the next NoFreeSlots step too.
                    _guides.JumpToStep(TutorialType.Restrictions, StepsRestrictions.Locking);
                });
                continue;
            }

            // Get the item if it exists, otherwise, null. (this is fine, it's only used to display the image, which has null checks.)
            _manager.ActiveItems.TryGetValue(index, out var item);

            // If the padlock is currently locked, show the 'Unlocking' group.
            if (data.IsLocked())
            {
                _activeItemDrawer.UnlockItemGroup(index, data, item);
                if (index == _tActiveRestriction.Item1)
                {
                    _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.Unlocking, WardrobeUI.LastPos, WardrobeUI.LastSize, () =>
                    {
                        _tActiveRestriction.Item2 = data with { Padlock = Padlocks.None, PadlockAssigner = string.Empty };
                        _selfBondage.DoSelfBind(_tActiveRestriction.Item1, _tActiveRestriction.Item2, DataUpdateType.Unlocked);
                    });
                }
            }
            else
            {
                // Otherwise, show the 'Locking' group. Locking group can still change applied items.
                _activeItemDrawer.LockItemGroup(index, data, item);
                if (index == _tActiveRestriction.Item1)
                {
                    _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.Locking, WardrobeUI.LastPos, WardrobeUI.LastSize, () =>
                    {
                        _tActiveRestriction.Item2 = data with { Padlock = Padlocks.Metal, PadlockAssigner = MainHub.UID };
                        _selfBondage.DoSelfBind(_tActiveRestriction.Item1, _tActiveRestriction.Item2, DataUpdateType.Locked);
                    });
                }
            }
        }

        // if we get through the loop without finding an empty slot, skip a few steps.
        if (_tActiveRestriction.Item1 == -1 && _guides.IsTutorialActive(TutorialType.Restrictions) &&
            _guides.CurrentStep(TutorialType.Restrictions) == (int)StepsRestrictions.Selecting)
            _guides.JumpToStep(TutorialType.Restrictions, StepsRestrictions.NoFreeSlots);
    }
}
