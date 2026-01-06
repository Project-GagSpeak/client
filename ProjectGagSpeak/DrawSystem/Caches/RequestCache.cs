using CkCommons.DrawSystem;
using CkCommons.DrawSystem.Selector;
using GagSpeak.PlayerClient;

namespace GagSpeak.DrawSystem;

// RequesterCache used by RequestInDrawer and RequestOutDrawer.
public class RequestCache(DynamicDrawSystem<RequestEntry> parent) : DynamicFilterCache<RequestEntry>(parent)
{
    public bool FilterConfigOpen { get; set; } = false;

    protected override bool IsVisible(IDynamicNode<RequestEntry> node)
    {
        if (Filter.Length is 0)
            return true;

        if (node is DynamicLeaf<RequestEntry> leaf)
            return leaf.Data.FromClient
                ? leaf.Data.RecipientAnonName.Contains(Filter, StringComparison.OrdinalIgnoreCase)
                : leaf.Data.SenderAnonName.Contains(Filter, StringComparison.OrdinalIgnoreCase);

        return base.IsVisible(node);
    }
}
