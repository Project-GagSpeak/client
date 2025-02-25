using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UI.Components.Combos;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.User;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.PairActions;

public sealed class PairTriggerCombo : CkFilterComboIconButton<LightTrigger>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;

    public PairTriggerCombo(Pair pairData, MainHub mainHub, ILogger log, UiSharedService uiShared, string bText, string bTT)
        : base(() => [
            .. pairData.LastLightStorage.Triggers.OrderBy(x => x.Label),
        ], log, uiShared, FontAwesomeIcon.Bell, bText, bTT)
    {
        _mainHub = mainHub;
        _pairRef = pairData;

        // update current selection to the last registered LightTrigger from that pair on construction.
        if (_pairRef.LastToyboxData is not null && _pairRef.LastLightStorage is not null)
            CurrentSelection = _pairRef.LastLightStorage.Triggers.FirstOrDefault();
    }

    // override the method to extract items by extracting all LightTriggers.
    protected override bool DisableCondition() => _pairRef.PairPerms.ToggleTriggers is false;

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalAlarmIdx, bool selected)
    {
        var trigger = Items[globalAlarmIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(trigger.Label, selected);

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
        _uiShared.IconText(FontAwesomeIcon.InfoCircle, ImGuiColors.TankBlue);
        DrawItemTooltip(trigger);

        return ret;
    }

    protected override void OnButtonPress()
    {
        if (CurrentSelection is null)
            return;

        // Construct the dto, and then send it off.
        var dto = new PushPairToyboxDataUpdateDto(_pairRef.UserData, _pairRef.LastToyboxData, DataUpdateType.TriggerToggled)
        {
            AffectedIdentifier = CurrentSelection.Id,
        };
        _mainHub.UserPushPairDataToybox(dto).ConfigureAwait(false);
        PairCombos.Opened = InteractionType.None;
        Log.LogDebug("Toggling Trigger " + CurrentSelection.Label + " on " + _pairRef.GetNickAliasOrUid() + "'s TriggerList", LoggerType.Permissions);
    }

    private void DrawItemTooltip(LightTrigger item)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            ImGui.BeginTooltip();

            // draw trigger description
            if (!item.Desc.IsNullOrWhitespace())
            {
                // push the text wrap position to the font size times 35
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                // we will then check to see if the text contains a tooltip
                if (item.Desc.Contains(UiSharedService.TooltipSeparator, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = item.Desc.Split(UiSharedService.TooltipSeparator, StringSplitOptions.None);
                    // for each of the split text, we will display the text unformatted
                    for (var i = 0; i < splitText.Length; i++)
                    {
                        ImGui.TextUnformatted(splitText[i]);
                        if (i != splitText.Length - 1) ImGui.Separator();
                    }
                }
                else
                {
                    ImGui.TextUnformatted(item.Desc);
                }
                // finally, pop the text wrap position
                ImGui.PopTextWrapPos();
                ImGui.Separator();
            }

            // draw trigger priority
            UiSharedService.ColorText("Priority:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.Text(item.Priority.ToString());

            // Draw the alarm time.
            UiSharedService.ColorText("Trigger Kind:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.TextUnformatted(item.Type.ToName());

            UiSharedService.ColorText("Action Kind Performed:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.Text(item.ActionOnTrigger.ToName());

            ImGui.EndTooltip();
        }
    }
}

