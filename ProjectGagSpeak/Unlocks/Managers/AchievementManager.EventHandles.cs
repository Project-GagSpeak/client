using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using GagSpeak.ChatMessages;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerState.Models;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Text.RegularExpressions;

namespace GagSpeak.Achievements;
public partial class AchievementManager
{
    private void OnCommendationsGiven(int amount)
    {
        (LatestCache.SaveData.Achievements[Achievements.KinkyTeacher.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(amount);
        (LatestCache.SaveData.Achievements[Achievements.KinkyProfessor.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(amount);
        (LatestCache.SaveData.Achievements[Achievements.KinkyMentor.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(amount);
    }

    private void OnPairVisible()
    {
        // We need to obtain the total visible user count, then update the respective achievements.
        var visiblePairs = _pairs.GetVisibleUserCount();
        (LatestCache.SaveData.Achievements[Achievements.BondageClub.Id] as ThresholdAchievement)?.UpdateThreshold(visiblePairs);
        (LatestCache.SaveData.Achievements[Achievements.Humiliation.Id] as ConditionalThresholdAchievement)?.UpdateThreshold(visiblePairs);
    }

    public void OnClientMessageContainsPairTrigger(string msg)
    {
        foreach (var pair in _pairs.DirectPairs)
        {
            var triggers = pair.PairPerms.TriggerPhrase.Split("|").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            // This ensures it is a full word.
            var foundTrigger = triggers.FirstOrDefault(trigger => Regex.IsMatch(msg, $@"(?<!\w){Regex.Escape(trigger)}(?!\w)", RegexOptions.IgnoreCase));

            if (!string.IsNullOrEmpty(foundTrigger))
            {
                // This was a trigger message for the pair, so let's see what the pairs settings are for.
                var startChar = pair.PairPerms.StartChar;
                var endChar = pair.PairPerms.EndChar;

                // Get the string that exists beyond the trigger phrase found in the message.
                Logger.LogTrace("Sent Message with trigger phrase set by " + pair.GetNickAliasOrUid() + ". Gathering Results.", LoggerType.Puppeteer);
                SeString remainingMessage = msg.Substring(msg.IndexOf(foundTrigger) + foundTrigger.Length).Trim();

                // Get the substring within the start and end char if provided. If the start and end chars are not both present in the remaining message, keep the remaining message.
                remainingMessage.GetSubstringWithinParentheses(startChar, endChar);
                Logger.LogTrace("Remaining message after brackets: " + remainingMessage, LoggerType.Puppeteer);

                // If the string contains the word "grovel", fire the grovel achievement.
                if (remainingMessage.TextValue.Contains("grovel"))
                    UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.GrovelOrder);
                else if (remainingMessage.TextValue.Contains("dance"))
                    UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.DanceOrder);
                else
                    UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.GenericOrder);

                return;
            }
        }
    }

    private DateTime worldTourStartedTime = DateTime.MinValue;
    private void CheckOnZoneSwitchStart(ushort prevZone)
    {
        // we left the zone we were at, so see if our woldTourStartedTime is not minvalue, if it isnt we need to check our conditonalProgressAchievement.
        if(worldTourStartedTime != DateTime.MinValue)
        {
            // Ensure it has been longer than 2 minutes since the recorded time. (in UTC)
            if ((DateTime.UtcNow - worldTourStartedTime).TotalMinutes > 2)
            {
                // Check to see if we qualify for starting any world tour conditions.
                if (LatestCache.SaveData.VisitedWorldTour.ContainsKey(prevZone) && LatestCache.SaveData.VisitedWorldTour[prevZone] is false)
                {
                    // Mark the conditional as finished in the achievement, and mark as completed.
                    if (_restraints.AppliedRestraint is not null)
                    {
                        (LatestCache.SaveData.Achievements[Achievements.TourDeBound.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                        LatestCache.SaveData.VisitedWorldTour[prevZone] = true;
                    }
                    else
                        (LatestCache.SaveData.Achievements[Achievements.TourDeBound.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
                    // reset the datetime to .MinValue
                    worldTourStartedTime = DateTime.MinValue;
                }
            }
        }
    }

    private void CheckOnZoneSwitchEnd()
    {
        Logger.LogTrace("Current Territory Id: " + _clientMonitor.TerritoryId, LoggerType.AchievementEvents);
        if(_clientMonitor.InMainCity)
            (LatestCache.SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

        var territory = _clientMonitor.TerritoryId;

        // if present in diadem (for diamdem achievement) (Accounts for going into diadem while a vibe is running)
        if (territory is 939 && !_clientMonitor.InPvP)
            (LatestCache.SaveData.Achievements[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        else
            (LatestCache.SaveData.Achievements[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

        // If we left before completing the duty, check that here.
        if ((LatestCache.SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (LatestCache.SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();

        if ((LatestCache.SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (LatestCache.SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();

        if ((LatestCache.SaveData.Achievements[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (LatestCache.SaveData.Achievements[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();

        // Check to see if we qualify for starting any world tour conditions.
        if (LatestCache.SaveData.VisitedWorldTour.ContainsKey(territory) && LatestCache.SaveData.VisitedWorldTour[territory] is false)
        {
            // if its already true, dont worry about it.
            if (LatestCache.SaveData.VisitedWorldTour[territory] is true)
            {
                Logger.LogTrace("World Tour Progress already completed for: " + territory, LoggerType.AchievementEvents);
                Logger.LogTrace("Current Progress for all items is: " + string.Join(", ", LatestCache.SaveData.VisitedWorldTour.Select(x => x.Key + " : " + x.Value)), LoggerType.Achievements);

                return;
            }
            else // Begin the progress for this city's world tour. 
            {
                Logger.LogTrace("Starting World Tour Progress for: " + territory, LoggerType.AchievementEvents);
                (LatestCache.SaveData.Achievements[Achievements.TourDeBound.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                worldTourStartedTime = DateTime.UtcNow;
            }
        }
    }

    private void CheckDeepDungeonStatus()
    {
        // Detect Specific Dungeon Types
        if (!Content.InDeepDungeon()) return;

        var floor = Content.GetFloor();
        if (floor is null) 
            return;

        var deepDungeonType = _clientMonitor.GetDeepDungeonType();
        if (deepDungeonType is null) 
            return;

        if (_clientMonitor.PartySize is 1)
            (LatestCache.SaveData.Achievements[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
        // start this under any condition.
        (LatestCache.SaveData.Achievements[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();


        switch (deepDungeonType)
        {
            case DeepDungeonType.PalaceOfTheDead:
                if ((floor > 40 && floor <= 50) || (floor > 90 && floor <= 100))
                {
                    (LatestCache.SaveData.Achievements[Achievements.BondagePalace.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                    if (floor is 50 || floor is 100)
                        (LatestCache.SaveData.Achievements[Achievements.BondagePalace.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                if(floor is 200)
                {
                    (LatestCache.SaveData.Achievements[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                    (LatestCache.SaveData.Achievements[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                break;
            case DeepDungeonType.HeavenOnHigh:
                if (floor > 20 && floor <= 30)
                {
                    (LatestCache.SaveData.Achievements[Achievements.HornyOnHigh.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                    if (floor is 30)
                        (LatestCache.SaveData.Achievements[Achievements.HornyOnHigh.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                if (floor is 100)
                {
                    (LatestCache.SaveData.Achievements[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                    (LatestCache.SaveData.Achievements[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                break;
            case DeepDungeonType.EurekaOrthos:
                if (floor > 20 && floor <= 30)
                {
                    (LatestCache.SaveData.Achievements[Achievements.EurekaWhorethos.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                    if (floor is 30)
                        (LatestCache.SaveData.Achievements[Achievements.EurekaWhorethos.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                if (floor is 100)
                {
                    (LatestCache.SaveData.Achievements[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                    (LatestCache.SaveData.Achievements[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                }
                break;
        }
    }

    private void OnDutyStart(object? sender, ushort e)
    {
        Logger.LogInformation("Duty Started", LoggerType.AchievementEvents);
        if (_clientMonitor.InPvP)
            return;

        (LatestCache.SaveData.Achievements[Achievements.KinkyExplorer.Id] as ConditionalAchievement)?.CheckCompletion();

        (LatestCache.SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
        (LatestCache.SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.BeginConditionalTask(25); // 10s delay.

        if (_clientMonitor.ClientPlayer.ClassJobRole() is ActionRoles.Healer)
            (LatestCache.SaveData.Achievements[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();

        // If the party size is 8, let's check for the trials.
        if(_clientMonitor.PartySize is 8 && _clientMonitor.Level >= 90)
        {
            (LatestCache.SaveData.Achievements[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
            (LatestCache.SaveData.Achievements[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
            (LatestCache.SaveData.Achievements[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
        }

        // check stuff for deep dungeons.
        CheckDeepDungeonStatus();
    }

    private void OnDutyEnd(object? sender, ushort e)
    {
        if (_clientMonitor.InPvP)
            return;
        Logger.LogInformation("Duty Ended", LoggerType.AchievementEvents);
        if ((LatestCache.SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (LatestCache.SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();

        if ((LatestCache.SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (LatestCache.SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();

        if ((LatestCache.SaveData.Achievements[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (LatestCache.SaveData.Achievements[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();

        // Trial has ended, check for completion.
        if ((LatestCache.SaveData.Achievements[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
        {
            if (_clientMonitor.PartySize is 8 && _clientMonitor.Level >= 90)
                (LatestCache.SaveData.Achievements[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
            else // if we are not in a full party, we should reset the task.
                (LatestCache.SaveData.Achievements[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
        }
        
        if ((LatestCache.SaveData.Achievements[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
        {
            if (_clientMonitor.PartySize is 8 && _clientMonitor.Level >= 90)
                (LatestCache.SaveData.Achievements[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
            else // if we are not in a full party, we should reset the task.
                (LatestCache.SaveData.Achievements[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
        }
        
        if ((LatestCache.SaveData.Achievements[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
        {
            if (_clientMonitor.PartySize is 8 && _clientMonitor.Level >= 90)
                (LatestCache.SaveData.Achievements[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
            else // if we are not in a full party, we should reset the task.
                (LatestCache.SaveData.Achievements[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
        }

        // check stuff for deep dungeons.
        CheckDeepDungeonStatus();
    }

    private void OnOrderAction(OrderInteractionKind orderKind)
    {
        switch (orderKind)
        {
            case OrderInteractionKind.Completed:
                (LatestCache.SaveData.Achievements[Achievements.JustAVolunteer.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.AsYouCommand.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.AnythingForMyOwner.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.GoodDrone.Id] as ProgressAchievement)?.IncrementProgress();
                break;
            case OrderInteractionKind.Fail:
                (LatestCache.SaveData.Achievements[Achievements.BadSlut.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.NeedsTraining.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.UsefulInOtherWays.Id] as ProgressAchievement)?.IncrementProgress();
                break;
            case OrderInteractionKind.Create:
                (LatestCache.SaveData.Achievements[Achievements.NewSlaveOwner.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.TaskManager.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.MaidMaster.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.QueenOfDrones.Id] as ProgressAchievement)?.IncrementProgress();
                break;
        }
    }

    private void OnGagStateChanged(int gagLayer, GagType gagAppliedOrRemoved, bool applying, string enactorUid)
    {
        if (_gags.ServerGagData is not { } gagData || gagAppliedOrRemoved is GagType.None)
            return;

        var trackingKey = gagLayer.ToString() + '_' + gagAppliedOrRemoved.GagName();

        // for enables.
        if (applying)
        {
            // the gag was applied to us by ourselves.
            if (enactorUid == MainHub.UID)
            {
                (LatestCache.SaveData.Achievements[Achievements.SelfApplied.Id] as ProgressAchievement)?.IncrementProgress();
            }
            // the gag was applied to us by someone else.
            else
            {
                (LatestCache.SaveData.Achievements[Achievements.SilencedSlut.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.InDeepSilence.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.SilentObsessions.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.GoldenSilence.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.AKinkForDrool.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.ThePerfectGagSlut.Id] as ProgressAchievement)?.IncrementProgress();

                (LatestCache.SaveData.Achievements[Achievements.ATrueGagSlut.Id] as TimedProgressAchievement)?.IncrementProgress();
            }

            // track regardless of who applied it.
            (LatestCache.SaveData.Achievements[Achievements.WhispersToWhimpers.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.OfMuffledMoans.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.SilentStruggler.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.QuietedCaptive.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.MessyDrooler.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.DroolingDiva.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.EmbraceOfSilence.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.SubjugationToSilence.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.SpeechSilverSilenceGolden.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.TheKinkyLegend.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);

            (LatestCache.SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion();
            (LatestCache.SaveData.Achievements[Achievements.GaggedPleasure.Id] as ConditionalAchievement)?.CheckCompletion();
        }
        // for disables.
        else
        {
            (LatestCache.SaveData.Achievements[Achievements.WhispersToWhimpers.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.OfMuffledMoans.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.SilentStruggler.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.QuietedCaptive.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.MessyDrooler.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.DroolingDiva.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.EmbraceOfSilence.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.SubjugationToSilence.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.SpeechSilverSilenceGolden.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (LatestCache.SaveData.Achievements[Achievements.TheKinkyLegend.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);

            // Halt our Silent But Deadly Progress if gag is removed mid-dungeon
            if ((LatestCache.SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
                (LatestCache.SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
        }

        // Update regardless
        (LatestCache.SaveData.Achievements[Achievements.ShushtainableResource.Id] as ThresholdAchievement)?.UpdateThreshold(gagData.TotalGagsEquipped());
    }

    private void OnPairGagStateChanged(int layer, GagType gag, bool applying, string assignerUid, string affectedUid)
    {
        if(applying)
        {
            if (gag is not GagType.None)
            {
                (LatestCache.SaveData.Achievements[Achievements.SilenceSlut.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.WatchYourTongue.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.TongueTamer.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.KinkyLibrarian.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.OrchestratorOfSilence.Id] as ProgressAchievement)?.IncrementProgress();

                (LatestCache.SaveData.Achievements[Achievements.YourFavoriteNurse.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
            }
        }
        else
        {
            // nothing for removing yet.
        }
    }

    private void OnGagLockStateChange(int layer, Padlocks padlock, bool isLocking, string assignerUid)
    {
        if (isLocking)
        {
            // nothing for locking yet.
        }
        else
        {
            // nothing for removing yet.
        }
    }

    private void OnPairGagLockStateChange(int layer, Padlocks padlock, bool isLocking, string assignerUid, string affectedUid)
    {
        if (isLocking)
        {
            // nothing for locking yet.
        }
        else
        {
            // nothing for removing yet.
        }
    }

    private void OnCharaOnlineCleanupForLatest(UserData user, CharaActiveGags gagInfo, CharaActiveRestrictions restrictionsInfo, CharaActiveRestraint restraintInfo)
    {
        var activeGagTrackingKeys = gagInfo.ActiveGagTrackingKeys();
        Logger.LogDebug("Player Character " + user.AliasOrUID + " went online and has new active data. Cleaning up expired information!", LoggerType.AchievementEvents);
        // Do stuff if its a gag type.
        (LatestCache.SaveData.Achievements[Achievements.WhispersToWhimpers.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (LatestCache.SaveData.Achievements[Achievements.OfMuffledMoans.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (LatestCache.SaveData.Achievements[Achievements.SilentStruggler.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (LatestCache.SaveData.Achievements[Achievements.QuietedCaptive.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (LatestCache.SaveData.Achievements[Achievements.MessyDrooler.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (LatestCache.SaveData.Achievements[Achievements.DroolingDiva.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (LatestCache.SaveData.Achievements[Achievements.EmbraceOfSilence.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (LatestCache.SaveData.Achievements[Achievements.SubjugationToSilence.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (LatestCache.SaveData.Achievements[Achievements.SpeechSilverSilenceGolden.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (LatestCache.SaveData.Achievements[Achievements.TheKinkyLegend.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);

        // Checks spesific to the direction of the application.
        if (user.UID == MainHub.UID)
        {
            (LatestCache.SaveData.Achievements[Achievements.FirstTimeBondage.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.AmateurBondage.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.ComfortRestraint.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.YourBondageMaid.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.YourRubberMaid.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.TrainedBondageSlave.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.YourRubberSlut.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.ATrueBondageSlave.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });

            (LatestCache.SaveData.Achievements[Achievements.ShushtainableResource.Id] as ThresholdAchievement)?.UpdateThreshold(gagInfo.TotalGagsEquipped());
        }
        else
        {
            (LatestCache.SaveData.Achievements[Achievements.RiggersFirstSession.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.MyLittlePlaything.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.SuitsYouBitch.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.TiesThatBind.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.SlaveTrainer.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
            (LatestCache.SaveData.Achievements[Achievements.CeremonyOfEternalBondage.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { restraintInfo.Identifier.ToString() });
        }

        // Do stuff if it is a pattern.
        (LatestCache.SaveData.Achievements[Achievements.ALittleTease.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (LatestCache.SaveData.Achievements[Achievements.ShortButSweet.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (LatestCache.SaveData.Achievements[Achievements.TemptingRythms.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (LatestCache.SaveData.Achievements[Achievements.MyBuildingDesire.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (LatestCache.SaveData.Achievements[Achievements.WithWavesOfSensation.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (LatestCache.SaveData.Achievements[Achievements.WithHeightenedSensations.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (LatestCache.SaveData.Achievements[Achievements.MusicalMoaner.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (LatestCache.SaveData.Achievements[Achievements.StimulatingExperiences.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (LatestCache.SaveData.Achievements[Achievements.EnduranceKing.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (LatestCache.SaveData.Achievements[Achievements.EnduranceQueen.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });

        // if these are started, inturrupt them so that they do not complete.
        (LatestCache.SaveData.Achievements[Achievements.ForcedFollow.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (LatestCache.SaveData.Achievements[Achievements.ForcedWalkies.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
    }

    private void OnRestrictionStateChange(Guid restrictionId, bool isEnabling, string enactorUID)
    {
        // Nothing yet.
    }

    private void OnRestrictionLock(Guid restrictionId, Padlocks padlock, bool isLocking, string enactorUID)
    {
        // Nothing yet.
    }

    private void OnPairRestrictionStateChange(Guid restrictionId, bool isLocking, string enactorUID, string affectedUID)
    {
        // Nothing yet.
    }

    private void OnPairRestrictionLockChange(Guid restrictionId, Padlocks padlock, bool isLocking, string enactorUID, string affectedUID)
    {
        // Nothing yet.
    }


    private void OnRestraintSetUpdated(RestraintSet set)
    {
        // check for dyes
        if (set.GetGlamour().Any(x => x.Value.GameStain != StainIds.None))
        {
            (LatestCache.SaveData.Achievements[Achievements.ToDyeFor.Id] as ProgressAchievement)?.IncrementProgress();
            (LatestCache.SaveData.Achievements[Achievements.DyeAnotherDay.Id] as ProgressAchievement)?.IncrementProgress();
            (LatestCache.SaveData.Achievements[Achievements.DyeHard.Id] as ProgressAchievement)?.IncrementProgress();
        }
    }

    private void OnRestraintStateChange(Guid restraintId, bool isEnabling, string enactorUID)
    {
        // Check this regardless.
        (LatestCache.SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

        // Set is being enabled.
        if (isEnabling)
        {
            var territory = _clientMonitor.TerritoryId;
            // Check to see if we qualify for starting any world tour conditions.
            if (LatestCache.SaveData.VisitedWorldTour.ContainsKey(territory) && LatestCache.SaveData.VisitedWorldTour[territory] is false)
            {
                // if its already true, dont worry about it.
                if (LatestCache.SaveData.VisitedWorldTour[territory] is true)
                {
                    Logger.LogTrace("World Tour Progress already completed for: " + territory, LoggerType.AchievementEvents);
                    return;
                }
                else // Begin the progress for this city's world tour. 
                {
                    Logger.LogTrace("Starting World Tour Progress for: " + territory, LoggerType.AchievementEvents);
                    (LatestCache.SaveData.Achievements[Achievements.TourDeBound.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                    worldTourStartedTime = DateTime.UtcNow;
                }
            }

            (LatestCache.SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion();
            (LatestCache.SaveData.Achievements[Achievements.FirstTiemers.Id] as ProgressAchievement)?.IncrementProgress();

            // if we are the applier
            if (enactorUID == MainHub.UID)
            {
                (LatestCache.SaveData.Achievements[Achievements.SelfBondageEnthusiast.Id] as ProgressAchievement)?.IncrementProgress();
            }
            else // someone else is enabling our set
            {
                (LatestCache.SaveData.Achievements[Achievements.AuctionedOff.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                // starts the timer.
                (LatestCache.SaveData.Achievements[Achievements.Bondodge.Id] as TimeLimitConditionalAchievement)?.StartTask();

                // track overkill if it is not yet completed
                if((LatestCache.SaveData.Achievements[Achievements.ExtremeBondageEnjoyer.Id] as ThresholdAchievement)?.IsCompleted is false)
                {
                    if (_restraints.Storage.TryGetRestraint(restraintId, out var match))
                        (LatestCache.SaveData.Achievements[Achievements.ExtremeBondageEnjoyer.Id] as ThresholdAchievement)?.UpdateThreshold(match.GetGlamour().Count());
                }

                // Track Bondage Bunny
                (LatestCache.SaveData.Achievements[Achievements.BondageBunny.Id] as TimedProgressAchievement)?.IncrementProgress();

                // see if valid for "cuffed-19" if it is not yet completed
                if ((LatestCache.SaveData.Achievements[Achievements.Cuffed19.Id] as ProgressAchievement)?.IsCompleted is false)
                {
                    // attempt to retrieve the set from our sets.
                    if (_restraints.Storage.TryGetRestraint(restraintId, out var match))
                        if (match.GetGlamour().Any(glam => glam.Key is EquipSlot.Hands))
                            (LatestCache.SaveData.Achievements[Achievements.Cuffed19.Id] as ProgressAchievement)?.IncrementProgress();
                }
            }
        }
        else // set is being disabled
        {
            // must be removed within limit or wont award.
            (LatestCache.SaveData.Achievements[Achievements.Bondodge.Id] as TimeLimitConditionalAchievement)?.CheckCompletion();

            // If a set is being disabled at all, we should reset our conditionals.
            (LatestCache.SaveData.Achievements[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
            (LatestCache.SaveData.Achievements[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
            (LatestCache.SaveData.Achievements[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();

            (LatestCache.SaveData.Achievements[Achievements.ExtremeBondageEnjoyer.Id] as ThresholdAchievement)?.UpdateThreshold(0);

            // Validate the world tour achievement.
            var territory = _clientMonitor.TerritoryId;
            // Ensure it has been longer than 2 minutes since the recorded time. (in UTC)
            if (LatestCache.SaveData.VisitedWorldTour.ContainsKey(territory) && LatestCache.SaveData.VisitedWorldTour[territory] is false)
            {
                // Fail the conditional task.
                (LatestCache.SaveData.Achievements[Achievements.TourDeBound.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
                worldTourStartedTime = DateTime.MinValue;
            }
        }
    }

    private void OnRestraintLock(Guid restraintId, Padlocks padlock, bool isLocking, string enactorUID)
    {
        Logger.LogTrace(enactorUID + " is " + (isLocking ? "locking" : "unlocking") + " a set that had the padlock: " + padlock.ToName());
        // we locked our set.
        if (isLocking)
        {
            if (padlock is not Padlocks.None or Padlocks.FiveMinutesPadlock)
            {
                // make sure that someone is locking us up in a set.
                if (true /*enactorUID != MainHub.UID*/)
                {
                    (LatestCache.SaveData.Achievements[Achievements.FirstTimeBondage.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.AmateurBondage.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.ComfortRestraint.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.YourBondageMaid.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.YourRubberMaid.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.TrainedBondageSlave.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.YourRubberSlut.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.ATrueBondageSlave.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                }
            }
        }
        else
        { 
            // if the set is being unlocked, stop progress regardless.
            if (padlock is not Padlocks.None or Padlocks.FiveMinutesPadlock)
            {
                (LatestCache.SaveData.Achievements[Achievements.FirstTimeBondage.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.AmateurBondage.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.ComfortRestraint.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.YourBondageMaid.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.YourRubberMaid.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.TrainedBondageSlave.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.YourRubberSlut.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.ATrueBondageSlave.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
            }
        }
    }

    /// <summary> Whenever we are applying a restraint set to a pair. This is fired in our pair manager once we recieve  </summary>
    private void OnPairRestraintStateChange(Guid setName, bool isEnabling, string enactorUID, string affectedUID)
    {
        Logger.LogTrace(enactorUID + " is "+ (isEnabling ? "applying" : "Removing") + " a set to a pair: " + setName);
        // if we enabled a set on someone else
        if (isEnabling && enactorUID == MainHub.UID)
        {
            (LatestCache.SaveData.Achievements[Achievements.FirstTiemers.Id] as ProgressAchievement)?.IncrementProgress();
            (LatestCache.SaveData.Achievements[Achievements.DiDEnthusiast.Id] as ProgressAchievement)?.IncrementProgress();
            (LatestCache.SaveData.Achievements[Achievements.YourFavoriteNurse.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
        }
    }

    private void OnPairRestraintLockChange(Guid restraintId, Padlocks padlock, bool isLocking, string enactorUID, string affectedPairUID) // uid is self applied if client.
    {
        // May need to figure this for pairs upon connection to validate any actions/unlocks that occured while we were away.
        Logger.LogInformation("Pair Restraint Lock Change: " + padlock.ToName() + " " + isLocking + " " + enactorUID, LoggerType.AchievementEvents);

        // if the pair's set is being locked and it is a timed lock.
        if (isLocking)
        {
            if (padlock is not Padlocks.None or Padlocks.FiveMinutesPadlock) // locking
            {
                // make sure we are the locker before continuing
                if(enactorUID == MainHub.UID)
                {
                    (LatestCache.SaveData.Achievements[Achievements.RiggersFirstSession.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (LatestCache.SaveData.Achievements[Achievements.MyLittlePlaything.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (LatestCache.SaveData.Achievements[Achievements.SuitsYouBitch.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (LatestCache.SaveData.Achievements[Achievements.TiesThatBind.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (LatestCache.SaveData.Achievements[Achievements.SlaveTrainer.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (LatestCache.SaveData.Achievements[Achievements.CeremonyOfEternalBondage.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                }
            }
        }
        if(!isLocking)
        {
            // if the padlock is a timed padlock that we have unlocked, we should stop tracking it from these achievements.
            if (padlock is not Padlocks.None or Padlocks.FiveMinutesPadlock)
            {
                (LatestCache.SaveData.Achievements[Achievements.RiggersFirstSession.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (LatestCache.SaveData.Achievements[Achievements.MyLittlePlaything.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (LatestCache.SaveData.Achievements[Achievements.SuitsYouBitch.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (LatestCache.SaveData.Achievements[Achievements.TiesThatBind.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (LatestCache.SaveData.Achievements[Achievements.SlaveTrainer.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (LatestCache.SaveData.Achievements[Achievements.CeremonyOfEternalBondage.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
            }

            // if we are unlocking in general, increment the rescuer
            (LatestCache.SaveData.Achievements[Achievements.TheRescuer.Id] as ProgressAchievement)?.IncrementProgress();
        }
    }

    private void OnPuppetAccessGiven(PuppetPerms permissionGiven)
    {
        if ((permissionGiven & PuppetPerms.All) != 0)
            (LatestCache.SaveData.Achievements[Achievements.CompleteDevotion.Id] as ProgressAchievement)?.IncrementProgress();

        if ((permissionGiven & PuppetPerms.Alias) != 0)
        {
            // Nothing yet.
        }

        if ((permissionGiven & PuppetPerms.Emotes) != 0)
            (LatestCache.SaveData.Achievements[Achievements.ControlMyBody.Id] as ProgressAchievement)?.IncrementProgress();

        if ((permissionGiven & PuppetPerms.Sit) != 0)
        {
            // Nothing yet.
        }
    }

    private void OnPatternAction(PatternInteractionKind actionType, Guid patternGuid, bool wasAlarm)
    {
        switch (actionType)
        {
            case PatternInteractionKind.Published:
                (LatestCache.SaveData.Achievements[Achievements.MyPleasantriesForAll.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.DeviousComposer.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Downloaded:
                (LatestCache.SaveData.Achievements[Achievements.TasteOfTemptation.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.SeekerOfSensations.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.CravingPleasure.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Liked:
                (LatestCache.SaveData.Achievements[Achievements.GoodVibes.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.DelightfulPleasures.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.PatternLover.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.SensualConnoisseur.Id] as ProgressAchievement)?.IncrementProgress();
                (LatestCache.SaveData.Achievements[Achievements.PassionateAdmirer.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Started:
                if (patternGuid != Guid.Empty)
                {
                    (LatestCache.SaveData.Achievements[Achievements.ALittleTease.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.ShortButSweet.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.TemptingRythms.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.MyBuildingDesire.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.WithWavesOfSensation.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.WithHeightenedSensations.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.MusicalMoaner.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.StimulatingExperiences.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.EnduranceKing.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (LatestCache.SaveData.Achievements[Achievements.EnduranceQueen.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);

                    // motivation for restoration: Unlike the DutyStart check, this accounts for us starting a pattern AFTER entering Diadem.
                    if(_clientMonitor.TerritoryId is 939 && (LatestCache.SaveData.Achievements[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.TaskStarted is false)
                        (LatestCache.SaveData.Achievements[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.StartTask();
                }
                if (wasAlarm && patternGuid != Guid.Empty)
                    (LatestCache.SaveData.Achievements[Achievements.HornyMornings.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Stopped:
                if (patternGuid != Guid.Empty)
                    (LatestCache.SaveData.Achievements[Achievements.ALittleTease.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.ShortButSweet.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.TemptingRythms.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.MyBuildingDesire.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.WithWavesOfSensation.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.WithHeightenedSensations.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.MusicalMoaner.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.StimulatingExperiences.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.EnduranceKing.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (LatestCache.SaveData.Achievements[Achievements.EnduranceQueen.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                // motivation for restoration:
                (LatestCache.SaveData.Achievements[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
                return;
        }
    }

    private void OnDeviceConnected()
    {
        (LatestCache.SaveData.Achievements[Achievements.CollectorOfSinfulTreasures.Id] as ConditionalAchievement)?.CheckCompletion();
    }

    private void OnTriggerFired()
    {
        (LatestCache.SaveData.Achievements[Achievements.SubtleReminders.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.LostInTheMoment.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.TriggerHappy.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void ClientHardcoreFollowChanged(string enactorUID, NewState newState)
    {
        Logger.LogDebug("We just had another pair set our ForceFollow to " + newState, LoggerType.AchievementInfo);
        // client will always be the affectedUID
        var affectedUID = MainHub.UID;

        // if the new state is enabled, we need to begin tracking on the relevant achievements.
        if (newState is NewState.Enabled)
        {
            // begin tracking for the world tour. (if we dont meet all conditions it wont start anyways so dont worry about it.
            (LatestCache.SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

            // Begin tracking for the walkies achievements.
            (LatestCache.SaveData.Achievements[Achievements.TimeForWalkies.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (LatestCache.SaveData.Achievements[Achievements.GettingStepsIn.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (LatestCache.SaveData.Achievements[Achievements.WalkiesLover.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }

        // if anyone is disabling us, run a completion check. Failure to meet required time will result in resetting the task.
        if (newState is NewState.Disabled)
        {
            // halt tracking for walk of shame if any requirements are no longer met.
            (LatestCache.SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

            // halt any tracking on walkies achievement.
            (LatestCache.SaveData.Achievements[Achievements.TimeForWalkies.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (LatestCache.SaveData.Achievements[Achievements.GettingStepsIn.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (LatestCache.SaveData.Achievements[Achievements.WalkiesLover.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

            // halt progress on being bound throughout a duty if forcedFollow is disabled at any point.
            (LatestCache.SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
        }
    }

    private void PairHardcoreFollowChanged(string enactorUID, string affectedUID, NewState newState)
    {
        Logger.LogDebug("You have set a pairs forcedFollow to " + newState, LoggerType.AchievementInfo);
        // Check to see if we are the one toggling this or if it was someone else.
        var enactorWasSelf = enactorUID == MainHub.UID;

        // if the new state is enabled but we are not the enactor, we should ignore startTracking period.
        if (newState is NewState.Enabled)
        {
            // dont allow tracking for the enabled state by any pairs that are not us.
            if (!enactorWasSelf)
            {
                Logger.LogDebug("We should not be tracking hardcore achievements for any pairs that we are not directly applying hardcore actions to!", LoggerType.AchievementInfo);
                return;
            }

            // Handle tracking for all achievements that we need to initialize the follow command on another pair for.
            (LatestCache.SaveData.Achievements[Achievements.AllTheCollarsOfTheRainbow.Id] as ProgressAchievement)?.IncrementProgress();

            // Handle the tracking start for the pair we just forced to follow, using our affectedUID as the item to track.
            // (We do this so that if another pair enacts the disable we still remove it.)
            (LatestCache.SaveData.Achievements[Achievements.ForcedFollow.Id] as DurationAchievement)?.StartTracking(affectedUID, affectedUID);
            (LatestCache.SaveData.Achievements[Achievements.ForcedWalkies.Id] as DurationAchievement)?.StartTracking(affectedUID, affectedUID);
        }

        // if the new state is disabled
        if (newState is NewState.Disabled)
        {
            // it doesn't madder who the enactor was, we should halt tracking for any progress made once that pair is disabled.
            (LatestCache.SaveData.Achievements[Achievements.ForcedFollow.Id] as DurationAchievement)?.StopTracking(affectedUID, affectedUID);
            (LatestCache.SaveData.Achievements[Achievements.ForcedWalkies.Id] as DurationAchievement)?.StopTracking(affectedUID, affectedUID);
        }
    }

    private void ClientHardcoreEmoteStateChanged(string enactorUID, NewState newState)
    {
        Logger.LogDebug("We just had another pair set our ForceEmote to " + newState, LoggerType.AchievementInfo);
        // client will always be the affectedUID
        var affectedUID = MainHub.UID;

        if (newState is NewState.Enabled)
        {
            (LatestCache.SaveData.Achievements[Achievements.LivingFurniture.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }

        if (newState is NewState.Disabled)
        {
            (LatestCache.SaveData.Achievements[Achievements.LivingFurniture.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
        }
    }

    private void PairHardcoreEmoteChanged(string enactorUID, string affectedUID, NewState newState)
    {
        // Nothing here currently.
    }

    private void ClientHardcoreStayChanged(string enactorUID, NewState newState)
    {
        Logger.LogDebug("We just had another pair set our ForceStay to " + newState, LoggerType.AchievementInfo);
        // client will always be the affectedUID
        var affectedUID = MainHub.UID;

        // and we have been ordered to start being forced to stay:
        if (newState is NewState.Enabled)
        {
            (LatestCache.SaveData.Achievements[Achievements.OfDomesticDiscipline.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (LatestCache.SaveData.Achievements[Achievements.HomeboundSubmission.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (LatestCache.SaveData.Achievements[Achievements.PerfectHousePet.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }
        else if(newState is NewState.Disabled)
        {
            (LatestCache.SaveData.Achievements[Achievements.OfDomesticDiscipline.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (LatestCache.SaveData.Achievements[Achievements.HomeboundSubmission.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (LatestCache.SaveData.Achievements[Achievements.PerfectHousePet.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
        }
    }

    private void PairHardcoreStayChanged(string enactorUID, string affectedUID, NewState newState)
    {
        // Nothing currently.
    }

    private void ClientHardcoreBlindfoldChanged(string enactorUID, NewState newState)
    {
        Logger.LogDebug("We just had another pair set our ForceBlindfold to " + newState, LoggerType.AchievementInfo);
        // client will always be the affectedUID
        var affectedUID = MainHub.UID;

        if (newState is NewState.Enabled)
        {
            // always check if walk of shame can be started.
            (LatestCache.SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

            // Check for conditional task.
            (LatestCache.SaveData.Achievements[Achievements.BlindLeadingTheBlind.Id] as ConditionalAchievement)?.CheckCompletion();

            // Startup timed ones.
            (LatestCache.SaveData.Achievements[Achievements.WhoNeedsToSee.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }

        if (newState is NewState.Disabled)
        {
            (LatestCache.SaveData.Achievements[Achievements.WhoNeedsToSee.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

            // stop walk of shame since one of its requirements are not fulfilled.
            (LatestCache.SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.InterruptTask();
        }
    }

    private void PairHardcoreBlindfoldChanged(string enactorUID, string affectedUID, NewState newState)
    {
        // Nothing currently.
    }

    /// <summary>
    /// For whenever a hardcore action is enabled or disabled. This can come from client change or pair change, so look out for differences.
    /// </summary>
    /// <param name="actionKind"> The kind of hardcore action that was performed. </param>
    /// <param name="state"> If the hardcore action began or ended. </param>
    /// <param name="affectedPairUID"> who the target of the action is. </param>
    /// <param name="enactorUID"> Who Called the action. </param>
    private void OnHardcoreAction(InteractionType actionKind, NewState state, string enactorUID, string affectedPairUID)
    {
        Logger.LogDebug("Hardcore Action: " + actionKind + " State: " + state + " Enactor: " + enactorUID + " Affected: " + affectedPairUID, LoggerType.AchievementInfo);
        
        var affectedPairIsSelf = affectedPairUID == MainHub.UID;

        if (actionKind is InteractionType.ForcedFollow)
        {
            if (affectedPairIsSelf) ClientHardcoreFollowChanged(enactorUID, state);
            else PairHardcoreFollowChanged(enactorUID, affectedPairUID, state);
        }
        else if (actionKind is InteractionType.ForcedEmoteState)
        {
            if (affectedPairIsSelf) ClientHardcoreEmoteStateChanged(enactorUID, state);
            else PairHardcoreEmoteChanged(enactorUID, affectedPairUID, state);
        }
        else if (actionKind is InteractionType.ForcedStay)
        {
            if (affectedPairIsSelf) ClientHardcoreStayChanged(enactorUID, state);
            else PairHardcoreStayChanged(enactorUID, affectedPairUID, state);
        }
    }

    private void OnShockSent()
    {
        (LatestCache.SaveData.Achievements[Achievements.IndulgingSparks.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.ShockingTemptations.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.TheCrazeOfShockies.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.WickedThunder.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.ElectropeHasNoLimits.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnShockReceived()
    {
        (LatestCache.SaveData.Achievements[Achievements.ElectrifyingPleasure.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.ShockingExperience.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.WiredForObedience.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.ShockAddiction.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.SlaveToTheShock.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.ShockSlut.Id] as ProgressAchievement)?.IncrementProgress();
    }


    private void OnChatMessage(ChatChannel.Channels channel)
    {
        (LatestCache.SaveData.Achievements[Achievements.HelplessDamsel.Id] as ConditionalAchievement)?.CheckCompletion();

        if (channel is ChatChannel.Channels.Say)
        {
            (LatestCache.SaveData.Achievements[Achievements.OfVoicelessPleas.Id] as ProgressAchievement)?.IncrementProgress();
            (LatestCache.SaveData.Achievements[Achievements.DefianceInSilence.Id] as ProgressAchievement)?.IncrementProgress();
            (LatestCache.SaveData.Achievements[Achievements.MuffledResilience.Id] as ProgressAchievement)?.IncrementProgress();
            (LatestCache.SaveData.Achievements[Achievements.TrainedInSubSpeech.Id] as ProgressAchievement)?.IncrementProgress();

        }
        else if (channel is ChatChannel.Channels.Yell)
        {
            (LatestCache.SaveData.Achievements[Achievements.PublicSpeaker.Id] as ProgressAchievement)?.IncrementProgress();
        }
        else if (channel is ChatChannel.Channels.Shout)
        {
            (LatestCache.SaveData.Achievements[Achievements.FromCriesOfHumility.Id] as ProgressAchievement)?.IncrementProgress();
        }
    }

    private void OnEmoteExecuted(IGameObject emoteCallerObj, ushort emoteId, IGameObject targetObject)
    {
        switch (emoteId)
        {
            case 22: // Lookout
                if(emoteCallerObj.ObjectIndex is 0)
                    (LatestCache.SaveData.Achievements[Achievements.WhatAView.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 36: // Stagger
                if (emoteCallerObj.ObjectIndex is 0)
                    (LatestCache.SaveData.Achievements[Achievements.VulnerableVibrations.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 105: // Stroke
                if (emoteCallerObj.ObjectIndex is 0 && targetObject.ObjectIndex is 0)
                    (LatestCache.SaveData.Achievements[Achievements.ProlificPetter.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 111: // Slap
                if (emoteCallerObj.ObjectIndex is not 0 && targetObject.ObjectIndex is 0)
                    (LatestCache.SaveData.Achievements[Achievements.ICantBelieveYouveDoneThis.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 146: //Dote
                if (emoteCallerObj.ObjectIndex is 0 && targetObject.ObjectIndex is not 0)
                    (LatestCache.SaveData.Achievements[Achievements.WithAKissGoodbye.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 231:
                if (emoteCallerObj.ObjectIndex is 0)
                {
                    (LatestCache.SaveData.Achievements[Achievements.QuietNowDear.Id] as ConditionalAchievement)?.CheckCompletion();
                }
                else if (emoteCallerObj.ObjectIndex is not 0 && targetObject.ObjectIndex is 0)
                {
                    (LatestCache.SaveData.Achievements[Achievements.SilenceOfShame.Id] as ConditionalAchievement)?.CheckCompletion();
                }
                else
                {
                    break;
                }
                break;

        }
    }

    private void OnPuppeteerOrderSent(PuppeteerMsgType orderType)
    {
        switch(orderType)
        {
            case PuppeteerMsgType.GrovelOrder:
                (LatestCache.SaveData.Achievements[Achievements.KissMyHeels.Id] as ProgressAchievement)?.IncrementProgress();
                break;

            case PuppeteerMsgType.DanceOrder:
                (LatestCache.SaveData.Achievements[Achievements.AMaestroOfMyProperty.Id] as ProgressAchievement)?.IncrementProgress();
                break;
        }
        // Increase regardless.
        (LatestCache.SaveData.Achievements[Achievements.MasterOfPuppets.Id] as TimedProgressAchievement)?.IncrementProgress();
        // inc the orders given counters.
        (LatestCache.SaveData.Achievements[Achievements.OrchestratorsApprentice.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.NoStringsAttached.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.PuppetMaster.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.MasterOfManipulation.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.TheGrandConductor.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.MaestroOfStrings.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.OfGrandiousSymphony.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.SovereignMaestro.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.OrchestratorOfMinds.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnPuppeteerReceivedOrder()
    {
        // inc the orders recieved counters.
        (LatestCache.SaveData.Achievements[Achievements.WillingPuppet.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.AtYourCommand.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.YourMarionette.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.TheInstrument.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.AMannequinsMadness.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.DevotedDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.EnthralledDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.ObedientDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.ServiceDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.MastersPlaything.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.MistressesPlaything.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.ThePerfectDoll.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnPuppeteerReceivedEmoteOrder(int emoteId)
    {
        switch(emoteId)
        {
            case 38: // Sulk
                (LatestCache.SaveData.Achievements[Achievements.Ashamed.Id] as ProgressAchievement)?.IncrementProgress();
                break;
            case 50: // Sit/Groundsit
            case 52:
                (LatestCache.SaveData.Achievements[Achievements.AnObedientPet.Id] as ProgressAchievement)?.IncrementProgress();
                break;
            case 223: //Sweep
                (LatestCache.SaveData.Achievements[Achievements.HouseServant.Id] as ProgressAchievement)?.IncrementProgress();
                break;
        }
    }

    private void OnPairAdded()
    {
        (LatestCache.SaveData.Achievements[Achievements.KinkyNovice.Id] as ConditionalAchievement)?.CheckCompletion();
        (LatestCache.SaveData.Achievements[Achievements.TheCollector.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnCursedLootFound()
    {
        (LatestCache.SaveData.Achievements[Achievements.TemptingFatesTreasure.Id] as ProgressAchievement)?.IncrementProgress();
        (LatestCache.SaveData.Achievements[Achievements.BadEndSeeker.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
        (LatestCache.SaveData.Achievements[Achievements.EverCursed.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
    }

    private void OnJobChange(uint newJobId)
    {
        (LatestCache.SaveData.Achievements[Achievements.EscapingIsNotEasy.Id] as ConditionalAchievement)?.CheckCompletion();
    }

    private void OnVibratorToggled(NewState newState)
    {
        if (newState is NewState.Enabled)
        {
            (LatestCache.SaveData.Achievements[Achievements.GaggedPleasure.Id] as ConditionalAchievement)?.CheckCompletion();
            (LatestCache.SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion();
        }
        else
        {

        }
    }

    private void OnPvpKill()
    {
        (LatestCache.SaveData.Achievements[Achievements.EscapedPatient.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (LatestCache.SaveData.Achievements[Achievements.BoundToKill.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (LatestCache.SaveData.Achievements[Achievements.TheShackledSlayer.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (LatestCache.SaveData.Achievements[Achievements.DangerousConvict.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (LatestCache.SaveData.Achievements[Achievements.OfUnyieldingForce.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (LatestCache.SaveData.Achievements[Achievements.StimulationOverdrive.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (LatestCache.SaveData.Achievements[Achievements.BoundYetUnbroken.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (LatestCache.SaveData.Achievements[Achievements.ChainsCantHoldMe.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
    }


    // We need to check for knockback effects in gold sacuer.
    private void OnActionEffectEvent(List<ActionEffectEntry> actionEffects)
    {

        // Check if client player is null
        if (!_clientMonitor.IsPresent)
            return;

        // Return if not in the gold saucer
        if (_clientMonitor.TerritoryId is not 144)
            return;

        // Check if the GagReflex achievement is already completed
        var gagReflexAchievement = LatestCache.SaveData.Achievements[Achievements.GagReflex.Id] as ProgressAchievement;
        if (gagReflexAchievement is null || gagReflexAchievement.IsCompleted)
        {
            Logger.LogInformation("GagReflex achievement is already completed or is null");
            return;
        }

        Logger.LogInformation("Current State: [GateDirectorValid]: " + Content.GateDirectorIsValid 
            + " [GateType]: " + Content.GetActiveGate()
            + " [Flags]: " + Content.GetGateFlags()
            + " [InGateWithKB] " + Content.IsInGateWithKnockback());

        // Check if the player is in a gate with knockback
        if (!Content.IsInGateWithKnockback())
        {
            Logger.LogInformation("Player is not in a gate with knockback");
            return;
        }

        // Check if any effects were a knockback effect targeting the local player
        if (actionEffects.Any(x => x.Type == LimitedActionEffectType.Knockback && x.TargetID == _clientMonitor.ObjectId))
        {
            // Increment progress if the achievement is not yet completed
            gagReflexAchievement.IncrementProgress();
        }
    }
}
