using CkCommons;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Controller;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using OtterGui.Text;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Gui.Components;

public class HypnoEffectEditor : IDisposable
{
    // Effect Constants
    const ImGuiColorEditFlags COLOR_FLAGS = ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoSidePreview;
    const ImGuiColorEditFlags KINKSTER_COLOR_FLAGS = ImGuiColorEditFlags.DisplayRgb | ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoSidePreview;

    private readonly HypnoEffectManager _presetManager;
    private readonly TutorialService _guides;

    private CompactConfigTab _compactConfigTab;
    private CompactPhrasesColorsTab _compactPhrasesColorsTab;
    private CompactPresetsTab _presetsTab;
    private IFancyTab[] EditorTabs;

    // Editor Control.
    private Task?                    _colorTask;
    private Task?                    _textTask;
    private CancellationTokenSource? _tasksCTS;
    private TagCollection            _displayTextEditor;

    // Locals.
    private bool            _isOpen = false;

    public HypnoEffectEditor(string popupLabel, HypnoEffectManager presetManager, TutorialService guides)
    {
        _presetManager = presetManager;
        _guides = guides;
        PopupLabel = popupLabel;
        _tasksCTS = new CancellationTokenSource();
        _displayTextEditor = new TagCollection();
        _compactConfigTab = new CompactConfigTab(this);
        _compactPhrasesColorsTab = new CompactPhrasesColorsTab(this);
        _presetsTab = new CompactPresetsTab(this, presetManager);
        EditorTabs = [_compactConfigTab, _compactPhrasesColorsTab, _presetsTab];
    }

    /// <summary>
    ///     The current active hypnotic effect being used in the editor. <para />
    ///     If the name is empty, then the effect is not in binding mode. <para />
    ///     While the effect is in binding mode, any changes made are updated to the preset manager. <para />
    ///     When an effect is applied from a preset, <see cref="_activeEffect.Name"/> is not set.
    /// </summary>
    private (string Name, HypnoticEffect? Effect) _current = (string.Empty, null);
    private HypnosisState _activeState = new();

    public readonly string PopupLabel = "HypnosisEditorModal";
    
    private Vector2 LastPos = Vector2.Zero;
    private Vector2 LastSize = Vector2.Zero;

    public bool InBindingMode => !string.IsNullOrEmpty(_current.Name);
    public bool IsEffectNull => _current.Effect is null;

    public void Dispose()
    {
        _tasksCTS?.SafeCancel();
        try
        {
            _colorTask?.Wait(1000);
            _textTask?.Wait(1000);
        }
        catch (Bagagwa ex)
        {
            Svc.Logger.Warning("Exception waiting for hypno background tasks to exit: " + ex);
        }

        _tasksCTS?.SafeDispose();
        _current = (string.Empty, null);
    }

    public bool TryGetEffect([NotNullWhen(true)] out HypnoticEffect? effect)
    {
        effect = _current.Effect;
        return effect != null;
    }

    public void SetBlankEffect()
    {
        _tasksCTS?.SafeCancel();
        // reset all values to new defaults.
        _current = (string.Empty, new HypnoticEffect());
        _activeState = new HypnosisState { ImageColor = _current.Effect.ImageColor };
        // Assign the new tasks for the display editor.
        _tasksCTS = new CancellationTokenSource();
        _colorTask = HypnoService.ColorTransposeTask(_current.Effect, _activeState, _tasksCTS.Token);
        _textTask = HypnoService.TextDisplayTask(_current.Effect, _activeState, _tasksCTS.Token);
    }

    /// <summary>
    ///     Loads a generic effect into the editor. <para />
    ///     Generic effects are not considered presets, and updates are not saved to the preset Manager. <para />
    ///     Only case in which a generic effect's values are saves is when the popup methods 
    ///     OnSaveAndClose copies them over before they are reset.
    /// </summary>
    public void SetGenericEffect(HypnoticEffect effect)
    {
        // halt any running tasks.
        _tasksCTS.SafeCancel();
        // set new effect via reference copy.
        _current = (string.Empty, effect);
        _activeState = new HypnosisState { ImageColor = _current.Effect.ImageColor };
        // Assign the new tasks for the display editor.
        _tasksCTS = new CancellationTokenSource();
        _colorTask = HypnoService.ColorTransposeTask(_current.Effect, _activeState, _tasksCTS.Token);
        _textTask = HypnoService.TextDisplayTask(_current.Effect, _activeState, _tasksCTS.Token);
    }

    public void SetEffectWithPresetValues(string presetName, bool isBindingMode = false)
    {
        // see if the effect even exists, if it does not, early return.
        if (!_presetManager.Presets.TryGetValue(presetName, out var effect))
            return;

        // Halt any running tasks.
        _tasksCTS?.SafeCancel();
        // set the new effect.
        // Copy the effect to avoid modifying the preset directly.
        _current = (isBindingMode ? presetName : string.Empty, new HypnoticEffect(effect));
        // Set the new effect.
        _activeState = new HypnosisState { ImageColor = _current.Effect.ImageColor };
        // Assign the new tasks for the display editor.
        _tasksCTS = new CancellationTokenSource();
        _colorTask = HypnoService.ColorTransposeTask(_current.Effect, _activeState, _tasksCTS.Token);
        _textTask = HypnoService.TextDisplayTask(_current.Effect, _activeState, _tasksCTS.Token);
    }

    /// <summary>
    ///     Performs an update to the current effect item, and then if it was a binded preset, updates the preset manager with the new values.
    /// </summary>
    public void UpdateEffect(Action updateAct)
    {
        updateAct();
        if (InBindingMode && _current.Effect is not null)
            _presetManager.UpdatePreset(_current.Name, _current.Effect);
    }

    public void OnEditorClose()
    {
        // Cancel any running tasks.
        _tasksCTS.SafeCancel();
        _current = (string.Empty, null);
        _activeState = new HypnosisState();
    }

    /// <summary> Draws the editor. Passes in the original so when we save the editor we can update the original entry. </summary>
    public void DrawPopup(HypnoticOverlay overlay, Action<HypnoticEffect>? onSaveAndClose = null)
    {
        if (_current.Effect is not { } eff)
            return;

        if (!ImGui.IsPopupOpen($"###{PopupLabel}"))
        {
            _isOpen = true;
            ImGui.OpenPopup($"###{PopupLabel}");
        }

        if (ImGui.BeginPopupModal($"Effect Editor###{PopupLabel}", ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            LastPos = ImGui.GetWindowPos();
            LastSize = ImGui.GetWindowSize();
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
                    using (var _ = CkRaii.FramedChildPaddedW($"##DisplayPhrases_{PopupLabel}", c.InnerRegion.X, height, CkCol.CurvedHeaderFade.Uint(), CkCol.CurvedHeaderFade.Uint(), DFlags.RoundCornersAll))
                    {
                        var pos = ImGui.GetCursorScreenPos();
                        if (_displayTextEditor.DrawTagsEditor($"##EffectPhrases_{PopupLabel}", eff.DisplayMessages, out var newDisplayWords, GsCol.VibrantPink.Vec4()))
                            UpdateEffect(() => eff.DisplayMessages = newDisplayWords.ToArray());
                        

                        if (_displayTextEditor.DrawHelpButtons(eff.DisplayMessages, out var newWords, true, GsCol.VibrantPink.Vec4()))
                            UpdateEffect(() => eff.DisplayMessages = newWords.ToArray());
                    }
                }
                _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.EffectWords, LastPos, LastSize);

                // Color Selections
                ImGui.TableNextColumn();
                using (ImRaii.Group())
                    DrawColorSections(size.X);
                _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.EffectColors, LastPos, LastSize, () => _isOpen=false);
            }

            CkGui.SeparatorSpaced(GsCol.LushPinkLine.Uint());

            CkGui.SetCursorXtoCenter(CkGui.IconTextButtonSize(FAI.Save, "Save and Close"));
            if (CkGui.IconTextButton(FAI.Save, "Save and Close"))
            {
                _isOpen = false;
                onSaveAndClose?.Invoke(eff);
                OnEditorClose();
            }

            ImGui.EndPopup();
        }

        if (!_isOpen)
        {
            // clear the current effect and active state when closing the popup.
            _current = (string.Empty, null);
        }
    }

    public int GetCompactHeightRowCount()
    {
        var fadeRows = (_current.Effect?.Attributes.HasAny(HypnoAttributes.TextFade) ?? false) ? 2 : 0;
        var transposeRow = (_current.Effect?.Attributes.HasAny(HypnoAttributes.TransposeColors) ?? false) ? 1 : 0;
        var speedUpRow = (_current.Effect?.Attributes.HasAny(HypnoAttributes.SpeedUpOnCycle) ?? false) ? 1 : 0;
        return 10 + fadeRows + transposeRow + speedUpRow;
    }

    public void DrawCompactEditorTabs(float width)
    {
        using (CkRaii.TabBarChild("EffectEdit", width, CkStyle.GetFrameRowsHeight(GetCompactHeightRowCount()), FancyTabBar.Rounding, GsCol.VibrantPink.Uint(), GsCol.VibrantPinkHovered.Uint(), CkCol.CurvedHeader.Uint(),
            LabelFlags.PadInnerChild | LabelFlags.AddPaddingToHeight, out var selected, EditorTabs))
            selected?.DrawContents(ImGui.GetContentRegionAvail().X);
    }

    private void DrawEditorArea(float width)
    {
        if (_current.Effect is not { } eff)
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
            var selectedAttributes = (uint)eff.Attributes;
            using (var inner = ImRaii.Table("###AttributesTable", 2))
            {
                if (!inner) return;

                ImGui.TableNextColumn();
                foreach (var attribute in HypnoAttrExtensions.ToggleFlags)
                {
                    if (ImGui.CheckboxFlags(attribute.ToName(), ref selectedAttributes, (uint)attribute))
                        UpdateEffect(() => eff.Attributes ^= attribute);
                    CkGui.AttachToolTip(attribute.ToTooltip());
                    ImGui.TableNextColumn();
                }
            }

            // Spin Speed
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Image Spin Speed");
            ImGui.TableNextColumn();
            var spinRef = eff.SpinSpeed;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.SliderFloat("##SpinSpeed", ref spinRef, HypnoService.SPIN_SPEED_MIN, HypnoService.SPIN_SPEED_MAX, "%.2fx"))
            {
                UpdateEffect(() => eff.SpinSpeed = spinRef);
                _activeState.SpinSpeed = eff.SpinSpeed; // Update the active state for the preview.
            }

            // Zoom Depth
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Image Zoom Depth");
            ImGui.TableNextColumn();
            var zoomRef = eff.ZoomDepth;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.SliderFloat("##ZoomDepth", ref zoomRef, HypnoService.ZOOM_MIN, HypnoService.ZOOM_MAX, "%.2fx"))
                UpdateEffect(() => eff.ZoomDepth = zoomRef);

            // Text Mode
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Text Display Order");
            ImGui.TableNextColumn();
            var currentMode = eff.Attributes & HypnoAttributes.TextDisplayMask;

            if (ImGui.RadioButton(HypnoAttributes.TextDisplayOrdered.ToName(), currentMode == HypnoAttributes.TextDisplayOrdered))
                UpdateEffect(() => eff.Attributes = (eff.Attributes & ~HypnoAttributes.TextDisplayMask) | HypnoAttributes.TextDisplayOrdered);
            CkGui.AttachToolTip(HypnoAttributes.TextDisplayOrdered.ToTooltip());

            ImGui.SameLine();
            if (ImGui.RadioButton(HypnoAttributes.TextDisplayRandom.ToName(), currentMode == HypnoAttributes.TextDisplayRandom))
                UpdateEffect(() => eff.Attributes = (eff.Attributes & ~HypnoAttributes.TextDisplayMask) | HypnoAttributes.TextDisplayRandom);
            CkGui.AttachToolTip(HypnoAttributes.TextDisplayRandom.ToTooltip());

            // Text Scale Properties
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Text Scaling");
            ImGui.TableNextColumn();
            var scaleMode = eff.Attributes & HypnoAttributes.ScaleMask;

            if (ImGui.RadioButton("Static", scaleMode == 0))
                UpdateEffect(() => eff.Attributes &= ~HypnoAttributes.ScaleMask);
            CkGui.AttachToolTip("Text should remain the same size.");

            ImGui.SameLine();
            if (ImGui.RadioButton("Grows Overtime", scaleMode == HypnoAttributes.LinearTextScale))
                UpdateEffect(() => eff.Attributes = (eff.Attributes & ~HypnoAttributes.ScaleMask) | HypnoAttributes.LinearTextScale);
            CkGui.AttachToolTip(HypnoAttributes.LinearTextScale.ToTooltip());

            ImGui.SameLine();
            if (ImGui.RadioButton("Random Scale", scaleMode == HypnoAttributes.RandomTextScale))
                UpdateEffect(() => eff.Attributes = (eff.Attributes & ~HypnoAttributes.ScaleMask) | HypnoAttributes.RandomTextScale);
            CkGui.AttachToolTip(HypnoAttributes.RandomTextScale.ToTooltip());

            // Text Font Size
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Font Size");
            ImGui.TableNextColumn();
            var textSize = eff.TextFontSize;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.SliderInt("##TextSize", ref textSize, HypnoService.FONTSIZE_MIN, HypnoService.FONTSIZE_MAX, "%dpx"))
                UpdateEffect(() => eff.TextFontSize = textSize);

            // Stroke Thickness
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Stroke Thickness");
            ImGui.TableNextColumn();
            var strokeThickness = eff.StrokeThickness;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.SliderInt("##StrokeThickness", ref strokeThickness, HypnoService.STROKE_THICKNESS_MIN, HypnoService.STROKE_THICKNESS_MAX, "%dpx"))
                UpdateEffect(() => eff.StrokeThickness = strokeThickness);

            // Text Display Time
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImUtf8.TextFrameAligned("Text Display Time");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var textLife = eff.TextDisplayTime;
            if (ImGui.SliderInt("##TextLife", ref textLife, HypnoService.DISPLAY_TIME_MIN * 3, HypnoService.DISPLAY_TIME_MAX, $"%dms"))
            {
                UpdateEffect(() =>
                {
                    eff.TextDisplayTime = textLife;

                    // Fix up fade-in and fade-out to ensure their combined value doesn't exceed the new display time
                    var totalFade = eff.TextFadeInTime + eff.TextFadeOutTime;
                    if (totalFade > eff.TextDisplayTime)
                    {
                        // Proportionally reduce both if they exceed
                        var ratio = eff.TextDisplayTime / (float)totalFade;
                        eff.TextFadeInTime = (int)(eff.TextFadeInTime * ratio);
                        eff.TextFadeOutTime = (int)(eff.TextFadeOutTime * ratio);
                    }

                    // Clamp each to half the display time, in case one was 0
                    eff.TextFadeInTime = Math.Min(eff.TextFadeInTime, eff.TextDisplayTime / 2);
                    eff.TextFadeOutTime = Math.Min(eff.TextFadeOutTime, eff.TextDisplayTime - eff.TextFadeInTime);
                });
            }
            CkGui.AttachToolTip("How frequently the text cycles through the display words.");

            var hasFade = eff.Attributes.HasAny(HypnoAttributes.TextFade);
            var hasSpeedUp = eff.Attributes.HasAny(HypnoAttributes.SpeedUpOnCycle);
            var hasTranspose = eff.Attributes.HasAny(HypnoAttributes.TransposeColors);

            if (hasFade)
            {
                // Text Fade In
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Fade-In Time");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                var maxFadeIn = Math.Max(HypnoService.DISPLAY_TIME_MIN, eff.TextDisplayTime - eff.TextFadeOutTime);
                var fadeIn = eff.TextFadeInTime;
                if (ImGui.SliderInt("##TextFadeIn", ref fadeIn, HypnoService.DISPLAY_TIME_MIN, maxFadeIn, "%dms"))
                {
                    UpdateEffect(() =>
                    {
                        eff.TextFadeInTime = fadeIn;
                        // Adjust fadeout to not exceed.
                        eff.TextFadeOutTime = Math.Min(eff.TextFadeOutTime, eff.TextDisplayTime - eff.TextFadeInTime);
                    });
                }

                // Text Fade Out
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Fade-Out Time");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                var maxFadeOut = Math.Max(HypnoService.DISPLAY_TIME_MIN, eff.TextDisplayTime - eff.TextFadeInTime);
                var fadeOut = eff.TextFadeOutTime;
                if (ImGui.SliderInt("##TextFadeOut", ref fadeOut, HypnoService.DISPLAY_TIME_MIN, maxFadeOut, "%dms"))
                {
                    UpdateEffect(() =>
                    {
                        // Ensure fade out does not exceed the display time minus fade in.
                        eff.TextFadeOutTime = fadeOut;// Adjust fadein to not exceed.
                        eff.TextFadeInTime = Math.Min(eff.TextFadeInTime, eff.TextDisplayTime - eff.TextFadeOutTime);
                    });
                }
            }

            if (hasSpeedUp)
            {
                // Speed Up On Cycle
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Acceleration Time");
                ImGui.TableNextColumn();
                var speedUp = eff.SpeedupTime;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.SliderInt("##SpeedUpTime", ref speedUp, HypnoService.SPEED_BETWEEN_MIN, HypnoService.SPEED_BETWEEN_MAX, "%dms"))
                    UpdateEffect(() => eff.SpeedupTime = speedUp);
                CkGui.AttachToolTip(HypnoAttributes.SpeedUpOnCycle.ToTooltip());
            }

            if (hasTranspose)
            {
                // Transpose Colors
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Transpose Time");
                ImGui.TableNextColumn();
                var transposeRef = eff.TransposeTime;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.DragInt("##TransposeTime", ref transposeRef, 10f, HypnoService.DISPLAY_TIME_MIN, HypnoService.DISPLAY_TIME_MAX, "%dms"))
                    UpdateEffect(() => eff.TransposeTime = transposeRef);
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
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.EffectConfig, LastPos, LastSize);
    }

    private void DrawColorSections(float width)
    {
        var colorPickerWidth = (width / 3);

        using (ImRaii.Group())
        {
            CkGui.CenterTextAligned("Image Color", colorPickerWidth);
            ImGui.SetNextItemWidth(colorPickerWidth);
            var tintVec = ColorHelpers.RgbaUintToVector4(_current.Effect!.ImageColor);
            if (ImGui.ColorPicker4("##ImageColor", ref tintVec, COLOR_FLAGS))
            {
                UpdateEffect(() => _current.Effect.ImageColor = ColorHelpers.RgbaVector4ToUint(tintVec));
                _activeState.ImageColor = _current.Effect.ImageColor;
            }
        }

        ImGui.SameLine();
        using (ImRaii.Group())
        {
            CkGui.CenterTextAligned("Text Color", colorPickerWidth);
            ImGui.SetNextItemWidth(colorPickerWidth);
            var textColVec = ColorHelpers.RgbaUintToVector4(_current.Effect.TextColor);
            if (ImGui.ColorPicker4("##TextColor", ref textColVec, COLOR_FLAGS))
                UpdateEffect(() => _current.Effect.TextColor = ColorHelpers.RgbaVector4ToUint(textColVec));
        }

        ImGui.SameLine();
        using (ImRaii.Group())
        {
            CkGui.CenterTextAligned("Text Stroke Color", colorPickerWidth);
            ImGui.SetNextItemWidth(colorPickerWidth);
            var textColVec = ColorHelpers.RgbaUintToVector4(_current.Effect.StrokeColor);
            if (ImGui.ColorPicker4("##TextStrokeColor", ref textColVec, COLOR_FLAGS))
                UpdateEffect(() => _current.Effect.StrokeColor = ColorHelpers.RgbaVector4ToUint(textColVec));
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


    private unsafe Vector2 DisplayPreviewEffect(float sizeScale, string path, float rounding = 0)
    {
        if (_current.Effect is not { } eff || TextureManagerEx.GetMetadataPath(ImageDataType.Hypnosis, path) is not { } hypnoImage)
            return Vector2.Zero;

        try
        {
            // Recalculate the necessary cycle speed that we should need for the rotation (may need optimizations later)
            var speed = _activeState.SpinSpeed * 0.001f;
            var direction = eff.Attributes.HasFlag(HypnoAttributes.InvertDirection) ? -1f : 1f;
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
            var zoom = eff.ZoomDepth * sizeScale;

            // Impacted by zoom factor. (Nessisary for Pulsating)
            var corners = new[]
            {
            new Vector2(-hypnoImage.Width, -hypnoImage.Height) * eff.ZoomDepth,
            new Vector2(hypnoImage.Width, -hypnoImage.Height) * eff.ZoomDepth,
            new Vector2(hypnoImage.Width, hypnoImage.Height) * eff.ZoomDepth,
            new Vector2(-hypnoImage.Width, hypnoImage.Height) * eff.ZoomDepth
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
                hypnoImage.Handle,
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

            // If text is not present, or font is not valid, do not draw.
            if (_activeState.CurrentText.IsNullOrEmpty() || Fonts.FullscreenFontPtr.Handle is null)
                return screenSize;

            // determine the font scalar.
            var fontScaler = sizeScale * (eff.TextFontSize / Fonts.FullscreenFontPtr.FontSize) * _activeState.TextScale;

            // determine the new target position.
            var targetPos = eff.Attributes.HasAny(HypnoAttributes.LinearTextScale)
                ? center - Vector2.Lerp(sizeScale * _activeState.TextOffsetStart, sizeScale * _activeState.TextOffsetEnd, _activeState.TextScaleProgress)
                : center - (CkGui.CalcTextSizeFontPtr(Fonts.FullscreenFontPtr, _activeState.CurrentText) * fontScaler) * 0.5f;

            drawList.OutlinedFontScaled(
                Fonts.FullscreenFontPtr,
                Fonts.FullscreenFontPtr.FontSize,
                fontScaler,
                targetPos,
                _activeState.CurrentText,
                ColorHelpers.ApplyOpacity(eff.TextColor, _activeState.TextOpacity),
                ColorHelpers.ApplyOpacity(eff.StrokeColor, _activeState.TextOpacity),
                eff.StrokeThickness);

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
            var effect = _editorRef._current.Effect;
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
                    _editorRef.UpdateEffect(() => effect.Attributes ^= attribute);
                CkGui.AttachToolTip(attribute.ToTooltip());
                ImGui.NextColumn();
            }
            ImGui.Columns(1);
            ImUtf8.TextFrameAligned("Order:");
            ImUtf8.SameLineInner();
            var txtMode = effect.Attributes & HypnoAttributes.TextDisplayMask;
            if (ImGui.RadioButton(HypnoAttributes.TextDisplayOrdered.ToCompactName(), txtMode == HypnoAttributes.TextDisplayOrdered))
                _editorRef.UpdateEffect(() => effect.Attributes = (effect.Attributes & ~HypnoAttributes.TextDisplayMask) | HypnoAttributes.TextDisplayOrdered);
            CkGui.AttachToolTip(HypnoAttributes.TextDisplayOrdered.ToTooltip());

            ImGui.SameLine();
            if (ImGui.RadioButton(HypnoAttributes.TextDisplayRandom.ToCompactName(), txtMode == HypnoAttributes.TextDisplayRandom))
                _editorRef.UpdateEffect(() => effect.Attributes = (effect.Attributes & ~HypnoAttributes.TextDisplayMask) | HypnoAttributes.TextDisplayRandom);
            CkGui.AttachToolTip(HypnoAttributes.TextDisplayRandom.ToTooltip());

            // Type
            ImUtf8.TextFrameAligned("Scale:");
            ImUtf8.SameLineInner();
            var scaleMode = effect.Attributes & HypnoAttributes.ScaleMask;
            if (ImGui.RadioButton("Static", scaleMode == 0))
                _editorRef.UpdateEffect(() => effect.Attributes &= ~HypnoAttributes.ScaleMask);
            CkGui.AttachToolTip("Text should remain the same size.");

            ImUtf8.SameLineInner();
            if (ImGui.RadioButton("Grows", scaleMode == HypnoAttributes.LinearTextScale))
                _editorRef.UpdateEffect(() => effect.Attributes = (effect.Attributes & ~HypnoAttributes.ScaleMask) | HypnoAttributes.LinearTextScale);
            CkGui.AttachToolTip(HypnoAttributes.LinearTextScale.ToTooltip());

            ImUtf8.SameLineInner();
            if (ImGui.RadioButton("Random", scaleMode == HypnoAttributes.RandomTextScale))
                _editorRef.UpdateEffect(() => effect.Attributes = (effect.Attributes & ~HypnoAttributes.ScaleMask) | HypnoAttributes.RandomTextScale);
            CkGui.AttachToolTip(HypnoAttributes.RandomTextScale.ToTooltip());

            var fullWidth = ImGui.GetContentRegionAvail().X;

            var spinRef = effect.SpinSpeed;
            ImGui.SetNextItemWidth(fullWidth);
            if (ImGui.SliderFloat("##SpinSpeed", ref spinRef, HypnoService.SPIN_SPEED_MIN, HypnoService.SPIN_SPEED_MAX, "%.2fx Spin Speed"))
            {
                _editorRef.UpdateEffect(() => effect.SpinSpeed = spinRef);
                _editorRef._activeState.SpinSpeed = effect.SpinSpeed; // Update the active state for the preview.
            }

            var zoomRef = effect.ZoomDepth;
            ImGui.SetNextItemWidth(fullWidth);
            if (ImGui.SliderFloat("##ZoomDepth", ref zoomRef, HypnoService.ZOOM_MIN, HypnoService.ZOOM_MAX, "%.2fx Zoom"))
                _editorRef.UpdateEffect(() => effect.ZoomDepth = zoomRef);


            var textSize = effect.TextFontSize;
            ImGui.SetNextItemWidth(fullWidth);
            if (ImGui.SliderInt("##TextSize", ref textSize, HypnoService.FONTSIZE_MIN, HypnoService.FONTSIZE_MAX, "%dpx Font Size"))
                _editorRef.UpdateEffect(() => effect.TextFontSize = textSize);

            var strokeThickness = effect.StrokeThickness;
            ImGui.SetNextItemWidth(fullWidth);
            if (ImGui.SliderInt("##StrokeThickness", ref strokeThickness, HypnoService.STROKE_THICKNESS_MIN, HypnoService.STROKE_THICKNESS_MAX, "%dpx Outline"))
                _editorRef.UpdateEffect(() => effect.StrokeThickness = strokeThickness);


            ImGui.SetNextItemWidth(fullWidth);
            var textLife = effect.TextDisplayTime;
            if (ImGui.SliderInt("##TextLife", ref textLife, HypnoService.DISPLAY_TIME_MIN, HypnoService.DISPLAY_TIME_MAX, $"%dms per phase"))
            {
                _editorRef.UpdateEffect(() =>
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
                });
            }
            CkGui.AttachToolTip("How frequently the text cycles through the display words.");

            if (hasFade)
            {
                var maxFadeIn = Math.Max(HypnoService.DISPLAY_TIME_MIN, effect.TextDisplayTime - effect.TextFadeOutTime);
                var fadeIn = effect.TextFadeInTime;
                ImGui.SetNextItemWidth(fullWidth);
                if (ImGui.SliderInt("##TextFadeIn", ref fadeIn, HypnoService.DISPLAY_TIME_MIN, maxFadeIn, "%dms Fade-In Time"))
                {
                    _editorRef.UpdateEffect(() =>
                    {
                        effect.TextFadeInTime = fadeIn;
                        // Adjust fadeout to not exceed.
                        effect.TextFadeOutTime = Math.Min(effect.TextFadeOutTime, effect.TextDisplayTime - effect.TextFadeInTime);
                    });
                }

                var maxFadeOut = Math.Max(HypnoService.DISPLAY_TIME_MIN, effect.TextDisplayTime - effect.TextFadeInTime);
                var fadeOut = effect.TextFadeOutTime;
                ImGui.SetNextItemWidth(fullWidth);
                if (ImGui.SliderInt("##TextFadeOut", ref fadeOut, HypnoService.DISPLAY_TIME_MIN, maxFadeOut, "%dms Fade-Out Time"))
                {
                    _editorRef.UpdateEffect(() =>
                    {
                        effect.TextFadeOutTime = fadeOut;
                        // Adjust fadein to not exceed.
                        effect.TextFadeInTime = Math.Min(effect.TextFadeInTime, effect.TextDisplayTime - effect.TextFadeOutTime);
                    });
                }
            }

            if (hasSpeedUp)
            {
                var speedUp = effect.SpeedupTime;
                ImGui.SetNextItemWidth(fullWidth);
                if (ImGui.SliderInt("##SpeedUpTime", ref speedUp, HypnoService.SPEED_BETWEEN_MIN, HypnoService.SPEED_BETWEEN_MAX, "%dms Transition Time"))
                    _editorRef.UpdateEffect(() => effect.SpeedupTime = speedUp);
                CkGui.AttachToolTip(HypnoAttributes.SpeedUpOnCycle.ToTooltip());
            }

            if (hasTranspose)
            {
                var transposeRef = effect.TransposeTime;
                ImGui.SetNextItemWidth(fullWidth);
                if (ImGui.SliderInt("##TransposeTime", ref transposeRef, HypnoService.DISPLAY_TIME_MIN, HypnoService.DISPLAY_TIME_MAX, "%dms Transpose Time"))
                    _editorRef.UpdateEffect(() => effect.TransposeTime = transposeRef);
            }
        }
    }
    internal class CompactPhrasesColorsTab : IFancyTab
    {
        private readonly HypnoEffectEditor _editorRef;
        public string Label => "Text & Color";
        public string Tooltip => "Adjust Displayed Phrases & Colors!";
        public bool Disabled => false;
        public CompactPhrasesColorsTab(HypnoEffectEditor editor) => _editorRef = editor;
        public void DrawContents(float width)
        {
            var effect = _editorRef._current.Effect;
            var activeState = _editorRef._activeState;
            if (effect is null) return;
            var height = CkStyle.GetFrameRowsHeight(3);
            using (CkRaii.FramedChildPaddedW($"##HypnoT_{_editorRef.PopupLabel}", width, height, 0, GsCol.VibrantPink.Uint(), DFlags.RoundCornersAll))
            {
                if (_editorRef._displayTextEditor.DrawTagsEditor($"##EffectPhrases_{_editorRef.PopupLabel}", effect.DisplayMessages, out var newDisplayWords, GsCol.VibrantPink.Vec4()))
                    _editorRef.UpdateEffect(() => effect.DisplayMessages = newDisplayWords.ToArray());

                if (_editorRef._displayTextEditor.DrawHelpButtons(effect.DisplayMessages, out var newWords, true, GsCol.VibrantPink.Vec4()))
                    _editorRef.UpdateEffect(() => effect.DisplayMessages = newWords.ToArray());
            }


            CkGui.CenterTextAligned("Image Color");
            ImGui.SetNextItemWidth(width);
            var tintVec = ColorHelpers.RgbaUintToVector4(effect!.ImageColor);
            if (ImGui.ColorEdit4("##EffectImageColor", ref tintVec, KINKSTER_COLOR_FLAGS))
            {
                _editorRef.UpdateEffect(() => effect.ImageColor = ColorHelpers.RgbaVector4ToUint(tintVec));
                activeState.ImageColor = effect.ImageColor;
            }

            CkGui.CenterTextAligned("Text Color");
            ImGui.SetNextItemWidth(width);
            var textColVec = ColorHelpers.RgbaUintToVector4(effect.TextColor);
            if (ImGui.ColorEdit4("##EffectTextColor", ref textColVec, KINKSTER_COLOR_FLAGS))
                _editorRef.UpdateEffect(() => effect.TextColor = ColorHelpers.RgbaVector4ToUint(textColVec));

            CkGui.CenterTextAligned("Text Stroke Color", width);
            ImGui.SetNextItemWidth(width);
            var textOutlineColVec = ColorHelpers.RgbaUintToVector4(effect.StrokeColor);
            if (ImGui.ColorEdit4("##EffectStrokeColor", ref textOutlineColVec, KINKSTER_COLOR_FLAGS))
                _editorRef.UpdateEffect(() => effect.StrokeColor = ColorHelpers.RgbaVector4ToUint(textOutlineColVec));
        }
    }

    internal class CompactPresetsTab : IFancyTab
    {
        private readonly HypnoEffectManager _presetManager;
        private readonly HypnoEffectEditor _editorRef;
        public string Label => "Presets";
        public string Tooltip => "Set, Create, Remove, Rename, or Modify Presets!";
        public bool Disabled => false;
        public CompactPresetsTab(HypnoEffectEditor editor, HypnoEffectManager presets)
        {
            _editorRef = editor;
            _presetManager = presets;
        }

        private (Guid EffectId, string NewName) _tmpRenameVars = (Guid.Empty, string.Empty);

        public void DrawContents(float width)
        {
            var itemSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());

            DrawAddCurrentButton(itemSize);

            foreach (var (name, preset) in _presetManager.Presets)
            {
                var bindedPreset = _editorRef._current.Name == name;
                if (DrawPresetItemBox(name, preset, bindedPreset))
                    break;
                CkGui.AttachToolTip("Keybinds:" +
                    "--SEP----COL--[Double-Click]--COL-- Load Preset" +
                    "--NL----COL--[Right-Click]--COL-- Rename Preset" +
                    "--NL----COL--[CTRL + Left-Click]--COL-- Toggle Binding Mode." +
                    "--NL--(Binding mode saves changes to bound preset)", color: GsCol.VibrantPink.Vec4());
            }

            bool DrawPresetItemBox(string setName, HypnoticEffect preset, bool isBinded)
            {
                var pos = ImGui.GetCursorScreenPos();
                var hovering = ImGui.IsMouseHoveringRect(pos, pos + itemSize);
                var color = hovering ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkCol.CurvedHeaderFade.Uint();
                var frameCol = isBinded ? 0xFF00FF00 : GsCol.VibrantPink.Uint();
                using (CkRaii.FramedChild($"Preset-{setName}", itemSize, color, frameCol, CkStyle.HeaderRounding(), 1.5f * ImGuiHelpers.GlobalScale))
                {
                    var comboW = ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.Eraser).X - ImGui.GetStyle().ItemInnerSpacing.X;
                    // Renaming Mode.
                    if (_tmpRenameVars.EffectId.Equals(preset.EffectId))
                    {
                        ImGui.SetNextItemWidth(comboW);
                        ImGui.InputText($"##RenamePreset-{setName}", ref _tmpRenameVars.NewName, 255);
                        // on deactivation, rename the preset, update the selected preset, and if it was in binding mode, update binding mode.
                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            // Update the new name to ensure uniqueness among all other preset names.
                            var newName = RegexEx.EnsureUniqueName(_tmpRenameVars.NewName, _presetManager.Presets.Keys, x => x);
                            // Attempt to rename the preset, if it fails, do nothing.
                            if (!_presetManager.TryRenamePreset(setName, newName))
                            {
                                _tmpRenameVars = (Guid.Empty, string.Empty);
                                return false;
                            }

                            // It Succeeded. Update preset to the effect data of new name, set to binding mode if it was previously.
                            _editorRef.SetEffectWithPresetValues(newName, _editorRef.InBindingMode);
                            Svc.Logger.Verbose($"Renamed Hypno Effect Preset: {setName} to {newName}");
                            _tmpRenameVars = (Guid.Empty, string.Empty);
                            return true;
                        }
                    }
                    else
                    {
                        ImGui.Spacing();
                        CkGui.TextFrameAlignedInline(setName);
                    }
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.Eraser).X);
                    // if selected, remove the preset from the preset manager (but don't clear the effect).
                    if (CkGui.IconButton(FAI.Eraser, inPopup: true))
                    {
                        _presetManager.RemovePreset(setName);
                        Svc.Logger.Verbose($"Removed Hypno Effect Preset: {setName}");
                        _editorRef._current.Name = string.Empty; // this removes binding mode but keeps effects.
                        return true;
                    }
                }
                // If right clicked, toggle renaming mode.
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && hovering)
                {
                    _tmpRenameVars = _tmpRenameVars.EffectId == Guid.Empty
                        ? _tmpRenameVars = (preset.EffectId, setName)
                        : _tmpRenameVars = (Guid.Empty, string.Empty);
                    Svc.Logger.Verbose($"Hypno Effect Preset '{setName}' toggled renaming mode.");
                }

                // if double clicked, process selecting, or binding a preset.
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && hovering)
                {
                    // it CTRL was clicked, process a binding over an application.
                    if (ImGui.GetIO().KeyCtrl)
                    {
                        if (_editorRef.InBindingMode)
                            _editorRef._current.Name = string.Empty;
                        else
                            _editorRef.SetEffectWithPresetValues(setName, true);
                        Svc.Logger.Verbose($"Hypno Effect Preset '{setName}' toggled binding mode.");
                    }
                    else
                    {
                        // Apply the preset item effects to the current editor.
                        _editorRef.SetEffectWithPresetValues(setName);
                        Svc.Logger.Verbose($"Applied Hypnotic Effects from Preset: {setName} to current options!");
                    }
                    return true;
                }
                return false;
            }
        }

        private void DrawAddCurrentButton(Vector2 size)
        {
            var pos = ImGui.GetCursorScreenPos();
            var hovering = ImGui.IsMouseHoveringRect(pos, pos + size);
            var color = hovering ? CkCol.LChildBg.Uint() : CkCol.CurvedHeader.Uint();
            using (CkRaii.FramedChild("NewPresetButton", size, color, GsCol.VibrantPink.Uint(), CkStyle.HeaderRounding(), 1.5f * ImGuiHelpers.GlobalScale))
                CkGui.CenterTextAligned("New Preset From Options");

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && hovering && _editorRef._current.Effect is { } eff)
            {
                // generate a random name, starting with _, that is 10 character long, and not in _presetManager.Presets.Keys.
                var random = new Random();
                var newPresetName = $"_{string.Concat(Enumerable.Range(0, 5).Select(_ => (char)('a' + random.Next(0, 26))))}";
                while (_presetManager.Presets.ContainsKey(newPresetName))
                    newPresetName = $"_{string.Concat(Enumerable.Range(0, 5).Select(_ => (char)('a' + random.Next(0, 26))))}";

                // Create a new effect from the current editor effect options.
                var newEffect = new HypnoticEffect(eff);
                newEffect.EffectId = Guid.NewGuid();
                // set the current effect to be the one from the preset manager.
                if (_presetManager.TryAddPreset(newPresetName, newEffect))
                    _editorRef.SetEffectWithPresetValues(newPresetName);
            }
        }
    }
}
