using Dalamud.Utility;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Penumbra.GameData.Structs;

/// <summary>
/// A generic enum for job flags.
/// We do not need to name these since they are just bit-shifted by job IDs.
/// </summary>
[Flags]
public enum JobFlag : ulong
{
    All = ulong.MaxValue,
}

/// <summary> The game specifies different job groups that can contain specific jobs or not. </summary>
public readonly struct JobGroup
{
    /// <summary> The name of the group. </summary>
    public readonly string Name;

    /// <summary> The number of jobs contained in the group. </summary>
    public readonly int Count;

    /// <summary> The ID of the group. </summary>
    public readonly JobGroupId Id;

    /// <summary> The contained jobs as bit set. </summary>
    public readonly JobFlag Flags;

    /// <summary>
    /// Create a job group from a given category and the ClassJob sheet.
    /// It looks up the different jobs contained in the category and sets the flags appropriately.
    /// </summary>
    public JobGroup(ClassJobCategory group, ExcelSheet<ClassJob> jobs)
    {
        Count  = 0;
        Flags = 0ul;
        Id     = (JobGroupId)group.RowId;
        Name   = group.Name.ToString();

        Debug.Assert(jobs.Count < 64, $"Number of Jobs exceeded 63 ({jobs.Count}).");
        foreach (var job in jobs)
        {
            var abbr = job.Abbreviation.ToString();
            if (abbr.Length == 0)
                continue;

            var prop = group.GetType().GetProperty(abbr);
            Debug.Assert(prop != null, $"Could not get job abbreviation {abbr} property.");

            if (!(bool)prop.GetValue(group)!)
                continue;

            ++Count;
            Flags |= (JobFlag)(1ul << (int)job.RowId);
        }
    }

    /// <summary> Check if a job is contained inside this group. </summary>
    public bool Fits(Job job)
        => Flags.HasFlag(job.Flag);

    /// <summary> Check if a job is contained inside this group. </summary>
    public bool Fits(JobId jobId)
    {
        var flag = (JobFlag)(1ul << jobId.Id);
        return Flags.HasFlag(flag);
    }

    /// <summary> Check if any of the jobs in the given flags fit this group. </summary>
    public bool Fits(JobFlag flag)
        => (Flags & flag) != 0;

    /// <inheritdoc/>
    public override string ToString()
        => Name;

    /// <summary> Iterate over all jobs set in this group. </summary>
    public IEnumerable<JobId> Iterate()
    {
        var minSet = BitOperations.TrailingZeroCount((ulong)Flags);
        var maxSet = 64 - BitOperations.LeadingZeroCount((ulong)Flags);
        for (var i = minSet; i < maxSet; ++i)
        {
            if (Flags.HasFlag((JobFlag)(1ul << i)))
                yield return (JobId)i;
        }
    }
}
