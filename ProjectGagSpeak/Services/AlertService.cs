using CkCommons;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagspeakAPI.Connection;
using GagspeakAPI.Extensions;
using Microsoft.Extensions.Hosting;
using TerraFX.Interop.Windows;

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

            if (_config.Data.GarblerWarnLocation is not AlertLocation.Nowhere && gags.IsGagged() && perms.ChatGarblerActive)
                ShowNotificationLocationBased(new NotificationMessage("Zone Switch", "Live Chat Garbler is still Active!", NotificationType.Warning), _config.Data.GarblerWarnLocation);
        });
    }

    public static void ShowCustomNotification(Notification customNotif)
        => Svc.Notifications.AddNotification(customNotif);

    private static void PrintErrorChat(string? message)
    {
        var se = new SeStringBuilder().AddText("[Gagspeak] Error: " + message);
        Svc.Chat.PrintError(se.BuiltString);
    }

    public static void PrintInfoChat(string? message)
    {
        var se = new SeStringBuilder().AddUiForeground("[Gagspeak] ", 561).AddUiForegroundOff().AddText("Info: ").AddItalics(message ?? string.Empty);
        Svc.Chat.Print(se.BuiltString);
    }

    public static void PrintWarnChat(string? message)
    {
        var se = new SeStringBuilder().AddUiForeground("[Gagspeak] ", 561).AddUiForegroundOff().AddUiForeground("Warning: " + (message ?? string.Empty), 31).AddUiForegroundOff();
        Svc.Chat.Print(se.BuiltString);
    }

    public static void PrintCustomChat(SeString builtMessage)
    {
       Svc.Chat.Print(builtMessage);
    }

    public static void PrintCustomErrorChat(SeString builtMessage)
    {
        Svc.Chat.PrintError(builtMessage);
    }

    public static void PrintVersionUpdateMessage(string message, string version)
    {
        var se = new SeStringBuilder()
            .AddUiForeground("[Gagspeak] ", 561)
            .AddUiForegroundOff()
            .AddUiForeground(message, 31)
            .AddUiForegroundOff();
        Svc.Logger.Warning($"A new GagSpeak version is out and pending for download!");
        Svc.Chat.Print(se.BuiltString);
        Svc.Notifications.AddNotification(new Notification()
        {
            Content = $"GagSpeak v{version} is available for download. Update to recieve its latest features.",
            Title = "GagSpeak Has Updated",
            Type = NotificationType.Warning,
            Minimized = false,
            InitialDuration = TimeSpan.FromSeconds(6)
        });
    }

    private static void ShowChat(NotificationMessage msg)
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

    private static void ShowToast(NotificationMessage msg)
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

    #region OnlineUsers
    private readonly HashSet<string> _pendingOnlineUsers = [];
    private readonly object _batchLock = new();
    private CancellationTokenSource? _batchCts;
    private readonly TimeSpan _onlineBatchDelay = TimeSpan.FromSeconds(2.5);
    public void NotifyOnline(OnlineKinkster onlineUser)
    {
        var filter = _config.Data.OnlineNotifyFilter;
        var policy = _config.Data.OnlineNotifyPolicy;
        if (filter is OnlineFilter.None)
            return;

        var kinkster = _kinksters.GetValueOrDefault(onlineUser.User);
        var isKinkster = kinkster is not null;
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
            lock (_batchLock)
            {
                _pendingOnlineUsers.Add(displayName);
                _batchCts = _batchCts.SafeCancelRecreate();
                _ = ProcessOnlineBatchAsync(_batchCts.Token);
            }
        }
    }

    private async Task ProcessOnlineBatchAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(_onlineBatchDelay, ct).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        List<string> users;
        lock (_batchLock)
        {
            users = [.. _pendingOnlineUsers];
            _pendingOnlineUsers.Clear();
        }

        if (users.Count is 0)
            return;
        if (users.Count is 1)
            ShowNotificationLocationBased(new("Kinkster Online", $"{users[0]} is now online.", NotificationType.Info, TimeSpan.FromSeconds(3)), _config.Data.OnlineAlertLocation);
        else
        {
            var summary = users.Count <= 3 ? string.Join(", ", users) : $"{string.Join(", ", users.Take(2))} and {users.Count - 2} others";
            ShowNotificationLocationBased(new("Kinksters Online", $"{users.Count} users went online:\n{summary}", NotificationType.Info, TimeSpan.FromSeconds(4)), _config.Data.OnlineAlertLocation);
        }
    }
    #endregion

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
