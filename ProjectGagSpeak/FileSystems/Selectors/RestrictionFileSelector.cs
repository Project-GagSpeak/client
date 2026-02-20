using CkCommons.FileSystem;
using CkCommons.FileSystem.Selector;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Wardrobe;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class RestrictionFileSelector : CkFileSystemSelector<RestrictionItem, RestrictionFileSelector.RestrictionState>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesConfig _favorites;
    private readonly RestrictionManager _manager;
    private readonly TutorialService _guides;
    
    public GagspeakMediator Mediator { get; init; }

    public RestrictionItem TutorialHypnoRestriction { get; private set; }
    public RestrictionItem TutorialBasicRestriction { get; private set; }

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct RestrictionState(uint Color) { }

    // Helper operations used for creating new items and cloning them.
    private RestrictionType _newType;
    // private RestrictionItem? _clonedRestrictionItem;

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new RestrictionFileSystem.Leaf? SelectedLeaf
    => base.SelectedLeaf;

    public RestrictionFileSelector(GagspeakMediator mediator, FavoritesConfig favorites, RestrictionManager manager,
        RestrictionFileSystem fileSystem, TutorialService guides) : base(fileSystem, Svc.Logger.Logger, Svc.KeyState, "##RestrictionFS")
    {
        Mediator = mediator;
        _favorites = favorites;
        _manager = manager;
        _guides = guides;

        Mediator.Subscribe<ConfigRestrictionChanged>(this, (msg) => OnRestrictionChange(msg.Type, msg.Item, msg.OldString));

        // Do not subscribe to the default renamer, we only want to rename the item itself.
        UnsubscribeRightClickLeaf(RenameLeaf);
        // Subscribe to the rename Restriction.
        SubscribeRightClickLeaf(RenameRestriction);
    }

    public override ISortMode<RestrictionItem> SortMode => new RestrictionSorter();

    private void RenameRestriction(RestrictionFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        var currentName = leaf.Value.Label;
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename Restriction:");
        if (ImGui.InputText("##RenameRestriction", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _manager.Rename(leaf.Value, currentName);
            ImGui.CloseCurrentPopup();
        }
        CkGui.AttachToolTip("Enter a new name here to rename the changed restriction.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.UnsubscribeAll(this);
    }

    protected override bool DrawLeafInner(CkFileSystem<RestrictionItem>.Leaf leaf, in RestrictionState state, bool selected)
    {
        var leafSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        ImGui.InvisibleButton("leaf", leafSize);
        var hovered = ImGui.IsItemHovered();
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var bgColor = hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        ImGui.GetWindowDrawList().AddRectFilled(rectMin, rectMax, bgColor, 5);

        // the border if selected.
        if (selected)
        {
            ImGui.GetWindowDrawList().AddRectFilledMultiColor(
                rectMin,
                rectMin + leafSize,
                CkGui.Color(new Vector4(0.886f, 0.407f, 0.658f, .3f)), 0, 0, CkGui.Color(new Vector4(0.886f, 0.407f, 0.658f, .3f)));

            ImGui.GetWindowDrawList().AddRectFilled(
                rectMin,
                new Vector2(rectMin.X + ImGuiHelpers.GlobalScale * 3, rectMax.Y),
                CkGui.Color(ImGuiColors.ParsedPink), 5);
        }

        ImGui.SetCursorScreenPos(rectMin with { X = rectMin.X + ImGui.GetStyle().ItemSpacing.X });
        if (Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restriction, leaf.Value.Identifier))
            SetFilterDirty();
        CkGui.TextFrameAlignedInline(leaf.Value.Label);
        // Only draw the deletion if the item is not active or occupied.
        if (!_manager.IsItemApplied(leaf.Value.Identifier))
        {
            ImGui.SameLine((rectMax.X - rectMin.X) - ImGui.GetFrameHeightWithSpacing());
            var pos = ImGui.GetCursorScreenPos();
            var hovering = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(ImGui.GetFrameHeight()));
            var col = (hovering && KeyMonitor.ShiftPressed()) ? ImGuiCol.Text : ImGuiCol.TextDisabled;
            CkGui.FramedIconText(FAI.Trash, ImGui.GetColorU32(col));
            if (hovering && KeyMonitor.ShiftPressed() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                Log.Debug($"Deleting {leaf.Value.Label} with SHIFT pressed.");
                _manager.Delete(leaf.Value);
            }
            CkGui.AttachToolTip("Delete this restriction item. This cannot be undone.--SEP--Must be holding SHIFT to remove.");
        }
        return hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left);
    }

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnRestrictionChange(StorageChangeType type, RestrictionItem restriction, string? oldString)
        => SetFilterDirty();


    /// <summary> Add the state filter combo-button to the right of the filter box. </summary>
    protected override float CustomFiltersWidth(float width)
    {
        return width
            - CkGui.IconButtonSize(FAI.Plus).X
            - CkGui.IconButtonSize(FAI.FolderPlus).X
            - ImGui.GetStyle().ItemInnerSpacing.X;
    }

    protected override void DrawCustomFilters()
    {
        if (CkGui.IconButton(FAI.Plus, inPopup: true))
            ImGui.OpenPopup("##NewRestriction");
        CkGui.AttachToolTip("Create a new Restriction Item.");
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.CreatingRestriction, WardrobeUI.LastPos, WardrobeUI.LastSize,
            () =>
            {
                // make a hypno item to show the user the extra things it can do
                TutorialHypnoRestriction = _manager.CreateNew("Tutorial Hypno", RestrictionType.Hypnotic); 
                // This is to apply later, as I don't feel comfy applying hypno for photosensitive reasons
                TutorialBasicRestriction = _manager.CreateNew("Tutorial Restriction", RestrictionType.Normal);
                TutorialBasicRestriction.Glamour = new GlamourSlot(EquipSlot.Head, EquipItem.FromId(2784));
            });
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.RestrictionTypes, WardrobeUI.LastPos, WardrobeUI.LastSize);

        ImGui.SameLine(0, 1);
        DrawFolderButton();
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.CreatingFolders, WardrobeUI.LastPos, WardrobeUI.LastSize,
            () => CkFileSystem.FindOrCreateAllFolders("Tutorial Folder"));
    }

    public override void DrawPopups()
        => NewRestrictionPopup();

    private void NewRestrictionPopup()
    {
        if (!OpenRestrictionNameField("##NewRestriction", ref _newName))
            return;

        _manager.CreateNew(_newName, _newType);
        _newName = string.Empty;
        _newType = RestrictionType.Normal;
    }

    private bool OpenRestrictionNameField(string popupName, ref string newName)
    {
        using var popup = ImRaii.Popup(popupName);
        if (!popup)
            return false;

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            ImGui.CloseCurrentPopup();

        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();

        var fullWidth = 200 * ImGuiHelpers.GlobalScale;
        var comboWidth = fullWidth / 2;
        var buttonWidth = fullWidth - comboWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        ImGui.SetNextItemWidth(fullWidth);
        var doit = ImGui.InputTextWithHint("##newName", "Enter New Name...", ref newName, 512, ITFlags.EnterReturnsTrue);

        if (CkGuiUtils.EnumCombo("##RestrictionType", comboWidth, _newType, out var newType,
            Enum.GetValues<RestrictionType>(), defaultText: "Select Type.."))
        {
            _newType = newType;
        }
        CkGui.AttachToolTip("Define what type of restriction you want to make.");

        // Alternative early exit.
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.PlusCircle, "Create Item", buttonWidth, disabled: string.IsNullOrWhiteSpace(newName)))
        {
            ImGui.CloseCurrentPopup();
            return true;
        }

        if (!doit)
            return false;

        ImGui.CloseCurrentPopup();
        return true;
    }

    // Placeholder until we Integrate the DynamicSorter
    private struct RestrictionSorter : ISortMode<RestrictionItem>
    {
        public string Name
            => "Restriction Sorter";

        public string Description
            => "Sort all Restrictions by their name, with favorites first.";

        public IEnumerable<CkFileSystem<RestrictionItem>.IPath> GetChildren(CkFileSystem<RestrictionItem>.Folder folder)
            => folder.GetSubFolders().Cast<CkFileSystem<RestrictionItem>.IPath>()
                .Concat(folder.GetLeaves().OrderByDescending(l => FavoritesConfig.Restrictions.Contains(l.Value.Identifier)));
    }
}

