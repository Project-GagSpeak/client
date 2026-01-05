using CkCommons.DrawSystem;
using CkCommons.DrawSystem.Selector;
using GagSpeak.PlayerClient;

namespace GagSpeak.DrawSystem;

// RequesterCache used by RequestInDrawer and RequestOutDrawer.
public class RequestCache(DynamicDrawSystem<RequestEntry> parent) : DynamicFilterCache<RequestEntry>(parent)
{
    /// <summary>
    ///     If the config options under the filter bar should show.
    /// </summary>
    public bool FilterConfigOpen = false;

    /// <summary>
    ///     The groups that the accepted selection of requests will go towards.
    /// </summary>
    public List<string> AssignedGroups { get; set; } = [];

    /// <summary>
    ///     The nickname applied to an accepted request. (Unused on bulk accepting)
    /// </summary>
    public string AppliedNick { get; set; } = string.Empty;

    /// <summary>
    ///     If the nickname requested by the requester should be applied or not.
    /// </summary>
    public bool AcceptRequestedNick { get; set; } = true;

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
