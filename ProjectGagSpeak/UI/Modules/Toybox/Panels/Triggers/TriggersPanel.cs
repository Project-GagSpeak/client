using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Tutorial;
using GagSpeak.Triggers;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Dto;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.UiToybox;

public partial class TriggersPanel
{
    private readonly ILogger<TriggersPanel> _logger;
    private readonly TriggerFileSelector _selector;
    private readonly TriggerManager _manager;
    private readonly TutorialService _guides;

    private RestraintCombo _restraintCombo;
    private RestrictionCombo _restrictionCombo;
    private RestrictionGagCombo _gagCombo;
    private JobActionCombo _jobActionCombo;
    private EmoteCombo _emoteCombo;
    private MoodleStatusCombo _moodleStatusCombo;
    private MoodlePresetCombo _moodlePresetCombo;
    private PatternCombo _patternCombo;

    public TriggersPanel(
        ILogger<TriggersPanel> logger,
        TriggerFileSelector selector,
        TriggerManager manager,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _manager = manager;
        _guides = guides;
    }

    private TriggerTab _selectedTab = TriggerTab.Detection;

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, ToyboxTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("TriggersTL", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("TriggersBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("TriggersTR", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawTriggerInfo(drawRegions.BotRight, curveSize);
    }

    private void DrawTriggerInfo(CkHeader.DrawRegion region, float curveSize)
    {
        DrawSelectedTrigger(region);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawSelectedTrigger(CkHeader.DrawRegion region)
    {
        var labelSize = new Vector2(region.SizeX * .7f, ImGui.GetFrameHeight());

        // Draw either the interactable label child, or the static label.
        if (_selector.Selected is null)
        {
            using var _ = CkRaii.LabelChildText(region.Size, labelSize, "No Trigger Selected!",
                ImGui.GetStyle().WindowPadding.X, ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersRight);
        }
        else
        {
            DrawSelectedDisplay(region, labelSize);
        }
    }

    private void DrawSelectedDisplay(CkHeader.DrawRegion region, Vector2 labelSize)
    {
        var item = _manager.ItemInEditor is { } editorItem ? editorItem : _selector.Selected!;
        var IsEditorItem = item.Identifier == _manager.ItemInEditor?.Identifier;

        // Styles shared throughout all draw settings.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, CkColor.FancyHeaderContrast.Uint());

        // Begin the child constraint.
        using (var c = CkRaii.Child("Sel_Outer", region.Size, CkColor.FancyHeader.Uint(), ImGui.GetFrameHeight(), DFlags.RoundCornersRight))
        {
            var minPos = ImGui.GetItemRectMin();
            DrawSelectedHeader(labelSize, item, IsEditorItem);

            ImGui.SetCursorScreenPos(minPos with { Y = ImGui.GetItemRectMax().Y });
            DrawTabSelector();

            ImGui.SetCursorScreenPos(minPos with { Y = ImGui.GetItemRectMax().Y });
            DrawSelectedBody(item, IsEditorItem);

            ImGui.SetCursorScreenPos(minPos);
            DrawLabelWithToggle(labelSize, item, IsEditorItem);
        }
    }

    private void DrawSelectedHeader(Vector2 region, Trigger trigger, bool isEditorItem)
    {
        var descH = ImGui.GetTextLineHeightWithSpacing() * 2;
        var height = descH.AddWinPadY() + ImGui.GetFrameHeightWithSpacing();
        var bgCol = CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        using var _ = CkRaii.ChildPaddedW("Sel_Header", ImGui.GetContentRegionAvail().X, height, bgCol, ImGui.GetFrameHeight(), DFlags.RoundCornersTopRight);

        // Dummy is a placeholder for the label area drawn afterward.
        ImGui.Dummy(region + new Vector2(CkRaii.GetFrameThickness()) - ImGui.GetStyle().ItemSpacing - ImGui.GetStyle().WindowPadding / 2);
        ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.X * 2);
        DrawPrioritySetter(trigger, isEditorItem);

        DrawDescription(trigger, isEditorItem);
    }

    private void DrawTabSelector()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        var wdl = ImGui.GetWindowDrawList();
        var width = ImGui.GetContentRegionAvail().X;
        var stripSize = new Vector2(width, CkRaii.GetFrameThickness());
        var tabSize = new Vector2(width / 2, ImGui.GetFrameHeight());
        var textYOffset = (ImGui.GetFrameHeight() - ImGui.GetTextLineHeight()) / 2;

        // Top Strip.
        ImGui.Dummy(stripSize);
        wdl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.ElementSplit.Uint());
        // Left Button.
        if (ImGui.InvisibleButton("tab_left", tabSize))
            _selectedTab = TriggerTab.Detection;

        DrawButtonText("Detection", TriggerTab.Detection);

        // Right Button.
        ImGui.SameLine();
        if (ImGui.InvisibleButton("tab_right", tabSize))
            _selectedTab = TriggerTab.Action;

        DrawButtonText("Applied Action", TriggerTab.Action);

        // Bot Strip.
        ImGui.Dummy(stripSize);
        wdl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.ElementSplit.Uint());

        void DrawButtonText(string text, TriggerTab tab)
        {
            var min = ImGui.GetItemRectMin();
            var col = ImGui.IsItemHovered()
                ? CkColor.VibrantPinkHovered 
                : (_selectedTab == tab ? CkColor.VibrantPink : CkColor.FancyHeaderContrast);
            wdl.AddRectFilled(min, ImGui.GetItemRectMax(), col.Uint());
            var textPos = min + new Vector2((tabSize.X - ImGui.CalcTextSize(text).X) / 2, textYOffset);
            wdl.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);
        }
    }

    private void DrawSelectedBody(Trigger trigger, bool isEditorItem)
    {
        using var bodyChild = CkRaii.Child("Sel_Body", ImGui.GetContentRegionAvail(), WFlags.AlwaysUseWindowPadding);

        if (_selectedTab is TriggerTab.Detection)
        {
            ImGui.Spacing();
            DrawTriggerTypeSelector(bodyChild.InnerRegion.X / 2, trigger, isEditorItem);
            CkGui.SeparatorSpaced(col: CkColor.FancyHeaderContrast.Uint());
            DrawDetectionInfo(trigger, isEditorItem);
        }
        else
        {
            ImGui.Spacing();
            DrawTriggerActionType(bodyChild.InnerRegion.X / 2, trigger, isEditorItem);
            CkGui.SeparatorSpaced(col: CkColor.FancyHeaderContrast.Uint());
            DrawActionInfo(trigger, isEditorItem);
        }

        DrawFooter(trigger);
    }

    private void DrawDetectionInfo(Trigger trigger, bool isEditorItem)
    {
        // What we draw, should be based on what triggerkind it is.
        switch (trigger)
        {
            case SpellActionTrigger spellAct:
                DrawSpellActionTrigger(spellAct);
                break;
            case HealthPercentTrigger healthPerc:
                DrawHealthPercentTrigger(healthPerc);
                break;
            case RestraintTrigger restraint:
                DrawRestraintTrigger(restraint);
                break;
            case RestrictionTrigger restriction:
                DrawRestrictionTrigger(restriction);
                break;
            case GagTrigger gag:
                DrawGagTrigger(gag);
                break;
            case SocialTrigger social:
                DrawSocialTrigger(social);
                break;
            case EmoteTrigger emote:
                DrawEmoteTrigger(emote);
                break;
        }

        void DrawSpellActionTrigger(SpellActionTrigger spellAct)
        {
            // Action Kind.
            CkGui.FramedIconText(FAI.Sitemap);
            ImGui.SameLine();
            var width = ImGui.GetContentRegionAvail().X / 3;
            if(CkGuiUtils.EnumCombo("##ActionType", width, spellAct.ActionKind, out var newVal, af => af.ToName()))
                spellAct.ActionKind = newVal;
            CkGui.HelpText("The action property type to detect." +
                "--SEP--Effects like shields or regen, that cast no heal value, do not count as heals.");

            // Display the Job selection and Action Selection Combo.
            // N I G H T M A R E (to later please save your sanity)

            // Determine how we draw out the rest of this based on the action type:
            switch (spellAct.ActionKind)
            {
                case LimitedActionEffectType.Miss:
                case LimitedActionEffectType.Attract1:
                case LimitedActionEffectType.Knockback:
                    DrawSpellDirection(ImGui.GetContentRegionAvail().X / 2, spellAct, isEditorItem);
                    return;
                case LimitedActionEffectType.BlockedDamage:
                case LimitedActionEffectType.ParriedDamage:
                case LimitedActionEffectType.Damage:
                case LimitedActionEffectType.Heal:
                    DrawSpellDirection(ImGui.GetContentRegionAvail().X/2, spellAct, isEditorItem);
                    DrawThresholds(ImGui.GetContentRegionAvail().X, spellAct, isEditorItem);
                    return;
            }
        }

        void DrawHealthPercentTrigger(HealthPercentTrigger healthPerc)
        {
            // Player to Track.
            CkGui.FramedIconText(FAI.Eye);

            ImUtf8.SameLineInner();
            var nameRef = healthPerc.PlayerNameWorld;
            if (DrawNameWorldField(ImGui.GetContentRegionAvail().X, ref nameRef, "The Monitored Player.", isEditorItem))
                healthPerc.PlayerNameWorld = nameRef;

            // If we use % based values.
            CkGui.FramedIconText(FAI.Filter);
            
            var usePercent = healthPerc.UsePercentageHealth;
            ImUtf8.SameLineInner();
            if (ImGui.Checkbox("Prefer Percentage", ref usePercent))
                healthPerc.UsePercentageHealth = usePercent;
            CkGui.HelpText("Calculates HP differences in % to overall HP, over fixed values." +
                "--SEP--Otherwise, listens for when it goes above or below a health range.");

            // The Pass Kind.
            CkGui.FramedIconText(FAI.Flag);

            ImUtf8.SameLineInner();
            if (CkGuiUtils.EnumCombo("Pass Type", ImGui.CalcTextSize("undermmm").X, healthPerc.PassKind, out var newVal))
                healthPerc.PassKind = newVal;
            CkGui.HelpText("Should Detection be true upon passing above or below the defined threshold?");

            // Draw out thresholds based on type.
            CkGui.FramedIconText(FAI.Percent);

            ImUtf8.SameLineInner();
            if (healthPerc.UsePercentageHealth)
            {
                var tt = "The Health % difference that must be crossed to activate the trigger.";
                DrawThresholdPercent(ImGui.GetContentRegionAvail().X, healthPerc, isEditorItem, tt);
            }
            else
            {
                var ttLower = "The lowest HP that is \"In Range\"." +
                    "--SEP--HP Dropping below the Minimum threshold is considered \"Passing Under\"";
                var ttUpper = "The highest HP that is \"In Range\"." +
                    "--SEP--Health that increases past this Max Threshold is considered \"Passing Over\"";
                DrawThresholds(ImGui.GetContentRegionAvail().X, healthPerc, isEditorItem, ttLower, ttUpper, "Min. %d%HP", "Max. %d%HP");
            }
        }

        void DrawRestraintTrigger(RestraintTrigger restraint)
        {
            // stuff for restraint trigger.
        }

        void DrawRestrictionTrigger(RestrictionTrigger restriction)
        {
            // stuff for restriction trigger.
        }

        void DrawGagTrigger(GagTrigger gag)
        {
            // stuff for gag trigger.
        }

        void DrawSocialTrigger(SocialTrigger social)
        {
            // stuff for social trigger.
        }

        void DrawEmoteTrigger(EmoteTrigger emote)
        {
            // stuff for emote trigger.
        }
    }

    private void DrawActionInfo(Trigger trigger, bool isEditorItem)
    {
        // What we draw, should be based on what triggerkind it is.
        switch (trigger.InvokableAction)
        {
            case TextAction textAct:
                // stuff for text responce action.
                break;
            case GagAction gagAct:
                // stuff for gag action.
                break;
            case RestraintAction restraintAct:
                // stuff for restraint action.
                break; 
            case RestrictionAction restrictionAct:
                // stuff for restriction action.
                break;
            case MoodleAction moodleAct:
                // stuff for moodle action.
                break;
            case PiShockAction shockAct:
                // stuff for shock collar action.
                break;
            case SexToyAction sexToyAct:
                // stuff for toys.
                break;
        }

        void DrawTextAction(TextAction textAct)
        {
            // stuff for text action.
        }

        void DrawGagAction(GagAction gagAct)
        {
            // stuff for gag action.
        }

        void DrawRestraintAction(RestraintAction restraintAct)
        {
            // stuff for restraint action.
        }

        void DrawRestrictionAction(RestrictionAction restrictionAct)
        {
            // stuff for restriction action.
        }

        void DrawMoodleAction(MoodleAction moodleAct)
        {
            // stuff for moodle action.
        }

        void DrawPiShockAction(PiShockAction shockAct)
        {
            // stuff for shock collar action.
        }

        void DrawToyAction(SexToyAction sexToyAct)
        {
            // stuff for toys.
        }
    }
}
