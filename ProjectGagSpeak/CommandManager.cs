using CkCommons;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using GagSpeak.Gui;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Kinksters;
using GagSpeak.Minigames.Watchers;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagspeakAPI.Chat;
using OtterGui.Classes;
namespace GagSpeak;

/// <summary>
///   Handles all of the commands that are used in the plugin.
/// </summary>
public sealed class CommandManager : DisposableMediatorSubscriberBase
{
    private const string MainCommand = "/gagspeak";
    private const string ShortCommand = "/gspeak";

    private const string SafewordCommand = "/safeword";
    private const string SafewordHardcoreCommand = "/safewordhardcore";

    internal const string GlobalChatCommand = "/globalchat";
    internal const string GlobalChatAbrv = "/gsglobal";

    internal const string GsTellCommand = "/gstell";

    internal const string DeathRollShortcutCommand = "/dr";
    internal const string DeathRollShortcutCommandAlias = "/gdr";

    private readonly MainConfig _mainConfig;
    private readonly KinksterManager _kinksters;
    private readonly AccountConfig _serverConfig;
    private readonly DeathRollMonitor _deathRolls;
    private readonly SafewordService _safeword;
    public CommandManager(ILogger<CommandManager> logger, GagspeakMediator mediator,
        MainConfig config, KinksterManager pairManager, AccountConfig server,
        DeathRollMonitor dr, SafewordService safeword)
        : base(logger, mediator)
    {
        _mainConfig = config;
        _kinksters = pairManager;
        _serverConfig = server;
        _deathRolls = dr;
        _safeword = safeword;

        Svc.Commands.AddHandler(ShortCommand, new CommandInfo(OnGagSpeak) { DisplayOrder = 0, ShowInHelp = true, HelpMessage = "Shorthand for /gagspeak." });
        Svc.Commands.AddHandler(MainCommand, new CommandInfo(OnGagSpeak) { DisplayOrder = 1, ShowInHelp = true, HelpMessage = "Toggles the GagSpeak UI." });

        Svc.Commands.AddHandler(SafewordCommand, new CommandInfo(OnSafeword) { DisplayOrder = 2, ShowInHelp = true, HelpMessage = "Reverts all active features. For emergency uses." });
        Svc.Commands.AddHandler(SafewordHardcoreCommand, new CommandInfo(OnSafewordHardcore) { DisplayOrder = 3, ShowInHelp = true, HelpMessage = "Reverts all hardcore settings. For emergency uses." });

        Svc.Commands.AddHandler(GlobalChatAbrv, new CommandInfo(VoidChatCmd) { DisplayOrder = 4, ShowInHelp = true, HelpMessage = $"Shorthand for {GlobalChatCommand}" });
        Svc.Commands.AddHandler(GlobalChatCommand, new CommandInfo(VoidChatCmd) { DisplayOrder = 5, ShowInHelp = true, HelpMessage = BuildChatHelp(), });
        Svc.Commands.AddHandler(GsTellCommand, new CommandInfo(VoidChatCmd) { DisplayOrder = 6, ShowInHelp = false });

        Svc.Commands.AddHandler(DeathRollShortcutCommand, new CommandInfo(OnDRShortcut) { DisplayOrder = 7, ShowInHelp = false });
        Svc.Commands.AddHandler(DeathRollShortcutCommandAlias, new CommandInfo(OnDRShortcut) { DisplayOrder = 8, ShowInHelp = false });

        Mediator.Subscribe<ChatCmdFailureMessage>(this, _ => OnChatCmdFailed(_.Kind, _.Command, _.Args, _.Reason, _.Data));
    }

    private static string BuildChatHelp()
        => "Switches the native chat channel to GagSpeak Global Chat.\n" +
            $"{GlobalChatAbrv} → Shorthand for {GlobalChatCommand}\n" +
            $"{GlobalChatCommand} <message> → Sends a message to the Global Chat.\n" +
            $"{GsTellCommand} <alias|uid|anon-user name|anon-user tag> → DM's another Kinkster.\n" +
            $"\n" +
            $"Subcommands for {MainCommand} & {ShortCommand}:\n" +
            $"\t {ShortCommand} settings <navIdx> <panelIdx> → Opens the settings UI to a defined navbar and panel.\n" +
            $"\t {ShortCommand} profile → Opens your UserProfile. (Append 'edit' to open the editor)\n" +
            $"\t {ShortCommand} account → Opens the settings for your Account.\n" +
            $"\t {ShortCommand} chat → Toggles the ChatUI.\n" +
            $"\t {DeathRollShortcutCommand} / {DeathRollShortcutCommandAlias} → Deathrolls. '/dr' to Start, '/dr r' to respond.\n" +
            $"\t {SafewordCommand} → Reverts all active features. For emergency uses.\n" +
            $"\t {SafewordHardcoreCommand} → Reverts all hardcore settings. For emergency uses.";

    protected override void Dispose(bool disposing)
    {
        Svc.Commands.RemoveHandler(ShortCommand);
        Svc.Commands.RemoveHandler(MainCommand);
        Svc.Commands.RemoveHandler(SafewordCommand);
        Svc.Commands.RemoveHandler(SafewordHardcoreCommand);
        Svc.Commands.RemoveHandler(GlobalChatAbrv);
        Svc.Commands.RemoveHandler(GlobalChatCommand);
        Svc.Commands.RemoveHandler(GsTellCommand);
        Svc.Commands.RemoveHandler(DeathRollShortcutCommand);
        Svc.Commands.RemoveHandler(DeathRollShortcutCommandAlias);
        base.Dispose(disposing);
    }

    private void VoidChatCmd(string _, string __)
    { }

    private void OnChatCmdFailed(GsChatKind? kind, string command, string args, ChatFailType reason, string data)
    {
        var sb = new SeStringBuilder().AddText("GagSpeak", 527, true);

        if (kind is not { } chatKind)
        {
            Svc.Chat.PrintError(new SeStringBuilder().AddText("GagSpeak", 527, true).AddText(" Invalid subcommand: ").AddRed(command, true).BuiltString);
            Svc.Chat.Print(new SeStringBuilder().AddText("GagSpeak", 527, true).AddText(" Valid args for ").AddText("/gspeak ", 527).AddText("are:").BuiltString);
            Svc.Chat.Print(new SeStringBuilder().AddCommand("settings <navIdx> <panelIdx>", "Opens the settings UI.").BuiltString);
            Svc.Chat.Print(new SeStringBuilder().AddCommand("account", "Opens the account settings UI.").BuiltString);
            Svc.Chat.Print(new SeStringBuilder().AddCommand("profile", "Previews your UserProfile. (Append 'edit' for editor).").BuiltString);
            Svc.Chat.Print(new SeStringBuilder().AddCommand("chat", "Toggles the Sundouleia Chat UI.").BuiltString);
            return;
        }

        // #FFAE00 - sample 1
        // #F1C600 - sample 2
        // #FFB619 - sample 3

        // #FFB864 - #FFE2A7
        switch (reason)
        {
            case ChatFailType.FeatureDisabled:
                sb.AddText($" Cannot switch to ").AddYellow(chatKind.ToString()).AddText(". Feature is disabled in config.");
                break;

            case ChatFailType.InvalidChatLog:
                sb.AddText($" Cannot switch to ").AddYellow(chatKind.ToString()).AddText(". Resolved to INVALID ChatlogId.");
                break;

            case ChatFailType.MissingArgument:
                sb.AddText(" The command ").AddText(command, 527).AddText($" requires a valid ").AddBlue(chatKind switch
                {
                    GsChatKind.Direct => "Alias, UID, VanityName, or Anon-User name/tag.",
                    _ => "target argument."
                }).AddText(".");
                break;

            case ChatFailType.TargetResolutionFailed:
                // msg.Data holds the target string that failed to resolve
                sb.AddText(" Could not find ").AddText(data, 527).AddText(". (They may have DMs off)");
                break;
        }
        Svc.Chat.PrintError(sb.BuiltString);
    }

    public GsChatKind? CommandToChatKind(string cmd)
    {
        if (IsGlobalChatCommand(cmd)) return GsChatKind.Global;
        if (cmd.Equals(GsTellCommand, StringComparison.OrdinalIgnoreCase)) return GsChatKind.Direct;
        return null;
    }

    public bool IsGsChatCommand(string cmd)
        => IsGlobalChatCommand(cmd) || IsTellCommand(cmd);

    public bool IsGlobalChatCommand(string cmd)
        => cmd.Equals(GlobalChatCommand, StringComparison.OrdinalIgnoreCase)
        || cmd.Equals(GlobalChatAbrv, StringComparison.OrdinalIgnoreCase);

    public bool IsTellCommand(string cmd)
        => cmd.Equals(GsTellCommand, StringComparison.OrdinalIgnoreCase);

    private void OnGagSpeak(string command, string args)
    {
        var splitArgs = args.ToLowerInvariant().Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);
        // if no arguements.
        if (splitArgs.Length == 0)
        {
            // Interpret this as toggling the UI
            if (_mainConfig.Data.HasValidSetup() && _serverConfig.Current.HasValidSetup())
                Mediator.Publish(new UiToggleMessage(typeof(MainUI)));
            else
                Mediator.Publish(new UiToggleMessage(typeof(IntroUi)));
            return;
        }

        else if (string.Equals(splitArgs[0], "settings", StringComparison.OrdinalIgnoreCase))
        {
            if (_mainConfig.Data.HasValidSetup())
                Mediator.Publish(new UiToggleMessage(typeof(SettingsUi)));
        }
        else if (string.Equals(splitArgs[0], "chat", StringComparison.OrdinalIgnoreCase))
        {
            if (_mainConfig.Data.HasValidSetup())
                Mediator.Publish(new UiToggleMessage(typeof(GlobalChatPopoutUI)));
        }
#if DEBUG
        else if (string.Equals(splitArgs[0], "intro", StringComparison.OrdinalIgnoreCase))
        {
            Mediator.Publish(new UiToggleMessage(typeof(IntroUi)));
            return;
        }
#endif
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
        if (string.Equals(_mainConfig.Data.Safeword, splitArgs[0], StringComparison.OrdinalIgnoreCase))
        {
            if (splitArgs.Length > 1)
            {
                var aliasOrUid = splitArgs[1];
                if (_kinksters.GetFromAliasOrUid(aliasOrUid) is { } validUserData)
                    UiService.SetUITask(_safeword.OnSafewordInvoked(validUserData.UID));
                else
                {
                    Svc.Chat.Print(new SeStringBuilder().AddYellow($"UID Provided is not in Pair List: {aliasOrUid}").BuiltString);
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
            var aliasOrUid = splitArgs[0];
            if (_kinksters.GetFromAliasOrUid(aliasOrUid) is { } validUserData)
                UiService.SetUITask(_safeword.OnHcSafewordUsed(validUserData.UID));
            else
            {
                Svc.Chat.Print(new SeStringBuilder().AddYellow($"UID Provided is not in Pair List: {aliasOrUid}, /safewordhardcore does not require your actual safeword.").BuiltString);
                PrintSafewordHardcoreHelp();
            }
        }
        else
        {
            UiService.SetUITask(_safeword.OnHcSafewordUsed());
        }
    }

    private void OnDRShortcut(string command, string args)
    {
        var splitArgs = args.ToLowerInvariant().Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);
        // if no arguments.
        if (splitArgs.Length == 0)
        {
            // we initialized a DeathRoll.
            ChatControlService.SendCommand("random");
            return;
        }

        // if the argument is s, start it just like above.
        if (string.Equals(splitArgs[0], "s", StringComparison.OrdinalIgnoreCase))
        {
            ChatControlService.SendCommand("random");
            return;
        }

        if (string.Equals(splitArgs[0], "r", StringComparison.OrdinalIgnoreCase))
        {
            if (!PlayerData.Available) 
                return;

            // get the last interacted with DeathRoll session.
            var lastRollCap = _deathRolls.GetLastRollCap();
            if (lastRollCap is not null)
            {
                ChatControlService.SendCommand($"random {lastRollCap}");
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
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/gspeak", "Toggles the primary UI").BuiltString);
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/gspeak settings", "Toggles the settings UI window.").BuiltString);
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/gspeak chat", "Toggles the global chat popout UI window.").BuiltString);
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/safeword", "Cries out your safeword, disabling any active restrictions.").BuiltString);
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/safewordhardcore", "Cries out your hardcore safeword, disabling any hardcore restrictions.").BuiltString);
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/dr", "Begins a DeathRoll. '/dr r' responds to the last seen or interacted DeathRoll").BuiltString);
        Svc.Chat.Print(new SeStringBuilder().AddCommand("/gdr", "Begins a DeathRoll. '/gdr r' responds to the last seen or interacted DeathRoll").BuiltString);
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

