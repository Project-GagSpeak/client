using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Localization;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Triggers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using Penumbra.GameData.Enums;
using static System.Windows.Forms.AxHost;

namespace GagSpeak.Achievements;
public partial class AchievementManager
{
    private void OnCommendationsGiven(int amount)
    {
        (SaveData.Achievements[Achievements.KinkyTeacher.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(amount);
        (SaveData.Achievements[Achievements.KinkyProfessor.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(amount);
        (SaveData.Achievements[Achievements.KinkyMentor.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(amount);
    }

    private void OnPairVisible()
    {
        // We need to obtain the total visible user count, then update the respective achievements.
        var visiblePairs = _pairManager.GetVisibleUserCount();
        (SaveData.Achievements[Achievements.BondageClub.Id] as ThresholdAchievement)?.UpdateThreshold(visiblePairs);
        (SaveData.Achievements[Achievements.Humiliation.Id] as ConditionalThresholdAchievement)?.UpdateThreshold(visiblePairs);
    }

    private void OnIconClicked(string windowLabel)
    {
        if (SaveData.EasterEggIcons.ContainsKey(windowLabel) && SaveData.EasterEggIcons[windowLabel] is false)
        {
            if (SaveData.EasterEggIcons[windowLabel])
                return;
            else
                SaveData.EasterEggIcons[windowLabel] = true;
            // update progress.
            (SaveData.Achievements[Achievements.HiddenInPlainSight.Id] as ProgressAchievement)?.IncrementProgress();
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
                if (SaveData.VisitedWorldTour.ContainsKey(prevZone) && SaveData.VisitedWorldTour[prevZone] is false)
                {
                    // Mark the conditonal as finished in the achievement, and mark as completed.
                    if (_clientConfigs.GetActiveSetIdx() != -1)
                    {
                        (SaveData.Achievements[Achievements.TourDeBound.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                        SaveData.VisitedWorldTour[prevZone] = true;
                    }
                    else
                        (SaveData.Achievements[Achievements.TourDeBound.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
                    // reset the datetime to .MinValue
                    worldTourStartedTime = DateTime.MinValue;
                }
            }
        }
    }

    private void CheckOnZoneSwitchEnd()
    {
        Logger.LogTrace("Current Territory Id: " + _clientService.TerritoryId, LoggerType.AchievementEvents);
        if(_clientService.InMainCity)
            (SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

        ushort territory = _clientService.TerritoryId;

        // if present in diadem (for diamdem achievement) (Accounts for going into diadem while a vibe is running)
        if (territory is 939 && !_clientService.InPvP)
            (SaveData.Achievements[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        else
            (SaveData.Achievements[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

        // If we left before completing the duty, check that here.
        if ((SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();

        if ((SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();

        if ((SaveData.Achievements[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (SaveData.Achievements[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();

        // Check to see if we qualify for starting any world tour conditions.
        if (SaveData.VisitedWorldTour.ContainsKey(territory) && SaveData.VisitedWorldTour[territory] is false)
        {
            // if its already true, dont worry about it.
            if (SaveData.VisitedWorldTour[territory] is true)
            {
                Logger.LogTrace("World Tour Progress already completed for: " + territory, LoggerType.AchievementEvents);
                Logger.LogTrace("Current Progress for all items is: " + string.Join(", ", SaveData.VisitedWorldTour.Select(x => x.Key + " : " + x.Value)), LoggerType.Achievements);

                return;
            }
            else // Begin the progress for this city's world tour. 
            {
                Logger.LogTrace("Starting World Tour Progress for: " + territory, LoggerType.AchievementEvents);
                (SaveData.Achievements[Achievements.TourDeBound.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                worldTourStartedTime = DateTime.UtcNow;
            }
        }
    }

    private void CheckDeepDungeonStatus()
    {
        // Detect Specific Dungeon Types
        if (!AchievementHelpers.InDeepDungeon()) return;

        var floor = AchievementHelpers.GetFloor();
        if (floor is null) 
            return;

        var deepDungeonType = _clientService.GetDeepDungeonType();
        if (deepDungeonType is null) 
            return;

        if (_clientService.PartySize is 1)
            (SaveData.Achievements[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
        // start this under any condition.
        (SaveData.Achievements[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();


        switch (deepDungeonType)
        {
            case DeepDungeonType.PalaceOfTheDead:
                if ((floor > 40 && floor <= 50) || (floor > 90 && floor <= 100))
                {
                    (SaveData.Achievements[Achievements.BondagePalace.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                    if (floor is 50 || floor is 100)
                    {
                        (SaveData.Achievements[Achievements.BondagePalace.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                        (SaveData.Achievements[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                        (SaveData.Achievements[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                    }
                }
                break;
            case DeepDungeonType.HeavenOnHigh:
                if (floor > 20 && floor <= 30)
                {
                    (SaveData.Achievements[Achievements.HornyOnHigh.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                    if (floor is 30)
                    {
                        (SaveData.Achievements[Achievements.HornyOnHigh.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                        (SaveData.Achievements[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                        (SaveData.Achievements[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                    }
                }
                break;
            case DeepDungeonType.EurekaOrthos:
                if (floor > 20 && floor <= 30)
                {
                    (SaveData.Achievements[Achievements.EurekaWhorethos.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                    if (floor is 30)
                    {
                        (SaveData.Achievements[Achievements.EurekaWhorethos.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                        (SaveData.Achievements[Achievements.MyKinkRunsDeep.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                        (SaveData.Achievements[Achievements.MyKinksRunDeeper.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
                    }
                }
                break;
        }
    }

    private void OnDutyStart(object? sender, ushort e)
    {
        Logger.LogInformation("Duty Started", LoggerType.AchievementEvents);
        if (_clientService.InPvP)
            return;

        (SaveData.Achievements[Achievements.KinkyExplorer.Id] as ConditionalAchievement)?.CheckCompletion();

        (SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
        (SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.BeginConditionalTask(25); // 10s delay.

        if (_clientService.ClientPlayer.ClassJobRole() is ActionRoles.Healer)
            (SaveData.Achievements[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();

        // If the party size is 8, let's check for the trials.
        if(_clientService.PartySize is 8 && _clientService.Level >= 90)
        {
            (SaveData.Achievements[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
            (SaveData.Achievements[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
            (SaveData.Achievements[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
        }

        // check stuff for deep dungeons.
        CheckDeepDungeonStatus();
    }

    private void OnDutyEnd(object? sender, ushort e)
    {
        if (_clientService.InPvP)
            return;
        Logger.LogInformation("Duty Ended", LoggerType.AchievementEvents);
        if ((SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();

        if ((SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();

        if ((SaveData.Achievements[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
            (SaveData.Achievements[Achievements.HealSlut.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();

        // Trial has ended, check for completion.
        if ((SaveData.Achievements[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
        {
            if (_clientService.PartySize is 8 && _clientService.Level >= 90)
                (SaveData.Achievements[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
            else // if we are not in a full party, we should reset the task.
                (SaveData.Achievements[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
        }
        
        if ((SaveData.Achievements[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
        {
            if (_clientService.PartySize is 8 && _clientService.Level >= 90)
                (SaveData.Achievements[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
            else // if we are not in a full party, we should reset the task.
                (SaveData.Achievements[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
        }
        
        if ((SaveData.Achievements[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
        {
            if (_clientService.PartySize is 8 && _clientService.Level >= 90)
                (SaveData.Achievements[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.FinishConditionalTask();
            else // if we are not in a full party, we should reset the task.
                (SaveData.Achievements[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
        }

        // check stuff for deep dungeons.
        CheckDeepDungeonStatus();
    }

    private void OnOrderAction(OrderInteractionKind orderKind)
    {
        switch (orderKind)
        {
            case OrderInteractionKind.Completed:
                (SaveData.Achievements[Achievements.JustAVolunteer.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.AsYouCommand.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.AnythingForMyOwner.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.GoodDrone.Id] as ProgressAchievement)?.IncrementProgress();
                break;
            case OrderInteractionKind.Fail:
                (SaveData.Achievements[Achievements.BadSlut.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.NeedsTraining.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.UsefulInOtherWays.Id] as ProgressAchievement)?.IncrementProgress();
                break;
            case OrderInteractionKind.Create:
                (SaveData.Achievements[Achievements.NewSlaveOwner.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.TaskManager.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.MaidMaster.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.QueenOfDrones.Id] as ProgressAchievement)?.IncrementProgress();
                break;
        }
    }

    private void OnGagStateChanged(bool applying, GagLayer gagLayer, GagType gagAppliedOrRemoved, string enactorUid)
    {
        if (gagAppliedOrRemoved is GagType.None) return;

        string trackingKey = gagLayer.ToString() + '_' + gagAppliedOrRemoved.GagName();

        // for enables.
        if (applying)
        {
            // the gag was applied to us by ourselves.
            if (enactorUid == MainHub.UID)
            {
                (SaveData.Achievements[Achievements.SelfApplied.Id] as ProgressAchievement)?.IncrementProgress();
            }
            // the gag was applied to us by someone else.
            else
            {
                (SaveData.Achievements[Achievements.SilencedSlut.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.InDeepSilence.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.SilentObsessions.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.GoldenSilence.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.AKinkForDrool.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.ThePerfectGagSlut.Id] as ProgressAchievement)?.IncrementProgress();

                (SaveData.Achievements[Achievements.ATrueGagSlut.Id] as TimedProgressAchievement)?.IncrementProgress();
            }

            // track regardless of who applied it.
            (SaveData.Achievements[Achievements.WhispersToWhimpers.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.OfMuffledMoans.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.SilentStruggler.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.QuietedCaptive.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.MessyDrooler.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.DroolingDiva.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.EmbraceOfSilence.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.SubjugationToSilence.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.SpeechSilverSilenceGolden.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.TheKinkyLegend.Id] as DurationAchievement)?.StartTracking(trackingKey, MainHub.UID);

            (SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion();
            (SaveData.Achievements[Achievements.GaggedPleasure.Id] as ConditionalAchievement)?.CheckCompletion();

            (SaveData.Achievements[Achievements.ShushtainableResource.Id] as ThresholdAchievement)?.UpdateThreshold(_playerData.TotalGagsEquipped);
        }
        // for disables.
        else
        {
            (SaveData.Achievements[Achievements.ShushtainableResource.Id] as ThresholdAchievement)?.UpdateThreshold(_playerData.TotalGagsEquipped);

            (SaveData.Achievements[Achievements.WhispersToWhimpers.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.OfMuffledMoans.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.SilentStruggler.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.QuietedCaptive.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.MessyDrooler.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.DroolingDiva.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.EmbraceOfSilence.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.SubjugationToSilence.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.SpeechSilverSilenceGolden.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);
            (SaveData.Achievements[Achievements.TheKinkyLegend.Id] as DurationAchievement)?.StopTracking(trackingKey, MainHub.UID);

            // Halt our Silent But Deadly Progress if gag is removed mid-dungeon
            if ((SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.ConditionalTaskBegun ?? false)
                (SaveData.Achievements[Achievements.SilentButDeadly.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
        }
    }

    private void OnPairGagStateChanged(bool isApplying, GagLayer layer, GagType gag, string assignerUid, string affectedUid)
    {
        if(isApplying)
        {
            if (gag is not GagType.None)
            {
                (SaveData.Achievements[Achievements.SilenceSlut.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.WatchYourTongue.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.TongueTamer.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.KinkyLibrarian.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.OrchestratorOfSilence.Id] as ProgressAchievement)?.IncrementProgress();

                (SaveData.Achievements[Achievements.YourFavoriteNurse.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
            }
        }
        else
        {
            // nothing for removing yet.
        }
    }

    private void OnGagLockStateChange(bool isLocking, GagLayer layer, Padlocks padlock, string assignerUid)
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

    private void OnPairGagLockStateChange(bool isLocking, GagLayer layer, Padlocks padlock, string assignerUid, string affectedUid)
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

    private void OnCharaOnlineCleanupForLatest(UserData user, CharaAppearanceData gagInfo, Guid activeRestraint)
    {
        var activeGagTrackingKeys = gagInfo.ActiveGagTrackingKeys();
        Logger.LogDebug("Player Character " + user.AliasOrUID + " went online and has new active data. Cleaning up expired information!", LoggerType.AchievementEvents);
        // Do stuff if its a gag type.
        (SaveData.Achievements[Achievements.WhispersToWhimpers.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (SaveData.Achievements[Achievements.OfMuffledMoans.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (SaveData.Achievements[Achievements.SilentStruggler.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (SaveData.Achievements[Achievements.QuietedCaptive.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (SaveData.Achievements[Achievements.MessyDrooler.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (SaveData.Achievements[Achievements.DroolingDiva.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (SaveData.Achievements[Achievements.EmbraceOfSilence.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (SaveData.Achievements[Achievements.SubjugationToSilence.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (SaveData.Achievements[Achievements.SpeechSilverSilenceGolden.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);
        (SaveData.Achievements[Achievements.TheKinkyLegend.Id] as DurationAchievement)?.CleanupTracking(user.UID, activeGagTrackingKeys);

        // Checks spesific to the direction of the application.
        if (user.UID == MainHub.UID)
        {
            (SaveData.Achievements[Achievements.FirstTimeBondage.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.AmateurBondage.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.ComfortRestraint.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.YourBondageMaid.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.YourRubberMaid.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.TrainedBondageSlave.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.YourRubberSlut.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.ATrueBondageSlave.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });

            (SaveData.Achievements[Achievements.ShushtainableResource.Id] as ThresholdAchievement)?.UpdateThreshold(_playerData.TotalGagsEquipped);
        }
        else
        {
            (SaveData.Achievements[Achievements.RiggersFirstSession.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.MyLittlePlaything.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.SuitsYouBitch.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.TiesThatBind.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.SlaveTrainer.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
            (SaveData.Achievements[Achievements.CeremonyOfEternalBondage.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { activeRestraint.ToString() });
        }

        // Do stuff if it is a pattern.
        (SaveData.Achievements[Achievements.ALittleTease.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (SaveData.Achievements[Achievements.ShortButSweet.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (SaveData.Achievements[Achievements.TemptingRythms.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (SaveData.Achievements[Achievements.MyBuildingDesire.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (SaveData.Achievements[Achievements.WithWavesOfSensation.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (SaveData.Achievements[Achievements.WithHeightenedSensations.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (SaveData.Achievements[Achievements.MusicalMoaner.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (SaveData.Achievements[Achievements.StimulatingExperiences.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (SaveData.Achievements[Achievements.EnduranceKing.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (SaveData.Achievements[Achievements.EnduranceQueen.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });

        // if these are started, inturrupt them so that they do not complete.
        (SaveData.Achievements[Achievements.ForcedFollow.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
        (SaveData.Achievements[Achievements.ForcedWalkies.Id] as DurationAchievement)?.CleanupTracking(user.UID, new List<string>() { Guid.Empty.ToString() });
    }

    private void OnRestraintSetUpdated(RestraintSet set)
    {
        // check for dyes
        if (set.DrawData.Any(x => x.Value.GameStain.Stain1 != 0 || x.Value.GameStain.Stain2 != 0))
        {
            (SaveData.Achievements[Achievements.ToDyeFor.Id] as ProgressAchievement)?.IncrementProgress();
            (SaveData.Achievements[Achievements.DyeAnotherDay.Id] as ProgressAchievement)?.IncrementProgress();
            (SaveData.Achievements[Achievements.DyeHard.Id] as ProgressAchievement)?.IncrementProgress();
        }
    }

    private void OnRestraintStateChange(Guid restraintId, bool isEnabling, string enactorUID)
    {
        // Check this regardless.
        (SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

        // Set is being enabled.
        if (isEnabling)
        {
            var territory = _clientService.TerritoryId;
            // Check to see if we qualify for starting any world tour conditions.
            if (SaveData.VisitedWorldTour.ContainsKey(territory) && SaveData.VisitedWorldTour[territory] is false)
            {
                // if its already true, dont worry about it.
                if (SaveData.VisitedWorldTour[territory] is true)
                {
                    Logger.LogTrace("World Tour Progress already completed for: " + territory, LoggerType.AchievementEvents);
                    return;
                }
                else // Begin the progress for this city's world tour. 
                {
                    Logger.LogTrace("Starting World Tour Progress for: " + territory, LoggerType.AchievementEvents);
                    (SaveData.Achievements[Achievements.TourDeBound.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                    worldTourStartedTime = DateTime.UtcNow;
                }
            }

            (SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion();
            (SaveData.Achievements[Achievements.FirstTiemers.Id] as ProgressAchievement)?.IncrementProgress();

            // if we are the applier
            if (enactorUID == MainHub.UID)
            {
                (SaveData.Achievements[Achievements.SelfBondageEnthusiast.Id] as ProgressAchievement)?.IncrementProgress();
            }
            else // someone else is enabling our set
            {
                (SaveData.Achievements[Achievements.AuctionedOff.Id] as ConditionalProgressAchievement)?.BeginConditionalTask();
                // starts the timer.
                (SaveData.Achievements[Achievements.Bondodge.Id] as TimeLimitConditionalAchievement)?.StartTask();

                // track overkill if it is not yet completed
                if((SaveData.Achievements[Achievements.ExtremeBondageEnjoyer.Id] as ThresholdAchievement)?.IsCompleted is false)
                {
                    if (_clientConfigs.StoredRestraintSets.TryGetItem(x => x.RestraintId == restraintId, out var setMatch))
                        (SaveData.Achievements[Achievements.ExtremeBondageEnjoyer.Id] as ThresholdAchievement)?.UpdateThreshold(setMatch.EquippedSlotsTotal);
                }

                // Track Bondage Bunny
                (SaveData.Achievements[Achievements.BondageBunny.Id] as TimedProgressAchievement)?.IncrementProgress();

                // see if valid for "cuffed-19" if it is not yet completed
                if ((SaveData.Achievements[Achievements.Cuffed19.Id] as ProgressAchievement)?.IsCompleted is false)
                {
                    // attempt to retrieve the set from our sets.
                    if (_clientConfigs.StoredRestraintSets.TryGetItem(x => x.RestraintId == restraintId, out var setMatch))
                        if (setMatch.DrawData.TryGetValue(EquipSlot.Hands, out var handData) && handData.GameItem.Id != ItemIdVars.NothingItem(EquipSlot.Hands).Id)
                            (SaveData.Achievements[Achievements.Cuffed19.Id] as ProgressAchievement)?.IncrementProgress();
                }
            }
        }
        else // set is being disabled
        {
            // must be removed within limit or wont award.
            (SaveData.Achievements[Achievements.Bondodge.Id] as TimeLimitConditionalAchievement)?.CheckCompletion();

            // If a set is being disabled at all, we should reset our conditionals.
            (SaveData.Achievements[Achievements.TrialOfFocus.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
            (SaveData.Achievements[Achievements.TrialOfDexterity.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
            (SaveData.Achievements[Achievements.TrialOfTheBlind.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();

            (SaveData.Achievements[Achievements.ExtremeBondageEnjoyer.Id] as ThresholdAchievement)?.UpdateThreshold(0);

            // Validate the world tour achievement.
            var territory = _clientService.TerritoryId;
            // Ensure it has been longer than 2 minutes since the recorded time. (in UTC)
            if (SaveData.VisitedWorldTour.ContainsKey(territory) && SaveData.VisitedWorldTour[territory] is false)
            {
                // Fail the conditional task.
                (SaveData.Achievements[Achievements.TourDeBound.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
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
                    (SaveData.Achievements[Achievements.FirstTimeBondage.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.AmateurBondage.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.ComfortRestraint.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.YourBondageMaid.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.YourRubberMaid.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.TrainedBondageSlave.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.YourRubberSlut.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.ATrueBondageSlave.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), MainHub.UID);
                }
            }
        }
        else
        { 
            // if the set is being unlocked, stop progress regardless.
            if (padlock is not Padlocks.None or Padlocks.FiveMinutesPadlock)
            {
                (SaveData.Achievements[Achievements.FirstTimeBondage.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.AmateurBondage.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.ComfortRestraint.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.YourBondageMaid.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.YourRubberMaid.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.TrainedBondageSlave.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.YourRubberSlut.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.ATrueBondageSlave.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), MainHub.UID);
            }
        }
    }

    /// <summary>
    /// Whenever we are applying a restraint set to a pair. This is fired in our pair manager once we recieve 
    /// </summary>
    private void OnPairRestraintStateChange(Guid setName, bool isEnabling, string enactorUID, string affectedUID)
    {
        Logger.LogTrace(enactorUID + " is "+ (isEnabling ? "applying" : "Removing") + " a set to a pair: " + setName);
        // if we enabled a set on someone else
        if (isEnabling && enactorUID == MainHub.UID)
        {
            (SaveData.Achievements[Achievements.FirstTiemers.Id] as ProgressAchievement)?.IncrementProgress();
            (SaveData.Achievements[Achievements.DiDEnthusiast.Id] as ProgressAchievement)?.IncrementProgress();
            (SaveData.Achievements[Achievements.YourFavoriteNurse.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
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
                    (SaveData.Achievements[Achievements.RiggersFirstSession.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (SaveData.Achievements[Achievements.MyLittlePlaything.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (SaveData.Achievements[Achievements.SuitsYouBitch.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (SaveData.Achievements[Achievements.TiesThatBind.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (SaveData.Achievements[Achievements.SlaveTrainer.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                    (SaveData.Achievements[Achievements.CeremonyOfEternalBondage.Id] as DurationAchievement)?.StartTracking(restraintId.ToString(), affectedPairUID);
                }
            }
        }
        if(!isLocking)
        {
            // if the padlock is a timed padlock that we have unlocked, we should stop tracking it from these achievements.
            if (padlock is not Padlocks.None or Padlocks.FiveMinutesPadlock)
            {
                (SaveData.Achievements[Achievements.RiggersFirstSession.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (SaveData.Achievements[Achievements.MyLittlePlaything.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (SaveData.Achievements[Achievements.SuitsYouBitch.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (SaveData.Achievements[Achievements.TiesThatBind.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (SaveData.Achievements[Achievements.SlaveTrainer.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
                (SaveData.Achievements[Achievements.CeremonyOfEternalBondage.Id] as DurationAchievement)?.StopTracking(restraintId.ToString(), affectedPairUID);
            }

            // if we are unlocking in general, increment the rescuer
            (SaveData.Achievements[Achievements.TheRescuer.Id] as ProgressAchievement)?.IncrementProgress();
        }
    }

    private void OnPuppetAccessGiven(bool wasAllPerms)
    {
        if (wasAllPerms) // All Perms access given to another pair.
            (SaveData.Achievements[Achievements.CompleteDevotion.Id] as ProgressAchievement)?.IncrementProgress();
        else // Emote perms given to another pair.
            (SaveData.Achievements[Achievements.ControlMyBody.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnPatternAction(PatternInteractionKind actionType, Guid patternGuid, bool wasAlarm)
    {
        switch (actionType)
        {
            case PatternInteractionKind.Published:
                (SaveData.Achievements[Achievements.MyPleasantriesForAll.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.DeviousComposer.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Downloaded:
                (SaveData.Achievements[Achievements.TasteOfTemptation.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.SeekerOfSensations.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.CravingPleasure.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Liked:
                (SaveData.Achievements[Achievements.GoodVibes.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.DelightfulPleasures.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.PatternLover.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.SensualConnoisseur.Id] as ProgressAchievement)?.IncrementProgress();
                (SaveData.Achievements[Achievements.PassionateAdmirer.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Started:
                if (patternGuid != Guid.Empty)
                {
                    (SaveData.Achievements[Achievements.ALittleTease.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.ShortButSweet.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.TemptingRythms.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.MyBuildingDesire.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.WithWavesOfSensation.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.WithHeightenedSensations.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.MusicalMoaner.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.StimulatingExperiences.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.EnduranceKing.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);
                    (SaveData.Achievements[Achievements.EnduranceQueen.Id] as DurationAchievement)?.StartTracking(patternGuid.ToString(), MainHub.UID);

                    // motivation for restoration: Unlike the DutyStart check, this accounts for us starting a pattern AFTER entering Diadem.
                    if(_clientService.TerritoryId is 939 && (SaveData.Achievements[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.TaskStarted is false)
                        (SaveData.Achievements[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.StartTask();
                }
                if (wasAlarm && patternGuid != Guid.Empty)
                    (SaveData.Achievements[Achievements.HornyMornings.Id] as ProgressAchievement)?.IncrementProgress();
                return;
            case PatternInteractionKind.Stopped:
                if (patternGuid != Guid.Empty)
                    (SaveData.Achievements[Achievements.ALittleTease.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.ShortButSweet.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.TemptingRythms.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.MyBuildingDesire.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.WithWavesOfSensation.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.WithHeightenedSensations.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.MusicalMoaner.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.StimulatingExperiences.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.EnduranceKing.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                (SaveData.Achievements[Achievements.EnduranceQueen.Id] as DurationAchievement)?.StopTracking(patternGuid.ToString(), MainHub.UID);
                // motivation for restoration:
                (SaveData.Achievements[Achievements.MotivationForRestoration.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
                return;
        }
    }

    private void OnDeviceConnected()
    {
        (SaveData.Achievements[Achievements.CollectorOfSinfulTreasures.Id] as ConditionalAchievement)?.CheckCompletion();
    }

    private void OnTriggerFired()
    {
        (SaveData.Achievements[Achievements.SubtleReminders.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.LostInTheMoment.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.TriggerHappy.Id] as ProgressAchievement)?.IncrementProgress();
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
            (SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

            // Begin tracking for the walkies achievements.
            (SaveData.Achievements[Achievements.TimeForWalkies.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (SaveData.Achievements[Achievements.GettingStepsIn.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (SaveData.Achievements[Achievements.WalkiesLover.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }

        // if anyone is disabling us, run a completion check. Failure to meet required time will result in resetting the task.
        if (newState is NewState.Disabled)
        {
            // halt tracking for walk of shame if any requirements are no longer met.
            (SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

            // halt any tracking on walkies achievement.
            (SaveData.Achievements[Achievements.TimeForWalkies.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (SaveData.Achievements[Achievements.GettingStepsIn.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (SaveData.Achievements[Achievements.WalkiesLover.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

            // halt progress on being bound throughout a duty if forcedFollow is disabled at any point.
            (SaveData.Achievements[Achievements.UCanTieThis.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt();
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
            (SaveData.Achievements[Achievements.AllTheCollarsOfTheRainbow.Id] as ProgressAchievement)?.IncrementProgress();

            // Handle the tracking start for the pair we just forced to follow, using our affectedUID as the item to track.
            // (We do this so that if another pair enacts the disable we still remove it.)
            (SaveData.Achievements[Achievements.ForcedFollow.Id] as DurationAchievement)?.StartTracking(affectedUID, affectedUID);
            (SaveData.Achievements[Achievements.ForcedWalkies.Id] as DurationAchievement)?.StartTracking(affectedUID, affectedUID);
        }

        // if the new state is disabled
        if (newState is NewState.Disabled)
        {
            // it doesn't madder who the enactor was, we should halt tracking for any progress made once that pair is disabled.
            (SaveData.Achievements[Achievements.ForcedFollow.Id] as DurationAchievement)?.StopTracking(affectedUID, affectedUID);
            (SaveData.Achievements[Achievements.ForcedWalkies.Id] as DurationAchievement)?.StopTracking(affectedUID, affectedUID);
        }
    }

    private void ClientHardcoreEmoteStateChanged(string enactorUID, NewState newState)
    {
        Logger.LogDebug("We just had another pair set our ForceEmote to " + newState, LoggerType.AchievementInfo);
        // client will always be the affectedUID
        var affectedUID = MainHub.UID;

        if (newState is NewState.Enabled)
        {
            (SaveData.Achievements[Achievements.LivingFurniture.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }

        if (newState is NewState.Disabled)
        {
            (SaveData.Achievements[Achievements.LivingFurniture.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
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
            (SaveData.Achievements[Achievements.OfDomesticDiscipline.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (SaveData.Achievements[Achievements.HomeboundSubmission.Id] as TimeRequiredConditionalAchievement)?.StartTask();
            (SaveData.Achievements[Achievements.PerfectHousePet.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }
        else if(newState is NewState.Disabled)
        {
            (SaveData.Achievements[Achievements.OfDomesticDiscipline.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (SaveData.Achievements[Achievements.HomeboundSubmission.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
            (SaveData.Achievements[Achievements.PerfectHousePet.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();
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
            (SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.StartTask();

            // Check for conditional task.
            (SaveData.Achievements[Achievements.BlindLeadingTheBlind.Id] as ConditionalAchievement)?.CheckCompletion();

            // Startup timed ones.
            (SaveData.Achievements[Achievements.WhoNeedsToSee.Id] as TimeRequiredConditionalAchievement)?.StartTask();
        }

        if (newState is NewState.Disabled)
        {
            (SaveData.Achievements[Achievements.WhoNeedsToSee.Id] as TimeRequiredConditionalAchievement)?.CheckCompletion();

            // stop walk of shame since one of its requirements are not fulfilled.
            (SaveData.Achievements[Achievements.WalkOfShame.Id] as TimeRequiredConditionalAchievement)?.InterruptTask();
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
        else if (actionKind is InteractionType.ForcedBlindfold)
        {
            if (affectedPairIsSelf) ClientHardcoreBlindfoldChanged(enactorUID, state);
            else PairHardcoreBlindfoldChanged(enactorUID, affectedPairUID, state);
        }
    }

    private void OnShockSent()
    {
        (SaveData.Achievements[Achievements.IndulgingSparks.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.ShockingTemptations.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.TheCrazeOfShockies.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.WickedThunder.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.ElectropeHasNoLimits.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnShockReceived()
    {
        (SaveData.Achievements[Achievements.ElectrifyingPleasure.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.ShockingExperience.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.WiredForObedience.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.ShockAddiction.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.SlaveToTheShock.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.ShockSlut.Id] as ProgressAchievement)?.IncrementProgress();
    }


    private void OnChatMessage(XivChatType channel)
    {
        (SaveData.Achievements[Achievements.HelplessDamsel.Id] as ConditionalAchievement)?.CheckCompletion();

        if (channel is XivChatType.Say)
        {
            (SaveData.Achievements[Achievements.OfVoicelessPleas.Id] as ProgressAchievement)?.IncrementProgress();
            (SaveData.Achievements[Achievements.DefianceInSilence.Id] as ProgressAchievement)?.IncrementProgress();
            (SaveData.Achievements[Achievements.MuffledResilience.Id] as ProgressAchievement)?.IncrementProgress();
            (SaveData.Achievements[Achievements.TrainedInSubSpeech.Id] as ProgressAchievement)?.IncrementProgress();

        }
        else if (channel is XivChatType.Yell)
        {
            (SaveData.Achievements[Achievements.PublicSpeaker.Id] as ProgressAchievement)?.IncrementProgress();
        }
        else if (channel is XivChatType.Shout)
        {
            (SaveData.Achievements[Achievements.FromCriesOfHumility.Id] as ProgressAchievement)?.IncrementProgress();
        }
    }

    private void OnEmoteExecuted(IGameObject emoteCallerObj, ushort emoteId, IGameObject targetObject)
    {
        switch (emoteId)
        {
            case 22: // Lookout
                if(emoteCallerObj.ObjectIndex is 0)
                    (SaveData.Achievements[Achievements.WhatAView.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 36: // Stagger
                if (emoteCallerObj.ObjectIndex is 0)
                    (SaveData.Achievements[Achievements.VulnerableVibrations.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 105: // Stroke
                if (emoteCallerObj.ObjectIndex is 0 && targetObject.ObjectIndex is 0)
                    (SaveData.Achievements[Achievements.ProlificPetter.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 111: // Slap
                if (emoteCallerObj.ObjectIndex is not 0 && targetObject.ObjectIndex is 0)
                    (SaveData.Achievements[Achievements.ICantBelieveYouveDoneThis.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 146: //Dote
                if (emoteCallerObj.ObjectIndex is 0 && targetObject.ObjectIndex is not 0)
                    (SaveData.Achievements[Achievements.WithAKissGoodbye.Id] as ConditionalAchievement)?.CheckCompletion();
                break;

            case 231:
                if (emoteCallerObj.ObjectIndex is 0)
                {
                    (SaveData.Achievements[Achievements.QuietNowDear.Id] as ConditionalAchievement)?.CheckCompletion();
                }
                else if (emoteCallerObj.ObjectIndex is not 0 && targetObject.ObjectIndex is 0)
                {
                    (SaveData.Achievements[Achievements.SilenceOfShame.Id] as ConditionalAchievement)?.CheckCompletion();
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
                (SaveData.Achievements[Achievements.KissMyHeels.Id] as ProgressAchievement)?.IncrementProgress();
                break;

            case PuppeteerMsgType.DanceOrder:
                (SaveData.Achievements[Achievements.AMaestroOfMyProperty.Id] as ProgressAchievement)?.IncrementProgress();
                break;
        }
        // Increase regardless.
        (SaveData.Achievements[Achievements.MasterOfPuppets.Id] as TimedProgressAchievement)?.IncrementProgress();
        // inc the orders given counters.
        (SaveData.Achievements[Achievements.OrchestratorsApprentice.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.NoStringsAttached.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.PuppetMaster.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.MasterOfManipulation.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.TheGrandConductor.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.MaestroOfStrings.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.OfGrandiousSymphony.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.SovereignMaestro.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.OrchestratorOfMinds.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnPuppeteerReceivedOrder()
    {
        // inc the orders recieved counters.
        (SaveData.Achievements[Achievements.WillingPuppet.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.AtYourCommand.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.YourMarionette.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.TheInstrument.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.AMannequinsMadness.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.DevotedDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.EnthralledDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.ObedientDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.ServiceDoll.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.MastersPlaything.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.MistressesPlaything.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.ThePerfectDoll.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnPuppeteerReceivedEmoteOrder(ushort emoteId)
    {
        switch(emoteId)
        {
            case 38: // Sulk
                (SaveData.Achievements[Achievements.Ashamed.Id] as ProgressAchievement)?.IncrementProgress();
                break;
            case 50: // Sit/Groundsit
            case 52:
                (SaveData.Achievements[Achievements.AnObedientPet.Id] as ProgressAchievement)?.IncrementProgress();
                break;
            case 223: //Sweep
                (SaveData.Achievements[Achievements.HouseServant.Id] as ProgressAchievement)?.IncrementProgress();
                break;
        }
    }

    private void OnPairAdded()
    {
        (SaveData.Achievements[Achievements.KinkyNovice.Id] as ConditionalAchievement)?.CheckCompletion();
        (SaveData.Achievements[Achievements.TheCollector.Id] as ProgressAchievement)?.IncrementProgress();
    }

    private void OnCursedLootFound()
    {
        (SaveData.Achievements[Achievements.TemptingFatesTreasure.Id] as ProgressAchievement)?.IncrementProgress();
        (SaveData.Achievements[Achievements.BadEndSeeker.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
        (SaveData.Achievements[Achievements.EverCursed.Id] as ConditionalProgressAchievement)?.CheckTaskProgress();
    }

    private void OnJobChange(uint newJobId)
    {
        (SaveData.Achievements[Achievements.EscapingIsNotEasy.Id] as ConditionalAchievement)?.CheckCompletion();
    }

    private void OnVibratorToggled(NewState newState)
    {
        if (newState is NewState.Enabled)
        {
            (SaveData.Achievements[Achievements.GaggedPleasure.Id] as ConditionalAchievement)?.CheckCompletion();
            (SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion();
        }
        else
        {

        }
    }

    private void OnPvpKill()
    {
        (SaveData.Achievements[Achievements.EscapedPatient.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (SaveData.Achievements[Achievements.BoundToKill.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (SaveData.Achievements[Achievements.TheShackledSlayer.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (SaveData.Achievements[Achievements.DangerousConvict.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (SaveData.Achievements[Achievements.OfUnyieldingForce.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (SaveData.Achievements[Achievements.StimulationOverdrive.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (SaveData.Achievements[Achievements.BoundYetUnbroken.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
        (SaveData.Achievements[Achievements.ChainsCantHoldMe.Id] as ConditionalProgressAchievement)?.CheckTaskProgress(1);
    }


    // We need to check for knockback effects in gold sacuer.
    private void OnActionEffectEvent(List<ActionEffectEntry> actionEffects)
    {

        // Check if client player is null
        if (!_clientService.IsPresent)
            return;

        // Return if not in the gold saucer
        if (_clientService.TerritoryId is not 144)
            return;

        // Check if the GagReflex achievement is already completed
        var gagReflexAchievement = SaveData.Achievements[Achievements.GagReflex.Id] as ProgressAchievement;
        if (gagReflexAchievement is null || gagReflexAchievement.IsCompleted)
        {
            Logger.LogInformation("GagReflex achievement is already completed or is null");
            return;
        }

        Logger.LogInformation("Current State: [GateDirectorValid]: " + AchievementHelpers.GateDirectorIsValid 
            + " [GateType]: " + AchievementHelpers.GetActiveGate()
            + " [Flags]: " + AchievementHelpers.GetGateFlags()
            + " [InGateWithKB] " + AchievementHelpers.IsInGateWithKnockback());

        // Check if the player is in a gate with knockback
        if (!AchievementHelpers.IsInGateWithKnockback())
        {
            Logger.LogInformation("Player is not in a gate with knockback");
            return;
        }

        // Check if any effects were a knockback effect targeting the local player
        if (actionEffects.Any(x => x.Type == LimitedActionEffectType.Knockback && x.TargetID == _clientService.ObjectId))
        {
            // Increment progress if the achievement is not yet completed
            gagReflexAchievement.IncrementProgress();
        }
    }
}
