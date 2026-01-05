using CkCommons.DrawSystem;
using Dalamud.Bindings.ImGui;
using GagSpeak.Kinksters;

namespace GagSpeak.DrawSystem;

public sealed class PairFolder : DynamicFolder<Kinkster>
{
    private Func<IReadOnlyList<Kinkster>> _generator;
    public PairFolder(DynamicFolderGroup<Kinkster> parent, uint id, FAI icon, string name,
        uint iconColor, Func<IReadOnlyList<Kinkster>> generator)
        : base(parent, icon, name, id)
    {
        // Can set stylizations here.
        NameColor = uint.MaxValue;
        IconColor = iconColor;
        BgColor = uint.MinValue;
        BorderColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        GradientColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        _generator = generator;
    }

    public PairFolder(DynamicFolderGroup<Kinkster> parent, uint id, FAI icon, string name,
        uint iconColor, Func<IReadOnlyList<Kinkster>> generator, IReadOnlyList<ISortMethod<DynamicLeaf<Kinkster>>> sortSteps)
        : base(parent, icon, name, id, new(sortSteps))
    {
        // Can set stylizations here.
        NameColor = uint.MaxValue;
        IconColor = iconColor;
        BgColor = uint.MinValue;
        BorderColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        _generator = generator;
    }

    public int Rendered => Children.Count((Func<DynamicLeaf<Kinkster>, bool>)(s => (bool)s.Data.IsRendered));
    public int Online => Children.Count(s => s.Data.IsOnline);
    protected override IReadOnlyList<Kinkster> GetAllItems() => _generator();
    protected override DynamicLeaf<Kinkster> ToLeaf(Kinkster item) => new(this, item.UserData.UID, item);

    // Maybe replace with something better later. Would be nice to not depend on multiple generators but idk.
    public string BracketText => Name switch
    {
        Constants.FolderTagAll => $"[{TotalChildren}]",
        Constants.FolderTagVisible => $"[{Rendered}]",
        Constants.FolderTagOnline => $"[{Online}]",
        Constants.FolderTagOffline => $"[{TotalChildren}]",
        _ => string.Empty,
    };

    public string BracketTooltip => Name switch
    {
        Constants.FolderTagAll => $"{TotalChildren} total",
        Constants.FolderTagVisible => $"{Rendered} visible",
        Constants.FolderTagOnline => $"{Online} online",
        Constants.FolderTagOffline => $"{TotalChildren} offline",
        _ => string.Empty,
    };
}
