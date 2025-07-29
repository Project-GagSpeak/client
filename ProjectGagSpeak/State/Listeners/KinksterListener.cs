using FFXIVClientStructs.FFXIV.Client.Game.UI;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Managers;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Network;

namespace GagSpeak.State.Listeners;

/// <summary>
///     Listens for all incoming updates to the kinkster's data, navigating to the pair to edit, and applying changes. <para />
///     This will additionally listen for spesific actions performed by other kinksters, intended for you. (ex. Shocks, Hypnosis)
/// </summary>
public sealed class KinksterListener
{
    private readonly ILogger<KinksterListener> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly OwnGlobals _ownGlobals;
    private readonly HardcoreHandler _hcHandler;
    private readonly KinksterManager _kinksters;
    public KinksterListener(ILogger<KinksterListener> logger, GagspeakMediator mediator,
        OwnGlobals ownGlobals, HardcoreHandler hcHandler, KinksterManager kinksters)
    {
        _logger = logger;
        _mediator = mediator;
        _ownGlobals = ownGlobals;
        _hcHandler = hcHandler;
        _kinksters = kinksters;
    }

    public void NewIpcData(UserData targetUser, UserData enactor, CharaIPCData newData)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        _logger.LogDebug($"Recieved Full IPC Data from {kinkster.GetNickAliasOrUid()}!", LoggerType.Callbacks);
        kinkster.UpdateActiveMoodles(enactor, newData.DataString, newData.DataInfoList);
        kinkster.SetNewMoodlesStatuses(enactor, newData.StatusList);
        kinkster.SetNewMoodlePresets(enactor, newData.PresetList);
    }
    public void NewIpcStatusManager(UserData targetUser, UserData enactor, string dataString, List<MoodlesStatusInfo> dataInfo)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        _logger.LogTrace($"{kinkster.GetNickAliasOrUid()}'s Moodle StatusManager updated!", LoggerType.Callbacks);
        kinkster.UpdateActiveMoodles(enactor, dataString, dataInfo);
    }
    public void NewIpcStatuses(UserData targetUser, UserData enactor, List<MoodlesStatusInfo> statuses)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        _logger.LogDebug($"Recieved IPC Data from {kinkster.GetNickAliasOrUid()}!", LoggerType.Callbacks);
        kinkster.SetNewMoodlesStatuses(enactor, statuses);
    }
    public void NewIpcPresets(UserData targetUser, UserData enactor, List<MoodlePresetInfo> newPresets)
    {
        if (!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        _logger.LogDebug($"Recieved IPC Data from {kinkster.GetNickAliasOrUid()}!", LoggerType.Callbacks);
        kinkster.SetNewMoodlePresets(enactor, newPresets);
    }


    public void NewActiveComposite(UserData targetUser, CharaCompositeActiveData data, bool safeword)
    {
        if(!_kinksters.TryGetKinkster(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        _logger.LogDebug($"Recieved Composite Active Data from {kinkster.GetNickAliasOrUid()}!", LoggerType.Callbacks);
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
}
