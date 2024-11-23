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
            MigrationsTabs.Tabs.MigrateRestraints => "Migrate your created Restraints from Old GagSpeak to the new GagSpeak.",
            MigrationsTabs.Tabs.TransferGags => "Import your Gag Data stored on another GagSpeak Profile.",
            MigrationsTabs.Tabs.TransferRestraints => "Import your Restraints stored on another GagSpeak Profile.",
            MigrationsTabs.Tabs.TransferCursedLoot => "Import your Cursed Loot stored on another GagSpeak Profile.",
            MigrationsTabs.Tabs.TransferTriggers => "Import your Triggers stored on another GagSpeak Profile.",
            MigrationsTabs.Tabs.TransferAlarms => "Import your Alarms stored on another GagSpeak Profile.",
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
