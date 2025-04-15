using Dalamud.Interface.Colors;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using ImGuiNET;
using OtterGui.Raii;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class RestraintCombo : CkFilterComboCache<RestraintSet>
{
    private readonly FavoritesManager _favorites;
    public RestraintCombo(ILogger log, FavoritesManager favorites, Func<IReadOnlyList<RestraintSet>> restraintsGenerator)
        : base(restraintsGenerator, log)
    {
        _favorites = favorites;
        SearchByParts = true;
    }

    protected override string ToString(RestraintSet obj)
        => obj.Label;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var pattern = Items[globalIdx];

        if(Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restraint, pattern.Identifier) && CurrentSelectionIdx == globalIdx)
        {
            // Force a recalculation on the cached display.
            CurrentSelectionIdx = -1;
            Current = default;
        }

        var ret = ImGui.Selectable(pattern.Label, selected);
        return ret;
    }

    public void Draw(float width)
    {
        var name = Current?.Label ?? string.Empty;
        Draw("##RestraintSets", name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
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
