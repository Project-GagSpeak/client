using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;
using OtterGui.Text;
using System.Globalization;
using GagSpeak.Kinksters;

namespace GagSpeak.CustomCombos.Pairs;

public sealed class PairAlarmCombo : CkFilterComboIconButton<LightAlarm>
{
    private readonly MainHub _mainHub;
    private Kinkster _kinksterRef;

    public PairAlarmCombo(ILogger log, MainHub hub, Kinkster kinkster) 
        : base(log, FAI.Bell, "Enable", () => [ ..kinkster.LastLightStorage.Alarms.OrderBy(x => x.Label)])
    {
        _mainHub = hub;
        _kinksterRef = kinkster;

        // update current selection to the last registered LightAlarm from that pair on construction.
        Current = kinkster.LastLightStorage?.Alarms.FirstOrDefault();
    }

    protected override bool DisableCondition() => _kinksterRef.PairPerms.ToggleAlarms is false;

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalAlarmIdx, bool selected)
    {
        var alarm = Items[globalAlarmIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(alarm.Label, selected);

        // Beside this, we should draw an alarm icon and the time it will go off.
        ImGui.SameLine(0, 10f);
        var localTime = alarm.SetTimeUTC.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
        using (ImRaii.Group())
        {
            CkGui.IconText(FAI.Stopwatch, ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            CkGui.ColorText(localTime + "(Your Time)", ImGuiColors.ParsedGold);
        }

        // shift over to draw an info button.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
        CkGui.IconText(FAI.WaveSquare, ImGuiColors.ParsedPink);
        DrawItemTooltip(alarm);

        return ret;
    }

    protected override async Task<bool> OnButtonPress()
    {
        if (_kinksterRef.LastToyboxData is null || Current is null) 
            return false;

        // Construct the dto, and then send it off.
        var dto = new PushKinksterToyboxUpdate(_kinksterRef.UserData, _kinksterRef.LastToyboxData, Current.Id, DataUpdateType.AlarmToggled);

        // Send out the command.
        var result = await _mainHub.UserChangeKinksterToyboxState(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform AlarmToggled on {_kinksterRef.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
            return false;
        }
        else
        {
            Log.LogDebug($"Toggling Alarm on {_kinksterRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
            return true;
        }
    }

    private void DrawItemTooltip(LightAlarm item)
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

            if(_kinksterRef.LastLightStorage.Patterns.FirstOrDefault(x => x.Id == item.Id) is { } pattern)
            {
                CkGui.ColorText("Alarm plays the Pattern:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(pattern.Label);
            }

            ImGui.EndTooltip();
        }
    }
}

