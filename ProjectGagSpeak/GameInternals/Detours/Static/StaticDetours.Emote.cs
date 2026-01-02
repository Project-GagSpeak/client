using CkCommons;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;

namespace GagSpeak.GameInternals.Detours;
public partial class StaticDetours
{
    /// <summary>
    ///     Detours Emote Requests sent by the Client Player to perform an emote.
    ///     Performs an early return if the emote is not allowed to be executed.
    /// </summary>
    internal Hook<AgentEmote.Delegates.ExecuteEmote> OnExecuteEmoteHook;

    /// <summary>
    ///     Processes emotes performed by other players besides yourself.
    /// </summary>
    public delegate void OnEmoteFuncDelegate(ulong unk, ulong emoteCallerAddr, ushort emoteId, ulong targetId, ulong unk2);
    internal static Hook<OnEmoteFuncDelegate> ProcessEmoteHook = null!;

    /// <summary>
    ///     Processes who did what emote for achievement and trigger purposes.
    ///     Provides the source and target along with the emote ID.
    /// </summary>
    private unsafe void ProcessEmoteDetour(ulong unk, ulong emoteCallerAddr, ushort emoteId, ulong targetId, ulong unk2)
    {
        try
        {
            // Cast it to a gameobject and validate it.
            GameObject* emoteCaller = (GameObject*)emoteCallerAddr;
            if (emoteCaller == null)
                throw new Bagagwa("Emote Caller GameObject is null in EmoteDetour.");

            var emoteCallerName = (emoteCaller->IsCharacter()) ? ((Character*)emoteCaller)->GetNameWithWorld() : "No Player Was Emote Caller";
            var emoteName = EmoteService.EmoteName(emoteId);
            var tgtObj = GameObjectManager.Instance()->Objects.GetObjectByGameObjectId(targetId);
            var targetName = (tgtObj != null && tgtObj->IsCharacter()) ? ((Character*)tgtObj)->GetNameWithWorld() : "No Player Was Target";
            Logger.LogTrace($"OnEmote >> [{emoteCallerName}] used Emote [{emoteName}](ID:{emoteId}) on Target: [{targetName}]", LoggerType.EmoteMonitor);

            // Could keep as nint or do snapshotting.
            GagspeakEventManager.AchievementEvent(UnlocksEvent.EmoteExecuted, (nint)emoteCaller, emoteId, (nint)tgtObj);
        }
        catch (Bagagwa e)
        {
            Logger.LogError(e, "Error in EmoteDetour");
        }

        ProcessEmoteHook.Original(unk, emoteCallerAddr, emoteId, targetId, unk2);
    }

    /// <summary>
    ///     Detours emote request from ClientPlayer. 
    ///     Performs an early return if the emote is not allowed to be executed.
    /// </summary>
    unsafe void OnExecuteEmote(AgentEmote* thisPtr, ushort emoteId, EmoteController.PlayEmoteOption* playEmoteOption, bool addToHistory, bool liveUpdateHistory)
    {
        Logger.LogTrace("OnExecuteEmote >> Emote [" + EmoteService.EmoteName(emoteId) + "](ID:"+emoteId+") requested to be Executed", LoggerType.EmoteMonitor);
            
        // Block all emotes if forced to follow
        if(ClientData.Hardcore.IsEnabled(HcAttribute.Follow))
            return;

        // If we are forced to emote, then we should prevent execution unless NextEmoteAllowed is true.
        if (ClientData.Hardcore is { } hc && hc.IsEnabled(HcAttribute.EmoteState))
        {
            // if our current emote state is any sitting pose and we are attempting to perform yes or no, allow it.
            if (hc.EmoteId is 50 or 52 && emoteId is 42 or 24)
            {
                Logger.LogDebug($"Allowing Emote Execution for [{EmoteService.EmoteName(emoteId)} ({emoteId})]", LoggerType.EmoteMonitor);
            }
            else
            {
                // If we are not allowed to execute the emote, then return early.
                if (EmoteService.SpecialAllowanceEmote <= 0)
                    return;
                // If we were allowed to execute an emote but a player tried to use another one, return.
                if (EmoteService.SpecialAllowanceEmote != emoteId)
                {
                    Logger.LogWarning("Sorry sugar, but you ain't cheesing this system that easily." + emoteId);
                    return; // Block Emote Execution
                }
                // The Emote is the same as the expected, so allow it.
                Logger.LogDebug($"Allowing Emote Execution for [{EmoteService.EmoteName(emoteId)} ({emoteId})]", LoggerType.EmoteMonitor);
                EmoteService.ResetSpecialAllowance();
            }
        }
        // Return the original.
        OnExecuteEmoteHook.Original(thisPtr, emoteId, playEmoteOption, addToHistory, liveUpdateHistory);
    }
}
