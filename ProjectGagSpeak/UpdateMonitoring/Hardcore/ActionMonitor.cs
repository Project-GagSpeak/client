using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Hardcore;
using GagSpeak.Hardcore.Hotbar;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.Utils;
using GagSpeak.Utils.Enums;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using System.Collections.Immutable;
using ClientStructFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace GagSpeak.UpdateMonitoring;
public class ActionMonitor : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly HardcoreHandler _hardcoreHandler;
    private readonly WardrobeHandler _wardrobeHandler;
    private readonly ClientMonitorService _clientService;

    // attempt to get the rapture hotbar module so we can modify the display of hotbar items
    public unsafe RaptureHotbarModule* raptureHotbarModule = ClientStructFramework.Instance()->GetUIModule()->GetRaptureHotbarModule();

    // if sigs fuck up, reference https://github.com/PunishXIV/Orbwalker/blob/f850e04eb9371aa5d7f881e3024d7f5d0953820a/Orbwalker/Memory.cs#L15
    internal unsafe delegate bool UseActionDelegate(ActionManager* am, ActionType type, uint acId, long target, uint a5, uint a6, uint a7, void* a8);
    internal Hook<UseActionDelegate> UseActionHook;

    public Dictionary<uint, AcReqProps[]> CurrentJobBannedActions = new Dictionary<uint, AcReqProps[]>(); // stores the current job actions
    public Dictionary<int, Tuple<float, DateTime>> CooldownList = new Dictionary<int, Tuple<float, DateTime>>(); // stores the recast timers for each action

    public unsafe ActionMonitor(ILogger<ActionMonitor> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, HardcoreHandler handler,
        WardrobeHandler wardrobeHandler, ClientMonitorService clientService,
        IGameInteropProvider interop) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _hardcoreHandler = handler;
        _wardrobeHandler = wardrobeHandler;
        _clientService = clientService;

        // set up a hook to fire every time the address signature is detected in our game.
        UseActionHook = interop.HookFromAddress<UseActionDelegate>((nint)ActionManager.MemberFunctionPointers.UseAction, UseActionDetour);
        UseActionHook.Enable();

        Mediator.Subscribe<SafewordHardcoreUsedMessage>(this, _ => SafewordUsed());
        Mediator.Subscribe<DalamudLoginMessage>(this, _ =>
        {
            _clientService.RunOnFrameworkThread(() => UpdateJobList(_clientService.ClientPlayer.ClassJobId())).ConfigureAwait(false);
        });
        Mediator.Subscribe<DalamudLogoutMessage>(this, _ => DisableManipulatedActionData());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => FrameworkUpdate());
        Mediator.Subscribe<JobChangeMessage>(this, (msg) => JobChanged(msg.jobId));
        IpcFastUpdates.HardcoreTraitsEventFired += ToggleHardcoreTraits;

        // if we are already logged in, then run the login function
        if (_clientService.IsLoggedIn)
        {
            _clientService.RunOnFrameworkThread(() => UpdateJobList(_clientService.ClientPlayer.ClassJobId())).ConfigureAwait(false);
        }
    }

    public static bool MonitorHardcoreRestraintSetTraits = false;

    private void DisableManipulatedActionData()
    {
        Logger.LogInformation("Disabling Manipulated Action Data", LoggerType.HardcoreActions);
        _hardcoreHandler.StimulationMultiplier = 1.0f;
        RestoreSavedSlots();
        HotbarLocker.SetHotbarLockState(NewState.Unlocked);
        // Halt monitoring of properties
        MonitorHardcoreRestraintSetTraits = false;
    }

    private async void EnableManipulatedActionData(RestraintSet set, string enabler)
    {
        _hardcoreHandler.ApplyMultiplier();
        // recalculate the cooldowns for the current job if using stimulation
        if (set.SetTraits[enabler].StimulationLevel is not StimulationLevel.None)
        {
            await _clientService.RunOnFrameworkThread(() => UpdateJobList(_clientService.ClientPlayer.ClassJobId()));
        }

        HotbarLocker.SetHotbarLockState(NewState.Locked);
        // Begin monitoring hardcore restraint properties.
        MonitorHardcoreRestraintSetTraits = true;
    }

    protected override void Dispose(bool disposing)
    {
        DisableManipulatedActionData();
        // dispose of the hook
        UseActionHook?.Disable();
        UseActionHook?.Dispose();
        UseActionHook = null!;

        IpcFastUpdates.HardcoreTraitsEventFired -= ToggleHardcoreTraits;
        // dispose of the base class
        base.Dispose(disposing);
    }

    public async void SafewordUsed()
    {
        // Wait 3 seconds to let everything else from the safeword process first.
        Logger.LogDebug("Safeword has been used, re-enabling actions in 3 seconds");
        await Task.Delay(3000);
        // set lock to visable again
        HotbarLocker.SetHotbarLockState(NewState.Unlocked);
        MonitorHardcoreRestraintSetTraits = false;
        // restore saved slots
        RestoreSavedSlots();
    }

    public void ToggleHardcoreTraits(NewState newState, RestraintSet restraintSetRef)
    {
        if (restraintSetRef.EnabledBy != MainHub.UID && newState is NewState.Enabled)
        {
            Logger.LogWarning(restraintSetRef.EnabledBy + " has enabled hardcore traits");
            EnableManipulatedActionData(restraintSetRef, restraintSetRef.EnabledBy);
        }
        if (restraintSetRef.EnabledBy != MainHub.UID && newState is NewState.Disabled)
        {
            Logger.LogWarning(restraintSetRef.EnabledBy + " has disabled hardcore traits");
            DisableManipulatedActionData();
        }
    }

    public unsafe void RestoreSavedSlots()
    {
        if (raptureHotbarModule is null)
            return;

        Logger.LogDebug("Restoring saved slots", LoggerType.HardcoreActions);
        var baseSpan = raptureHotbarModule->StandardHotbars; // the length of our hotbar count
        for (var i = 0; i < baseSpan.Length; i++)
        {
            var hotbarRow = baseSpan.GetPointer(i);
            // if the hotbar is not null, we can get the slots data
            if (hotbarRow is not null)
                raptureHotbarModule->LoadSavedHotbar(_clientService.ClientPlayer.ClassJobId(), (uint)i);
        }
    }

    // fired on framework tick while a set is active
    private unsafe void UpdateSlots(HardcoreTraits setProperties)
    {
        var hotbarSpan = raptureHotbarModule->StandardHotbars; // the length of our hotbar count
        for (var i = 0; i < hotbarSpan.Length; i++)
        {
            var hotbarRow = hotbarSpan.GetPointer(i);
            if (hotbarSpan == null) continue;

            // get the slots data...
            for (var j = 0; j < 16; j++)
            {
                var slot = hotbarRow->Slots.GetPointer(j);
                if (slot == null) break;
                // if the slot is not empty, get the command id
                var isAction = slot->CommandType == RaptureHotbarModule.HotbarSlotType.Action ||
                               slot->CommandType == RaptureHotbarModule.HotbarSlotType.GeneralAction;
                // if it is a valid action, scan to see if the commandID is equal to any of our banned actions
                if (isAction && CurrentJobBannedActions.TryGetValue(slot->CommandId, out var props))
                {
                    // see if any of the indexes in the array contain a AcReqPros
                    if (setProperties.Gagged && props.Contains(AcReqProps.Speech))
                    {
                        // speech should be restrained, so remove any actions requiring speech
                        slot->Set(raptureHotbarModule->UIModule, RaptureHotbarModule.HotbarSlotType.Action, 2886);
                        continue;
                    }
                    if (setProperties.Blindfolded && props.Contains(AcReqProps.Sight))
                    {
                        // sight should be restrained, so remove any actions requireing sight
                        slot->Set(raptureHotbarModule->UIModule, RaptureHotbarModule.HotbarSlotType.Action, 99);
                        continue;
                    }
                    if (setProperties.Weighty && props.Contains(AcReqProps.Weighted))
                    {
                        // weighted should be restrained, so remove any actions requireing weight
                        slot->Set(raptureHotbarModule->UIModule, RaptureHotbarModule.HotbarSlotType.Action, 151);
                        continue;
                    }
                    if (setProperties.Immobile && props.Contains(AcReqProps.Movement))
                    {
                        // immobile should be restrained, so remove any actions requireing movement
                        slot->Set(raptureHotbarModule->UIModule, RaptureHotbarModule.HotbarSlotType.Action, 2883);
                        continue;
                    }
                    if (setProperties.LegsRestrained && props.Contains(AcReqProps.LegMovement))
                    {
                        // legs should be restrained, so remove any actions requireing leg movement
                        slot->Set(raptureHotbarModule->UIModule, RaptureHotbarModule.HotbarSlotType.Action, 55);
                        continue;
                    }
                    if (setProperties.ArmsRestrained && props.Contains(AcReqProps.ArmMovement))
                    {
                        // arms should be restrained, so remove any actions requireing arm movement
                        slot->Set(raptureHotbarModule->UIModule, RaptureHotbarModule.HotbarSlotType.Action, 68);
                        continue;
                    }
                }
            }
        }
    }

    // this will be called by the job changed event. When it does,
    // we will update our job list with the new job.
    // This updates the job list dictionary.
    private Task UpdateJobList(uint jobId)
    {
        // change the getawaiter if running into issues here.
        if (_clientService.IsPresent)
        {
            Logger.LogDebug("Updating job list to : " + (JobType)jobId, LoggerType.HardcoreActions);
            GagspeakActionData.GetJobActionProperties((JobType)jobId, out var bannedJobActions);
            CurrentJobBannedActions = bannedJobActions; // updated our job list
            // only do this if we are logged in
            unsafe
            {
                if (raptureHotbarModule->StandardHotbars != null)
                    GenerateCooldowns();
            }
        }
        return Task.CompletedTask;
    }

    private unsafe void GenerateCooldowns()
    {
        // if our current dictionary is not empty, empty it
        if (CooldownList.Count > 0)
        {
            Logger.LogTrace("Emptying previous class cooldowns", LoggerType.HardcoreActions);
            CooldownList.Clear();
        }
        Logger.LogTrace("Generating new class cooldowns", LoggerType.HardcoreActions);
        // get the current job actions
        var baseSpan = raptureHotbarModule->StandardHotbars; // the length of our hotbar count
        for (var i = 0; i < baseSpan.Length; i++)
        {
            // get our hotbar row
            var hotbar = baseSpan.GetPointer(i);
            // if the hotbar is not null, we can get the slots data
            if (hotbar is null)
                continue;

            // get the slots data...
            for (var j = 0; j < 16; j++)
            {
                var slot = hotbar->Slots.GetPointer(j);
                if (slot is null) break;

                if (slot->CommandType == RaptureHotbarModule.HotbarSlotType.Action)
                {
                    // we will want to add the tuple for each slot, the tuple should contain the cooldown group
                    var adjustedId = ActionManager.Instance()->GetAdjustedActionId(slot->CommandId);
                    // get the cooldown group
                    var cooldownGroup = -1;
                    if (_clientService.TryGetAction(adjustedId, out Lumina.Excel.Sheets.Action action))
                    {
                        // there is a minus one offset for actions, while general actions do not have them.
                        cooldownGroup = action.CooldownGroup - 1;
                        // get recast time
                        var recastTime = ActionManager.GetAdjustedRecastTime(ActionType.Action, adjustedId);
                        recastTime = (int)(recastTime * _hardcoreHandler.StimulationMultiplier);
                        // if it is an action or general action, append it
                        //Logger.LogTrace($" SlotID {slot->CommandId} Cooldown group {cooldownGroup} with recast time {recastTime}", LoggerType.HardcoreActions);
                        if (!CooldownList.ContainsKey(cooldownGroup))
                        {
                            CooldownList.Add(cooldownGroup, new Tuple<float, DateTime>(recastTime, DateTime.MinValue));
                        }
                    }
                }
            }
        }
    }

    private void JobChanged(uint jobId)
    {
        UpdateJobList(jobId);
        if (MonitorHardcoreRestraintSetTraits)
        {
            RestoreSavedSlots();
        }
    }

    #region Framework Updates
    private DateTime LastKeybindSafewordUsed = DateTime.MinValue;

    private unsafe void FrameworkUpdate()
    {
        // make sure we only do checks when we are properly logged in and have a character loaded
        if (!_clientService.IsPresent)
            return;

        // Setup a hotkey for safeword keybinding to trigger a hardcore safeword message.
        if (DateTime.UtcNow - LastKeybindSafewordUsed > TimeSpan.FromSeconds(10))
        {
            // Check for hardcore Safeword Keybind
            if (KeyMonitor.CtrlPressed() && KeyMonitor.AltPressed() && KeyMonitor.BackPressed())
            {
                // Safeword keybind is pressed
                Logger.LogDebug("Safeword keybind CTRL+ALT+BACKSPACE has been pressed, firing HardcoreSafeword", LoggerType.HardcoreActions);
                LastKeybindSafewordUsed = DateTime.UtcNow;
                Mediator.Publish(new SafewordHardcoreUsedMessage());
            }
        }

        // Block out Chat Input if we should be.
        if (_hardcoreHandler.IsBlockingChatInput)
            ChatLogAddonHelper.DiscardCursorNodeWhenFocused();



        // This seems redundant? Since we recalculate on job change and also lock hotbar and ability to move slots around? But idk.
        if (MonitorHardcoreRestraintSetTraits)
        {
            // Probably just remove it if we need to.
            var activeSet = _clientConfigs.GetActiveSet();
            if (activeSet is not null && activeSet.PropertiesEnabledForUser(activeSet.EnabledBy))
                UpdateSlots(activeSet.SetTraits[activeSet.EnabledBy]);
        }
    }

    #endregion Framework Updates
    private unsafe bool UseActionDetour(ActionManager* am, ActionType type, uint acId, long target, uint a5, uint a6, uint a7, void* a8)
    {
        try
        {
            // If we are forced to sit, decline any actions sent.
            if (_hardcoreHandler.MonitorEmoteLogic)
                return false;
            // If someone is forcing us to stay, we should block access to teleports and other methods of death.
            if (_hardcoreHandler.MonitorStayLogic)
            {
                // check if we are trying to hit teleport or return from hotbars /  menus
                if (type is ActionType.GeneralAction && acId is 7 or 8)
                {
                    Logger.LogTrace("You are currently locked away, canceling teleport/return execution", LoggerType.HardcoreActions);
                    return false;
                }
                // if we somehow managed to start executing it, then stop that too
                if (type is ActionType.Action && acId is 5 or 6 or 11408)
                {
                    Logger.LogTrace("You are currently locked away, canceling teleport/return execution", LoggerType.HardcoreActions);
                    return false;
                }
            }

            //Logger.LogTrace($" UseActionDetour called {acId} {type}");
            if (MonitorHardcoreRestraintSetTraits)
            {
                // Shortcut to avoid fetching active set for stimulation level every action.
                if (_hardcoreHandler.StimulationMultiplier is not 1.0)
                {
                    // then let's check our action ID's to apply the modified cooldown timers
                    if (type is ActionType.Action && acId > 7)
                    {
                        var recastTime = ActionManager.GetAdjustedRecastTime(type, acId);
                        var adjustedId = am->GetAdjustedActionId(acId);
                        var recastGroup = am->GetRecastGroup((int)type, adjustedId);
                        if (CooldownList.ContainsKey(recastGroup))
                        {
                            //Logger.LogDebug($" GROUP FOUND - Recast Time: {recastTime} | Cast Group: {recastGroup}");
                            var cooldownData = CooldownList[recastGroup];
                            // if we are beyond our recast time from the last time used, allow the execution
                            if (DateTime.Now >= cooldownData.Item2.AddMilliseconds(cooldownData.Item1))
                            {
                                // Update the last execution time before execution
                                //Logger.LogTrace("ACTION COOLDOWN FINISHED", LoggerType.HardcoreActions);
                                CooldownList[recastGroup] = new Tuple<float, DateTime>(cooldownData.Item1, DateTime.Now);
                            }
                            else
                            {
                                //Logger.LogTrace("ACTION COOLDOWN NOT FINISHED", LoggerType.HardcoreActions);
                                return false; // Do not execute the action
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
        }
        // return the original if we reach here
        var ret = UseActionHook.Original(am, type, acId, target, a5, a6, a7, a8);
        // invoke the action used event
        return ret;
    }

}
