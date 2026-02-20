using Dalamud.Interface.Colors;
using GagSpeak.Localization;
using Dalamud.Bindings.ImGui;
using System.Runtime.CompilerServices;

// A Modified take on OtterGui.Widgets.Tutorial.
// This iteration removes redundant buttons, adds detailed text, and sections.
namespace GagSpeak.Services.Tutorial;

/// <summary>
///     Service for the in-game tutorial.
/// </summary>
public class TutorialService
{
    private readonly Dictionary<TutorialType, Tutorial> tutorials = new();

    public TutorialService() { }
    public bool IsTutorialActive(TutorialType type) => tutorials[type].CurrentStep is not -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartTutorial(TutorialType guide)
    {
        if (!tutorials.ContainsKey(guide))
            return;

        // set all other tutorials to -1, stopping them.
        foreach (var t in tutorials)
            t.Value.CurrentStep = (t.Key != guide) ?  -1 : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OpenTutorial<TEnum>(TutorialType guide, TEnum step, Vector2 pos, Vector2 size, Action? onNext = null) where TEnum : Enum
    {
        if (tutorials.TryGetValue(guide, out var tutorial))
            tutorial.Open(Convert.ToInt32(step), pos, size, onNext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SkipTutorial(TutorialType guide)
    {
        // reset the step to -1, stopping the tutorial.
        if (tutorials.TryGetValue(guide, out var tutorial))
            tutorial.CurrentStep = -1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void JumpToStep<TEnum>(TutorialType guide, TEnum step)
    {
        // reset the step to -1, stopping the tutorial.
        if (tutorials.TryGetValue(guide, out var tutorial))
            tutorial.CurrentStep = Convert.ToInt32(step);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CurrentStep(TutorialType guide)
    {
        if (tutorials.TryGetValue(guide, out var tutorial))
            return tutorial.CurrentStep;

        return -1;
    }

    // Create a mapping between the tutorialTypes and the associated enum size.
    private static readonly Dictionary<TutorialType, int> TutorialSizes = new()
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
        tutorials[TutorialType.MainUi] = new Tutorial()
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
        .EnsureSize(TutorialSizes[TutorialType.MainUi]);

        var remoteStr = GSLoc.Tutorials.Remote;
        tutorials[TutorialType.Remote] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Remote Tutorial",
        }
        .EnsureSize(0);

        var restraintsStr = GSLoc.Tutorials.Restraints;
        tutorials[TutorialType.Restraints] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Restraints Tutorial",
        }
        .AddStep(restraintsStr.Step1Title, restraintsStr.Step1Desc, restraintsStr.Step1DescExtended)
        .AddStep(restraintsStr.Step2Title, restraintsStr.Step2Desc, restraintsStr.Step2DescExtended)
        .AddStep(restraintsStr.Step3Title, restraintsStr.Step3Desc, restraintsStr.Step3DescExtended)
        .AddStep(restraintsStr.Step4Title, restraintsStr.Step4Desc, restraintsStr.Step4DescExtended)
        .AddStep(restraintsStr.Step5Title, restraintsStr.Step5Desc, restraintsStr.Step5DescExtended)
        .AddStep(restraintsStr.Step6Title, restraintsStr.Step6Desc, restraintsStr.Step6DescExtended)
        .AddStep(restraintsStr.Step7Title, restraintsStr.Step7Desc, restraintsStr.Step7DescExtended)
        .AddStep(restraintsStr.Step8Title, restraintsStr.Step8Desc, restraintsStr.Step8DescExtended)
        .AddStep(restraintsStr.Step9Title, restraintsStr.Step9Desc, restraintsStr.Step9DescExtended)
        .AddStep(restraintsStr.Step10Title, restraintsStr.Step10Desc, restraintsStr.Step10DescExtended)
        .AddStep(restraintsStr.Step11Title, restraintsStr.Step11Desc, restraintsStr.Step11DescExtended)
        .AddStep(restraintsStr.Step12Title, restraintsStr.Step12Desc, restraintsStr.Step12DescExtended)
        .AddStep(restraintsStr.Step13Title, restraintsStr.Step13Desc, restraintsStr.Step13DescExtended)
        .AddStep(restraintsStr.Step14Title, restraintsStr.Step14Desc, restraintsStr.Step14DescExtended)
        .AddStep(restraintsStr.Step14Title, restraintsStr.Step14Desc, restraintsStr.Step14DescExtended) // buffer step
        .AddStep(restraintsStr.Step15Title, restraintsStr.Step15Desc, restraintsStr.Step15DescExtended)
        .AddStep(restraintsStr.Step16Title, restraintsStr.Step16Desc, restraintsStr.Step16DescExtended)
        .AddStep(restraintsStr.Step17Title, restraintsStr.Step17Desc, restraintsStr.Step17DescExtended)
        .AddStep(restraintsStr.Step17Title, restraintsStr.Step17Desc, restraintsStr.Step17DescExtended) // buffer step, uses same text as previous
        .AddStep(restraintsStr.Step18Title, restraintsStr.Step18Desc, restraintsStr.Step18DescExtended) 
        .AddStep(restraintsStr.Step19Title, restraintsStr.Step19Desc, restraintsStr.Step19DescExtended)
        .AddStep(restraintsStr.Step20Title, restraintsStr.Step20Desc, restraintsStr.Step20DescExtended)
        .AddStep(restraintsStr.Step21Title, restraintsStr.Step21Desc, restraintsStr.Step21DescExtended)
        .AddStep(restraintsStr.Step22Title, restraintsStr.Step22Desc, restraintsStr.Step22DescExtended)
        .AddStep(restraintsStr.Step23Title, restraintsStr.Step23Desc, restraintsStr.Step23DescExtended)
        .AddStep(restraintsStr.Step24Title, restraintsStr.Step24Desc, restraintsStr.Step24DescExtended)
        .AddStep(restraintsStr.Step25Title, restraintsStr.Step25Desc, restraintsStr.Step25DescExtended)
        .AddStep(restraintsStr.Step26Title, restraintsStr.Step26Desc, restraintsStr.Step26DescExtended)
        .AddStep(restraintsStr.Step27Title, restraintsStr.Step27Desc, restraintsStr.Step27DescExtended)
        .AddStep(restraintsStr.Step28Title, restraintsStr.Step28Desc, restraintsStr.Step28DescExtended)
        .AddStep(restraintsStr.Step29Title, restraintsStr.Step29Desc, restraintsStr.Step29DescExtended)
        .AddStep(restraintsStr.Step30Title, restraintsStr.Step30Desc, restraintsStr.Step30DescExtended)
        .AddStep(restraintsStr.Step31Title, restraintsStr.Step31Desc, restraintsStr.Step31DescExtended)
        .AddStep(restraintsStr.Step32Title, restraintsStr.Step32Desc, restraintsStr.Step32DescExtended)
        .AddStep(restraintsStr.Step33Title, restraintsStr.Step33Desc, restraintsStr.Step33DescExtended)
        .AddStep(restraintsStr.Step34Title, restraintsStr.Step34Desc, restraintsStr.Step34DescExtended)
        .AddStep(restraintsStr.Step35Title, restraintsStr.Step35Desc, restraintsStr.Step35DescExtended)
        .EnsureSize(TutorialSizes[TutorialType.Restraints]);

        var restrictionsStr = GSLoc.Tutorials.Restrictions;
        tutorials[TutorialType.Restrictions] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Restrictions Tutorial",
        }
        .AddStep(restrictionsStr.Step1Title, restrictionsStr.Step1Desc, restrictionsStr.Step1DescExtended)
        .AddStep(restrictionsStr.Step2Title, restrictionsStr.Step2Desc, restrictionsStr.Step2DescExtended)
        .AddStep(restrictionsStr.Step3Title, restrictionsStr.Step3Desc, restrictionsStr.Step3DescExtended)
        .AddStep(restrictionsStr.Step4Title, restrictionsStr.Step4Desc, restrictionsStr.Step4DescExtended)
        .AddStep(restrictionsStr.Step5Title, restrictionsStr.Step5Desc, restrictionsStr.Step5DescExtended)
        .AddStep(restrictionsStr.Step6Title, restrictionsStr.Step6Desc, restrictionsStr.Step6DescExtended)
        .AddStep(restrictionsStr.Step7Title, restrictionsStr.Step7Desc, restrictionsStr.Step7DescExtended)
        .AddStep(restrictionsStr.Step8Title, restrictionsStr.Step8Desc, restrictionsStr.Step8DescExtended)
        .AddStep(restrictionsStr.Step9Title, restrictionsStr.Step9Desc, restrictionsStr.Step9DescExtended)
        .AddStep(restrictionsStr.Step10Title, restrictionsStr.Step10Desc, restrictionsStr.Step10DescExtended)
        .AddStep(restrictionsStr.Step11Title, restrictionsStr.Step11Desc, restrictionsStr.Step11DescExtended)
        .AddStep(restrictionsStr.Step12Title, restrictionsStr.Step12Desc, restrictionsStr.Step12DescExtended)
        .AddStep(restrictionsStr.Step13Title, restrictionsStr.Step13Desc, restrictionsStr.Step13DescExtended)
        .AddStep(restrictionsStr.Step14Title, restrictionsStr.Step14Desc, restrictionsStr.Step14DescExtended)
        .AddStep(restrictionsStr.Step15Title, restrictionsStr.Step15Desc, restrictionsStr.Step15DescExtended)
        .AddStep(restrictionsStr.Step16Title, restrictionsStr.Step16Desc, restrictionsStr.Step16DescExtended)
        .AddStep(restrictionsStr.Step17Title, restrictionsStr.Step17Desc, restrictionsStr.Step17DescExtended)
        .AddStep(restrictionsStr.Step18Title, restrictionsStr.Step18Desc, restrictionsStr.Step18DescExtended)
        .AddStep(restrictionsStr.Step19Title, restrictionsStr.Step19Desc, restrictionsStr.Step19DescExtended)
        .AddStep(restrictionsStr.Step20Title, restrictionsStr.Step20Desc, restrictionsStr.Step20DescExtended)
        .AddStep(restrictionsStr.Step21Title, restrictionsStr.Step21Desc, restrictionsStr.Step21DescExtended)
        .AddStep(restrictionsStr.Step22Title, restrictionsStr.Step22Desc, restrictionsStr.Step22DescExtended)
        .AddStep(restrictionsStr.Step23Title, restrictionsStr.Step23Desc, restrictionsStr.Step23DescExtended)
        .AddStep(restrictionsStr.Step24Title, restrictionsStr.Step24Desc, restrictionsStr.Step24DescExtended)
        .AddStep(restrictionsStr.Step25Title, restrictionsStr.Step25Desc, restrictionsStr.Step25DescExtended)
        .AddStep(restrictionsStr.Step26Title, restrictionsStr.Step26Desc, restrictionsStr.Step26DescExtended)
        .AddStep(restrictionsStr.Step27Title, restrictionsStr.Step27Desc, restrictionsStr.Step27DescExtended)
        .AddStep(restrictionsStr.Step28Title, restrictionsStr.Step28Desc, restrictionsStr.Step28DescExtended)
        .AddStep(restrictionsStr.Step29Title, restrictionsStr.Step29Desc, restrictionsStr.Step29DescExtended)
        .AddStep(restrictionsStr.Step30Title, restrictionsStr.Step30Desc, restrictionsStr.Step30DescExtended)
        .AddStep(restrictionsStr.Step31Title, restrictionsStr.Step31Desc, restrictionsStr.Step31DescExtended)
        .AddStep(restrictionsStr.Step32Title, restrictionsStr.Step32Desc, restrictionsStr.Step32DescExtended)
        .AddStep(restrictionsStr.Step33Title, restrictionsStr.Step33Desc, restrictionsStr.Step33DescExtended)
        .AddStep(restrictionsStr.Step34Title, restrictionsStr.Step34Desc, restrictionsStr.Step34DescExtended)
        .AddStep(restrictionsStr.Step35Title, restrictionsStr.Step35Desc, restrictionsStr.Step35DescExtended)
        .AddStep(restrictionsStr.Step36Title, restrictionsStr.Step36Desc, restrictionsStr.Step36DescExtended)
        .AddStep(restrictionsStr.Step37Title, restrictionsStr.Step37Desc, restrictionsStr.Step37DescExtended)
        .AddStep(restrictionsStr.Step38Title, restrictionsStr.Step38Desc, restrictionsStr.Step38DescExtended)
        .AddStep(restrictionsStr.Step39Title, restrictionsStr.Step39Desc, restrictionsStr.Step39DescExtended)
        .EnsureSize(TutorialSizes[TutorialType.Restrictions]);

        var gagsStr = GSLoc.Tutorials.Gags;
        tutorials[TutorialType.Gags] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Gags Tutorial",
        }
        .AddStep(gagsStr.Step1Title, gagsStr.Step1Desc, gagsStr.Step1DescExtended)
        .AddStep(gagsStr.Step2Title, gagsStr.Step2Desc, gagsStr.Step2DescExtended)
        .AddStep(gagsStr.Step3Title, gagsStr.Step3Desc, gagsStr.Step3DescExtended)
        .AddStep(gagsStr.Step4Title, gagsStr.Step4Desc, gagsStr.Step4DescExtended)
        .AddStep(gagsStr.Step5Title, gagsStr.Step5Desc, gagsStr.Step5DescExtended)
        .AddStep(gagsStr.Step6Title, gagsStr.Step6Desc, gagsStr.Step6DescExtended)
        .AddStep(gagsStr.Step7Title, gagsStr.Step7Desc, gagsStr.Step7DescExtended)
        .AddStep(gagsStr.Step8Title, gagsStr.Step8Desc, gagsStr.Step8DescExtended)
        .AddStep(gagsStr.Step9Title, gagsStr.Step9Desc, gagsStr.Step9DescExtended)
        .AddStep(gagsStr.Step10Title, gagsStr.Step10Desc, gagsStr.Step10DescExtended)
        .AddStep(gagsStr.Step11Title, gagsStr.Step11Desc, gagsStr.Step11DescExtended)
        .AddStep(gagsStr.Step12Title, gagsStr.Step12Desc, gagsStr.Step12DescExtended)
        .AddStep(gagsStr.Step13Title, gagsStr.Step13Desc, gagsStr.Step13DescExtended)
        .AddStep(gagsStr.Step14Title, gagsStr.Step14Desc, gagsStr.Step14DescExtended)
        .AddStep(gagsStr.Step15Title, gagsStr.Step15Desc, gagsStr.Step15DescExtended)
        .AddStep(gagsStr.Step16Title, gagsStr.Step16Desc, gagsStr.Step16DescExtended)
        .AddStep(gagsStr.Step17Title, gagsStr.Step17Desc, gagsStr.Step17DescExtended)
        .AddStep(gagsStr.Step18Title, gagsStr.Step18Desc, gagsStr.Step18DescExtended)
        .AddStep(gagsStr.Step19Title, gagsStr.Step19Desc, gagsStr.Step19DescExtended)
        .AddStep(gagsStr.Step20Title, gagsStr.Step20Desc, gagsStr.Step20DescExtended)
        .AddStep(gagsStr.Step21Title, gagsStr.Step21Desc, gagsStr.Step21DescExtended)
        .AddStep(gagsStr.Step22Title, gagsStr.Step22Desc, gagsStr.Step22DescExtended)
        .AddStep(gagsStr.Step23Title, gagsStr.Step23Desc, gagsStr.Step23DescExtended)
        .AddStep(gagsStr.Step24Title, gagsStr.Step24Desc, gagsStr.Step24DescExtended)
        .AddStep(gagsStr.Step25Title, gagsStr.Step25Desc, gagsStr.Step25DescExtended)
        .EnsureSize(TutorialSizes[TutorialType.Gags]);

        var cursedLootStr = GSLoc.Tutorials.CursedLoot;
        tutorials[TutorialType.CursedLoot] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Cursed Loot Tutorial",
        }
        .EnsureSize(0);

        var puppetStr = GSLoc.Tutorials.Puppeteer;
        tutorials[TutorialType.Puppeteer] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Puppeteer Tutorial",
        }
        .AddStep(puppetStr.Step1Title, puppetStr.Step1Desc, puppetStr.Step1DescExtended)
        .AddStep(puppetStr.Step2Title, puppetStr.Step2Desc, puppetStr.Step2DescExtended)
        .AddStep(puppetStr.Step3Title, puppetStr.Step3Desc, puppetStr.Step3DescExtended)
        .AddStep(puppetStr.Step4Title, puppetStr.Step4Desc, puppetStr.Step4DescExtended)
        .AddStep(puppetStr.Step5Title, puppetStr.Step5Desc, puppetStr.Step5DescExtended)
        .AddStep(puppetStr.Step6Title, puppetStr.Step6Desc, puppetStr.Step6DescExtended)
        .AddStep(puppetStr.Step7Title, puppetStr.Step7Desc, puppetStr.Step7DescExtended)
        .AddStep(puppetStr.Step8Title, puppetStr.Step8Desc, puppetStr.Step8DescExtended)
        .AddStep(puppetStr.Step9Title, puppetStr.Step9Desc, puppetStr.Step9DescExtended)
        .AddStep(puppetStr.Step10Title, puppetStr.Step10Desc, puppetStr.Step10DescExtended)
        .AddStep(puppetStr.Step11Title, puppetStr.Step11Desc, puppetStr.Step11DescExtended)
        .AddStep(puppetStr.Step12Title, puppetStr.Step12Desc, puppetStr.Step12DescExtended)
        .AddStep(puppetStr.Step13Title, puppetStr.Step13Desc, puppetStr.Step13DescExtended)
        .AddStep(puppetStr.Step14Title, puppetStr.Step14Desc, puppetStr.Step14DescExtended)
        .AddStep(puppetStr.Step15Title, puppetStr.Step15Desc, puppetStr.Step15DescExtended)
        .AddStep(puppetStr.Step16Title, puppetStr.Step16Desc, puppetStr.Step16DescExtended)
        .AddStep(puppetStr.Step17Title, puppetStr.Step17Desc, puppetStr.Step17DescExtended)
        .AddStep(puppetStr.Step18Title, puppetStr.Step18Desc, puppetStr.Step18DescExtended)
        .AddStep(puppetStr.Step19Title, puppetStr.Step19Desc, puppetStr.Step19DescExtended)
        .AddStep(puppetStr.Step20Title, puppetStr.Step20Desc, puppetStr.Step20DescExtended)
        .AddStep(puppetStr.Step21Title, puppetStr.Step21Desc, puppetStr.Step21DescExtended)
        .AddStep(puppetStr.Step22Title, puppetStr.Step22Desc, puppetStr.Step22DescExtended)
        .AddStep(puppetStr.Step23Title, puppetStr.Step23Desc, puppetStr.Step23DescExtended)
        .AddStep(puppetStr.Step24Title, puppetStr.Step24Desc, puppetStr.Step24DescExtended)
        .EnsureSize(TutorialSizes[TutorialType.Puppeteer]);

        var toyboxStr = GSLoc.Tutorials.Toys;
        tutorials[TutorialType.Toys] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Toybox Tutorial",
        }
        .EnsureSize(0);

        var patternsStr = GSLoc.Tutorials.Patterns;
        tutorials[TutorialType.Patterns] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Patterns Tutorial",
        }
        .EnsureSize(0);

        var alarmsStr = GSLoc.Tutorials.Alarms;
        tutorials[TutorialType.Alarms] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Alarms Tutorial",
        }
        .EnsureSize(0);

        var triggersStr = GSLoc.Tutorials.Triggers;
        tutorials[TutorialType.Triggers] = new Tutorial()
        {
            BorderColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            HighlightColor = ImGui.GetColorU32(ImGuiColors.TankBlue),
            PopupLabel = "Triggers Tutorial",
        }
        .EnsureSize(0);

        var achievementsStr = GSLoc.Tutorials.Achievements;
        tutorials[TutorialType.Achievements] = new Tutorial()
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
        .EnsureSize(TutorialSizes[TutorialType.Achievements]);
    }
}
