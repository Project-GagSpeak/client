using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using System.Collections.Immutable;
using GagSpeak.PlayerClient;

namespace GagSpeak.State.Handlers;

// what is in here is service behavior...
public class TraitsHandler : DisposableMediatorSubscriberBase
{
    private readonly TraitsCache _cache;
    private readonly PlayerData _player;

    public TraitsHandler(ILogger<TraitsHandler> logger, GagspeakMediator mediator,
        TraitsCache cache, PlayerData monitor) : base(logger, mediator)
    {
        _cache = cache;
        _player = monitor;

        Mediator.Subscribe<JobChangeMessage>(this, msg => OnJobChange(msg.jobId));
    }

    // Currently there is no way to know what traits get added on each calculation, so it is a bit difficult
    // to know when certain traits should be applied.
    // we must know when they change so we know which banned actions to make, and also when to lock the hotbar state.
    // (because it currently doesnt do that)
    public Traits FinalTraits => _cache.FinalTraits;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // just incase.
        RestoreSavedSlots();
    }

    // Handle the reapplication of various trait attributes here.

    /// <summary> Updates the slots on the visible hotbars based on <see cref="_finalTraits"/></summary>
    private unsafe void UpdateSlots()
    {
        var hotbarModule = Framework.Instance()->GetUIModule()->GetRaptureHotbarModule();
        // the length of our hotbar count
        var hotbarSpan = hotbarModule->StandardHotbars;
        // Trait -> Override ActionId map, ordered by priority.
        var overrides = new (Traits Trait, uint ActionId)[]
        {
            (Traits.Gagged,      2886),
            (Traits.Blindfolded, 99),
            (Traits.Weighty,     151),
            (Traits.Immobile,    2883),
            (Traits.BoundLegs,   55),
            (Traits.BoundArms,   68),
        };

        // Check all active hotbar spans.
        for (var i = 0; i < hotbarSpan.Length; i++)
        {
            // Get all slots for the row. (their pointers)
            var hotbarRow = hotbarSpan.GetPointer(i);
            if (hotbarSpan == Span<RaptureHotbarModule.Hotbar>.Empty)
                continue;

            // get the slots data...
            for (var j = 0; j < 16; j++)
            {
                // From the pointer, get the individual slot.
                var slot = hotbarRow->Slots.GetPointer(j);
                if (slot is null)
                    break;

                // If not a valid action type, ignore it.
                if (slot->CommandType != RaptureHotbarModule.HotbarSlotType.Action &&
                    slot->CommandType != RaptureHotbarModule.HotbarSlotType.GeneralAction)
                    continue;

                // if unable to find the properties for this item, ignore it.
                if (!_cache.BannedActions.TryGetValue(slot->CommandId, out var props))
                    continue;

                // Apply the first matching trait override
                foreach (var (trait, actionId) in overrides)
                {
                    if (props.HasAny(trait))
                    {
                        slot->Set(hotbarModule->UIModule, RaptureHotbarModule.HotbarSlotType.Action, actionId);
                        break;
                    }
                }
            }
        }
    }

    public unsafe void RestoreSavedSlots()
    {
        var hotbarModule = Framework.Instance()->GetUIModule()->GetRaptureHotbarModule();
        if (hotbarModule is null)
            return;

        Logger.LogDebug("Restoring saved slots", LoggerType.HardcoreActions);
        var baseSpan = hotbarModule->StandardHotbars; // the length of our hotbar count
        for (var i = 0; i < baseSpan.Length; i++)
        {
            var hotbarRow = baseSpan.GetPointer(i);
            // if the hotbar is not null, we can get the slots data
            if (hotbarRow is not null)
                hotbarModule->LoadSavedHotbar(_player.ClientPlayer.ClassJobId(), (uint)i);
        }
    }

    private void OnJobChange(uint jobId)
    {
        Logger.LogInformation($"Job Changed to [{((JobType)jobId)}], recalculating Banned Actions.");
        _cache.UpdateBannedActions((JobType)jobId switch
        {
            JobType.ADV => RestrictedActions.Adventurer,
            JobType.GLA => RestrictedActions.Gladiator,
            JobType.PGL => RestrictedActions.Pugilist,
            JobType.MRD => RestrictedActions.Marauder,
            JobType.LNC => RestrictedActions.Lancer,
            JobType.ARC => RestrictedActions.Archer,
            JobType.CNJ => RestrictedActions.Conjurer,
            JobType.THM => RestrictedActions.Thaumaturge,
            JobType.CRP => RestrictedActions.Carpenter,
            JobType.BSM => RestrictedActions.Blacksmith,
            JobType.ARM => RestrictedActions.Armorer,
            JobType.GSM => RestrictedActions.Goldsmith,
            JobType.LTW => RestrictedActions.Leatherworker,
            JobType.WVR => RestrictedActions.Weaver,
            JobType.ALC => RestrictedActions.Alchemist,
            JobType.CUL => RestrictedActions.Culinarian,
            JobType.MIN => RestrictedActions.Miner,
            JobType.BTN => RestrictedActions.Botanist,
            JobType.FSH => RestrictedActions.Fisher,
            JobType.PLD => RestrictedActions.Paladin,
            JobType.MNK => RestrictedActions.Monk,
            JobType.WAR => RestrictedActions.Warrior,
            JobType.DRG => RestrictedActions.Dragoon,
            JobType.BRD => RestrictedActions.Bard,
            JobType.WHM => RestrictedActions.WhiteMage,
            JobType.BLM => RestrictedActions.BlackMage,
            JobType.ACN => RestrictedActions.Arcanist,
            JobType.SMN => RestrictedActions.Summoner,
            JobType.SCH => RestrictedActions.Scholar,
            JobType.ROG => RestrictedActions.Rogue,
            JobType.NIN => RestrictedActions.Ninja,
            JobType.MCH => RestrictedActions.Machinist,
            JobType.DRK => RestrictedActions.DarkKnight,
            JobType.AST => RestrictedActions.Astrologian,
            JobType.SAM => RestrictedActions.Samurai,
            JobType.RDM => RestrictedActions.RedMage,
            JobType.BLU => RestrictedActions.BlueMage,
            JobType.GNB => RestrictedActions.Gunbreaker,
            JobType.DNC => RestrictedActions.Dancer,
            JobType.RPR => RestrictedActions.Reaper,
            JobType.SGE => RestrictedActions.Sage,
            JobType.VPR => RestrictedActions.Viper,
            JobType.PCT => RestrictedActions.Pictomancer,
            _ => ImmutableDictionary<uint, Traits>.Empty,
        });
        // Update the slots based on the new job.
        UpdateSlots();
    }
}
