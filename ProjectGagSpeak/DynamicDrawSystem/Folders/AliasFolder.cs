using CkCommons.DrawSystem;
using Dalamud.Bindings.ImGui;
using GagSpeak.Kinksters;
using GagspeakAPI.Data;

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
}
