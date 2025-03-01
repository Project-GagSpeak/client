using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;
using System.Runtime.InteropServices;

namespace GagSpeak.Achievements;

public partial class AchievementManager
{
    public void ReInitializeSaveData()
    {
        LatestCache = new SaveDataCache();
        InitializeAchievements();
    }

    public void InitializeAchievements()
    {
        // Module Finished
        #region ORDERS MODULE
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.JustAVolunteer, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Finished");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.AsYouCommand, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Finished");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.AnythingForMyOwner, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Finished");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.GoodDrone, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Finished");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.BadSlut, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Failed");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.NeedsTraining, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Failed");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.UsefulInOtherWays, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Failed");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.NewSlaveOwner, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Created");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.TaskManager, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Created");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.MaidMaster, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Created");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Orders, Achievements.QueenOfDrones, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Orders Created");
        #endregion ORDERS MODULE

        // Module Finished
        #region GAG MODULE
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.SelfApplied, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags Self-Applied");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.SilenceSlut, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.WatchYourTongue, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.TongueTamer, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.KinkyLibrarian, 500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.OrchestratorOfSilence, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.SilencedSlut, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.InDeepSilence, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.SilentObsessions, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.GoldenSilence, 500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.AKinkForDrool, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.ThePerfectGagSlut, 5000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");

        LatestCache.SaveData.AddThreshold(AchievementModuleKind.Gags, Achievements.ShushtainableResource, 3, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags Active at Once");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.OfVoicelessPleas, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.DefianceInSilence, 500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.MuffledResilience, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.TrainedInSubSpeech, 2500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.PublicSpeaker, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.FromCriesOfHumility, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Garbled Messages Sent");

        LatestCache.SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.WhispersToWhimpers, TimeSpan.FromMinutes(5), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes Gagged", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.OfMuffledMoans, TimeSpan.FromMinutes(10), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes Gagged", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.SilentStruggler, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes Gagged", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.QuietedCaptive, TimeSpan.FromHours(1), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hour Gagged", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.MessyDrooler, TimeSpan.FromHours(6), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hours Gagged", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.DroolingDiva, TimeSpan.FromHours(12), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hours Gagged", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.EmbraceOfSilence, TimeSpan.FromDays(1), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Day Gagged", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.SubjugationToSilence, TimeSpan.FromDays(4), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days Gagged", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.SpeechSilverSilenceGolden, TimeSpan.FromDays(7), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days Gagged", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Gags, Achievements.TheKinkyLegend, TimeSpan.FromDays(14), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days Gagged", "Spent");

        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Gags, Achievements.SilentButDeadly, 10,
            () => _gags.ActiveGagsData is { } gagData && gagData.AnyGagActive(), (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Roulettes Completed");

        LatestCache.SaveData.AddTimedProgress(AchievementModuleKind.Gags, Achievements.ATrueGagSlut, 10, TimeSpan.FromHours(1), (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gags Received In Hour");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Gags, Achievements.GagReflex, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Gag Reflexes Experienced");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Gags, Achievements.QuietNowDear, () =>
        {
            var targetIsGagged = false;
            if (_pairs.GetVisiblePairGameObjects().Any(x => x.GameObjectId == _clientMonitor.TargetObjectId))
            {
                Logger.LogTrace("Target is visible in the pair manager, checking if they are gagged.", LoggerType.Achievements);
                var targetPair = _pairs.DirectPairs.FirstOrDefault(x => x.VisiblePairGameObject?.GameObjectId == _clientMonitor.TargetObjectId);
                if (targetPair is not null)
                {
                    Logger.LogTrace("Target is in the direct pairs, checking if they are gagged.", LoggerType.Achievements);
                    targetIsGagged = targetPair.LastGagData.GagSlots.Any(x => x.GagItem is not GagType.None);
                }
            }
            return targetIsGagged;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Kinkster Hushed");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Gags, Achievements.SilenceOfShame, () => _gags.ActiveGagsData?.IsGagged() ?? false, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Kinksters", "Hushed by");

        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Gags, Achievements.YourFavoriteNurse, 20,
            () => _gags.ActiveGagsData is { } gagData && gagData.GagSlots.Any(x => x.GagItem == GagType.MedicalMask), (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Patients Serviced", reqBeginAndFinish: false);

        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Gags, Achievements.SayMmmph, 1, () => _gags.ActiveGagsData?.IsGagged() ?? false, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Photos Taken");
        #endregion GAG MODULE

        #region WARDROBE MODULE
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.FirstTiemers, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Applied");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.Cuffed19, 19, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Cuffs Applied");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.TheRescuer, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Unlocked");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.SelfBondageEnthusiast, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Applied");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.DiDEnthusiast, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Applied");

        LatestCache.SaveData.AddConditionalThreshold(AchievementModuleKind.Wardrobe,Achievements.CrowdPleaser, 15,
            () => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "People Nearby");
        LatestCache.SaveData.AddConditionalThreshold(AchievementModuleKind.Wardrobe,Achievements.Humiliation, 5,
            () => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "GagSpeak Pairs Nearby");

        LatestCache.SaveData.AddTimedProgress(AchievementModuleKind.Wardrobe,Achievements.BondageBunny, 5, TimeSpan.FromHours(2), (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Received In 2 Hours");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.ToDyeFor, 5, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Dyed");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.DyeAnotherDay, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Dyed");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.DyeHard, 15, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restraints Dyed");

        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.RiggersFirstSession, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.MyLittlePlaything, TimeSpan.FromHours(1), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hour");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.SuitsYouBitch, TimeSpan.FromHours(6), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hours");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.TiesThatBind, TimeSpan.FromDays(1), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Day");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.SlaveTrainer, TimeSpan.FromDays(7), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.CeremonyOfEternalBondage, TimeSpan.FromDays(30), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days");

        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.FirstTimeBondage, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes locked up", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.AmateurBondage, TimeSpan.FromHours(1), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hour locked up", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.ComfortRestraint, TimeSpan.FromHours(6), DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hours locked up", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.YourBondageMaid, TimeSpan.FromDays(1), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Day locked up", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.YourRubberMaid, TimeSpan.FromDays(4), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days locked up", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.TrainedBondageSlave, TimeSpan.FromDays(7), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days locked up", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.YourRubberSlut, TimeSpan.FromDays(14), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days locked up", "Spent");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Wardrobe,Achievements.ATrueBondageSlave, TimeSpan.FromDays(30), DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Days locked up", "Spent");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Wardrobe,Achievements.KinkyExplorer, () => _mainConfig.Config.CursedLootPanel, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Cursed Runs Started");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.TemptingFatesTreasure, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Cursed Loot Discovered");
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.BadEndSeeker, 25,
            () => _cursedLoot.LockChance <= 25, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Cursed Loot Discovered", reqBeginAndFinish: false);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.EverCursed, 100,
            () => _cursedLoot.LockChance <= 25, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Cursed Loot Discovered", reqBeginAndFinish: false);

        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.HealSlut, 1, () =>
        {
            int activeItems = 0;
            if(_gags.ActiveGagsData?.IsGagged() ?? false) activeItems++;
            if(_restraints.EnabledSet is not null) activeItems++;
            if(_sexToys.ConnectedToyActive) activeItems++;
            return activeItems >= 2;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Duties Completed");

        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.BondagePalace, 1, () 
            => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "FloorSets Cleared");
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.HornyOnHigh, 1, () 
            => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "FloorSets Cleared");
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.EurekaWhorethos, 1, () 
            => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "FloorSets Cleared");
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.MyKinkRunsDeep, 1, () 
            => _restraints.EnabledSet is not null && _traits.ActiveTraits != 0, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "FloorSets Cleared");
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.MyKinksRunDeeper, 1, ()
            => _restraints.EnabledSet is not null && _traits.ActiveTraits != 0, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "FloorSets Cleared");
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe,Achievements.TrialOfFocus, 1, () =>
        {
            if (_clientMonitor.Level < 90)
                return false;
            return (_restraints.EnabledSet is not null && (_traits.ActiveTraits & Traits.AnyStim) != 0) ? true : false;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hardcore Trials Cleared");
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.TrialOfDexterity, 1, () =>
        {
            if (_clientMonitor.Level < 90)
                return false;
            return (_restraints.EnabledSet is not null && (_traits.ActiveTraits & Traits.ArmsRestrained | Traits.LegsRestrained) != 0) ? true : false;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hardcore Trials Cleared");

        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.TrialOfTheBlind, 1, () =>
        {
            if (_clientMonitor.Level < 90)
                return false;
            return (_restraints.EnabledSet is not null && (_traits.ActiveTraits & Traits.Blindfolded) != 0) ? true : false;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Hardcore Trials Cleared");

        // While actively moving, incorrectly guess a restraint lock while gagged (Secret)
        LatestCache.SaveData.AddConditional(AchievementModuleKind.Wardrobe,Achievements.RunningGag, () =>
        {
            unsafe
            {
                var gameControl = FFXIVClientStructs.FFXIV.Client.Game.Control.Control.Instance();
                var movementByte = Marshal.ReadByte((nint)gameControl, 30211);
                var movementDetection = AgentMap.Instance();
                var result = movementDetection->IsPlayerMoving;
                GagSpeak.StaticLog.Information("IsPlayerMoving Result: " + result +" || IsWalking Byte: "+movementByte);
                return (_gags.ActiveGagsData?.IsGagged() ?? false) && _restraints.EnabledSet is not null && result == 1 && movementByte == 0;
            }
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Funny Conditions Met");

        // Check this in the action function handler
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.AuctionedOff, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Auctioned Off", suffix: "Times");

        // Check this in the action function handler
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Wardrobe,Achievements.SoldSlave, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Sold off in Bondage ", suffix: "Times");

        // Bondodge - Within 2 seconds of having a restraint set applied to you, remove it from yourself (might want to add a duration conditional but idk?)
        LatestCache.SaveData.AddTimeLimitedConditional(AchievementModuleKind.Wardrobe,Achievements.Bondodge,
            TimeSpan.FromSeconds(2), () => _restraints.EnabledSet is not null, DurationTimeUnit.Seconds, (id, name) => WasCompleted(id, name).ConfigureAwait(false));

        #endregion WARDROBE MODULE

        // Module Finished
        #region PUPPETEER MODULE
        // (can work both ways)
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.AnObedientPet, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Recieved", suffix: "Sit Orders");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.ControlMyBody, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Granted", suffix: "Pairs Access");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.CompleteDevotion, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Granted", suffix: "Pairs Access");

        LatestCache.SaveData.AddTimedProgress(AchievementModuleKind.Puppeteer,Achievements.MasterOfPuppets, 10, TimeSpan.FromHours(1), (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Within the last Hour");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.KissMyHeels, 50, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Ordered", suffix: "Grovels");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.Ashamed, 5, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Ordered to sulk", suffix: "Times");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.HouseServant, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Ordered to sweep", suffix: "Times");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.AMaestroOfMyProperty, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Ordered", suffix: "Dances");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.OrchestratorsApprentice, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.NoStringsAttached, 25, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.PuppetMaster, 50, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.MasterOfManipulation, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.TheGrandConductor, 250, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.MaestroOfStrings, 500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.OfGrandiousSymphony, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.SovereignMaestro, 2500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.OrchestratorOfMinds, 5000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.WillingPuppet, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.AtYourCommand, 25, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.YourMarionette, 50, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.TheInstrument, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.AMannequinsMadness, 250, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.DevotedDoll, 500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.EnthralledDoll, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.ObedientDoll, 1750, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.ServiceDoll, 2500, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.MastersPlaything, 5000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.MistressesPlaything, 5000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Puppeteer,Achievements.ThePerfectDoll, 10000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        #endregion PUPPETEER MODULE

        // Module Finished
        #region TOYBOX MODULE
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.MyPleasantriesForAll, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Published", suffix: "Patterns");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.DeviousComposer, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Published", suffix: "Patterns");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.TasteOfTemptation, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Downloaded", suffix: "Patterns");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.SeekerOfSensations, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Downloaded", suffix: "Patterns");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.CravingPleasure, 30, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Downloaded", suffix: "Patterns");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.GoodVibes, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.DelightfulPleasures, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.PatternLover, 25, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.SensualConnoisseur, 50, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.PassionateAdmirer, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");

        LatestCache.SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.ALittleTease, TimeSpan.FromSeconds(20), DurationTimeUnit.Seconds, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Seconds", "Vibrated for");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.ShortButSweet, TimeSpan.FromMinutes(1), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.TemptingRythms, TimeSpan.FromMinutes(2), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.MyBuildingDesire, TimeSpan.FromMinutes(5), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.WithWavesOfSensation, TimeSpan.FromMinutes(10), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.WithHeightenedSensations, TimeSpan.FromMinutes(15), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.MusicalMoaner, TimeSpan.FromMinutes(20), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.StimulatingExperiences, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.EnduranceKing, TimeSpan.FromMinutes(59), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Toybox,Achievements.EnduranceQueen, TimeSpan.FromMinutes(59), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Toybox,Achievements.CollectorOfSinfulTreasures, () =>
        { return (_playerData.GlobalPerms?.HasValidShareCode() ?? false) || _sexToys.DeviceHandler.AnyDeviceConnected; }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Devices Connected");

        LatestCache.SaveData.AddRequiredTimeConditional(AchievementModuleKind.Toybox,Achievements.MotivationForRestoration, TimeSpan.FromMinutes(30),
            () => _patterns.ActivePattern is not null, DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), suffix: " Vibrated in Diadem");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Toybox, Achievements.VulnerableVibrations, () => _patterns.ActivePattern is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Staggers Performed");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Toybox,Achievements.KinkyGambler,
            () => _triggers.Storage.Social.Count() > 0, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "DeathRolls Gambled");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.SubtleReminders, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Triggers Fired");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.LostInTheMoment, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Triggers Fired");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.TriggerHappy, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Triggers Fired");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Toybox,Achievements.HornyMornings, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Alarms Went Off");
        #endregion TOYBOX MODULE

        #region HARDCORE MODULE
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.AllTheCollarsOfTheRainbow, 20, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Forced", suffix: "Pairs To Follow You");

        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Hardcore,Achievements.UCanTieThis, 1,
            () => _playerData.GlobalPerms?.IsFollowing() ?? false, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Completed", suffix: "Duties in ForcedFollow.");

        // Forced follow achievements
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Hardcore,Achievements.ForcedFollow, TimeSpan.FromMinutes(1), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Leashed a Kinkster for");
        LatestCache.SaveData.AddDuration(AchievementModuleKind.Hardcore,Achievements.ForcedWalkies, TimeSpan.FromMinutes(5), DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Minutes", "Leashed a Kinkster for");

        // Time for Walkies achievements
        LatestCache.SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.TimeForWalkies, TimeSpan.FromMinutes(1), () => _playerData.GlobalPerms?.IsFollowing() ?? false, 
            DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Leashed", "Spent");
        LatestCache.SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.GettingStepsIn, TimeSpan.FromMinutes(5), () => _playerData.GlobalPerms?.IsFollowing() ?? false, 
            DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Leashed", "Spent");
        LatestCache.SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.WalkiesLover, TimeSpan.FromMinutes(10), () => _playerData.GlobalPerms?.IsFollowing() ?? false, 
            DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Leashed", "Spent");

        //Part of the Furniture - Be forced to sit for 1 hour or more
        LatestCache.SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.LivingFurniture, TimeSpan.FromHours(1), () => _playerData.GlobalPerms?.IsSitting() ?? false, DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), suffix: "Forced to Sit");

        LatestCache.SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.WalkOfShame, TimeSpan.FromMinutes(5),
            () =>
            {
                if (_restraints.EnabledSet is not null && (_traits.ActiveTraits & Traits.Blindfolded) != 0 && (_playerData.GlobalPerms?.IsFollowing() ?? false))
                    if (_clientMonitor.InMainCity)
                        return true;
                return false;
            }, DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Walked for", suffix: "In a Major City");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Hardcore,Achievements.BlindLeadingTheBlind,
            () =>
            {
                // This is temporarily impossible until i can make fetching active traits from pairs less cancer to handle.
/*                if ((_traits.ActiveTraits & Traits.Blindfolded) != 0)
                    if (_pairs.DirectPairs.Any(x => x.PairGlobals.IsFollowing() && x.LastLightStorage.IsBlindfolded()))
                        return true;*/
                return false;
            }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Blind Pairs Led");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Hardcore,Achievements.WhatAView, () => (_traits.ActiveTraits & Traits.Blindfolded) != 0, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Blind Lookouts Performed");

        LatestCache.SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.WhoNeedsToSee, TimeSpan.FromHours(3), () => (_traits.ActiveTraits & Traits.Blindfolded) != 0, 
            DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Blindfolded for");

        LatestCache.SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.OfDomesticDiscipline, TimeSpan.FromMinutes(30), () => (_playerData.GlobalPerms?.IsStaying() ?? false), 
            DurationTimeUnit.Minutes, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Locked away for");
        LatestCache.SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.HomeboundSubmission, TimeSpan.FromHours(1), () => (_playerData.GlobalPerms?.IsStaying() ?? false), 
            DurationTimeUnit.Hours, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Locked away for");
        LatestCache.SaveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore,Achievements.PerfectHousePet, TimeSpan.FromDays(1), () => (_playerData.GlobalPerms?.IsStaying() ?? false), 
            DurationTimeUnit.Days, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Locked away for");

        // Shock-related achievements - Give out shocks
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.IndulgingSparks, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Sent");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ShockingTemptations, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Sent");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.TheCrazeOfShockies, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Sent");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.WickedThunder, 10000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Sent");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ElectropeHasNoLimits, 25000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Sent");

        // Shock-related achievements - Get shocked
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ElectrifyingPleasure, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ShockingExperience, 100, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.WiredForObedience, 1000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ShockAddiction, 10000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.SlaveToTheShock, 25000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Hardcore,Achievements.ShockSlut, 50000, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Shocks Received");
        #endregion HARDCORE MODULE

        #region REMOTES MODULE
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Remotes, Achievements.JustVibing, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Remotes Opened");

        // TODO: Make this turning down someone else's once its implemented.
        // (on second thought this could introduce lots of issues so maybe not? Look into later idk, for now its dormant.)
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Remotes, Achievements.DontKillMyVibe, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Vibes Killed");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Remotes, Achievements.VibingWithFriends, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Rooms Joined");
        #endregion REMOTES MODULE

        #region GENERIC MODULE
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Generic, Achievements.TutorialComplete, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Tutorial Completed");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Generic, Achievements.KinkyNovice, () => _pairs.DirectPairs.Count > 0, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Pair Added");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Generic, Achievements.TheCollector, 20, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Pairs Added");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Generic, Achievements.BoundaryRespecter, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Presets Applied");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Generic, Achievements.HelloKinkyWorld, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Global Messages Sent");

        LatestCache.SaveData.AddProgress(AchievementModuleKind.Generic, Achievements.KnowsMyLimits, 1, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Safewords Used");

        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.WarriorOfLewd, 1,
            () => (_gags.ActiveGagsData?.IsGagged() ?? false) && _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), suffix: "Cutscenes Watched Bound & Gagged");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Generic, Achievements.EscapingIsNotEasy, () => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Escape Attempts Made");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Generic, Achievements.ICantBelieveYouveDoneThis, () => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Slaps Received");

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Generic, Achievements.WithAKissGoodbye, () =>
        {
            var targetIsImmobile = false;
            if (_pairs.GetVisiblePairGameObjects().Any(x => x.GameObjectId == _clientMonitor.TargetObjectId))
            {
                Logger.LogTrace("Target is visible in the pair manager, checking if they are gagged.", LoggerType.Achievements);
                var targetPair = _pairs.DirectPairs.FirstOrDefault(x => x.VisiblePairGameObject?.GameObjectId == _clientMonitor.TargetObjectId);
                if (targetPair is not null)
                {
                    Logger.LogTrace("Target is in the direct pairs, checking if they are gagged.", LoggerType.Achievements);
                    // store if they are stuck emoting.
                    targetIsImmobile = !targetPair.PairGlobals.ForcedEmoteState.IsNullOrWhitespace();
                    // TODO:
                    // we can add restraint trait alternatives later, but wait until later when we restructure how we manage pair information.
                }
            }
            return targetIsImmobile;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Dotes to Helpless Kinksters", "Gave");

        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.ProlificPetter, 10, () =>
        {
            var targetIsImmobile = false;
            if (_pairs.GetVisiblePairGameObjects().Any(x => x.GameObjectId == _clientMonitor.TargetObjectId))
            {
                var targetPair = _pairs.DirectPairs.FirstOrDefault(x => x.VisiblePairGameObject?.GameObjectId == _clientMonitor.TargetObjectId);
                if (targetPair is not null)
                {
                    // store if they are stuck emoting.
                    targetIsImmobile = !targetPair.PairGlobals.ForcedEmoteState.IsNullOrWhitespace();
                    // TODO:
                    // we can add restraint trait alternatives later, but wait until later when we restructure how we manage pair information.
                }
            }
            return targetIsImmobile;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Helpless Kinksters", "Pet", false);

        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.EscapedPatient, 10, () => _clientMonitor.InPvP && (_restraints.EnabledSet is not null || _sexToys.ConnectedToyActive), 
            (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.BoundToKill, 25, () => _clientMonitor.InPvP && (_restraints.EnabledSet is not null || _sexToys.ConnectedToyActive), 
            (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.TheShackledSlayer, 50, () => _clientMonitor.InPvP && (_restraints.EnabledSet is not null || _sexToys.ConnectedToyActive), 
            (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.DangerousConvict, 100, () => _clientMonitor.InPvP && (_restraints.EnabledSet is not null || _sexToys.ConnectedToyActive), 
            (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.OfUnyieldingForce, 200, () => _clientMonitor.InPvP && (_restraints.EnabledSet is not null || _sexToys.ConnectedToyActive), 
            (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.StimulationOverdrive, 300, () => _clientMonitor.InPvP && (_restraints.EnabledSet is not null || _sexToys.ConnectedToyActive), 
            (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.BoundYetUnbroken, 400, () => _clientMonitor.InPvP && (_restraints.EnabledSet is not null || _sexToys.ConnectedToyActive), 
            (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.ChainsCantHoldMe, 500, () => _clientMonitor.InPvP && (_restraints.EnabledSet is not null || _sexToys.ConnectedToyActive), 
            (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        #endregion GENERIC MODULE

        #region SECRETS MODULE
        LatestCache.SaveData.AddProgress(AchievementModuleKind.Secrets, Achievements.HiddenInPlainSight, 7, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Found", suffix: "Easter Eggs", isSecret: true);

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.Experimentalist, () =>
        {
            return _gags.ActiveGagsData is { } gags && gags.IsGagged() && _restraints.EnabledSet is not null && _patterns.ActivePattern is not null && _triggers.EnabledTriggers.Count() > 0 && _alarms.ActiveAlarms.Count() > 0 && _sexToys.ConnectedToyActive;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Met", suffix: "Conditions", isSecret: true);

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.HelplessDamsel, () =>
        {
            return _gags.ActiveGagsData is { } gags && gags.IsGagged() && _restraints.EnabledSet is not null && _sexToys.ConnectedToyActive && _pairs.DirectPairs.Any(x => x.OwnPerms.InHardcore)
            && _playerData.GlobalPerms is { } globals && (!globals.ForcedFollow.IsNullOrWhitespace() || !globals.ForcedEmoteState.IsNullOrWhitespace());
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Met", suffix: "Hardcore Conditions", isSecret: true);

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.GaggedPleasure, () => _sexToys.ConnectedToyActive && _gags.ActiveGagsData is { } gags && gags.IsGagged(), (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Pleasure Requirements Met", isSecret: true);
        LatestCache.SaveData.AddThreshold(AchievementModuleKind.Secrets, Achievements.BondageClub, 8, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Club Members Gathered", isSecret: true);
        LatestCache.SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.BadEndHostage, () => _restraints.EnabledSet is not null && _clientMonitor.IsDead, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Encountered", suffix: "Bad Ends", isSecret: true);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.TourDeBound, 11, () => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Taken", suffix: "Tours in Bondage", isSecret: true);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.MuffledProtagonist, 1, () => _gags.ActiveGagsData is { } gags && gags.IsGagged() && _playerData.GlobalPerms is { } globals && globals.ChatGarblerActive, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "MissTypes Made", isSecret: true);
        // The above is currently non functional as i dont have the data to know which chat message type contains these request tasks.

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.BoundgeeJumping, () => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), prefix: "Attempted", suffix: "Dangerous Acts", isSecret: true);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.KinkyTeacher, 10, () => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Thanks Received", reqBeginAndFinish: false, isSecret: true);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.KinkyProfessor, 50, () => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Thanks Received", reqBeginAndFinish: false, isSecret: true);
        LatestCache.SaveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.KinkyMentor, 100, () => _restraints.EnabledSet is not null, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Thanks Received", reqBeginAndFinish: false, isSecret: true);
        LatestCache.SaveData.AddThreshold(AchievementModuleKind.Secrets, Achievements.ExtremeBondageEnjoyer, 10, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Restriction Conditions Satisfied", isSecret: true); 
        LatestCache.SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.WildRide, () =>
        {
            var raceEndVisible = false;
            unsafe
            {
                var raceEnded = (AtkUnitBase*)AtkFuckery.GetAddonByName("RaceChocoboResult");
                if (raceEnded != null)
                    raceEndVisible = raceEnded->RootNode->IsVisible();
            };
            return _clientMonitor.IsChocoboRacing && raceEndVisible && _restraints.EnabledSet is not null;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Races Won In Unusual Conditions", isSecret: true);

        LatestCache.SaveData.AddConditional(AchievementModuleKind.Secrets, Achievements.SlavePresentation, () =>
        {
            return _gags.ActiveGagsData is { } gags && gags.IsGagged() && _restraints.EnabledSet is not null;
        }, (id, name) => WasCompleted(id, name).ConfigureAwait(false), "Presentations Given on Stage", isSecret: true);
        #endregion SECRETS MODULE

    }
}
