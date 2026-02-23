using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using CkCommons.Textures;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using Microsoft.VisualBasic;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;

// Scoped, sealed class to draw the editor and display components of aliasItems.
public sealed class DetectionDrawer : IDisposable
{
    private readonly ILogger<DetectionDrawer> _logger;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
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

    public DetectionDrawer(
        ILogger<DetectionDrawer> logger,
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
        _restrictions = restrictions;
        _restraints = restraints;
        _manager = manager;
        _moodleDrawer = moodleDrawer;

        _restrictionCombo = new RestrictionCombo(logger, mediator, favorites, () => [
            ..restrictions.Storage.OrderByDescending(p => FavoritesConfig.Restrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);

        _restraintCombo = new RestraintCombo(logger, mediator, favorites, () => [
            ..restraints.Storage.OrderByDescending(p => FavoritesConfig.Restraints.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);

        _patternCombo = new PatternCombo(logger, mediator, favorites, () => [
            ..patterns.Storage.OrderByDescending(p => FavoritesConfig.Patterns.Contains(p.Identifier)).ThenBy(p => p.Label)
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

    public void DrawSpellAction(SpellActionTrigger trigger)
    {

        if (trigger.IsGenericDetection)
        {
            CkGui.FramedIconText(FAI.Filter);
            CkGui.TextFrameAlignedInline("For any");
            CkGui.ColorTextFrameAlignedInline(trigger.ActionKind.ToString(), ImGuiColors.TankBlue);
        }

        CkGui.FramedIconText(FAI.ArrowsLeftRight);
        CkGui.TextFrameAlignedInline("When");
        CkGui.ColorTextFrameAlignedInline(trigger.Direction.GetDirectionText(trigger.ActionKind), ImGuiColors.TankBlue);

        if (!string.IsNullOrWhiteSpace(trigger.PlayerNameWorld))
        {
            CkGui.FramedIconText(FAI.User);
            CkGui.TextFrameAlignedInline("Limited to");
            CkGui.ColorTextFrameAlignedInline(trigger.PlayerNameWorld, ImGuiColors.TankBlue);
        }

        if (trigger.ActionKind is not LimitedActionEffectType.Miss or LimitedActionEffectType.Knockback or LimitedActionEffectType.Attract1)
        {
            CkGui.FramedIconText(FAI.BarsProgress);
            var format = trigger.ActionKind is LimitedActionEffectType.Heal ? "HP" : "DPS";

            CkGui.TextFrameAlignedInline("Between");
            CkGui.ColorTextFrameAlignedInline($"{trigger.ThresholdMinValue}{format}", ImGuiColors.TankBlue);
            CkGui.AttachToolTip("The lowest HP that is \"In Range\".");

            CkGui.TextFrameAlignedInline("and");
            CkGui.ColorTextFrameAlignedInline($"{trigger.ThresholdMaxValue}{format}", ImGuiColors.TankBlue);
            CkGui.AttachToolTip("The highest HP that is \"In Range\".");
        }

        if (trigger.IsGenericDetection)
            return;

        // Draw out the non-generic detection information
        var iconsToShow = trigger.StoredActions.Values.SelectMany(a => a).Distinct().ToList();
        var iconSize = new Vector2(ImGui.GetFrameHeight());
        var width = ImGui.GetContentRegionAvail().X;
        using (CkRaii.FramedChildPaddedW("JobActionIcons", width, CkStyle.TwoRowHeight(), 0, ImGui.GetColorU32(ImGuiCol.FrameBg), CkStyle.ChildRounding(), ImGuiHelpers.GlobalScale))
        {
            if (!iconsToShow.Any())
                return;

            var padding = ImGui.GetStyle().ItemInnerSpacing.X;
            var iconsPerRow = MathF.Floor((width - padding) / (iconSize.X + padding));
            // display each moodle.
            var currentRow = 0;
            var iconsInRow = 0;
            foreach (var actionIcon in iconsToShow)
            {
                if (!SpellActionService.AllActionsLookup.TryGetValue(actionIcon, out var iconData))
                    continue;
                // Draw the icon.
                var img = Svc.Texture.GetFromGameIcon((uint)iconData.IconID).GetWrapOrEmpty();
                ImGui.Image(img.Handle, iconSize);
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
        CkGui.AttachToolTip("The actions that must be used to satisfy the above paramaters.");
    }

    private JobType _selectedJob = JobType.ADV;
    private uint _selectedAction = uint.MaxValue;
    public void DrawSpellActionEditor(SpellActionTrigger trigger)
    {
        CkGui.FramedIconText(FAI.ArrowsLeftRight);
        CkGui.AttachToolTip($"Required Direction of the {trigger.ActionKind}");
        CkGui.TextFrameAlignedInline("When");
        var options = trigger.GetOptions();
        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##Dir", ImGui.GetContentRegionAvail().X, trigger.Direction, out var newDir, options, _ => _.GetDirectionText(trigger.ActionKind)))
            trigger.Direction = newDir;
        CkGui.AttachToolTip($"Required Direction of the {trigger.ActionKind}");
        
        CkGui.FramedIconText(FAI.User);
        CkGui.AttachToolTip("Limit trigger to only accept 'other(s)' as valid if they are the defined player.--SEP--Leave blank to ignore.");
        CkGui.TextFrameAlignedInline("Limit to");

        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        var limiter = trigger.PlayerNameWorld;
        if (ImGui.InputTextWithHint("##PlayerMonitor", "Devious Diva@Balmung..", ref limiter, 68))
            trigger.PlayerNameWorld = limiter;
        CkGui.AttachToolTip("Limit 'others' to this Player. --COL--(Must use Player Name@World format)--COL--" +
            "--SEP--Leave blank to ignore.", ImGuiColors.DalamudGrey2); 

        CkGui.FramedIconText(FAI.Filter);
        ImUtf8.SameLineInner();
        var refVal = trigger.IsGenericDetection;
        if (ImGui.Checkbox("Use Generic Type##is-generic", ref refVal))
        {
            trigger.IsGenericDetection = refVal;
            if (!refVal) trigger.ActionKind = LimitedActionEffectType.Nothing;
        }
        CkGui.AttachToolTip("If detection is done under an umbrella catagory, or spesific actions.");

        if (trigger.IsGenericDetection)
        {
            ImUtf8.SameLineInner();
            if (CkGuiUtils.EnumCombo("##ActionType", ImGui.GetContentRegionAvail().X, trigger.ActionKind, out var newVal, af => af.ToName(), skip: 1))
                trigger.ActionKind = newVal;
            CkGui.AttachToolTip("The generic catagory to detect.");
        }

        if (trigger.ActionKind is not LimitedActionEffectType.Miss or LimitedActionEffectType.Knockback or LimitedActionEffectType.Attract1)
        {
            var formatPost = trigger.ActionKind is LimitedActionEffectType.Heal ? "HP" : "DPS";
            CkGui.FramedIconText(FAI.BarsProgress);
            ImUtf8.SameLineInner();
            var dragW = (ImGui.GetContentRegionAvail().X - ImUtf8.ItemInnerSpacing.X) / 2;
            var minRef = trigger.ThresholdMinValue;
            var maxRef = trigger.ThresholdMaxValue;

            ImGui.SetNextItemWidth(dragW);
            if (ImGui.DragInt("##MinThresSlider", ref minRef, 10.0f, -1, 1000000, $"Min. %d{formatPost}"))
                trigger.ThresholdMinValue = minRef;
            CkGui.AttachToolTip("The lowest HP that is --COL--\"In Range\"--COL--.", ImGuiColors.TankBlue);

            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(dragW);
            if (ImGui.DragInt("##MaxThresSlider", ref maxRef, 10.0f, minRef, 1000000, $"Max. %d{formatPost}"))
                trigger.ThresholdMaxValue = maxRef;
            CkGui.AttachToolTip("The highest HP that is --COL--\"In Range\"--COL--.", ImGuiColors.TankBlue);
        }

        // DetectableActions - This filter display allows you to append certain jobs and actions you want the trigger to qualify for.
        if (trigger.IsGenericDetection)
            return;

        // Combo Row.
        var img = Svc.Texture.GetFromGameIcon(SpellActionService.GetLightJob(_selectedJob).GetIconId()).GetWrapOrEmpty();
        ImGui.Image(img.Handle, new Vector2(ImGui.GetFrameHeight()));
        CkGui.AttachToolTip("The selected Job");

        ImUtf8.SameLineInner();
        if (_jobCombo.Draw("##JobSelector", _selectedJob, ImGui.GetContentRegionAvail().X / 2, 1.25f, flags: CFlags.NoArrowButton))
            _selectedJob = _jobCombo.Current.JobId;
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            _selectedJob = JobType.ADV;
        CkGui.AttachToolTip("The job to add actions for.--SEP----COL--[R-Click]:--COL-- Reset to ADV", GsCol.VibrantPink.Vec4Ref());

        ImUtf8.SameLineInner();
        if (_jobActionCombo.Draw("##JobActionSelector", _selectedAction, ImGui.GetContentRegionAvail().X, 1.5f))
        {
            if (trigger.StoredActions.TryGetValue(_selectedJob, out var list))
            {
                if (!list.Contains(_jobActionCombo.Current.ActionID))
                    list.Add(_jobActionCombo.Current.ActionID);
            }
            else
            {
                trigger.StoredActions.TryAdd(_selectedJob, [_jobActionCombo.Current.ActionID]);
            }
            // Reset the selected action to default.
            _selectedAction = uint.MaxValue;
        }

        // Draw out the non-generic detection information
        var iconsToShow = trigger.StoredActions.Values.SelectMany(a => a).Distinct().ToList();
        var iconSize = new Vector2(ImGui.GetFrameHeight());
        using (var _ = CkRaii.FramedChildPaddedW("jobactions", ImGui.GetContentRegionAvail().X, ImUtf8.FrameHeight, 0, ImGui.GetColorU32(ImGuiCol.FrameBg), CkStyle.ChildRounding(), ImGuiHelpers.GlobalScale))
        {
            if (!iconsToShow.Any())
                return;

            var iconsPerRow = MathF.Floor((_.InnerRegion.X - ImUtf8.ItemInnerSpacing.X) / (iconSize.X + ImUtf8.ItemInnerSpacing.X));
            var currentRow = 0;
            var iconsInRow = 0;
            foreach (var actionIcon in iconsToShow)
            {
                if (!SpellActionService.AllActionsLookup.TryGetValue(actionIcon, out var iconData))
                    continue;

                // I really dont understand why hell rendering a single image here consumes more drawtime than an 
                // entire combo of actions drawing icons via the exact same method.
                // Might be some internal problem or something where ImGui doesnt reconize the caching, look into later.
                // Maybe check out XIVCombo or something to figure out how they properly display all actions to their screen fine.
                // (They handle icon management well, perhaps we could learn a thing or two from them)
                ImGui.Image(Svc.Texture.GetFromGameIcon((uint)iconData.IconID).GetWrapOrEmpty().Handle, iconSize);
                CkGui.AttachToolTip($"--COL--[R-Click]:--COL-- Remove {iconData.Name} from actions", GsCol.VibrantPink.Vec4Ref());
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    if (trigger.StoredActions.FirstOrDefault(kvp => kvp.Value.Contains(actionIcon)) is { } list)
                    {
                        list.Value.Remove(actionIcon);
                        if (list.Value.Count is 0)
                            trigger.StoredActions.Remove(list.Key);
                    }
                }

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
        CkGui.AttachToolTip("The actions that must be used to satisfy the above paramaters.");
    }

    public void DrawHealthPercent(HealthPercentTrigger trigger)
    {
        CkGui.FramedIconText(FAI.User);
        CkGui.TextFrameAlignedInline("Monitoring:");
        var displayName = trigger.HasValidNameFormat() ? trigger.PlayerNameWorld : "<No Name@World Set!>";
        CkGui.ColorTextFrameAlignedInline(displayName, ImGuiColors.TankBlue);

        CkGui.FramedIconText(FAI.AssistiveListeningSystems);
        CkGui.TextFrameAlignedInline("For when HP falls");
        CkGui.ColorTextFrameAlignedInline(trigger.PassKind.ToString(), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("the threshold.");

        CkGui.FramedIconText(FAI.BarsProgress);
        CkGui.TextFrameAlignedInline("Threshold:");
        var format = trigger.UsePercentageHealth ? "%" : "HP";
        CkGui.ColorTextFrameAlignedInline($"{trigger.ThresholdMinValue}{format}", ImGuiColors.TankBlue);
        if (!trigger.UsePercentageHealth)
        {
            CkGui.TextFrameAlignedInline("to");
            CkGui.ColorTextFrameAlignedInline($"{trigger.ThresholdMaxValue}{format}", ImGuiColors.TankBlue);
        }
    }

    public void DrawHealthPercentEditor(HealthPercentTrigger trigger)
    {
        CkGui.FramedIconText(FAI.User);
        CkGui.TextFrameAlignedInline("Monitoring:");

        var name = trigger.PlayerNameWorld;
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputTextWithHint("##PlayerMonitor", "Devious Diva@Balmung..", ref name, 68))
            trigger.PlayerNameWorld = name;
        CkGui.AttachToolTip("The monitored player. --COL--(Must use Player Name@World format)--COL--", ImGuiColors.DalamudGrey2);

        var refVal = trigger.UsePercentageHealth;
        if (ImGui.Checkbox("Detect HP % Instead##UsePercent", ref refVal))
            trigger.UsePercentageHealth = refVal;
        CkGui.AttachToolTip("If we detect based on % health difference, or flat HP values.");

        CkGui.FramedIconText(FAI.Flag);
        CkGui.AttachToolTip("What Pass-Kind to use for HP Detection.");
        CkGui.TextFrameAlignedInline("Threshold Pass Type:");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##PassTypeCombo", ImGui.CalcTextSize("undermmm").X, trigger.PassKind, out var newVal))
            trigger.PassKind = newVal;
        CkGui.AttachToolTip("--COL--[Under]:--COL-- HP drops below the threshold" +
            "--SEP----COL--[Over]:--COL-- HP goes above the threshold", GsCol.VibrantPink.Vec4Ref());

        if (trigger.UsePercentageHealth)
        {
            CkGui.FramedIconText(FAI.BarsProgress);
            var hpRef = trigger.ThresholdMinValue;
            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.DragInt("##HealthPercentage", ref hpRef, 0.1f, 0, 100, "%d%%"))
                trigger.ThresholdMinValue = hpRef;
            CkGui.AttachToolTip("The HP% that must be crossed.");
        }
        else
        {
            CkGui.FramedIconText(FAI.BarsProgress);

            var minHpRef = trigger.ThresholdMinValue;
            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2 - ImGui.GetStyle().ItemInnerSpacing.X);
            if (ImGui.DragInt("##MinThresSlider", ref minHpRef, 10.0f, -1, 1000000, "Min. %dHP"))
                trigger.ThresholdMinValue = minHpRef;
            CkGui.AttachToolTip("The lowest HP that is --COL--\"In Range\"--COL--.", ImGuiColors.TankBlue);

            var maxHpRef = trigger.ThresholdMaxValue;
            ImUtf8.SameLineInner();
            if (ImGui.DragInt("##MaxThresSlider", ref maxHpRef, 10.0f, minHpRef, 1000000, "Max. %dHP"))
                trigger.ThresholdMaxValue = maxHpRef;
            CkGui.AttachToolTip("The highest HP that is --COL--\"In Range\"--COL--.", ImGuiColors.TankBlue);
        }
    }

    public void DrawRestraint(RestraintTrigger trigger)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Restrained].Handle, new(ImUtf8.FrameHeight));
        CkGui.AttachToolTip("Invokes an interaction with the Restraint module");

        CkGui.TextFrameAlignedInline("Monitoring:");
        var item = _restraints.Storage.FirstOrDefault(r => r.Identifier == trigger.RestraintSetId);
        CkGui.ColorTextFrameAlignedInline(item is { } re ? $"{re.Label.TrimText(40)}.." : "<UNK>", ImGuiColors.TankBlue);

        CkGui.FramedIconText(FAI.Flag);
        CkGui.TextFrameAlignedInline("When set to");
        CkGui.ColorTextFrameAlignedInline(trigger.RestraintState.ToString(), ImGuiColors.TankBlue);
    }

    public void DrawRestraintEditor(RestraintTrigger trigger)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Restrained].Handle, new(ImUtf8.FrameHeight));
        CkGui.TextFrameAlignedInline("Monitor");
        
        ImUtf8.SameLineInner();
        if (_restraintCombo.Draw("##restraintsets", trigger.RestraintSetId, ImGui.GetContentRegionAvail().X))
            trigger.RestraintSetId = _restraintCombo.Current?.Identifier ?? Guid.Empty;
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            trigger.RestraintSetId = Guid.Empty;
        CkGui.AttachToolTip("The Restraint Set to monitor for state changes.");

        CkGui.FramedIconText(FAI.Flag);
        CkGui.TextFrameAlignedInline("When set to");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##rs-state", 75f, trigger.RestraintState, out var newState))
            trigger.RestraintState = newState;
        CkGui.AttachToolTip("The state change type that will invoke this trigger");
    }

    public void DrawRestriction(RestrictionTrigger trigger)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Restrained].Handle, new(ImUtf8.FrameHeight));
        CkGui.AttachToolTip("Invokes an interaction with the Restraint module");

        CkGui.TextFrameAlignedInline("Monitoring:");
        var item = _restrictions.Storage.FirstOrDefault(r => r.Identifier == trigger.RestrictionId);
        CkGui.ColorTextFrameAlignedInline(item is { } re ? $"{re.Label.TrimText(40)}.." : "<UNK>", ImGuiColors.TankBlue);

        CkGui.FramedIconText(FAI.Flag);
        CkGui.TextFrameAlignedInline("When set to");
        CkGui.ColorTextFrameAlignedInline(trigger.RestrictionState.ToString(), ImGuiColors.TankBlue);
    }

    public void DrawRestrictionEditor(RestrictionTrigger trigger)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Restrained].Handle, new(ImUtf8.FrameHeight));
        CkGui.TextFrameAlignedInline("Monitor");

        ImUtf8.SameLineInner();
        if (_restrictionCombo.Draw("##restrictions", trigger.RestrictionId, ImGui.GetContentRegionAvail().X))
            trigger.RestrictionId = _restrictionCombo.Current?.Identifier ?? Guid.Empty;
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            trigger.RestrictionId = Guid.Empty;
        CkGui.AttachToolTip("The Restriction to monitor for state changes.");

        CkGui.FramedIconText(FAI.Flag);
        CkGui.TextFrameAlignedInline("When set to");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##r-state", 75f, trigger.RestrictionState, out var newState))
            trigger.RestrictionState = newState;
        CkGui.AttachToolTip("The state change type that will invoke this trigger");
    }

    public void DrawGag(GagTrigger trigger)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, new(ImUtf8.FrameHeight));
        CkGui.AttachToolTip("Invokes an interaction with the Restraint module");

        CkGui.TextFrameAlignedInline("Monitoring:");
        CkGui.ColorTextFrameAlignedInline(trigger.Gag.GagName(), ImGuiColors.TankBlue);

        CkGui.FramedIconText(FAI.Flag);
        CkGui.TextFrameAlignedInline("When set to");
        CkGui.ColorTextFrameAlignedInline(trigger.GagState.ToString(), ImGuiColors.TankBlue);
    }

    public void DrawGagEditor(GagTrigger trigger)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, new(ImUtf8.FrameHeight));
        CkGui.TextFrameAlignedInline("Monitor");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##gag", ImGui.GetContentRegionAvail().X, trigger.Gag, out var newGag, i => i.GagName(), "Any Gag", skip: 1))
            trigger.Gag = newGag;
        CkGui.AttachToolTip("The Gag to monitor for state changes.");

        CkGui.FramedIconText(FAI.Flag);
        CkGui.TextFrameAlignedInline("When set to");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##gag-state", 75f, trigger.GagState, out var newState))
            trigger.GagState = newState;
        CkGui.AttachToolTip("The state change type that will invoke this trigger");
    }

    public void DrawSocial(SocialTrigger trigger)
    {
        CkGui.FramedIconText(FAI.Dice);
        CkGui.TextFrameAlignedInline("Monitoring Social Activity:");
        CkGui.ColorTextFrameAlignedInline(trigger.Game.ToString(), ImGuiColors.TankBlue);

        CkGui.FramedIconText(FAI.Trophy);
        CkGui.TextFrameAlignedInline("When you end the game with a");
        CkGui.ColorTextFrameAlignedInline(trigger.Result.ToString(), ImGuiColors.TankBlue);
    }

    public void DrawSocialEditor(SocialTrigger trigger)
    {
        CkGui.FramedIconText(FAI.Dice);
        CkGui.TextFrameAlignedInline("Monitor:");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##SocialActivity", ImGui.GetContentRegionAvail().X * .5f, trigger.Game, out var newVal, _ => _.ToString()))
            trigger.Game = newVal;
        CkGui.AttachToolTip("The Social Game to monitor for.");

        CkGui.FramedIconText(FAI.Trophy);
        CkGui.TextFrameAlignedInline("When outcome is a");
        ImUtf8.SameLineInner();
        var resWidth = ImGui.CalcTextSize("lossmmm").X;
        if (CkGuiUtils.EnumCombo("##SocialResult", resWidth, trigger.Result, out var newRes, _ => _.ToString()))
            trigger.Result = newRes;
        CkGui.AttachToolTip("What result from the game will invoke this trigger.");
    }

    public void DrawEmote(EmoteTrigger trigger)
    {
        CkGui.FramedIconText(FAI.PersonFallingBurst);
        CkGui.TextFrameAlignedInline("Monitoring Emote");
        DrawEmoteIconWithTooltip(trigger.EmoteID);

        CkGui.FramedIconText(FAI.ArrowsLeftRight);
        CkGui.TextFrameAlignedInline("Direction is");
        CkGui.ColorTextFrameAlignedInline(trigger.EmoteDirection.ToName(), ImGuiColors.TankBlue);

        if (trigger.EmoteDirection is not (TriggerDirection.Self or TriggerDirection.Any) && !string.IsNullOrWhiteSpace(trigger.PlayerNameWorld))
        {
            CkGui.FramedIconText(FAI.User);
            CkGui.TextFrameAlignedInline("\"Other\" is");
            CkGui.ColorTextFrameAlignedInline(trigger.PlayerNameWorld, ImGuiColors.TankBlue);
        }
    }

    public void DrawEmoteEditor(EmoteTrigger trigger)
    {
        CkGui.FramedIconText(FAI.PersonFallingBurst);
        CkGui.TextFrameAlignedInline("Monitoring Emote");

        ImUtf8.SameLineInner();
        var width = ImGui.GetContentRegionAvail().X - ImUtf8.FrameHeight + ImUtf8.ItemInnerSpacing.X;
        if (_emoteCombo.Draw("##emote-sel", trigger.EmoteID, width, 1.25f))
            trigger.EmoteID = _emoteCombo.Current.RowId;
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            trigger.EmoteID = uint.MaxValue;
        CkGui.AttachToolTip("The emote to detect.");
        DrawEmoteIconWithTooltip(_emoteCombo.Current.RowId);

        // Direction row
        CkGui.FramedIconText(FAI.ArrowsLeftRight);
        CkGui.TextFrameAlignedInline("Direction is");
        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##direction", ImGui.GetContentRegionAvail().X, trigger.EmoteDirection, out var newVal, _ => _.ToName()))
            trigger.EmoteDirection = newVal;
        CkGui.AttachToolTip("How detection is validated." +
            "--SEP----COL--[Self]:--COL-- Emote is from you" +
            "--NL----COL--[Self ⇒ Others]:--COL-- You used an emote on someone" +
            "--NL----COL--[Others]:--COL-- Someone else used an emote" +
            "--NL----COL--[Others ⇒ Self]:--COL-- Done by someone else, and the target WAS you." +
            "--NL----COL--[Any]--COL-- Ignores Direction.", GsCol.VibrantPink.Vec4Ref());

        if (trigger.EmoteDirection is not (TriggerDirection.Self or TriggerDirection.Any))
        {
            CkGui.FramedIconText(FAI.User);
            CkGui.TextFrameAlignedInline("\"Other\" limited to");
            var playerStrRef = trigger.PlayerNameWorld;
            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.InputTextWithHint("##PlayerMonitor", "Devious Diva@Balmung..", ref playerStrRef, 68))
                trigger.PlayerNameWorld = playerStrRef;
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                trigger.PlayerNameWorld = string.Empty;
            CkGui.AttachToolTip("Defines the --COL--Target--COL----SEP--Leaving this blank allows anyone.", GsCol.VibrantPink.Vec4Ref());
        }
    }

    private void DrawEmoteIconWithTooltip(uint emoteId)
    {
        if (emoteId is 0 || emoteId is uint.MaxValue)
            return;

        if (EmoteService.ValidLightEmoteCache.FirstOrDefault(e => e.RowId == emoteId) is { } emote)
        {
            // Draw it out
            var image = Svc.Texture.GetFromGameIcon(emote.IconId).GetWrapOrEmpty();
            ImUtf8.SameLineInner();
            ImGui.Image(image.Handle, new(ImUtf8.FrameHeight));
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.RectOnly))
            {
                using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
                    .Push(ImGuiStyleVar.WindowRounding, 4f).Push(ImGuiStyleVar.PopupRounding, 4f);
                using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
                // begin the tooltip interface
                ImGui.BeginTooltip();
                using (ImRaii.Group())
                {
                    ImGui.Image(image.Handle, new Vector2(ImGui.GetFrameHeight() * 2));
                    ImGui.SameLine();
                    using (ImRaii.Group())
                    {
                        ImGui.Text(emote.Name);
                        CkGui.ColorTextInline($"(Id: {emote.RowId})", CkGui.Color(ImGuiColors.DalamudGrey2));
                        CkGui.ColorText($"(Icon: {emote.IconId})", CkGui.Color(ImGuiColors.DalamudGrey));
                    }
                }
                ImGui.Separator();

                CkGui.ColorText("Commands:", ImGuiColors.ParsedPink);
                CkGui.TextInline(string.Join(", ", emote.CommandsSafe.Select(cmd => "/" + cmd)));
                ImGui.EndTooltip();
            }
        }
    }
}
