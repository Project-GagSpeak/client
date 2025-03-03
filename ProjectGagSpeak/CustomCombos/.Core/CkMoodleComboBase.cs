using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.UI;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CustomCombos;

public abstract class CkMoodleComboBase<T> : CkFilterComboCache<T>
{
    protected readonly CharaIPCData _moodleData;
    protected readonly MoodleStatusMonitor _statuses;
    protected float _iconScale;

    protected CkMoodleComboBase(float iconScale, CharaIPCData data, MoodleStatusMonitor monitor, ILogger log,
        Func<IReadOnlyList<T>> generator) : base(generator, log)
    {
        _statuses = monitor;
        _iconScale = iconScale;
        _moodleData = data;
    }

    protected virtual Vector2 IconSize => MoodleStatusMonitor.DefaultSize * _iconScale;

    protected override void DrawList(float width, float itemHeight)
    {
        ImGui.SetWindowFontScale(_iconScale);
        base.DrawList(width, itemHeight);
        ImGui.SetWindowFontScale(1f);
    }

    protected void DrawItemTooltip(MoodlesStatusInfo item)
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
                var status = _moodleData.MoodlesStatuses
                    .FirstOrDefault(x => x.GUID == item.StatusOnDispell).Title ?? "Unknown";
                ImGui.Text(status);
            }

            ImGui.EndTooltip();
        }
    }
}
