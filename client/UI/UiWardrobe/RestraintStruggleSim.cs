using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.Simulation;
using ImGuiNET;

namespace GagSpeak.UI.UiWardrobe;

public class StruggleSim : DisposableMediatorSubscriberBase
{
    private readonly UiSharedService _uiSharedService;
    private readonly WardrobeHandler _handler;
    private readonly StruggleStamina _stamina;
    private readonly ProgressBar _progressBar;
    private readonly StruggleItem _currentItem;

    public StruggleSim(ILogger<StruggleSim> logger,
        GagspeakMediator mediator, UiSharedService uiSharedService,
        WardrobeHandler handler, StruggleStamina stamina,
        ProgressBar progressBar, StruggleItem currentItem) : base(logger, mediator)
    {
        _uiSharedService = uiSharedService;
        _handler = handler;
        _stamina = stamina;
        _progressBar = progressBar;
        _currentItem = currentItem;
    }

    public void DrawStruggleSim()
    {
        ImGui.Text("Struggle Simulation");

        // Stamina Bar
        ImGui.Text("Stamina");
        ImGui.ProgressBar(_stamina.CurrentStamina / 100.0f, new System.Numerics.Vector2(-1, 0), $"{_stamina.CurrentStamina}/100");

        // Progress Bar
        ImGui.Text("Escape Progress");
        ImGui.ProgressBar(_progressBar.Progress, new System.Numerics.Vector2(-1, 0), $"{_progressBar.Progress * 100}%");

        // Task Buttons (for now, let's use simple button interactions)
        if (ImGui.Button("Attempt Struggle") && _stamina.HasStamina(2))
        {
            _stamina.UseStamina(2);

            // Simulate success/failure logic
            Random random = new Random();
            bool success = random.NextDouble() > 0.5 - (_currentItem.Tightness * 0.2);  // Higher tightness reduces success rate

            if (success)
            {
                _progressBar.IncreaseProgress(0.1f);  // Increase progress by 10%
                _currentItem.IncreaseWear(0.05f);    // Increase wear by 5%
            }
            else
            {
                _stamina.UseStamina(5);  // Fail, lose extra stamina
            }
        }

        // Regenerate stamina over time
        _stamina.RegenerateStamina();

        // Drain progress bar over time
        _progressBar.Drain();

        // Check win/loss conditions
        if (_progressBar.IsFilled)
        {
            ImGui.Text("You escaped!");
        }
        else if (_stamina.CurrentStamina <= 0)
        {
            ImGui.Text("You failed to escape.");
        }

        _uiSharedService.BigText("WIP Test Idea. Has no actual Functionality.");
    }
}
