using CkCommons.DrawSystem;
using Dalamud.Bindings.ImGui;
using GagSpeak.Kinksters;
using GagspeakAPI.Data;
using static GagSpeak.DrawSystem.SorterEx;

namespace GagSpeak.DrawSystem;

public sealed class AliasFolder : DynamicFolder<AliasTrigger>
{
    private Func<IReadOnlyList<AliasTrigger>> _generator;
    public AliasFolder(DynamicFolderGroup<AliasTrigger> parent, uint id, string name, uint iconColor,
        Func<IReadOnlyList<AliasTrigger>> generator)
        : base(parent, FAI.None, name, id, new([new SorterEx.ByAliasName()]))
    {
        // Can set stylizations here.
        NameColor = uint.MaxValue;
        IconColor = iconColor;
        BgColor = uint.MinValue;
        BorderColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        GradientColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        _generator = generator;
    }

    protected override IReadOnlyList<AliasTrigger> GetAllItems() => _generator();
    protected override DynamicLeaf<AliasTrigger> ToLeaf(AliasTrigger item) => new(this, item.Label, item);

    // Maybe replace with something better later. Would be nice to not depend on multiple generators but idk.
    public string BracketText => Name switch
    {
        Constants.FolderTagAliasesActive => $"[{TotalChildren}]",
        Constants.FolderTagAliasesInactive => $"[{TotalChildren}]",
        _ => string.Empty,
    };

    public string BracketTooltip => Name switch
    {
        Constants.FolderTagAliasesActive => $"{TotalChildren} Shared aliases are enabled",
        Constants.FolderTagAliasesInactive => $"{TotalChildren} Shared aliases are disabled",
        _ => string.Empty,
    };
}
