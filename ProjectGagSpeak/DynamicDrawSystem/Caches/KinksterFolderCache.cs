using CkCommons.DrawSystem;
using CkCommons.DrawSystem.Selector;
using GagSpeak.Kinksters;

namespace GagSpeak.DrawSystem;

// Cache for DDS's using Kinkster items.
public class KinksterFolderCache(DynamicDrawSystem<Kinkster> parent) : DynamicFilterCache<Kinkster>(parent)
{
    protected override bool IsVisible(IDynamicNode<Kinkster> node)
    {
        if (Filter.Length is 0)
            return true;

        if (node is DynamicLeaf<Kinkster> leaf)
            return leaf.Data.UserData.AliasOrUID.Contains(Filter, StringComparison.OrdinalIgnoreCase)
                || (leaf.Data.GetNickname()?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false)
                || (leaf.Data.PlayerName?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false);

        return base.IsVisible(node);
    }
}
