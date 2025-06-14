using GagSpeak.State.Models;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data;
using ImGuiNET;
using System.Timers;
using GagSpeak.CkCommons.Gui;
using GagSpeak.PlayerClient;

namespace GagSpeak.Services.Controller;

/// <summary> Manages the rendering of a blindfold overlay onto your screen. </summary>
public class BlindfoldService
{
    private readonly MainConfig _config;
    private readonly CosmeticService _renderService;

    // Overlay control.
    private readonly System.Timers.Timer _blindfoldAnimationTimer;

    // Blindfold Information
    private UserData? _applier = null;
    private BlindfoldRestriction? _appliedItem = null;

    // Display Corners (Currently unused, but useful in hypno)
    private readonly Vector2[] _blindfoldUV = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];

    public bool IsBlindfolded => _appliedItem is not null;
    public bool AnimationFinished = false;

    public BlindfoldService(MainConfig config, CosmeticService renderService)
    {
        _config = config;
        _renderService = renderService;

        // Initialize the timer for the blindfold animation
        _blindfoldAnimationTimer = new System.Timers.Timer(int.MaxValue) { AutoReset = false };
        _blindfoldAnimationTimer.Elapsed += OnAnimationFinished;
    }

    private void OnAnimationFinished(object? sender, ElapsedEventArgs e)
    {
        AnimationFinished = true;
    }

    public void ApplyBlindfold(BlindfoldRestriction appliedBlindfold, UserData enactor)
    {
        // Set the applied blindfold and enactor
        _appliedItem = appliedBlindfold;
        _applier = enactor;

        AnimationFinished = false;

        // Start the timer for the blindfold animation
        _blindfoldAnimationTimer.Interval = 3000;
        _blindfoldAnimationTimer.Start();
    }

    public void RemoveBlindfold(UserData enactor)
    {
        // Could add a thing where only specific people could remove if we want, but otherwise dont worry about it.
        _appliedItem = null;
        _applier = null;

        AnimationFinished = false;
    }

    public void DrawBlindfoldOverlay()
    {
        if (_appliedItem is null)
            return;

        var blindfoldImage = _renderService.GetImageMetadataPath(ImageDataType.Blindfolds, _appliedItem.Properties.OverlayPath);
        if (blindfoldImage is null)
            return;

        // Fetch the windows foreground drawlist. This ensures that we do not conflict drawlist layers, with ChatTwo, or other windows.
        var foregroundList = ImGui.GetForegroundDrawList();

        // Screen positions
        var screenSize = ImGui.GetIO().DisplaySize;
        var screenCenter = screenSize / 2;

        // Display the blindfold to the screen. (Don't worry about UV's just yet.)
        foregroundList.AddImage(
            blindfoldImage.ImGuiHandle,
            Vector2.Zero,
            screenSize,
            Vector2.Zero,
            Vector2.One,
            CkGui.Color(new Vector4(1.0f, 1.0f, 1.0f, _config.Current.BlindfoldMaxOpacity))
        );
    }
}

