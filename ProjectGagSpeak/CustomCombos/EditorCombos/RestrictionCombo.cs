using Dalamud.Interface.Colors;
using Dalamud.Utility;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class RestrictionCombo : CkFilterComboCache<RestrictionItem>
{
    private readonly FavoritesManager _favorites;
    public Guid _currentRestriction;
    public RestrictionCombo(ILogger log, FavoritesManager favorites, Func<IReadOnlyList<RestrictionItem>> generator)
        : base(generator, log)
    {
        _favorites = favorites;
        _currentRestriction = Guid.Empty;
        SearchByParts = true;
    }

    protected override string ToString(RestrictionItem obj)
        => obj.Label;

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current?.Identifier == _currentRestriction)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.Identifier == _currentRestriction);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : null;
        return CurrentSelectionIdx;
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, Guid current, float width)
        => Draw(label, current, width, ImGuiComboFlags.None);

    public bool Draw(string label, Guid current, float width, ImGuiComboFlags flags)
    {
        InnerWidth = width * 1.25f;
        _currentRestriction = current;
        var preview = Items.FirstOrDefault(i => i.Identifier == current)?.Label ?? "Select Restriction...";
        return Draw(label, preview, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var restriction = Items[globalIdx];

        if(Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restriction, restriction.Identifier) && CurrentSelectionIdx == globalIdx)
        {
            CurrentSelectionIdx = -1;
            Current = default;
        }
        ImUtf8.SameLineInner();
        var ret = ImGui.Selectable(restriction.Label, selected);
        return ret;
    }

    private void DrawItemTooltip(Pattern item)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            // begin the tooltip interface
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(item.Label);
            ImGui.EndTooltip();
        }
    }

}
