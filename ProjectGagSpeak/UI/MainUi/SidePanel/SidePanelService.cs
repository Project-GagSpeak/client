using CkCommons;
using CkCommons.DrawSystem;
using Dalamud.Bindings.ImGui;
using GagSpeak.CustomCombos;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.CustomCombos.Moodles;
using GagSpeak.CustomCombos.Padlock;
using GagSpeak.CustomCombos.Pairs;
using GagSpeak.Gui.Components;
using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.Gui.MainWindow;

public enum SidePanelMode
{
    None,
    Interactions,
    // can add more as nescessary.
}

public interface ISidePanelCache
{
    SidePanelMode Mode { get; }

    // If we can draw this displayMode. (Maybe remove this since it wont be necessary)
    bool IsValid { get; }

    // The width of the draw display.
    public float DispWidth { get; }
}

// Update this with whatever we had for caching services prior.
public class KinksterInfoCache : ISidePanelCache, IDisposable
{
    public SidePanelMode Mode => SidePanelMode.Interactions;

    // Stored internally for regenerators.
    private readonly ILogger _log;
    private readonly MainHub _hub;
   
    private HypnoEffectEditor _hypnoEditor;
    public KinksterInfoCache(ILogger log, MainHub hub, Kinkster kinkster, HypnoEffectManager hypno, TutorialService guides)
    {
        _log = log;
        _hub = hub;
        _hypnoEditor = new HypnoEffectEditor("KinksterEffectEditor", hypno, guides);

        UpdateKinkster(kinkster);
    }

    // Variables that are freely changable and used for the various opened windows.
    public int GagLayer = 0;
    public int RestrictionLayer = 0;
    public int RestraintLayer = 0;

    public string HypnoTimer = string.Empty;

    public string EmoteTimer = string.Empty;
    public uint EmoteId = 0;
    public int CyclePose = 0;

    public string ConfinementTimer = string.Empty;
    public AddressBookEntry Address = new();

    public string ImprisonTimer = string.Empty;
    public Vector3 ImprisonPos = Vector3.Zero;
    public float ImprisonRadius = 1f;

    public string ChatBoxHideTimer = string.Empty;
    public string ChatInputHideTimer = string.Empty;
    public string ChatInputBlockTimer = string.Empty;

    public int ApplyIntensity = 0;
    public int ApplyVibeIntensity = 0;
    public float ApplyDuration = 0;
    public float ApplyVibeDur = 0;
    public float TmpVibeDur = -1;

    // Readonly Publics
    public string DisplayName => Kinkster.GetNickAliasOrUid();
    public bool   IsValid     => Kinkster is not null;
    public float  DispWidth   => Math.Max(300f, ImGui.CalcTextSize($"{DisplayName} prevents removing locked layers ").X + ImGui.GetFrameHeightWithSpacing() * 2).AddWinPadX();

    // Instance get-private setters.
    public string                        LastUID     { get; private set; }
    public Kinkster                      Kinkster    { get; private set; }
    public InteractionType               OpenItem    { get; private set; } = InteractionType.None;
    public PresetName                    CurPreset   { get; set; }         = PresetName.NoneSelected;

    // Custom Combos
    public PairGagCombo                 Gags { get; private set; } = null!;
    public PairGagPadlockCombo          GagLocks { get; private set; } = null!;
    public PairRestrictionCombo         Restrictions { get; private set; } = null!;
    public PairRestrictionPadlockCombo  RestrictionLocks{ get; private set; } = null!;
    public PairRestraintCombo           Restraints { get; private set; } = null!;
    public PairRestraintPadlockCombo    RestraintLocks { get; private set; } = null!;
    public PairStatusCombo              Statuses { get; private set; } = null!;
    public PairPresetCombo              Presets { get; private set; } = null!;
    public PairPatternCombo             Patterns { get; private set; } = null!;
    public PairStatusCombo              Remover { get; private set; } = null!;

    public PairAlarmCombo               Alarms { get; private set; } = null!;
    public PairTriggerCombo             Triggers { get; private set; } = null!;
    public OwnStatusCombo   OwnStatuses { get; private set; } = null!;
    public OwnPresetCombo   OwnPresets { get; private set; } = null!;
    public EmoteCombo                   Emotes { get; private set; } = null!;
    public WorldCombo                   Worlds { get; private set; } = null!;

    public void Dispose()
    {
        _hypnoEditor.Dispose();
    }

    public void ToggleInteraction(InteractionType act)
        => OpenItem = (OpenItem == act) ? InteractionType.None : act;

    public void ClearInteraction()
        => OpenItem = InteractionType.None;

    public void ToggleHypnosisView()
    {
        if (OpenItem is not InteractionType.HypnosisEffect)
        {
            ToggleInteraction(InteractionType.HypnosisEffect);
            if (_hypnoEditor.IsEffectNull)
                _hypnoEditor.SetBlankEffect();
        }
        else
        {
            ClearInteraction();
        }
    }

    public void DrawHypnosisEditor(float width)
    {
        _hypnoEditor.DrawCompactEditorTabs(width);
        ImGui.Dummy(_hypnoEditor.DisplayPreviewWidthConstrained(width, Constants.DefaultHypnoPath));
    }

    public void UpdateKinkster(Kinkster kinkster, bool resetVars = true)
    {
        LastUID = kinkster.UserData.UID;
        Kinkster = kinkster;

        Gags = new PairGagCombo(_log, _hub, Kinkster, ClearInteraction);
        GagLocks = new PairGagPadlockCombo(_log, _hub, Kinkster, ClearInteraction);
        Restrictions = new PairRestrictionCombo(_log, _hub, Kinkster, ClearInteraction);
        RestrictionLocks = new PairRestrictionPadlockCombo(_log, _hub, Kinkster, ClearInteraction);
        Restraints = new PairRestraintCombo(_log, _hub, Kinkster, ClearInteraction);
        RestraintLocks = new PairRestraintPadlockCombo(_log, _hub, Kinkster, ClearInteraction);
        Statuses = new PairStatusCombo(_log, _hub, Kinkster, 1.3f);
        Presets = new PairPresetCombo(_log, _hub, Kinkster, 1.3f);
        Patterns = new PairPatternCombo(_log, _hub, Kinkster, ClearInteraction);
        Alarms = new PairAlarmCombo(_log, _hub, Kinkster);
        Triggers = new PairTriggerCombo(_log, _hub, Kinkster);
        OwnStatuses = new OwnStatusCombo(_log, _hub, Kinkster, 1.3f);
        OwnPresets = new OwnPresetCombo(_log, _hub, Kinkster, 1.3f);
        Remover = new PairStatusCombo(_log, _hub, Kinkster, 1.3f, () =>
        {
            if (Kinkster.PairPerms.MoodleAccess.HasAny(MoodleAccess.RemoveAny))
                return [.. Kinkster.MoodleData.DataInfoList.OrderBy(x => x.Title)];
            else if (Kinkster.PairPerms.MoodleAccess.HasAny(MoodleAccess.RemoveApplied))
                return [.. Kinkster.MoodleData.DataInfoList.Where(x => x.Applier == PlayerData.NameWithWorld).OrderBy(x => x.Title)];
            else
                return [];
        });
        Emotes = new EmoteCombo(_log, 1.3f, () => [
            ..Kinkster.PairPerms.AllowLockedEmoting ? EmoteEx.LoopedEmotes() : EmoteEx.SittingEmotes()
        ]);
        Worlds = new WorldCombo(_log);

        if (resetVars)
            ResetModifiableVariables();

        // Reset the opened item and preset.
        OpenItem = InteractionType.None;
        CurPreset = PresetName.NoneSelected;
    }

    private void ResetModifiableVariables()
    {
        GagLayer = 0;
        RestrictionLayer = 0;
        RestraintLayer = 0;
        HypnoTimer = string.Empty;
        EmoteId = 0;
        CyclePose = 0;
        Address = new AddressBookEntry();
        ImprisonPos = Vector3.Zero;
        ImprisonRadius = 1f;
        ApplyIntensity = 0;
        ApplyVibeIntensity = 0;
        ApplyDuration = 0.1f;
        ApplyVibeDur = 0.1f;
        TmpVibeDur = 0.1f;
    }

    // Try to push all the below methods into seperate classes that call them or something.
    public void TrySendHypnosisAction()
    {
        if (!_hypnoEditor.TryGetEffect(out var effect))
        {
            Svc.Logger.Verbose("Effect was null or time parsing failed!");
            return;
        }
        if (!PadlockEx.TryParseTimeSpan(HypnoTimer, out var newTime))
        {
            HypnoTimer = string.Empty;
            Svc.Logger.Verbose("Effect was null or time parsing failed!");
            return;
        }
        // compose the DTO to send.
        UiService.SetUITask(async () =>
        {
            var dto = new HypnoticAction(Kinkster!.UserData, DateTimeOffset.UtcNow.AddSeconds(newTime.TotalSeconds), effect);
            if (await _hub.UserHypnotizeKinkster(dto) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            {
                switch (res.ErrorCode)
                {
                    case GagSpeakApiEc.BadUpdateKind: Svc.Toasts.ShowError("Invalid Update Kind. Please try again."); break;
                    case GagSpeakApiEc.InvalidTime: Svc.Toasts.ShowError("Invalid Timer Syntax. Must be a valid time format (Ex: 1h2m7s)."); break;
                    case GagSpeakApiEc.LackingPermissions: Svc.Toasts.ShowError("You do not have permission to perform this action."); break;
                    default: Svc.Logger.Debug($"Failed to send Hypnosis Effect to {DisplayName}: {res.ErrorCode}."); break;
                }
            }
            else
            {
                Svc.Logger.Debug($"Sent Hypnosis Effect to {DisplayName} with duration: {newTime} (seconds)", LoggerType.StickyUI);
            }
        });
    }

    // Try to push all the below methods into seperate classes that call them or something.
    public void TryEnableHardcoreAction(HcAttribute attribute)
    {
        if (attribute is HcAttribute.HypnoticEffect)
            return;

        var timerStr = attribute switch
        {
            HcAttribute.EmoteState => EmoteTimer,
            HcAttribute.Confinement => ConfinementTimer,
            HcAttribute.Imprisonment => ImprisonTimer,
            HcAttribute.HiddenChatBox => ChatBoxHideTimer,
            HcAttribute.HiddenChatInput => ChatInputHideTimer,
            HcAttribute.BlockedChatInput => ChatInputBlockTimer,
            _ => string.Empty
        };

        // Default to infinite.
        var expireTimer = DateTimeOffset.MaxValue;
        // if the timer is not null or whitespace try to parse it.
        if (!string.IsNullOrWhiteSpace(timerStr))
        {
            if (!PadlockEx.TryParseTimeSpan(timerStr, out var newTime))
            {
                Svc.Toasts.ShowError($"Failed to parse time for {attribute} with: [{timerStr}]");
                return;
            }
            // Otherwise it is valid, so update expire timer.
            expireTimer = DateTimeOffset.UtcNow.Add(newTime);
        }

        var enactingString = Kinkster!.PairPerms.DevotionalLocks ? $"{MainHub.UID}{Constants.DevotedString}" : MainHub.UID;
        var newHcData = attribute switch
        {
            HcAttribute.Follow => Kinkster.PairHardcore with { LockedFollowing = enactingString },
            HcAttribute.EmoteState => Kinkster.PairHardcore with
            {
                LockedEmoteState = enactingString,
                EmoteExpireTime = expireTimer,
                EmoteId = (ushort)EmoteId,
                EmoteCyclePose = (byte)CyclePose
            },
            HcAttribute.Confinement => Kinkster.PairHardcore with
            {
                IndoorConfinement = enactingString,
                ConfinementTimer = expireTimer,
                ConfinedWorld = Address.World,
                ConfinedCity = (int)Address.City,
                ConfinedWard = Address.Ward,
                ConfinedPlaceId = Address.PropertyType is PropertyType.House ? Address.Plot : Address.Apartment,
                ConfinedInApartment = Address.PropertyType is PropertyType.Apartment,
                ConfinedInSubdivision = Address.ApartmentSubdivision
            },
            HcAttribute.Imprisonment => Kinkster.PairHardcore with
            {
                Imprisonment = enactingString,
                ImprisonmentTimer = expireTimer,
                ImprisonedTerritory = (short)PlayerContent.TerritoryIdInstanced,
                ImprisonedPos = ImprisonPos,
                ImprisonedRadius = ImprisonRadius
            },
            HcAttribute.HiddenChatBox => Kinkster.PairHardcore with { ChatBoxesHidden = enactingString, ChatBoxesHiddenTimer = expireTimer },
            HcAttribute.HiddenChatInput => Kinkster.PairHardcore with { ChatInputHidden = enactingString, ChatInputHiddenTimer = expireTimer },
            HcAttribute.BlockedChatInput => Kinkster.PairHardcore with { ChatInputBlocked = enactingString, ChatInputBlockedTimer = expireTimer },
            _ => Kinkster!.PairHardcore
        };

        // Process the task.
        UiService.SetUITask(async () =>
        {
            var dto = new HardcoreStateChange(Kinkster.UserData, newHcData, attribute, MainHub.OwnUserData);
            if (await _hub.UserChangeOtherHardcoreState(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            {
                switch (res.ErrorCode)
                {
                    case GagSpeakApiEc.BadUpdateKind: Svc.Toasts.ShowError("Invalid Update Kind. Please try again."); break;
                    case GagSpeakApiEc.InvalidDataState: Svc.Toasts.ShowError("Tried to switch to Invalid Data State!"); break;
                    case GagSpeakApiEc.InvalidTime: Svc.Toasts.ShowError("Invalid Timer Syntax. Must be a valid time format (Ex: 1h2m7s)."); break;
                    case GagSpeakApiEc.LackingPermissions: Svc.Toasts.ShowError("You do not have permission to perform this action."); break;
                    default: Svc.Logger.Debug($"Failed to send HardcoreStatusChange to {DisplayName}: {res.ErrorCode}."); break;
                }
            }
            else
            {
                Svc.Logger.Debug($"Changed {DisplayName}'s Hardcore State ({attribute}) to enabled [For {(expireTimer - DateTimeOffset.UtcNow)}]", LoggerType.HardcoreActions);
                ClearInteraction();
            }
        });
    }

    // Try to push all the below methods into seperate classes that call them or something.
    public void TryDisableHardcoreAction(HcAttribute attribute)
    {
        UiService.SetUITask(async () =>
        {
            var dto = new HardcoreStateChange(Kinkster!.UserData, new HardcoreStatus(), attribute, MainHub.OwnUserData);
            if (await _hub.UserChangeOtherHardcoreState(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            {
                switch (res.ErrorCode)
                {
                    case GagSpeakApiEc.BadUpdateKind: Svc.Toasts.ShowError("Invalid Update Kind. Please try again."); break;
                    case GagSpeakApiEc.InvalidDataState: Svc.Toasts.ShowError("Tried to switch to Invalid Data State!"); break;
                    case GagSpeakApiEc.InvalidTime: Svc.Toasts.ShowError("Invalid Timer Syntax. Must be a valid time format (Ex: 1h2m7s)."); break;
                    case GagSpeakApiEc.LackingPermissions: Svc.Toasts.ShowError("You do not have permission to perform this action."); break;
                    default: Svc.Logger.Debug($"Failed to send HardcoreStatusChange to {DisplayName}: {res.ErrorCode}."); break;
                }
            }
            else
            {
                Svc.Logger.Debug($"Changed {DisplayName}'s Hardcore State ({attribute}) to disabled", LoggerType.HardcoreActions);
                ClearInteraction();
            }
        });
    }
}

public sealed class SidePanelService : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly SidePanelTabs _tabs;
    private readonly HypnoEffectManager _hypnoManager;
    private readonly TutorialService _guides;

    public SidePanelService(ILogger<SidePanelService> logger, GagspeakMediator mediator, 
        MainHub hub, HypnoEffectManager hypnoManager, SidePanelTabs tabs, TutorialService guides)
        : base(logger, mediator)
    {
        _hub = hub;
        _hypnoManager = hypnoManager;
        _tabs = tabs;
        _guides = guides;

        // Dont clear display entirely if on Interactions.
        Mediator.Subscribe<DisconnectedMessage>(this, _ => ClearDisplay());

        Mediator.Subscribe<MainWindowTabChangeMessage>(this, _ => UpdateForNewTab(_.NewTab));
        Mediator.Subscribe<OpenKinksterSidePanel>(this, _ => ForInteractions(_.Kinkster, _.ForceOpen));
        Mediator.Subscribe<KinksterRemovedMessage>(this, _ => CloseInteractions());
        Mediator.Subscribe<ConnectedMessage>(this, _ =>
        {
            // Maybe do some restoration of Interactions here if any were present, but otherwise, ignore.
        });
        
    }

    private void UpdateForNewTab(MainMenuTabs.SelectedTab newTab)
    {
        if (DisplayMode is SidePanelMode.Interactions)
        {
            // If we are switching to whitelist, keep it open.
            if (newTab is MainMenuTabs.SelectedTab.Whitelist)
                return;
            // Otherwise clear it.
            ClearDisplay();
        }
    }

    public ISidePanelCache? DisplayCache { get; private set; }

    public SidePanelMode DisplayMode => DisplayCache?.Mode ?? SidePanelMode.None;
    public bool CanDraw => DisplayCache?.IsValid ?? false;
    public float DisplayWidth => DisplayCache?.DispWidth ?? 250f;
    public void ClearDisplay()
    {
        // DO NOT CLOSE THIS RIGHT AWAY UNLESS FORCED IF DONE VIA A DISCONNECT!

        // Before setting the display cache to null check if it is disposable, and if so, dispose it.
        if (DisplayCache is IDisposable disp)
            disp.Dispose();
        // Clear it.
        DisplayCache = null;
    }

    // Opens, or toggles, or swaps current data for interactions.
    public void ForInteractions(Kinkster kinkster, bool forceOpen = false)
    {
        // If the mode is already interactions.
        if (DisplayCache is KinksterInfoCache pairCache)
        {
            // If the kinkster is the same, toggle off.
            if (pairCache.Kinkster == kinkster)
            {
                Logger.LogInformation($"Toggling Side Panel Interactions for {kinkster.GetNickAliasOrUid()}");
                // If we are forcing it open, do nothing.
                if (forceOpen)
                    return;
                // Otherwise clear the data.
                ClearDisplay();
            }
            // Update the displayed data to show the new kinkster.
            else
            {
                Logger.LogInformation($"Switching Side Panel Interactions to {kinkster.GetNickAliasOrUid()}");
                pairCache.UpdateKinkster(kinkster);
            }
        }
        // Was displaying something else, so make sure we update and open.
        else
        {
            Logger.LogInformation($"Opening Side Panel Interactions for {kinkster.GetNickAliasOrUid()}");
            DisplayCache = new KinksterInfoCache(Logger, _hub, kinkster, _hypnoManager, _guides);
        }
    }

    private void CloseInteractions()
    {
        // Change this later!
        ClearDisplay();
    }
}
