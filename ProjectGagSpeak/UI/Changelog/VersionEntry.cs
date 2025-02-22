using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace GagSpeak.UI.Components;
public class VersionEntry
{
    public readonly int Major;
    public readonly int Minor;
    public readonly int Build;
    public readonly int Fix;
    public Dictionary<EntryType, List<ChangelogEntry>> CategorizedEntries { get; private set; } = new Dictionary<EntryType, List<ChangelogEntry>>();

    public VersionEntry(int vMajor, int vMinor, int vBuild, int vFix)
    {
        Major = vMajor;
        Minor = vMinor;
        Build = vBuild;
        Fix = vFix;
        foreach (EntryType entryType in Enum.GetValues(typeof(EntryType)))
        {
            CategorizedEntries[entryType] = new List<ChangelogEntry>();
        }
    }

    public string VersionString => $"v{Major}.{Minor}.{Build}.{Fix}";

    public VersionEntry RegisterMain(string text)
    {
        CategorizedEntries[EntryType.MainUpdatePoint].Add(new ChangelogEntry(EntryType.MainUpdatePoint, text));
        return this;
    }

    public VersionEntry RegisterFeature(string text)
    {
        CategorizedEntries[EntryType.FeatureAddition].Add(new ChangelogEntry(EntryType.FeatureAddition, text));
        return this;
    }

    public VersionEntry RegisterQol(string text)
    {
        CategorizedEntries[EntryType.QualityOfLife].Add(new ChangelogEntry(EntryType.QualityOfLife, text));
        return this;
    }

    public VersionEntry RegisterBugfix(string text)
    {
        CategorizedEntries[EntryType.BugFix].Add(new ChangelogEntry(EntryType.BugFix, text));
        return this;
    }
}

public readonly struct ChangelogEntry
{
    public readonly EntryType Type;
    public readonly string Text;
    public ChangelogEntry(EntryType type, string text)
    {
        Type = type;
        Text = text;
    }
    public void DrawContents()
    {
        using var color = ImRaii.PushColor(ImGuiCol.Text, GetEntryColor());
        ImGui.Bullet();
        ImGui.PushTextWrapPos();
        ImGui.TextUnformatted(Text);
        ImGui.PopTextWrapPos();
    }

    private Vector4 GetEntryColor()
    {
        return Type switch
        {
            EntryType.MainUpdatePoint => ImGuiColors.ParsedPink,
            EntryType.FeatureAddition => ImGuiColors.ParsedGold,
            EntryType.QualityOfLife => ImGuiColors.HealerGreen,
            EntryType.BugFix => ImGuiColors.DalamudGrey,
            _ => ImGuiColors.DalamudWhite,
        };
    }
}

public enum EntryType
{
    MainUpdatePoint,
    FeatureAddition,
    QualityOfLife,
    BugFix,
}
