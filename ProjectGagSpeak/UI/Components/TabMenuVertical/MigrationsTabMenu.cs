namespace GagSpeak.UI.Components;

/// <summary> Tab Menu for the GagSetup UI </summary>
public class MigrationsTabMenu : TabMenuBase<MigrationsTabs.Tabs>
{
    public MigrationsTabMenu(UiSharedService uiShared) : base(uiShared) { }

    protected override string GetTabDisplayName(MigrationsTabs.Tabs tab) => MigrationsTabs.GetTabName(tab);
    protected override string GetTabTooltip(MigrationsTabs.Tabs tab)
    {
        return tab switch
        {
            MigrationsTabs.Tabs.MigrateRestraints => "Migrate Restraint Sets from old to new GagSpeak.",
            MigrationsTabs.Tabs.TransferGags => "Import saved gag data from another GagSpeak profile.",
            MigrationsTabs.Tabs.TransferRestraints => "Import saved restraint data from another GagSpeak profile.",
            MigrationsTabs.Tabs.TransferCursedLoot => "Import saved Cursed Loot data from another GagSpeak profile.",
            MigrationsTabs.Tabs.TransferTriggers => "Import saved trigger data from another GagSpeak profile.",
            MigrationsTabs.Tabs.TransferAlarms => "Import saved alarm data from another GagSpeak profile.",
            _ => string.Empty,
        };
    }
}

public static class MigrationsTabs
{
    public enum Tabs
    {
        MigrateRestraints,
        TransferGags,
        TransferRestraints,
        TransferCursedLoot,
        TransferTriggers,
        TransferAlarms,
    }

    public static string GetTabName(Tabs tab)
    {
        return tab switch
        {
            Tabs.MigrateRestraints => "Old GagSpeak",
            Tabs.TransferGags => "Gag Storage",
            Tabs.TransferRestraints => "Restraint Sets",
            Tabs.TransferCursedLoot => "Cursed Loot",
            Tabs.TransferTriggers => "Triggers",
            Tabs.TransferAlarms => "Alarms",
            _ => "None",
        };
    }
}
