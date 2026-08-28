using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagspeakAPI.Connection;
using GagspeakAPI.Extensions;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services;

public enum AlertLocation
{
    Nowhere,
    Chat,
    Toast,
    Both
}

/// <summary> Service responsible for displaying any sent notifications out to the user. </summary>
public class AlertService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly MainConfig _config;
    private readonly NicksConfig _nicks;
    private readonly GagRestrictionManager _gags;
    private readonly KinksterManager _kinksters;
    private readonly PairService _pairService;
    public AlertService(ILogger<AlertService> logger, GagspeakMediator mediator, 
        MainConfig mainConfig, NicksConfig nicks, GagRestrictionManager gags,
        KinksterManager kinksters, PairService pairService)
        : base(logger, mediator)
    {
        _config = mainConfig;
        _nicks = nicks;
        _gags = gags;
        _kinksters = kinksters;
        _pairService = pairService;

        Mediator.Subscribe<NotificationMessage>(this, ShowNotification);

        // notify about live chat garbler on zone switch.
        Mediator.Subscribe<TerritoryChanged>(this, (_) =>
        {
            if(_gags.ServerGagData is not { } gags || ClientData.Globals is not { } perms)
                return;

            if (_config.Data.LiveGarblerZoneChangeWarn && gags.IsGagged() && perms.ChatGarblerActive)
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
                ShowNotificationLocationBased(msg, _config.Data.InfoNotification);
                break;

            case NotificationType.Warning:
                ShowNotificationLocationBased(msg, _config.Data.WarningNotification);
                break;

            case NotificationType.Error:
                ShowNotificationLocationBased(msg, _config.Data.ErrorNotification);
                break;
        }
    }

    private void ShowNotificationLocationBased(NotificationMessage msg, AlertLocation location)
    {
        switch (location)
        {
            case AlertLocation.Toast:
                ShowToast(msg);
                break;

            case AlertLocation.Chat:
                ShowChat(msg);
                break;

            case AlertLocation.Both:
                ShowToast(msg);
                ShowChat(msg);
                break;

            case AlertLocation.Nowhere:
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

    public void NotifyOnline(OnlineKinkster onlineUser)
    {
        var filter = _config.Data.OnlineNotifyFilter;
        var policy = _config.Data.OnlineNotifyPolicy;
        if (filter is OnlineFilter.None)
            return;

        var kinkster = _kinksters.GetValueOrDefault(onlineUser.User);
        var isTemp = kinkster?.IsTemporary ?? false;
        var isNicked = !string.IsNullOrEmpty(_nicks.GetNicknameForUid(onlineUser.User.UID));
        var isFavorite = FavoritesConfig.Kinksters.Contains(onlineUser.User.UID);

        bool any = false, all = true;

        void Eval(bool filterEnabled, bool conditionMet)
        {
            any |= filterEnabled && conditionMet;
            all &= !filterEnabled || conditionMet;
        }

        Eval(filter.HasFlag(OnlineFilter.Temporary), isTemp);
        Eval(filter.HasFlag(OnlineFilter.Nicknamed), isNicked);
        Eval(filter.HasFlag(OnlineFilter.Favorited), isFavorite);

        if (policy is FilterPolicy.MatchAny ? any : all)
        {
            var displayName = _pairService.GetDisplayName(onlineUser.User);
            Mediator.Publish(new NotificationMessage("Pair Online", $"{displayName} is now online", NotificationType.Info, TimeSpan.FromSeconds(2)));
        }
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
