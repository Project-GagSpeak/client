using Lumina.Excel.Sheets;

namespace GagSpeak.Utils;

/// <summary> helps out with locating and parsing moodles icons. </summary>
public struct ParsedIconInfo
{
    /// <summary> The icon Name of the moodle. </summary>
    public string Name;
    
    /// <summary> It's ID </summary>
    public uint IconID;

    /// <summary> The Type of Status it is. </summary>
    public StatusType Type;

    /// <summary> If this Moodle is a stackable type. </summary>
    public bool IsStackable;

    /// <summary> The ClassJob Category of the Moodle. (Likely not useful for us) </summary>
    public ClassJobCategory ClassJobCategory;

    /// <summary> If it is an FC Buff (Likely not useful for us) </summary>
    public bool IsFCBuff;
    
    /// <summary> The Description of the Moodle. </summary>
    public string Description;

    public ParsedIconInfo(Status status)
    {
        Name = status.Name.ToDalamudString().ExtractText();
        IconID = status.Icon;
        Type = status.CanIncreaseRewards == 1 ? StatusType.Special : (status.StatusCategory == 2 ? StatusType.Negative : StatusType.Positive);
        ClassJobCategory = status.ClassJobCategory.Value;
        IsFCBuff = status.IsFcBuff;
        IsStackable = status.MaxStacks > 1;
        Description = status.Description.ToDalamudString().ExtractText();
    }
}

