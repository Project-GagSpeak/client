using Dalamud.Interface.Colors;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using ImGuiNET;
using OtterGui.Raii;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class RestrictionCombo : CkFilterComboCache<RestrictionItem>
{
    private readonly FavoritesManager _favorites;
    public RestrictionCombo(RestrictionManager restrictions, FavoritesManager favorites, ILogger log)
        : base(() => GetRestrictionItems(favorites, restrictions), log)
    {
        _favorites = favorites;
        SearchByParts = true;
    }

    private static List<RestrictionItem> GetRestrictionItems(FavoritesManager favorites, RestrictionManager restrictions)
    {
        return restrictions.Storage.OrderByDescending(p => favorites._favoritePatterns.Contains(p.Identifier)).ThenBy(p => p.Label).ToList();
    }

    protected override string ToString(RestrictionItem obj)
        => obj.Label;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var restriction = Items[globalIdx];

        if(Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restriction, restriction.Identifier) && CurrentSelectionIdx == globalIdx)
        {
            CurrentSelectionIdx = -1;
            CurrentSelection = default;
        }

        var ret = ImGui.Selectable(restriction.Label, selected);
        return ret;
    }

    public void Draw(float width)
    {
        var name = CurrentSelection?.Label ?? "Select a Restriction Item...";
        Draw("##RestrictionItems", name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
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
