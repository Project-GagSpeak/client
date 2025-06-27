using CkCommons.Gui;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using ImGuiNET;
using OtterGui.Extensions;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Editor;

public sealed class PatternCombo : CkFilterComboCache<Pattern>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesManager _favorites;
    public Guid _current { get; private set; }
    public PatternCombo(ILogger log, GagspeakMediator mediator, FavoritesManager favorites,
        Func<IReadOnlyList<Pattern>> generator) : base(generator, log)
    {
        _favorites = favorites;
        _current = Guid.Empty;
        SearchByParts = true;

        Mediator = mediator;
        Mediator.Subscribe<ConfigPatternChanged>(this, _ => RefreshCombo());
    }

    public GagspeakMediator Mediator { get; }

    void IDisposable.Dispose()
    {
        Mediator.Unsubscribe<ConfigPatternChanged>(this);
        GC.SuppressFinalize(this);
    }

    protected override string ToString(Pattern obj)
        => obj.Label;

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current?.Identifier == _current)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.Identifier == _current);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : null;
        return CurrentSelectionIdx;
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, Guid current, float width, uint? searchBg = null)
        => Draw(label, current, width, CFlags.None, searchBg);

    public bool Draw(string label, Guid current, float width, CFlags flags, uint? searchBg = null)
    {
        InnerWidth = width * 1.45f;
        _current = current;
        var preview = Items.FirstOrDefault(i => i.Identifier == current)?.Label ?? "Select Pattern...";
        return Draw(label, preview, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags, searchBg);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var pattern = Items[globalIdx];

        if(Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Pattern, pattern.Identifier) && CurrentSelectionIdx == globalIdx)
        {
            CurrentSelectionIdx = -1;
            Current = default;
        }

        ImUtf8.SameLineInner();
        var ret = ImGui.Selectable(pattern.Label, selected);

        // shift over and draw an info circle, and a loop circle if any.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 2 * ImGui.GetTextLineHeight() - ImGui.GetStyle().ItemSpacing.X);

        // draw the shouldLoop icon.
        CkGui.IconText(FAI.Sync, ImGui.GetColorU32(pattern.ShouldLoop ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey));
        if (pattern.ShouldLoop) CkGui.AttachToolTip("This is a Looping Pattern.");

        // draw the info icon.
        ImGui.SameLine();
        CkGui.IconText(FAI.InfoCircle, ImGuiColors.TankBlue);
        DrawItemTooltip(pattern);

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

            if (!item.Description.IsNullOrWhitespace())
            {
                // push the text wrap position to the font size times 35
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                // we will then check to see if the text contains a tooltip
                if (item.Description.Contains(CkGui.TipSep, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = item.Description.Split(CkGui.TipSep, StringSplitOptions.None);
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
            ImGui.EndTooltip();
        }
    }

}
