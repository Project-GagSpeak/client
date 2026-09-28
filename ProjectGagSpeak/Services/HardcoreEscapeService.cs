using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;

namespace GagSpeak.Services;

/// <summary>
///   Handles the logic for escaping your own equippables.
/// </summary>
public class HardcoreEscapeService : DisposableMediatorSubscriberBase
{
    public enum Type
    {
        Gag,
        Restraint,
        Restriction
    }

    private readonly ILogger<HardcoreEscapeService> _logger;
    private readonly MainConfig _config;
    private readonly TraitsCache _traits;
    private readonly GagRestrictionManager _gags;
    private readonly RestraintManager _restraint;
    private readonly RestrictionManager _restrictions;
    private readonly Random _rand = new();

    private readonly int[] _gagTightness = [-1, -1, -1];
    private readonly int[] _restrictionTightness = [-1, -1, -1, -1, -1];
    private int _restraintTightness = -1;

    private DateTime _nextAllowedAttempt = DateTime.Now;

    public bool HardcoreEscapeEnabled => _config.Data.HardcoreEscape;
    public bool CanDisable => _traits.FinalTraits == Traits.None;

    public HardcoreEscapeService(
        ILogger<HardcoreEscapeService> logger, MainConfig config, TraitsCache traits, GagspeakMediator mediator,
        GagRestrictionManager gags, RestraintManager restraint, RestrictionManager restrictions)
        : base(logger, mediator)
    {
        _logger = logger;
        _config = config;
        _traits = traits;
        _gags = gags;
        _restraint = restraint;
        _restrictions = restrictions;

        Mediator.Subscribe<GagStateChanged>(this, e =>
        {
            if (e.Target != MainHub.UID) return;

            if (e.State != NewState.Disabled)
            {
                // When added or changed, reset tightness
                var gag = _gags.ActiveItems[e.Layer];
                _gagTightness[e.Layer] = gag.DefaultTightness;

                _logger.LogDebug(
                    $"Hardcore Escape GagChange for layer {e.Layer}: {e.State} - Initial Tightness {gag.DefaultTightness}",
                    LogFilter.HardcoreActions);
            }
        });
        Mediator.Subscribe<RestrictionStateChanged>(this, e =>
        {
            if (e.Target != MainHub.UID) return;

            if (e.State != NewState.Disabled)
            {
                // When added or changed, reset tightness
                var restriction = _restrictions.ActiveItems[e.Layer];
                _restrictionTightness[e.Layer] = restriction.DefaultTightness;

                _logger.LogDebug(
                    $"Hardcore Escape RestrictionsChange for layer {e.Layer}: {e.State} - Initial Tightness {restriction.DefaultTightness}",
                    LogFilter.HardcoreActions);
            }
        });
        Mediator.Subscribe<RestraintStateChanged>(this, e =>
        {
            if (e.Target != MainHub.UID) return;

            if (e.State != NewState.Disabled)
            {
                // When added or changed, reset tightness
                var appliedRestraint = _restraint.AppliedRestraint;
                if (appliedRestraint is null)
                {
                    _logger.LogError(
                        "RestraintStateChanged with active restraint, but RestraintManager.AppliedRestraint was null! This should never happen!");
                    return;
                }

                _restraintTightness = appliedRestraint.DefaultTightness;

                _logger.LogDebug(
                    $"Hardcore Escape RestraintChange: {e.State} - Initial Tightness {appliedRestraint.DefaultTightness}",
                    LogFilter.HardcoreActions);
            }
        });
        Mediator.Subscribe<RestraintLayersChanged>(this, e =>
        {
            if (e.Target != MainHub.UID) return;

            var appliedRestraint = _restraint.AppliedRestraint;
            if (appliedRestraint is null)
            {
                _logger.LogError(
                    "RestraintLayersChanged, but RestraintManager.AppliedRestraint was null! This should never happen!");
                return;
            }

            _restraintTightness = appliedRestraint.DefaultTightness;

            _logger.LogDebug(
                $"Hardcore Escape RestraintLayerChange - Initial Tightness {appliedRestraint.DefaultTightness}",
                LogFilter.HardcoreActions);
        });
    }

    public bool AttemptSelfRemove(Type type, int layerIdx = 0)
    {
        // Hardcore escape not enabled, always allow
        if (!HardcoreEscapeEnabled)
            return true;

        var (item, tightness, updateTightnessAction) = GetItemDetails(type, layerIdx);

        var difficultyMultiplier = CalculateDifficultyMultiplier(item);

        if (item.DefaultTightness == 0)
        {
            Svc.Toasts.ShowError("Try as you might, you cannot remove this item in your current condition!");
            return false;
        }

        // Allow, when item poses no challenge
        if (item.DefaultTightness == 1)
        {
            return true;
        }

        // Allow, when no traits pose a challenge
        if (difficultyMultiplier == 1)
            return true;

        // If cooldown is active, disallow it.
        if (DateTime.Now < _nextAllowedAttempt)
        {
            Svc.Toasts.ShowError(
                $"You are too exhausted to do that! You may try again in {CooldownString()}.");
            return false;
        }

        // D100
        var roll = _rand.Next(100) + 1;

        // Base amount of progress on only arms bound = 25 per roll (difficulty 4), with a bit of randomness to keep it more natural
        var progress = (int)Math.Ceiling(100d / difficultyMultiplier) + _rand.Next(3);
        var oldTightness = tightness;

        // 1 in 20 low: critical fail
        var criticalFail = roll < 5;
        UpdateNextAllowedAttempt(difficultyMultiplier, criticalFail);

        // Critical fail reverses progress greatly
        // Low rolls reverse progress slightly
        // Middling rolls do not make progress
        // Progress guaranteed over long term
        if (criticalFail)
        {
            tightness += 2 * progress;
            Svc.Toasts.ShowError("You make a big mistake and the item tightens its grip on you!");
        }
        else
            switch (roll)
            {
                case <= 19:
                    tightness += (int)Math.Ceiling(progress / 2d);
                    Svc.Toasts.ShowError("You make a mistake and the item tightens its grip on you!");
                    break;
                case <= 35:
                    // No tightness change
                    Svc.Toasts.ShowError("You struggle, but make no progress.");
                    break;
                case <= 90:
                    tightness -= progress;
                    Svc.Toasts.ShowError("You struggle and feel a sense of progress!");
                    break;
                default:
                    tightness -= progress * 2;
                    Svc.Toasts.ShowError("You struggle and feel the item giving in!");
                    break;
            }

        _logger.LogDebug(
            $"Attempted remove hardcore, difficulty {difficultyMultiplier} => progress {progress}, rolled {roll}. Tightness change {oldTightness} => {tightness} / {item.DefaultTightness} Next attempt allowed at {_nextAllowedAttempt}",
            LogFilter.HardcoreActions);

        updateTightnessAction(tightness);

        // If tightness was reduced below maximum, allow unlock
        return tightness <= 0;
    }

    public string ProgressTooltip(Type type, int layerIdx = 0)
    {
        var (item, tightness, _) = GetItemDetails(type, layerIdx);
        if (!IsHardToRemove(item))
            return "";

        if (item.DefaultTightness == 0)
            return "--SEP--Tightness: Impossible";

        var cooldown = DateTime.Now < _nextAllowedAttempt ? $" - Try again in {CooldownString()}" : "";

        return $"--SEP--Tightness: {tightness} / {item.DefaultTightness}{cooldown}";
    }

    public (int Current, int Total) Progress(Type type, int layerIdx = 0)
    {
        var (item, tightness, _) = GetItemDetails(type, layerIdx);
        return (tightness, item.DefaultTightness);
    }

    public bool IsHardToRemove(IAttributeItem item)
    {
        if (!HardcoreEscapeEnabled)
            return false;

        return CalculateDifficultyMultiplier(item) > 1;
    }

    private int CalculateDifficultyMultiplier(IAttributeItem item)
    {
        // One in X chance to succeed
        var armsLimited = false;
        int difficultyMultiplier = 1;
        if (_traits.FinalTraits.HasFlag(Traits.Blindfolded))
            difficultyMultiplier += 1;
        if (_traits.FinalTraits.HasAny(Traits.BoundArms | Traits.Immobile))
        {
            armsLimited = true;
            difficultyMultiplier += 3;

            // Some traits compound difficulty, but only if more restrictive traits already exist.
            if (_traits.FinalTraits.HasFlag(Traits.BoundLegs))
                difficultyMultiplier += 2;
            if (_traits.FinalTraits.HasFlag(Traits.Gagged))
                difficultyMultiplier += 2;
            if (_traits.FinalTraits.HasFlag(Traits.Weighty))
                difficultyMultiplier += 1;
        }

        // When arms are restricted, escaping from other items is much more challenging
        if (armsLimited && !item.Traits.HasAny(Traits.BoundArms | Traits.Immobile))
        {
            difficultyMultiplier *= 3;
        }

        return difficultyMultiplier;
    }

    private (IAttributeItem, int, Action<int>) GetItemDetails(Type type, int layerIdx)
    {
        switch (type)
        {
            case Type.Gag:
                var tightness = _gagTightness[layerIdx] < 0
                                    ? _gags.ActiveItems[layerIdx].DefaultTightness
                                    : _gagTightness[layerIdx];
                return (_gags.ActiveItems[layerIdx], tightness,
                           (newTightness) =>
                           {
                               if (newTightness > _gags.ActiveItems[layerIdx].DefaultTightness)
                                   newTightness = _gags.ActiveItems[layerIdx].DefaultTightness;
                               _gagTightness[layerIdx] = newTightness;
                           });
            case Type.Restraint:
                tightness = _restraintTightness < 0
                                ? _restraint.AppliedRestraint!.DefaultTightness
                                : _restraintTightness;
                return (_restraint.AppliedRestraint!, tightness,
                           (newTightness) =>
                           {
                               if (newTightness > _restraint.AppliedRestraint!.DefaultTightness)
                                   newTightness = _restraint.AppliedRestraint!.DefaultTightness;
                               _restraintTightness = newTightness;
                           });
            case Type.Restriction:
                tightness = _restrictionTightness[layerIdx] < 0
                                ? _restrictions.ActiveItems[layerIdx].DefaultTightness
                                : _restrictionTightness[layerIdx];
                return (_restrictions.ActiveItems[layerIdx], tightness,
                           (newTightness) =>
                           {
                               if (newTightness > _restrictions.ActiveItems[layerIdx].DefaultTightness)
                                   newTightness = _restrictions.ActiveItems[layerIdx].DefaultTightness;
                               _restrictionTightness[layerIdx] = newTightness;
                           });
        }

        throw new Exception("HardcoreEscapeService.Item unhandled type! This should not happen!");
    }

    private void UpdateNextAllowedAttempt(int difficulty, bool criticalFail = false)
    {
        // Random cooldown between 2 and 5 minutes, double on a critical fail
        var cd = TimeSpan.FromSeconds((difficulty * 30) + _rand.NextInt64(90));
        if (criticalFail)
            cd *= 2;
        _nextAllowedAttempt = DateTime.Now + cd;
    }

    private string CooldownString()
    {
        var duration = _nextAllowedAttempt - DateTime.Now;
        var minutes = (int)Math.Floor(duration.TotalMinutes);
        var minLabel = minutes == 1 ? "minute" : "minutes";
        var seconds = duration.Seconds;
        var secLabel = seconds == 1 ? "second" : "seconds";
        return minutes > 0 ? $"{minutes} {minLabel}" : $"{seconds} {secLabel}";
    }
}
