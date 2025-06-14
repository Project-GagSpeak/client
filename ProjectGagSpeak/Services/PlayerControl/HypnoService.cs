using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using ImGuiNET;
using System.Timers;
using GagSpeak.CkCommons.Gui;

namespace GagSpeak.Services.Controller;

/// <summary> Manages the rendering of a hypnosis overlay onto your screen. </summary>
public class HypnoService
{
    private readonly OnFrameworkService _frameworkUtils;
    private readonly CosmeticService _renderService;

    // Overlay control.
    private readonly System.Timers.Timer _textDisplayTimer;
    private string _currentText = string.Empty;
    private float _currentRotation;
    private float _spiralScale = 1f;
    private int _lastTextIdx;

    // Hypno Restriction Info
    private UserData? _applier = null;
    private HypnoticRestriction? _appliedItem = null;

    // Display Corners.
    private readonly Vector2[] _hypnoUV = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];

    // Effect Attributes. (Speed, Color, Text, TextColor, Attributes, ext)
    private HypnoticEffect? _effect => _appliedItem?.Properties.Effect;

    // Helper Information.
    public bool IsHypnotized => _appliedItem is not null;

    public HypnoService(OnFrameworkService framework, CosmeticService render)
    {
        _frameworkUtils = framework;
        _renderService = render;

        // Initialize the timer for the blindfold animation
        _textDisplayTimer = new System.Timers.Timer(int.MaxValue) { AutoReset = true };
        _textDisplayTimer.Elapsed += ToNextPhrase;
    }

    public void ApplyHypnosisEffect(HypnoticRestriction hypnoRestriction, UserData enactor)
    {
        // Set the applied hypno restriction and enactor
        _appliedItem = hypnoRestriction;
        _applier = enactor;

        // Initialize the Text Display Cycle.
        _textDisplayTimer.Interval = hypnoRestriction.Properties.Effect.TextCycleSpeed;
        _textDisplayTimer.Start();
    }

    public void RemoveHypnosisEffect()
    {
        _appliedItem = null;
        _applier = null;
    }

    public void DrawHypnoEffect()
    {
        if (_appliedItem is null || _effect is null)
            return;

        if (_renderService.GetImageMetadataPath(ImageDataType.Hypnosis, _appliedItem.Properties.OverlayPath) is not { } hypnoImage)
            return;

        // Recalculate the necessary cycle speed that we should need for the rotation (may need optimizations later)
        var speed = _effect.SpinSpeed;
        _currentRotation += _frameworkUtils.GetUpdateDelta().Milliseconds * speed;
        _currentRotation %= MathF.PI * 2f;

        // Fetch the windows foreground drawlist to avoid conflict with other UI's & be layered ontop.
        var foregroundList = ImGui.GetForegroundDrawList();

        // Screen positions
        var screenSize = ImGui.GetIO().DisplaySize;
        var screenCenter = screenSize / 2;

        // time for rotation maths
        var cos = MathF.Cos(_currentRotation);
        var sin = MathF.Sin(_currentRotation);

        var corners = new[]
        {
            new Vector2(-hypnoImage.Width, -hypnoImage.Height),
            new Vector2(hypnoImage.Width, -hypnoImage.Height),
            new Vector2(hypnoImage.Width, hypnoImage.Height),
            new Vector2(-hypnoImage.Width, hypnoImage.Height)
        };

        var rotatedBounds = new Vector2[4];
        for (var i = 0; i < corners.Length; i++)
        {
            var x = corners[i].X;
            var y = corners[i].Y;

            rotatedBounds[i] = new Vector2(
                screenCenter.X + (x * cos - y * sin) * _spiralScale,
                screenCenter.Y + (x * sin + y * cos) * _spiralScale
            );
        }

        var imgTint = _effect.TintColor;

        // Display the image stretched to the bounds of the screen and stuff.
        foregroundList.AddImageQuad(
            hypnoImage.ImGuiHandle,
            rotatedBounds[0],
            rotatedBounds[1],
            rotatedBounds[2],
            rotatedBounds[3],
            _hypnoUV[0],
            _hypnoUV[1],
            _hypnoUV[2],
            _hypnoUV[3],
            imgTint);

        // Can set the windows font scale here if we want for the attributes.


        // Display the text in the center of the spiral.
        using (UiFontService.FullScreenFont.Push())
        {
            var textSize = ImGui.CalcTextSize(_currentText);
            ImGui.SetCursorScreenPos(screenCenter - textSize / 2);
            CkGui.ColorText(_currentText, _effect.TextColor);
        }

        // Then can pop the font scale here.

    }

    private void ToNextPhrase(object? sender, ElapsedEventArgs e)
    {
        // abort if there is no effect active.
        if (_appliedItem is null || _effect is null)
            return;

        // Set the current text to nothing and return if no text was assigned.
        if (_effect.DisplayWords.Length <= 0)
        {
            _currentText = string.Empty;
            return;
        }
        // if only one was assigned, just set it to that.
        else if (_effect.DisplayWords.Length == 1)
        {
            _currentText = _effect.DisplayWords[0];
            return;
        }


        if (_effect.Attributes.HasAny(HypnoAttributes.TextIsRandom))
        {
            var randomIndex = Random.Shared.Next(0, _effect.DisplayWords.Length);
            // If the random INDEX (not text) is the same as the current text, pick again.
            // This should only ever occur with more than one index, so it should be fine.
            while (_lastTextIdx == randomIndex)
                randomIndex = Random.Shared.Next(0, _effect.DisplayWords.Length);

            _lastTextIdx = randomIndex;
            _currentText = _effect.DisplayWords[randomIndex];
            return;
        }

        // We should do it sequentially if we are not random.
        _currentText = _effect.DisplayWords[(_currentText.Length + 1) % _effect.DisplayWords.Length];
    }
}

