using GagSpeak.Services.Mediator;
using ImGuiNET;

namespace GagSpeak.UI.UiOrders;
public class OrdersCreator
{
    private readonly ILogger<OrdersCreator> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly UiSharedService _uiSharedService;

    public OrdersCreator(ILogger<OrdersCreator> logger, GagspeakMediator mediator,
        UiSharedService uiSharedService)
    {
        _logger = logger;
        _mediator = mediator;
        _uiSharedService = uiSharedService;
    }

    public void DrawOrderCreatorPanel()
    {
        ImGui.Text("Order Creator\n(Still Under Development during Open Beta)");
    }
}
