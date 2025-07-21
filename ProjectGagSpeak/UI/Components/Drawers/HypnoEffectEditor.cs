using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Controller;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;
public class HypnoEffectEditor : IDisposable
{
    // Effect Constants
    const ImGuiColorEditFlags COLOR_FLAGS = ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoSidePreview;

    // Editor Control.
    private Task?                    _colorTask;
    private Task?                    _textTask;
    private CancellationTokenSource? _tasksCTS;
    private TagCollection            _displayTextEditor;

    // Locals.
    private bool            _isOpen = false;
    private HypnoticEffect? _effect = null;
    private HypnosisState   _activeState = new();

    private readonly CosmeticService _display;
    public HypnoEffectEditor(CosmeticService display)
    {
        _display = display;

        _tasksCTS = new CancellationTokenSource();
        _displayTextEditor = new TagCollection();
    }

    public void Dispose()
    {
        // Halt any background tasks.
        _tasksCTS?.Cancel();
        try
        {
            _colorTask?.Wait(1000);
            _textTask?.Wait(1000);
        }
        catch (Exception ex)
        {
            Svc.Logger.Warning("Exception waiting for hypno background tasks to exit: " + ex);
        }

        _tasksCTS?.Dispose();
        _effect = null;
    }

    public void SetHypnoEffect(HypnoticEffect effect)
    {
        // Halt any running tasks.
        _tasksCTS?.Cancel();
        // Set the new effect.
        _effect = new(effect);
        _activeState = new HypnosisState { ImageColor = _effect.ImageColor };

        // Assign the new tasks for the display editor.
        _tasksCTS = new CancellationTokenSource();
        _colorTask = HypnoService.ColorTransposeTask(_effect, _activeState, _tasksCTS.Token);
        _textTask = HypnoService.TextDisplayTask(_effect, _activeState, _tasksCTS.Token);
    }

    private void OnEditorClose()
    {
        // Cancel any running tasks.
        _tasksCTS?.Cancel();
        _effect = null;
        _activeState = new HypnosisState();
    }

    /// <summary> Draws the editor. Passes in the original so when we save the editor we can update the original entry. </summary>
    public void DrawPopup(CosmeticService imgFinder, HypnoticOverlay overlay)
    {
        if (_effect is null)
            return;

        if (!ImGui.IsPopupOpen("###HypnoEditModal"))
        {
            _isOpen = true;
            ImGui.OpenPopup("###HypnoEditModal");
        }

        if (ImGui.BeginPopupModal($"Effect Editor###HypnoEditModal", ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            // Draw out the editor contents.
            using (ImRaii.Table("##EditorContainerOuter", 2, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.BordersInner))
            {
                ImGui.TableSetupColumn("EditorArea", ImGuiTableColumnFlags.WidthFixed, 440f);
                ImGui.TableSetupColumn("PreviewArea", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var cellWidth = ImGui.GetContentRegionAvail().X;

                DrawEditorArea(cellWidth);
                var editorHeight = ImGui.GetItemRectSize().Y;


                ImGui.TableNextColumn();
                var size = DisplayPreviewEffect(editorHeight, overlay.OverlayPath);
                ImGui.Dummy(size);

                // Draw the phrase editors.
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var height = (size.X / 3) - ImGui.GetFrameHeight() * 2;
                using (var c = CkRaii.HeaderChild("Display Text Phrases", new Vector2(cellWidth, height.AddWinPadY()), HeaderFlags.AddPaddingToHeight))
                {
                    using (CkRaii.FramedChildPaddedW("##DisplayPhrases", c.InnerRegion.X, height, CkColor.FancyHeaderContrast.Uint(), DFlags.RoundCornersAll))
                    {
                        if (_displayTextEditor.DrawTagsEditor("##EffectPhrases", _effect.DisplayMessages, out var newDisplayWords))
                            _effect.DisplayMessages = newDisplayWords.ToArray();
                        
                        if (_displayTextEditor.DrawHelpButtons(_effect.DisplayMessages, out var newWords, true))
                            _effect.DisplayMessages = newWords.ToArray();
                    }
                }

                // Color Selections
                ImGui.TableNextColumn();
                DrawColorSections(size.X);
            }

            CkGui.SeparatorSpacedColored(col: CkColor.LushPinkLine.Uint());

            CkGui.SetCursorXtoCenter(CkGui.IconTextButtonSize(FAI.Save, "Save and Close"));
            if (CkGui.IconTextButton(FAI.Save, "Save and Close"))
            {
                _isOpen = false;
                overlay.Effect = new HypnoticEffect(_effect);
                OnEditorClose();
            }

            ImGui.EndPopup();
        }

        if (!_isOpen)
            _effect = null;
    }

    private void DrawEditorArea(float width)
    {
        if (_effect is null)
            return;

        using var _ = ImRaii.Group();
        using (var t = ImRaii.Table("HypnoEffectEditTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t)
                return;

            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 110);
            ImGui.TableSetupColumn("Input", ImGuiTableColumnFlags.WidthFixed, width - 110);

            // General Attributes
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Attributes");
            ImGui.TableNextColumn();
            var selectedAttributes = (uint)_effect.Attributes;
            using (var inner = ImRaii.Table("###AttributesTable", 2))
            {
                if (!inner) return;

                foreach (var attribute in Enum.GetValues<HypnoAttributes>().Skip(5))
                {
                    if (ImGui.CheckboxFlags(attribute.ToName(), ref selectedAttributes, (uint)attribute))
                        _effect.Attributes ^= attribute;
                    CkGui.AttachToolTip(attribute.ToTooltip());
                    ImGui.TableNextColumn();
                }
            }

            // Spin Speed
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Image Spin Speed");
            ImGui.TableNextColumn();
            var spinRef = _effect.SpinSpeed;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.SliderFloat("##SpinSpeed", ref spinRef, HypnoService.SPIN_SPEED_MIN, HypnoService.SPIN_SPEED_MAX, "%.2fx"))
            {
                _effect.SpinSpeed = spinRef;
                _activeState.SpinSpeed = _effect.SpinSpeed; // Update the active state for the preview.
            }

            // Zoom Depth
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Image Zoom Depth");
            ImGui.TableNextColumn();
            var zoomRef = _effect.ZoomDepth;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.SliderFloat("##ZoomDepth", ref zoomRef, HypnoService.ZOOM_MIN, HypnoService.ZOOM_MAX, "%.2fx"))
                _effect.ZoomDepth = zoomRef;

            // Text Mode
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Text Display Order");
            ImGui.TableNextColumn();
            var currentMode = _effect.Attributes & HypnoAttributes.TextDisplayMask;

            if (ImGui.RadioButton(HypnoAttributes.TextDisplayOrdered.ToName(), currentMode == HypnoAttributes.TextDisplayOrdered))
                _effect.Attributes = (_effect.Attributes & ~HypnoAttributes.TextDisplayMask) | HypnoAttributes.TextDisplayOrdered;
            CkGui.AttachToolTip(HypnoAttributes.TextDisplayOrdered.ToTooltip());

            ImGui.SameLine();
            if (ImGui.RadioButton(HypnoAttributes.TextDisplayRandom.ToName(), currentMode == HypnoAttributes.TextDisplayRandom))
                _effect.Attributes = (_effect.Attributes & ~HypnoAttributes.TextDisplayMask) | HypnoAttributes.TextDisplayRandom;
            CkGui.AttachToolTip(HypnoAttributes.TextDisplayRandom.ToTooltip());

            // Text Scale Properties
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Text Scaling");
            ImGui.TableNextColumn();
            var scaleMode = _effect.Attributes & HypnoAttributes.ScaleMask;

            if (ImGui.RadioButton("Static", scaleMode == 0))
                _effect.Attributes &= ~HypnoAttributes.ScaleMask;
            CkGui.AttachToolTip("Text should remain the same size.");

            ImGui.SameLine();
            if (ImGui.RadioButton("Grows Overtime", scaleMode == HypnoAttributes.LinearTextScale))
                _effect.Attributes = (_effect.Attributes & ~HypnoAttributes.ScaleMask) | HypnoAttributes.LinearTextScale;
            CkGui.AttachToolTip(HypnoAttributes.LinearTextScale.ToTooltip());

            ImGui.SameLine();
            if (ImGui.RadioButton("Random Scale", scaleMode == HypnoAttributes.RandomTextScale))
                _effect.Attributes = (_effect.Attributes & ~HypnoAttributes.ScaleMask) | HypnoAttributes.RandomTextScale;
            CkGui.AttachToolTip(HypnoAttributes.RandomTextScale.ToTooltip());

            // Text Font Size
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Font Size");
            ImGui.TableNextColumn();
            var textSize = _effect.TextFontSize;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.SliderInt("##TextSize", ref textSize, HypnoService.FONTSIZE_MIN, HypnoService.FONTSIZE_MAX, "%dpx"))
                _effect.TextFontSize = textSize;

            // Stroke Thickness
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Stroke Thickness");
            ImGui.TableNextColumn();
            var strokeThickness = _effect.StrokeThickness;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.SliderInt("##StrokeThickness", ref strokeThickness, HypnoService.STROKE_THICKNESS_MIN, HypnoService.STROKE_THICKNESS_MAX, "%dpx"))
                _effect.StrokeThickness = strokeThickness;

            // Text Display Time
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Text Display Time");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var textLife = _effect.TextDisplayTime;
            if (ImGui.SliderInt("##TextLife", ref textLife, HypnoService.DISPLAY_TIME_MIN, HypnoService.DISPLAY_TIME_MAX, $"%dms"))
                _effect.TextDisplayTime = textLife;
            CkGui.AttachToolTip("How frequently the text cycles through the display words.");

            var hasFade = _effect.Attributes.HasAny(HypnoAttributes.TextFade);
            var hasSpeedUp = _effect.Attributes.HasAny(HypnoAttributes.SpeedUpOnCycle);
            var hasTranspose = _effect.Attributes.HasAny(HypnoAttributes.TransposeColors);

            if (hasFade)
            {
                // Text Fade In
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Fade-In Time");
                ImGui.TableNextColumn();
                var fadeIn = _effect.TextFadeInTime;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.SliderInt("##TextFadeIn", ref fadeIn, HypnoService.DISPLAY_TIME_MIN, HypnoService.DISPLAY_TIME_MAX / 2, "%dms"))
                    _effect.TextFadeInTime = fadeIn;
                // Text Fade Out
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Fade-Out Time");
                ImGui.TableNextColumn();
                var fadeOut = _effect.TextFadeOutTime;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.SliderInt("##TextFadeOut", ref fadeOut, HypnoService.DISPLAY_TIME_MIN, HypnoService.DISPLAY_TIME_MAX / 2, "%dms"))
                    _effect.TextFadeOutTime = fadeOut;
            }

            if (hasSpeedUp)
            {
                // Speed Up On Cycle
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Acceleration Time");
                ImGui.TableNextColumn();
                var speedUp = _effect.SpeedupTime;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.SliderInt("##SpeedUpTime", ref speedUp, HypnoService.SPEED_BETWEEN_MIN, HypnoService.SPEED_BETWEEN_MAX, "%dms"))
                    _effect.SpeedupTime = speedUp;
                CkGui.AttachToolTip(HypnoAttributes.SpeedUpOnCycle.ToTooltip());
            }

            if (hasTranspose)
            {
                // Transpose Colors
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Transpose Time");
                ImGui.TableNextColumn();
                var transposeRef = _effect.TransposeTime;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.DragInt("##TransposeTime", ref transposeRef, 10f, HypnoService.DISPLAY_TIME_MIN, HypnoService.DISPLAY_TIME_MAX, "%dms"))
                    _effect.TransposeTime = transposeRef;
            }

            if (!hasFade || !hasSpeedUp || !hasTranspose)
            {
                // Filler Frame Heights
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var fillerFrameHeights = (hasFade ? 0 : 2) + (hasSpeedUp ? 0 : 1) + (hasTranspose ? 0 : 1);
                ImGui.Dummy(new Vector2(0, ImGui.GetFrameHeight() * fillerFrameHeights));
            }
        }
    }

    private void DrawColorSections(float width)
    {
        var colorPickerWidth = (width / 3);

        using (ImRaii.Group())
        {
            CkGui.CenterTextAligned("Image Color", colorPickerWidth);
            ImGui.SetNextItemWidth(colorPickerWidth);
            var tintVec = ColorHelpers.RgbaUintToVector4(_effect!.ImageColor);
            if (ImGui.ColorPicker4("##ImageColor", ref tintVec, COLOR_FLAGS))
            {
                _effect.ImageColor = ColorHelpers.RgbaVector4ToUint(tintVec);
                _activeState.ImageColor = _effect.ImageColor;
            }
        }

        ImGui.SameLine();
        using (ImRaii.Group())
        {
            CkGui.CenterTextAligned("Text Color", colorPickerWidth);
            ImGui.SetNextItemWidth(colorPickerWidth);
            var textColVec = ColorHelpers.RgbaUintToVector4(_effect.TextColor);
            if (ImGui.ColorPicker4("##TextColor", ref textColVec, COLOR_FLAGS))
                _effect.TextColor = ColorHelpers.RgbaVector4ToUint(textColVec);
        }

        ImGui.SameLine();
        using (ImRaii.Group())
        {
            CkGui.CenterTextAligned("Text Stroke Color", colorPickerWidth);
            ImGui.SetNextItemWidth(colorPickerWidth);
            var textColVec = ColorHelpers.RgbaUintToVector4(_effect.StrokeColor);
            if (ImGui.ColorPicker4("##TextStrokeColor", ref textColVec, COLOR_FLAGS))
                _effect.StrokeColor = ColorHelpers.RgbaVector4ToUint(textColVec);
        }
    }

    private Vector2 DisplayPreviewEffect(float height, string path)
    {
        if (_effect is null || TextureManagerEx.GetMetadataPath(ImageDataType.Hypnosis, path) is not { } hypnoImage)
            return Vector2.Zero;

        try
        {
            // Recalculate the necessary cycle speed that we should need for the rotation (may need optimizations later)
            var speed = _activeState.SpinSpeed * 0.001f;
            var direction = _effect.Attributes.HasFlag(HypnoAttributes.InvertDirection) ? -1f : 1f;
            _activeState.Rotation += direction * (Svc.Framework.UpdateDelta.Milliseconds * speed);
            _activeState.Rotation %= MathF.PI * 2f;

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
            var cos = MathF.Cos(_activeState.Rotation);
            var sin = MathF.Sin(_activeState.Rotation);

            // Impacted by zoom factor. (Nessisary for Pulsating)
            var corners = new[]
            {
            new Vector2(-hypnoImage.Width, -hypnoImage.Height) * _effect.ZoomDepth,
            new Vector2(hypnoImage.Width, -hypnoImage.Height) * _effect.ZoomDepth,
            new Vector2(hypnoImage.Width, hypnoImage.Height) * _effect.ZoomDepth,
            new Vector2(-hypnoImage.Width, hypnoImage.Height) * _effect.ZoomDepth
        };

            var rotatedBounds = new Vector2[4];
            for (var i = 0; i < corners.Length; i++)
            {
                var x = corners[i].X;
                var y = corners[i].Y;

                rotatedBounds[i] = new Vector2(
                    center.X + (x * cos - y * sin) * _effect.ZoomDepth,
                    center.Y + (x * sin + y * cos) * _effect.ZoomDepth
                );
            }

            // So we can account for transposing colors.
            var imgTint = _activeState.ImageColor;

            // Workaround the popup / windows cliprect and draw it at the correct dimentions.
            drawList.PushClipRect(topLeft, topLeft + screenSize, true);// Display the image stretched to the bounds of the screen and stuff.
            drawList.AddImageQuad(
                hypnoImage.ImGuiHandle,
                rotatedBounds[0],
                rotatedBounds[1],
                rotatedBounds[2],
                rotatedBounds[3],
                HypnoService.UVCorners[0],
                HypnoService.UVCorners[1],
                HypnoService.UVCorners[2],
                HypnoService.UVCorners[3],
                imgTint);
            drawList.PopClipRect();

            if (string.IsNullOrEmpty(_activeState.CurrentText))
                return screenSize;

            // determine the font scalar.
            var fontScaler = UiFontService.FullScreenFont.Available
                ? (_effect.TextFontSize / UiFontService.FullScreenFontPtr.FontSize) * _activeState.TextScale
                : _activeState.TextScale;

            // determine the new target position.
            var targetPos = _effect.Attributes.HasAny(HypnoAttributes.LinearTextScale)
                ? center - Vector2.Lerp(_activeState.TextOffsetStart, _activeState.TextOffsetEnd, _activeState.TextScaleProgress)
                : center - (CkGui.CalcFontTextSize(_activeState.CurrentText, UiFontService.FullScreenFont) * fontScaler) * 0.5f;

            drawList.OutlinedFontScaled(
                UiFontService.FullScreenFontPtr,
                UiFontService.FullScreenFontPtr.FontSize * fontScaler,
                targetPos,
                _activeState.CurrentText,
                ColorHelpers.ApplyOpacity(_effect.TextColor, _activeState.TextOpacity),
                ColorHelpers.ApplyOpacity(_effect.StrokeColor, _activeState.TextOpacity),
                _effect.StrokeThickness);

            return screenSize;
        }
        catch (Exception ex)
        {
            Svc.Logger.Error($"Error displaying Hypnotic Effect Preview: {ex}");
            return Vector2.Zero;
        }
    }
}
