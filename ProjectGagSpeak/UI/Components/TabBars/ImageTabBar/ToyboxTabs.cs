using CkCommons.Widgets;

namespace GagSpeak.Gui.Components;

public class ToyboxTabs : ImageTabBar<ToyboxTabs.SelectedTab>
{
    // Look, its hard to come up with a proper way to word this, it's very confusing in technical terms.
    public enum SelectedTab
    {
        BuzzToys,
        VibeLobbies,
        Patterns,
        Alarms,
        Triggers
    }

    public ToyboxTabs()
    { }

}
