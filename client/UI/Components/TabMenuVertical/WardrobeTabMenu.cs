using GagSpeak.PlayerData.Handlers;

namespace GagSpeak.UI.Components;

/// <summary> Tab Menu for the Wardrobe UI </summary>
public class WardrobeTabMenu : TabMenuBase<WardrobeTabs.Tabs>
{
    public WardrobeTabMenu(UiSharedService uiShared) : base(uiShared) { }

    protected override string GetTabDisplayName(WardrobeTabs.Tabs tab) => WardrobeTabs.GetTabName(tab);
    protected override bool IsTabDisabled(WardrobeTabs.Tabs tab) => tab == WardrobeTabs.Tabs.StruggleSim;
    protected override string GetTabTooltip(WardrobeTabs.Tabs tab)
    {
        return tab switch
        {
            WardrobeTabs.Tabs.ManageSets => "Overview of current restraint sets.--SEP--Apply, Lock, Unlock, Disable, and Customize them here.",
            WardrobeTabs.Tabs.StruggleSim => "A WIP Concept that is questionable on if it will ever be added.--SEP--WIP During Open Beta.",
            WardrobeTabs.Tabs.CursedLoot => "Remember those Pic Sets & Videos about dungeon Mimics\nthat make people helpless & bound in bad ends?" +
            "--SEP--Yeah, that's effectively what this makes a reality for you.",
            WardrobeTabs.Tabs.ManageMoodles => "See the details of your status and presets, and your pairs status and presets.",
            _ => string.Empty,
        };
    }
}

public static class WardrobeTabs
{
    public enum Tabs
    {
        ManageSets, // view the list of your sets and see combined overview.
        StruggleSim, // for trying to struggle out of your restraints.
        CursedLoot, // Cursed Bondage Loot.
        ManageMoodles, // Manage the permissions for your Moodles.
    }

    public static string GetTabName(Tabs tab)
    {
        return tab switch
        {
            Tabs.ManageSets => "Restraint Sets",
            Tabs.StruggleSim => "Struggle Sim",
            Tabs.CursedLoot => "Cursed Loot",
            Tabs.ManageMoodles => "Moodles",
            _ => "None",
        };
    }
}
