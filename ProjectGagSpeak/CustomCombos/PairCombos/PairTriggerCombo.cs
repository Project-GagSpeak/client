using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.PairActions;

public sealed class PairTriggerCombo : CkFilterComboIconButton<LightTrigger>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;

    public PairTriggerCombo(Pair pair, MainHub hub, ILogger log)
        : base([ .. pair.LastLightStorage.Triggers.OrderBy(x => x.Label)], log, FAI.Bell, "Enable")
    {
        _mainHub = hub;
        _pairRef = pair;

        // update current selection to the last registered LightTrigger from that pair on construction.
        Current = _pairRef.LastLightStorage?.Triggers.FirstOrDefault();
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
        CkGui.IconText(FAI.InfoCircle, ImGuiColors.TankBlue);
        DrawItemTooltip(trigger);

        return ret;
    }

    protected override async Task<bool> OnButtonPress()
    {
        if (Current is null)
            return false;

        // Construct the dto, and then send it off.
        var dto = new PushPairToyboxDataUpdateDto(_pairRef.UserData, _pairRef.LastToyboxData, DataUpdateType.TriggerToggled)
        {
            AffectedIdentifier = Current.Id,
        };

        // send the dto to the server.
        var result = await _mainHub.UserPushPairDataToybox(dto);
        if (result is not GsApiPairErrorCodes.Success)
        {
            Log.LogDebug($"Failed to perform TriggerToggle on {_pairRef.GetNickAliasOrUid()}, Reason:{LoggerType.Permissions}");
            return false;
        }
        else
        {
            Log.LogDebug($"Toggling Trigger {Current.Label} on {_pairRef.GetNickAliasOrUid()}'s TriggerList", LoggerType.Permissions);
            return true;
        }
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
                if (item.Description.Contains(CkGui.TooltipSeparator, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = item.Description.Split(CkGui.TooltipSeparator, StringSplitOptions.None);
                    // for each of the split text, we will display the text unformatted
                    for (var i = 0; i < splitText.Length; i++)
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
            CkGui.ColorText("Priority:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.Text(item.Priority.ToString());

            // Draw the alarm time.
            CkGui.ColorText("Trigger Kind:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.TextUnformatted(item.Type.ToName());

            CkGui.ColorText("Action Kind Performed:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.Text(item.ActionOnTrigger.ToName());

            ImGui.EndTooltip();
        }
    }
}

