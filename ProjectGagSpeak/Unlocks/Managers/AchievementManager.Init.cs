using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using System.Runtime.InteropServices;

namespace GagSpeak.Achievements;

public partial class AchievementManager
{
    public void InitializeAchievements()
    {
        // Module Finished
        #region ORDERS MODULE
        SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.JustAVolunteer, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Finished");
        SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.AsYouCommand, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Finished");
        SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.AnythingForMyOwner, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Finished");
        SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.GoodDrone, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Finished");

        SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.BadSlut, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Failed");
        SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.NeedsTraining, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Failed");
        SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.UsefulInOtherWays, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Failed");

        SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.NewSlaveOwner, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Created");
        SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.TaskManager, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Created");
        SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.MaidMaster, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Created");
        SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.QueenOfDrones, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Created");
        #endregion ORDERS MODULE

        // Module Finished
        #region GAG MODULE
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.SelfApplied, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags Self-Applied");

        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.SilenceSlut, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.WatchYourTongue, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.TongueTamer, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.KinkyLibrarian, 500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.OrchestratorOfSilence, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");

        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.SilencedSlut, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.InDeepSilence, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.SilentObsessions, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.GoldenSilence, 500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.AKinkForDrool, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.ThePerfectGagSlut, 5000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");

        SaveData.AddThreshold(AchievementModuleKind.Gags, Achievements.ShushtainableResource, 3, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags Active at Once");

        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.OfVoicelessPleas, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.DefianceInSilence, 500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.MuffledResilience, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.TrainedInSubSpeech, 2500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.PublicSpeaker, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.FromCriesOfHumility, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");

        SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.WhispersToWhimpers, TimeSpan.FromMinutes(5), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes Gagged", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.OfMuffledMoans, TimeSpan.FromMinutes(10), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes Gagged", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.SilentStruggler, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes Gagged", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.QuietedCaptive, TimeSpan.FromHours(1), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hour Gagged", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.MessyDrooler, TimeSpan.FromHours(6), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hours Gagged", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.DroolingDiva, TimeSpan.FromHours(12), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hours Gagged", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.EmbraceOfSilence, TimeSpan.FromDays(1), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Day Gagged", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.SubjugationToSilence, TimeSpan.FromDays(4), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days Gagged", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.SpeechSilverSilenceGolden, TimeSpan.FromDays(7), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days Gagged", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.TheKinkyLegend, TimeSpan.FromDays(14), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days Gagged", "Spent");

        SaveData.AddConditionalProgress(AchievementModuleKind.Gags, Achievements.SilentButDeadly, 10,
            () => _playerData.AppearanceData?.GagSlots.Any(x => x.GagType.ToGagType() != GagType.None) ?? false, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Roulettes Completed");

        SaveData.AddTimedProgress(AchievementModuleKind.Gags, Achievements.ATrueGagSlut, 10, TimeSpan.FromHours(1), (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags Received In Hour");

        SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.GagReflex, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gag Reflexes Experienced");

        SaveData.AddConditional(AchievementModuleKind.Gags, Achievements.QuietNowDear, () =>
        {
            bool targetIsGagged = false;
            if (_pairManager.GetVisiblePairGameObjects().Any(x => x.GameObjectId == _clientService.TargetObjectId))
            {
                Logger.LogTrace("Target is visible in the pair manager, checking if they are gagged.", LoggerType.Achievements);
                var targetPair = _pairManager.DirectPairs.FirstOrDefault(x => x.VisiblePairGameObject?.GameObjectId == _clientService.TargetObjectId);
                if (targetPair is not null)
                {
                    Logger.LogTrace("Target is in the direct pairs, checking if they are gagged.", LoggerType.Achievements);
                    targetIsGagged = targetPair.LastAppearanceData?.GagSlots.Any(x => x.GagType.ToGagType() is not GagType.None) ?? false;
                }
            }
            return targetIsGagged;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Kinkster Hushed");

        SaveData.AddConditional(AchievementModuleKind.Gags, Achievements.SilenceOfShame, () => _playerData.IsPlayerGagged, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Kinksters", "Hushed by");

        SaveData.AddConditionalProgress(AchievementModuleKind.Gags, Achievements.YourFavoriteNurse, 20,
            () => _playerData.AppearanceData?.GagSlots.Any(x => x.GagType.ToGagType() == GagType.MedicalMask) ?? false, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Patients Serviced", reqBeginAndFinish: false);

        SaveData.AddConditionalProgress(AchievementModuleKind.Gags, Achievements.SayMmmph, 1, () => _playerData.IsPlayerGagged, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Photos Taken");
        #endregion GAG MODULE

        #region WARDROBE MODULE
        SaveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.FirstTiemers, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Applied");
        SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.Cuffed19, 19, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Cuffs Applied");
        SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.TheRescuer, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Unlocked");
        SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.SelfBondageEnthusiast, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Applied");
        SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.DiDEnthusiast, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Applied");

        SaveData.AddConditionalThreshold(AchievementModuleKind.Wardrobe,Achievements.CrowdPleaser, 15,
            () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "People Nearby");
        SaveData.AddConditionalThreshold(AchievementModuleKind.Wardrobe,Achievements.Humiliation, 5,
            () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "GagSpeak Pairs Nearby");

        SaveData.AddTimedProgress(AchievementModuleKind.Wardrobe,Achievements.BondageBunny, 5, TimeSpan.FromHours(2), (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Received In 2 Hours");

        SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.ToDyeFor, 5, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Dyed");
        SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.DyeAnotherDay, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Dyed");
        SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.DyeHard, 15, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Dyed");

        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.RiggersFirstSession, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.MyLittlePlaything, TimeSpan.FromHours(1), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hour");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.SuitsYouBitch, TimeSpan.FromHours(6), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hours");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.TiesThatBind, TimeSpan.FromDays(1), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Day");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.SlaveTrainer, TimeSpan.FromDays(7), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.CeremonyOfEternalBondage, TimeSpan.FromDays(30), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days");

        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.FirstTimeBondage, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes locked up", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.AmateurBondage, TimeSpan.FromHours(1), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hour locked up", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.ComfortRestraint, TimeSpan.FromHours(6), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hours locked up", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.YourBondageMaid, TimeSpan.FromDays(1), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Day locked up", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.YourRubberMaid, TimeSpan.FromDays(4), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days locked up", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.TrainedBondageSlave, TimeSpan.FromDays(7), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days locked up", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.YourRubberSlut, TimeSpan.FromDays(4), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days locked up", "Spent");
        SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.ATrueBondageSlave, TimeSpan.FromDays(30), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days locked up", "Spent");

        SaveData.AddConditional(AchievementModuleKind.Wardrobe,Achievements.KinkyExplorer, () => _clientConfigs.GagspeakConfig.CursedDungeonLoot, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Cursed Runs Started");
        SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.TemptingFatesTreasure, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Cursed Loot Discovered");
        SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.BadEndSeeker, 25,
            () => _clientConfigs.CursedLootConfig.CursedLootStorage.LockChance <= 25, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Cursed Loot Discovered", reqBeginAndFinish: false);
        SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.EverCursed, 100,
            () => _clientConfigs.CursedLootConfig.CursedLootStorage.LockChance <= 25, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Cursed Loot Discovered", reqBeginAndFinish: false);

        SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.HealSlut, 1,
            () => _playerData.IsPlayerGagged || _clientConfigs.GetActiveSetIdx() != -1 || _vibeService.ConnectedToyActive, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Duties Completed");

        SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.BondagePalace, 1, () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "FloorSets Cleared");
        SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.HornyOnHigh, 1, () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "FloorSets Cleared");
        SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.EurekaWhorethos, 1, () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "FloorSets Cleared");
        SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.MyKinkRunsDeep, 1, () =>
        {
            var activeSet = _clientConfigs.GetActiveSet();
            if (activeSet is null) return false;
            return activeSet.PropertiesEnabledForUser(activeSet.EnabledBy);
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "FloorSets Cleared");
        SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.MyKinksRunDeeper, 1, () =>
        {
            var activeSet = _clientConfigs.GetActiveSet();
            if (activeSet is null) return false;
            return activeSet.PropertiesEnabledForUser(activeSet.EnabledBy);
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "FloorSets Cleared");

        SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.TrialOfFocus, 1, () =>
        {
            if (_clientService.Level < 90) return false;
            var activeSet = _clientConfigs.GetActiveSet();
            if (activeSet is null) return false;

            if (activeSet.SetTraits.TryGetValue(activeSet.EnabledBy, out var prop))
                return prop.StimulationLevel is not StimulationLevel.None;

            return false;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hardcore Trials Cleared");
        SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.TrialOfDexterity, 1, () =>
        {
            if (_clientService.Level < 90) return false;
            var activeSet = _clientConfigs.GetActiveSet();
            if (activeSet is null) return false;

            if (activeSet.SetTraits.TryGetValue(activeSet.EnabledBy, out var prop))
                return prop.ArmsRestrained || prop.LegsRestrained;

            return false;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hardcore Trials Cleared");
        SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.TrialOfTheBlind, 1, () =>
        {
            if (_clientService.Level < 90) return false;
            // get the set and make sure stimulation is enabled for the person who enabled it.
            var activeSet = _clientConfigs.GetActiveSet();
            if (activeSet is null) return false;

            if (activeSet.SetTraits.TryGetValue(activeSet.EnabledBy, out var prop))
                return prop.Blindfolded;

            return false;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hardcore Trials Cleared");

        // While actively moving, incorrectly guess a restraint lock while gagged (Secret)
        SaveData.AddConditional(AchievementModuleKind.Wardrobe,Achievements.RunningGag, () =>
        {
            unsafe
            {
                var gameControl = FFXIVClientStructs.FFXIV.Client.Game.Control.Control.Instance();
                var movementByte = Marshal.ReadByte((nint)gameControl, 30211);
                var movementDetection = AgentMap.Instance();
                // do a marshal read from this byte offset if it doesnt return proper value.
                var result = movementDetection->IsPlayerMoving;
                StaticLogger.Logger.LogInformation("IsPlayerMoving Result: " + result +" || IsWalking Byte: "+movementByte);
                return _playerData.IsPlayerGagged && _clientConfigs.GetActiveSetIdx() == -1 && result == 1 && movementByte == 0;
            }
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Funny Conditions Met");

        // Check this in the action function handler
        SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.AuctionedOff, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Auctioned Off", suffix: "Times");

        // Check this in the action function handler
        SaveData.AddConditional(AchievementModuleKind.Wardrobe,Achievements.SoldSlave,
            () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Sold off in Bondage ", suffix: "Times");

        // Bondodge - Within 2 seconds of having a restraint set applied to you, remove it from yourself (might want to add a duration conditional but idk?)
        SaveData.AddTimeLimitedConditional(AchievementModuleKind.Wardrobe,Achievements.Bondodge,
            TimeSpan.FromSeconds(2), () => _clientConfigs.GetActiveSetIdx() != -1, DurationTimeUnit.Seconds, (id, name) => WasCompleted(id, name).ConfigureAwait(false));

        #endregion WARDROBE MODULE

        // Module Finished
        #region PUPPETEER MODULE
        // (can work both ways)
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.AnObedientPet, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Recieved", suffix: "Sit Orders");

        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.ControlMyBody, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Granted", suffix: "Pairs Access");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.CompleteDevotion, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Granted", suffix: "Pairs Access");

        SaveData.AddTimedProgress(AchievementModuleKind.Puppeteer,Achievements.MasterOfPuppets, 10, TimeSpan.FromHours(1), (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Within the last Hour");

        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.KissMyHeels, 50, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Ordered", suffix: "Grovels");

        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.Ashamed, 5, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Ordered to sulk", suffix: "Times");

        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.HouseServant, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Ordered to sweep", suffix: "Times");

        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.AMaestroOfMyProperty, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Ordered", suffix: "Dances");

        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.OrchestratorsApprentice, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.NoStringsAttached, 25, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.PuppetMaster, 50, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.MasterOfManipulation, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.TheGrandConductor, 250, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.MaestroOfStrings, 500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.OfGrandiousSymphony, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.SovereignMaestro, 2500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.OrchestratorOfMinds, 5000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");

        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.WillingPuppet, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.AtYourCommand, 25, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.YourMarionette, 50, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.TheInstrument, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.AMannequinsMadness, 250, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.DevotedDoll, 500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.EnthralledDoll, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.ObedientDoll, 1750, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.ServiceDoll, 2500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.MastersPlaything, 5000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.MistressesPlaything, 5000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.ThePerfectDoll, 10000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        #endregion PUPPETEER MODULE

        // Module Finished
        #region TOYBOX MODULE
        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.MyPleasantriesForAll, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Published", suffix: "Patterns");
        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.DeviousComposer, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Published", suffix: "Patterns");

        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.TasteOfTemptation, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Downloaded", suffix: "Patterns");
        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.SeekerOfSensations, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Downloaded", suffix: "Patterns");
        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.CravingPleasure, 30, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Downloaded", suffix: "Patterns");

        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.GoodVibes, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.DelightfulPleasures, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.PatternLover, 25, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.SensualConnoisseur, 50, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.PassionateAdmirer, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");

        SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.ALittleTease, TimeSpan.FromSeconds(20), DurationTimeUnit.Seconds, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Seconds", "Vibrated for");
        SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.ShortButSweet, TimeSpan.FromMinutes(1), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.TemptingRythms, TimeSpan.FromMinutes(2), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.MyBuildingDesire, TimeSpan.FromMinutes(5), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.WithWavesOfSensation, TimeSpan.FromMinutes(10), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.WithHeightenedSensations, TimeSpan.FromMinutes(15), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.MusicalMoaner, TimeSpan.FromMinutes(20), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.StimulatingExperiences, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.EnduranceKing, TimeSpan.FromMinutes(59), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.EnduranceQueen, TimeSpan.FromMinutes(59), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");

        SaveData.AddConditional(AchievementModuleKind.Toybox,Achievements.CollectorOfSinfulTreasures, () =>
        { return (_playerData.GlobalPerms?.HasValidShareCode() ?? false) || _vibeService.DeviceHandler.AnyDeviceConnected; }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Devices Connected");

        SaveData.AddRequiredTimeConditional(AchievementModuleKind.Toybox,Achievements.MotivationForRestoration, TimeSpan.FromMinutes(30),
            () => _clientConfigs.ActivePatternGuid() != Guid.Empty, DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), suffix: " Vibrated in Diadem");

        SaveData.AddConditional(AchievementModuleKind.Toybox, Achievements.VulnerableVibrations, () => _clientConfigs.ActivePatternGuid() != Guid.Empty, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Staggers Performed");

        SaveData.AddConditional(AchievementModuleKind.Toybox,Achievements.KinkyGambler,
            () => _clientConfigs.ActiveSocialTriggers.Count() > 0, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "DeathRolls Gambled");

        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.SubtleReminders, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Triggers Fired");
        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.LostInTheMoment, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Triggers Fired");
        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.TriggerHappy, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Triggers Fired");

        SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.HornyMornings, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Alarms Went Off");
        #endregion TOYBOX MODULE

        #region HARDCORE MODULE
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.AllTheCollarsOfTheRainbow, 20, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Forced", suffix: "Pairs To Follow You");

        SaveData.AddConditionalProgress(AchievementModuleKind.Hardcore,Achievements.UCanTieThis, 1,
            () => _playerData.GlobalPerms?.IsFollowing() ?? false, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Completed", suffix: "Duties in ForcedFollow.");

        // Forced follow achievements
        SaveData.AddDuration(AchievementModuleKind.Hardcore,Achievements.ForcedFollow, TimeSpan.FromMinutes(1), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Leashed a Kinkster for");
        SaveData.AddDuration(AchievementModuleKind.Hardcore,Achievements.ForcedWalkies, TimeSpan.FromMinutes(5), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Leashed a Kinkster for");

        // Time for Walkies achievements
        SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.TimeForWalkies, TimeSpan.FromMinutes(1), () => _playerData.GlobalPerms?.IsFollowing() ?? false, DurationTimeUnit.Seconds, (id, name) => WasCompleted(id, name).ConfigureAwait(false));
        SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.GettingStepsIn, TimeSpan.FromMinutes(5), () => _playerData.GlobalPerms?.IsFollowing() ?? false, DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false));
        SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.WalkiesLover, TimeSpan.FromMinutes(10), () => _playerData.GlobalPerms?.IsFollowing() ?? false, DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false));

        //Part of the Furniture - Be forced to sit for 1 hour or more
        SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.LivingFurniture, TimeSpan.FromHours(1), () => _playerData.GlobalPerms?.IsSitting() ?? false, DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), suffix: "Forced to Sit");

        SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.WalkOfShame, TimeSpan.FromMinutes(5),
            () =>
            {
                if (_clientConfigs.GetActiveSetIdx() != -1 && (_playerData.GlobalPerms?.IsBlindfolded() ?? false) && (_playerData.GlobalPerms?.IsFollowing() ?? false))
                    if (_clientService.InMainCity)
                        return true;
                return false;
            }, DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Walked for", suffix: "In a Major City");

        SaveData.AddConditional(AchievementModuleKind.Hardcore,Achievements.BlindLeadingTheBlind,
            () =>
            {
                if (_playerData.GlobalPerms?.IsBlindfolded() ?? false)
                    if (_pairManager.DirectPairs.Any(x => x.PairGlobals.IsFollowing() && x.PairGlobals.IsBlindfolded()))
                        return true;
                return false;
            }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Blind Pairs Led");

        SaveData.AddConditional(AchievementModuleKind.Hardcore,Achievements.WhatAView, () => (_playerData.GlobalPerms?.IsBlindfolded() ?? false), (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Blind Lookouts Performed");

        SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.WhoNeedsToSee, TimeSpan.FromHours(3), () => (_playerData.GlobalPerms?.IsBlindfolded() ?? false), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false));

        SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.OfDomesticDiscipline, TimeSpan.FromMinutes(30), () => (_playerData.GlobalPerms?.IsStaying() ?? false), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false));
        SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.HomeboundSubmission, TimeSpan.FromHours(1), () => (_playerData.GlobalPerms?.IsStaying() ?? false), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false));
        SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.PerfectHousePet, TimeSpan.FromDays(1), () => (_playerData.GlobalPerms?.IsStaying() ?? false), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false));

        // Shock-related achievements - Give out shocks
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.IndulgingSparks, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Sent");
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ShockingTemptations, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Sent");
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.TheCrazeOfShockies, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Sent");
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.WickedThunder, 10000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Sent");
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ElectropeHasNoLimits, 25000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Sent");

        // Shock-related achievements - Get shocked
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ElectrifyingPleasure, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ShockingExperience, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.WiredForObedience, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ShockAddiction, 10000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.SlaveToTheShock, 25000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ShockSlut, 50000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        #endregion HARDCORE MODULE

        #region REMOTES MODULE
        SaveData.AddProgress(AchievementModuleKind.Remotes, Achievements.JustVibing, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Remotes Opened");

        // TODO: Make this turning down someone else's once its implemented.
        // (on second thought this could introduce lots of issues so maybe not? Look into later idk, for now its dormant.)
        SaveData.AddProgress(AchievementModuleKind.Remotes, Achievements.DontKillMyVibe, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Vibes Killed");

        SaveData.AddProgress(AchievementModuleKind.Remotes, Achievements.VibingWithFriends, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Rooms Joined");
        #endregion REMOTES MODULE

        #region GENERIC MODULE
        SaveData.AddProgress(AchievementModuleKind.Generic, Achievements.TutorialComplete, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Tutorial Completed");

        SaveData.AddConditional(AchievementModuleKind.Generic, Achievements.KinkyNovice, () => _pairManager.DirectPairs.Count > 0, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Pair Added");

        SaveData.AddProgress(AchievementModuleKind.Generic, Achievements.TheCollector, 20, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Pairs Added");

        SaveData.AddProgress(AchievementModuleKind.Generic, Achievements.BoundaryRespecter, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Presets Applied");

        SaveData.AddProgress(AchievementModuleKind.Generic, Achievements.HelloKinkyWorld, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Global Messages Sent");

        SaveData.AddProgress(AchievementModuleKind.Generic, Achievements.KnowsMyLimits, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Safewords Used");

        SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.WarriorOfLewd, 1,
            () => _playerData.IsPlayerGagged && _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), suffix: "Cutscenes Watched Bound & Gagged");

        SaveData.AddConditional(AchievementModuleKind.Generic, Achievements.EscapingIsNotEasy, () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Escape Attempts Made");

        SaveData.AddConditional(AchievementModuleKind.Generic, Achievements.ICantBelieveYouveDoneThis, () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Slaps Received");

        SaveData.AddConditional(AchievementModuleKind.Generic, Achievements.WithAKissGoodbye, () =>
        {
            bool targetIsImmobile = false;
            if (_pairManager.GetVisiblePairGameObjects().Any(x => x.GameObjectId == _clientService.TargetObjectId))
            {
                Logger.LogTrace("Target is visible in the pair manager, checking if they are gagged.", LoggerType.Achievements);
                var targetPair = _pairManager.DirectPairs.FirstOrDefault(x => x.VisiblePairGameObject?.GameObjectId == _clientService.TargetObjectId);
                if (targetPair is not null)
                {
                    Logger.LogTrace("Target is in the direct pairs, checking if they are gagged.", LoggerType.Achievements);
                    // store if they are stuck emoting.
                    targetIsImmobile = targetPair.PairGlobals.IsAnySitting();
                    var lightRestraintActive = targetPair.LastLightStorage?.Restraints.FirstOrDefault(x => x.Identifier == targetPair.LastWardrobeData?.ActiveSetId);
                    if(lightRestraintActive is not null && lightRestraintActive.HardcoreTraits.TryGetValue(targetPair.LastWardrobeData?.ActiveSetEnabledBy ?? "", out var traits))
                    {
                        Logger.LogTrace("Targets active set enabled by someone that locked it with immobilize.", LoggerType.Achievements);
                        targetIsImmobile = traits.Immobile;
                    }
                }
            }
            return targetIsImmobile;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Dotes to Helpless Kinksters", "Gave");

        SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.ProlificPetter, 10, () =>
        {
            bool targetIsImmobile = false;
            if (_pairManager.GetVisiblePairGameObjects().Any(x => x.GameObjectId == _clientService.TargetObjectId))
            {
                var targetPair = _pairManager.DirectPairs.FirstOrDefault(x => x.VisiblePairGameObject?.GameObjectId == _clientService.TargetObjectId);
                if (targetPair is not null)
                {
                    // store if they are stuck emoting.
                    targetIsImmobile = targetPair.PairGlobals.IsAnySitting();
                    var lightRestraintActive = targetPair.LastLightStorage?.Restraints.FirstOrDefault(x => x.Identifier == targetPair.LastWardrobeData?.ActiveSetId);
                    if (lightRestraintActive is not null && lightRestraintActive.HardcoreTraits.TryGetValue(targetPair.LastWardrobeData?.ActiveSetEnabledBy ?? "", out var traits))
                    {
                        targetIsImmobile = traits.Immobile;
                    }
                }
            }
            return targetIsImmobile;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Helpless Kinksters", "Pet", false);

        SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.EscapedPatient, 10, () => _clientService.InPvP, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Players Slain", "", false);
        SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.BoundToKill, 25, () => _clientService.InPvP, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Players Slain", "", false);
        SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.TheShackledSlayer, 50, () => _clientService.InPvP, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Players Slain", "", false);
        SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.DangerousConvict, 100, () => _clientService.InPvP, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Players Slain", "", false);
        SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.OfUnyieldingForce, 200, () => _clientService.InPvP, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Players Slain", "", false);
        SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.StimulationOverdrive, 300, () => _clientService.InPvP, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Players Slain", "", false);
        SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.BoundYetUnbroken, 400, () => _clientService.InPvP, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Players Slain", "", false);
        SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.ChainsCantHoldMe, 500, () => _clientService.InPvP, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Players Slain", "", false);
        #endregion GENERIC MODULE

        #region SECRETS MODULE
        SaveData.AddProgress(AchievementModuleKind.Secrets, Achievements.HiddenInPlainSight, 6, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Found", suffix: "Easter Eggs", isSecret: true);

        SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.Experimentalist, () =>
        {
            return _playerData.IsPlayerGagged && _clientConfigs.GetActiveSetIdx() != -1 && _clientConfigs.ActivePatternGuid() != Guid.Empty
            && _clientConfigs.ActiveTriggers.Count() > 0 && _clientConfigs.ActiveAlarmCount > 0 && _vibeService.ConnectedToyActive;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Met", suffix: "Conditions", isSecret: true);

        SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.HelplessDamsel, () =>
        {
            return _playerData.IsPlayerGagged && _clientConfigs.GetActiveSetIdx() != -1 && _vibeService.ConnectedToyActive && _pairManager.DirectPairs.Any(x => x.OwnPerms.InHardcore)
            && (_playerData.GlobalPerms?.IsFollowing() ?? false) || (_playerData.GlobalPerms?.IsSitting() ?? false);
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Met", suffix: "Hardcore Conditions", isSecret: true);

        SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.GaggedPleasure, () => _vibeService.ConnectedToyActive && _playerData.IsPlayerGagged, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Pleasure Requirements Met", isSecret: true);
        SaveData.AddThreshold(AchievementModuleKind.Secrets, Achievements.BondageClub, 8, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Club Members Gathered", isSecret: true);
        SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.BadEndHostage, () => _clientConfigs.GetActiveSetIdx() != -1 && _clientService.IsDead, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Encountered", suffix: "Bad Ends", isSecret: true);
        SaveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.TourDeBound, 11, () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Taken", suffix: "Tours in Bondage", isSecret: true);
        SaveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.MuffledProtagonist, 1, () => _playerData.IsPlayerGagged && _playerData.GlobalPerms!.LiveChatGarblerActive, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "MissTypes Made", isSecret: true);
        // The above is currently non functional as i dont have the data to know which chat message type contains these request tasks.

        SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.BoundgeeJumping, () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Attempted", suffix: "Dangerous Acts", isSecret: true);
        SaveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.KinkyTeacher, 10, () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Thanks Received", reqBeginAndFinish: false, isSecret: true);
        SaveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.KinkyProfessor, 50, () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Thanks Received", reqBeginAndFinish: false, isSecret: true);
        SaveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.KinkyMentor, 100, () => _clientConfigs.GetActiveSetIdx() != -1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Thanks Received", reqBeginAndFinish: false, isSecret: true);
        SaveData.AddThreshold(AchievementModuleKind.Secrets, Achievements.ExtremeBondageEnjoyer, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restriction Conditions Satisfied", isSecret: true); 
        SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.WildRide, () =>
        {
            bool raceEndVisible = false;
            unsafe
            {
                var raceEnded = (AtkUnitBase*)AtkFuckery.GetAddonByName("RaceChocoboResult");
                if (raceEnded != null)
                    raceEndVisible = raceEnded->RootNode->IsVisible();
            };
            return _clientService.IsChocoboRacing && raceEndVisible && _clientConfigs.GetActiveSetIdx() != -1;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Races Won In Unusual Conditions", isSecret: true);

        SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.SlavePresentation, () =>
        {
            return _clientConfigs.GetActiveSetIdx() != -1 && _playerData.IsPlayerGagged;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Presentations Given on Stage", isSecret: true);
        #endregion SECRETS MODULE

    }
}
