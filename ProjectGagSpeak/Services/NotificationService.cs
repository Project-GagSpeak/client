using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Extensions;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services;

public enum NotificationLocation
{
    Nowhere,
    Chat,
    Toast,
    Both
}

/// <summary> Service responsible for displaying any sent notifications out to the user. </summary>
public class NotificationService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly GagspeakConfigService _mainConfig;
    private readonly GlobalData _playerData;
    private readonly GagRestrictionManager _gags;
    private readonly INotificationManager _notifications;
    private readonly IChatGui _chat;

    public NotificationService(ILogger<NotificationService> logger, GagspeakMediator mediator,
        GagspeakConfigService mainConfig, GlobalData playerData, GagRestrictionManager gags,
        IChatGui chat, INotificationManager notifications) : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _playerData = playerData;
        _gags = gags;
        _chat = chat;
        _notifications = notifications;

        Mediator.Subscribe<NotificationMessage>(this, ShowNotification);
        Mediator.Subscribe<NotifyChatMessage>(this, ShowChat);

        // notify about live chat garbler on zone switch.
        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (_) =>
        {
            if(_gags.ServerGagData is not { } gags || _playerData.GlobalPerms is not { } perms)
                return;

            if (_mainConfig.Config.LiveGarblerZoneChangeWarn && gags.IsGagged() && perms.ChatGarblerActive)
                ShowNotification(new NotificationMessage("Zone Switch", "Live Chat Garbler is still Active!", NotificationType.Warning));
        });
    }

    private void PrintErrorChat(string? message)
    {
        var se = new SeStringBuilder().AddText("[Gagspeak] Error: " + message);
        _chat.PrintError(se.BuiltString);
    }

    private void PrintInfoChat(string? message)
    {
        var se = new SeStringBuilder().AddText("[Gagspeak] Info: ").AddItalics(message ?? string.Empty);
        _chat.Print(se.BuiltString);
    }

    private void PrintWarnChat(string? message)
    {
        var se = new SeStringBuilder().AddText("[Gagspeak] ").AddUiForeground("Warning: " + (message ?? string.Empty), 31).AddUiForegroundOff();
        _chat.Print(se.BuiltString);
    }

    public void PrintCustomChat(SeString builtMessage)
    {
       _chat.Print(builtMessage);
    }

    public void PrintCustomErrorChat(SeString builtMessage)
    {
        _chat.PrintError(builtMessage);
    }

    private void ShowChat(NotificationMessage msg)
    {
        switch (msg.Type)
        {
            case NotificationType.Info:
            case NotificationType.Success:
            case NotificationType.None:
                PrintInfoChat(msg.Message);
                break;

            case NotificationType.Warning:
                PrintWarnChat(msg.Message);
                break;

            case NotificationType.Error:
                PrintErrorChat(msg.Message);
                break;
        }
    }

    private void ShowChat(NotifyChatMessage msg)
    {
        switch (msg.Type)
        {
            case NotificationType.Info:
            case NotificationType.Success:
            case NotificationType.None:
            case NotificationType.Warning:
                PrintCustomChat(msg.Message);
                break;

            case NotificationType.Error:
                PrintCustomErrorChat(msg.Message);
                break;
        }
    }

    private void ShowNotification(NotificationMessage msg)
    {
        Logger.LogInformation(msg.ToString());

        switch (msg.Type)
        {
            case NotificationType.Info:
            case NotificationType.Success:
            case NotificationType.None:
                ShowNotificationLocationBased(msg, _mainConfig.Config.InfoNotification);
                break;

            case NotificationType.Warning:
                ShowNotificationLocationBased(msg, _mainConfig.Config.WarningNotification);
                break;

            case NotificationType.Error:
                ShowNotificationLocationBased(msg, _mainConfig.Config.ErrorNotification);
                break;
        }
    }

    private void ShowNotificationLocationBased(NotificationMessage msg, NotificationLocation location)
    {
        switch (location)
        {
            case NotificationLocation.Toast:
                ShowToast(msg);
                break;

            case NotificationLocation.Chat:
                ShowChat(msg);
                break;

            case NotificationLocation.Both:
                ShowToast(msg);
                ShowChat(msg);
                break;

            case NotificationLocation.Nowhere:
                break;
        }
    }

    private void ShowToast(NotificationMessage msg)
    {
        _notifications.AddNotification(new Notification()
        {
            Content = msg.Message ?? string.Empty,
            Title = msg.Title,
            Type = msg.Type,
            Minimized = false,
            InitialDuration = msg.TimeShownOnScreen ?? TimeSpan.FromSeconds(3)
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Notification Service is starting.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Notification Service is stopping.");
        return Task.CompletedTask;
    }
}
