using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using System.Text.RegularExpressions;
using GagSpeak.PlayerClient;

namespace GagSpeak.Services;

public sealed class DeathRollService
{
    private readonly ILogger<DeathRollService> _logger;
    private readonly KinksterRequests _globals;
    private readonly PlayerData _player;
    private readonly TriggerManager _triggers;
    private readonly TriggerActionService _triggerActions;
    private readonly IChatGui _chatGui;

    public DeathRollService(ILogger<DeathRollService> logger, KinksterRequests globals,
        PlayerData clientMonitor, TriggerManager manager, TriggerActionService actions,
        IChatGui chatGui)
    {
        _logger = logger;
        _globals = globals;
        _player = clientMonitor;
        _triggers = manager;
        _triggerActions = actions;
        _chatGui = chatGui;
    }

    private Dictionary<string, DeathRollSession> MonitoredSessions = new();

    // add a helper function to retrieve the roll cap of the last active session our player is in.
    public int? GetLastRollCap()
    {
        var player = _player.ClientPlayer.NameWithWorld();
        // Sort all sessions in order by their LastRollTime, and return the first one where either the opponent is nullorEmpty, or matches the clientplayernameandworld.
        var matchedSession = MonitoredSessions.Values
        .OrderByDescending(s => s.LastRollTime)
            .FirstOrDefault(s => s.Opponent.IsNullOrEmpty() || ((s.Opponent == player || s.Initializer == player) && s.LastRoller != player));

        return matchedSession?.CurrentRollCap ?? null;
    }

    public void ProcessMessage(XivChatType type, string nameWithWorld, SeString message)
    {
        if (_player.Address == nint.Zero || !message.Payloads.Exists(p => p.Type == PayloadType.Icon))
        {
            _logger.LogDebug("Ignoring message due to not being in a world or not being a chat message.", LoggerType.Triggers);
            return;
        }

        var (rollValue, rollCap) = ParseMessage(message.TextValue);

        // if the roll value and cap are 0, its an invalid, so return.
        if (rollValue is -1 && rollCap is -1)
        {
            _logger.LogDebug("Ignoring message due to invalid roll values.", LoggerType.Triggers);
            return;
        }

        _logger.LogDebug($"{nameWithWorld} rolled {rollValue} with cap {rollCap}", LoggerType.Triggers);

        if (rollValue is -1)
        {
            _logger.LogDebug($"A Player has started a different deathroll session!", LoggerType.Triggers);
            StartNewSession(nameWithWorld, rollCap);
        }
        else
        {
            _logger.LogDebug($"{nameWithWorld} is attempting to continue / join a session", LoggerType.Triggers);
            ContinueSession(nameWithWorld, rollValue, rollCap);
        }
    }

    private void StartNewSession(string initializer, int initialRollCap)
    {
        // Remove any existing sessions involving this player
        RemovePlayerSessions(initializer);

        // Create and add new session
        var session = new DeathRollSession(initializer, initialRollCap, OnSessionComplete);
        MonitoredSessions[initializer] = session;
        _logger.LogDebug($"New session started by {initializer} with cap {initialRollCap}");
    }

    private void ContinueSession(string playerName, int rollValue, int rollCap)
    {
        // Find a matching active session where the cap matches
        var session = MonitoredSessions.Values
            .FirstOrDefault(s => s.CurrentRollCap == rollCap && !s.IsComplete && s.LastRoller != playerName);

        if (session is null) {
            _logger.LogDebug("No active session found to match roll.", LoggerType.Triggers);
            return;
        }

        // do not join a session we are not a part of.
        if (!session.Opponent.IsNullOrEmpty() && (session.Opponent != playerName && session.Initializer != playerName)) {
            _logger.LogTrace($"{playerName} is not part of the session, ignoring!", LoggerType.Triggers);
            return;
        }

        // if the opponent is not yet set, we are joining the session with a reply,
        // and should clear all other instances with our name.
        if (session.Opponent.IsNullOrEmpty())
        {
            _logger.LogDebug($"{playerName} joined session with {session.Initializer}.", LoggerType.Triggers);
            RemovePlayerSessions(playerName);
        }

        if (session.TryProcessRoll(playerName, rollValue))
        {
            _logger.LogDebug($"{playerName} rolled {rollValue} in session.", LoggerType.Triggers);
        }
        else
        {
            _logger.LogDebug("Invalid roll attempt by " + playerName);
        }
    }

    /// <summary>
    /// Removes all sessions that the playerName is either an opponent or initializer of.
    /// </summary>
    private void RemovePlayerSessions(string playerName)
    {
        foreach (var session in MonitoredSessions.Values.Where(k => k.Initializer == playerName || k.Opponent == playerName).ToList())
        {
            _logger.LogDebug($"Removing session involving {playerName}, (Initializer: {session.Initializer} and Opponent {session.Opponent}" +
                " due to them joining / creating another!", LoggerType.Triggers);
            MonitoredSessions.Remove(session.Initializer);
        }
    }

    /// <summary>
    /// Triggered by a DeathRoll sessions action upon completion.
    /// </summary>
    private async void OnSessionComplete(DeathRollSession session)
    {
        var se = new SeStringBuilder().AddText("[Deathroll]").AddUiForeground("Match ended. Loser: " + session.LastRoller, 31).AddUiForegroundOff();
        _chatGui.Print(se.BuiltString);
        _logger.LogInformation("Session completed and removed.");
        // if we were the loser, then fire the deathroll trigger.
        if (session.LastRoller == _player.ClientPlayer.NameWithWorld())
            foreach (var trigger in _triggers.Storage.Social)
                await _triggerActions.HandleActionAsync(trigger.InvokableAction, MainHub.UID, ActionSource.TriggerAction);

        MonitoredSessions.Remove(session.Initializer);
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
