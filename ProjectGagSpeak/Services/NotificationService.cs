using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Extensions;
using Microsoft.Extensions.Hosting;
using GagSpeak.State.Managers;
using GagSpeak.PlayerClient;

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
    private readonly MainConfig _mainConfig;
    private readonly GagRestrictionManager _gags;
    public NotificationService(ILogger<NotificationService> logger, GagspeakMediator mediator, 
        MainConfig mainConfig, GagRestrictionManager gags) : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _gags = gags;

        Mediator.Subscribe<NotificationMessage>(this, ShowNotification);

        // notify about live chat garbler on zone switch.
        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (_) =>
        {
            if(_gags.ServerGagData is not { } gags || OwnGlobals.Perms is not { } perms)
                return;

            if (_mainConfig.Current.LiveGarblerZoneChangeWarn && gags.IsGagged() && perms.ChatGarblerActive)
                ShowNotification(new NotificationMessage("Zone Switch", "Live Chat Garbler is still Active!", NotificationType.Warning));
        });
    }

    public void ShowCustomNotification(Notification customNotif)
        => Svc.Notifications.AddNotification(customNotif);

    private void PrintErrorChat(string? message)
    {
        var se = new SeStringBuilder().AddText("[Gagspeak] Error: " + message);
        Svc.Chat.PrintError(se.BuiltString);
    }

    private void PrintInfoChat(string? message)
    {
        var se = new SeStringBuilder().AddText("[Gagspeak] Info: ").AddItalics(message ?? string.Empty);
        Svc.Chat.Print(se.BuiltString);
    }

    private void PrintWarnChat(string? message)
    {
        var se = new SeStringBuilder().AddText("[Gagspeak] ").AddUiForeground("Warning: " + (message ?? string.Empty), 31).AddUiForegroundOff();
        Svc.Chat.Print(se.BuiltString);
    }

    public void PrintCustomChat(SeString builtMessage)
    {
       Svc.Chat.Print(builtMessage);
    }

    public void PrintCustomErrorChat(SeString builtMessage)
    {
        Svc.Chat.PrintError(builtMessage);
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
        Svc.Notifications.AddNotification(new Notification()
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
