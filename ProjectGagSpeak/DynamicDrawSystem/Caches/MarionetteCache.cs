using CkCommons.DrawSystem;
using CkCommons.DrawSystem.Selector;
using GagSpeak.Kinksters;
using GagspeakAPI.Data;

namespace GagSpeak.DrawSystem;

public class MarionetteCache(DynamicDrawSystem<AliasTrigger> parent) : DynamicFilterCache<AliasTrigger>(parent)
{
    protected override bool IsVisible(IDynamicNode<AliasTrigger> node)
    {
        if (Filter.Length is 0)
            return true;

        if (node is DynamicLeaf<AliasTrigger> leaf)
            return leaf.Data.Label.Contains(Filter, StringComparison.OrdinalIgnoreCase)
                || leaf.Data.InputCommand.Contains(Filter, StringComparison.OrdinalIgnoreCase);

        return base.IsVisible(node);
    }
}
