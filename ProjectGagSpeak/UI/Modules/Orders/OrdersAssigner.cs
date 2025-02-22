using GagSpeak.Services.Mediator;
using ImGuiNET;

namespace GagSpeak.UI.Orders;

public class OrdersAssigner
{
    private readonly ILogger<OrdersAssigner> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly UiSharedService _uiShared;

    public OrdersAssigner(ILogger<OrdersAssigner> logger, GagspeakMediator mediator,
        UiSharedService uiShared)
    {
        _logger = logger;
        _mediator = mediator;
        _uiShared = uiShared;
    }

    public void DrawOrderAssignerPanel()
    {
        ImGui.Text("Order Assigner Panel\n(Still Under Development during Open Beta)");
    }
}
