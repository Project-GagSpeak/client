using CkCommons;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.Gui;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using OtterGui.Classes;

namespace GagSpeak;

/// <summary> Handles all of the commands that are used in the plugin. </summary>
public sealed class CommandManager : IDisposable
{
    private const string MainCommand = "/gagspeak";
    private const string SafewordCommand = "/safeword";
    private const string SafewordHardcoreCommand = "/safewordhardcore";
    private const string DeathRollShortcutCommand = "/dr";
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _mainConfig;
    private readonly KinksterManager _kinksters;
    private readonly ServerConfigService _serverConfig;
    private readonly DeathRollService _deathRolls;
    private readonly SafewordService _safeword;
    public CommandManager(GagspeakMediator mediator, MainConfig config, KinksterManager pairManager,
        ServerConfigService server, DeathRollService dr, SafewordService safeword)
    {
        _mediator = mediator;
        _mainConfig = config;
        _kinksters = pairManager;
        _serverConfig = server;
        _deathRolls = dr;

        // Add handlers to the main commands
        Svc.Commands.AddHandler(MainCommand, new CommandInfo(OnGagSpeak)
        {
            HelpMessage = "Toggles the UI. Use with 'help' or '?' to view sub-commands.",
            ShowInHelp = true
        });
        Svc.Commands.AddHandler(SafewordCommand, new CommandInfo(OnSafeword)
        {
            HelpMessage = "reverts all active features. For emergency uses.",
            ShowInHelp = true
        });
        Svc.Commands.AddHandler(SafewordHardcoreCommand, new CommandInfo(OnSafewordHardcore)
        {
            HelpMessage = "reverts all hardcore settings. For emergency uses.",
            ShowInHelp = true
        });
        Svc.Commands.AddHandler(DeathRollShortcutCommand, new CommandInfo(OnDeathRollShortcut)
        {
            HelpMessage = "DeathRoll Shortcut '/dr' to Start, '/dr r' to respond",
            ShowInHelp = true
        });
    }

    public void Dispose()
    {
        // Remove the handlers from the main commands
        Svc.Commands.RemoveHandler(MainCommand);
        Svc.Commands.RemoveHandler(SafewordCommand);
        Svc.Commands.RemoveHandler(SafewordHardcoreCommand);
        Svc.Commands.RemoveHandler(DeathRollShortcutCommand);
    }

    private void OnGagSpeak(string command, string args)
    {
        var splitArgs = args.ToLowerInvariant().Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);
        // if no arguements.
        if (splitArgs.Length == 0)
        {
            // Interpret this as toggling the UI
            if (_mainConfig.Current.HasValidSetup() && _serverConfig.Storage.HasValidSetup())
                _mediator.Publish(new UiToggleMessage(typeof(MainUI)));
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
            Svc.Chat.Print(new SeStringBuilder().AddYellow("Please provide a safeword.").BuiltString);
            PrintSafewordHelp();
            return;
        }

        // If safeword matches, invoke the safeword mediator
        if (string.Equals(_mainConfig.Current.Safeword, splitArgs[0], StringComparison.OrdinalIgnoreCase))
        {
            if (splitArgs.Length > 1)
            {
                var uid = splitArgs[1];
                if (_kinksters.GetUserDataFromUID(uid) is { } validUserData)
                    UiService.SetUITask(_safeword.OnSafewordInvoked(validUserData.UID));
                else
                {
                    Svc.Chat.Print(new SeStringBuilder().AddYellow($"UID Provided is not in Pair List: {uid}").BuiltString);
                    PrintSafewordHelp();
                }
            }
            else
            {
                UiService.SetUITask(_safeword.OnSafewordInvoked());
            }
        }
        else
        {
            Svc.Chat.Print(new SeStringBuilder().AddYellow("Invalid Safeword Provided.").BuiltString);
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
            if (_kinksters.GetUserDataFromUID(uid) is { } validUserData)
                UiService.SetUITask(_safeword.OnHcSafewordUsed(validUserData.UID));
            else
            {
                Svc.Chat.Print(new SeStringBuilder().AddYellow($"UID Provided is not in Pair List: {uid}, /safewordhardcore does not require your actual safeword.").BuiltString);
                PrintSafewordHardcoreHelp();
            }
        }
        else
        {
            UiService.SetUITask(_safeword.OnHcSafewordUsed());
        }
    }

    private void OnDeathRollShortcut(string command, string args)
    {
        var splitArgs = args.ToLowerInvariant().Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);
        // if no arguments.
        if (splitArgs.Length == 0)
        {
            // we initialized a DeathRoll.
            ChatService.SendCommand("random");
            return;
        }

        // if the argument is s, start it just like above.
        if (string.Equals(splitArgs[0], "s", StringComparison.OrdinalIgnoreCase))
        {
            ChatService.SendCommand("random");
            return;
        }

        if (string.Equals(splitArgs[0], "r", StringComparison.OrdinalIgnoreCase))
        {
            if (PlayerData.Object is null) 
                return;

            // get the last interacted with DeathRoll session.
            var lastRollCap = _deathRolls.GetLastRollCap();
            if (lastRollCap is not null)
            {
                ChatService.SendCommand($"random {lastRollCap}");
                return;
            }
            Svc.Chat.Print(new SeStringBuilder().AddItalics("No DeathRolls active to reply to.").BuiltString);
        }
        else
        {
            PrintHelpToChat();
        }
    }

    private void PrintHelpToChat()
    {
        Svc.Chat.Print(new SeStringBuilder().AddYellow(" -- Gagspeak Commands --").BuiltString);
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/gagspeak", "Toggles the primary UI").BuiltString);
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/gagspeak settings", "Toggles the settings UI window.").BuiltString);
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/safeword", "Cries out your safeword, disabling any active restrictions.").BuiltString);
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/safewordhardcore", "Cries out your hardcore safeword, disabling any hardcore restrictions.").BuiltString);
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/dr", "Begins a DeathRoll. '/dr r' responds to the last seen or interacted DeathRoll").BuiltString);
    }

    private void PrintSafewordHelp()
    {
        Svc.Chat.Print(new SeStringBuilder().AddYellow("Usage: /safeword [safeword] [optional_UID]").BuiltString);
    }
    private void PrintSafewordHardcoreHelp()
    {
        Svc.Chat.Print(new SeStringBuilder().AddYellow("Usage: /safewordhardcore [optional_UID]").BuiltString);
    }
}

