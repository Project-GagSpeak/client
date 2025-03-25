using Dalamud.Interface;
using Dalamud.Interface.Colors;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CustomCombos;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class RestrictionGagCombo : CkFilterComboCache<GarblerRestriction>
{
    private readonly FavoritesManager _favorites;
    private GagType _currentGag;
    private int _currentSlotIdx;
    public RestrictionGagCombo(ILogger log, FavoritesManager favorites, Func<IReadOnlyList<GarblerRestriction>> generator)
    : base(generator, log)
    {
        _favorites = favorites;
        _currentGag = GagType.None;
        SearchByParts = true;
    }

    protected override string ToString(GarblerRestriction obj) => obj.GagType.GagName();

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (CurrentSelection?.GagType == _currentGag)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.GagType == _currentGag);
        CurrentSelection = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, GagType currentGag, float width)
    {
        InnerWidth = width * 1.25f;
        _currentGag = currentGag;
        var previewLabel = _currentGag.GagName();
        return Draw(label, previewLabel, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
    }


    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var gagItem = Items[globalIdx];

        if (Icons.DrawFavoriteStar(_favorites, gagItem.GagType) && CurrentSelectionIdx == globalIdx)
        {
            CurrentSelectionIdx = -1;
            CurrentSelection = default;
        }

        var ret = ImGui.Selectable(gagItem.GagType.GagName(), selected);

        // IF the GagType is active in the gag storage, draw it's link icon.
        if (ItemIsActiveWithData(gagItem))
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
            CkGui.IconText(FAI.Link, ImGui.GetColorU32(ImGuiColors.HealerGreen));
        }
        return ret;
    }

    private bool ItemIsActiveWithData(GarblerRestriction item)
    {
        return item.IsEnabled 
            && (item.Glamour.GameItem.ItemId != ItemService.NothingItem(item.Glamour.Slot).ItemId
                || item.Moodle.Id != Guid.Empty
                || item.Mod is not null);
    }
}

