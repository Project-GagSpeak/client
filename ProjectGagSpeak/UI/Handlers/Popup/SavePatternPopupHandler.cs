using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using ImGuiNET;

namespace GagSpeak.Gui.Components;

/// <summary> A interface for handling the popups in the UI. </summary>
public class SavePatternPopupHandler : IPopupHandler
{
    private readonly GagspeakMediator _mediator;
    private readonly PatternManager _patterns;

    private readonly TutorialService _guides;
    private Pattern CompiledPatternData = new Pattern(); // compile a new pattern to save

    // tag management
    private float SaveWidth;
    private float RevertWidth;
    private const float PopupWidth = 270;
    public SavePatternPopupHandler(GagspeakMediator mediator, PatternManager patterns, TutorialService guides)
    {
        _mediator = mediator;
        _patterns = patterns;

        _guides = guides;
    }

    private Vector2 _size = new(PopupWidth, 400);
    public Vector2 PopupSize => _size;
    public bool ShowClosed => false;
    public bool CloseHovered { get; set; } = false;
    public Vector2? WindowPadding => null;
    public float? WindowRounding => null;

    public void DrawContent()
    {
        SaveWidth = CkGui.IconTextButtonSize(FAI.Save, "Save Pattern Data");
        RevertWidth = CkGui.IconTextButtonSize(FAI.Undo, "Discard Pattern");
        var start = 0f;
        using (UiFontService.UidFont.Push())
        {
            start = ImGui.GetCursorPosY() - ImGui.CalcTextSize("Create New Pattern").Y;
            ImGui.Text("Create New Pattern");
        }
        ImGuiHelpers.ScaledDummy(5f);
        ImGui.Separator();
        var name = CompiledPatternData.Label;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputTextWithHint("Pattern Name", "Enter a name...", ref name, 48))
        {
            CompiledPatternData.Label = name;
        }
        // _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.SavingPatternName, ImGui.GetWindowPos(), _size,
        //    () => CompiledPatternData.Label = "Tutorial Pattern");

        // description field
        var description = CompiledPatternData.Description;
        if (ImGui.InputTextMultiline("Description", ref description, 256, new Vector2(150, 100)))
        {
            CompiledPatternData.Description = description;
        }
        // _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.SavingPatternDescription, ImGui.GetWindowPos(), _size);

        // duration field.
        ImGui.Text("Pattern Duration: ");
        ImGui.SameLine();
        var text = CompiledPatternData.Duration.Hours > 0
                    ? CompiledPatternData.Duration.ToString("hh\\:mm\\:ss")
                    : CompiledPatternData.Duration.ToString("mm\\:ss");
        CkGui.ColorText(text, ImGuiColors.ParsedPink);
        // loop field
        var loop = CompiledPatternData.ShouldLoop;
        if (ImGui.Checkbox("Loop Pattern", ref loop))
        {
            CompiledPatternData.ShouldLoop = loop;
        }
        // _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.SavingPatternLoop, ImGui.GetWindowPos(), _size);

        // display save options
        ImGui.Separator();
        if (CkGui.IconTextButton(FAI.Save, "Save Pattern Data", SaveWidth))
            Close();
        // _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.FinalizingSave, ImGui.GetWindowPos(), _size, () => _mediator.Publish(new ClosePatternSavePromptMessage()));

        ImGui.SameLine();
        if (CkGui.IconTextButton(FAI.Undo, "Discard Pattern", RevertWidth, disabled: _guides.IsTutorialActive(TutorialType.Patterns)))
        {
            CompiledPatternData = new Pattern();
            ImGui.CloseCurrentPopup();
        }
        // _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.DiscardingPattern, ImGui.GetWindowPos(), _size);

        var height = ImGui.GetCursorPosY() - start;
        _size = _size with { Y = height };
    }

    public void Open(PatternSavePromptMessage message)
    {
        // compile a fresh pattern object
        CompiledPatternData = new Pattern();
        // set the duration
        CompiledPatternData.Duration = message.Duration;
        CompiledPatternData.PlaybackDuration = message.Duration;
        // set the pattern data
        CompiledPatternData.PlaybackData = message.Data;
    }

    public void Close()
    {
        _patterns.CreateClone(CompiledPatternData, CompiledPatternData.Label);
        ImGui.CloseCurrentPopup();
    }
}
