using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Localization;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using OtterGui.Text;
using System.Drawing;
using TerraFX.Interop.Windows;
using static FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.DynamicEvent.Delegates;

namespace GagSpeak.Gui.Wardrobe;

// Independant window for Triggers.
public class TriggersUI : WindowMediatorSubscriberBase
{
    // Revamp this later.
    private static bool THEME_PUSHED = false;

    private readonly TriggerFileSelector _selector;
    private readonly DetectionDrawer _detections;
    private readonly ReactionsDrawer _reactions;
    private readonly TriggerManager _manager;
    private readonly TutorialService _guides;

    // Help swap between the different parts of a trigger.
    private TriggerPartTabs _componentTabs = new();

    public TriggersUI(ILogger<TriggersUI> logger, GagspeakMediator mediator, TriggerFileSelector selector,
        DetectionDrawer detections, ReactionsDrawer reactions, TriggerManager manager, TutorialService guides)
        : base(logger, mediator, "Triggers ###GagSpeakTriggers")
    {
        _selector = selector;
        _detections = detections;
        _reactions = reactions;
        _manager = manager;
        _guides = guides;

        this.PinningClickthroughFalse();
        this.SetBoundaries(new(640, 490), ImGui.GetIO().DisplaySize);
        TitleBarButtons = new TitleBarButtonBuilder().AddTutorial(_guides, TutorialType.Puppeteer).Build();
    }

    // Accessed by Tutorial System
    public static Vector2 LastPos { get; private set; } = Vector2.Zero;
    public static Vector2 LastSize { get; private set; } = Vector2.Zero;

    protected override void PreDrawInternal()
    {
        if (!THEME_PUSHED)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .403f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.428f));
            THEME_PUSHED = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (THEME_PUSHED)
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(2);
            THEME_PUSHED = false;
        }
    }

    protected override void DrawInternal()
    {
        LastPos = ImGui.GetWindowPos();
        LastSize = ImGui.GetWindowSize();

        var regions = CkHeader.FlatWithBends(CkCol.CurvedHeader.Uint(), ImUtf8.FrameHeight * .5f, ImUtf8.ItemSpacing.X, ImUtf8.FrameHeight);

        ImGui.SetCursorScreenPos(regions.BotLeft.Pos);
        using var _ = CkRaii.Child("drawspace", regions.BotSize, wFlags: WFlags.AlwaysUseWindowPadding);

        var leftW = regions.BotSize.X * .45f;
        using (var list = CkRaii.FramedChildPaddedWH("list", new(leftW, ImGui.GetContentRegionAvail().Y), 0, GsCol.VibrantPink.Uint(), FancyTabBar.Rounding))
        {
            _selector.DrawFilterRow(list.InnerRegion.X);
            _selector.DrawList(list.InnerRegion.X);
        }

        ImGui.SameLine();
        using (var content = CkRaii.FramedChildPaddedWH("content", ImGui.GetContentRegionAvail(), 0, GsCol.VibrantPink.Uint(), FancyTabBar.Rounding))
        {
            if (_manager.ItemInEditor is { } item)
                DrawTriggerEditor(content.InnerRegion, item, FancyTabBar.Rounding);
            else
                DrawTrigger(content.InnerRegion, FancyTabBar.Rounding);
        }
    }

    private void DrawTrigger(Vector2 region, float rounding)
    {
        if (_selector.Selected is not { } trigger)
            return;

        CkGui.BooleanToColoredIcon(trigger.Enabled, false);
        CkGui.AttachToolTip("If this alias is enabled.--SEP----COL--Click to toggle!--COL--", ImGuiColors.ParsedPink);
        if (ImGui.IsItemClicked())
        {
            _manager.ToggleState(trigger);
            // Do state toggles here later.
        }
        CkGui.AttachToolTip($"{(trigger.Enabled ? "Disable" : "Enable")} this Trigger.");

        CkGui.TextFrameAlignedInline(trigger.Label);
        ImGui.SameLine(0, ImUtf8.ItemSpacing.X * 2);
        CkGui.TagLabelTextFrameAligned($"Priority: {trigger.Priority}", GsCol.SideButtonBG.Vec4Ref(), 6 * ImGuiHelpers.GlobalScale);

        // Shift for the editor
        var endX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        ImGui.SameLine(endX -= CkGui.IconButtonSize(FAI.Edit).X);
        if (CkGui.IconButton(FAI.Edit, inPopup: true))
            _manager.StartEditing(trigger);
        CkGui.AttachToolTip("Edit this Trigger.");
        _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.EditingTrigger, LastPos, LastSize, () => _manager.StartEditing(trigger));

        // Next line draw out the description area.
        CkGui.FramedIconText(FAI.InfoCircle);
        ImGui.SameLine();
        CkGui.ColorTextWrapped(trigger.Description.IsNullOrEmpty() ? "No Description Given..." : trigger.Description, ImGuiColors.DalamudGrey2);

        ImGui.Separator();
        _componentTabs.Draw(region.X);
        ImGui.Separator();
        if (_componentTabs.TabSelection is TriggerPartTabs.SelectedTab.Detection)
        {
            // get offset for drawn space.
            var comboW = region.X * .65f;
            var offset = (region.X - comboW) / 2;
            ImGui.Spacing();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            if (CkGuiUtils.EnumCombo("##DetectionType", comboW, trigger.Type, out var newVal, _ => _.ToName(), flags: CFlags.None))
            {
                if (newVal != trigger.Type)
                {
                    _logger.LogInformation($"Trigger Type changed from {trigger.Type} to {newVal}");
                    _manager.ChangeTriggerType(trigger, newVal);
                }
            }
            CkGui.AttachToolTip("The detection method used to invoke this trigger");

            switch (trigger)
            {
                case SpellActionTrigger sp: _detections.DrawSpellAction(sp); break;
                case HealthPercentTrigger hp: _detections.DrawHealthPercent(hp); break;
                case RestraintTrigger rt: _detections.DrawRestraint(rt); break;
                case RestrictionTrigger rst: _detections.DrawRestriction(rst); break;
                case GagTrigger gt: _detections.DrawGag(gt); break;
                case SocialTrigger st: _detections.DrawSocial(st); break;
                case EmoteTrigger et: _detections.DrawEmote(et); break;
            }
        }
        else
        {
            // get offset for drawn space.
            var comboW = region.X * .65f;
            var offset = (region.X - comboW) / 2;
            ImGui.Spacing();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            if (CkGuiUtils.EnumCombo("##action-type", comboW, trigger.ActionType, out var newVal, _ => _.ToName(), flags: CFlags.None))
            {
                if (newVal != trigger.ActionType)
                {
                    trigger.InvokableAction = newVal switch
                    {
                        InvokableActionType.TextOutput => new TextAction(),
                        InvokableActionType.Gag => new GagAction(),
                        InvokableActionType.Restriction => new RestrictionAction(),
                        InvokableActionType.Restraint => new RestraintAction(),
                        InvokableActionType.Moodle => new MoodleAction(),
                        InvokableActionType.ShockCollar => new PiShockAction(),
                        _ => new SexToyAction(),
                    };
                }
            }
            CkGui.AttachToolTip("What action occurs when this trigger's detection is met.");

            switch (trigger.InvokableAction)
            {
                case TextAction ta: _reactions.DrawText(ta); break;
                case GagAction ga: _reactions.DrawGag(ga); break;
                case RestrictionAction rsa: _reactions.DrawRestriction(rsa); break;
                case RestraintAction rta: _reactions.DrawRestraint(rta); break;
                case MoodleAction ma: _reactions.DrawMoodle(ma); break;
                case PiShockAction ps: _reactions.DrawShock(ps); break;
                case SexToyAction sta: _reactions.DrawToy(sta); break;
            }
        }
    }

    private void DrawTriggerEditor(Vector2 region, Trigger trigger, float rounding)
    {
        var sliderArea = 75f * ImGuiHelpers.GlobalScale;
        var rightW = sliderArea + CkGui.IconButtonSize(FAI.Redo).X + CkGui.IconButtonSize(FAI.Save).X + ImUtf8.ItemInnerSpacing.X * 2;
        
        // Checkbox with label, then shift right and draw revert/save
        var enabled = trigger.Enabled;
        if (ImGui.Checkbox("##state", ref enabled))
            trigger.Enabled = !trigger.Enabled;
        CkGui.AttachToolTip("If this can be detected as an alias");

        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - rightW);
        var label = trigger.Label;
        if (ImGui.InputTextWithHint("##name", "Display Name..", ref label, 64))
            trigger.Label = label;
        CkGui.AttachToolTip("The UI display name for the Trigger");
        
        ImUtf8.SameLineInner();
        var priority = trigger.Priority;
        ImGui.SetNextItemWidth(sliderArea);
        if (ImGui.DragInt("##priority", ref priority, 1.0f, 0, 100, "%d"))
            trigger.Priority = priority;

        var endX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        ImUtf8.SameLineInner();
        if (CkGui.IconButton(FAI.Undo, inPopup: true))
            _manager.StopEditing();
        CkGui.AttachToolTip("Reverts any changes and exits the editor");
        ImGui.SameLine(0, 0);
        if (CkGui.IconButton(FAI.Save, inPopup: true))
            _manager.SaveChangesAndStopEditing();
        CkGui.AttachToolTip("Saves all changes and exits the editor");

        // Description area
        CkGui.FramedIconText(FAI.InfoCircle);
        ImUtf8.SameLineInner();
        var desc = trigger.Description;
        var mutliLineSize = new Vector2(ImGui.GetContentRegionAvail().X, ImUtf8.TextHeightSpacing * 2);
        if (ImGui.InputTextMultiline("##description", ref desc, 256, ImGui.GetContentRegionAvail()))
            trigger.Description = desc;
        // Draw a hint if no text is present.
        if (string.IsNullOrWhiteSpace(trigger.Description))
            ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, 0xFFBBBBBB, "yell Hello World!");

        ImGui.Separator();
        _componentTabs.Draw(region.X);
        ImGui.Separator();
        if (_componentTabs.TabSelection is TriggerPartTabs.SelectedTab.Detection)
        {
            switch (trigger)
            {
                case SpellActionTrigger sp: _detections.DrawSpellActionEditor(sp); break;
                case HealthPercentTrigger hp: _detections.DrawHealthPercentEditor(hp); break;
                case RestraintTrigger rt: _detections.DrawRestraintEditor(rt); break;
                case RestrictionTrigger rst: _detections.DrawRestrictionEditor(rst); break;
                case GagTrigger gt: _detections.DrawGagEditor(gt); break;
                case SocialTrigger st: _detections.DrawSocialEditor(st); break;
                case EmoteTrigger et: _detections.DrawEmoteEditor(et); break;
            }
        }
        else
        {
            switch (trigger.InvokableAction)
            {
                case TextAction ta: _reactions.DrawTextEditor(ta, ImUtf8.TextHeightSpacing * 3); break;
                case GagAction ga: _reactions.DrawGagEditor(ga); break;
                case RestrictionAction rsa: _reactions.DrawRestrictionEditor(rsa); break;
                case RestraintAction rta: _reactions.DrawRestraintEditor(rta); break;
                case MoodleAction ma: _reactions.DrawMoodleEditor(ma); break;
                case PiShockAction ps: _reactions.DrawShockEditor(ps); break;
                case SexToyAction sta: _reactions.DrawToyEditor(sta); break;
            }
        }
    }
}
