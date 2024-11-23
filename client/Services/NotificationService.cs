using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services.Mediator;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services;

/// <summary>
/// Service responsible for displaying any sent notifications out to the user.
/// </summary>
public class NotificationService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly GagspeakConfigService _mainConfig;
    private readonly PlayerCharacterData _playerData;
    private readonly INotificationManager _notifications;
    private readonly IChatGui _chat;

    public NotificationService(ILogger<NotificationService> logger, GagspeakMediator mediator,
        GagspeakConfigService mainConfig, PlayerCharacterData playerData, IChatGui chat,
        INotificationManager notifications) : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _playerData = playerData;
        _chat = chat;
        _notifications = notifications;

        Mediator.Subscribe<NotificationMessage>(this, ShowNotification);
        Mediator.Subscribe<NotifyChatMessage>(this, ShowChat);

        // notify about live chat garbler on zone switch.
        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (_) =>
        {
            if (_mainConfig.Current.LiveGarblerZoneChangeWarn && _playerData.IsPlayerGagged && (_playerData.GlobalPerms?.LiveChatGarblerActive ?? false))
                ShowNotification(new NotificationMessage("Zone Switch", "Live Chat Garbler is still Active!", NotificationType.Warning));
        });
    }

    private void PrintErrorChat(string? message)
    {
        SeStringBuilder se = new SeStringBuilder().AddText("[Gagspeak] Error: " + message);
        _chat.PrintError(se.BuiltString);
    }

    private void PrintInfoChat(string? message)
    {
        SeStringBuilder se = new SeStringBuilder().AddText("[Gagspeak] Info: ").AddItalics(message ?? string.Empty);
        _chat.Print(se.BuiltString);
    }

    private void PrintWarnChat(string? message)
    {
        SeStringBuilder se = new SeStringBuilder().AddText("[Gagspeak] ").AddUiForeground("Warning: " + (message ?? string.Empty), 31).AddUiForegroundOff();
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
                ShowNotificationLocationBased(msg, _mainConfig.Current.InfoNotification);
                break;

            case NotificationType.Warning:
                ShowNotificationLocationBased(msg, _mainConfig.Current.WarningNotification);
                break;

            case NotificationType.Error:
                ShowNotificationLocationBased(msg, _mainConfig.Current.ErrorNotification);
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
