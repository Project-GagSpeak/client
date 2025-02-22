using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.UI.Components;
using GagSpeak.WebAPI;
using Lumina.Excel.Sheets;

namespace GagSpeak.UpdateMonitoring;

/// <summary>
/// The service responsible for handling framework updates and other Dalamud related services.
/// </summary>
public sealed class DtrBarService : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly GagspeakConfigService _mainConfig;
    private readonly EventAggregator _eventAggregator;
    private readonly PairManager _pairManager;
    private readonly ClientMonitor _clientMonitor;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly IDataManager _gameData;
    private readonly IDtrBar _dtrBar;
    public DtrBarService(ILogger<DtrBarService> logger, GagspeakMediator mediator, 
        MainHub hub, GagspeakConfigService mainConfig,  EventAggregator eventAggregator, 
        PairManager pairs, OnFrameworkService frameworkUtils, ClientMonitor clientMonitor,
        IDataManager gameData, IDtrBar dtrBar) : base(logger, mediator)
    {
        _hub = hub;
        _mainConfig = mainConfig;
        _eventAggregator = eventAggregator;
        _pairManager = pairs;
        _frameworkUtils = frameworkUtils;
        _clientMonitor = clientMonitor;
        _gameData = gameData;
        _dtrBar = dtrBar;

        PrivacyEntry = _dtrBar.Get("GagSpeakPrivacy");
        PrivacyEntry.OnClick += () => Mediator.Publish(new UiToggleMessage(typeof(DtrVisibleWindow)));
        PrivacyEntry.Shown = true;

        UpdateMessagesEntry = _dtrBar.Get("GagSpeakNotifications");
        UpdateMessagesEntry.OnClick += () => Mediator.Publish(new UiToggleMessage(typeof(InteractionEventsUI)));
        UpdateMessagesEntry.Shown = true;

        VibratorEntry = _dtrBar.Get("GagSpeakVibrator");
        VibratorEntry.Shown = false;

        Mediator.Subscribe<MainHubConnectedMessage>(this, _ =>
        {
            PrivacyEntry.Shown = true;
            UpdateMessagesEntry.Shown = true;
        });

        Mediator.Subscribe<MainHubDisconnectedMessage>(this, _ =>
        {
            PrivacyEntry.Shown = false;
            UpdateMessagesEntry.Shown = false;
        });
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => UpdateDtrBar());
    }

    protected override void Dispose(bool disposing)
    {
        PrivacyEntry.Remove();
        UpdateMessagesEntry.Remove();
        VibratorEntry.Remove();
        base.Dispose(disposing);
    }

    public List<IPlayerCharacter> _visiblePlayers;

    public IDtrBarEntry PrivacyEntry { get; private set; }
    public IDtrBarEntry UpdateMessagesEntry { get; private set; }
    public IDtrBarEntry VibratorEntry { get; private set; }

    private void UpdateDtrBar()
    {
        if (!MainHub.IsServerAlive)
            return;

        PrivacyEntry.Shown = _mainConfig.Config.ShowPrivacyRadar;
        UpdateMessagesEntry.Shown = (EventAggregator.UnreadInteractionsCount is 0) ? false : _mainConfig.Config.ShowActionNotifs;
        VibratorEntry.Shown = _mainConfig.Config.ShowVibeStatus;

        if (PrivacyEntry.Shown)
        {
            // update the privacy dtr bar
            var visiblePairGameObjects = _pairManager.GetVisiblePairGameObjects();
            // get players not included in our gagspeak pairs.
            var playersNotInPairs = _frameworkUtils.GetObjectTablePlayers()
                .Where(player => player != _clientMonitor.ClientPlayer && !visiblePairGameObjects.Contains(player))
                .Where(o => o.ObjectIndex < 200)
                .ToList();

            // Store the list of visible players
            _visiblePlayers = playersNotInPairs;

            var displayedPlayers = playersNotInPairs.Take(10).ToList();
            var remainingCount = playersNotInPairs.Count - displayedPlayers.Count;

            // set the text based on if privacy was breeched or not.
            var DisplayIcon = playersNotInPairs.Any() ? BitmapFontIcon.Warning : BitmapFontIcon.Recording;
            var TextDisplay = playersNotInPairs.Any() ? (playersNotInPairs.Count + " Others") : "Only Pairs";
            // Limit to 10 players and indicate if there are more
            var TooltipDisplay = playersNotInPairs.Any()
                ? "Non-GagSpeak Players:\n" + string.Join("\n", displayedPlayers.Select(player => player.Name.ToString() + "  " + player.HomeWorld.Value.Name.ToString())) +
                    (remainingCount > 0 ? $"\nand {remainingCount} others..." : string.Empty)
                : "Only GagSpeak Pairs Visible";
            // pair display string for tooltip.
            PrivacyEntry.Text = new SeString(new IconPayload(DisplayIcon), new TextPayload(TextDisplay));
            PrivacyEntry.Tooltip = new SeString(new TextPayload(TooltipDisplay));
        }

        if (UpdateMessagesEntry.Shown)
        {
            UpdateMessagesEntry.Text = new SeString(new IconPayload(BitmapFontIcon.Alarm), new TextPayload(EventAggregator.UnreadInteractionsCount.ToString()));
            UpdateMessagesEntry.Tooltip = new SeString(new TextPayload("Unread Notifications: " + EventAggregator.UnreadInteractionsCount));
        }
    }

    public void LocatePlayer(IPlayerCharacter player)
    {
        if (!_clientMonitor.IsPresent) 
            return;

        try
        {
            if(!_gameData.GetExcelSheet<TerritoryType>().TryGetRow(_clientMonitor.TerritoryId, out var row))
            {
                Logger.LogError("Failed to get map data.");
                return;
            }
            var coords = GenerateMapLinkMessageForObject(player);
            Logger.LogTrace($"{player.Name} at {coords}", LoggerType.ContextDtr);
            var mapLink = new MapLinkPayload(_clientMonitor.TerritoryId, row.Map.RowId, coords.Item1, coords.Item2);
            _clientMonitor.OpenMapWithMapLink(mapLink);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to locate player.");
        }
    }

    private (float, float) GenerateMapLinkMessageForObject(IGameObject playerObject)
    {
        var place = _gameData
            .GetExcelSheet<Map>(_clientMonitor.ClientLanguage)?
            .FirstOrDefault(m => m.TerritoryType.RowId == _clientMonitor.TerritoryId);
        var placeName = place?.PlaceName.RowId;
        var scale = place?.SizeFactor ?? 100f;

        return ((float)ToMapCoordinate(playerObject.Position.X, scale), (float)ToMapCoordinate(playerObject.Position.Z, scale));
    }

    // Ref: https://github.com/Bluefissure/MapLinker/blob/master/MapLinker/MapLinker.cs#L223
    private double ToMapCoordinate(double val, float scale)
    {
        var c = scale / 100.0;
        val *= c;
        return ((41.0 / c) * ((val + 1024.0) / 2048.0)) + 1;
    }
}

