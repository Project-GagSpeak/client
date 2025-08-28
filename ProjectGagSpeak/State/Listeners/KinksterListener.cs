using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;

namespace GagSpeak.State.Listeners;

/// <summary>
///     Listens for all incoming updates to the kinkster's data, navigating to the pair to edit, and applying changes. <para />
///     This will additionally listen for spesific actions performed by other kinksters, intended for you. (ex. Shocks, Hypnosis)
/// </summary>
public sealed class KinksterListener
{
    private readonly ILogger<KinksterListener> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly KinksterManager _kinksters;
    public KinksterListener(ILogger<KinksterListener> logger, GagspeakMediator mediator, KinksterManager kinksters)
    {
        _logger = logger;
        _mediator = mediator;
        _kinksters = kinksters;
    }

    #region DataUpdates
    public void NewAppearanceData(UserData target, CharaIpcDataFull newData)
    {
        if (!_kinksters.TryGetKinkster(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        _logger.LogTrace($"{kinkster.GetNickAliasOrUid()}'s Full Appearance changed!", LoggerType.Callbacks);
        kinkster.ApplyLatestAppearance(newData);
    }

    public void NewAppearanceData(UserData target, DataSyncKind type, string newDataString)
    {
        if (!_kinksters.TryGetKinkster(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        _logger.LogTrace($"{kinkster.GetNickAliasOrUid()}'s Appearance changed for {type}!", LoggerType.Callbacks);
        kinkster.ApplyLatestAppearance(type, newDataString);
    }

    public void NewMoodlesData(UserData targetUser, UserData enactor, CharaMoodleData newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        _logger.LogTrace($"{kinkster.GetNickAliasOrUid()}'s Moodle Data updated!", LoggerType.Callbacks);
        kinkster.ApplyLatestMoodles(enactor, newData.DataString, newData.DataInfoList);
        kinkster.SetNewMoodlesStatuses(enactor, newData.StatusList);
        kinkster.SetNewMoodlePresets(enactor, newData.PresetList);
    }
    public void NewStatusManager(UserData targetUser, UserData enactor, string dataString, List<MoodlesStatusInfo> dataInfo)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        _logger.LogTrace($"{kinkster.GetNickAliasOrUid()}'s Moodle StatusManager updated!", LoggerType.Callbacks);
        kinkster.ApplyLatestMoodles(enactor, dataString, dataInfo);
    }
    public void NewStatuses(UserData targetUser, UserData enactor, List<MoodlesStatusInfo> statuses)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        _logger.LogTrace($"{kinkster.GetNickAliasOrUid()}'s Moodle Statuses updated!", LoggerType.Callbacks);
        kinkster.SetNewMoodlesStatuses(enactor, statuses);
    }
    public void NewPresets(UserData targetUser, UserData enactor, List<MoodlePresetInfo> newPresets)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        _logger.LogTrace($"{kinkster.GetNickAliasOrUid()}'s Moodle Presets updated!", LoggerType.Callbacks);
        kinkster.SetNewMoodlePresets(enactor, newPresets);
    }

    public void NewActiveComposite(UserData targetUser, CharaCompositeActiveData data, bool safeword)
    {
        if(!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        _logger.LogDebug($"Received Composite Active Data from {kinkster.GetNickAliasOrUid()}!", LoggerType.Callbacks);
        kinkster.NewActiveCompositeData(data, safeword);
    }

    public void NewActiveGags(KinksterUpdateActiveGag dto)
    {
        if (!_kinksters.TryGetKinkster(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveGagData(dto);
    }

    public void NewActiveRestriction(KinksterUpdateActiveRestriction dto)
    {
        if (!_kinksters.TryGetKinkster(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveRestrictionData(dto);
    }

    public void NewActiveRestraint(KinksterUpdateActiveRestraint dto)
    {
        if (!_kinksters.TryGetKinkster(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveRestraintData(dto);
    }

    public void NewActiveCollar(KinksterUpdateActiveCollar dto)
    {
        if (!_kinksters.TryGetKinkster(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveCollarData(dto);
    }

    public void NewActiveCursedLoot(KinksterUpdateActiveCursedLoot dto)
    {
        if (!_kinksters.TryGetKinkster(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveCursedLoot(dto.ActiveItems, dto.ChangedItem);
    }

    public void NewAliasGlobal(UserData targetUser, Guid id, AliasTrigger? newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.NewGlobalAlias(id, newData);
    }

    public void NewAliasUnique(UserData targetUser, Guid id, AliasTrigger? newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.NewUniqueAlias(id, newData);
    }

    public void NewValidToys(UserData targetUser, List<ToyBrandName> validToys)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.NewValidToys(validToys);
    }

    public void NewActivePattern(KinksterUpdateActivePattern dto)
    {
        if (!_kinksters.TryGetKinkster(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActivePattern(dto.Enactor, dto.ActivePattern, dto.Type);
    }

    public void NewActiveAlarms(KinksterUpdateActiveAlarms dto)
    {
        if (!_kinksters.TryGetKinkster(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveAlarms(dto.Enactor, dto.ActiveAlarms, dto.Type);
    }

    public void NewActiveTriggers(KinksterUpdateActiveTriggers dto)
    {
        if (!_kinksters.TryGetKinkster(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveTriggers(dto.Enactor, dto.ActiveTriggers, dto.Type);
    }

    public void NewListenerName(UserData targetUser, string newName)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");

        _logger.LogDebug($"Updating Listener name to [{newName}]", LoggerType.PairDataTransfer);
        kinkster.LastPairAliasData.StoredNameWorld = newName;
    }

    public void CachedGagDataChange(UserData targetUser, GagType gagItem, LightGag? newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateGagItem(gagItem, newData);
    }

    public void CachedRestrictionDataChange(UserData targetUser, Guid itemId, LightRestriction? newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateRestrictionItem(itemId, newData);
    }

    public void CachedRestraintDataChange(UserData targetUser, Guid itemId, LightRestraint? newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateRestraintItem(itemId, newData);
    }

    public void CachedCollarDataChange(UserData targetUser, Guid itemId, LightCollar? newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateCollarItem(itemId, newData);
    }

    public void CachedCursedLootDataChange(UserData targetUser, Guid itemId, LightCursedLoot? newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateLootItem(itemId, newData);
    }

    public void CachedPatternDataChange(UserData targetUser, Guid itemId, LightPattern? newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdatePatternItem(itemId, newData);
    }

    public void CachedAlarmDataChange(UserData targetUser, Guid itemId, LightAlarm? newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateAlarmItem(itemId, newData);
    }

    public void CachedTriggerDataChange(UserData targetUser, Guid itemId, LightTrigger? newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateTriggerItem(itemId, newData);
    }

    public void CachedAllowancesChange(UserData targetUser, GagspeakModule module, List<string> newAllowances)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateAllowances(module, newAllowances);
    }
    #endregion DataUpdates

    #region Permissions
    public void PermBulkChangeGlobal(BulkChangeGlobal dto)
    {
        if (!_kinksters.TryGetKinkster(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        // cache prev state.
        var prevGlobals = kinkster.PairGlobals;

        // update them.
        kinkster.UserPair.Globals = dto.NewPerms;
        kinkster.UserPair.Hardcore = dto.NewState;

        _logger.LogDebug($"BulkChangeGlobal for [{kinkster.GetNickAliasOrUid()}]", LoggerType.PairDataTransfer);
        _kinksters.RecreateLazy(false);
        // use comparisons to fire various achievements related to global permissions.
    }

    public void PermChangeGlobal(UserData target, UserData enactor, string permName, object newValue)
    {
        if (!_kinksters.TryGetKinkster(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");

        if (!PropertyChanger.TrySetProperty(kinkster.PairGlobals, permName, newValue, out var finalVal) || finalVal is null)
            throw new InvalidOperationException($"Failed to set property '{permName}' on {kinkster.GetNickAliasOrUid()} with value '{newValue}'");

        _kinksters.RecreateLazy(false);
        _logger.LogDebug($"PermChangeGlobal for [{kinkster.GetNickAliasOrUid()}] set [{permName}] to [{finalVal}]", LoggerType.PairDataTransfer);
        // use comparisons to fire various achievements related to global permissions.
    }

    public void PermBulkChangeUniqueOwn(UserData target, PairPerms newPerms, PairPermAccess newAccess)
    {
        if (!_kinksters.TryGetKinkster(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        // cache prev state.
        var prevPerms = kinkster.OwnPerms;
        var prevAccess = kinkster.OwnPermAccess;

        // Update.
        kinkster.UserPair.OwnPerms = newPerms;
        kinkster.UserPair.OwnAccess = newAccess;

        _logger.LogDebug($"OWN BulkChangeUnique for [{kinkster.GetNickAliasOrUid()}]", LoggerType.PairDataTransfer);
        _kinksters.RecreateLazy(false);

        // Handle achievements with changes here.
    }

    public void PermBulkChangeUniqueOther(UserData target, PairPerms newPerms, PairPermAccess newAccess)
    {
        if (!_kinksters.TryGetKinkster(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        // cache prev state.
        var prevPerms = kinkster.PairPerms;
        var prevAccess = kinkster.PairPermAccess;

        // Update.
        kinkster.UserPair.Perms = newPerms;
        kinkster.UserPair.Access = newAccess;

        _logger.LogDebug($"OTHER BulkChangeUnique for [{kinkster.GetNickAliasOrUid()}]", LoggerType.PairDataTransfer);
        _kinksters.RecreateLazy(false);

        // Handle informing moodles of permission changes.
        var MoodlesChanged = (prevPerms.MoodlePerms != prevPerms.MoodlePerms) || (kinkster.PairPerms.MaxMoodleTime != kinkster.PairPerms.MaxMoodleTime);
        if (kinkster.IsVisible && MoodlesChanged)
            _mediator.Publish(new MoodlesPermissionsUpdated(kinkster));

        // Handle achievements with changes here.
    }

    public void PermChangeUnique(UserData target, UserData enactor, string permName, object newValue)
    {
        if (!_kinksters.TryGetKinkster(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        // cache prev state.
        var prevPuppetPerms = kinkster.OwnPerms.PuppetPerms;
        var prevPauseState = kinkster.OwnPerms.IsPaused;

        // Perform change.
        if (!PropertyChanger.TrySetProperty(kinkster.OwnPerms, permName, newValue, out var finalVal) || finalVal is null)
            throw new InvalidOperationException($"Failed to set property '{permName}' on {kinkster.GetNickAliasOrUid()} with value '{newValue}'");

        _kinksters.RecreateLazy(false);
        _logger.LogDebug($"OWN PermChangeUnique for [{kinkster.GetNickAliasOrUid()}] set [{permName}] to [{finalVal}]", LoggerType.PairDataTransfer);

        // Toggle pausing if pausing changed.
        if (permName.Equals(nameof(PairPerms.IsPaused)) && prevPauseState != (bool)finalVal)
            _mediator.Publish(new ClearProfileDataMessage(target));

        if (kinkster.IsVisible && permName.Equals(nameof(PairPerms.MoodlePerms)) || permName.Equals(nameof(PairPerms.MaxMoodleTime)))
            _mediator.Publish(new MoodlesPermissionsUpdated(kinkster));

        // Achievement if permissions were granted.
        if ((kinkster.OwnPerms.PuppetPerms & ~prevPuppetPerms) != 0)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerAccessGiven, (kinkster.OwnPerms.PuppetPerms & ~prevPuppetPerms));
    }

    public void PermChangeUniqueOther(UserData target, UserData enactor, string permName, object newValue)
    {
        if (!_kinksters.TryGetKinkster(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");

        var prevPauseState = kinkster.PairPerms.IsPaused;

        if (!PropertyChanger.TrySetProperty(kinkster.PairPerms, permName, newValue, out var finalVal) || finalVal is null)
            throw new InvalidOperationException($"Failed to set property '{permName}' on {kinkster.GetNickAliasOrUid()} with value '{newValue}'");

        _kinksters.RecreateLazy(false);
        _logger.LogDebug($"OTHER SingleChangeUnique for [{kinkster.GetNickAliasOrUid()}] set [{permName}] to [{finalVal}]", LoggerType.PairDataTransfer);
        // process distinct handles here.

        // Toggle pausing if pausing changed.
        if (permName.Equals(nameof(PairPerms.IsPaused)) && prevPauseState != (bool)finalVal)
            _mediator.Publish(new ClearProfileDataMessage(target));

        // If moodle permissions updated, notify IpcProvider (Moodles) that we have a change.
        if (permName.Equals(nameof(PairPerms.MoodlePerms)) || permName.Equals(nameof(PairPerms.MaxMoodleTime)))
            if (_kinksters.GetOnlineUserDatas().Contains(kinkster.UserData))
                _mediator.Publish(new MoodlesPermissionsUpdated(kinkster));
    }

    public void PermChangeAccess(UserData target, UserData enactor, string permName, object newValue)
    {
        if (!_kinksters.TryGetKinkster(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");

        // Perform change.
        if (!PropertyChanger.TrySetProperty(kinkster.OwnPermAccess, permName, newValue, out var finalVal) || finalVal is null)
            throw new InvalidOperationException($"Failed to set property '{permName}' on {kinkster.GetNickAliasOrUid()} with value '{newValue}'");

        _kinksters.RecreateLazy(false);
        _logger.LogDebug($"OWN PermChangeAccess for [{kinkster.GetNickAliasOrUid()}] set [{permName}] to [{finalVal}]", LoggerType.PairDataTransfer);
        // process distinct handles here.
    }

    public void PermChangeAccessOther(UserData target, UserData enactor, string permName, object newValue)
    {
        if (!_kinksters.TryGetKinkster(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");

        // Perform change.
        if (!PropertyChanger.TrySetProperty(kinkster.PairPermAccess, permName, newValue, out var finalVal) || finalVal is null)
            throw new InvalidOperationException($"Failed to set property '{permName}' on {kinkster.GetNickAliasOrUid()} with value '{newValue}'");

        _kinksters.RecreateLazy(false);
        _logger.LogDebug($"OTHER PermChangeAccess for [{kinkster.GetNickAliasOrUid()}] set [{permName}] to [{finalVal}]", LoggerType.PairDataTransfer);
        // process distinct handles here.
    }

    public void StateChangeHardcore(UserData target, UserData enactor, HcAttribute attribute, HardcoreState newData)
    {
        if (!_kinksters.TryGetKinkster(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        // cache prev state.
        var prevState = kinkster.PairHardcore;

        // make changes based on type.
        switch (attribute)
        {
            case HcAttribute.Follow:
                kinkster.PairHardcore.LockedFollowing = newData.LockedFollowing;
                break;

            case HcAttribute.EmoteState:
                kinkster.PairHardcore.LockedEmoteState = newData.LockedEmoteState;
                kinkster.PairHardcore.EmoteExpireTime = newData.EmoteExpireTime;
                kinkster.PairHardcore.EmoteId = newData.EmoteId;
                kinkster.PairHardcore.EmoteCyclePose = newData.EmoteCyclePose;
                break;

            case HcAttribute.Confinement:
                kinkster.PairHardcore.IndoorConfinement = newData.IndoorConfinement;
                kinkster.PairHardcore.ConfinementTimer = newData.ConfinementTimer;
                kinkster.PairHardcore.ConfinedWorld = newData.ConfinedWorld;
                kinkster.PairHardcore.ConfinedCity = newData.ConfinedCity;
                kinkster.PairHardcore.ConfinedWard = newData.ConfinedWard;
                kinkster.PairHardcore.ConfinedPlaceId = newData.ConfinedPlaceId;
                kinkster.PairHardcore.ConfinedInApartment = newData.ConfinedInApartment;
                kinkster.PairHardcore.ConfinedInSubdivision = newData.ConfinedInSubdivision;
                break;

            case HcAttribute.Imprisonment:
                kinkster.PairHardcore.Imprisonment = newData.Imprisonment;
                kinkster.PairHardcore.ImprisonmentTimer = newData.ImprisonmentTimer;
                kinkster.PairHardcore.ImprisonedTerritory = newData.ImprisonedTerritory;
                kinkster.PairHardcore.ImprisonedPos = newData.ImprisonedPos;
                kinkster.PairHardcore.ImprisonedRadius = newData.ImprisonedRadius;
                break;

            case HcAttribute.HiddenChatBox:
                kinkster.PairHardcore.ChatBoxesHidden = newData.ChatBoxesHidden;
                kinkster.PairHardcore.ChatBoxesHiddenTimer = newData.ChatBoxesHiddenTimer;
                break;

            case HcAttribute.HiddenChatInput:
                kinkster.PairHardcore.ChatInputHidden = newData.ChatInputHidden;
                kinkster.PairHardcore.ChatInputHiddenTimer = newData.ChatInputHiddenTimer;
                break;

            case HcAttribute.BlockedChatInput:
                kinkster.PairHardcore.ChatInputBlocked = newData.ChatInputBlocked;
                kinkster.PairHardcore.ChatInputBlockedTimer = newData.ChatInputBlockedTimer;
                break;

            case HcAttribute.HypnoticEffect:
                kinkster.PairHardcore.HypnoticEffect = newData.HypnoticEffect;
                kinkster.PairHardcore.HypnoticEffectTimer = newData.HypnoticEffectTimer;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Invalid Hardcore State attribute to change.");
        }
    }

    #endregion Permissions
}
