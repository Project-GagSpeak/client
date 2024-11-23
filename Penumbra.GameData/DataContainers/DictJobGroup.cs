using System.Collections.Frozen;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using OtterGui.Services;
using Penumbra.GameData.Data;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers;

/// <summary> A dictionary containing all the job groups. This is rather small and simple so not shared via data share. </summary>
public sealed class DictJobGroup : IDataContainer, IReadOnlyDictionary<JobGroupId, JobGroup>
{
    /// <summary> Create jobs. </summary>
    public DictJobGroup(IDataManager gameData)
    {
        var stopwatch = Stopwatch.StartNew();
        var sheet     = gameData.GetExcelSheet<ClassJobCategory>()!;
        var jobs      = gameData.GetExcelSheet<ClassJob>(ClientLanguage.English)!;
        AllJobGroups = sheet.Select(j => new JobGroup(j, jobs)).ToArray();
        _jobGroups   = AllJobGroups.Where(g => JobGroupIsValid(g.Id)).ToFrozenDictionary(g => g.Id, g => g);
        Memory       = DataUtility.DictionaryMemory(32, Count) + _jobGroups.Sum(kvp => kvp.Value.Name.Length) * 2 + 24 * AllJobGroups.Count;
        Time         = stopwatch.ElapsedMilliseconds;
    }

    /// <summary> All job groups, including those we do not care for. </summary>
    public IReadOnlyList<JobGroup> AllJobGroups { get; }

    /// <summary> The jobs. </summary>
    private readonly IReadOnlyDictionary<JobGroupId, JobGroup> _jobGroups;

    /// <inheritdoc/>
    public long Time { get; }

    /// <inheritdoc/>
    public long Memory { get; }

    /// <inheritdoc/>
    public string Name
        => nameof(DictJobGroup);

    /// <inheritdoc/>
    public int TotalCount
        => _jobGroups.Count;

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<JobGroupId, JobGroup>> GetEnumerator()
        => _jobGroups.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => _jobGroups.Count;

    /// <inheritdoc/>
    public bool ContainsKey(JobGroupId key)
        => _jobGroups.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(JobGroupId key, out JobGroup value)
        => _jobGroups.TryGetValue(key, out value);

    /// <inheritdoc/>
    public JobGroup this[JobGroupId key]
        => _jobGroups[key];

    /// <inheritdoc/>
    public IEnumerable<JobGroupId> Keys
        => _jobGroups.Keys;

    /// <inheritdoc/>
    public IEnumerable<JobGroup> Values
        => _jobGroups.Values;

    private static bool JobGroupIsValid(JobGroupId id)
        => id.Id switch
        {
            0    => false,
            < 36 => true,
            // Single jobs and big groups
            91  => true,
            92  => true,
            96  => true,
            98  => true,
            99  => true,
            111 => true,
            112 => true,
            129 => true,
            149 => true,
            150 => true,
            156 => true,
            157 => true,
            158 => true,
            159 => true,
            180 => true,
            181 => true,
            188 => true,
            189 => true,
            196 => true,
            197 => true,

            // Class + Job
            38 => true,
            41 => true,
            44 => true,
            47 => true,
            50 => true,
            53 => true,
            55 => true,
            69 => true,
            68 => true,
            93 => true,
            _  => false,
        };
}
