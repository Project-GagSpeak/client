namespace GagSpeak.UI.Components;

/// <summary> Tab Menu for the GagSetup UI </summary>
public class OrdersTabMenu : TabMenuBase<OrdersTabs.Tabs>
{
    public OrdersTabMenu(UiSharedService uiShared) : base(uiShared) { }

    protected override string GetTabDisplayName(OrdersTabs.Tabs tab) => OrdersTabs.GetTabName(tab);
}

public static class OrdersTabs
{
    public enum Tabs
    {
        ActiveOrders, // Displays the currently active orders of the client.
        CreateOrder, // creates order presets that can be assigned to others
        AssignOrder, // interface for the creation of an order for another user pair.
    }

    public static string GetTabName(Tabs tab)
    {
        return tab switch
        {
            Tabs.ActiveOrders => "Active Orders",
            Tabs.CreateOrder => "Create Order",
            Tabs.AssignOrder => "Assign Order",
            _ => "None",
        };
    }
}
