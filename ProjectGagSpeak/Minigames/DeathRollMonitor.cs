using CkCommons;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using GagSpeak.Services.Mediator;
using System.Text.RegularExpressions;

namespace GagSpeak.Minigames.Watchers;

/// <summary>
///     Monitors the chatlogs for deathroll messages sent to the chat.
/// </summary>
public sealed class DeathRollMonitor : DisposableMediatorSubscriberBase
{
    private Dictionary<string, DeathRollSession> _monitored = [];

    public DeathRollMonitor(ILogger<DeathRollMonitor> logger, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        Mediator.Subscribe<DeathrollMessage>(this, _ => OnDeathrollMessage(_.Type, _.SenderNameWorld, _.Msg));
    }

    // add a helper function to retrieve the roll cap of the last active session our player is in.
    public int? GetLastRollCap()
    {
        var player = PlayerData.NameWithWorld;
        // Sort all sessions in order by their LastRollTime, and return the first one where either the opponent is nullorEmpty, or matches the clientplayernameandworld.
        var matchedSession = _monitored.Values
        .OrderByDescending(s => s.LastRollTime)
            .FirstOrDefault(s => s.Opponent.IsNullOrEmpty() || ((s.Opponent == player || s.Initializer == player) && s.LastRoller != player));

        return matchedSession?.CurrentRollCap ?? null;
    }

    private void OnDeathrollMessage(XivChatType type, string nameWithWorld, SeString message)
    {
        if (!PlayerData.Available || !message.Payloads.Exists(p => p.Type == PayloadType.Icon))
            return;

        var (rollValue, rollCap) = ParseMessage(message.TextValue);

        // if the roll value and cap are 0, its an invalid, so return.
        if (rollValue is -1 && rollCap is -1)
        {
            Logger.LogDebug("Ignoring message due to invalid roll values.", LoggerType.Triggers);
            return;
        }

        Logger.LogDebug($"{nameWithWorld} rolled {rollValue} with cap {rollCap}", LoggerType.Triggers);

        if (rollValue is -1)
        {
            Logger.LogDebug($"A Player has started a different deathroll session!", LoggerType.Triggers);
            StartNewSession(nameWithWorld, rollCap);
        }
        else
        {
            Logger.LogDebug($"{nameWithWorld} is attempting to continue / join a session", LoggerType.Triggers);
            ContinueSession(nameWithWorld, rollValue, rollCap);
        }
    }

    private void StartNewSession(string initializer, int initialRollCap)
    {
        // Remove any existing sessions involving this player
        RemovePlayerSessions(initializer);

        // Create and add new session
        var session = new DeathRollSession(initializer, initialRollCap, OnSessionComplete);
        _monitored[initializer] = session;
        Logger.LogDebug($"New session started by {initializer} with cap {initialRollCap}");
    }

    private void ContinueSession(string playerName, int rollValue, int rollCap)
    {
        // Find a matching active session where the cap matches
        var session = _monitored.Values
            .FirstOrDefault(s => s.CurrentRollCap == rollCap && !s.IsComplete && s.LastRoller != playerName);

        if (session is null) {
            Logger.LogDebug("No active session found to match roll.", LoggerType.Triggers);
            return;
        }

        // do not join a session we are not a part of.
        if (!session.Opponent.IsNullOrEmpty() && (session.Opponent != playerName && session.Initializer != playerName)) {
            Logger.LogTrace($"{playerName} is not part of the session, ignoring!", LoggerType.Triggers);
            return;
        }

        // if the opponent is not yet set, we are joining the session with a reply,
        // and should clear all other instances with our name.
        if (session.Opponent.IsNullOrEmpty())
        {
            Logger.LogDebug($"{playerName} joined session with {session.Initializer}.", LoggerType.Triggers);
            RemovePlayerSessions(playerName);
        }

        if (session.TryProcessRoll(playerName, rollValue))
        {
            Logger.LogDebug($"{playerName} rolled {rollValue} in session.", LoggerType.Triggers);
        }
        else
        {
            Logger.LogDebug("Invalid roll attempt by " + playerName);
        }
    }

    /// <summary>
    /// Removes all sessions that the playerName is either an opponent or initializer of.
    /// </summary>
    private void RemovePlayerSessions(string playerName)
    {
        foreach (var session in _monitored.Values.Where(k => k.Initializer == playerName || k.Opponent == playerName).ToList())
        {
            Logger.LogDebug($"Removing session involving {playerName}, (Initializer: {session.Initializer} and Opponent {session.Opponent}" +
                " due to them joining / creating another!", LoggerType.Triggers);
            _monitored.Remove(session.Initializer);
        }
    }

    /// <summary>
    /// Triggered by a DeathRoll sessions action upon completion.
    /// </summary>
    private async void OnSessionComplete(DeathRollSession session)
    {
        Svc.Chat.Print(new SeStringBuilder().AddText("[Deathroll]").AddUiForeground("Match ended. Loser: " + session.LastRoller, 31).AddUiForegroundOff().BuiltString);
        // if we were the loser, then fire the deathroll trigger.
        var playerName = PlayerData.NameWithWorld;
        var loser = session.LastRoller;
        var winner = session.Initializer == loser ? session.Opponent : session.Initializer;
        // It was a match where we were involved.
        if (playerName == loser || playerName == winner)
            Mediator.Publish(new DeathrollResult(winner, loser));
        // Remove the match
        _monitored.Remove(session.Initializer);
    }

    /// <summary>
    /// Parses the message string for the rolled value and cap value in a DeathRoll.
    /// Roll Value is the lower of two numbers found; Roll Cap is the higher.
    /// If only one number is found, it is assumed to be the Roll Cap.
    /// </summary>
    /// <returns>A tuple containing the Roll Value (-1 if not found) and Roll Cap (-1 if not found).</returns>
    private (int rollValue, int rollCap) ParseMessage(string message)
    {
        var regex = new Regex(@"\b(\d+)\b");
        var matches = regex.Matches(message);

        if (matches.Count == 0)
            return (-1, -1);

        var firstNumber = int.Parse(matches[0].Groups[1].Value);
        var secondNumber = matches.Count > 1 ? int.Parse(matches[1].Groups[1].Value) : -1;

        // If only one number is found, treat it as the roll cap
        if (secondNumber is -1)
            return (-1, firstNumber);

        // Otherwise, return the minimum as rollValue and maximum as rollCap
        return (Math.Min(firstNumber, secondNumber), Math.Max(firstNumber, secondNumber));
    }
}
