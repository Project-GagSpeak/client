using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Utils;

// References for Knowledge
// https://github.com/MgAl2O4/PatMeDalamud/blob/42385a92d1a9c3f043f35128ee68dc623cfc6a20/plugin/EmoteReaderHooks.cs#L21C87-L21C130

namespace GagSpeak.UpdateMonitoring.Triggers;
public class OnEmote : IDisposable
{
    private readonly ILogger<OnEmote> _logger;
    private readonly HardcoreHandler _hardcoreHandler;
    private readonly OnFrameworkService _frameworkUtils;
    private static class Signatures
    {
        // Will process rendered uses an emote. Can be us or anyone else. This is called after the emote has been executed.
        public const string OnEmoteDetour = "E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24";
    }

    // Processed only when the client requests to perform an emote. Performing an early return of this will result in it not being processed.
    internal Hook<AgentEmote.Delegates.ExecuteEmote> OnExecuteEmoteHook;

    public delegate void OnEmoteFuncDelegate(ulong unk, ulong emoteCallerAddr, ushort emoteId, ulong targetId, ulong unk2);
    internal static Hook<OnEmoteFuncDelegate> ProcessEmoteHook = null!;
    public OnEmote(ILogger<OnEmote> logger, HardcoreHandler hardcoreHandler,
        OnFrameworkService frameworkUtils, ISigScanner sigScanner, IGameInteropProvider interopProvider)
    {
        _logger = logger;
        _hardcoreHandler = hardcoreHandler;
        _frameworkUtils = frameworkUtils;
        interopProvider.InitializeFromAttributes(this);

        ProcessEmoteHook = interopProvider.HookFromSignature<OnEmoteFuncDelegate>(Signatures.OnEmoteDetour, OnEmoteDetour);
        unsafe
        {
            OnExecuteEmoteHook = interopProvider.HookFromAddress<AgentEmote.Delegates.ExecuteEmote>((nint)AgentEmote.MemberFunctionPointers.ExecuteEmote, OnExecuteEmote);
        }
        EnableHook();
        _logger.LogInformation("Started EmoteDetour");
    }

    public void EnableHook()
    {
        _logger.LogInformation("Enabling EmoteDetour");
        ProcessEmoteHook.Enable();
        OnExecuteEmoteHook.Enable();
    }

    public void DisableHook()
    {
        if (!ProcessEmoteHook.IsEnabled) return;
        _logger.LogInformation("Disabling EmoteDetour");
        ProcessEmoteHook.Disable();
        OnExecuteEmoteHook.Disable();
    }

    public void Dispose()
    {
        _logger.LogInformation("Stopping EmoteDetour");
        try
        {
            DisableHook();
            ProcessEmoteHook?.Dispose();
            OnExecuteEmoteHook?.Dispose();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error disposing of EmoteDetour");
        }
        _logger.LogInformation("Stopped EmoteDetour");
    }

    /// <summary>
    /// Processes an executed emote from any rendered player in game. Provides the source and target along with the emote ID.
    /// </summary>
    async void OnEmoteDetour(ulong unk, ulong emoteCallerAddr, ushort emoteId, ulong targetId, ulong unk2)
    {
        try
        {
            await _frameworkUtils.RunOnFrameworkThread(() =>
            {
                var emoteCaller = _frameworkUtils.CreateGameObject((nint)emoteCallerAddr);
                var emoteCallerName = (emoteCaller as IPlayerCharacter)?.GetNameWithWorld() ?? "No Player Was Emote Caller";
                var emoteName = EmoteMonitor.GetEmoteName(emoteId);
                var targetObj = (_frameworkUtils.SearchObjectTableById((uint)targetId));
                var targetName = (targetObj as IPlayerCharacter)?.GetNameWithWorld() ?? "No Player Was Target";
                _logger.LogTrace("OnEmote >> [" + emoteCallerName + "] used Emote [" + emoteName + "](ID:"+emoteId+") on Target: [" + targetName+"]", LoggerType.EmoteMonitor);

                UnlocksEventManager.AchievementEvent(UnlocksEvent.EmoteExecuted, emoteCaller, emoteId, targetObj);
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in EmoteDetour");
        }
        ProcessEmoteHook.Original(unk, emoteCallerAddr, emoteId, targetId, unk2);
    }


    public static (bool NextEmoteAllowed, ushort EmoteId) AllowExecution { get; set; } = (false, 0);

    /// <summary>
    /// Detours a emote request from our client player. If we decide to, we can perform an early return from this.
    /// </summary>
    unsafe void OnExecuteEmote(AgentEmote* thisPtr, ushort emoteId, EmoteController.PlayEmoteOption* playEmoteOption, bool addToHistory, bool liveUpdateHistory)
    {
        try
        {
            _logger.LogTrace("OnExecuteEmote >> Emote [" + EmoteMonitor.GetEmoteName(emoteId) + "](ID:"+emoteId+") requested to be Executed", LoggerType.EmoteMonitor);
            // Block all emotes if forced to follow
            if(_hardcoreHandler.IsForcedToFollow)
                return;

            // If we are forced to emote, then we should prevent execution unless NextEmoteAllowed is true.
            if (_hardcoreHandler.IsForcedToEmote)
            {
                // if our current emote state is any sitting pose and we are attempting to perform yes or no, allow it.
                if (_hardcoreHandler.ForcedEmoteState.EmoteID is 50 or 52 && emoteId is 42 or 24)
                {
                    _logger.LogDebug("Allowing Emote Execution for Emote ID: " + emoteId, LoggerType.EmoteMonitor);
                }
                else
                {
                    // If we are not allowed to execute the emote, then return early.
                    if (AllowExecution.NextEmoteAllowed is false)
                    {
                        _logger.LogWarning("Emote Execution is not allowed!");
                        return;
                    }
                    // If we were allowed to execute an emote but a player tried to squeeze in another emote that wasnt the expected, return.
                    if (AllowExecution.EmoteId != emoteId)
                    {
                        _logger.LogInformation("Sorry sugar, but you ain't cheesing this system that easily." + emoteId);
                        return;
                    }
                    // The Emote is the same as the expected, so allow it.
                    _logger.LogTrace("Allowing Emote Execution for Emote ID: " + emoteId, LoggerType.EmoteMonitor);
                }
                AllowExecution = (false, 0);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in OnExecuteEmoteDetour");
        }
        OnExecuteEmoteHook.Original(thisPtr, emoteId, playEmoteOption, addToHistory, liveUpdateHistory);
    }
}
