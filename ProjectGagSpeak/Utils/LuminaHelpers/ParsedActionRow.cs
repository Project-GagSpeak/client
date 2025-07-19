using CkCommons;
using Lumina.Excel.Sheets;
using GameAction = Lumina.Excel.Sheets.Action;

namespace GagSpeak.Utils;

public readonly record struct LightJob
{
    public readonly JobType JobId; // ex. NIN
    public readonly string Name;
    public readonly string Abbreviation;
    public readonly JobRole Role;
    public readonly JobType ParentJobId; // ex. ROUGE
    public LightJob(ClassJob job)
    {
        JobId = (JobType)job.RowId;
        Name = string.Intern(job.Name.ToString());
        Abbreviation = string.Intern(job.Abbreviation.ToString());
        Role = (JobRole)job.Role;
        ParentJobId = (JobType)job.ClassJobParent.RowId;
    }

    public bool IsUpgradedJob() => JobId != ParentJobId;

    public uint GetIconId()
        => JobId is JobType.ADV ? 62143 : (uint)(062100 + (int)JobId);

    public static uint GetIconId(JobType jobId)
        => jobId is JobType.ADV ? 62143 : (uint)(062100 + (int)jobId);

    public override string ToString()
        => $"ID: {JobId}, Name: {Name}, Abbreviation: {Abbreviation}, Role: {Role}";
}

/// <summary> Extracts only the information we care about from an action row. </summary>
public readonly record struct ParsedActionRow : IEquatable<ParsedActionRow>
{
    public readonly string Name;
    public readonly uint ActionID;
    public readonly ushort IconID;
    public readonly byte CooldownGroup;
    public readonly JobType Job; // ex. NIN
    public readonly JobType ParentJob; // ex. ROUGE

    public ParsedActionRow(GameAction luminaAction)
    {
        Name = luminaAction.Name.ToString();
        ActionID = luminaAction.RowId;
        IconID = luminaAction.Icon;
        CooldownGroup = luminaAction.CooldownGroup;
        Job = (JobType)luminaAction.ClassJob.Value.RowId;
        ParentJob = (JobType)luminaAction.ClassJob.Value.ClassJobParent.RowId;
    }

    /// <inheritdoc/>
    public override string ToString()
        => Name;

    /// <inheritdoc/>
    public bool Equals(ParsedActionRow other)
        => ActionID == other.ActionID;

    /// <inheritdoc/>
    public override int GetHashCode()
        => ActionID.GetHashCode();
}
