using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Hosting;
using System.Collections.Immutable;
using GagSpeak.Utils;
using GameAction = Lumina.Excel.Sheets.Action;

namespace GagSpeak.Services;

// Hosted service responsible for allocating a cache of valid SpellAction data,
// and holding it for future references.
public sealed class SpellActionService : IHostedService
{
    // Immutable (one-time gen) readonly cache of all valid existing Emotes.
    private static ImmutableList<GameAction> _keyGameActions;

    // Accessors.
    public static ImmutableList<LightJob> AllJobs { get; private set; }
    public static ImmutableList<LightJob> BattleClassJobs => AllJobs.Where(x => x.Role != 0).ToImmutableList();
    public static ImmutableList<ParsedActionRow> AllActions { get; private set; }
    public static ImmutableDictionary<uint, ParsedActionRow> AllActionsLookup => AllActions.ToImmutableDictionary(x => x.ActionID);
    public static ImmutableDictionary<LightJob, List<ParsedActionRow>> JobActionsDict { get; private set; }

    public SpellActionService()
    {

        // Start performance timer.
        var stopwatch = Stopwatch.StartNew();

        _keyGameActions = Svc.Data.GetExcelSheet<GameAction>()
            .Where(r => r.IsPlayerAction && r.ClassJob.ValueNullable.HasValue).ToImmutableList();
        
        AllJobs = Svc.Data.GetExcelSheet<ClassJob>().Select(x => new LightJob(x)).ToImmutableList();
        AllActions = _keyGameActions.Select(x => new ParsedActionRow(x)).ToImmutableList();

        var jobLookup = AllJobs.ToDictionary(j => (uint)j.JobId);
        var jobActionsDict = AllJobs.ToDictionary(j => j, _ => new List<ParsedActionRow>());
        // For each action, filter it into the correct class.
        foreach (var gameAct in _keyGameActions)
        {
            // If a valid job is present, append it.
            if (jobLookup.TryGetValue(gameAct.ClassJob.Value.RowId, out var job))
                jobActionsDict[job].Add(new ParsedActionRow(gameAct));
        }

        // Go back through and iterate over the light jobs. For any that are upgraded, concat their actions with their parent.
        foreach (var (job, actions) in jobActionsDict)
        {
            // if the job is not an upgraded job continue.
            if (!job.IsUpgradedJob())
                continue;

            // Otherwise, try and locate the actions for its parent job.
            if (jobActionsDict.FirstOrDefault(x => x.Key.JobId == job.ParentJobId) is { } match)
                // concatinate those actions with our jobs current actions.
                actions.InsertRange(0, match.Value);
        }

        // Stop the performance timer.
        stopwatch.Stop();

        Svc.Logger.Information($"Cached {_keyGameActions.Count()} actions " +
            $"in {stopwatch.ElapsedMilliseconds}ms for {AllJobs.Count()} Jobs.");

        // Assign the final immutable lists.
        JobActionsDict = jobActionsDict.ToImmutableDictionary(x => x.Key, x => x.Value);
    }

    /// <inheritdoc cref="GetLightJob(JobType)"/>
    public static LightJob GetLightJob(uint id) => GetLightJob((JobType)id);

    /// <summary> Helper to aquire the LightJob data for a JobId where needed. </summary>
    public static LightJob GetLightJob(JobType id) => AllJobs.FirstOrDefault(x => x.JobId == id);


    /// <inheritdoc cref="GetJobActions(LightJob)"/>"
    public static IEnumerable<ParsedActionRow> GetJobActions(uint jobId)
        => GetJobActions(GetLightJob(jobId));

    /// <inheritdoc cref="GetJobActions(LightJob)"/>/>
    public static IEnumerable<ParsedActionRow> GetJobActions(JobType jobId)
        => GetJobActions(GetLightJob(jobId));

    /// <summary> Returns the ParsedActionRow actions for a given job. </summary>
    public static IEnumerable<ParsedActionRow> GetJobActions(LightJob job)
        => JobActionsDict.TryGetValue(job, out var acts) ? acts : Enumerable.Empty<ParsedActionRow>();

    public Task StartAsync(CancellationToken ct)
    {
        Svc.Logger.Information("SpellAction Monitor started.", LoggerType.EmoteMonitor);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        Svc.Logger.Information("SpellAction Monitor stopped.", LoggerType.EmoteMonitor);
        return Task.CompletedTask;
    }
}
