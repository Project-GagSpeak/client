using GagSpeak.Services.Mediator;
using ImGuiNET;

namespace GagSpeak.UI.Orders;

public class OrdersAssigner
{
    private readonly ILogger<OrdersAssigner> _logger;
    private readonly GagspeakMediator _mediator;


    public OrdersAssigner(ILogger<OrdersAssigner> logger, GagspeakMediator mediator,
        CkGui uiShared)
    {
        _logger = logger;
        _mediator = mediator;

    }

    public void DrawOrderAssignerPanel()
    {
        ImGui.Text("Order Assigner Panel\n(Still Under Development during Open Beta)");
    }
}
