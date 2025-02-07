using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Data;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.Components.Combos;

public sealed class PairTriggerCombo : GagspeakComboToggleButtonBase<LightTrigger>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;

    public PairTriggerCombo(Pair pairData, MainHub mainHub, ILogger log, UiSharedService uiShared, string bOnText, string bOnTT, string bOffText, string bOffTT)
        : base(log, uiShared, bOnText, bOnTT, bOffText, bOffTT)
    {
        _mainHub = mainHub;
        _pairRef = pairData;

        // update current selection to the last registered LightTrigger from that pair on construction.
        if (_pairRef.LastToyboxData is not null && _pairRef.LastLightStorage is not null)
            CurrentSelection = _pairRef.LastLightStorage.Triggers.FirstOrDefault();
    }

    // override the method to extract items by extracting all LightTriggers.
    protected override IReadOnlyList<LightTrigger> ExtractItems() => _pairRef.LastLightStorage?.Triggers ?? new List<LightTrigger>();
    protected override string ToItemString(LightTrigger item) => item.Name;
    protected override bool DisableCondition() => _pairRef.PairPerms.CanToggleTriggers is false;

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(LightTrigger triggerItem, bool selected)
    {
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(triggerItem.Name, selected);

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
        _uiShared.IconText(FontAwesomeIcon.InfoCircle, ImGuiColors.TankBlue);
        DrawItemTooltip(triggerItem);

        return ret;
    }

    protected override void OnButtonPress()
    {
        if (_pairRef.LastToyboxData is null || CurrentSelection is null) return;

        // clone the toybox data.
        var newToyboxData = _pairRef.LastToyboxData.DeepClone();
        // update the interaction ID.
        newToyboxData.InteractionId = CurrentSelection.Identifier;

        // if it was active, deactivate it, otherwise, activate it.
        if (newToyboxData.ActiveTriggers.Contains(CurrentSelection.Identifier)) 
            newToyboxData.ActiveTriggers.Remove(CurrentSelection.Identifier);
        else 
            newToyboxData.ActiveTriggers.Add(CurrentSelection.Identifier);

        // Send out the command.
        _ = _mainHub.UserPushPairDataToyboxUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, newToyboxData, ToyboxUpdateType.TriggerToggled, UpdateDir.Other));
        PairCombos.Opened = InteractionType.None;
        _logger.LogDebug("Toggling Trigger " + CurrentSelection.Name + " on " + _pairRef.GetNickAliasOrUid() + "'s TriggerList", LoggerType.Permissions);
    }

    protected override bool IsToggledOn(LightTrigger? selection)
    {
        if (selection is null || _pairRef.LastToyboxData is null) return false;
        // if the select is not present in the LastToyboxData's ActiveTriggers list, it is not on.
        return _pairRef.LastToyboxData.ActiveTriggers.Contains(selection.Identifier);
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
            if (!item.Description.IsNullOrWhitespace())
            {
                // push the text wrap position to the font size times 35
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                // we will then check to see if the text contains a tooltip
                if (item.Description.Contains(UiSharedService.TooltipSeparator, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = item.Description.Split(UiSharedService.TooltipSeparator, StringSplitOptions.None);
                    // for each of the split text, we will display the text unformatted
                    for (int i = 0; i < splitText.Length; i++)
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

            // draw trigger priority
            UiSharedService.ColorText("Priority:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.Text(item.Priority.ToString());

            // Draw the alarm time.
            UiSharedService.ColorText("Trigger Kind:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.TextUnformatted(item.Type.TriggerKindToString());

            UiSharedService.ColorText("Action Kind Performed:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.Text(item.ActionOnTrigger.ToName());

            ImGui.EndTooltip();
        }
    }
}

