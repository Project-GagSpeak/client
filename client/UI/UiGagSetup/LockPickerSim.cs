using GagSpeak.Services.Mediator;
using GagSpeak.UI.Simulation;
using ImGuiNET;
using System.Numerics;

namespace GagSpeak.UI.UiGagSetup;

public class LockPickerSim : DisposableMediatorSubscriberBase
{
    private readonly UiSharedService _uiShared;
    private readonly LockPickingMinigame _lockpickSim;

    public LockPickerSim(ILogger<LockPickerSim> logger,
        GagspeakMediator mediator, UiSharedService uiShared,
        LockPickingMinigame lockpickSim) : base(logger, mediator)
    {
        _uiShared = uiShared;
        _lockpickSim = lockpickSim;
    }

    // Sample data for pin positions and targets
    public List<float> pinOffsets = new List<float> { 0, 0, 0, 0, 0 };
    public List<float> pinTargetOffsets = new List<float> { 80, 60, 90, 50, 70 };

    public void DrawLockPickingSim()
    {
        _uiShared.BigText("Very Likely to not be added. Idk");
        _lockpickSim.DrawLockPickingUI();
    }
}
