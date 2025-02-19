using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Components;
using GagSpeak.Utils;
using ImGuiNET;

namespace GagSpeak.UI.Orders;

public class OrdersUI : WindowMediatorSubscriberBase
{
    private readonly OrderTabs _tabMenu;
    private readonly OrdersViewActive _activePanel;
    private readonly OrdersCreator _creatorPanel;
    private readonly OrdersAssigner _assignerPanel;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;

    public OrdersUI(ILogger<OrdersUI> logger, GagspeakMediator mediator, 
        OrdersViewActive activePanel, OrdersCreator creatorPanel, 
        OrdersAssigner assignerPanel, CosmeticService cosmetics,
        UiSharedService uiShared) : base(logger, mediator, "Orders UI")
    {
        _activePanel = activePanel;
        _creatorPanel = creatorPanel;
        _assignerPanel = assignerPanel;
        _cosmetics = cosmetics;
        _uiShared = uiShared;

        _tabMenu = new OrderTabs(_uiShared);
        // define initial size of window and to not respect the close hotkey.
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(550, float.MaxValue)
        };
        RespectCloseHotkey = false;
    }
    // perhaps migrate the opened selectable for the UIShared service so that other trackers can determine if they should refresh / update it or not.
    // (this is not yet implemented, but we can modify it later when we need to adapt)

    private bool ThemePushed = false;
    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));

            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        // get information about the window region, its item spacing, and the topleftside height.
        var region = ImGui.GetContentRegionAvail();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var topLeftSideHeight = region.Y;
        _tabMenu.Draw(region.X);
        // display right half viewport based on the tab selection
        using (var rightChild = ImRaii.Child($"###OrdersRightSide", Vector2.Zero, false))
        {
            switch (_tabMenu.TabSelection)
            {
                case OrderTabs.SelectedTab.ActiveOrders:
                    _activePanel.DrawActiveOrdersPanel();
                    break;
                case OrderTabs.SelectedTab.OrderCreator:
                    _creatorPanel.DrawOrderCreatorPanel();
                    break;
                case OrderTabs.SelectedTab.OrderMonitor:
                    _assignerPanel.DrawOrderAssignerPanel();
                    break;
                default:
                    break;
            };
        }
    }
}
