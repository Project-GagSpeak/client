namespace GagSpeak.UI.Components;

/// <summary> Tab Menu for the GagSetup UI </summary>
public class PublicationsTabMenu : TabMenuBase<PublicationsTabs.Tabs>
{
    public PublicationsTabMenu(UiSharedService uiShared) : base(uiShared) { }

    protected override string GetTabDisplayName(PublicationsTabs.Tabs tab) => PublicationsTabs.GetTabName(tab);
}

public static class PublicationsTabs
{
    public enum Tabs
    {
        ManagePatterns,
        ManageMoodles,
    }

    public static string GetTabName(Tabs tab)
    {
        return tab switch
        {
            Tabs.ManagePatterns => "My Patterns",
            Tabs.ManageMoodles => "My Moodles",
            _ => "None",
        };
    }
}
