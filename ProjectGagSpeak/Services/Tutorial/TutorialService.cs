using Dalamud.Interface.Colors;
using GagSpeak.Localization;
using GagSpeak.UI;
using ImGuiNET;
using OtterGui.Services;
using System.Numerics;
using System.Runtime.CompilerServices;

// A Modified take on OtterGui.Widgets.Tutorial.
// This iteration removes redundant buttons, adds detailed text, and sections.
namespace GagSpeak.Services.Tutorial;

/// <summary> Service for the in-game tutorial. </summary>
public class TutorialService
{
    private readonly UiSharedService _uiShared;

    private readonly Dictionary<TutorialType, Tutorial> _tutorials = new();

    public TutorialService(UiSharedService uiShared)
    {
        _uiShared = uiShared;
    }

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
        { TutorialType.Gags, Enum.GetValues<StepsActiveGags>().Length },
        { TutorialType.GagStorage, Enum.GetValues<StepsGagStorage>().Length },
        { TutorialType.Restraints, Enum.GetValues<StepsRestraints>().Length },
        { TutorialType.CursedLoot, Enum.GetValues<StepsCursedLoot>().Length },
        { TutorialType.Toybox, Enum.GetValues<StepsToybox>().Length },
        { TutorialType.Patterns, Enum.GetValues<StepsPatterns>().Length },
        { TutorialType.Triggers, Enum.GetValues<StepsTriggers>().Length },
        { TutorialType.Alarms, Enum.GetValues<StepsAlarms>().Length },
        { TutorialType.Achievements, Enum.GetValues<StepsAchievements>().Length },
    };

    public void InitializeTutorialStrings()
    {
        var mainUiStr = GSLoc.Tutorials.HelpMainUi;
        _tutorials[TutorialType.MainUi] = new Tutorial(_uiShared)
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Main UI Tutorial",
        }
        .AddStep(mainUiStr.Step1Title, mainUiStr.Step1Desc, mainUiStr.Step1DescExtended)
        .AddStep(mainUiStr.Step2Title, mainUiStr.Step2Desc, string.Empty)
        .AddStep(mainUiStr.Step3Title, mainUiStr.Step3Desc, mainUiStr.Step3DescExtended)
        .AddStep(mainUiStr.Step4Title, mainUiStr.Step4Desc, mainUiStr.Step4DescExtended)
        .AddStep(mainUiStr.Step5Title, mainUiStr.Step5Desc, mainUiStr.Step5DescExtended)
        .AddStep(mainUiStr.Step6Title, mainUiStr.Step6Desc, mainUiStr.Step6DescExtended)
        .AddStep(mainUiStr.Step7Title, mainUiStr.Step7Desc, string.Empty)
        .AddStep(mainUiStr.Step8Title, mainUiStr.Step8Desc, mainUiStr.Step8DescExtended)
        .AddStep(mainUiStr.Step9Title, mainUiStr.Step9Desc, mainUiStr.Step9DescExtended)
        .AddStep(mainUiStr.Step10Title, mainUiStr.Step10Desc, string.Empty)
        .AddStep(mainUiStr.Step11Title, mainUiStr.Step11Desc, string.Empty)
        .AddStep(mainUiStr.Step12Title, mainUiStr.Step12Desc, string.Empty)
        .AddStep(mainUiStr.Step13Title, mainUiStr.Step13Desc, string.Empty)
        .AddStep(mainUiStr.Step14Title, mainUiStr.Step14Desc, string.Empty)
        .AddStep(mainUiStr.Step15Title, mainUiStr.Step15Desc, mainUiStr.Step15DescExtended)
        .AddStep(mainUiStr.Step16Title, mainUiStr.Step16Desc, string.Empty)
        .AddStep(mainUiStr.Step17Title, mainUiStr.Step17Desc, string.Empty)
        .AddStep(mainUiStr.Step18Title, mainUiStr.Step18Desc, string.Empty)
        .AddStep(mainUiStr.Step19Title, mainUiStr.Step19Desc, string.Empty)
        .AddStep(mainUiStr.Step20Title, mainUiStr.Step20Desc, string.Empty)
        .AddStep(mainUiStr.Step21Title, mainUiStr.Step21Desc, mainUiStr.Step21DescExtended)
        .EnsureSize(_tutorialSizes[TutorialType.MainUi]);

        var remoteStr = GSLoc.Tutorials.HelpRemote;
        _tutorials[TutorialType.Remote] = new Tutorial(_uiShared)
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Remote Tutorial",
        }
        .AddStep(remoteStr.Step1Title, remoteStr.Step1Desc, string.Empty)
        .AddStep(remoteStr.Step2Title, remoteStr.Step2Desc, remoteStr.Step2DescExtended)
        .AddStep(remoteStr.Step3Title, remoteStr.Step3Desc, remoteStr.Step3DescExtended)
        .AddStep(remoteStr.Step4Title, remoteStr.Step4Desc, string.Empty)
        .AddStep(remoteStr.Step5Title, remoteStr.Step5Desc, string.Empty)
        .AddStep(remoteStr.Step6Title, remoteStr.Step6Desc, string.Empty)
        .AddStep(remoteStr.Step7Title, remoteStr.Step7Desc, remoteStr.Step7DescExtended)
        .EnsureSize(_tutorialSizes[TutorialType.Remote]);

        var gagsStr = GSLoc.Tutorials.HelpGags;
        _tutorials[TutorialType.Gags] = new Tutorial(_uiShared)
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Gags Tutorial",
        }
        .AddStep(gagsStr.Step1Title, gagsStr.Step1Desc, gagsStr.Step1DescExtended)
        .AddStep(gagsStr.Step2Title, gagsStr.Step2Desc, string.Empty)
        .AddStep(gagsStr.Step3Title, gagsStr.Step3Desc, string.Empty)
        .AddStep(gagsStr.Step4Title, gagsStr.Step4Desc, string.Empty)
        .AddStep(gagsStr.Step5Title, gagsStr.Step5Desc, gagsStr.Step5DescExtended)
        .AddStep(gagsStr.Step6Title, gagsStr.Step6Desc, string.Empty)
        .AddStep(gagsStr.Step7Title, gagsStr.Step7Desc, string.Empty)
        .EnsureSize(_tutorialSizes[TutorialType.Gags]);

        var gagsStorageStr = GSLoc.Tutorials.HelpGagStorage;
        _tutorials[TutorialType.GagStorage] = new Tutorial(_uiShared)
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Gag Storage Tutorial",
        }
        .AddStep(gagsStorageStr.Step1Title, gagsStorageStr.Step1Desc, string.Empty)
        .AddStep(gagsStorageStr.Step2Title, gagsStorageStr.Step2Desc, string.Empty)
        .AddStep(gagsStorageStr.Step3Title, gagsStorageStr.Step3Desc, string.Empty)
        .AddStep(gagsStorageStr.Step4Title, gagsStorageStr.Step4Desc, string.Empty)
        .AddStep(gagsStorageStr.Step5Title, gagsStorageStr.Step5Desc, string.Empty)
        .AddStep(gagsStorageStr.Step6Title, gagsStorageStr.Step6Desc, string.Empty)
        .EnsureSize(_tutorialSizes[TutorialType.GagStorage]);

        var restraintsStr = GSLoc.Tutorials.HelpRestraints;
        _tutorials[TutorialType.Restraints] = new Tutorial(_uiShared)
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Restraints Tutorial",
        }
        .AddStep(restraintsStr.Step1Title, restraintsStr.Step1Desc, string.Empty)
        .AddStep(restraintsStr.Step2Title, restraintsStr.Step2Desc, string.Empty)
        .AddStep(restraintsStr.Step3Title, restraintsStr.Step3Desc, string.Empty)
        .AddStep(restraintsStr.Step4Title, restraintsStr.Step4Desc, string.Empty)
        .AddStep(restraintsStr.Step5Title, restraintsStr.Step5Desc, string.Empty)
        .AddStep(restraintsStr.Step6Title, restraintsStr.Step6Desc, string.Empty)
        .AddStep(restraintsStr.Step7Title, restraintsStr.Step7Desc, string.Empty)
        .AddStep(restraintsStr.Step8Title, restraintsStr.Step8Desc, string.Empty)
        .AddStep(restraintsStr.Step9Title, restraintsStr.Step9Desc, string.Empty)
        .AddStep(restraintsStr.Step10Title, restraintsStr.Step10Desc, string.Empty)
        .AddStep(restraintsStr.Step11Title, restraintsStr.Step11Desc, string.Empty)
        .AddStep(restraintsStr.Step12Title, restraintsStr.Step12Desc, string.Empty)
        .AddStep(restraintsStr.Step13Title, restraintsStr.Step13Desc, restraintsStr.Step13DescExtended)
        .AddStep(restraintsStr.Step14Title, restraintsStr.Step14Desc, string.Empty)
        .AddStep(restraintsStr.Step15Title, restraintsStr.Step15Desc, string.Empty)
        .AddStep(restraintsStr.Step16Title, restraintsStr.Step16Desc, string.Empty)
        .AddStep(restraintsStr.Step17Title, restraintsStr.Step17Desc, string.Empty)
        .AddStep(restraintsStr.Step18Title, restraintsStr.Step18Desc, string.Empty)
        .AddStep(restraintsStr.Step19Title, restraintsStr.Step19Desc, string.Empty)
        .AddStep(restraintsStr.Step20Title, restraintsStr.Step20Desc, string.Empty)
        .AddStep(restraintsStr.Step21Title, restraintsStr.Step21Desc, restraintsStr.Step22DescExtended)
        .AddStep(restraintsStr.Step22Title, restraintsStr.Step22Desc, string.Empty)
        .AddStep(restraintsStr.Step23Title, restraintsStr.Step23Desc, string.Empty)
        .AddStep(restraintsStr.Step24Title, restraintsStr.Step24Desc, string.Empty)
        .AddStep(restraintsStr.Step25Title, restraintsStr.Step25Desc, string.Empty)
        .AddStep(restraintsStr.Step26Title, restraintsStr.Step26Desc, string.Empty)
        .EnsureSize(_tutorialSizes[TutorialType.Restraints]);

        var cursedLootStr = GSLoc.Tutorials.HelpCursedLoot;
        _tutorials[TutorialType.CursedLoot] = new Tutorial(_uiShared)
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Cursed Loot Tutorial",
        }
        .AddStep(cursedLootStr.Step1Title, cursedLootStr.Step1Desc, string.Empty)
        .AddStep(cursedLootStr.Step2Title, cursedLootStr.Step2Desc, string.Empty)
        .AddStep(cursedLootStr.Step3Title, cursedLootStr.Step3Desc, cursedLootStr.Step3DescExtended)
        .AddStep(cursedLootStr.Step4Title, cursedLootStr.Step4Desc, cursedLootStr.Step4DescExtended)
        .AddStep(cursedLootStr.Step5Title, cursedLootStr.Step5Desc, string.Empty)
        .AddStep(cursedLootStr.Step6Title, cursedLootStr.Step6Desc, string.Empty)
        .AddStep(cursedLootStr.Step7Title, cursedLootStr.Step7Desc, string.Empty)
        .AddStep(cursedLootStr.Step8Title, cursedLootStr.Step8Desc, string.Empty)
        .AddStep(cursedLootStr.Step9Title, cursedLootStr.Step9Desc, cursedLootStr.Step9DescExtended)
        .AddStep(cursedLootStr.Step10Title, cursedLootStr.Step10Desc, cursedLootStr.Step10DescExtended)
        .AddStep(cursedLootStr.Step11Title, cursedLootStr.Step11Desc, string.Empty)
        .EnsureSize(_tutorialSizes[TutorialType.CursedLoot]);

        var toyboxStr = GSLoc.Tutorials.HelpToybox;
        _tutorials[TutorialType.Toybox] = new Tutorial(_uiShared)
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Toybox Tutorial",
        }
        .AddStep(toyboxStr.Step1Title, toyboxStr.Step1Desc, toyboxStr.Step1DescExtended)
        .AddStep(toyboxStr.Step2Title, toyboxStr.Step2Desc, string.Empty)
        .AddStep(toyboxStr.Step3Title, toyboxStr.Step3Desc, string.Empty)
        .AddStep(toyboxStr.Step4Title, toyboxStr.Step4Desc, string.Empty)
        .AddStep(toyboxStr.Step5Title, toyboxStr.Step5Desc, string.Empty)
        .AddStep(toyboxStr.Step6Title, toyboxStr.Step6Desc, toyboxStr.Step6DescExtended)
        .EnsureSize(_tutorialSizes[TutorialType.Toybox]);

        var patternsStr = GSLoc.Tutorials.HelpPatterns;
        _tutorials[TutorialType.Patterns] = new Tutorial(_uiShared)
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Patterns Tutorial",
        }
        .AddStep(patternsStr.Step1Title, patternsStr.Step1Desc, string.Empty)
        .AddStep(patternsStr.Step2Title, patternsStr.Step2Desc, string.Empty)
        .AddStep(patternsStr.Step3Title, patternsStr.Step3Desc, patternsStr.Step3DescExtended)
        .AddStep(patternsStr.Step4Title, patternsStr.Step4Desc, patternsStr.Step4DescExtended)
        .AddStep(patternsStr.Step5Title, patternsStr.Step5Desc, string.Empty)
        .AddStep(patternsStr.Step6Title, patternsStr.Step6Desc, string.Empty)
        .AddStep(patternsStr.Step7Title, patternsStr.Step7Desc, string.Empty)
        .AddStep(patternsStr.Step8Title, patternsStr.Step8Desc, string.Empty)
        .AddStep(patternsStr.Step9Title, patternsStr.Step9Desc, patternsStr.Step9DescExtended)
        .AddStep(patternsStr.Step10Title, patternsStr.Step10Desc, string.Empty)
        .AddStep(patternsStr.Step11Title, patternsStr.Step11Desc, string.Empty)
        .AddStep(patternsStr.Step12Title, patternsStr.Step12Desc, string.Empty)
        .AddStep(patternsStr.Step13Title, patternsStr.Step13Desc, string.Empty)
        .AddStep(patternsStr.Step14Title, patternsStr.Step14Desc, string.Empty)
        .AddStep(patternsStr.Step15Title, patternsStr.Step15Desc, string.Empty)
        .AddStep(patternsStr.Step16Title, patternsStr.Step16Desc, string.Empty)
        .AddStep(patternsStr.Step17Title, patternsStr.Step17Desc, string.Empty)
        .AddStep(patternsStr.Step18Title, patternsStr.Step18Desc, string.Empty)
        .AddStep(patternsStr.Step19Title, patternsStr.Step19Desc, string.Empty)
        .AddStep(patternsStr.Step20Title, patternsStr.Step20Desc, string.Empty)
        .AddStep(patternsStr.Step21Title, patternsStr.Step21Desc, string.Empty)
        .AddStep(patternsStr.Step22Title, patternsStr.Step22Desc, patternsStr.Step22DescExtended)
        .EnsureSize(_tutorialSizes[TutorialType.Patterns]);

        var triggersStr = GSLoc.Tutorials.HelpTriggers;
        _tutorials[TutorialType.Triggers] = new Tutorial(_uiShared)
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Triggers Tutorial",
        }
        .AddStep(triggersStr.Step1Title, triggersStr.Step1Desc, string.Empty)
        .AddStep(triggersStr.Step2Title, triggersStr.Step2Desc, string.Empty)
        .AddStep(triggersStr.Step3Title, triggersStr.Step3Desc, string.Empty)
        .AddStep(triggersStr.Step4Title, triggersStr.Step4Desc, string.Empty)
        .AddStep(triggersStr.Step5Title, triggersStr.Step5Desc, string.Empty)
        .AddStep(triggersStr.Step6Title, triggersStr.Step6Desc, string.Empty)
        .AddStep(triggersStr.Step7Title, triggersStr.Step7Desc, triggersStr.Step7DescExtended)
        .AddStep(triggersStr.Step8Title, triggersStr.Step8Desc, string.Empty)
        .AddStep(triggersStr.Step9Title, triggersStr.Step9Desc, string.Empty)
        .AddStep(triggersStr.Step10Title, triggersStr.Step10Desc, string.Empty)
        .AddStep(triggersStr.Step11Title, triggersStr.Step11Desc, triggersStr.Step11DescExtended)
        .AddStep(triggersStr.Step12Title, triggersStr.Step12Desc, string.Empty)
        .AddStep(triggersStr.Step13Title, triggersStr.Step13Desc, string.Empty)
        .AddStep(triggersStr.Step14Title, triggersStr.Step14Desc, string.Empty)
        .EnsureSize(_tutorialSizes[TutorialType.Triggers]);

        var alarmsStr = GSLoc.Tutorials.HelpAlarms;
        _tutorials[TutorialType.Alarms] = new Tutorial(_uiShared)
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Alarms Tutorial",
        }
        .AddStep(alarmsStr.Step1Title, alarmsStr.Step1Desc, string.Empty)
        .AddStep(alarmsStr.Step2Title, alarmsStr.Step2Desc, string.Empty)
        .AddStep(alarmsStr.Step3Title, alarmsStr.Step3Desc, string.Empty)
        .AddStep(alarmsStr.Step4Title, alarmsStr.Step4Desc, string.Empty)
        .AddStep(alarmsStr.Step5Title, alarmsStr.Step5Desc, string.Empty)
        .AddStep(alarmsStr.Step6Title, alarmsStr.Step6Desc, string.Empty)
        .AddStep(alarmsStr.Step7Title, alarmsStr.Step7Desc, string.Empty)
        .AddStep(alarmsStr.Step8Title, alarmsStr.Step8Desc, string.Empty)
        .AddStep(alarmsStr.Step9Title, alarmsStr.Step9Desc, string.Empty)
        .AddStep(alarmsStr.Step10Title, alarmsStr.Step10Desc, string.Empty)
        .AddStep(alarmsStr.Step11Title, alarmsStr.Step11Desc, string.Empty)
        .EnsureSize(_tutorialSizes[TutorialType.Alarms]);

        var achievementsStr = GSLoc.Tutorials.HelpAchievements;
        _tutorials[TutorialType.Achievements] = new Tutorial(_uiShared)
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
