using CkCommons;
using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Pairs;

public sealed class PairTriggerCombo : CkFilterComboIconTextButton<KinksterTrigger>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _ref;

    public PairTriggerCombo(ILogger log, MainHub hub, Kinkster kinkster, Action postButtonPress)
        : base(log, FAI.Bell, () => [ .. kinkster.LightCache.Triggers.Values.OrderBy(x => x.Label)])
    {
        _mainHub = hub;
        _ref = kinkster;
        PostButtonPress = postButtonPress;
    }

    public string TrueLabel = "Enable";
    public string FalseLabel = "Disable";

    // override the method to extract items by extracting all LightTriggers.
    protected override bool DisableCondition() 
        => !_ref.PairPerms.ToggleTriggers;

    protected override string ToString(KinksterTrigger obj)
        => obj.Label;

    public bool Draw(string label, float width, string tooltipSuffix)
    {
        var state = _ref.ActiveTriggers.Contains(Current?.Id ?? Guid.Empty);
        var buttonText = state ? FalseLabel : TrueLabel;
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
        var trigger = Items[globalAlarmIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(trigger.Label, selected);

        var isEnabled = _ref.ActiveTriggers.Contains(trigger.Id);
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - _infoIconWidth - _powerIconWidth - ImGui.GetStyle().ItemInnerSpacing.X);
        CkGui.IconText(FAI.PowerOff, isEnabled ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey);
        CkGui.AttachToolTip($"Trigger is currently {(isEnabled ? "Enabled" : "Disabled")}");

        ImUtf8.SameLineInner();
        CkGui.HoverIconText(FAI.InfoCircle, ImGuiColors.TankBlue.ToUint(), ImGui.GetColorU32(ImGuiCol.TextDisabled));
        DrawItemTooltip(trigger);
        return ret;
    }

    protected override void OnButtonPress()
    {
        if (Current is null)
            return;

        var triggers = new List<Guid>(_ref.ActiveTriggers);
        if (!triggers.Remove(Current.Id))
            triggers.Add(Current.Id);

        UiService.SetUITask(async () =>
        {
            // Construct the dto, and then send it off.
            var dto = new PushKinksterActiveTriggers(_ref.UserData, triggers, Current.Id, DataUpdateType.TriggerToggled);
            var result = await _mainHub.UserChangeKinksterActiveTriggers(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform TriggerToggle on {_ref.GetNickAliasOrUid()}, Reason:{result.ErrorCode}", LoggerType.StickyUI);
                PostButtonPress?.Invoke();
            }
            else
            {
                Log.LogDebug($"Toggling Trigger {Current.Label} on {_ref.GetNickAliasOrUid()}'s TriggerList", LoggerType.StickyUI);
                PostButtonPress?.Invoke();
            }
        });
    }

    private void DrawItemTooltip(KinksterTrigger item)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
                .Push(ImGuiStyleVar.WindowRounding, 4f)
                .Push(ImGuiStyleVar.PopupBorderSize, 1f);
            using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            ImGui.BeginTooltip();

            CkGui.ColorText("Description:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            if (string.IsNullOrWhiteSpace(item.Description))
                ImGui.TextUnformatted("<None Provided>");
            else
                CkGui.WrappedTooltipText(item.Description, 35f, ImGuiColors.ParsedPink);

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

