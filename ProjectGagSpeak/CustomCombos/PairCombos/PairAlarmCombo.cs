using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UI.Components.Combos;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.User;
using ImGuiNET;
using OtterGui.Text;
using System.Globalization;

namespace GagSpeak.CustomCombos.PairActions;

public sealed class PairAlarmCombo : CkFilterComboIconButton<LightAlarm>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;

    public PairAlarmCombo(Pair pairData, MainHub mainHub, ILogger log, UiSharedService uiShared, string bText, string bTT) 
        : base(() => [
            .. pairData.LastLightStorage.Alarms.OrderBy(x => x.Label),
        ], log, uiShared, FontAwesomeIcon.Bell, bText, bTT)
    {
        _mainHub = mainHub;
        _pairRef = pairData;

        // update current selection to the last registered LightAlarm from that pair on construction.
        if (_pairRef.LastToyboxData is not null && _pairRef.LastLightStorage is not null)
            CurrentSelection = _pairRef.LastLightStorage.Alarms.FirstOrDefault();
    }

    protected override bool DisableCondition() => _pairRef.PairPerms.ToggleAlarms is false;

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
            _uiShared.IconText(FontAwesomeIcon.Stopwatch, ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            UiSharedService.ColorText(localTime + "(Your Time)", ImGuiColors.ParsedGold);
        }

        // shift over to draw an info button.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
        _uiShared.IconText(FontAwesomeIcon.WaveSquare, ImGuiColors.ParsedPink);
        DrawItemTooltip(alarm);

        return ret;
    }

    protected override void OnButtonPress()
    {
        if (_pairRef.LastToyboxData is null || CurrentSelection is null) 
            return;

        // Construct the dto, and then send it off.
        var dto = new PushPairToyboxDataUpdateDto(_pairRef.UserData, _pairRef.LastToyboxData, DataUpdateType.AlarmToggled)
        {
            AffectedIdentifier = CurrentSelection.Id,
        };
        // Send out the command.
        _ = _mainHub.UserPushPairDataToybox(dto);
        PairCombos.Opened = InteractionType.None;
        Log.LogDebug("Toggling Alarm " + CurrentSelection.Label + " on " + _pairRef.GetNickAliasOrUid() + "'s AlarmList", LoggerType.Permissions);
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
            UiSharedService.ColorText("Goes Off @", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            var localTime = item.SetTimeUTC.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
            ImGui.Text(localTime);

            ImGui.Spacing();

            if(_pairRef.LastLightStorage.Patterns.FirstOrDefault(x => x.Id == item.Id) is { } pattern)
            {
                UiSharedService.ColorText("Alarm plays the Pattern:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(pattern.Label);
            }

            ImGui.EndTooltip();
        }
    }
}

