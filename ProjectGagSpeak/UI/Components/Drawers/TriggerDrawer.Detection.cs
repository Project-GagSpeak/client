using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using CkCommons.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Util;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;

// Scoped, sealed class to draw the editor and display components of aliasItems.
public sealed partial class TriggerDrawer : IDisposable
{
    private static readonly string[] ThreeLayerNames = ["Layer 1", "Layer 2", "Layer 3", "Any Layer"];
    private static readonly string[] FiveLayerNames = ["Layer 1", "Layer 2", "Layer 3", "Layer 4", "Layer 5", "Any Layer"];

    private readonly ILogger<TriggerDrawer> _logger;
    private readonly TriggerManager _manager;
    private readonly MoodleDrawer _moodleDrawer;

    private RestraintCombo _restraintCombo;
    private RestrictionCombo _restrictionCombo;
    private PatternCombo _patternCombo;
    private MoodleStatusCombo _statusCombo;
    private MoodlePresetCombo _presetCombo;
    private JobCombo _jobCombo;
    private JobActionCombo _jobActionCombo;
    private EmoteCombo _emoteCombo;

    public TriggerDrawer(
        ILogger<TriggerDrawer> logger,
        GagspeakMediator mediator,
        MoodleDrawer moodleDrawer,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        PatternManager patterns,
        TriggerManager manager,
        FavoritesConfig favorites)
    {
        _logger = logger;
        _manager = manager;
        _moodleDrawer = moodleDrawer;

        _restrictionCombo = new RestrictionCombo(logger, mediator, favorites, () => [
            ..restrictions.Storage.OrderByDescending(p => favorites.Restrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);

        _restraintCombo = new RestraintCombo(logger, mediator, favorites, () => [
            ..restraints.Storage.OrderByDescending(p => favorites.Restraints.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);

        _patternCombo = new PatternCombo(logger, mediator, favorites, () => [
            ..patterns.Storage.OrderByDescending(p => favorites.Patterns.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);

        _statusCombo = new MoodleStatusCombo(logger, 1.15f);
        _presetCombo = new MoodlePresetCombo(logger, 1.15f);

        _jobCombo = new JobCombo(logger, 1.15f);
        _jobActionCombo = new JobActionCombo(logger, 1.15f, () => [
            .. _jobCombo.Current.JobId is not JobType.ADV
                ? SpellActionService.GetJobActions(_jobCombo.Current) ?? []
                : SpellActionService.AllActions.OrderBy(c => c.ParentJob)
        ]);

        _emoteCombo = new EmoteCombo(logger, 1.15f);

        // Listener for refreshing the actions and stuff.
        _jobCombo.SelectionChanged += OnJobSelected;

    }

    void IDisposable.Dispose()
    {
        _jobCombo.SelectionChanged -= OnJobSelected;
        GC.SuppressFinalize(this);
    }

    private void OnJobSelected(LightJob oldJob, LightJob newJob)
    {
        // Refresh the actions based on the selected job.
        _logger.LogTrace($"Changed from ({oldJob.ToString()}) to ({newJob.ToString()}). Refreshing action list.", LoggerType.Triggers);
        _jobActionCombo.RefreshActionList();
    }

    public void DrawDetectionInfo(Trigger trigger, bool isEditorItem, uint searchBg)
    {
        // What we draw, should be based on what triggerkind it is.
        switch (trigger)
        {
            case SpellActionTrigger spellAct:
                DrawSpellActionTrigger(spellAct, isEditorItem, searchBg);
                break;
            
            case HealthPercentTrigger healthPerc:
                DrawHealthPercentTrigger(healthPerc, isEditorItem);
                break;
            
            case RestraintTrigger restraint:
                DrawRestraintTrigger(restraint, isEditorItem, searchBg);
                break;
            
            case RestrictionTrigger restriction:
                DrawRestrictionTrigger(restriction, isEditorItem, searchBg);
                break;
            
            case GagTrigger gag:
                DrawGagTrigger(gag, isEditorItem, searchBg);
                break;
            
            case SocialTrigger social:
                DrawSocialTrigger(social, isEditorItem);
                break;

            case EmoteTrigger emote:
                DrawEmoteTrigger(emote, isEditorItem, searchBg);
                break;
        }
    }

    private void DrawSpellActionTrigger(SpellActionTrigger spellAct, bool isEditorItem, uint searchBg)
    {
        // The Direction of the SpellAction (Who was the action Source, who was the action target?)
        var tooltip = $"Required Direction of the {spellAct.ActionKind}";
        var options = spellAct.GetOptions();
        // Ensure valid option is selected.
        if (!options.Contains(spellAct.Direction))
            spellAct.Direction = options.FirstOrDefault();

        using (CkRaii.InfoRow(FAI.ArrowsLeftRight, "When", tooltip, string.Empty))
        {
            var width = ImGui.GetContentRegionAvail().X;
            CkGuiUtils.FramedEditDisplay("##Direction", width, isEditorItem, spellAct.Direction.GetDirectionText(spellAct.ActionKind), _ =>
            {
                if (CkGuiUtils.EnumCombo("##Dir", width, spellAct.Direction, out var newVal, options, 
                    _ => _.GetDirectionText(spellAct.ActionKind), flags: CFlags.NoArrowButton))
                {
                    spellAct.Direction = newVal;
                }
            });
            CkGui.AttachToolTip(tooltip);
        }

        // Limit who 'other' / 'someone else' is to a single player.
        if(spellAct.Direction is TriggerDirection.SelfToOther or TriggerDirection.Other or TriggerDirection.OtherToSelf)
        {
            var iconTT = "Limit trigger to only accept 'other(s)' as valid if they are the defined player.--SEP--Leave blank to ignore.";
            var comboTT = "The player that replaces 'others' in direction.--SEP--Leave blank to ignore.--SEP--Must use Player Name@World format.";
            using (CkRaii.InfoRow(FAI.Eye, "Limit to", iconTT, string.Empty))
            {
                var width = ImGui.GetContentRegionAvail().X;
                var playerStrRef = spellAct.PlayerNameWorld;
                var label = playerStrRef.IsNullOrEmpty() ? "<No Name@World Set!>" : playerStrRef;
                CkGuiUtils.FramedEditDisplay("##PlayerMonitor", width, isEditorItem, label, _ =>
                {
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputTextWithHint("##PlayerMonitor", "Devious Diva@Balmung..", ref playerStrRef, 68))
                        spellAct.PlayerNameWorld = playerStrRef;
                });
                CkGui.AttachToolTip(comboTT);
            }
        }

        // If this SpellActionTrigger is a generic Detection.
        // Generic Detections means that all Spells/Actions under an ActionType count for detection.
        using (CkRaii.InfoRow(FAI.Filter, "If Generic Detection is for filters, and if so what kind."))
        {
            using var dis = ImRaii.Disabled(!isEditorItem);
            using var style = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1f, !isEditorItem);

            var refVal = spellAct.IsGenericDetection;
            if (ImGui.Checkbox("Use Generic Detection##UseGeneric", ref refVal))
            {
                spellAct.IsGenericDetection = refVal;
                // reset type if false.
                if (!refVal) spellAct.ActionKind = LimitedActionEffectType.Nothing;
            }
            CkGui.AttachToolTip("If the trigger should scan a series of defined actions, or all actions under a catagory?");

            if(spellAct.IsGenericDetection)
            {
                ImGui.SameLine();
                var width = ImGui.GetContentRegionAvail().X;
                CkGuiUtils.FramedEditDisplay("##ActionType", width, isEditorItem, spellAct.ActionKind.ToName(), _ =>
                {
                    if (CkGuiUtils.EnumCombo("##ActionType", width, spellAct.ActionKind, out var newVal, af => af.ToName(), skip: 1))
                        spellAct.ActionKind = newVal;
                });
            }
        }

        // Threshold Values - Determine what damage/heal values have to be met when performing an action qualified by the above filters.
        // These are not nessisary if the ActionEffectType is not related to DPS/Heal/Mitigation values.
        if(spellAct.ActionKind is not LimitedActionEffectType.Miss or LimitedActionEffectType.Knockback or LimitedActionEffectType.Attract1)
        {
            using (CkRaii.InfoRow(FAI.BarsProgress, spellAct.ActionKind is LimitedActionEffectType.Heal ? "Heal Threshold Range" : "DPS Threshold Range"))
            {
                var barWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X) / 2;
                var minRef = spellAct.ThresholdMinValue;
                var formatPost = spellAct.ActionKind is LimitedActionEffectType.Heal ? "HP" : "DPS";
                CkGuiUtils.FramedEditDisplay("##MinThreshold", barWidth, isEditorItem, $"Min. {minRef}{formatPost}", _ =>
                {
                    ImGui.SetNextItemWidth(barWidth);
                    if (ImGui.DragInt("##MinThresSlider", ref minRef, 10.0f, -1, 1000000, $"Min. %d{formatPost}"))
                        spellAct.ThresholdMinValue = minRef;
                });
                CkGui.AttachToolTip("The lowest HP that is \"In Range\".");

                // Ensure that the max HP is not less than the min.
                if (spellAct.ThresholdMaxValue < spellAct.ThresholdMinValue)
                    spellAct.ThresholdMaxValue = spellAct.ThresholdMinValue;

                var maxRef = spellAct.ThresholdMaxValue;
                ImUtf8.SameLineInner();
                CkGuiUtils.FramedEditDisplay("##MaxThreshold", barWidth, isEditorItem, $"Max. {maxRef}{formatPost}", _ =>
                {
                    ImGui.SetNextItemWidth(barWidth);
                    if (ImGui.DragInt("##MaxThresSlider", ref maxRef, 10.0f, minRef, 1000000, $"Max. %d{formatPost}"))
                        spellAct.ThresholdMaxValue = maxRef;
                });
                CkGui.AttachToolTip("The highest HP that is \"In Range\".");
            }
        }

        // DetectableActions - This filter display allows you to append certain jobs and actions you want the trigger to qualify for.
        if(!spellAct.IsGenericDetection)
            DrawDetectableActions(spellAct, isEditorItem, searchBg);
    }

    private JobType _selectedJob = JobType.ADV;
    private uint _selectedAction = uint.MaxValue;
    private void DrawDetectableActions(SpellActionTrigger spellAct, bool isEditorItem, uint searchBg)
    {
        using var _ = ImRaii.Group();

        // Combo Row.
        using (ImRaii.Group())
        {
            var img = MoodleIcon.GetGameIconOrEmpty(SpellActionService.GetLightJob(_selectedJob).GetIconId());
            ImGui.Image(img.Handle, new Vector2(ImGui.GetFrameHeight()));

            ImUtf8.SameLineInner();
            var diff = _jobCombo.Draw("##JobSelector", _selectedJob, ImGui.GetContentRegionAvail().X / 2, 1.25f, searchBg, CFlags.NoArrowButton);
            if (diff && _selectedJob != _jobCombo.Current.JobId)
                _selectedJob = _jobCombo.Current.JobId;
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                _selectedJob = JobType.ADV;
        }
        CkGui.AttachToolTip("The Job to add actions for. Leave blank to add view actions from all jobs.");

        ImUtf8.SameLineInner();
        var jobActChange = _jobActionCombo.Draw("##JobActionSelector", _selectedAction, ImGui.GetContentRegionAvail().X, 1.5f, searchBg);
        if (jobActChange && _selectedAction != _jobActionCombo.Current.ActionID)
        {
            var list = spellAct.StoredActions[_selectedJob] ??= [];
            if (!list.Contains(_jobActionCombo.Current.ActionID))
                list.Add(_jobActionCombo.Current.ActionID);

            // Reset the selected action to default.
            _selectedAction = uint.MaxValue;
        }

        // The icons to display.  
        var iconsToShow = spellAct.StoredActions.Values.SelectMany(a => a).Distinct().ToList();
        var iconSize = new Vector2(ImGui.GetFrameHeight());
        var width = ImGui.GetContentRegionAvail().X;
        using (CkRaii.FramedChildPaddedW("JobActionIcons", width, iconSize.Y * 2, CkColor.FancyHeaderContrast.Uint(), 0))
        {
            if (iconsToShow.Count() <= 0)
                return;

            var padding = ImGui.GetStyle().ItemInnerSpacing.X;
            var iconsPerRow = MathF.Floor((width - padding) / (iconSize.X + padding));

            // display each moodle.
            var currentRow = 0;
            var iconsInRow = 0;
            foreach (var actionIcon in iconsToShow)
            {
                // Prevent invalid draws
                if (!SpellActionService.AllActionsLookup.TryGetValue(actionIcon, out var iconData))
                    continue;

                // Draw the icon.
                ImGui.Image(MoodleIcon.GetGameIconOrEmpty(iconData.IconID).Handle, iconSize);
                if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    // Remove the action from the list.
                    if (spellAct.StoredActions.TryGetValue(_selectedJob, out var actions))
                        actions.Remove(actionIcon);
                }
                CkGui.AttachToolTip("Right-Click me to remove from the list.");
                
                iconsInRow++;
                if (iconsInRow >= iconsPerRow)
                {
                    currentRow++;
                    iconsInRow = 0;
                }
                else
                    ImUtf8.SameLineInner();
                if (currentRow >= 2)
                    break;
            }
        }
    }

    private void DrawHealthPercentTrigger(HealthPercentTrigger healthPerc, bool isEditorItem)
    {
        // Player to Track.
        using (CkRaii.InfoRow(FAI.Eye, string.Empty, "The Monitored Player", "The Monitored Player.--SEP--Must use Player Name@World format."))
        {
            var width = ImGui.GetContentRegionAvail().X;
            var playerStrRef = healthPerc.PlayerNameWorld;
            var label = playerStrRef.IsNullOrEmpty() ? "<No Name@World Set!>" : playerStrRef;
            CkGuiUtils.FramedEditDisplay("##PlayerMonitor", width, isEditorItem, label, _ =>
            {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputTextWithHint("##PlayerMonitor", "Devious Diva@Balmung..", ref playerStrRef, 68))
                    healthPerc.PlayerNameWorld = playerStrRef;
            });
        }

        // If we use % based values.
        using (CkRaii.InfoRow(FAI.Filter, string.Empty, "If Detection is based off % Difference, or HP Values."))
        {
            using var dis = ImRaii.Disabled(!isEditorItem);
            using var style = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1f, !isEditorItem);

            var refVal = healthPerc.UsePercentageHealth;
            if (ImGui.Checkbox("Detect HP % Instead##UsePercent", ref refVal))
                healthPerc.UsePercentageHealth = refVal;
        }

        // The Pass Kind.
        var iconTT = "What Pass-Kind to use for HP Detection.";
        var groupTT = "Under ⇒ When HP Drops below the Min threshold--SEP--Over ⇒ When HP increases past the Max Threshold";
        using (CkRaii.InfoRow(FAI.Flag, "When HP falls", iconTT, groupTT, "the threshold."))
        {
            var width = ImGui.CalcTextSize("underm").X;
            CkGuiUtils.FramedEditDisplay("##PassType", width, isEditorItem, healthPerc.PassKind.ToString(), _ =>
            {
                if (CkGuiUtils.EnumCombo("##PassTypeCombo", width, healthPerc.PassKind, out var newVal, flags: CFlags.NoArrowButton))
                    healthPerc.PassKind = newVal;
            });
        }

        // Draw out thresholds based on type.
        using (CkRaii.InfoRow(FAI.BarsProgress, "The Threshold Range(s)"))
        {
            var isPercent = healthPerc.UsePercentageHealth;
            var barWidth = isPercent ? ImGui.GetContentRegionAvail().X : (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X) / 2;
            // Display based on the value.
            if (isPercent)
            {
                var hpRef = healthPerc.ThresholdMinValue;
                CkGuiUtils.FramedEditDisplay("##Perc_Thres", barWidth, isEditorItem, $"{hpRef}%", _ =>
                {
                    ImGui.SetNextItemWidth(barWidth);
                    if (ImGui.DragInt("##HealthPercentage", ref hpRef, 0.1f, 0, 100, "%d%%"))
                        healthPerc.ThresholdMinValue = hpRef;
                });
                CkGui.AttachToolTip("The Health % difference that must be crossed to activate the trigger.");
            }
            else
            {
                var minHpRef = healthPerc.ThresholdMinValue;
                CkGuiUtils.FramedEditDisplay("##MinThreshold", barWidth, isEditorItem, $"Min. {minHpRef}HP", _ =>
                {
                    ImGui.SetNextItemWidth(barWidth);
                    if (ImGui.DragInt("##MinThresSlider", ref minHpRef, 10.0f, -1, 1000000, "Min. %dHP"))
                        healthPerc.ThresholdMinValue = minHpRef;
                });
                CkGui.AttachToolTip("The lowest HP that is \"In Range\".");

                // Ensure that the max HP is not less than the min.
                if (healthPerc.ThresholdMaxValue < healthPerc.ThresholdMinValue)
                    healthPerc.ThresholdMaxValue = healthPerc.ThresholdMinValue;

                var maxHpRef = healthPerc.ThresholdMaxValue;
                ImUtf8.SameLineInner();
                CkGuiUtils.FramedEditDisplay("##MaxThreshold", barWidth, isEditorItem, $"Max. {maxHpRef}HP", _ =>
                {
                    ImGui.SetNextItemWidth(barWidth);
                    if (ImGui.DragInt("##MaxThresSlider", ref maxHpRef, 10.0f, minHpRef, 1000000, "Max. %dHP"))
                        healthPerc.ThresholdMaxValue = maxHpRef;
                });
                CkGui.AttachToolTip("The highest HP that is \"In Range\".");
            }
        }
    }

    private void DrawRestraintTrigger(RestraintTrigger restraint, bool isEditorItem, uint? searchBg)
    {
        // Restraint Selection.
        using (CkRaii.InfoRow(FAI.Eye, "Detect", "The Restraint Set to detect changes of."))
        {
            var label = _restraintCombo.Current?.Label ?? "<No Set Selected>";
            var width = ImGui.GetContentRegionAvail().X * .7f;
            CkGuiUtils.FramedEditDisplay("##Restraint", width, isEditorItem, label, _ =>
            {
                var change = _restraintCombo.Draw("##Restraint", restraint.RestraintSetId, width, searchBg);
                
                if (change && !Guid.Equals(restraint.RestraintSetId, _restraintCombo.Current?.Identifier))
                    restraint.RestraintSetId = _restraintCombo.Current?.Identifier ?? Guid.Empty;
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    restraint.RestraintSetId = Guid.Empty;
            });
        }

        // State Detection.
        using (CkRaii.InfoRow(FAI.Flag, "When set to", "The state change that must occur for detection."))
        {
            var width = ImGui.CalcTextSize("unlockedm").X;
            CkGuiUtils.FramedEditDisplay("##RS_State", width, isEditorItem, restraint.RestraintState.ToString(), _ =>
            {
                if (CkGuiUtils.EnumCombo("##RS_State", width, restraint.RestraintState, out var newVal, flags: CFlags.NoArrowButton))
                    restraint.RestraintState = newVal;
            });
        }
    }

    private void DrawRestrictionTrigger(RestrictionTrigger restriction, bool isEditorItem, uint? searchBg)
    {
        // Restriction Item Selection.
        using (CkRaii.InfoRow(FAI.Eye, "Detect", "What Restriction Item to monitor."))
        {
            var label = _restrictionCombo.Current?.Label ?? "<No Item Selected>";
            var width = ImGui.GetContentRegionAvail().X * .7f;
            CkGuiUtils.FramedEditDisplay("##Restriction", width, isEditorItem, label, _ =>
            {
                var change = _restrictionCombo.Draw("##Restriction", restriction.RestrictionId, width, searchBg);

                if (change && !Guid.Equals(restriction.RestrictionId, _restrictionCombo.Current?.Identifier))
                    restriction.RestrictionId = _restrictionCombo.Current?.Identifier ?? Guid.Empty;
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    restriction.RestrictionId = Guid.Empty;
            });
        }

        // State Detection.
        using (CkRaii.InfoRow(FAI.Flag, "When set to", "The state change that must occur for detection."))
        {
            var width = ImGui.CalcTextSize("unlockedm").X;
            CkGuiUtils.FramedEditDisplay("##RI_State", width, isEditorItem, restriction.RestrictionState.ToString(), _ =>
            {
                if (CkGuiUtils.EnumCombo("##RI_State", width, restriction.RestrictionState, out var newVal, flags: CFlags.NoArrowButton))
                    restriction.RestrictionState = newVal;
            });
        }
    }

    private void DrawGagTrigger(GagTrigger gag, bool isEditorItem, uint? searchBg)
    {
        // Gag Selection.
        using (CkRaii.InfoRow(FAI.Eye, "Detect", "The Gag to monitor."))
        {
            var label = gag.Gag is GagType.None ? "<No Gag Selected>" : gag.Gag.GagName();
            var width = ImGui.GetContentRegionAvail().X * .7f;
            CkGuiUtils.FramedEditDisplay("##GagType", width, isEditorItem, label, _ =>
            {
                if (CkGuiUtils.EnumCombo("##GagType", width, gag.Gag, out var newType, _ => _.GagName(), skip: 1, flags: CFlags.None))
                    gag.Gag = newType;
            });
        }

        // State Detection.
        using (CkRaii.InfoRow(FAI.Flag, "When set to", "The state change that must occur for detection."))
        {
            var width = ImGui.CalcTextSize("unlockedm").X;
            CkGuiUtils.FramedEditDisplay("##GagState", width, isEditorItem, gag.GagState.ToString(), _ =>
            {
                if (CkGuiUtils.EnumCombo("##GagState", width, gag.GagState, out var newVal, flags: CFlags.NoArrowButton))
                    gag.GagState = newVal;
            });
        }
    }

    private void DrawSocialTrigger(SocialTrigger social, bool isEditorItem)
    {
        // Social Game.
        using (CkRaii.InfoRow(FAI.Dice, string.Empty, "Social Activity to Detect", "Selected Activity"))
        {
            var width = 100f * ImGuiHelpers.GlobalScale;
            CkGuiUtils.FramedEditDisplay("##SocialActivity", width, isEditorItem, social.SocialType.ToString(), _ =>
            {
                if (CkGuiUtils.EnumCombo("##SocialActivity", width, social.SocialType, out var newVal))
                    social.SocialType = newVal;
            });
        }
    }

    private void DrawEmoteTrigger(EmoteTrigger emote, bool isEditorItem, uint? searchBg)
    {
        using (CkRaii.InfoRow(FAI.Eye, "Detect", "The Emote to detect.", string.Empty))
        {
            var label = _emoteCombo.Current.Name ?? "<No Emote Selected>";
            var width = ImGui.GetContentRegionAvail().X * .7f;
            CkGuiUtils.FramedEditDisplay("##EmoteSelect", width, isEditorItem, label, _ =>
            {
                var change = _emoteCombo.Draw("##EmoteSelectCombo", emote.EmoteID, width, 1.25f, CFlags.None, searchBg);
                if (change && emote.EmoteID != _emoteCombo.Current.RowId)
                    emote.EmoteID = _emoteCombo.Current.RowId;
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    emote.EmoteID = uint.MaxValue;
            });
            CkGui.AttachToolTip("The Emote to detect.");

            ImGui.SameLine();
            _emoteCombo.DrawSelectedIcon(ImGui.GetFrameHeight());
        }

        // The direction the emote should be in.
        var directionsTT = "Determines how the trigger is fired." +
            "--SEP----COL--From Self ⇒--COL-- Done by you." +
            "--SEP----COL--Self to Others ⇒--COL-- Done by you, and the target WAS NOT you." +
            "--SEP----COL--From Others ⇒--COL-- Done by someone else." +
            "--SEP----COL--Others to You ⇒--COL-- Done by someone else, and the target WAS you." +
            "--SEP----COL--Any ⇒--COL-- Ignores Direction. Source & Target can be anyone.";
        using (CkRaii.InfoRow(FAI.Flag, "Direction is", "Required Direction of the Emote", string.Empty))
        {
            var width = ImGui.CalcTextSize("From others to Youm").X;
            CkGuiUtils.FramedEditDisplay("##Direction", width, isEditorItem, emote.EmoteDirection.ToName(), _ =>
            {
                if (CkGuiUtils.EnumCombo("##DirectionCombo", width, emote.EmoteDirection, out var newVal, _ => _.ToName(), flags: CFlags.NoArrowButton))
                    emote.EmoteDirection = newVal;
            });
            CkGui.AttachToolTip(directionsTT, color: CkColor.LushPinkButton.Vec4());
        }


        // Player to Monitor that would use this emote.
        using (CkRaii.InfoRow(FAI.Eye, "Defines who the \"Target\" is, if desired.--SEP--Leaving this blank allows anyone."))
        {
            var width = ImGui.GetContentRegionAvail().X;
            var playerStrRef = emote.PlayerNameWorld;
            var label = playerStrRef.IsNullOrEmpty() ? "<No Name@World Set!>" : playerStrRef;
            CkGuiUtils.FramedEditDisplay("##PlayerMonitor", width, isEditorItem, label, _ =>
            {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputTextWithHint("##PlayerMonitor", "Devious Diva@Balmung..", ref playerStrRef, 68))
                    emote.PlayerNameWorld = playerStrRef;
            });
            CkGui.AttachToolTip("The Target Emote User.--SEP--Must use Player Name@World format.");
        }
    }
}
