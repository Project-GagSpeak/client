using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;

namespace GagSpeak.Gui.Toybox;

public partial class AlarmsPanel
{
    private readonly ILogger<AlarmsPanel> _logger;
    private readonly AlarmFileSelector _selector;
    private readonly PatternManager _patterns;
    private readonly AlarmManager _manager;
    private readonly TutorialService _guides;

    private PatternCombo _patternCombo;
    public AlarmsPanel(ILogger<AlarmsPanel> logger, GagspeakMediator mediator,
        AlarmFileSelector selector, PatternManager patterns, AlarmManager manager,
        FavoritesConfig favorites, TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _patterns = patterns;
        _manager = manager;
        _guides = guides;

        _patternCombo = new PatternCombo(logger, mediator, favorites, () => [
            ..patterns.Storage.OrderByDescending(p => FavoritesConfig.Patterns.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, ToyboxTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("AlarmsTL", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("AlarmsBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("AlarmsTR", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawAlarmInfo(drawRegions.BotRight, curveSize);
    }

    private void DrawAlarmInfo(CkHeader.DrawRegion region, float curveSize)
    {
        DrawSelectedAlarm(region);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawSelectedAlarm(CkHeader.DrawRegion region)
    {
        var item = _selector.Selected;
        var editorItem = _manager.ItemInEditor;

        var isEditing = item is not null && item.Identifier == editorItem?.Identifier;
        var isActive = item is not null && _manager.ActiveAlarms.Any(g => g.Identifier == item.Identifier);

        var label = item is null ? "No Item Selected!" : isEditing ? $"{item!.Label} - (Editing)" : item!.Label;
        var tooltip = item is null ? "No item selected!" : isActive ? "Alarm is Active!"
                : $"Double Click to {(editorItem is null ? "Edit" : "Save Changes to")} this Alarm.--SEP--Right Click to cancel and exit Editor.";

        using (CkRaii.ChildLabelCustomButton("##AlarmSel", region.Size, ImGui.GetFrameHeight(), DrawLabel, BeginEdits, tooltip, DFlags.RoundCornersRight, LabelFlags.SizeIncludesHeader))
        {
            if(editorItem is { } itemInEditor)
                DrawSelectedInner(itemInEditor, true);
            else if (item is not null)
                DrawSelectedInner(item, false);
        }

        void DrawLabel()
        {
            using var c = CkRaii.Child("##AlarmSelLabel", new Vector2(region.SizeX * .6f, ImGui.GetFrameHeight()));
            ImGui.Spacing();
            ImGui.SameLine();
            ImUtf8.TextFrameAligned(label);
            ImGui.TextUnformatted(label);
            ImGui.SameLine(c.InnerRegion.X * .7f - (ImGui.GetFrameHeight() * 1.5f));
            CkGui.FramedIconText(isEditing ? FAI.Save : FAI.Edit);
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is not ImGuiMouseButton.Left || isActive)
                return;

            if (isEditing) 
                _manager.SaveChangesAndStopEditing();
            else 
                _manager.StartEditing(_selector.Selected!);
        }
    }

    private void DrawSelectedInner(Alarm alarm, bool isEditorItem)
    {
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 1)))
        {
            CkGui.Separator();
            DrawAlarmTime(alarm, isEditorItem);

            CkGui.Separator();
            DrawPatternSelection(alarm, isEditorItem);

            CkGui.Separator();
            DrawPatternTimeSpans(alarm, isEditorItem);

            CkGui.Separator();
        }
        DrawAlarmFrequency(alarm, isEditorItem);

        DrawFooter(alarm);
    }
}
