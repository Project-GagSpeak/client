using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui.Text;
using System.Globalization;
using System.Numerics;

namespace GagSpeak.UI.Components.Combos;

public sealed class PairAlarmCombo : PairComboToggle<LightAlarm>
{
    private readonly UiSharedService _uiShared;

    public PairAlarmCombo(ILogger log, MainHub mainHub, UiSharedService uiShared, Pair pairData, string bOnText, string bOnTT, string bOffText, string bOffTT)
        : base(log, uiShared, mainHub, pairData, bOnText, bOnTT, bOffText, bOffTT)
    {
        _uiShared = uiShared;

        // update current selection to the last registered LightAlarm from that pair on construction.
        if (_pairRef.LastToyboxData is not null && _pairRef.LastLightStorage is not null)
            CurrentSelection = _pairRef.LastLightStorage.Alarms.FirstOrDefault();
    }

    // override the method to extract items by extracting all LightAlarms.
    protected override IEnumerable<LightAlarm> ExtractItems() => _pairRef.LastLightStorage?.Alarms ?? new List<LightAlarm>();

    protected override string ToItemString(LightAlarm item) => item.Name;

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(LightAlarm alarmItem, bool selected)
    {
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(alarmItem.Name, selected);

        // Beside this, we should draw an alarm icon and the time it will go off.
        ImGui.SameLine(0, 10f);
        var localTime = alarmItem.SetTimeUTC.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
        using (ImRaii.Group())
        {
            _uiShared.IconText(FontAwesomeIcon.Stopwatch, ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            UiSharedService.ColorText(localTime, ImGuiColors.ParsedGold);
        }
        UiSharedService.AttachToolTip("Goes off at " + localTime + " (Your Time)");

        // shift over to draw an info button.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
        _uiShared.IconText(FontAwesomeIcon.WaveSquare, ImGuiColors.ParsedPink);
        DrawItemTooltip(alarmItem);

        return ret;
    }

    protected override void OnTogglePressed(LightAlarm item)
    {
        if (_pairRef.LastToyboxData is null) return;

        // clone the toybox data.
        var newToyboxData = _pairRef.LastToyboxData.DeepClone();
        // update the interaction ID.
        newToyboxData.InteractionId = item.Identifier;

        // if it was active, deactivate it, otherwise, activate it.
        if (newToyboxData.ActiveAlarms.Contains(item.Identifier)) newToyboxData.ActiveAlarms.Remove(item.Identifier);
        else newToyboxData.ActiveAlarms.Add(item.Identifier);

        // Send out the command.
        _ = _mainHub.UserPushPairDataToyboxUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, newToyboxData, ToyboxUpdateType.AlarmToggled, UpdateDir.Other));
        PairCombos.Opened = InteractionType.None;
        _logger.LogDebug("Toggling Alarm " + item.Name + " on " + _pairRef.GetNickAliasOrUid() + "'s AlarmList", LoggerType.Permissions);
    }

    protected override bool IsToggledOn(LightAlarm? selection)
    {
        if (selection is null || _pairRef.LastToyboxData is null) return false;
        // if the select is not present in the LastToyboxData's ActiveAlarms list, it is not on.
        return _pairRef.LastToyboxData.ActiveAlarms.Contains(selection.Identifier);
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

            ImGui.TextUnformatted(item.Name + "'s Details:");
            ImGui.Separator();

            // Draw the alarm time.
            UiSharedService.ColorText("Goes Off @", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            var localTime = item.SetTimeUTC.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
            ImGui.Text(localTime);

            ImGui.Spacing();

            UiSharedService.ColorText("Alarm plays the Pattern:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.PatternThatPlays);

            ImGui.EndTooltip();
        }
    }
}

