using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class PatternCombo : CkFilterComboCache<Pattern>
{
    private readonly FavoritesManager _favorites;
    public PatternCombo(PatternManager patterns, FavoritesManager favorites, ILogger log)
        : base(() => GetPatterns(favorites, patterns), log)
    {
        _favorites = favorites;
        SearchByParts = true;
    }

    private static List<Pattern> GetPatterns(FavoritesManager favorites, PatternManager patterns)
    {
        return patterns.Storage.OrderByDescending(p => favorites._favoritePatterns.Contains(p.Identifier)).ThenBy(p => p.Label).ToList();
    }

    protected override string ToString(Pattern obj)
        => obj.Label;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var pattern = Items[globalIdx];

        if(Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Pattern, pattern.Identifier) && CurrentSelectionIdx == globalIdx)
        {
            // Force a recalculation on the cached display.
            CurrentSelectionIdx = -1;
            Current = default;
        }

        var ret = ImGui.Selectable(pattern.Label, selected);

        // draws a fancy box when the mod is hovered giving you the details about the mod.
        if (ImGui.IsItemHovered())
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
            using var tt = ImRaii.Tooltip();

            // shift over and draw an info circle, and a loop circle if any.
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - 2 * ImGui.GetTextLineHeight() - ImGui.GetStyle().ItemSpacing.X);

            // draw the shouldLoop icon.
            CkGui.IconText(FAI.Sync, ImGui.GetColorU32(pattern.ShouldLoop ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey));
            if (pattern.ShouldLoop) CkGui.AttachToolTip("This is a Looping Pattern.");

            // draw the info icon.
            ImGui.SameLine();
            CkGui.IconText(FAI.InfoCircle, ImGuiColors.TankBlue);
            DrawItemTooltip(pattern);
        }

        return ret;
    }

    public void Draw(float width)
    {
        // Begin Draw.
        var name = Current?.Label ?? "Select a Pattern...";
        Draw("##Patterns", name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
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

            if (!item.Description.IsNullOrWhitespace())
            {
                // push the text wrap position to the font size times 35
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                // we will then check to see if the text contains a tooltip
                if (item.Description.Contains(CkGui.TooltipSeparator, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = item.Description.Split(CkGui.TooltipSeparator, StringSplitOptions.None);
                    // for each of the split text, we will display the text unformatted
                    for (var i = 0; i < splitText.Length; i++)
                    {
                        ImGui.TextUnformatted(splitText[i]);
                        if (i != splitText.Length - 1) ImGui.Separator();
                    }
                }
                else
                {
                    ImGui.TextUnformatted(item.Description);
                }
                // finally, pop the text wrap position
                ImGui.PopTextWrapPos();
                ImGui.Separator();
            }

            CkGui.ColorText("Duration:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            var durationStr = item.Duration.Hours > 0 ? item.Duration.ToString("hh\\:mm\\:ss") : item.Duration.ToString("mm\\:ss");
            ImGui.Text(durationStr);

            CkGui.ColorText("Loops?:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.Text(item.ShouldLoop ? "Yes" : "No");

            ImGui.EndTooltip();
        }
    }

}
