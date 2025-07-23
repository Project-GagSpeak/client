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

namespace GagSpeak.CustomCombos.Pairs;

public sealed class PairTriggerCombo : CkFilterComboIconButton<KinksterTrigger>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _ref;

    public PairTriggerCombo(ILogger log, MainHub hub, Kinkster kinkster, Action postButtonPress)
        : base(log, FAI.Bell, "Enable", () => [ .. kinkster.LightCache.Triggers.Values.OrderBy(x => x.Label)])
    {
        _mainHub = hub;
        _ref = kinkster;
        PostButtonPress = postButtonPress;
    }

    // override the method to extract items by extracting all LightTriggers.
    protected override bool DisableCondition() => _ref.PairPerms.ToggleTriggers is false;

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
        var dto = new PushKinksterActiveTriggers(_ref.UserData,  _ref.ActiveTriggers, Current.Id, DataUpdateType.TriggerToggled);
        var result = await _mainHub.UserChangeKinksterActiveTriggers(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform TriggerToggle on {_ref.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
            PostButtonPress?.Invoke();
            return false;
        }
        else
        {
            Log.LogDebug($"Toggling Trigger {Current.Label} on {_ref.GetNickAliasOrUid()}'s TriggerList", LoggerType.StickyUI);
            PostButtonPress?.Invoke();
            return true;
        }
    }

    private void DrawItemTooltip(KinksterTrigger item)
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
                if (item.Description.Contains(CkGui.TipSep, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = item.Description.Split(CkGui.TipSep, StringSplitOptions.None);
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
            ImGui.TextUnformatted(item.Kind.ToName());

            CkGui.ColorText("Action Kind Performed:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.Text(item.ActionType.ToName());

            ImGui.EndTooltip();
        }
    }
}

