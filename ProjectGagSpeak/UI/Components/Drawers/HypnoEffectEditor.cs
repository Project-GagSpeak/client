using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui.Text;
using System;

namespace GagSpeak.CkCommons.Gui;
public class HypnoEffectEditor : IDisposable
{
    // Effect Constants
    const ImGuiColorEditFlags COLOR_FLAGS = ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoSidePreview;

    // Effect Preview Items.
    private static readonly int[] TextLifeTimeSpeeds = [ 10000, 8000, 6000, 4000, 3000, 2000, 1500, 1000, 800, 600, 400, 300, 250 ];

    // The selected preview values.
    private static uint  _imageColor = 0xFFFFFFFF; // EditorEntry.TintColor
    private static int _transposeLifetime = 2000; // how long it takes to transpose between colors.
    private static float _currentSpinSpeed = 1.0f; // EditorEntry.SpinSpeed
    private static float _currentRotation = 0f;
    private static float _currentZoom = 0.5f;

    // Text Handles.
    private static string _currentText = string.Empty;
    private static int _lastTextIdx;
    private static int _currentTextLifetime = 1000; // in ms, how long the text should be displayed for.
    private static int _textSize = 175; // FontSize base.

    private static int _textFadeInTime = 200; // in ms
    private static int _textFadeOutTime = 200; // in ms
    private static float _currentTextScale = 1f;
    private static float _currentTextOpacity = 1f;

    private uint TextColor => ColorHelpers.ApplyOpacity(EditorEntry?.TextColor ?? 0xFFFFFFFF, _currentTextOpacity);
    private uint TextOutlineColor => ColorHelpers.ApplyOpacity(0xFF000000, _currentTextOpacity);
    private float MaxTextOpacity => CkGui.GetAlpha(EditorEntry?.TextColor ?? 0xFFFFFFFF);

    private readonly Vector2[] _hypnoUV = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
    private bool _open = false;

    private readonly ILogger<HypnoEffectEditor> _logger;
    public HypnoEffectEditor(ILogger<HypnoEffectEditor> logger)
    {
        _logger = logger;
    }

    public void Dispose()
    {
        _textPhraseLoopCTS?.Cancel();
        _transposeColorCTS?.Cancel();

        try
        {
            _textPhraseTask?.Wait(1000);
            _transposeTask?.Wait(1000);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Exception waiting for hypno background tasks to exit: {ex}");
        }

        _textPhraseLoopCTS?.Dispose();
        _transposeColorCTS?.Dispose();

        _textPhraseLoopCTS = null;
        _transposeColorCTS = null;
        _textPhraseTask = null;
        _transposeTask = null;
        EditorEntry = null;
    }

    // The Cached stuff.
    private HypnoticEffect? EditorEntry = null;
    private TagCollection HypnoEffectPhrases = new();
    private Task? _transposeTask;
    private Task? _textPhraseTask;
    private CancellationTokenSource? _textPhraseLoopCTS = new();
    private CancellationTokenSource? _transposeColorCTS = new();
    private async Task TransposeColorLoop()
    {
        _transposeColorCTS?.Cancel();
        _transposeColorCTS = new();
        try
        {
            while (!_transposeColorCTS.IsCancellationRequested)
            {
                // if ever null, break out.
                if (EditorEntry is null)
                    return;

                if(EditorEntry.Attributes.HasAny(HypnoAttributes.TransposeColors))
                {
                    // get the Vector4's for interpolation.
                    Vector4 start = ColorHelpers.RgbaUintToVector4(EditorEntry?.TintColor ?? 0xFF000000);
                    Vector4 target = CkGui.InvertColor(start); // Keep Alpha.

                    await HandleColorTranspose(start, target, _transposeColorCTS.Token);
                    // Swap the colors for the next iteration.
                    await HandleColorTranspose(target, start, _transposeColorCTS.Token);
                }
                else
                {
                    // Just chill out for like a second or 2 doing nothing.
                    await Task.Delay(500, _transposeColorCTS.Token);
                }
            }
        }
        catch (TaskCanceledException) { /* Consume */ }
        catch (Exception ex) { _logger.LogError($"Error in TransposeColorLoop: {ex}"); }
    }
    private async Task HandleColorTranspose(Vector4 start, Vector4 target, CancellationToken token)
    {
        if (EditorEntry is null)
            return;

        var startTime = DateTime.UtcNow;
        var transposeDelta = _transposeLifetime / 2;

        while (!token.IsCancellationRequested)
        {
            float elapsed = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            float t = Math.Clamp(elapsed / transposeDelta, 0f, 1f);

            // Attempt Smoothstep Easing
            t = t * t * (3f - 2f * t);

            Vector4 lerped = new(
                GsExtensions.Lerp(start.X, target.X, t),
                GsExtensions.Lerp(start.Y, target.Y, t),
                GsExtensions.Lerp(start.Z, target.Z, t),
                start.W // Keep Alpha
            );

            _imageColor = ColorHelpers.RgbaVector4ToUint(lerped);

            // If the elapsed time exceeds the transpose lifetime, break out of the loop.
            if (elapsed >= transposeDelta)
                break;

            // Delay for the next frame.
            await Task.Delay(Svc.Framework.UpdateDelta.Milliseconds, token);
        }
    }

    private async Task TextDisplayLoop()
    {
        _textPhraseLoopCTS?.Cancel();
        _textPhraseLoopCTS = new();
        try
        {
            while (!_textPhraseLoopCTS.IsCancellationRequested)
                await HandleTextDisplay(_textPhraseLoopCTS.Token);
        }
        catch (TaskCanceledException) { /* Consume */ }
        catch (Exception) { /* Consume */ }
    }
    private async Task HandleTextDisplay(CancellationToken token)
    {
        // Determine the next phase
        if (EditorEntry?.DisplayWords is not { Length: > 0 } words)
            _currentText = string.Empty;
        else if (words.Length == 1)
        {
            _currentText = words[0];
            _lastTextIdx = 0;
        }
        else if (EditorEntry.Attributes.HasAny(HypnoAttributes.TextDisplayRandom))
        {
            int randomIdx;
            do
            {
                randomIdx = Random.Shared.Next(0, EditorEntry.DisplayWords.Length);
            } while (randomIdx == _lastTextIdx);

            _lastTextIdx = randomIdx;
            _currentText = EditorEntry.DisplayWords[_lastTextIdx];
        }
        else
        {
            _lastTextIdx = (_lastTextIdx + 1) % EditorEntry.DisplayWords.Length;
            _currentText = EditorEntry.DisplayWords[_lastTextIdx];
        }

        // Back out early if we closed out.
        if (EditorEntry is null)
            return;

        // Now we need to process and update the text for the visual state based on our attributes.
        bool doTextFade = EditorEntry.Attributes.HasAny(HypnoAttributes.TextFade);
        bool linearScale = EditorEntry.Attributes.HasAny(HypnoAttributes.LinearTextScale);
        bool randomScale = EditorEntry.Attributes.HasAny(HypnoAttributes.RandomTextScale);

        // Update the current opacity.
        _currentTextOpacity = doTextFade ? 0f : MaxTextOpacity;
        _currentTextScale = randomScale ? (0.75f + Random.Shared.NextSingle() * (1.35f - 0.75f)) : linearScale ? 0.8f : 1f;

        // If Scaling linearily, store the start and end draw regions.
        if (linearScale)
        {
            var sizeBase = GetTextSizeFromFontPtr(UiFontService.FullScreenFontPtr, _currentText) * (_textSize / UiFontService.FullScreenFontPtr.FontSize);
            TextStartOffset = (sizeBase * 0.75f) * 0.5f; // offset from center
            TextEndOffset = (sizeBase * 1.35f) * 0.5f; // offset from center
        }

        PhraseStartTime = DateTime.UtcNow;
        var duration = _currentTextLifetime;

        while (!token.IsCancellationRequested)
        {
            var elapsed = (int)(DateTime.UtcNow - PhraseStartTime).TotalMilliseconds;
            if (elapsed >= duration)
                break;

            var progress = elapsed / (float)duration;

            // Handle Opacity Fadein
            if (doTextFade && elapsed < _textFadeInTime)
                _currentTextOpacity = GsExtensions.EaseInExpo(elapsed / (float)_textFadeInTime);
            // Handle Opacity Fade Out.
            else if (doTextFade && elapsed >= (duration - _textFadeOutTime))
            {
                var fadeOutStart = duration - _textFadeOutTime;
                var value = (elapsed - fadeOutStart) / (float)_textFadeOutTime;
                _currentTextOpacity = MaxTextOpacity * GsExtensions.EaseOutExpo(1f - value);
            }
            // Handle simply displaying the text.
            else
            {
                _currentTextOpacity = MaxTextOpacity;
            }

            // Lighten Stress Load a little bit.
            await Task.Delay(Svc.Framework.UpdateDelta.Milliseconds);
        }

        if (EditorEntry.Attributes.HasAny(HypnoAttributes.SpeedUpOnCycle))
            _ = AccelerateTemporarily(duration);
    }

    private async Task AccelerateTemporarily(int duration)
    {
        var speedUpTime = Math.Clamp(250, 10, duration);
        var currentSpeed = _currentSpinSpeed;
        var newSpeed = _currentSpinSpeed * 2f;
        // quickly accelerate the speed to 3x the current speed, then back to normal 50ms later, combined should take 100ms
        _currentSpinSpeed = newSpeed;
        await Task.Delay(speedUpTime);
        _currentSpinSpeed = currentSpeed; // reset back to the original speed.
    }


    public void SetHypnoEffect(HypnoticEffect effect)
    {
        _textPhraseLoopCTS?.Cancel();
        _transposeColorCTS?.Cancel();

        EditorEntry = effect;

        // Assign and start the Text Phrase loop
        _textPhraseTask = Task.Run(TextDisplayLoop);
        // Assign the color transpose task.
        _transposeTask = Task.Run(TransposeColorLoop);
    }

    private void OnEditorClose()
    {
        _textPhraseLoopCTS?.Cancel();
        _transposeColorCTS?.Cancel();
        // Reset the editor entry.
        EditorEntry = null;
        // Reset the current text.
        _currentText = string.Empty;
        _lastTextIdx = 0;
    }

    /// <summary> Draws the editor. Passes in the original so when we save the editor we can update the original entry. </summary>
    public void DrawPopup(CosmeticService imgFinder, HypnoticOverlay overlay)
    {
        if (EditorEntry is null)
            return;

        if (!ImGui.IsPopupOpen("###HypnoEditModal"))
        {
            _open = true;
            ImGui.OpenPopup("###HypnoEditModal");
        }

        if (ImGui.BeginPopupModal($"Effect Editor###HypnoEditModal", ref _open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            // Draw out the editor contents.
            using (ImRaii.Table("##EditorContainerOuter", 2, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("EditorArea", ImGuiTableColumnFlags.WidthFixed, 440f);
                ImGui.TableSetupColumn("PreviewArea", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var cellWidth = ImGui.GetContentRegionAvail().X;

                DrawEditorArea(cellWidth);
                var editorHeight = ImGui.GetItemRectSize().Y;

                ImGui.Separator();
                using (var c = CkRaii.HeaderChild("Display Text Phrases", new Vector2(cellWidth, CkStyle.GetFrameRowsHeight(3).AddWinPadY()), HeaderFlags.AddPaddingToHeight))
                {
                    using (CkRaii.FramedChildPaddedW("##DisplayPhrases", c.InnerRegion.X, CkStyle.GetFrameRowsHeight(3), CkColor.FancyHeaderContrast.Uint(), DFlags.RoundCornersAll))
                        if (HypnoEffectPhrases.DrawTagsEditor("##EffectPhrases", EditorEntry.DisplayWords, out var newDisplayWords))
                            EditorEntry.DisplayWords = newDisplayWords.ToArray();
                }

                // Next Column,
                ImGui.TableNextColumn();
                try
                {
                    if (imgFinder.GetImageMetadataPath(ImageDataType.Hypnosis, overlay.OverlayPath) is { } img)
                    {
                        var dispSize = DisplayPreviewEffect(editorHeight, img);
                        ImGui.Dummy(dispSize);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error displaying Hypnotic Effect Preview: {ex}");
                }

                ImGui.Separator();
                DrawPreviewManipulations();
            }

            CkGui.SeparatorSpaced(col: CkColor.LushPinkLine.Uint());

            CkGui.SetCursorXtoCenter(CkGui.IconTextButtonSize(FAI.Save, "Save and Close"));
            if (CkGui.IconTextButton(FAI.Save, "Save and Close"))
            {
                _open = false;
                overlay.Effect = EditorEntry ?? new HypnoticEffect();
                OnEditorClose();
            }

            ImGui.EndPopup();
        }

        if (!_open)
            EditorEntry = null;
    }

    private void DrawEditorArea(float width)
    {
        if (EditorEntry is null)
            return;

        using var _ = ImRaii.Group();
        using (var t = ImRaii.Table("HypnoEffectEditTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t)
                return;

            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 110);
            ImGui.TableSetupColumn("Input", ImGuiTableColumnFlags.WidthFixed, width - 110);

            // Spin Speed
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Image Spin Speed");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.DragFloat("##SpinSpeed", ref EditorEntry.SpinSpeed, 0.01f, 0f, 5f, "%.2fx Speed");

            // Text Mode
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Text Display Order");
            ImGui.TableNextColumn();
            var selectedAttributes = (uint)EditorEntry.Attributes;
            var currentMode = EditorEntry.Attributes & HypnoAttributes.TextDisplayMask;

            if (ImGui.RadioButton(HypnoAttributes.TextDisplayOrdered.ToName(), currentMode == HypnoAttributes.TextDisplayOrdered))
                EditorEntry.Attributes = (EditorEntry.Attributes & ~HypnoAttributes.TextDisplayMask) | HypnoAttributes.TextDisplayOrdered;
            CkGui.AttachToolTip(HypnoAttributes.TextDisplayOrdered.ToTooltip());

            ImGui.SameLine();
            if (ImGui.RadioButton(HypnoAttributes.TextDisplayRandom.ToName(), currentMode == HypnoAttributes.TextDisplayRandom))
                EditorEntry.Attributes = (EditorEntry.Attributes & ~HypnoAttributes.TextDisplayMask) | HypnoAttributes.TextDisplayRandom;
            CkGui.AttachToolTip(HypnoAttributes.TextDisplayRandom.ToTooltip());

            // Text Scale Properties
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Text Scaling");
            ImGui.TableNextColumn();
            var scaleMode = EditorEntry.Attributes & HypnoAttributes.ScaleMask;

            if (ImGui.RadioButton("Static", scaleMode == 0))
                EditorEntry.Attributes &= ~HypnoAttributes.ScaleMask;
            CkGui.AttachToolTip("Text should remain the same size.");

            ImGui.SameLine();
            if (ImGui.RadioButton("Grows Overtime", scaleMode == HypnoAttributes.LinearTextScale))
                EditorEntry.Attributes = (EditorEntry.Attributes & ~HypnoAttributes.ScaleMask) | HypnoAttributes.LinearTextScale;
            CkGui.AttachToolTip(HypnoAttributes.LinearTextScale.ToTooltip());

            ImGui.SameLine();
            if (ImGui.RadioButton("Random Scale", scaleMode == HypnoAttributes.RandomTextScale))
                EditorEntry.Attributes = (EditorEntry.Attributes & ~HypnoAttributes.ScaleMask) | HypnoAttributes.RandomTextScale;
            CkGui.AttachToolTip(HypnoAttributes.RandomTextScale.ToTooltip());

            // Text Cycle Speed
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Text Cycle Speed");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.DragInt("##TextLifeTime", ref EditorEntry.TextLifeTime, 0.05f, 50, 5000, "%dms");
            CkGui.AttachToolTip("How frequently the text cycles through the display words.");

            // Other Attributes
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Other Attributes");
            ImGui.TableNextColumn();

            using (var inner = ImRaii.Table("###AttributesTable", 2))
            {
                if (!t) return;

                foreach (var attribute in Enum.GetValues<HypnoAttributes>().Skip(5))
                {
                    if (ImGui.CheckboxFlags(attribute.ToName(), ref selectedAttributes, (uint)attribute))
                        EditorEntry.Attributes ^= attribute;
                    CkGui.AttachToolTip(attribute.ToTooltip());
                    ImGui.TableNextColumn();
                }
            }
        }

        var colorPickerWidth = (width / 2) - ImGui.GetStyle().ItemSpacing.X;

        using (ImRaii.Group())
        {
            CkGui.CenterTextAligned("Hypnotic Effect Colors", colorPickerWidth);
            ImGui.SetNextItemWidth(colorPickerWidth);
            var tintVec = ColorHelpers.RgbaUintToVector4(EditorEntry.TintColor);
            if (ImGui.ColorPicker4("##ImageColor", ref tintVec, COLOR_FLAGS))
            {
                EditorEntry.TintColor = ColorHelpers.RgbaVector4ToUint(tintVec);
                _imageColor = EditorEntry.TintColor; // Update the image color to match the new tint.
            }
        }

        ImGui.SameLine();
        using (ImRaii.Group())
        {
            CkGui.CenterTextAligned("Display Phrase Color", colorPickerWidth);
            ImGui.SetNextItemWidth(colorPickerWidth);
            var textColVec = ColorHelpers.RgbaUintToVector4(EditorEntry.TextColor);
            if (ImGui.ColorPicker4("##TextColor", ref textColVec, COLOR_FLAGS))
            {
                EditorEntry.TextColor = ColorHelpers.RgbaVector4ToUint(textColVec);
            }
        }
    }

    private void DrawPreviewManipulations()
    {
        // Spin Speed Slider 
        ImGui.SliderFloat("Image Spin Speed", ref _currentSpinSpeed, 0.1f, 10f, "%.2fx Speed");

        // Speed Selector Slider (uses only values from PreviewSpeeds)
        var speedIndex = Array.IndexOf(TextLifeTimeSpeeds, _currentTextLifetime);
        if (speedIndex < 0) speedIndex = TextLifeTimeSpeeds.Length - 1;

        // Create a slider that changes between all the selectable values of Preview Speeds. Formatted Display shows correct value.
        if (ImGui.SliderInt("Text Display Speed", ref speedIndex, 0, TextLifeTimeSpeeds.Length - 1, $"{TextLifeTimeSpeeds[speedIndex]}ms Lifetime"))
        {
            _currentTextLifetime = TextLifeTimeSpeeds[speedIndex];
            // Adjust the max fadeout time.
            _textFadeOutTime = Math.Clamp(_textFadeOutTime, 10, _currentTextLifetime / 2);
        }

        // Zoom Slider (0.1 to 20)
        ImGui.SliderFloat("Zoom", ref _currentZoom, 0.05f, 1.5f, "%.2fx Zoom");

        // Text Size Slider (175 - 350)
        ImGui.SliderInt("Text Size", ref _textSize, 175, 350, "%dpx Size");

        if (EditorEntry!.Attributes.HasAny(HypnoAttributes.TextFade))
        {
            // Text Fade In (50 to 1000ms, independent)
            ImGui.SliderInt("Text Fade In", ref _textFadeInTime, 10, 1000, "%dms Delay");

            // Text Fade Out (50 to selected speed)
            var max = _currentTextLifetime / 2;
            ImGui.SliderInt("Text Fade Out", ref _textFadeOutTime, 10, _currentTextLifetime / 2, "%dms Delay");
        }
        if (EditorEntry!.Attributes.HasAny(HypnoAttributes.TransposeColors))
        {
            // Color Transpose Duration
            ImGui.SliderInt("ColorTranspose Duration", ref _transposeLifetime, 500, 5000, "%dms Duration");
        }
    }

    private Vector2 DisplayPreviewEffect(float height, IDalamudTextureWrap hypnoImage)
    {
        if (EditorEntry is null)
            return Vector2.Zero;

        // Recalculate the necessary cycle speed that we should need for the rotation (may need optimizations later)
        var speed = _currentSpinSpeed * 0.001f;
        var direction = EditorEntry.Attributes.HasFlag(HypnoAttributes.InvertDirection) ? -1f : 1f;
        _currentRotation += direction * (Svc.Framework.UpdateDelta.Milliseconds * speed);
        _currentRotation %= MathF.PI * 2f;

        // Fetch the windows foreground drawlist to avoid conflict with other UI's & be layered ontop.
        var drawList = ImGui.GetWindowDrawList();
        var topLeft = ImGui.GetCursorScreenPos();
        // Calculate the size by getting our display size, then scaling it to make the height match the provided height.
        var screenSize = ImGui.GetIO().DisplaySize;
        var scale = height / screenSize.Y;
        screenSize *= scale;

        // Get the center position.
        var center = topLeft + screenSize * 0.5f;

        // time for rotation maths
        var cos = MathF.Cos(_currentRotation);
        var sin = MathF.Sin(_currentRotation);

        // Impacted by zoom factor. (Nessisary for Pulsating)
        var corners = new[]
        {
            new Vector2(-hypnoImage.Width, -hypnoImage.Height) * _currentZoom,
            new Vector2(hypnoImage.Width, -hypnoImage.Height) * _currentZoom,
            new Vector2(hypnoImage.Width, hypnoImage.Height) * _currentZoom,
            new Vector2(-hypnoImage.Width, hypnoImage.Height) * _currentZoom
        };

        var rotatedBounds = new Vector2[4];
        for (var i = 0; i < corners.Length; i++)
        {
            var x = corners[i].X;
            var y = corners[i].Y;

            rotatedBounds[i] = new Vector2(
                center.X + (x * cos - y * sin) * _currentZoom,
                center.Y + (x * sin + y * cos) * _currentZoom
            );
        }

        // So we can account for transposing colors.
        var imgTint = _imageColor;

        // Workaround the popup / windows cliprect and draw it at the correct dimentions.
        drawList.PushClipRect(topLeft, topLeft + screenSize, true);// Display the image stretched to the bounds of the screen and stuff.
        drawList.AddImageQuad(
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
        drawList.PopClipRect();

        if(string.IsNullOrEmpty(_currentText))
            return screenSize;

        // update the scale progress and get target position. Do this in the drawframe so it is in sync.
        UpdateScaleProgress(EditorEntry);
        var targetPos = GetTargetPos(EditorEntry, center);
        
        drawList.OutlinedFontScaled(
            UiFontService.FullScreenFontPtr,
            UiFontService.FullScreenFontPtr.FontSize * FontScale,
            targetPos, 
            _currentText,
            TextColor,
            TextOutlineColor,
            10,
            20);

        return screenSize;  
    }

    private void UpdateScaleProgress(HypnoticEffect effect)
    {
        if (!effect.Attributes.HasAny(HypnoAttributes.LinearTextScale))
            return; // No need to update progress if we are not scaling linearily.

        var elapsed = (int)(DateTime.UtcNow - PhraseStartTime).TotalMilliseconds;
        if (elapsed >= _currentTextLifetime)
            return;

        ScaleProgress = elapsed / (float)_currentTextLifetime;
        _currentTextScale = Math.Clamp(GsExtensions.Lerp(0.75f, 1.35f, ScaleProgress), 0.75f, 1.35f);
    }

    private Vector2 GetTargetPos(HypnoticEffect effect, Vector2 center)
    {
        // if we are doing linear scaling, calculate that.
        if (effect.Attributes.HasAny(HypnoAttributes.LinearTextScale))
            return center - Vector2.Lerp(TextStartOffset, TextEndOffset, ScaleProgress);
        else
        {
            var size = CkGui.CalcFontTextSize(_currentText, UiFontService.FullScreenFont) * FontScale;
            return center - size * 0.5f; // offset from center
        }
    }

    // Because ImGui.CalcTextSize is unreliable for larger font scales.
    public static unsafe Vector2 GetTextSizeFromFontPtr(ImFontPtr fontPtr, string text)
    {
        if (!fontPtr.IsLoaded() || string.IsNullOrEmpty(text))
            return Vector2.Zero;

        float width = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var current = text[i];
            var glyph = fontPtr.FindGlyph(current);
            if (glyph.NativePtr is null)
                continue;

            width += glyph.AdvanceX;

            // handle final char.
            if (i + 1 < text.Length)
            {
                var next = text[i + 1];
                width += fontPtr.GetDistanceAdjustmentForPair(current, next);
            }
        }

        return new Vector2(width, fontPtr.FontSize);
    }

    private float FontScale => UiFontService.FullScreenFont.Available
        ? (_textSize / UiFontService.FullScreenFontPtr.FontSize) * _currentTextScale
        : _currentTextScale;

    private Vector2 TextStartOffset = Vector2.Zero;
    private Vector2 TextEndOffset = Vector2.Zero;
    private DateTime PhraseStartTime = DateTime.MinValue;
    private float ScaleProgress = 0;
}
