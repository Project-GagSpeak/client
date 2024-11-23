namespace GagSpeak.UI.Components;

/// <summary> Tab Menu for the GagSetup UI </summary>
public class ToyboxTabMenu : TabMenuBase<ToyboxTabs.Tabs>
{
    public ToyboxTabMenu(UiSharedService uiShared) : base(uiShared) { }

    protected override string GetTabDisplayName(ToyboxTabs.Tabs tab) => ToyboxTabs.GetTabName(tab);
    protected override bool IsTabDisabled(ToyboxTabs.Tabs tab) => tab == ToyboxTabs.Tabs.VibeServer;
    protected override string GetTabTooltip(ToyboxTabs.Tabs tab)
    {
        return tab switch
        {
            ToyboxTabs.Tabs.ToyOverview => "Manage connections to your sex toys or simulated vibrator options.",
            ToyboxTabs.Tabs.VibeServer => "Create, invite, or join other private rooms, control other pairs sex toys live.--SEP--Under Construction during Open Beta",
            ToyboxTabs.Tabs.PatternManager => "Manage or upload your patterns.",
            ToyboxTabs.Tabs.TriggerManager => "Manage your triggers.",
            ToyboxTabs.Tabs.AlarmManager => "Manage your Alarms.",
            _ => string.Empty,
        };
    }
}

public static class ToyboxTabs
{
    public enum Tabs
    {
        ToyOverview, // manage connections, active toys, audio sounds, ext.
        VibeServer, // connect to hub, create groups, invite others, access instanced vibrators for each
        PatternManager, // manage, create, or send patterns.
        TriggerManager, // create a new trigger.
        AlarmManager, // manage, create, or send alarms.
    }

    public static string GetTabName(Tabs tab)
    {
        return tab switch
        {
            Tabs.ToyOverview => "Overview",
            Tabs.VibeServer => "Vibe Server",
            Tabs.PatternManager => "Patterns",
            Tabs.TriggerManager => "Triggers",
            Tabs.AlarmManager => "Alarms",
            _ => "None",
        };
    }
}
