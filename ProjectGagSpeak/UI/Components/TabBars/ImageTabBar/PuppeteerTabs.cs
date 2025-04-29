using GagSpeak.CkCommons.Widgets;

namespace GagSpeak.CkCommons.Gui.Components;

public class PuppeteerTabs : ImageTabBar<PuppeteerTabs.SelectedTab>
{
    // Look, its hard to come up with a proper way to word this, it's very confusing in technical terms.
    public enum SelectedTab
    {
        VictimGlobal,
        VictimUnique,
        ControllerUnique,
    }

    public PuppeteerTabs()
    { }

}
