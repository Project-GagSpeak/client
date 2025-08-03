using CkCommons;
using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;
using OtterGui.Text;
using System.Globalization;

namespace GagSpeak.CustomCombos.Pairs;

public sealed class PairAlarmCombo : CkFilterComboIconTextButton<KinksterAlarm>
{
    private readonly MainHub _mainHub;
    private Kinkster _ref;

    public PairAlarmCombo(ILogger log, MainHub hub, Kinkster kinkster)
        : base(log, FAI.Bell, () => [ ..kinkster.LightCache.Alarms.Values.OrderBy(x => x.Label) ])
    {
        _mainHub = hub;
        _ref = kinkster;
    }

    protected override bool DisableCondition() 
        => !_ref.PairPerms.ToggleAlarms;

    protected override string ToString(KinksterAlarm obj)
        => obj.Label;

    public bool Draw(string label, float width, string tooltipSuffix)
    {
        var state = _ref.ActiveAlarms.Contains(Current?.Id ?? Guid.Empty);
        var buttonText = state ? "Disable" : "Enable";
        var tt = $"{buttonText} {tooltipSuffix}.";
        // determine the text based on the state of Current.
        return Draw(label, width, buttonText, tt);
    }

    protected override void DrawList(float width, float itemHeight, float filterHeight)
    {
        _infoIconWidth = CkGui.IconSize(FAI.InfoCircle).X;
        _powerIconWidth = CkGui.IconSize(FAI.PowerOff).X;
        base.DrawList(width, itemHeight, filterHeight);
    }

    private float _infoIconWidth;
    private float _powerIconWidth;

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalAlarmIdx, bool selected)
    {
        var alarm = Items[globalAlarmIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(alarm.Label, selected);

        // Beside this, we should draw an alarm icon and the time it will go off.
        ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.X);
        CkGui.ColorText(alarm.SetTimeUTC.ToLocalTime().ToString("t", CultureInfo.CurrentCulture), ImGuiColors.ParsedGold);
        CkGui.AttachToolTip("(Your Local Time)");

        var isEnabled = _ref.ActiveAlarms.Contains(alarm.Id);
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - _infoIconWidth - _powerIconWidth - ImGui.GetStyle().ItemInnerSpacing.X);
        CkGui.IconText(FAI.PowerOff, isEnabled ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey);
        CkGui.AttachToolTip($"Alarm is currently {(isEnabled ? "Enabled" : "Disabled")}");

        ImUtf8.SameLineInner();
        CkGui.HoverIconText(FAI.InfoCircle, ImGuiColors.TankBlue.ToUint(), ImGui.GetColorU32(ImGuiCol.TextDisabled));
        DrawItemTooltip(alarm);

        return ret;
    }

    protected override void OnButtonPress()
    {
        if (Current is null) 
            return;

        var alarms = new List<Guid>(_ref.ActiveAlarms);
        if (!alarms.Remove(Current.Id))
            alarms.Add(Current.Id);

        UiService.SetUITask(async () =>
        {
            // Construct the dto, and then send it off.
            var dto = new PushKinksterActiveAlarms(_ref.UserData, alarms, Current.Id, DataUpdateType.AlarmToggled);
            var result = await _mainHub.UserChangeKinksterActiveAlarms(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
                Log.LogDebug($"Failed to perform AlarmToggled on {_ref.GetNickAliasOrUid()}, Reason:{result.ErrorCode}", LoggerType.StickyUI);
        });
    }

    private void DrawItemTooltip(KinksterAlarm item)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            ImGui.BeginTooltip();

            ImGui.TextUnformatted(item.Label + "'s Details:");
            ImGui.Separator();

            // Draw the alarm time.
            CkGui.ColorText("Goes Off @", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            var localTime = item.SetTimeUTC.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
            ImGui.Text(localTime);

            ImGui.Spacing();

            if(item.PatternRef is { } pattern)
            {
                CkGui.ColorText("Alarm plays the Pattern:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(pattern.Label);
            }

            ImGui.EndTooltip();
        }
    }
}

