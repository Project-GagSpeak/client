using GagSpeak.CkCommons.Widgets;

namespace GagSpeak.CkCommons.Gui.Components;

public class WardrobeTabs : ImageTabBar<WardrobeTabs.SelectedTab>
{
    public enum SelectedTab
    {
        MyRestraints,
        MyRestrictions,
        MyGags,
        MyCursedLoot,
    }

    public WardrobeTabs()
    { }
}
