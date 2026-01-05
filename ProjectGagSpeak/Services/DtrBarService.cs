using CkCommons;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using GagSpeak.Gui;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.Watchers;
using GagSpeak.WebAPI;
using Lumina.Excel.Sheets;

namespace GagSpeak.Services;

/// <summary>
/// The service responsible for handling framework updates and other Dalamud related services.
/// </summary>
public sealed class DtrBarService : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _mainConfig;
    private readonly KinksterManager _kinksters;

    // maybe change up how this is shown, as there are new detailed tooltips and additional click methods for DTR entries.
    public DtrBarService(ILogger<DtrBarService> logger, GagspeakMediator mediator,
        MainConfig mainConfig, KinksterManager pairs) 
        : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _kinksters = pairs;

        PrivacyEntry = Svc.DtrBar.Get("GagSpeakPrivacy");
        PrivacyEntry.OnClick += _ => Mediator.Publish(new UiToggleMessage(typeof(DtrVisibleWindow)));
        PrivacyEntry.Shown = true;

        UpdateMessagesEntry = Svc.DtrBar.Get("GagSpeakNotifications");
        UpdateMessagesEntry.OnClick += _ => Mediator.Publish(new UiToggleMessage(typeof(InteractionEventsUI)));
        UpdateMessagesEntry.Shown = true;

        VibratorEntry = Svc.DtrBar.Get("GagSpeakVibrator");
        VibratorEntry.Shown = false;

        Mediator.Subscribe<ConnectedMessage>(this, _ =>
        {
            PrivacyEntry.Shown = true;
            UpdateMessagesEntry.Shown = true;
        });

        Mediator.Subscribe<DisconnectedMessage>(this, _ =>
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

    public static HashSet<nint> NonKinksters { get; private set; } = [];

    public IDtrBarEntry PrivacyEntry { get; private set; }
    public IDtrBarEntry UpdateMessagesEntry { get; private set; }
    public IDtrBarEntry VibratorEntry { get; private set; }

    // We could change this to only update when a new object is created or
    // destroyed now, instead of updating it every second, since we are free from object table limitations.
    private unsafe void UpdateDtrBar()
    {
        if (!MainHub.IsServerAlive)
            return;

        PrivacyEntry.Shown = _mainConfig.Current.ShowPrivacyRadar;
        UpdateMessagesEntry.Shown = (EventAggregator.UnreadInteractionsCount is 0) ? false : _mainConfig.Current.ShowActionNotifs;
        VibratorEntry.Shown = _mainConfig.Current.ShowVibeStatus;

        if (PrivacyEntry.Shown)
        {
            var visibleKinksters = _kinksters.DirectPairs.Where(k => k.IsRendered).Select(k => k.PlayerAddress).ToHashSet();
            // Gets the rendered players that are not paired kinksters.
            var otherNonKinksters = CharaObjectWatcher.Rendered.Where(addr => !visibleKinksters.Contains(addr) && !PlayerData.Address.Equals(addr)).ToHashSet();

            // Update the stored list of visible non-paired players.
            NonKinksters = otherNonKinksters;
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
            PrivacyEntry.Text = new SeString(new IconPayload(dispIcon), new TextPayload(txtDisp));
            PrivacyEntry.Tooltip = new SeString(new TextPayload(ttDisp));
        }

        // Pull into seperate function and fire only whenever unread notifications update.
        if (UpdateMessagesEntry.Shown)
        {
            UpdateMessagesEntry.Text = new SeString(new IconPayload(BitmapFontIcon.Alarm), new TextPayload(EventAggregator.UnreadInteractionsCount.ToString()));
            UpdateMessagesEntry.Tooltip = new SeString(new TextPayload("Unread Notifications: " + EventAggregator.UnreadInteractionsCount));
        }
    }

    public unsafe void LocatePlayer(Character* chara)
    {
        if (!PlayerData.Available || !CharaObjectWatcher.Rendered.Contains((nint)chara))
            return;

        try
        {
            if(!Svc.Data.GetExcelSheet<TerritoryType>().TryGetRow(PlayerContent.TerritoryID, out var row))
            {
                Logger.LogError("Failed to get map data.");
                return;
            }

            var coords = GenerateMapLinkMessageForObject(*chara);
            Logger.LogTrace($"{chara->NameString} at {coords}", LoggerType.ContextDtr);
            var mapLink = new MapLinkPayload(PlayerContent.TerritoryID, row.Map.RowId, coords.Item1, coords.Item2);
            PlayerData.OpenMapWithMapLink(mapLink);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError(ex, "Failed to locate player.");
        }
    }

    private unsafe (float, float) GenerateMapLinkMessageForObject(Character chara)
    {
        var place = Svc.Data
            .GetExcelSheet<Map>(Svc.ClientState.ClientLanguage)?
            .FirstOrDefault(m => m.TerritoryType.RowId == PlayerContent.TerritoryID);
        var placeName = place?.PlaceName.RowId;
        var scale = place?.SizeFactor ?? 100f;

        return ((float)ToMapCoordinate(chara.Position.X, scale), (float)ToMapCoordinate(chara.Position.Z, scale));
    }

    // Ref: https://github.com/Bluefissure/MapLinker/blob/master/MapLinker/MapLinker.cs#L223
    private double ToMapCoordinate(double val, float scale)
    {
        var c = scale / 100.0;
        val *= c;
        return ((41.0 / c) * ((val + 1024.0) / 2048.0)) + 1;
    }
}

