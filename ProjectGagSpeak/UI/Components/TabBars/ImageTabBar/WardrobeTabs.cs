using CkCommons.Widgets;

namespace GagSpeak.Gui.Components;

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
