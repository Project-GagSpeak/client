using GagSpeak.Services.Mediator;
using ImGuiNET;

namespace GagSpeak.UI.UiOrders;
public class OrdersViewActive
{
    private readonly ILogger<OrdersViewActive> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly UiSharedService _uiSharedService;

    public OrdersViewActive(ILogger<OrdersViewActive> logger, GagspeakMediator mediator,
        UiSharedService uiSharedService)
    {
        _logger = logger;
        _mediator = mediator;
        _uiSharedService = uiSharedService;
    }

    public void DrawActiveOrdersPanel()
    {
        ImGui.Text("My Active Orders\n(Still Under Development during Open Beta)");
    }
}
