using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Hosting;
using System.Collections.Immutable;
using GagSpeak.Utils;
using ClientStructFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace GagSpeak.Services;

// Hosted service responsible for allocating a cache of valid emote data during startup, and holding it for future references.
public sealed class EmoteService : IHostedService
{
    private static unsafe AgentEmote* Agent = (AgentEmote*)ClientStructFramework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.Emote);

    // Immutable (one-time gen) readonly cache of all valid existing Emotes.
    public static ImmutableList<Emote>          ValidEmoteCache      { get; private set; }
    public static ImmutableList<ParsedEmoteRow> ValidLightEmoteCache { get; private set; }

    // (Universal across languages) EmoteID variants for standing idles, sitting cposes, and groundsit cposes.
    public static readonly ushort[] StandIdleList   = [  0, 91, 92, 107, 108, 218, 219 ];
    public static readonly ushort[] SitIdList       = [ 50, 95, 96, 254, 255 ];
    public static readonly ushort[] GroundSitIdList = [ 52, 97, 98, 117 ];

    /// <summary> An Emote that can be whitelisted by OnExecuteEmote while in LockedEmote state.</summary>
    /// <remarks> Only works for one execution. </remarks>
    public static ushort SpecialAllowanceEmote { get; private set; } = 0;

    public EmoteService()
    {
        ValidEmoteCache = Svc.Data.GetExcelSheet<Emote>().Where(x => x.EmoteCategory.IsValid && !x.Name.ExtractText().IsNullOrWhitespace()).ToImmutableList();
        ValidLightEmoteCache = ValidEmoteCache.Select(x => new ParsedEmoteRow(x)).ToImmutableList();
        // Svc.Logger.Verbose("Emote Commands: " + string.Join("|", ValidLightEmoteCache.Select(row => row.InfoString)));
    }

    public static bool IsStandingIdle(ushort emoteId) => StandIdleList.Contains(emoteId);
    public static bool IsSitting(ushort emoteId) => SitIdList.Contains(emoteId);
    public static bool IsGroundSitting(ushort emoteId) => GroundSitIdList.Contains(emoteId);
    public static bool IsSittingAny(ushort emoteId) => SitIdList.Concat(GroundSitIdList).Contains(emoteId);
    public static bool IsAnyPoseWithCyclePose(ushort emoteId) => SitIdList.Concat(GroundSitIdList).Concat(StandIdleList).Contains(emoteId);


    /// <summary> The current emote ID of the player. </summary>
    public static unsafe ushort CurrentEmoteId(IntPtr address) => ((Character*)address)->EmoteController.EmoteId;

    /// <summary> The current cycle pose of the player's active emote. </summary>
    public static unsafe byte CurrentCyclePose(IntPtr address) => ((Character*)address)->EmoteController.CPoseState;

    /// <summary> If the game object at this address is currently performing an emote. </summary>
    public static unsafe bool InPositionLoop(IntPtr address) => ((Character*)address)->Mode is CharacterModes.InPositionLoop;

    /// <summary> If we are able to execute an emote or not. </summary>
    /// <remarks> Emotes that return false cannot be executed by the player. </remarks>
    public static unsafe bool CanUseEmote(ushort emoteID) => Agent->CanUseEmote(emoteID);

    /// <summary> Get the name of the emote from the ID. </summary>
    public static string EmoteName(ushort emoteId) => ValidLightEmoteCache
        .FirstOrDefault(x => x.RowId == emoteId).ToString() ?? "UNKNOWN / INVALID EMOTE";

    /// <summary> Perform the Emote if we can execute it. </summary>
    public static unsafe void ExecuteEmote(ushort emoteID)
    {
        if (!CanUseEmote(emoteID))
            return;

        // set the next allowance, and execute it.
        SpecialAllowanceEmote = emoteID;
        Agent->ExecuteEmote(emoteID);
    }

    public static void ResetSpecialAllowance() => SpecialAllowanceEmote = 0;

    /// <summary> Gets the total number of times you can do /cpose on an emote. </summary>
    public static int CyclePoseCount(ushort emoteId)
    {
        if (IsStandingIdle(emoteId)) return 7;
        if (IsSitting(emoteId)) return 4;
        if (IsGroundSitting(emoteId)) return 3;
        return 0;
    }

    public static bool IsCyclePoseTaskRunning => EnforceCyclePoseTask is not null && !EnforceCyclePoseTask.IsCompleted;
    private static Task? EnforceCyclePoseTask;

    public static void ForceCyclePose(IntPtr playerAddr, byte expectedCyclePose)
    {
        if (IsCyclePoseTaskRunning)
            return;

        Svc.Logger.Verbose("Forcing player into cycle pose: " + expectedCyclePose, LoggerType.EmoteMonitor);
        EnforceCyclePoseTask = ForceCyclePoseInternal(playerAddr, expectedCyclePose);
    }

    /// <summary> Force player into a certain CyclePose. Will not fire if Task is currently running. </summary>
    private static async Task ForceCyclePoseInternal(IntPtr playerAddr, byte expectedCyclePose)
    {
        try
        {
            // Only do this task if we are currently in a groundsit pose.
            var currentPose = CurrentEmoteId(playerAddr);
            var totalcycleposes = CyclePoseCount(currentPose);
            // if our emote is not any type of sit, dont perform this task.
            if (IsAnyPoseWithCyclePose(currentPose))
            {
                // Attempt the cycles, break out when we hit the count.
                for (var i = 0; i < totalcycleposes; i++)
                {
                    var current = CurrentCyclePose(playerAddr);
                    if (current == expectedCyclePose)
                        break;

                    Svc.Logger.Verbose("Cycle Pose State was [" + current + "], expected [" + expectedCyclePose + "]. Sending /cpose.", LoggerType.EmoteMonitor);
                    ExecuteEmote(90);
                    await WaitForCondition(() => CanUseEmote(90), 5);
                }
            }
        }
        finally
        {
            EnforceCyclePoseTask = null;
        }
    }

    /// <summary> Await for emote execution to be allowed again </summary>
    /// <returns>true when the condition was fulfilled, false if timed out or cancelled</returns>
    public static async Task<bool> WaitForCondition(Func<bool> condition, int timeoutSeconds = 5, CancellationToken token = default)
    {
        // Create a cancellation token source with the specified timeout
        using var timeout = new CancellationTokenSource(timeoutSeconds * 1000);
        try
        {
            // Try for condition until timeout or cancellation is requested.
            while (!timeout.Token.IsCancellationRequested && (token == default || !token.IsCancellationRequested))
            {
                if (condition()) 
                    return true;

                Svc.Logger.Verbose("(Excessive) Waiting for condition to be true.", LoggerType.EmoteMonitor);
                await Task.Delay(100, timeout.Token);
            }
        }
        catch (TaskCanceledException)
        {
            Svc.Logger.Verbose("WaitForCondition was canceled due to timeout.", LoggerType.EmoteMonitor);
        }
        return false;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Svc.Logger.Information("EmoteMonitor started.", LoggerType.EmoteMonitor);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Svc.Logger.Information("EmoteMonitor stopped.", LoggerType.EmoteMonitor);
        return Task.CompletedTask;
    }
}

public static class EmoteEx
{
    /// <summary> The ParsedEmoteRow collection for all emotes that have looping animations. </summary>
    public static IEnumerable<ParsedEmoteRow> LoopedEmotes()
        => EmoteService.ValidLightEmoteCache.Where(x => x.EmoteConditionMode is 3 || x.RowId is 50 or 52).ToList();

    /// <summary> The ParsedEmoteRow collection for all emotes used in sitting emotes and cycle poses. </summary>
    public static IEnumerable<ParsedEmoteRow> SittingEmotes()
        => LoopedEmotes().Where(x => x.RowId is 50 or 52);

    /// <summary> Retrieves the list of all possible commands that can be used through text commands to perform such emotes. </summary>
    public static IEnumerable<string> AllCommands()
        => EmoteService.ValidLightEmoteCache.SelectMany(x => x.EmoteCommands).Distinct().ToList();

    /// <summary> Retrieves the list of all possible commands that can be used through text commands to perform such emotes. </summary>
    /// <remarks> This version takes away /yes and /no from this list. Allowing it to be filtered out. </remarks>
    public static IEnumerable<string> AllCommandsMinusYesNo()
        => EmoteService.ValidLightEmoteCache.Where(x => x.RowId is not 42 and not 24).SelectMany(x => x.EmoteCommands).Distinct().ToList();
}
