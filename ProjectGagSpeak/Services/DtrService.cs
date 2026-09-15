using CkCommons;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.Gui;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.Watchers;
using GagSpeak.WebAPI;
using Luna;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services;

/// <summary>
/// The service responsible for handling framework updates and other Dalamud related services.
/// </summary>
public sealed class DtrService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly MainConfig _config;
    private readonly ChatConfig _chatConfig;
    private readonly KinksterManager _kinksters;
    private readonly RequestsManager _requests;
    private readonly VisibilityWatcher _visibility;

    private IDtrBarEntry _requestsEntry;
    private IDtrBarEntry _privacyEntry;
    private IDtrBarEntry _notifierEntry;
    private IDtrBarEntry _vibeEntry;

    private static HashSet<nint> _nonKinksters = [];

    // maybe change up how this is shown, as there are new detailed tooltips and additional click methods for DTR entries.
    public DtrService(ILogger<DtrService> logger, GagspeakMediator mediator,
        MainConfig mainConfig, ChatConfig chatConfig, KinksterManager pairs,
        RequestsManager requests, VisibilityWatcher visibility)
        : base(logger, mediator)
    {
        _config = mainConfig;
        _chatConfig = chatConfig;
        _kinksters = pairs;
        _requests = requests;
        _visibility = visibility;

        _requestsEntry = CreateRequestsDtr();
        _privacyEntry = CreatePrivacyDtr();
        _notifierEntry = CreateNotifierDtr();
        _vibeEntry = CreateVibeDtr();

        Mediator.Subscribe<DDSUpdateRequests>(this, _ => UpdateRequests());
        Mediator.Subscribe<DDSUpdateKinkster>(this, _ => UpdatePrivacy());
        Mediator.Subscribe<WatchedObjectCreated>(this, _ => UpdatePrivacy());
        Mediator.Subscribe<WatchedObjectDestroyed>(this, _ => UpdatePrivacy());
        Mediator.Subscribe<ConnectedMessage>(this, _ => Refresh());
        Mediator.Subscribe<DisconnectedMessage>(this, _ => Refresh());
        Mediator.Subscribe<DTRRefreshMessage>(this, _ => Refresh());
    }

    public static IReadOnlySet<nint> NonKinksters => _nonKinksters;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ClearAll();
        _requestsEntry.Remove();
        _privacyEntry.Remove();
        _notifierEntry.Remove();
        _vibeEntry.Remove();
        _requestsEntry = null!;
        _privacyEntry = null!;
        _notifierEntry = null!;
        _vibeEntry = null!;
        base.Dispose(disposing);
    }

    public IDtrBarEntry CreateRequestsDtr()
    {
        Logger.LogInformation($"Creating Requests DTR Entry!", LogFilter.DtrBar);
        var entry = Svc.DtrBar.Get("GagSpeakRequests");
        entry.Shown = true;
        entry.OnClick = _ => Mediator.Publish(new OpenMainUiTab(MainMenuTabs.SelectedTab.Requests));
        return entry;
    }

    public IDtrBarEntry CreatePrivacyDtr()
    {
        Logger.LogInformation($"Creating Privacy DTR Entry!", LogFilter.DtrBar);
        var entry = Svc.DtrBar.Get("GagSpeakPrivacy");
        entry.Shown = true;
        entry.OnClick = _ => Mediator.Publish(new UiToggleMessage(typeof(DtrVisibleWindow)));
        return entry;
    }

    public IDtrBarEntry CreateNotifierDtr()
    {
        Logger.LogInformation($"Creating Notifier DTR Entry!", LogFilter.DtrBar);
        var entry = Svc.DtrBar.Get("GagSpeakNotifications");
        entry.Shown = true;
        entry.OnClick = _ => Mediator.Publish(new UiToggleMessage(typeof(InteractionEventsUI)));
        return entry;
    }

    public IDtrBarEntry CreateVibeDtr()
    {
        Logger.LogInformation($"Creating VibeToy DTR Entry!", LogFilter.DtrBar);
        var entry = Svc.DtrBar.Get("GagSpeakVibrator");
        entry.Shown = true;
        return entry;
    }

    private void ClearAll()
    {
        Logger.LogInformation("Clearing all DTR entries.", LogFilter.DtrBar);
        _requestsEntry.Shown = false;
        _privacyEntry.Shown = false;
        _notifierEntry.Shown = false;
        _vibeEntry.Shown = false;
    }

    public void Refresh()
    {
        UpdateRequests();
        UpdatePrivacy();
        UpdateNotifier();
        UpdateVibeToy();
    }

    private void UpdateRequests()
    {
        if (!_config.Data.AlertKind.HasAny(AlertKind.DtrBar) || !MainHub.IsConnectionDataSynced || _requests.Incoming.Count is 0)
        {
            _requestsEntry.Shown = false;
            return;
        }

        _requestsEntry.Shown = true;
        var tooltip = new SeStringBuilder();
        var entryTxt = new SeStringBuilder();
        // Create the entry display.
        entryTxt.AddIcon(BitmapFontIcon.VentureDeliveryMoogle);
        entryTxt.AddText($"{_requests.Incoming.Count}");
        tooltip.AddYellow($"{_requests.Incoming.Count} Incoming Requests\n");
        foreach (var req in _requests.Incoming)
        {
            tooltip.AddIcon(req.IsTemporaryRequest ? BitmapFontIcon.GoldStar : BitmapFontIcon.BlueStar);
            tooltip.AddText($" {req.SenderAnonName}\n");
        }
        _requestsEntry.Text = entryTxt.BuiltString;
        _requestsEntry.Tooltip = tooltip.BuiltString;
    }

    private unsafe void UpdatePrivacy()
    {
        if (!_config.Data.DtrPrivacy || !MainHub.IsConnectionDataSynced)
        {
            _privacyEntry.Shown = false;
            return;
        }

        _privacyEntry.Shown = true;
        var visibleKinksters = _kinksters.DirectPairs.Where(k => k.IsRendered).Select(k => k.PlayerAddress).ToHashSet();
        // Gets the rendered players that are not paired kinksters.
        var otherNonKinksters = CharaWatcher.Rendered.Where(addr => !visibleKinksters.Contains(addr) && !PlayerData.Address.Equals(addr)).ToHashSet();

        // Update the stored list of visible non-paired players.
        _nonKinksters = otherNonKinksters;
        // (There are many ways we can improve this, we can split this privacy into its own method,
        // only update on object creation/destruction, snapshotting *Character, ext.)
        var displayed = otherNonKinksters.Take(10).ToList();
        var remaining = otherNonKinksters.Count - displayed.Count;
        bool anyNonKinksters = otherNonKinksters.Count is not 0;

        // Set the text based on if privacy was breeched or not.
        var dispIcon = anyNonKinksters ? BitmapFontIcon.Warning : BitmapFontIcon.Recording;
        var txtDisp = anyNonKinksters ? $"{otherNonKinksters.Count} Others" : "Only Pairs";
        // Limit to 10 players and indicate if there are more
        var ttDisp = anyNonKinksters
            ? $"Non-GagSpeak Players:\n{string.Join("\n", displayed.Select(p => $"{((Character*)p)->NameString}  {((Character*)p)->GetWorld()}"))}{(remaining > 0 ? $"\nand {remaining} others..." : string.Empty)}"
            : "Only GagSpeak Pairs Visible";

        // pair display string for tooltip.
        _privacyEntry.Text = new SeString(new IconPayload(dispIcon), new TextPayload(txtDisp));
        _privacyEntry.Tooltip = new SeString(new TextPayload(ttDisp));
    }

    private void UpdateNotifier()
    {
        var shouldShow = (EventAggregator.UnreadInteractionsCount is 0) ? false : _config.Data.DtrActionNotifs;
        if (!MainHub.IsConnectionDataSynced || !shouldShow)
        {
            _notifierEntry.Shown = false;
            return;
        }

        _notifierEntry.Shown = true;
        
        _notifierEntry.Text = new SeString(new IconPayload(BitmapFontIcon.Alarm), new TextPayload(EventAggregator.UnreadInteractionsCount.ToString()));
        _notifierEntry.Tooltip = new SeString(new TextPayload("Unread Notifications: " + EventAggregator.UnreadInteractionsCount));
    }

    private void UpdateVibeToy()
    {
        if (!_config.Data.DtrVibeStatus || !MainHub.IsConnectionDataSynced)
        {
            _vibeEntry.Shown = false;
            return;
        }

        //_vibeEntry.Shown = true;
        //_notifierEntry.Text = new SeString(new IconPayload(BitmapFontIcon.ElementLightning));
        //_notifierEntry.Tooltip = new SeString(new TextPayload("VibeStatus is WIP"));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("DTR Bar Service Starting");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("DTR Bar Service Stopping");
    }

    public unsafe void LocatePlayer(Character* chara)
    {
        if (!PlayerData.Available || !CharaWatcher.Rendered.Contains((nint)chara))
            return;

        try
        {
            var map = AgentMap.Instance();
            if (map == null)
            {
                Logger.LogError("Failed to open map: AgentMap instance is null.");
                return;
            }
            map->FlagMarkerCount = 0;
            map->SetFlagMapMarker(Svc.ClientState.TerritoryType, Svc.ClientState.MapId, chara->Position.X, chara->Position.Z);
            map->OpenMap(Svc.ClientState.MapId, Svc.ClientState.TerritoryType);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError(ex, "Failed to locate player.");
        }
    }
}

