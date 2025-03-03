using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UI.Components.Combos;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CustomCombos;

public abstract class CkMoodleComboButtonBase<T> : CkFilterComboButton<T>
{
    protected readonly MoodleStatusMonitor _statuses;
    protected readonly MainHub _mainHub;
    protected readonly Pair _pairRef;
    protected float _iconScale;

    protected CkMoodleComboButtonBase(float iconScale, MoodleStatusMonitor monitor, Pair pair, MainHub hub,
        ILogger log, string bText, string bTT, Func<IReadOnlyList<T>> itemSource)
        : base(itemSource, log, bText, bTT)
    {
        _statuses = monitor;
        _mainHub = hub;
        _iconScale = iconScale;
        CurrentSelection = itemSource().FirstOrDefault();
    }

    protected virtual Vector2 IconSize => MoodleStatusMonitor.DefaultSize * _iconScale;

    protected override void DrawList(float width, float itemHeight)
    {
        ImGui.SetWindowFontScale(_iconScale);
        base.DrawList(width, itemHeight);
        ImGui.SetWindowFontScale(1f);
    }

    protected abstract bool CanDoAction(T item);
    protected abstract void DoAction(T item);

    protected override void OnButtonPress()
    {
        if(CurrentSelection is null)
            return;

        if (!CanDoAction(CurrentSelection))
            return;

        DoAction(CurrentSelection);
        PairCombos.Opened = InteractionType.None;
    }

    protected void DrawItemTooltip(MoodlesStatusInfo item, string dispellMoodleTitle)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            ImGui.BeginTooltip();

            if (!item.Description.IsNullOrWhitespace())
            {
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                ImGui.TextUnformatted(item.Description);
                ImGui.PopTextWrapPos();
            }

            ImGui.Separator();
            CkGui.ColorText("Stacks:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Stacks.ToString());
            if (item.StackOnReapply)
            {
                ImGui.SameLine();
                CkGui.ColorText(" (inc by " + item.StacksIncOnReapply + ")", ImGuiColors.ParsedGold);
            }

            CkGui.ColorText("Duration:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text($"{item.Days}d {item.Hours}h {item.Minutes}m {item.Seconds}");

            CkGui.ColorText("Category:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Type.ToString());

            CkGui.ColorText("Dispellable:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Dispelable ? "Yes" : "No");

            if (!item.StatusOnDispell.IsEmptyGuid())
            {
                CkGui.ColorText("StatusOnDispell:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(dispellMoodleTitle);
            }

            ImGui.EndTooltip();
        }
    }
}
