using Dalamud.Utility;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Toybox.Data;

public class DeathRollSession
{
    private Action<DeathRollSession> _onSessionComplete;
    public string Initializer { get; init; }
    public string Opponent { get; private set; } = string.Empty;
    public string LastRoller { get; private set; }
    public int CurrentRollCap { get; private set; } = 999;
    public bool IsComplete { get; private set; } = false;
    public DateTime LastRollTime { get; private set; } = DateTime.UtcNow;

    public DeathRollSession(string initializer, int initialRollCap, Action<DeathRollSession> onFinish)
    {
        Initializer = initializer;
        CurrentRollCap = initialRollCap;
        _onSessionComplete = onFinish;
    }

    public bool TryProcessRoll(string playerName, int rollValue)
    {
        if (IsComplete || DateTime.UtcNow - LastRollTime > TimeSpan.FromMinutes(5))
        {
            StaticLogger.Logger.LogDebug("[DeathRoll] Session is complete or expired.");
            return false;
        }

        // Validate roll and assign opponent if necessary
        if (Opponent.IsNullOrEmpty() && playerName != Initializer)
        {
            StaticLogger.Logger.LogDebug("[DeathRoll] Opponent was empty, assign it.");
            Opponent = playerName; // Opponent was empty, assign it
        }
        else if (Opponent != playerName && Initializer != playerName)
        {
            StaticLogger.Logger.LogDebug("[DeathRoll] Invalid roll by a non-participant.");
            return false; // Invalid roll by a non-participant
        }

        // Enforce turn order (Return false if not the players turn)
        if (string.Equals(LastRoller, playerName, StringComparison.OrdinalIgnoreCase))
        {
            StaticLogger.Logger.LogDebug("[DeathRoll] Invalid roll by the same player twice.");
            return false;
        }

        // Check if roll matches cap and is in sequence
        if (rollValue < CurrentRollCap)
        {
            LastRollTime = DateTime.UtcNow;
            CurrentRollCap = rollValue;
            LastRoller = playerName; // Update the last roller

            if (rollValue is 1) // Game ends when roll is 1
                EndGame();

            // Valid Roll
            return true;
        }
        return false;
    }

    private void EndGame()
    {
        IsComplete = true;
        _onSessionComplete?.Invoke(this);
    }
}
