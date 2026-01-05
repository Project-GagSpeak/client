using CkCommons.Gui;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using GagSpeak.PlayerClient;
using GagSpeak.State.Models;
using GagspeakAPI.Util;
using Dalamud.Bindings.ImGui;
using OtterGui.Extensions;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Editor;

public sealed class RestrictionGagCombo : CkFilterComboCache<GarblerRestriction>
{
    private readonly FavoritesConfig _favorites;
    private GagType _currentGag;
    public RestrictionGagCombo(ILogger log, FavoritesConfig favorites, Func<IReadOnlyList<GarblerRestriction>> generator)
    : base(generator, log)
    {
        _favorites = favorites;
        _currentGag = GagType.None;
        SearchByParts = true;
    }

    protected override string ToString(GarblerRestriction obj) 
        => obj.GagType.GagName();

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current?.GagType == _currentGag)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.GagType == _currentGag);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, GagType current, float width, uint? searchBg = null)
        => Draw(label, current, width, CFlags.None, searchBg);

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, GagType current, float width, CFlags flags, uint? searchBg = null)
    {
        InnerWidth = width * 1.25f;
        _currentGag = current;
        var previewLabel = _currentGag.GagName();
        return Draw(label, previewLabel, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags, searchBg);
    }

    public bool DrawPopup(string label, GagType current, float width, Vector2 drawPos, uint? searchBg = null)
    {
        InnerWidth = width * 1.25f;
        _currentGag = current;
        
        return DrawPopup(label, drawPos, ImGui.GetTextLineHeightWithSpacing(), searchBg);
    }


    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var gagItem = Items[globalIdx];

        if (Icons.DrawFavoriteStar(_favorites, gagItem.GagType, false) && CurrentSelectionIdx == globalIdx)
        {
            CurrentSelectionIdx = -1;
            Current = default;
        }

        ImUtf8.SameLineInner();
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
            && (item.Glamour.GameItem.ItemId != ItemSvc.NothingItem(item.Glamour.Slot).ItemId
                || item.Moodle.Id != Guid.Empty || !string.IsNullOrEmpty(item.Mod.Label));
    }
}

