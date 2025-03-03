using GagSpeak.Services.Mediator;
using ImGuiNET;

namespace GagSpeak.UI.Orders;
public class OrdersViewActive
{
    private readonly ILogger<OrdersViewActive> _logger;
    private readonly GagspeakMediator _mediator;


    public OrdersViewActive(ILogger<OrdersViewActive> logger, GagspeakMediator mediator,
        CkGui uiShared)
    {
        _logger = logger;
        _mediator = mediator;

    }

    public void DrawActiveOrdersPanel()
    {
        ImGui.Text("My Active Orders\n(Still Under Development during Open Beta)");
    }
}
