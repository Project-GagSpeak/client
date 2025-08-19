using CkCommons;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using GagSpeak.Gui;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using Lumina.Excel.Sheets;

namespace GagSpeak.Services;

/// <summary>
/// The service responsible for handling framework updates and other Dalamud related services.
/// </summary>
public sealed class DtrBarService : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _mainConfig;
    private readonly KinksterManager _pairManager;

    // maybe change up how this is shown, as there are new detailed tooltips and additional click methods for DTR entries.
    public DtrBarService(ILogger<DtrBarService> logger, GagspeakMediator mediator,
        MainConfig mainConfig, KinksterManager pairs) 
        : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _pairManager = pairs;

        PrivacyEntry = Svc.DtrBar.Get("GagSpeakPrivacy");
        PrivacyEntry.OnClick += _ => Mediator.Publish(new UiToggleMessage(typeof(DtrVisibleWindow)));
        PrivacyEntry.Shown = true;

        UpdateMessagesEntry = Svc.DtrBar.Get("GagSpeakNotifications");
        UpdateMessagesEntry.OnClick += _ => Mediator.Publish(new UiToggleMessage(typeof(InteractionEventsUI)));
        UpdateMessagesEntry.Shown = true;

        VibratorEntry = Svc.DtrBar.Get("GagSpeakVibrator");
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

    public List<IPlayerCharacter> VisiblePlayers { get; private set; }

    public IDtrBarEntry PrivacyEntry { get; private set; }
    public IDtrBarEntry UpdateMessagesEntry { get; private set; }
    public IDtrBarEntry VibratorEntry { get; private set; }

    private void UpdateDtrBar()
    {
        if (!MainHub.IsServerAlive)
            return;

        PrivacyEntry.Shown = _mainConfig.Current.ShowPrivacyRadar;
        UpdateMessagesEntry.Shown = (EventAggregator.UnreadInteractionsCount is 0) ? false : _mainConfig.Current.ShowActionNotifs;
        VibratorEntry.Shown = _mainConfig.Current.ShowVibeStatus;

        if (PrivacyEntry.Shown)
        {
            // update the privacy dtr bar
            var visiblePairGameObjects = _pairManager.GetVisiblePairGameObjects();
            // get players not included in our gagspeak pairs.
            var playersNotInPairs = Svc.Objects.OfType<IPlayerCharacter>()
                .Where(player => player != PlayerData.Object && !visiblePairGameObjects.Contains(player))
                .Where(o => o.ObjectIndex < 200)
                .ToList();

            // Store the list of visible players
            VisiblePlayers = playersNotInPairs;

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
        if (!PlayerData.Available) 
            return;

        try
        {
            if(!Svc.Data.GetExcelSheet<TerritoryType>().TryGetRow(PlayerContent.TerritoryID, out var row))
            {
                Logger.LogError("Failed to get map data.");
                return;
            }
            var coords = GenerateMapLinkMessageForObject(player);
            Logger.LogTrace($"{player.Name} at {coords}", LoggerType.ContextDtr);
            var mapLink = new MapLinkPayload(PlayerContent.TerritoryID, row.Map.RowId, coords.Item1, coords.Item2);
            PlayerData.OpenMapWithMapLink(mapLink);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError(ex, "Failed to locate player.");
        }
    }

    private (float, float) GenerateMapLinkMessageForObject(IGameObject playerObject)
    {
        var place = Svc.Data
            .GetExcelSheet<Map>(PlayerData.Language)?
            .FirstOrDefault(m => m.TerritoryType.RowId == PlayerContent.TerritoryID);
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

