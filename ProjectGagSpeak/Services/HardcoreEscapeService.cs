using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;

namespace GagSpeak.Services;

/// <summary>
///   Handles the logic for escaping your own equippables.
/// </summary>
public class HardcoreEscapeService : DisposableMediatorSubscriberBase
{
    private readonly ILogger<HardcoreEscapeService> _logger;
    private readonly MainConfig _config;
    private readonly TraitsCache _traits;
    private readonly Random _rand = new();

    private DateTime
        _nextAllowedAttempt =
            DateTime.Now +
            TimeSpan.FromMinutes(5); // Initial cooldown to avoid brats reloading the plugin to reset cooldown
    private int _pityCounter = 0;
    private const int MAX_PITY = 10;

    public bool HardcoreEscapeEnabled => _config.Data.HardcoreEscape;
    public bool CanDisable => _traits.FinalTraits == Traits.None;

    public HardcoreEscapeService(
        ILogger<HardcoreEscapeService> logger, MainConfig config, TraitsCache traits, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        _logger = logger;
        _config = config;
        _traits = traits;

        Mediator.Subscribe<GagStateChanged>(this, e =>
        {
            if (e.Target == MainHub.UID) _pityCounter = 0;
        });
        Mediator.Subscribe<RestrictionStateChanged>(this, e =>
        {
            if (e.Target == MainHub.UID) _pityCounter = 0;
        });
        Mediator.Subscribe<RestraintStateChanged>(this, e =>
        {
            if (e.Target == MainHub.UID) _pityCounter = 0;
        });
        Mediator.Subscribe<RestraintLayersChanged>(this, e =>
        {
            if (e.Target == MainHub.UID) _pityCounter = 0;
        });
    }

    public bool AttemptSelfRemove()
    {
        // Hardcore escape not enabled, always allow
        if (!HardcoreEscapeEnabled)
            return true;

        // One in X chance to succeed
        int difficultyOneIn = 1;
        if (_traits.FinalTraits.HasFlag(Traits.Blindfolded))
            difficultyOneIn += 3;
        if (_traits.FinalTraits.HasAny(Traits.BoundArms | Traits.Immobile))
            difficultyOneIn += 19;

        // Some traits compound difficulty, but only if more restrictive traits already exist.
        if (difficultyOneIn >= 10)
        {
            if (_traits.FinalTraits.HasFlag(Traits.BoundLegs))
                difficultyOneIn += 8;
            if (_traits.FinalTraits.HasFlag(Traits.Gagged))
                difficultyOneIn += 7;
            if (_traits.FinalTraits.HasFlag(Traits.Weighty))
                difficultyOneIn += 5;
        }

        var prePity = difficultyOneIn;
        var pityCount = Math.Min(_pityCounter, MAX_PITY);
        // 0.9^0 = 1 => no change
        // 0.9^10 ~= 0.34 => ~66% easier to escape at the end
        difficultyOneIn = (int)Math.Ceiling(difficultyOneIn * Math.Pow(0.89, pityCount));

        // If there is no challenge present, allow it.
        if (difficultyOneIn == 1)
        {
            _pityCounter = 0;
            return true;
        }

        // If cooldown is active, disallow it.
        if (DateTime.Now < _nextAllowedAttempt)
        {
            Svc.Toasts.ShowError(
                $"You are too exhausted to do that! You may try again in {CooldownString()}.");
            return false;
        }

        var roll = _rand.NextInt64(difficultyOneIn);
        var criticalFail = roll == difficultyOneIn - 1;
        UpdateNextAllowedAttempt(roll, criticalFail);
        _logger.LogDebug(
            $"Attempted remove hardcore, difficulty one in {prePity} with pity {_pityCounter}=>{difficultyOneIn}, rolled {roll}. Next attempt allowed at {_nextAllowedAttempt}",
            LoggerType.HardcoreActions);

        if (roll > 0)
        {
            if (criticalFail)
            {
                Svc.Toasts.ShowError(
                    $"You make a mistake and the restraint tightens! You may try again in {CooldownString()}");
                _pityCounter -= 2;
                if (_pityCounter < 0) _pityCounter = 0;
            }
            else if (pityCount < MAX_PITY)
            {
                Svc.Toasts.ShowError(
                    $"Failed to remove, but you feel a sense of progress! You may try again in {CooldownString()}.");
                _pityCounter++;
            }
            else
            {
                Svc.Toasts.ShowError($"Failed to remove! You may try again in {CooldownString()}");
            }

            return false;
        }

        _pityCounter = 0;
        return true;
    }

    private void UpdateNextAllowedAttempt(long roll, bool criticalFail = false)
    {
        var baseCooldown = TimeSpan.FromMinutes(1);
        if (roll == 0)
        {
            _nextAllowedAttempt = DateTime.Now + baseCooldown;
            return;
        }

        // Add a small penalty for each active trait, simulating higher exhaustion from higher restriction
        var exhaustionCooldownMultiplier = 0;
        for (int i = 1; i <= 6; i++)
        {
            var trait = 1 << i;
            if (_traits.FinalTraits.HasFlag((Traits)trait))
                exhaustionCooldownMultiplier++;
        }

        var exhaustionCooldown = TimeSpan.FromSeconds(20) * exhaustionCooldownMultiplier;
        var rollCooldown = TimeSpan.FromSeconds(10) * roll; // Add penalty for rolling poorly, worst possible roll is 43
        var cooldown = baseCooldown + rollCooldown + exhaustionCooldown;
        if (criticalFail)
        {
            cooldown += rollCooldown;
        }
        _nextAllowedAttempt = DateTime.Now + cooldown;
    }

    private string CooldownString()
    {
        var duration = _nextAllowedAttempt - DateTime.Now;
        var minutes = Math.Floor(duration.TotalMinutes);
        var seconds = duration.Seconds;
        return minutes > 0 ? $"{minutes} minutes" : $"{seconds} seconds";
    }
}
