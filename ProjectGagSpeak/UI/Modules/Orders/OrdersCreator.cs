using GagSpeak.Services.Mediator;
using ImGuiNET;

namespace GagSpeak.UI.Orders;
public class OrdersCreator
{
    private readonly ILogger<OrdersCreator> _logger;
    private readonly GagspeakMediator _mediator;


    public OrdersCreator(ILogger<OrdersCreator> logger, GagspeakMediator mediator,
        CkGui uiShared)
    {
        _logger = logger;
        _mediator = mediator;

    }

    public void DrawOrderCreatorPanel()
    {
        ImGui.Text("Order Creator\n(Still Under Development during Open Beta)");
    }
}
