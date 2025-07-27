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
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Gui.Components;
public class HypnoEffectEditor : IDisposable
{
    // Effect Constants
    const ImGuiColorEditFlags COLOR_FLAGS = ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoSidePreview;
    const ImGuiColorEditFlags KINKSTER_COLOR_FLAGS = ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoSidePreview;

    private CompactConfigTab _compactConfigTab;
    private CompactPhrasesColorsTab _compactPhrasesColorsTab;
    private IFancyTab[] EditorTabs;

    // Editor Control.
    private Task?                    _colorTask;
    private Task?                    _textTask;
    private CancellationTokenSource? _tasksCTS;
    private TagCollection            _displayTextEditor;

    // Locals.
    private bool            _isOpen = false;
    private HypnoticEffect? _effect = null;
    private HypnosisState   _activeState = new();
    public HypnoEffectEditor(string popupLabel)
    {
        PopupLabel = popupLabel;
        _tasksCTS = new CancellationTokenSource();
        _displayTextEditor = new TagCollection();
        _compactConfigTab = new CompactConfigTab(this);
        _compactPhrasesColorsTab = new CompactPhrasesColorsTab(this);
        EditorTabs = [ _compactConfigTab, _compactPhrasesColorsTab ];
    }

    public readonly string PopupLabel = "HypnosisEditorModal";

    public bool IsEffectNull => _effect is null;
    public void Dispose()
    {
        // Halt any background tasks.
        _tasksCTS?.Cancel();
        try
        {
            _colorTask?.Wait(1000);
            _textTask?.Wait(1000);
        }
        catch (Bagagwa ex)
        {
            Svc.Logger.Warning("Exception waiting for hypno background tasks to exit: " + ex);
        }

        _tasksCTS?.Dispose();
        _effect = null;
    }

    public bool TryGetEffect([NotNullWhen(true)] out HypnoticEffect? effect)
    {
        effect = _effect;
        return effect != null;
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

    public void OnEditorClose()
    {
        // Cancel any running tasks.
        _tasksCTS?.Cancel();
        _effect = null;
        _activeState = new HypnosisState();
    }

    /// <summary> Draws the editor. Passes in the original so when we save the editor we can update the original entry. </summary>
    public void DrawPopup(HypnoticOverlay overlay, Action<HypnoticEffect>? onSaveAndClose = null)
    {
        if (_effect is null)
            return;

        if (!ImGui.IsPopupOpen($"###{PopupLabel}"))
        {
            _isOpen = true;
            ImGui.OpenPopup($"###{PopupLabel}");
        }

        if (ImGui.BeginPopupModal($"Effect Editor###{PopupLabel}", ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            // Draw out the editor contents.
            using (ImRaii.Table($"EditorContainerOuter{PopupLabel}", 2, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.BordersInner))
            {
                ImGui.TableSetupColumn("EditorArea", ImGuiTableColumnFlags.WidthFixed, 440f);
                ImGui.TableSetupColumn("PreviewArea", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var cellWidth = ImGui.GetContentRegionAvail().X;

                DrawEditorArea(cellWidth);
                var editorHeight = ImGui.GetItemRectSize().Y;


                ImGui.TableNextColumn();
                var size = DisplayPreviewHeightConstrained(editorHeight, overlay.OverlayPath);
                ImGui.Dummy(size);

                // Draw the phrase editors.
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var height = size == Vector2.Zero ? CkStyle.GetFrameRowsHeight(5) : (size.X / 3) - ImGui.GetFrameHeight() * 2;
                using (var c = CkRaii.HeaderChild("Display Text Phrases", new Vector2(cellWidth, height.AddWinPadY()), HeaderFlags.AddPaddingToHeight))
                {
                    using (CkRaii.FramedChildPaddedW($"##DisplayPhrases_{PopupLabel}", c.InnerRegion.X, height, CkColor.FancyHeaderContrast.Uint(), DFlags.RoundCornersAll))
                    {
                        if (_displayTextEditor.DrawTagsEditor($"##EffectPhrases_{PopupLabel}", _effect.DisplayMessages, out var newDisplayWords))
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
                onSaveAndClose?.Invoke(_effect);
                OnEditorClose();
            }

            ImGui.EndPopup();
        }

        if (!_isOpen)
            _effect = null;
    }

    public int GetCompactHeightRowCount()
    {
        var fadeRows = (_effect?.Attributes.HasAny(HypnoAttributes.TextFade) ?? false) ? 2 : 0;
        var transposeRow = (_effect?.Attributes.HasAny(HypnoAttributes.TransposeColors) ?? false) ? 1 : 0;
        var speedUpRow = (_effect?.Attributes.HasAny(HypnoAttributes.SpeedUpOnCycle) ?? false) ? 1 : 0;
        return 10 + fadeRows + transposeRow + speedUpRow;
    }

    public void DrawCompactEditorTabs(float width)
    {
        using (CkRaii.TabBarChild("EffectEdit", width, CkStyle.GetFrameRowsHeight((uint)GetCompactHeightRowCount()), FancyTabBar.Rounding, CkColor.VibrantPink.Uint(), CkColor.VibrantPinkHovered.Uint(), CkColor.FancyHeader.Uint(), 
            LabelFlags.PadInnerChild | LabelFlags.AddPaddingToHeight, out var selected, EditorTabs))
                selected?.DrawContents(ImGui.GetContentRegionAvail().X);
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

                ImGui.TableNextColumn();
                foreach (var attribute in HypnoAttrExtensions.ToggleFlags)
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
            if (ImGui.SliderInt("##TextLife", ref textLife, HypnoService.DISPLAY_TIME_MIN * 3, HypnoService.DISPLAY_TIME_MAX, $"%dms"))
            {
                _effect.TextDisplayTime = textLife;

                // Fix up fade-in and fade-out to ensure their combined value doesn't exceed the new display time
                var totalFade = _effect.TextFadeInTime + _effect.TextFadeOutTime;
                if (totalFade > _effect.TextDisplayTime)
                {
                    // Proportionally reduce both if they exceed
                    var ratio = _effect.TextDisplayTime / (float)totalFade;
                    _effect.TextFadeInTime = (int)(_effect.TextFadeInTime * ratio);
                    _effect.TextFadeOutTime = (int)(_effect.TextFadeOutTime * ratio);
                }

                // Clamp each to half the display time, in case one was 0
                _effect.TextFadeInTime = Math.Min(_effect.TextFadeInTime, _effect.TextDisplayTime / 2);
                _effect.TextFadeOutTime = Math.Min(_effect.TextFadeOutTime, _effect.TextDisplayTime - _effect.TextFadeInTime);
            }
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
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                var maxFadeIn = Math.Max(HypnoService.DISPLAY_TIME_MIN, _effect.TextDisplayTime - _effect.TextFadeOutTime);
                var fadeIn = _effect.TextFadeInTime;
                if (ImGui.SliderInt("##TextFadeIn", ref fadeIn, HypnoService.DISPLAY_TIME_MIN, maxFadeIn, "%dms"))
                {
                    _effect.TextFadeInTime = fadeIn;
                    // Adjust fadeout to not exceed.
                    _effect.TextFadeOutTime = Math.Min(_effect.TextFadeOutTime, _effect.TextDisplayTime - _effect.TextFadeInTime);
                }

                // Text Fade Out
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Fade-Out Time");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                var maxFadeOut = Math.Max(HypnoService.DISPLAY_TIME_MIN, _effect.TextDisplayTime - _effect.TextFadeInTime);
                var fadeOut = _effect.TextFadeOutTime;
                if (ImGui.SliderInt("##TextFadeOut", ref fadeOut, HypnoService.DISPLAY_TIME_MIN, maxFadeOut, "%dms"))
                {
                    _effect.TextFadeOutTime = fadeOut;
                    // Adjust fadein to not exceed.
                    _effect.TextFadeInTime = Math.Min(_effect.TextFadeInTime, _effect.TextDisplayTime - _effect.TextFadeOutTime);
                }
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

    public Vector2 DisplayPreviewHeightConstrained(float height, string path, float rounding = 0)
    {
        var screenSize = ImGui.GetIO().DisplaySize;
        var scale = height / screenSize.Y;
        return DisplayPreviewEffect(scale, path);
    }

    public Vector2 DisplayPreviewWidthConstrained(float width, string path, float rounding = 0)
    {
        var screenSize = ImGui.GetIO().DisplaySize;
        var scale = width / screenSize.X;
        return DisplayPreviewEffect(scale, path);
    }


    private Vector2 DisplayPreviewEffect(float sizeScale, string path, float rounding = 0)
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
            var screenSize = ImGui.GetIO().DisplaySize * sizeScale;

            // Get the center position.
            var center = topLeft + screenSize * 0.5f;

            // time for rotation maths
            var cos = MathF.Cos(_activeState.Rotation);
            var sin = MathF.Sin(_activeState.Rotation);

            // scaled zoom depth.
            var zoom = _effect.ZoomDepth * sizeScale;

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
                    center.X + (x * cos - y * sin) * zoom,
                    center.Y + (x * sin + y * cos) * zoom
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
            var fontScaler = sizeScale * (UiFontService.FullScreenFont.Available
                ? (_effect.TextFontSize / UiFontService.FullScreenFontPtr.FontSize) * _activeState.TextScale
                : _activeState.TextScale);

            // determine the new target position.
            var targetPos = _effect.Attributes.HasAny(HypnoAttributes.LinearTextScale)
                ? center - Vector2.Lerp(sizeScale * _activeState.TextOffsetStart, sizeScale * _activeState.TextOffsetEnd, _activeState.TextScaleProgress)
                : center - (CkGui.CalcFontTextSize(_activeState.CurrentText, UiFontService.FullScreenFont) * fontScaler) * 0.5f;

            drawList.OutlinedFontScaled(
                UiFontService.FullScreenFontPtr,
                UiFontService.FullScreenFontPtr.FontSize,
                fontScaler,
                targetPos,
                _activeState.CurrentText,
                ColorHelpers.ApplyOpacity(_effect.TextColor, _activeState.TextOpacity),
                ColorHelpers.ApplyOpacity(_effect.StrokeColor, _activeState.TextOpacity),
                _effect.StrokeThickness);

            return screenSize;
        }
        catch (Bagagwa ex)
        {
            Svc.Logger.Error($"Error displaying Hypnotic Effect Preview: {ex}");
            return Vector2.Zero;
        }
    }

    // Compact Editor Tabs.
    internal class CompactConfigTab : IFancyTab
    {
        private readonly HypnoEffectEditor _editorRef;
        public string Label => "Options";
        public string Tooltip => "Adjust Effect Display";
        public bool Disabled => false;
        public CompactConfigTab(HypnoEffectEditor editor) => _editorRef = editor;
        public void DrawContents(float width)
        {
            var effect = _editorRef._effect;
            if (effect is null) return;
            var hasFade = effect.Attributes.HasAny(HypnoAttributes.TextFade);
            var hasSpeedUp = effect.Attributes.HasAny(HypnoAttributes.SpeedUpOnCycle);
            var hasTranspose = effect.Attributes.HasAny(HypnoAttributes.TransposeColors);

            ImGui.Columns(2, "HypnoAttributes", false);
            ImGui.SetColumnWidth(0, width * .5f);

            var selectedAttributes = (uint)effect.Attributes;
            foreach (var attribute in HypnoAttrExtensions.ToggleFlags)
            {
                if (ImGui.CheckboxFlags(attribute.ToCompactName(), ref selectedAttributes, (uint)attribute))
                    effect.Attributes ^= attribute;
                CkGui.AttachToolTip(attribute.ToTooltip());
                ImGui.NextColumn();
            }
            ImGui.Columns(1);
            ImUtf8.TextFrameAligned("Order:");
            ImUtf8.SameLineInner();
            var txtMode = effect.Attributes & HypnoAttributes.TextDisplayMask;
            if (ImGui.RadioButton(HypnoAttributes.TextDisplayOrdered.ToCompactName(), txtMode == HypnoAttributes.TextDisplayOrdered))
                effect.Attributes = (effect.Attributes & ~HypnoAttributes.TextDisplayMask) | HypnoAttributes.TextDisplayOrdered;
            CkGui.AttachToolTip(HypnoAttributes.TextDisplayOrdered.ToTooltip());

            ImGui.SameLine();
            if (ImGui.RadioButton(HypnoAttributes.TextDisplayRandom.ToCompactName(), txtMode == HypnoAttributes.TextDisplayRandom))
                effect.Attributes = (effect.Attributes & ~HypnoAttributes.TextDisplayMask) | HypnoAttributes.TextDisplayRandom;
            CkGui.AttachToolTip(HypnoAttributes.TextDisplayRandom.ToTooltip());

            // Type
            ImUtf8.TextFrameAligned("Scale:");
            ImUtf8.SameLineInner();
            var scaleMode = effect.Attributes & HypnoAttributes.ScaleMask;
            if (ImGui.RadioButton("Static", scaleMode == 0))
                effect.Attributes &= ~HypnoAttributes.ScaleMask;
            CkGui.AttachToolTip("Text should remain the same size.");

            ImUtf8.SameLineInner();
            if (ImGui.RadioButton("Grows", scaleMode == HypnoAttributes.LinearTextScale))
                effect.Attributes = (effect.Attributes & ~HypnoAttributes.ScaleMask) | HypnoAttributes.LinearTextScale;
            CkGui.AttachToolTip(HypnoAttributes.LinearTextScale.ToTooltip());

            ImUtf8.SameLineInner();
            if (ImGui.RadioButton("Random", scaleMode == HypnoAttributes.RandomTextScale))
                effect.Attributes = (effect.Attributes & ~HypnoAttributes.ScaleMask) | HypnoAttributes.RandomTextScale;
            CkGui.AttachToolTip(HypnoAttributes.RandomTextScale.ToTooltip());

            var fullWidth = ImGui.GetContentRegionAvail().X;

            var spinRef = effect.SpinSpeed;
            ImGui.SetNextItemWidth(fullWidth);
            if (ImGui.SliderFloat("##SpinSpeed", ref spinRef, HypnoService.SPIN_SPEED_MIN, HypnoService.SPIN_SPEED_MAX, "%.2fx Spin Speed"))
            {
                effect.SpinSpeed = spinRef;
                _editorRef._activeState.SpinSpeed = effect.SpinSpeed; // Update the active state for the preview.
            }

            var zoomRef = effect.ZoomDepth;
            ImGui.SetNextItemWidth(fullWidth);
            if (ImGui.SliderFloat("##ZoomDepth", ref zoomRef, HypnoService.ZOOM_MIN, HypnoService.ZOOM_MAX, "%.2fx Zoom"))
                effect.ZoomDepth = zoomRef;


            var textSize = effect.TextFontSize;
            ImGui.SetNextItemWidth(fullWidth);
            if (ImGui.SliderInt("##TextSize", ref textSize, HypnoService.FONTSIZE_MIN, HypnoService.FONTSIZE_MAX, "%dpx Font Size"))
                effect.TextFontSize = textSize;

            var strokeThickness = effect.StrokeThickness;
            ImGui.SetNextItemWidth(fullWidth);
            if (ImGui.SliderInt("##StrokeThickness", ref strokeThickness, HypnoService.STROKE_THICKNESS_MIN, HypnoService.STROKE_THICKNESS_MAX, "%dpx Outline"))
                effect.StrokeThickness = strokeThickness;


            ImGui.SetNextItemWidth(fullWidth);
            var textLife = effect.TextDisplayTime;
            if (ImGui.SliderInt("##TextLife", ref textLife, HypnoService.DISPLAY_TIME_MIN, HypnoService.DISPLAY_TIME_MAX, $"%dms per phase"))
            {
                effect.TextDisplayTime = textLife;

                // Fix up fade-in and fade-out to ensure their combined value doesn't exceed the new display time
                var totalFade = effect.TextFadeInTime + effect.TextFadeOutTime;
                if (totalFade > effect.TextDisplayTime)
                {
                    // Proportionally reduce both if they exceed
                    var ratio = effect.TextDisplayTime / (float)totalFade;
                    effect.TextFadeInTime = (int)(effect.TextFadeInTime * ratio);
                    effect.TextFadeOutTime = (int)(effect.TextFadeOutTime * ratio);
                }

                // Clamp each to half the display time, in case one was 0
                effect.TextFadeInTime = Math.Min(effect.TextFadeInTime, effect.TextDisplayTime / 2);
                effect.TextFadeOutTime = Math.Min(effect.TextFadeOutTime, effect.TextDisplayTime - effect.TextFadeInTime);
            }
            CkGui.AttachToolTip("How frequently the text cycles through the display words.");

            if (hasFade)
            {
                var maxFadeIn = Math.Max(HypnoService.DISPLAY_TIME_MIN, effect.TextDisplayTime - effect.TextFadeOutTime);
                var fadeIn = effect.TextFadeInTime;
                if (ImGui.SliderInt("##TextFadeIn", ref fadeIn, HypnoService.DISPLAY_TIME_MIN, maxFadeIn, "%dms"))
                {
                    effect.TextFadeInTime = fadeIn;
                    // Adjust fadeout to not exceed.
                    effect.TextFadeOutTime = Math.Min(effect.TextFadeOutTime, effect.TextDisplayTime - effect.TextFadeInTime);
                }

                var maxFadeOut = Math.Max(HypnoService.DISPLAY_TIME_MIN, effect.TextDisplayTime - effect.TextFadeInTime);
                var fadeOut = effect.TextFadeOutTime;
                if (ImGui.SliderInt("##TextFadeOut", ref fadeOut, HypnoService.DISPLAY_TIME_MIN, maxFadeOut, "%dms"))
                {
                    effect.TextFadeOutTime = fadeOut;
                    // Adjust fadein to not exceed.
                    effect.TextFadeInTime = Math.Min(effect.TextFadeInTime, effect.TextDisplayTime - effect.TextFadeOutTime);
                }
            }

            if (hasSpeedUp)
            {
                var speedUp = effect.SpeedupTime;
                ImGui.SetNextItemWidth(fullWidth);
                if (ImGui.SliderInt("##SpeedUpTime", ref speedUp, HypnoService.SPEED_BETWEEN_MIN, HypnoService.SPEED_BETWEEN_MAX, "%dms Transition Time"))
                    effect.SpeedupTime = speedUp;
                CkGui.AttachToolTip(HypnoAttributes.SpeedUpOnCycle.ToTooltip());
            }

            if (hasTranspose)
            {
                var transposeRef = effect.TransposeTime;
                ImGui.SetNextItemWidth(fullWidth);
                if (ImGui.SliderInt("##TransposeTime", ref transposeRef, HypnoService.DISPLAY_TIME_MIN, HypnoService.DISPLAY_TIME_MAX, "%dms Transpose Time"))
                    effect.TransposeTime = transposeRef;
            }
        }
    }
    internal class CompactPhrasesColorsTab : IFancyTab
    {
        private readonly HypnoEffectEditor _editorRef;
        public string Label => "Phrases & Colors";
        public string Tooltip => "Adjust Displayed Phrases & Colors!";
        public bool Disabled => false;
        public CompactPhrasesColorsTab(HypnoEffectEditor editor) => _editorRef = editor;
        public void DrawContents(float width)
        {
            var effect = _editorRef._effect;
            var activeState = _editorRef._activeState;
            if (effect is null) return;
            var height = CkStyle.GetFrameRowsHeight(3);
            using (CkRaii.FramedChildPaddedW($"##DisplayPhrases_{_editorRef.PopupLabel}", width, height, CkColor.ElementBG.Uint(), DFlags.RoundCornersAll))
            {
                if (_editorRef._displayTextEditor.DrawTagsEditor($"##EffectPhrases_{_editorRef.PopupLabel}", effect.DisplayMessages, out var newDisplayWords))
                    effect.DisplayMessages = newDisplayWords.ToArray();

                if (_editorRef._displayTextEditor.DrawHelpButtons(effect.DisplayMessages, out var newWords, true))
                    effect.DisplayMessages = newWords.ToArray();
            }
            
            CkGui.CenterTextAligned("Image Color");
            ImGui.SetNextItemWidth(width);
            var tintVec = ColorHelpers.RgbaUintToVector4(effect!.ImageColor);
            if (ImGui.ColorEdit4("##EffectImageColor", ref tintVec, KINKSTER_COLOR_FLAGS))
            {
                effect.ImageColor = ColorHelpers.RgbaVector4ToUint(tintVec);
                activeState.ImageColor = effect.ImageColor;
            }
            
            CkGui.CenterTextAligned("Text Color");
            ImGui.SetNextItemWidth(width);
            var textColVec = ColorHelpers.RgbaUintToVector4(effect.TextColor);
            if (ImGui.ColorEdit4("##EffectTextColor", ref textColVec, KINKSTER_COLOR_FLAGS))
                effect.TextColor = ColorHelpers.RgbaVector4ToUint(textColVec);

            CkGui.CenterTextAligned("Text Stroke Color", width);
            ImGui.SetNextItemWidth(width);
            var textOutlineColVec = ColorHelpers.RgbaUintToVector4(effect.StrokeColor);
            if (ImGui.ColorEdit4("##EffectStrokeColor", ref textOutlineColVec, KINKSTER_COLOR_FLAGS))
                effect.StrokeColor = ColorHelpers.RgbaVector4ToUint(textOutlineColVec);
        }
    }
}
