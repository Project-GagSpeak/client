using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Toybox.Controllers;
using GagSpeak.UI;
using GagSpeak.UI.MainWindow;
using GagSpeak.UpdateMonitoring.Chat;
using OtterGui.Classes;

namespace GagSpeak.StateManagers;

/// <summary> Handles all of the commands that are used in the plugin. </summary>
public sealed class CommandManager : IDisposable
{
    private const string MainCommand = "/gagspeak";
    private const string SafewordCommand = "/safeword";
    private const string SafewordHardcoreCommand = "/safewordhardcore";
    private const string DeathRollShortcutCommand = "/dr";
    private readonly GagspeakMediator _mediator;
    private readonly PairManager _pairManager;
    private readonly GagspeakConfigService _mainConfig;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly ChatBoxMessage _chatMessages;
    private readonly DeathRollService _deathRolls;
    private readonly IChatGui _chat;
    private readonly IClientState _clientState;
    private readonly ICommandManager _commands;

    public CommandManager(GagspeakMediator mediator, PairManager pairManager,
        GagspeakConfigService mainConfig, ServerConfigurationManager serverConfigs,
        ChatBoxMessage chatMessages, DeathRollService deathRolls, 
        IChatGui chat, IClientState clientState, ICommandManager commandManager)
    {
        _mediator = mediator;
        _pairManager = pairManager;
        _mainConfig = mainConfig;
        _serverConfigs = serverConfigs;
        _chatMessages = chatMessages;
        _deathRolls = deathRolls;
        _chat = chat;
        _clientState = clientState;
        _commands = commandManager;

        // Add handlers to the main commands
        _commands.AddHandler(MainCommand, new CommandInfo(OnGagSpeak)
        {
            HelpMessage = "Toggles the UI. Use with 'help' or '?' to view sub-commands.",
            ShowInHelp = true
        });
        _commands.AddHandler(SafewordCommand, new CommandInfo(OnSafeword)
        {
            HelpMessage = "reverts all active features. For emergency uses.",
            ShowInHelp = true
        });
        _commands.AddHandler(SafewordHardcoreCommand, new CommandInfo(OnSafewordHardcore)
        {
            HelpMessage = "reverts all hardcore settings. For emergency uses.",
            ShowInHelp = true
        });
        _commands.AddHandler(DeathRollShortcutCommand, new CommandInfo(OnDeathRollShortcut)
        {
            HelpMessage = "DeathRoll Shortcut '/dr' to Start, '/dr r' to respond",
            ShowInHelp = true
        });
    }

    public void Dispose()
    {
        // Remove the handlers from the main commands
        _commands.RemoveHandler(MainCommand);
        _commands.RemoveHandler(SafewordCommand);
        _commands.RemoveHandler(SafewordHardcoreCommand);
        _commands.RemoveHandler(DeathRollShortcutCommand);
    }

    private void OnGagSpeak(string command, string args)
    {
        var splitArgs = args.ToLowerInvariant().Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);
        // if no arguements.
        if (splitArgs.Length == 0)
        {
            // Interpret this as toggling the UI
            if (_mainConfig.Current.HasValidSetup())
                _mediator.Publish(new UiToggleMessage(typeof(MainWindowUI)));
            else
                _mediator.Publish(new UiToggleMessage(typeof(IntroUi)));
            return;
        }

        else if (string.Equals(splitArgs[0], "settings", StringComparison.OrdinalIgnoreCase))
        {
            if (_mainConfig.Current.HasValidSetup())
                _mediator.Publish(new UiToggleMessage(typeof(SettingsUi)));
        }

        // if its help or ?, print help
        else if (string.Equals(splitArgs[0], "help", StringComparison.OrdinalIgnoreCase) || string.Equals(splitArgs[0], "?", StringComparison.OrdinalIgnoreCase))
        {
            PrintHelpToChat();
        }
    }

    private void OnSafeword(string command, string args)
    {
        var splitArgs = args.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);
        // splitArg[0] is the safeword
        // splitArg[1] is the UID (optional) to restrict the clear for.

        // if the safeword was not provided, ask them to provide it.
        // if the safeword was not provided, ask them to provide it.
        if (splitArgs.Length == 0 || string.IsNullOrWhiteSpace(splitArgs[0]))
        { 
            // If no safeword is provided
            _chat.Print(new SeStringBuilder().AddYellow("Please provide a safeword.").BuiltString);
            PrintSafewordHelp();
            return;
        }

        // If safeword matches, invoke the safeword mediator
        if (string.Equals(_mainConfig.Current.Safeword, splitArgs[0], StringComparison.OrdinalIgnoreCase))
        {
            if (splitArgs.Length > 1)
            {
                var uid = splitArgs[1];
                var validUserData = _pairManager.GetUserDataFromUID(uid);
                // if the UID is valid, use it.
                if (validUserData is not null) _mediator.Publish(new SafewordUsedMessage(uid));
                else
                {
                    _chat.Print(new SeStringBuilder().AddYellow($"UID Provided is not in Pair List: {uid}").BuiltString);
                    PrintSafewordHelp();
                }
            }
            else
            {
                // publish generic safeword used message.
                _mediator.Publish(new SafewordUsedMessage());
            }
        }
        else
        {
            _chat.Print(new SeStringBuilder().AddYellow("Invalid Safeword Provided.").BuiltString);
            PrintSafewordHelp();
        }
    }

    private void OnSafewordHardcore(string command, string args)
    {
        var splitArgs = args.ToUpperInvariant().Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);

        // if there is a first argument given, see if it matches one of our pairs.
        if (splitArgs.Length > 0 && !splitArgs[0].IsNullOrWhitespace())
        {
            var uid = splitArgs[0];
            var validUserData = _pairManager.GetUserDataFromUID(uid);
            // if the UID is valid, use it.
            if (validUserData is not null) _mediator.Publish(new SafewordHardcoreUsedMessage(uid));
            else
            {
                _chat.Print(new SeStringBuilder().AddYellow($"UID Provided is not in Pair List: {uid}, /safewordhardcore does not require your actual safeword.").BuiltString);
                PrintSafewordHardcoreHelp();
            }
        }
        else
        {
            _chat.Print("Triggered Hardcore Safeword");
            _mediator.Publish(new SafewordHardcoreUsedMessage());
        }
    }

    private void OnDeathRollShortcut(string command, string args)
    {
        var splitArgs = args.ToLowerInvariant().Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);
        // if no arguments.
        if (splitArgs.Length == 0)
        {
            // we initialized a DeathRoll.
            _chatMessages.SendRealMessage("/random");
            return;
        }

        // if the argument is s, start it just like above.
        if (string.Equals(splitArgs[0], "s", StringComparison.OrdinalIgnoreCase))
        {
            _chatMessages.SendRealMessage("/random");
            return;
        }

        if (string.Equals(splitArgs[0], "r", StringComparison.OrdinalIgnoreCase))
        {
            if (_clientState.LocalPlayer is null) 
                return;

            // get the last interacted with DeathRoll session.
            var lastRollCap = _deathRolls.GetLastRollCap();
            if (lastRollCap is not null)
            {
                _chatMessages.SendRealMessage($"/random "+ lastRollCap);
                return;
            }
            _chat.Print(new SeStringBuilder().AddItalics("No DeathRolls active to reply to.").BuiltString);
        }
        else
        {
            PrintHelpToChat();
        }
    }

    private void PrintHelpToChat()
    {
        _chat.Print(new SeStringBuilder().AddYellow(" -- Gagspeak Commands --").BuiltString);
        _chat.Print(new SeStringBuilder().AddCommand("/gagspeak", "Toggles the primary UI").BuiltString);
        _chat.Print(new SeStringBuilder().AddCommand("/gagspeak settings", "Toggles the settings UI window.").BuiltString);
        _chat.Print(new SeStringBuilder().AddCommand("/safeword", "Cries out your safeword, disabling any active restrictions.").BuiltString);
        _chat.Print(new SeStringBuilder().AddCommand("/safewordhardcore", "Cries out your hardcore safeword, disabling any hardcore restrictions.").BuiltString);
        _chat.Print(new SeStringBuilder().AddCommand("/dr", "Begins a DeathRoll. '/dr r' responds to the last seen or interacted DeathRoll").BuiltString);
    }

    private void PrintSafewordHelp()
    {
        _chat.Print(new SeStringBuilder().AddYellow("Usage: /safeword [safeword] [optional_UID]").BuiltString);
    }
    private void PrintSafewordHardcoreHelp()
    {
        _chat.Print(new SeStringBuilder().AddYellow("Usage: /safewordhardcore [optional_UID]").BuiltString);
    }
}

