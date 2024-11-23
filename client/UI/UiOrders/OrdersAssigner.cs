using GagSpeak.Services.Mediator;
using ImGuiNET;

namespace GagSpeak.UI.UiOrders;

public class OrdersAssigner
{
    private readonly ILogger<OrdersAssigner> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly UiSharedService _uiSharedService;

    public OrdersAssigner(ILogger<OrdersAssigner> logger, GagspeakMediator mediator,
        UiSharedService uiSharedService)
    {
        _logger = logger;
        _mediator = mediator;
        _uiSharedService = uiSharedService;
    }

    public void DrawOrderAssignerPanel()
    {
        ImGui.Text("Order Assigner Panel\n(Still Under Development during Open Beta)");
    }
}
