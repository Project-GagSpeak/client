using GagSpeak.Services.Mediator;
using ImGuiNET;

namespace GagSpeak.UI.Orders;
public class OrdersCreator
{
    private readonly ILogger<OrdersCreator> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly UiSharedService _uiShared;

    public OrdersCreator(ILogger<OrdersCreator> logger, GagspeakMediator mediator,
        UiSharedService uiShared)
    {
        _logger = logger;
        _mediator = mediator;
        _uiShared = uiShared;
    }

    public void DrawOrderCreatorPanel()
    {
        ImGui.Text("Order Creator\n(Still Under Development during Open Beta)");
    }
}
