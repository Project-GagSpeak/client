using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;
using Dalamud.Utility;

namespace GagSpeak.CkCommons.Gui.Components;

// Scoped, sealed class to draw the editor and display components of aliasItems.
public sealed class AliasItemDrawer
{
    private readonly ILogger<AliasItemDrawer> _logger;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly MoodleDrawer _moodleDrawer;

    private MoodleStatusCombo _statusCombo { get; init; }
    private MoodlePresetCombo _presetCombo { get; init; }
    public AliasItemDrawer(
        ILogger<AliasItemDrawer> logger,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        MoodleDrawer moodleDrawer)
    {
        _logger = logger;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _moodleDrawer = moodleDrawer;
    }

    public void DrawOutputTextAction(TextAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.Font);

        var txt = action.OutputCommand.IsNullOrEmpty() ? "<Undefined Output!>" : action.OutputCommand;
        CkGui.ColorText("/" + txt, ImGuiColors.TankBlue);
        CkGui.AttachToolTip("What command you execute when the above alias string is said." +
            "--SEP-- TIP: Do not include the '/' in your output.");
    }

    public void DrawGagAction(GagAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.Comment);
        CkGui.AttachToolTip("An applied Gag state change.");

        var isPadlockAct = action.NewState is NewState.Locked or NewState.Unlocked;
        CkGui.SameLineText($"{(action.LayerIdx is -1 ? "On any open layer," : "On layer ")} {action.LayerIdx}, a");
        CkGui.SameLineColorText(isPadlockAct ? action.GagType.GagName() : action.Padlock.ToName(), ImGuiColors.TankBlue);
        CkGui.SameLineText("will be");
        CkGui.SameLineColorText(action.NewState.ToString(), ImGuiColors.TankBlue);
    }

    public void DrawRestrictionAction(RestrictionAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.ToiletPortable);
        CkGui.AttachToolTip("An applied Restriction Item state change.");

        // the following gag
        var isPadlockAct = action.NewState is NewState.Locked or NewState.Unlocked;
        CkGui.SameLineText($"{(action.LayerIdx is -1 ? "On the 1st open layer," : "On layer ")} {action.LayerIdx}, a");

        var name = _restrictions.Storage.FirstOrDefault(r => r.Identifier == action.RestrictionId) is { } r ? r.Label : action.RestrictionId.ToString();
        if (isPadlockAct)
        {
            CkGui.SameLineColorText(action.Padlock.ToName(), ImGuiColors.TankBlue);
            CkGui.SameLineText("will get");
            CkGui.SameLineColorText(action.NewState is NewState.Locked ? "locked" : "unlocked", ImGuiColors.TankBlue);
            CkGui.SameLineText("on the restriction");
            CkGui.SameLineColorText(name, ImGuiColors.TankBlue);
        }
        else
        {
            CkGui.SameLineColorText(name, ImGuiColors.TankBlue);
            CkGui.SameLineText("will be");
            CkGui.SameLineColorText(action.NewState.ToString(), ImGuiColors.TankBlue);
        }
    }

    public void DrawRestraintAction(RestraintAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.ToiletPortable);
        CkGui.AttachToolTip("An applied Restriction Item state change.");

        var isPadlockAct = action.NewState is NewState.Locked or NewState.Unlocked;
        CkGui.SameLineText("A");

        var name = _restraints.Storage.FirstOrDefault(r => r.Identifier == action.RestrictionId) is { } r ? r.Label : action.RestrictionId.ToString();
        if (isPadlockAct)
        {
            CkGui.SameLineColorText(action.Padlock.ToName(), ImGuiColors.TankBlue);
            CkGui.SameLineText("will get");
            CkGui.SameLineColorText(action.NewState is NewState.Locked ? "locked" : "unlocked", ImGuiColors.TankBlue);
            CkGui.SameLineText("on the set");
            CkGui.SameLineColorText(name, ImGuiColors.TankBlue);
        }
        else
        {
            CkGui.SameLineColorText(name, ImGuiColors.TankBlue);
            CkGui.SameLineText("will be");
            CkGui.SameLineColorText(action.NewState.ToString(), ImGuiColors.TankBlue);
        }
    }

    public void DrawMoodleAction(MoodleAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.WandMagicSparkles);
        CkGui.AttachToolTip("Displays the Identity of the moodle status/preset applied.");

        if (action.MoodleItem is MoodlePresetApi presetApi)
        {
            CkGui.SameLineText("The Moodle preset ");
            CkGui.SameLineColorText(presetApi.Preset.Title, ImGuiColors.TankBlue);
            CkGui.SameLineText("applies the moodles");
            _moodleDrawer.DrawMoodleStatuses(presetApi.Statuses, MoodleDrawer.IconSizeFramed);
        }
        else if (action.MoodleItem is MoodleStatusApi statusApi)
        {
            CkGui.SameLineText("The Moodle status ");
            CkGui.SameLineColorText(statusApi.Status.Title, ImGuiColors.TankBlue);
            CkGui.SameLineText("applies the moodle");
            _moodleDrawer.DrawMoodleStatuses(new[] { statusApi.Status }, MoodleDrawer.IconSizeFramed);

        }
    }

    public void DrawShockAction(PiShockAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.Bolt);
        CkGui.AttachToolTip("The shock collar instruction executed when the input command is detected.");

        CkGui.SameLineText("Instructs:");
        CkGui.SameLineColorText(action.ShockInstruction.OpCode.ToString(), ImGuiColors.TankBlue);

        CkGui.SameLineText("for");
        CkGui.SameLineColorText(action.ShockInstruction.Duration.ToString(), ImGuiColors.TankBlue);
        CkGui.SameLineText("ms");

        // display extra information if a vibrator or shock.
        if (action.ShockInstruction.OpCode is not ShockMode.Beep)
        {
            CkGui.SameLineText("at");
            CkGui.SameLineColorText(action.ShockInstruction.Intensity.ToString(), ImGuiColors.TankBlue);
            CkGui.SameLineText("intensity");
        }
    }

    public void DrawSexToyAction(SexToyAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.WaveSquare);
        CkGui.AttachToolTip("The action to be executed on the listed toys.");

        // in theory this listing could get pretty expansive so for now just list a summary.
        CkGui.SameLineText("After");
        CkGui.SameLineColorText(action.StartAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);
        CkGui.SameLineText(", actives");
        CkGui.SameLineColorText(action.DeviceActions.Count.ToString(), ImGuiColors.TankBlue);
        CkGui.SameLineText("toys to perform vibrations or patterns for the next");
        CkGui.SameLineColorText(action.EndAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);
    }
}
