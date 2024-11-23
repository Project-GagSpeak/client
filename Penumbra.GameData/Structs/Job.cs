using Dalamud.Utility;
using Lumina.Excel.Sheets;

namespace Penumbra.GameData.Structs;

/// <summary>
/// A struct containing the different jobs the game supports.
/// Also contains the jobs Name and Abbreviation as strings.
/// </summary>
public readonly struct Job(in ClassJob job)
{
    public enum JobRole : byte
    {
        Unknown        = 0,
        Tank           = 1,
        Melee          = 2,
        RangedPhysical = 3,
        RangedMagical  = 4,
        Healer         = 5,
        Crafter        = 6,
        Gatherer       = 7,
    }

    /// <summary> The name of the job. </summary>
    public readonly string Name = string.Intern(job.Name.ToString());

    /// <summary> The 3-letter abbreviation of the job. </summary>
    public readonly string Abbreviation = string.Intern(job.Abbreviation.ToString());

    /// <summary> The ID of the job. </summary>
    public readonly JobId Id = (JobId)job.RowId;

    /// <summary> The role of the job. </summary>
    public readonly JobRole Role = job.Role switch
    {
        1                           => JobRole.Tank,
        2                           => JobRole.Melee,
        3 when job.PrimaryStat is 4 => JobRole.RangedMagical,
        3 when job.PrimaryStat is 2 => JobRole.RangedPhysical,
        4                           => JobRole.Healer,
        0 when job.PartyBonus is 6  => JobRole.Gatherer,
        0 when job.PartyBonus is 7  => JobRole.Crafter,
        _                           => JobRole.Unknown,
    };

    /// <summary> The flag corresponding to the job's ID. </summary>
    public JobFlag Flag
        => (JobFlag)(1ul << Id.Id);

    /// <inheritdoc/>
    public override string ToString()
        => Name;
}
