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

        PrivacyEntry.Shown = _mainConfig.Data.DtrPrivacy;
        UpdateMessagesEntry.Shown = (EventAggregator.UnreadInteractionsCount is 0) ? false : _mainConfig.Data.DtrActionNotifs;
        VibratorEntry.Shown = _mainConfig.Data.DtrVibeStatus;

        if (PrivacyEntry.Shown)
        {
            var visibleKinksters = _kinksters.DirectPairs.Where(k => k.IsRendered).Select(k => k.PlayerAddress).ToHashSet();
            // Gets the rendered players that are not paired kinksters.
            var otherNonKinksters = CharaWatcher.Rendered.Where(addr => !visibleKinksters.Contains(addr) && !PlayerData.Address.Equals(addr)).ToHashSet();

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
            map->SetFlagMapMarker(Svc.ClientState.TerritoryType, Svc.ClientState.MapId, chara->Position.X,
                                  chara->Position.Z);
            map->OpenMap(Svc.ClientState.MapId, Svc.ClientState.TerritoryType);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError(ex, "Failed to locate player.");
        }
    }
}

