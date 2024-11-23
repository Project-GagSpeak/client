using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.UpdateMonitoring.Triggers;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Collections.ObjectModel;
using ClientStructFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;


namespace GagSpeak.UpdateMonitoring;
public class EmoteMonitor
{
    private readonly ILogger<EmoteMonitor> _logger;
    private readonly ClientMonitorService _clientService;
    private readonly IDataManager _gameData;

    private static unsafe AgentEmote* EmoteAgentRef = (AgentEmote*)ClientStructFramework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.Emote);
    public unsafe EmoteMonitor(ILogger<EmoteMonitor> logger, ClientMonitorService clientService, IDataManager dataManager)
    {
        _logger = logger;
        _clientService = clientService;
        _gameData = dataManager;
        EmoteDataAll = _gameData.GetExcelSheet<Emote>();
        EmoteDataLoops = EmoteDataAll.Where(x => x.RowId is (50 or 52) || x.EmoteMode.Value.ConditionMode is 3).ToDictionary(x => x.RowId, x => x).AsReadOnly();

        // Generate Emote List.
        EmoteCommandsWithId = EmoteDataAll
        .Where(x => x.EmoteCategory.IsValid && x.EmoteCategory.Value.RowId is not 3)
        .SelectMany(emoteCommand => new[]
        {
            (Command: emoteCommand.TextCommand.ValueNullable?.Command.ToString().TrimStart('/'), emoteCommand.RowId),
            (Command: emoteCommand.TextCommand.ValueNullable?.ShortCommand.ToString().TrimStart('/'), emoteCommand.RowId),
            (Command: emoteCommand.TextCommand.ValueNullable?.Alias.ToString().TrimStart('/'), emoteCommand.RowId),
            (Command: emoteCommand.TextCommand.ValueNullable?.ShortAlias.ToString().TrimStart('/'), emoteCommand.RowId)
        })
        .Where(cmd => !string.IsNullOrWhiteSpace(cmd.Command))
        .GroupBy(cmd => cmd.Command)
        .OrderBy(x => x.Key)
        .Select(group => group.First())
        .ToDictionary(cmd => cmd.Command!, cmd => cmd.RowId);

        // Filter for yes/no commands based on RowId (42 or 24).
        var yesNoCommands = EmoteCommandsWithId.Where(kvp => kvp.Value is 42 or 24).Select(kvp => kvp.Key).ToHashSet();

        // Create the final set by excluding yesNoCommands from EmoteCommandsWithId keys.
        EmoteCommandsYesNoAccepted = EmoteCommandsWithId.Keys.Except(yesNoCommands).ToHashSet();

        // log all recorded emotes.
        _logger.LogDebug("Emote Commands: " + string.Join(", ", EmoteCommands), LoggerType.EmoteMonitor);

        _logger.LogDebug("CposeInfo => " + EmoteDataAll.FirstOrDefault(x => x.RowId is 90).Name.ToString(), LoggerType.EmoteMonitor);
    }

    public static readonly ushort[] StandIdleList = new ushort[] { 0, 91, 92, 107, 108, 218, 219 };
    public static readonly ushort[] SitIdList = new ushort[] { 50, 95, 96, 254, 255 };
    public static readonly ushort[] GroundSitIdList = new ushort[] { 52, 97, 98, 117 };
    public static ExcelSheet<Emote> EmoteDataAll = null!;
    public static ReadOnlyDictionary<uint, Emote> EmoteDataLoops = null!;
    public static Dictionary<string, uint> EmoteCommandsWithId = null!;
    public static HashSet<string> EmoteCommands => EmoteCommandsWithId.Keys.ToHashSet();
    public static HashSet<string> EmoteCommandsYesNoAccepted = [];

    // create a IEnumerable array that only consists of the emote data from keys of 50 and 52.
    public static IEnumerable<Emote> SitEmoteComboList => EmoteDataLoops.Where(x => x.Key == 50 || x.Key == 52).Select(x => x.Value);
    public static IEnumerable<Emote> EmoteComboList => EmoteDataLoops.Values.ToArray();
    public static string GetEmoteName(uint emoteId)
    {
        if (EmoteDataLoops.TryGetValue(emoteId, out var emote)) return emote.Name.ExtractText().Replace("\u00AD", "") ?? $"Emote#{emoteId}";
        return $"Emote#{emoteId}";
    }

    public unsafe ushort CurrentEmoteId() => ((Character*)(_clientService.Address))->EmoteController.EmoteId;
    public unsafe byte CurrentCyclePose() => ((Character*)(_clientService.Address))->EmoteController.CPoseState;
    public unsafe bool InPositionLoop() => ((Character*)(_clientService.Address))->Mode is CharacterModes.InPositionLoop;

    // This is valid for both if its not unlocked or if you are on cooldown.
    public static unsafe bool CanUseEmote(ushort emoteId) => EmoteAgentRef->CanUseEmote(emoteId);

    // Perform the Emote if we can execute it.
    public static unsafe void ExecuteEmote(ushort emoteId)
    {
        if (!CanUseEmote(emoteId))
        {
            StaticLogger.Logger.LogWarning("Can't perform this emote!");
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
        if (EmoteMonitor.IsCyclePoseTaskRunning) return;
        _logger.LogDebug("Forcing player into cycle pose: " + expectedCyclePose, LoggerType.EmoteMonitor);
        EnforceCyclePoseTask = ForceCyclePoseInternal(expectedCyclePose);
    }

    /// <summary>
    /// Force player into a certain CyclePose. Will not fire if Task is currently running.
    /// </summary>
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

    /// <summary>
    /// Await for emote execution to be allowed again
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="timeoutSeconds"></param>
    /// <returns></returns>
    public async Task WaitForCondition(Func<bool> condition, int timeoutSeconds = 5)
    {
        // Create a cancellation token source with the specified timeout
        using var timeout = new CancellationTokenSource(timeoutSeconds * 1000);
        try
        {
            while (!condition() && !timeout.Token.IsCancellationRequested)
            {
                StaticLogger.Logger.LogTrace("(Excessive) Waiting for condition to be true.", LoggerType.EmoteMonitor);
                await Task.Delay(100, timeout.Token);
            }
        }
        catch (TaskCanceledException)
        {
            StaticLogger.Logger.LogTrace("WaitForCondition was canceled due to timeout.", LoggerType.EmoteMonitor);
        }
    }
}
