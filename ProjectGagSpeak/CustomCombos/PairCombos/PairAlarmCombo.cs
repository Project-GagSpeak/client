using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;
using OtterGui.Text;
using System.Globalization;

namespace GagSpeak.CustomCombos.Pairs;

public sealed class PairAlarmCombo : CkFilterComboIconButton<KinksterAlarm>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _kinksterRef;

    public PairAlarmCombo(ILogger log, MainHub hub, Kinkster kinkster, Action postButtonPress)
        : base(log, FAI.Bell, "Enable", () => [ ..kinkster.LightCache.Alarms.Values.OrderBy(x => x.Label) ])
    {
        _mainHub = hub;
        _kinksterRef = kinkster;
        PostButtonPress = postButtonPress;
    }

    protected override bool DisableCondition() 
        => !_kinksterRef.PairPerms.ToggleAlarms;

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
        if (Current is null) 
            return false;

        // Construct the dto, and then send it off.
        var dto = new PushKinksterActiveAlarms(_kinksterRef.UserData, _kinksterRef.ActiveAlarms, Current.Id, DataUpdateType.AlarmToggled);
        var result = await _mainHub.UserChangeKinksterActiveAlarms(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform AlarmToggled on {_kinksterRef.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
            PostButtonPress?.Invoke();
            return false;
        }
        else
        {
            Log.LogDebug($"Toggling Alarm on {_kinksterRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
            PostButtonPress?.Invoke();
            return true;
        }
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

