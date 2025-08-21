using CkCommons;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.CustomCombos.Moodles;
using GagSpeak.CustomCombos.Padlock;
using GagSpeak.CustomCombos.Pairs;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using static CkCommons.GameDataHelp;


namespace GagSpeak.Services;

/// <summary>
///     Service for holding cached information about the active Kinkster's Interactions UI. <para />
///     Also holds any information that other UI Elements should know about and need to use for communication.
/// </summary>
public sealed class InteractionsService : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly MainMenuTabs _mainMenu;

    private HypnoEffectEditor _hypnosisEditor;

    public InteractionsService(ILogger<InteractionsService> logger, GagspeakMediator mediator,
        MainHub hub, MainMenuTabs mainMenu, HypnoEffectManager effectManager)
        : base(logger, mediator)
    {
        _hub = hub;
        _mainMenu = mainMenu;

        _hypnosisEditor = new HypnoEffectEditor("KinksterEffectEditor", effectManager);

        Mediator.Subscribe<KinksterInteractionUiChangeMessage>(this, msg => NewKinksterOrTab(msg.Kinkster, msg.Type));
        Mediator.Subscribe<PairWasRemovedMessage>(this, _ => CloseInteractionsUI());
        Mediator.Subscribe<ClosedMainUiMessage>(this, _ => CloseInteractionsUI());
        Mediator.Subscribe<MainWindowTabChangeMessage>(this, _ => CloseIfNotWhitelist(_.NewTab));
    }

    public PairGagCombo Gags { get; private set; } = null!;
    public PairGagPadlockCombo GagLocks { get; private set; } = null!;
    public PairRestrictionCombo Restrictions { get; private set; } = null!;
    public PairRestrictionPadlockCombo RestrictionLocks { get; private set; } = null!;
    public PairRestraintCombo Restraints { get; private set; } = null!;
    public PairRestraintPadlockCombo RestraintLocks { get; private set; } = null!;
    public PairMoodleStatusCombo Statuses { get; private set; } = null!;
    public PairMoodlePresetCombo Presets { get; private set; } = null!;
    public PairPatternCombo Patterns { get; private set; } = null!;
    public PairAlarmCombo Alarms { get; private set; } = null!;
    public PairTriggerCombo Triggers { get; private set; } = null!;
    public OwnMoodleStatusToPairCombo OwnStatuses { get; private set; } = null!;
    public OwnMoodlePresetToPairCombo OwnPresets { get; private set; } = null!;
    public EmoteCombo Emotes { get; private set; } = null!;
    public PairMoodleStatusCombo ActiveStatuses { get; private set; } = null!;

    public Kinkster? Kinkster { get; private set; } = null;
    public InteractionsTab CurrentTab { get; private set; } = InteractionsTab.None;
    public InteractionType OpenItem { get; private set; } = InteractionType.None;


    // Variable Inits that are freely changable.
    public string DispName = "Anon. Kinkster";

    public int GagLayer = 0;
    public int RestrictionLayer = 0;
    public int RestraintLayer = 0;
    
    public string HypnoTimer = string.Empty;

    public string EmoteTimer = string.Empty;
    public uint EmoteId = 0;
    public int CyclePose = 0;
    
    public string ConfinementTimer = string.Empty;
    public AddressBookEntry ConfinementLoc = new();

    public string ImprisonTimer = string.Empty;
    public Vector3 ImprisonPos = Vector3.Zero;
    public float ImprisonRadius = 0f;

    public string ChatBoxHideTimer = string.Empty;
    public string ChatInputHideTimer = string.Empty;
    public string ChatInputBlockTimer = string.Empty;

    public int ApplyIntensity = 0;
    public int ApplyVibeIntensity = 0;
    public float ApplyDuration = 0;
    public float ApplyVibeDur = 0;
    public float TmpVibeDur = -1;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _hypnosisEditor.Dispose();
    }

    private void CloseIfNotWhitelist(MainMenuTabs.SelectedTab newTab)
    {
        if (newTab != MainMenuTabs.SelectedTab.Whitelist)
        {
            Logger.LogTrace("Closing Interactions UI because we are not on the Whitelist tab.", LoggerType.StickyUI);
            CloseInteractionsUI();
        }
    }

    private void CloseInteractionsUI() 
        => Mediator.Publish(new UiToggleMessage(typeof(KinksterInteractionsUI), ToggleType.Hide));

    private void ResetModifiableVariables()
    {
        GagLayer = 0;
        RestrictionLayer = 0;
        RestraintLayer = 0;
        HypnoTimer = string.Empty;
        EmoteId = 0;
        CyclePose = 0;
        ConfinementLoc = new AddressBookEntry();
        ImprisonPos = Vector3.Zero;
        ImprisonRadius = 0f;
        ApplyIntensity = 0;
        ApplyVibeIntensity = 0;
        ApplyDuration = 0.1f;
        ApplyVibeDur = 0.1f;
        TmpVibeDur = 0.1f;
    }

    /// <summary> Updates the service info the latest Kinkster / Tab change. </summary>
    /// <returns> True if the window should be opened, false if it should be closed. </returns>
    private void NewKinksterOrTab(Kinkster kinkster, InteractionsTab tab)
    {
        // if the oped tab is the same, and the kinkster is the same, perform a silent cleanup.
        if (Kinkster == kinkster && CurrentTab == tab)
        {
            Logger.LogDebug($"Silently cleaning up interactions for {kinkster.GetNickAliasOrUid()} on tab {tab}", LoggerType.StickyUI);
            OpenItem = InteractionType.None;
            _hypnosisEditor.OnEditorClose();
            Mediator.Publish(new UiToggleMessage(typeof(KinksterInteractionsUI)));
            return;
        }
        else if (CurrentTab == tab && Kinkster != null && kinkster != Kinkster)
        {
            Logger.LogDebug($"Silently cleaning up interactions, but keeping window open!");
            OpenItem = InteractionType.None;
            _hypnosisEditor.OnEditorClose();
        }

        // Log we are changing.
        Logger.LogInformation($"Updating Sticky UI for {kinkster.GetNickAliasOrUid()} on tab {tab}", LoggerType.StickyUI);
        // if the result of this is not 0 it means the kinkster changed and we should update our data.
        if (kinkster.CompareTo(Kinkster) != 0 || CurrentTab is InteractionsTab.None)
            SyncDataForKinkster(kinkster);

        // forcibly put us in the whitelist tab.
        Mediator.Publish(new UiToggleMessage(typeof(MainUI), ToggleType.Show));
        _mainMenu.TabSelection = MainMenuTabs.SelectedTab.Whitelist;
        CurrentTab = tab;
        Mediator.Publish(new UiToggleMessage(typeof(KinksterInteractionsUI), ToggleType.Show));

    }

    private void SyncDataForKinkster(Kinkster kinkster)
    {
        Kinkster = kinkster;

        Gags = new PairGagCombo(Logger, _hub, Kinkster, CloseInteraction);
        GagLocks = new PairGagPadlockCombo(Logger, _hub, Kinkster, CloseInteraction);
        Restrictions = new PairRestrictionCombo(Logger, _hub, Kinkster, CloseInteraction);
        RestrictionLocks = new PairRestrictionPadlockCombo(Logger, _hub, Kinkster, CloseInteraction);
        Restraints = new PairRestraintCombo(Logger, _hub, Kinkster, CloseInteraction);
        RestraintLocks = new PairRestraintPadlockCombo(Logger, _hub, Kinkster, CloseInteraction);
        Statuses = new PairMoodleStatusCombo(Logger, _hub, Kinkster, 1.3f);
        Presets = new PairMoodlePresetCombo(Logger, _hub, Kinkster, 1.3f);
        Patterns = new PairPatternCombo(Logger, _hub, Kinkster, CloseInteraction);
        Alarms = new PairAlarmCombo(Logger, _hub, Kinkster);
        Triggers = new PairTriggerCombo(Logger, _hub, Kinkster);
        OwnStatuses = new OwnMoodleStatusToPairCombo(Logger, _hub, Kinkster, 1.3f);
        OwnPresets = new OwnMoodlePresetToPairCombo(Logger, _hub, Kinkster, 1.3f);
        ActiveStatuses = new PairMoodleStatusCombo(Logger, _hub, Kinkster, 1.3f, 
            () => [ .. Kinkster.LastIpcData.DataInfo.Values.OrderBy(x => x.Title) ]);

        Emotes = new EmoteCombo(Logger, 1.3f, () => [
            ..Kinkster.PairPerms.AllowLockedEmoting ? EmoteExtensions.LoopedEmotes() : EmoteExtensions.SittingEmotes()
        ]);

        DispName = kinkster.GetNickAliasOrUid();
        ResetModifiableVariables();
    }

    public void UpdateDispName()
    {
        DispName = Kinkster?.GetNickAliasOrUid() ?? "Anon. Kinkster";
    }

    public void CloseInteraction()
        => OpenItem = InteractionType.None;

    public void ToggleInteraction(InteractionType type)
        => OpenItem = (type == OpenItem) ? InteractionType.None : type;

    // Hypnosis is a special case.
    public void ToggleHypnosisView()
    {
        if (OpenItem is not InteractionType.HypnosisEffect)
        {
            ToggleInteraction(InteractionType.HypnosisEffect);
            if (_hypnosisEditor.IsEffectNull)
                _hypnosisEditor.SetBlankEffect();
        }
        else
        {
            CloseInteraction();
        }
    }

    public void DrawHypnosisEditor(float width)
    {
        _hypnosisEditor.DrawCompactEditorTabs(width);
        ImGui.Dummy(_hypnosisEditor.DisplayPreviewWidthConstrained(width, Constants.DefaultHypnoPath));
    }

    public void TrySendHypnosisAction()
    {
        if (!_hypnosisEditor.TryGetEffect(out var effect))
        {
            Logger.LogTrace("Effect was null or time parsing failed!");
            return;
        }
        if (!PadlockEx.TryParseTimeSpan(HypnoTimer, out var newTime))
        {
            HypnoTimer = string.Empty;
            Logger.LogTrace("Effect was null or time parsing failed!");
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
                    default: Svc.Logger.Debug($"Failed to send Hypnosis Effect to {DispName}: {res.ErrorCode}."); break;
                }
            }
            else
            {
                Logger.LogDebug($"Sent Hypnosis Effect to {DispName} with duration: {newTime} (seconds)", LoggerType.StickyUI);
            }
        });
    }

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
                ConfinedWorld = ConfinementLoc.World,
                ConfinedCity = (int)ConfinementLoc.City,
                ConfinedWard = ConfinementLoc.Ward,
                ConfinedPlaceId = ConfinementLoc.PropertyType is PropertyType.House ? ConfinementLoc.Plot : ConfinementLoc.Apartment,
                ConfinedInApartment = ConfinementLoc.PropertyType is PropertyType.Apartment,
                ConfinedInSubdivision = ConfinementLoc.ApartmentSubdivision
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
            var dto = new HardcoreStateChange(Kinkster.UserData, newHcData, attribute, MainHub.PlayerUserData);
            if (await _hub.UserChangeOtherHardcoreState(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            {
                switch (res.ErrorCode)
                {
                    case GagSpeakApiEc.BadUpdateKind: Svc.Toasts.ShowError("Invalid Update Kind. Please try again."); break;
                    case GagSpeakApiEc.InvalidDataState: Svc.Toasts.ShowError("Tried to switch to Invalid Data State!"); break;
                    case GagSpeakApiEc.InvalidTime: Svc.Toasts.ShowError("Invalid Timer Syntax. Must be a valid time format (Ex: 1h2m7s)."); break;
                    case GagSpeakApiEc.LackingPermissions: Svc.Toasts.ShowError("You do not have permission to perform this action."); break;
                    default: Svc.Logger.Debug($"Failed to send HardcoreStateChange to {DispName}: {res.ErrorCode}."); break;
                }
            }
            else
            {
                Logger.LogDebug($"Changed {DispName}'s Hardcore State ({attribute}) to enabled [For {(expireTimer - DateTimeOffset.UtcNow)}]", LoggerType.HardcoreActions);
                CloseInteraction();
            }
        });
    }

    public void TryDisableHardcoreAction(HcAttribute attribute)
    {
        UiService.SetUITask(async () =>
        {
            var dto = new HardcoreStateChange(Kinkster!.UserData, new HardcoreState(), attribute, MainHub.PlayerUserData);
            if (await _hub.UserChangeOtherHardcoreState(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            {
                switch (res.ErrorCode)
                {
                    case GagSpeakApiEc.BadUpdateKind: Svc.Toasts.ShowError("Invalid Update Kind. Please try again."); break;
                    case GagSpeakApiEc.InvalidDataState: Svc.Toasts.ShowError("Tried to switch to Invalid Data State!"); break;
                    case GagSpeakApiEc.InvalidTime: Svc.Toasts.ShowError("Invalid Timer Syntax. Must be a valid time format (Ex: 1h2m7s)."); break;
                    case GagSpeakApiEc.LackingPermissions: Svc.Toasts.ShowError("You do not have permission to perform this action."); break;
                    default: Svc.Logger.Debug($"Failed to send HardcoreStateChange to {DispName}: {res.ErrorCode}."); break;
                }
            }
            else
            {
                Logger.LogDebug($"Changed {DispName}'s Hardcore State ({attribute}) to disabled", LoggerType.HardcoreActions);
                CloseInteraction();
            }
        });
    }
}
