using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using GagSpeak.Hardcore;
using GagSpeak.Hardcore.Hotbar;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.Utils;
using GagSpeak.Utils.Enums;
using System.Collections.Immutable;
using ClientStructFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace GagSpeak.UpdateMonitoring;
public class ActionMonitor : DisposableMediatorSubscriberBase
{
    private readonly GlobalData _globals;
    private readonly TraitsManager _traits;
    private readonly ClientMonitor _monitor;

    // attempt to get the rapture hotbar module so we can modify the display of hotbar items
    public unsafe RaptureHotbarModule* raptureHotbarModule = ClientStructFramework.Instance()->GetUIModule()->GetRaptureHotbarModule();

    // if sigs fuck up, reference https://github.com/PunishXIV/Orbwalker/blob/f850e04eb9371aa5d7f881e3024d7f5d0953820a/Orbwalker/Memory.cs#L15
    internal unsafe delegate bool UseActionDelegate(ActionManager* am, ActionType type, uint acId, long target, uint a5, uint a6, uint a7, void* a8);
    internal Hook<UseActionDelegate> UseActionHook;

    public Dictionary<uint, AcReqProps[]> CurrentJobBannedActions = new Dictionary<uint, AcReqProps[]>(); // stores the current job actions
    public Dictionary<int, Tuple<float, DateTime>> CooldownList = new Dictionary<int, Tuple<float, DateTime>>(); // stores the recast timers for each action

    public unsafe ActionMonitor(ILogger<ActionMonitor> logger, GagspeakMediator mediator, GlobalData globals,
        TraitsManager traits, ClientMonitor monitor, IGameInteropProvider interop) : base(logger, mediator)
    {
        _globals = globals;
        _traits = traits;
        _monitor = monitor;

        // set up a hook to fire every time the address signature is detected in our game.
        UseActionHook = interop.HookFromAddress<UseActionDelegate>((nint)ActionManager.MemberFunctionPointers.UseAction, UseActionDetour);
        UseActionHook.Enable();

        Mediator.Subscribe<SafewordHardcoreUsedMessage>(this, _ => SafewordUsed());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => FrameworkUpdate());
        Mediator.Subscribe<JobChangeMessage>(this, (msg) => JobChanged(msg.jobId));

        traits.OnTraitStateChanged += ProcessTraitChange;
        traits.OnStimulationStateChanged += ProcessStimulationChange;
    }

    private void ProcessStimulationChange(Stimulation prevStim, Stimulation newStim)
    {
        // We must update the job list on the next framework tick because the multiplier changed.
        if (prevStim != newStim)
        {
            // Ensure this is run on the framework tick.
            _monitor.RunOnFrameworkThread(() => UpdateJobList(_monitor.ClientPlayer.ClassJobId())).ConfigureAwait(false);
        }
    }

    private void ProcessTraitChange(Traits prevTraits, Traits newTraits)
    {
        // If prevTraits.AnyHotbarModifier was different from newTraits.AnyHotbarModifier, we must update the slots and lock the hotbar.
        if ((prevTraits ^ newTraits) != 0)
        {
            if (newTraits is not 0)
            {
                Logger.LogInformation("Disabling Manipulated Action Data", LoggerType.HardcoreActions);
                HotbarLocker.SetHotbarLockState(NewState.Locked);
                UpdateSlots(newTraits);
            }
            else
            {
                Logger.LogInformation("Disabling Manipulated Action Data", LoggerType.HardcoreActions);
                HotbarLocker.SetHotbarLockState(NewState.Unlocked);
                RestoreSavedSlots();
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        UseActionHook?.Disable();
        UseActionHook?.Dispose();
        UseActionHook = null!;
        base.Dispose(disposing);
    }

    public async void SafewordUsed()
    {
        // Wait 3 seconds to let everything else from the safeword process first.
        Logger.LogDebug("Safeword has been used, re-enabling actions in 3 seconds");
        await Task.Delay(3000);
        // set lock to visable again
        HotbarLocker.SetHotbarLockState(NewState.Unlocked);
        RestoreSavedSlots();
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
                raptureHotbarModule->LoadSavedHotbar(_monitor.ClientPlayer.ClassJobId(), (uint)i);
        }
    }

    /// <summary> Updates the slots with the current traits. </summary>
    /// <remarks> Stimulation trait does not impact this. </remarks>
    private unsafe void UpdateSlots(Traits activeTraits)
    {
        // Define trait-to-action replacements once, outside the loops
        (Traits Trait, AcReqProps Required, uint ReplacementId)[] ReplacementActions =
        {
            (Traits.Gagged, AcReqProps.Speech, 2886),
            (Traits.Blindfolded, AcReqProps.Sight, 99),
            (Traits.Weighty, AcReqProps.Weighted, 151),
            (Traits.Immobile, AcReqProps.Movement, 2883),
            (Traits.LegsRestrained, AcReqProps.LegMovement, 55),
            (Traits.ArmsRestrained, AcReqProps.ArmMovement, 68),
        };

        // the length of our hotbar count
        var hotbarSpan = raptureHotbarModule->StandardHotbars;

        // Check all active hotbar spans.
        for (var i = 0; i < hotbarSpan.Length; i++)
        {
            // Get all slots for the row. (their pointers)
            var hotbarRow = hotbarSpan.GetPointer(i);
            if (hotbarSpan == Span<RaptureHotbarModule.Hotbar>.Empty)
                continue;

            // get the slots data...
            for (var j = 0; j < 16; j++)
            {
                // From the pointer, get the individual slot.
                var slot = hotbarRow->Slots.GetPointer(j);
                if (slot is null)
                    break;

                // If not a valid action type, ignore it.
                if (slot->CommandType != RaptureHotbarModule.HotbarSlotType.Action &&
                    slot->CommandType != RaptureHotbarModule.HotbarSlotType.GeneralAction)
                    continue;

                // if unable to find the properties for this item, ignore it.
                if (!CurrentJobBannedActions.TryGetValue(slot->CommandId, out var props))
                    continue;

                // Check each required trait and set its replacement action if the flag is active
                foreach (var (trait, required, replacementId) in ReplacementActions)
                {
                    if (activeTraits.HasFlag(trait) && props.Contains(required))
                    {
                        slot->Set(raptureHotbarModule->UIModule, RaptureHotbarModule.HotbarSlotType.Action, replacementId);
                        break; // Early exit once a match is found to increase efficiency.
                    }
                }
            }
        }
    }

    /// <summary> update our job list to the new job upon a job change. </summary>
    /// <param name="jobId"> the JobId provided by ClientMonitors OnJobChanged event. </param>
    /// <remarks> This updates the job list dictionary. (Generates new CD's based on stimulation lvl) </remarks>
    private Task UpdateJobList(uint jobId)
    {
        // change the getawaiter if running into issues here.
        if (_monitor.IsPresent)
        {
            Logger.LogDebug("Updating job list to : " + (JobType)jobId, LoggerType.HardcoreActions);
            GagspeakActionData.GetJobActionProperties((JobType)jobId, out var bannedJobActions);
            CurrentJobBannedActions = bannedJobActions; // updated our job list
            unsafe
            {
                if (raptureHotbarModule->StandardHotbars != Span<RaptureHotbarModule.Hotbar>.Empty)
                    GenerateCooldowns();
            }
        }
        return Task.CompletedTask;
    }

    private unsafe void GenerateCooldowns()
    {
        // if our current dictionary is not empty, empty it
        if (CooldownList.Count > 0)
            CooldownList.Clear();

        var vibeMultiplier = _traits.GetVibeMultiplier();

        Logger.LogTrace("Generating new class cooldowns", LoggerType.HardcoreActions);
        var baseSpan = raptureHotbarModule->StandardHotbars;
        for (var i = 0; i < baseSpan.Length; i++)
        {
            var hotbar = baseSpan.GetPointer(i);
            if (hotbar is null)
                continue;

            for (var slotIdx = 0; slotIdx < 16; slotIdx++)
            {
                var slot = hotbar->Slots.GetPointer(slotIdx);
                if (slot is null)
                    break;

                if (slot->CommandType != RaptureHotbarModule.HotbarSlotType.Action)
                    continue;

                var adjustedId = ActionManager.Instance()->GetAdjustedActionId(slot->CommandId);
                if (!_monitor.TryGetAction(adjustedId, out var action))
                    continue;

                // there is a minus one offset for actions, while general actions do not have them.
                var cooldownGroup = action.CooldownGroup - 1;
                var recastTime = ActionManager.GetAdjustedRecastTime(ActionType.Action, adjustedId);
                recastTime = (int)(recastTime * vibeMultiplier);

                // if it is an action or general action, append it
                //Logger.LogTrace($" SlotID {slot->CommandId} Cooldown group {cooldownGroup} with recast time {recastTime}", LoggerType.HardcoreActions);
                if (!CooldownList.ContainsKey(cooldownGroup))
                    CooldownList.Add(cooldownGroup, new Tuple<float, DateTime>(recastTime, DateTime.MinValue));
            }
        }
    }

    private void JobChanged(uint jobId)
    {
        UpdateJobList(jobId);

        // If we are still monitoring our hotbar state, restore the previous saved slots.
        // This allows us to recalculate new ones.
        if (_traits.ActiveTraits != 0)
            RestoreSavedSlots();
    }

    #region Framework Updates
    private unsafe void FrameworkUpdate()
    {
        // make sure we only do checks when we are properly logged in and have a character loaded
        if (!_monitor.IsPresent)
            return;

        // Setup a hotkey for safeword keybinding to trigger a hardcore safeword message.
        if (KeyMonitor.CtrlPressed() && KeyMonitor.AltPressed() && KeyMonitor.BackPressed())
        {
            // Safeword keybind is pressed
            Logger.LogWarning("Safeword keybind CTRL+ALT+BACKSPACE has been pressed, firing HardcoreSafeword", LoggerType.HardcoreActions);
            Mediator.Publish(new SafewordHardcoreUsedMessage());
        }

        // Block out Chat Input if we should be.
        if (_traits.ActiveHcState.HasAny(HardcoreState.ChatInputBlocked))
            ChatLogAddonHelper.DiscardCursorNodeWhenFocused();
    }

    #endregion Framework Updates
    private unsafe bool UseActionDetour(ActionManager* am, ActionType type, uint acId, long target, uint a5, uint a6, uint a7, void* a8)
    {
        try
        {
            // Prevent if Immobile or forced to Emote.
            if (_traits.IsImmobile)
                return false;

            // If ForcedStay, prevent teleports and methods of death.
            if (_traits.ActiveHcState.HasAny(HardcoreState.ForceStay))
            {
                // check if we are trying to hit teleport or return from hotbars /  menus
                if (type is ActionType.GeneralAction && acId is 7 or 8)
                    return false;
                // if we somehow managed to start executing it, then stop that too
                if (type is ActionType.Action && acId is 5 or 6 or 11408)
                    return false;
            }

            //Logger.LogTrace($" UseActionDetour called {acId} {type}");
            if (_traits.ActiveTraits != 0)
            {
                // Shortcut to avoid fetching active set for stimulation level every action.
                if (_traits.GetVibeMultiplier() != 1.0f)
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
                                // Logger.LogTrace("ACTION COOLDOWN FINISHED", LoggerType.HardcoreActions);
                                CooldownList[recastGroup] = new Tuple<float, DateTime>(cooldownData.Item1, DateTime.Now);
                            }
                            else
                            {
                                // Logger.LogTrace("ACTION COOLDOWN NOT FINISHED", LoggerType.HardcoreActions);
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
