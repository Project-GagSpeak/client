using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.UpdateMonitoring.Triggers;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Collections.Immutable;
using ClientStructFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace GagSpeak.UpdateMonitoring;
public class EmoteMonitor
{
    private readonly ILogger<EmoteMonitor> _logger;
    private readonly ClientMonitor _clientMonitor;

    private static unsafe AgentEmote* EmoteAgentRef = (AgentEmote*)ClientStructFramework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.Emote);
    public unsafe EmoteMonitor(ILogger<EmoteMonitor> logger, ClientMonitor clientMonitor, IDataManager dataManager)
    {
        _logger = logger;
        _clientMonitor = clientMonitor;
        EmoteDataAll = dataManager.GetExcelSheet<Emote>();
        // Inject the nessisary data into the cache.
        ValidEmotes = EmoteDataAll.Where(x =>
        {
            // Emote must be valid
            if (x.EmoteCategory.IsValid is false)
                return false;
            // Extracted name cannot be empty.
            if (x.Name.ExtractText().IsNullOrWhitespace())
                return false;

            // Will be sent to parsed emote row storage.
            return true;
        }).Select(x => new ParsedEmoteRow(x));

        // log all recorded emotes.
        _logger.LogDebug("Emote Commands: " + string.Join(", ", EmoteCommands), LoggerType.EmoteMonitor);
        _logger.LogDebug("CposeInfo => " + EmoteDataAll.FirstOrDefault(x => x.RowId is 90).Name.ToString(), LoggerType.EmoteMonitor);
    }

    // Universal across languages.
    public static readonly ushort[] StandIdleList = new ushort[] { 0, 91, 92, 107, 108, 218, 219 };
    public static readonly ushort[] SitIdList = new ushort[] { 50, 95, 96, 254, 255 };
    public static readonly ushort[] GroundSitIdList = new ushort[] { 52, 97, 98, 117 };

    public static ExcelSheet<Emote> EmoteDataAll = null!;
    public static IEnumerable<ParsedEmoteRow> ValidEmotes;      // Efficent low memory storage.
    public static IEnumerable<ParsedEmoteRow> EmoteDataLoops    => ValidEmotes.Where(x => x.EmoteConditionMode is 3 || x.RowId is 50 or 52);
    public static IEnumerable<string> EmoteCommands             => ValidEmotes.SelectMany(c => c.EmoteCommands);
    public static IEnumerable<string> EmoteCommandsWithoutYesNo => ValidEmotes.Where(c => c.RowId is not 42 and not 24).SelectMany(c => c.EmoteCommands);
    public static IEnumerable<ParsedEmoteRow> SitEmoteComboList => EmoteDataLoops.Where(x => x.RowId is 50 || x.RowId is 52);
    public static string GetEmoteName(ushort emoteId) 
        => ValidEmotes.FirstOrDefault(x => x.RowId == emoteId).GetEmoteName() ?? "UNKNOWN / INVALID EMOTE";

    public unsafe ushort CurrentEmoteId()
        => ((Character*)(_clientMonitor.Address))->EmoteController.EmoteId;
    public unsafe byte CurrentCyclePose()
        => ((Character*)(_clientMonitor.Address))->EmoteController.CPoseState;
    public unsafe bool InPositionLoop() // Performing an emote currently.
        => ((Character*)(_clientMonitor.Address))->Mode is CharacterModes.InPositionLoop;

    // This is valid for both if its not unlocked or if you are on cooldown.
    public static unsafe bool CanUseEmote(ushort emoteId) => EmoteAgentRef->CanUseEmote(emoteId);

    // Perform the Emote if we can execute it.
    public static unsafe void ExecuteEmote(ushort emoteId)
    {
        if (!CanUseEmote(emoteId))
        {
            GagSpeak.StaticLog.Warning("Can't perform this emote!");
            return;
        }
        // set the next allowance.
        OnEmote.AllowExecution = (true, emoteId);
        // Execute.
        EmoteAgentRef->ExecuteEmote(emoteId);
    }

    // Obtain the number of cycle poses available for the given emote ID.
    public static int EmoteCyclePoses(ushort emoteId)
    {
        if (IsStandingIdle(emoteId)) return 7;
        if (IsSitting(emoteId)) return 4;
        if (IsGroundSitting(emoteId)) return 3;
        return 0;
    }
    public static bool IsStandingIdle(ushort emoteId) => StandIdleList.Contains(emoteId);
    public static bool IsSitting(ushort emoteId) => SitIdList.Contains(emoteId);
    public static bool IsGroundSitting(ushort emoteId) => GroundSitIdList.Contains(emoteId);
    public static bool IsSittingAny(ushort emoteId) => SitIdList.Concat(GroundSitIdList).Contains(emoteId);
    public static bool IsAnyPoseWithCyclePose(ushort emoteId) => SitIdList.Concat(GroundSitIdList).Concat(StandIdleList).Contains(emoteId);

    public static bool IsCyclePoseTaskRunning => EnforceCyclePoseTask is not null && !EnforceCyclePoseTask.IsCompleted;
    private static Task? EnforceCyclePoseTask;

    public void ForceCyclePose(byte expectedCyclePose)
    {
        if (IsCyclePoseTaskRunning)
            return;

        _logger.LogDebug("Forcing player into cycle pose: " + expectedCyclePose, LoggerType.EmoteMonitor);
        EnforceCyclePoseTask = ForceCyclePoseInternal(expectedCyclePose);
    }

    /// <summary> Force player into a certain CyclePose. Will not fire if Task is currently running. </summary>
    private async Task ForceCyclePoseInternal(byte expectedCyclePose)
    {
        try
        {
            // Only do this task if we are currently in a groundsit pose.
            var currentPose = CurrentEmoteId();
            // if our emote is not any type of sit, dont perform this task.
            if (IsAnyPoseWithCyclePose(currentPose))
            {
                // Attempt the cycles, break out when we hit the count.
                for (var i = 0; i < 7; i++)
                {
                    var current = CurrentCyclePose();

                    if (current == expectedCyclePose)
                        break;

                    _logger.LogTrace("Cycle Pose State was [" + current + "], expected [" + expectedCyclePose + "]. Sending /cpose.", LoggerType.EmoteMonitor);
                    ExecuteEmote(90);
                    await WaitForCondition(() => EmoteMonitor.CanUseEmote(90), 5);
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
    public async Task<bool> WaitForCondition(Func<bool> condition, int timeoutSeconds = 5, CancellationToken token = default)
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

                GagSpeak.StaticLog.Verbose("(Excessive) Waiting for condition to be true.", LoggerType.EmoteMonitor);
                await Task.Delay(100, timeout.Token);
            }
        }
        catch (TaskCanceledException)
        {
            GagSpeak.StaticLog.Verbose("WaitForCondition was canceled due to timeout.", LoggerType.EmoteMonitor);
        }
        return false;
    }
}
