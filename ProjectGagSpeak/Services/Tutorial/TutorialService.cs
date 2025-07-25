using Dalamud.Interface.Colors;
using GagSpeak.Localization;
using ImGuiNET;
using System.Runtime.CompilerServices;

// A Modified take on OtterGui.Widgets.Tutorial.
// This iteration removes redundant buttons, adds detailed text, and sections.
namespace GagSpeak.Services.Tutorial;

/// <summary>
///     Service for the in-game tutorial.
/// </summary>
public class TutorialService
{
    private readonly Dictionary<TutorialType, Tutorial> _tutorials = new();

    public TutorialService() { }
    public bool IsTutorialActive(TutorialType type) => _tutorials[type].CurrentStep is not -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartTutorial(TutorialType guide)
    {
        if (!_tutorials.ContainsKey(guide))
            return;

        // set all other tutorials to -1, stopping them.
        foreach (var t in _tutorials)
            t.Value.CurrentStep = (t.Key != guide) ?  -1 : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OpenTutorial<TEnum>(TutorialType guide, TEnum step, Vector2 pos, Vector2 size, Action? onNext = null) where TEnum : Enum
    {
        if (_tutorials.TryGetValue(guide, out var tutorial))
            tutorial.Open(Convert.ToInt32(step), pos, size, onNext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SkipTutorial(TutorialType guide)
    {
        // reset the step to -1, stopping the tutorial.
        if (_tutorials.TryGetValue(guide, out var tutorial))
            tutorial.CurrentStep = -1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void JumpToStep<TEnum>(TutorialType guide, TEnum step)
    {
        // reset the step to -1, stopping the tutorial.
        if (_tutorials.TryGetValue(guide, out var tutorial))
            tutorial.CurrentStep = Convert.ToInt32(step);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CurrentStep(TutorialType guide)
    {
        if (_tutorials.TryGetValue(guide, out var tutorial))
            return tutorial.CurrentStep;

        return -1;
    }

    // Create a mappinng between the tutorialTypes and the associated enum size.
    private static readonly Dictionary<TutorialType, int> _tutorialSizes = new()
    {
        { TutorialType.MainUi, Enum.GetValues<StepsMainUi>().Length },
        { TutorialType.Remote, Enum.GetValues<StepsRemote>().Length },
        { TutorialType.Restraints, Enum.GetValues<StepsRestraints>().Length },
        { TutorialType.Restrictions, Enum.GetValues<StepsRestrictions>().Length },
        { TutorialType.Gags, Enum.GetValues<StepsGags>().Length },
        { TutorialType.CursedLoot, Enum.GetValues<StepsCursedLoot>().Length },
        { TutorialType.Puppeteer, Enum.GetValues<StepsPuppeteer>().Length },
        { TutorialType.Toys, Enum.GetValues<StepsToys>().Length },
        { TutorialType.VibeLobby, Enum.GetValues<StepsVibeLobby>().Length },
        { TutorialType.Patterns, Enum.GetValues<StepsPatterns>().Length },
        { TutorialType.Triggers, Enum.GetValues<StepsTriggers>().Length },
        { TutorialType.Alarms, Enum.GetValues<StepsAlarms>().Length },
        { TutorialType.Achievements, Enum.GetValues<StepsAchievements>().Length },
    };

    public void InitializeTutorialStrings()
    {
        var mainUiStr = GSLoc.Tutorials.MainUi;
        _tutorials[TutorialType.MainUi] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Main UI Tutorial",
        }
        .AddStep(mainUiStr.Step1Title, mainUiStr.Step1Desc, mainUiStr.Step1DescExtended)
        .AddStep(mainUiStr.Step2Title, mainUiStr.Step2Desc, mainUiStr.Step2DescExtended)
        .AddStep(mainUiStr.Step3Title, mainUiStr.Step3Desc, mainUiStr.Step3DescExtended)
        .AddStep(mainUiStr.Step4Title, mainUiStr.Step4Desc, mainUiStr.Step4DescExtended)
        .AddStep(mainUiStr.Step5Title, mainUiStr.Step5Desc, mainUiStr.Step5DescExtended)
        .AddStep(mainUiStr.Step6Title, mainUiStr.Step6Desc, mainUiStr.Step6DescExtended)
        .AddStep(mainUiStr.Step7Title, mainUiStr.Step7Desc, mainUiStr.Step7DescExtended)
        .AddStep(mainUiStr.Step8Title, mainUiStr.Step8Desc, mainUiStr.Step8DescExtended)
        .AddStep(mainUiStr.Step9Title, mainUiStr.Step9Desc, mainUiStr.Step9DescExtended)
        .AddStep(mainUiStr.Step10Title, mainUiStr.Step10Desc, mainUiStr.Step10DescExtended)
        .AddStep(mainUiStr.Step11Title, mainUiStr.Step11Desc, mainUiStr.Step11DescExtended)
        .AddStep(mainUiStr.Step12Title, mainUiStr.Step12Desc, mainUiStr.Step12DescExtended)
        .AddStep(mainUiStr.Step13Title, mainUiStr.Step13Desc, mainUiStr.Step13DescExtended)
        .AddStep(mainUiStr.Step14Title, mainUiStr.Step14Desc, mainUiStr.Step14DescExtended)
        .AddStep(mainUiStr.Step15Title, mainUiStr.Step15Desc, mainUiStr.Step15DescExtended)
        .AddStep(mainUiStr.Step16Title, mainUiStr.Step16Desc, mainUiStr.Step16DescExtended)
        .AddStep(mainUiStr.Step17Title, mainUiStr.Step17Desc, mainUiStr.Step17DescExtended)
        .AddStep(mainUiStr.Step18Title, mainUiStr.Step18Desc, mainUiStr.Step18DescExtended)
        .AddStep(mainUiStr.Step19Title, mainUiStr.Step19Desc, mainUiStr.Step19DescExtended)
        .AddStep(mainUiStr.Step20Title, mainUiStr.Step20Desc, mainUiStr.Step20DescExtended)
        .AddStep(mainUiStr.Step21Title, mainUiStr.Step21Desc, mainUiStr.Step21DescExtended)
        .AddStep(mainUiStr.Step22Title, mainUiStr.Step22Desc, mainUiStr.Step22DescExtended)
        .AddStep(mainUiStr.Step23Title, mainUiStr.Step23Desc, mainUiStr.Step23DescExtended)
        .AddStep(mainUiStr.Step24Title, mainUiStr.Step24Desc, mainUiStr.Step24DescExtended)
        .AddStep(mainUiStr.Step25Title, mainUiStr.Step25Desc, mainUiStr.Step25DescExtended)
        .AddStep(mainUiStr.Step26Title, mainUiStr.Step26Desc, mainUiStr.Step26DescExtended)
        .AddStep(mainUiStr.Step27Title, mainUiStr.Step27Desc, mainUiStr.Step27DescExtended)
        .AddStep(mainUiStr.Step28Title, mainUiStr.Step28Desc, mainUiStr.Step28DescExtended)
        .AddStep(mainUiStr.Step29Title, mainUiStr.Step29Desc, mainUiStr.Step29DescExtended)
        .AddStep(mainUiStr.Step30Title, mainUiStr.Step30Desc, mainUiStr.Step30DescExtended)
        .AddStep(mainUiStr.Step31Title, mainUiStr.Step31Desc, mainUiStr.Step31DescExtended)
        .AddStep(mainUiStr.Step32Title, mainUiStr.Step32Desc, mainUiStr.Step32DescExtended)
        .AddStep(mainUiStr.Step33Title, mainUiStr.Step33Desc, mainUiStr.Step33DescExtended)
        .EnsureSize(_tutorialSizes[TutorialType.MainUi]);

        var remoteStr = GSLoc.Tutorials.Remote;
        _tutorials[TutorialType.Remote] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Remote Tutorial",
        }
        .EnsureSize(0);

        var restraintsStr = GSLoc.Tutorials.Restraints;
        _tutorials[TutorialType.Restraints] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Restraints Tutorial",
        }
        .EnsureSize(0);

        var restrictionsStr = GSLoc.Tutorials.Restrictions;
        _tutorials[TutorialType.Restrictions] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Restrictions Tutorial",
        }
        .EnsureSize(0);

        var gagsStr = GSLoc.Tutorials.Gags;
        _tutorials[TutorialType.Gags] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Gags Tutorial",
        }
        .EnsureSize(0);

        var cursedLootStr = GSLoc.Tutorials.CursedLoot;
        _tutorials[TutorialType.CursedLoot] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Cursed Loot Tutorial",
        }
        .EnsureSize(0);

        var puppetStr = GSLoc.Tutorials.Puppeteer;
        _tutorials[TutorialType.Puppeteer] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Puppeteer Tutorial",
        }
        .EnsureSize(0);

        var toyboxStr = GSLoc.Tutorials.Toys;
        _tutorials[TutorialType.Toys] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Toybox Tutorial",
        }
        .EnsureSize(0);

        var patternsStr = GSLoc.Tutorials.Patterns;
        _tutorials[TutorialType.Patterns] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Patterns Tutorial",
        }
        .EnsureSize(0);

        var alarmsStr = GSLoc.Tutorials.Alarms;
        _tutorials[TutorialType.Alarms] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Alarms Tutorial",
        }
        .EnsureSize(0);

        var triggersStr = GSLoc.Tutorials.Triggers;
        _tutorials[TutorialType.Triggers] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Triggers Tutorial",
        }
        .EnsureSize(0);

        var achievementsStr = GSLoc.Tutorials.Achievements;
        _tutorials[TutorialType.Achievements] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Achievements Tutorial",
        }
        .AddStep(achievementsStr.Step1Title, achievementsStr.Step1Desc, string.Empty)
        .AddStep(achievementsStr.Step2Title, achievementsStr.Step2Desc, string.Empty)
        .AddStep(achievementsStr.Step3Title, achievementsStr.Step3Desc, string.Empty)
        .AddStep(achievementsStr.Step4Title, achievementsStr.Step4Desc, string.Empty)
        .AddStep(achievementsStr.Step5Title, achievementsStr.Step5Desc, achievementsStr.Step5DescExtended)
        .AddStep(achievementsStr.Step6Title, achievementsStr.Step6Desc, achievementsStr.Step6DescExtended)
        .EnsureSize(_tutorialSizes[TutorialType.Achievements]);
    }
}
