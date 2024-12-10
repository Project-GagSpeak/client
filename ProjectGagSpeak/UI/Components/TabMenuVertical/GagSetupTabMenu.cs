namespace GagSpeak.UI.Components;

/// <summary> Tab Menu for the GagSetup UI </summary>
public class GagSetupTabMenu : TabMenuBase<GagSetupTabs.Tabs>
{
    public GagSetupTabMenu(UiSharedService uiShared) : base(uiShared) { }

    protected override string GetTabDisplayName(GagSetupTabs.Tabs tab) => GagSetupTabs.GetTabName(tab);
    protected override bool IsTabDisabled(GagSetupTabs.Tabs tab) => tab == GagSetupTabs.Tabs.LockPicker;
    protected override string GetTabTooltip(GagSetupTabs.Tabs tab)
    {
        return tab switch
        {
            GagSetupTabs.Tabs.ActiveGags => "View information of your currently active gags and locks.",
            GagSetupTabs.Tabs.LockPicker => "A WIP Concept that is questionable on if it will ever be added.--SEP--WIP During Open Beta.",
            GagSetupTabs.Tabs.GagStorage => "Customize the appearance and properties of your gags.",
            _ => string.Empty,
        };
    }
}

public static class GagSetupTabs
{
    public enum Tabs
    {
        ActiveGags,
        LockPicker,
        GagStorage,
    }

    public static string GetTabName(Tabs tab)
    {
        return tab switch
        {
            Tabs.ActiveGags => "Active Gags",
            Tabs.LockPicker => "Lock Picker",
            Tabs.GagStorage => "Gag Storage",
            _ => "None",
        };
    }
}
