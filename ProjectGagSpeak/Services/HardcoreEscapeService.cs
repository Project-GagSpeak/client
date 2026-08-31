using GagSpeak.PlayerClient;
using GagSpeak.State.Caches;
using GagspeakAPI.Attributes;

namespace GagSpeak.Services;

/// <summary>
///   Handles the logic for escaping your own equippables.
/// </summary>
public class HardcoreEscapeService : IDisposable
{
    private readonly ILogger<HardcoreEscapeService> _logger;
    private readonly MainConfig _config;
    private readonly TraitsCache _traits;
    private readonly Random _rand = new();

    private DateTime _nextAllowedAttempt = DateTime.Now;

    public bool HardcoreEscapeEnabled => _config.Data.HardcoreEscape;
    public bool CanDisable => _traits.FinalTraits == Traits.None;

    public HardcoreEscapeService(ILogger<HardcoreEscapeService> logger, MainConfig config, TraitsCache traits)
    {
        _logger = logger;
        _config = config;
        _traits = traits;

        UpdateNextAllowedAttempt(0);
    }

    public void Dispose() { }

    public bool AttemptSelfRemove()
    {
        // Hardcore escape not enabled, always allow
        if (!_config.Data.HardcoreEscape) return true;

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

        // If there is no challenge present, allow it.
        if (difficultyOneIn == 1) return true;

        // If cooldown is active, disallow it.
        if (DateTime.Now < _nextAllowedAttempt)
            Svc.Toasts.ShowError(
                $"You are too exhausted to do that! You may try again in {CooldownString()}.");

        var roll = _rand.NextInt64(difficultyOneIn);
        UpdateNextAllowedAttempt(roll);
        _logger.LogDebug(
            $"Attempted remove hardcore, difficulty one in {difficultyOneIn}, rolled {roll}. Next attempt allowed at {_nextAllowedAttempt}",
            LoggerType.HardcoreActions);

        if (roll > 0)
            Svc.Toasts.ShowError($"Failed to remove! You may try again in {CooldownString()}.");
        return roll == 0;
    }

    private void UpdateNextAllowedAttempt(long roll)
    {
        var baseCooldown = TimeSpan.FromMinutes(2);
        if (roll == 0)
        {
            _nextAllowedAttempt = DateTime.Now + (baseCooldown / 2);
            return;
        }

        // Add a small penalty for each active trait, simulating higher exhaustion from higher restriction
        var exhaustionCooldownMultiplier = 0;
        for (int i = 1; i <= (1 << 6); i++)
        {
            if (_traits.FinalTraits.HasFlag((Traits)i))
                exhaustionCooldownMultiplier++;
        }

        var exhaustionCooldown = TimeSpan.FromSeconds(10) * exhaustionCooldownMultiplier;
        var rollCooldown = TimeSpan.FromSeconds(2) * roll; // Max 86 seconds penalty for rolling poorly
        var cooldown = baseCooldown + rollCooldown + exhaustionCooldown;
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
