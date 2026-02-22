using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.Kinksters;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using NAudio.CoreAudioApi;
using OtterGui;
using OtterGui.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace GagSpeak.Gui.Components;

// Common shared draw logic for GsInvokableActions, used by Puppeteer and Triggers
public sealed class ReactionsDrawer
{
    private readonly ILogger<ReactionsDrawer> _logger;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly PatternManager _patterns;
    private readonly MoodleDrawer _moodles;

    // Related combos
    private RestrictionCombo _restrictionCombo;
    private RestraintCombo _restraintCombo;
    private PatternCombo _patternCombo;
    private MoodleStatusCombo _statusCombo;
    private MoodlePresetCombo _presetCombo;
    // toy combo ext.

    private IEnumerable<NewState> _statesNoUnlock => [NewState.Enabled, NewState.Locked, NewState.Disabled];
    private string? _lowerTime = null;
    private string? _upperTime = null;

    public ReactionsDrawer(
        ILogger<ReactionsDrawer> logger,
        GagspeakMediator mediator,
        MoodleDrawer moodleDrawer,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        PatternManager patterns,
        FavoritesConfig favorites)
    {
        _logger = logger;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _patterns = patterns;
        _moodles = moodleDrawer;

        _restrictionCombo = new RestrictionCombo(logger, mediator, favorites, () => [
            ..restrictions.Storage.OrderByDescending(p => FavoritesConfig.Restrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        _restraintCombo = new RestraintCombo(logger, mediator, favorites, () => [
            ..restraints.Storage.OrderByDescending(p => FavoritesConfig.Restraints.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        _patternCombo = new PatternCombo(logger, mediator, favorites, () => [
            ..patterns.Storage.OrderByDescending(p => FavoritesConfig.Patterns.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        _statusCombo = new MoodleStatusCombo(logger, 1.15f);
        _presetCombo = new MoodlePresetCombo(logger, 1.15f);
    }

    private string GetStateName(NewState state) => state switch
    {
        NewState.Enabled => "Apply",
        NewState.Locked => "Lock",
        NewState.Disabled => "Remove",
        _ => string.Empty
    };

    public void DrawText(TextAction act)
    {
        CkGui.FramedIconText(FAI.Comment);
        CkGui.AttachToolTip("The Classic PuppetMaster reaction of sending a message");
        var isValid = act.IsValid();
        ImUtf8.SameLineInner();
        CkGui.ColorTextWrapped($"/{act.OutputCommand}", isValid ? ImGuiColors.TankBlue : CkCol.TriStateCross.Vec4Ref());
        CkGui.AttachToolTip("What you send in chat.--SEP--The --COL--'/'--COL--, it is added for you.", GsCol.VibrantPink.Vec4());
    }

    public void DrawTextRow(TextAction act)
    {
        CkGui.FramedIconText(FAI.Comment);
        CkGui.AttachToolTip("The Classic PuppetMaster reaction of sending a message");

        var isValid = !string.IsNullOrEmpty(act.OutputCommand) && !act.OutputCommand.StartsWith('/');
        CkGui.ColorTextFrameAlignedInline(isValid ? $"/{act.OutputCommand}" : "<Invalid>", ImGuiColors.TankBlue);
        CkGui.AttachToolTip("What you send in chat." +
            "--SEP----COL--TIP:--COL-- Don't include --COL--'/'--COL--, it is added for you.", GsCol.VibrantPink.Vec4());
    }

    public void DrawTextEditor(TextAction act, float txtHeight)
    {
        using (ImRaii.Group())
        {
            CkGui.FramedIconText(FAI.Comment);
            CkGui.TextFrameAlignedInline("/");
        }
        CkGui.AttachToolTip("The Classic PuppetMaster reaction of sending a message");

        ImUtf8.SameLineInner();
        var output = act.OutputCommand;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, txtHeight);
        if (ImGui.InputTextMultiline("##TextAction", ref output, 480, size))
            act.OutputCommand = output;
        // Draw a hint if no text is present.
        if (string.IsNullOrWhiteSpace(act.OutputCommand))
            ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, 0xFFBBBBBB, "party Hello World!");
        CkGui.AttachToolTip("What you send in chat." +
            "--SEP----COL--TIP:--COL-- Don't include --COL--'/'--COL--, it is added for you.", GsCol.VibrantPink.Vec4());
    }

    public void DrawTextRowEditor(TextAction act)
    {
        CkGui.FramedIconText(FAI.Comment);
        CkGui.AttachToolTip("The Classic PuppetMaster reaction of sending a message");

        ImUtf8.SameLineInner();
        var outputText = act.OutputCommand;
        using (ImRaii.PushFont(UiBuilder.MonoFont))
            if (ImGui.InputTextWithHint("##TextOutputEdit", "output text response", ref outputText, 256))
                act.OutputCommand = outputText;
    }

    public void DrawGag(GagAction act)
    {
        // State and layer row
        CkGui.FramedIconText(FAI.ListOl);
        CkGui.AttachToolTip("Invokes an interaction with the Gags module");

        CkGui.ColorTextFrameAlignedInline(GetStateName(act.NewState), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline(act.NewState is NewState.Locked ? "the gag on" : "a gag to");
        CkGui.ColorTextFrameAlignedInline(CkGuiUtils.LayerIdxName(act.LayerIdx), ImGuiColors.TankBlue);
        if (act.NewState is NewState.Disabled)
            return;

        // For Apply, do the gag selection, otherwise do the lock selection.
        if (act.NewState is NewState.Enabled)
        {
            ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, new(ImUtf8.FrameHeight));
            CkGui.AttachToolTip("Indicates what gag will be applied.");
            CkGui.TextFrameAlignedInline("Using a");
            CkGui.ColorTextFrameAlignedInline(act.GagType.GagName(), ImGuiColors.TankBlue);
            return;
        }

        // Lock state
        CkGui.FramedIconText(FAI.Lock);
        CkGui.AttachToolTip("Indicates what lock will be applied to the gag.");
        CkGui.TextFrameAlignedInline("Using a");
        CkGui.ColorTextFrameAlignedInline(act.Padlock.ToName(), ImGuiColors.TankBlue);
        if (act.Padlock.IsTimerLock() && act.Padlock is not Padlocks.FiveMinutes)
        {
            CkGui.FramedIconText(FAI.HourglassHalf);
            CkGui.TextFrameAlignedInline("Between");
            CkGui.ColorTextFrameAlignedInline(act.LowerBound.ToGsRemainingTime(), ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("and");
            CkGui.ColorTextFrameAlignedInline(act.UpperBound.ToGsRemainingTime(), ImGuiColors.TankBlue);
        }
    }

    public void DrawGagRow(GagAction act)
    {
        var isLockAndKey = act.NewState is NewState.Locked or NewState.Unlocked;

        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, new(ImUtf8.FrameHeight));
        CkGui.AttachToolTip("Invokes an interaction with the Gags module");

        CkGui.TextFrameAlignedInline($"{(act.LayerIdx is -1 ? "On any open layer" : $"On layer {act.LayerIdx}")}, a");
        CkGui.ColorTextFrameAlignedInline(isLockAndKey ? act.Padlock.ToName() : act.GagType.GagName(), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("will be");
        CkGui.ColorTextFrameAlignedInline(act.NewState.ToName(), ImGuiColors.TankBlue);
        if (!act.IsValid())
        {
            ImGui.SameLine();
            CkGui.FramedIconText(FAI.ExclamationTriangle, ImGuiColors.DalamudRed);
            CkGui.AttachToolTip("An Invalid Gag/Lock/Layer is selected!");
        }
    }

    public void DrawGagEditor(GagAction act)
    {
        // State and layer row
        CkGui.FramedIconText(FAI.ListOl);
        CkGui.AttachToolTip("Invokes an interaction with the Gags module");

        var stateW = ImGui.CalcTextSize("removem").X;
        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##edit-state", stateW, act.NewState, out var newVal, _statesNoUnlock, GetStateName, flags: CFlags.NoArrowButton))
            act.NewState = newVal;

        CkGui.TextFrameAlignedInline(act.NewState is NewState.Locked ? "the gag on" : "a gag to");
        
        var width = ImGui.CalcTextSize("Any Layerm").X;
        ImUtf8.SameLineInner();
        if (CkGuiUtils.LayerIdxCombo("##edit-layer", width, act.LayerIdx, out int newIdx, 3, true, CFlags.NoArrowButton))
        {
            act.LayerIdx = (newIdx == 3) ? -1 : newIdx;
            Svc.Logger.Information($"Updating to IDX: {act.LayerIdx}");
        }

        if (act.NewState is NewState.Disabled)
            return;

        // For Apply, do the gag selection, otherwise do the lock selection.
        if (act.NewState is NewState.Enabled)
        {
            ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, new(ImUtf8.FrameHeight));
            CkGui.AttachToolTip("Indicates what gag will be applied.");
            CkGui.TextFrameAlignedInline("Using a");

            ImUtf8.SameLineInner();
            if (CkGuiUtils.EnumCombo("##edit-gag", width, act.GagType, out var newGag, i => i.GagName(), "Randomly chosen Gag", skip: 1))
                act.GagType = newGag;
            return;
        }

        // Lock state
        CkGui.FramedIconText(FAI.Lock);
        CkGui.AttachToolTip("Indicates what lock will be applied to the gag.");
        CkGui.TextFrameAlignedInline("Using a");

        ImUtf8.SameLineInner();
        var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
        if (CkGuiUtils.EnumCombo("##edit-lock", ImGui.GetContentRegionAvail().X, act.Padlock, out var newLock, options, i => i.ToName(), flags: CFlags.NoArrowButton))
            act.Padlock = newLock;
        CkGui.AttachToolTip("The padlock that the trigger attempts to apply");

        // For timer locks we should ask for timer input
        if (act.Padlock.IsTimerLock() && act.Padlock is not Padlocks.FiveMinutes)
        {
            CkGui.FramedIconText(FAI.HourglassStart);
            CkGui.TextFrameAlignedInline("Minimum Lock Time");
            var minTime = _lowerTime ?? act.LowerBound.ToGsRemainingTime();
            var maxTime = _upperTime ?? act.UpperBound.ToGsRemainingTime();
            var length = ImGui.GetContentRegionAvail().X;
            if (CkGui.IconInputText(FAI.Clock, "minTime", "0h0m0s..", ref minTime, 32, length, true))
            {
                if (minTime != act.LowerBound.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(minTime, out var newTime))
                    act.LowerBound = newTime;
                // Clear it
                _lowerTime = null;
            }
            CkGui.AttachToolTip("The lower range when randomly picking a lock time.");

            CkGui.FramedIconText(FAI.HourglassStart);
            CkGui.TextFrameAlignedInline("Maximum Lock Time");
            if (CkGui.IconInputText(FAI.Clock, "minTime", "0h0m0s..", ref minTime, 32, length, true))
            {
                if (minTime != act.UpperBound.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(minTime, out var newTime))
                    act.UpperBound = newTime;
                // Clear it
                _upperTime = null;
            }
            CkGui.AttachToolTip("The upper range when randomly picking a lock time.");
        }
    }

    public void DrawGagRowEditor(GagAction act)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, new(ImUtf8.FrameHeight));
        CkGui.AttachToolTip("Invokes an interaction with the Gags module");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##edit-state", 60f, act.NewState, out var newState, [NewState.Enabled, NewState.Locked, NewState.Disabled],
            i => i switch { NewState.Enabled => "Apply", NewState.Locked => "Lock", _ => "Remove" }, flags: CFlags.NoArrowButton))
            act.NewState = newState;
        CkGui.AttachToolTip("The new state set on the targeted gag.");

        if (newState is NewState.Locked)
        {
            ImUtf8.SameLineInner();
            var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
            if (CkGuiUtils.EnumCombo("##PadlockType", 100f, act.Padlock, out var newVal, options, i => i.ToName(), flags: CFlags.NoArrowButton))
                act.Padlock = newVal;

            if (act.Padlock.IsTimerLock())
            {
                CkGui.TextFrameAlignedInline("[TBD]");
            }
        }
        else
        {
            ImUtf8.SameLineInner();
            if (CkGuiUtils.EnumCombo("##GagType", 100f, act.GagType, out var newVal, i => i switch { GagType.None => "Any Gag", _ => i.GagName() }, flags: CFlags.NoArrowButton))
                act.GagType = newVal;
        }

        ImUtf8.SameLineInner();
        var width = ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.Minus).X;
        if (CkGuiUtils.LayerIdxCombo("##edit-layer", width, act.LayerIdx, out int newIdx, 3, true, CFlags.NoArrowButton))
            act.LayerIdx = (newIdx == 3) ? -1 : newIdx;
    }

    public void DrawRestriction(RestrictionAction act)
    {
        // State and layer row
        CkGui.FramedIconText(FAI.ListOl);
        CkGui.AttachToolTip("Invokes an interaction with the Restriction module");

        CkGui.ColorTextFrameAlignedInline(GetStateName(act.NewState), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline(act.NewState is NewState.Locked ? "the binding on" : "a binding to");
        CkGui.ColorTextFrameAlignedInline(CkGuiUtils.LayerIdxName(act.LayerIdx), ImGuiColors.TankBlue);
        if (act.NewState is NewState.Disabled)
            return;

        // For Apply, do the gag selection, otherwise do the lock selection.
        if (act.NewState is NewState.Enabled)
        {
            CkGui.FramedIconText(FAI.Handcuffs);
            CkGui.AttachToolTip("Indicates what restriction is applied.");
            CkGui.TextFrameAlignedInline("Using");
            var item = _restrictions.Storage.FirstOrDefault(r => r.Identifier == act.RestrictionId);
            CkGui.ColorTextFrameAlignedInline(item is { } re ? $"{re.Label.TrimText(50)}" : "<UNK>", ImGuiColors.TankBlue);
            return;
        }

        // Lock state
        CkGui.FramedIconText(FAI.Lock);
        CkGui.AttachToolTip("Indicates what lock will be applied to the restriction.");
        CkGui.TextFrameAlignedInline("Uses a");
        CkGui.ColorTextFrameAlignedInline(act.Padlock.ToName(), ImGuiColors.TankBlue);
        if (act.Padlock.IsTimerLock() && act.Padlock is not Padlocks.FiveMinutes)
        {
            CkGui.FramedIconText(FAI.HourglassHalf);
            CkGui.TextFrameAlignedInline("Between");
            CkGui.ColorTextFrameAlignedInline(act.LowerBound.ToGsRemainingTime(), ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("and");
            CkGui.ColorTextFrameAlignedInline(act.UpperBound.ToGsRemainingTime(), ImGuiColors.TankBlue);
        }
    }

    public void DrawRestrictionRow(RestrictionAction act)
    {
        CkGui.FramedIconText(FAI.Handcuffs);
        CkGui.AttachToolTip("Invokes an interaction with the Restrictions module");
        switch (act.NewState)
        {
            case NewState.Enabled:
                var item = _restrictions.Storage.FirstOrDefault(r => r.Identifier == act.RestrictionId);
                CkGui.ColorTextFrameAlignedInline("Applies", ImGuiColors.TankBlue);
                CkGui.TextFrameAlignedInline("a");
                CkGui.ColorTextFrameAlignedInline(item is { } re ? $"{re.Label.TrimText(20)}" : "<UNK>", ImGuiColors.TankBlue);
                CkGui.AttachToolTip(item?.Label, item is null);
                CkGui.TextFrameAlignedInline("on");
                CkGui.ColorTextFrameAlignedInline($"{(act.LayerIdx is -1 ? "any layer" : $"layer {act.LayerIdx}")}", ImGuiColors.TankBlue);
                break;
            case NewState.Locked:
                CkGui.TextFrameAligned("Use a");
                CkGui.ColorTextFrameAlignedInline(act.Padlock.ToName(), ImGuiColors.TankBlue);
                CkGui.TextFrameAlignedInline("on");
                if (act.LayerIdx is -1)
                    CkGui.ColorTextFrameAlignedInline("any unlocked restriction", ImGuiColors.TankBlue);
                else
                {
                    CkGui.TextFrameAlignedInline("on");
                    CkGui.ColorTextFrameAlignedInline($"layer {act.LayerIdx}'s restriction", ImGuiColors.TankBlue);
                    CkGui.TextFrameAlignedInline("if unlocked");
                }
                break;
            case NewState.Disabled:
                CkGui.ColorTextFrameAligned("Remove", ImGuiColors.TankBlue);
                CkGui.TextFrameAlignedInline("the restriction on");
                CkGui.ColorTextFrameAligned($"layer {act.LayerIdx}", ImGuiColors.TankBlue);
                CkGui.TextFrameAlignedInline("if unlocked");
                break;
        };


        if (!act.IsValid())
        {
            ImGui.SameLine();
            CkGui.FramedIconText(FAI.ExclamationTriangle, ImGuiColors.DalamudRed);
            CkGui.AttachToolTip("An Invalid Item/Lock/Layer is selected!");
        }
    }

    public void DrawRestrictionEditor(RestrictionAction act)
    {
        // State and layer row
        CkGui.FramedIconText(FAI.ListOl);
        CkGui.AttachToolTip("Invokes an interaction with the Restrictions module");

        var stateW = ImGui.CalcTextSize("removem").X;
        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##edit-state", stateW, act.NewState, out var newVal, _statesNoUnlock, GetStateName, flags: CFlags.NoArrowButton))
            act.NewState = newVal;

        CkGui.TextFrameAlignedInline(act.NewState is NewState.Locked ? "the binding on" : "a binding to");

        var width = ImGui.CalcTextSize("Any Layerm").X;
        ImUtf8.SameLineInner();
        if (CkGuiUtils.LayerIdxCombo("##edit-layer", width, act.LayerIdx, out int newIdx, 5, true, CFlags.NoArrowButton))
            act.LayerIdx = (newIdx == 5) ? -1 : newIdx;

        if (act.NewState is NewState.Disabled)
            return;

        // For Apply, do the gag selection, otherwise do the lock selection.
        if (act.NewState is NewState.Enabled)
        {
            CkGui.FramedIconText(FAI.Handcuffs);
            CkGui.AttachToolTip("Indicates what restriction will be applied.");
            CkGui.TextFrameAlignedInline("Using");

            ImUtf8.SameLineInner();
            if (_restrictionCombo.Draw("##restrictions", act.RestrictionId, ImGui.GetContentRegionAvail().X))
            {
                _logger.LogInformation($"Selected Restriction: {_restrictionCombo.Current?.Label} ({_restrictionCombo.Current?.Identifier})");
                act.RestrictionId = _restrictionCombo.Current?.Identifier ?? Guid.Empty;
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                act.RestrictionId = Guid.Empty;
            return;
        }

        // Lock state
        CkGui.FramedIconText(FAI.Lock);
        CkGui.AttachToolTip("Indicates what lock will be applied to the restriction.");
        CkGui.TextFrameAlignedInline("Using a");

        ImUtf8.SameLineInner();
        var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
        if (CkGuiUtils.EnumCombo("##Padlock", ImGui.GetContentRegionAvail().X, act.Padlock, out var newLock, options, i => i.ToName(), flags: CFlags.NoArrowButton))
            act.Padlock = newLock;
        CkGui.AttachToolTip("The padlock that the trigger attempts to apply");

        // For timer locks we should ask for timer input
        if (act.Padlock.IsTimerLock() && act.Padlock is not Padlocks.FiveMinutes)
        {
            CkGui.FramedIconText(FAI.HourglassStart);
            CkGui.TextFrameAlignedInline("Minimum Lock Time");
            var minTime = _lowerTime ?? act.LowerBound.ToGsRemainingTime();
            var maxTime = _upperTime ?? act.UpperBound.ToGsRemainingTime();
            var length = ImGui.GetContentRegionAvail().X;
            if (CkGui.IconInputText(FAI.Clock, "minTime", "0h0m0s..", ref minTime, 32, length, true))
            {
                if (minTime != act.LowerBound.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(minTime, out var newTime))
                    act.LowerBound = newTime;
                // Clear it
                _lowerTime = null;
            }
            CkGui.AttachToolTip("The lower range when randomly picking a lock time.");

            CkGui.FramedIconText(FAI.HourglassStart);
            CkGui.TextFrameAlignedInline("Maximum Lock Time");
            if (CkGui.IconInputText(FAI.Clock, "minTime", "0h0m0s..", ref minTime, 32, length, true))
            {
                if (minTime != act.UpperBound.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(minTime, out var newTime))
                    act.UpperBound = newTime;
                // Clear it
                _upperTime = null;
            }
            CkGui.AttachToolTip("The upper range when randomly picking a lock time.");
        }
    }

    public void DrawRestrictionRowEditor(RestrictionAction act)
    {
        CkGui.FramedIconText(FAI.Handcuffs);
        CkGui.AttachToolTip("The Restriction Action performed to the Kinkster.");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##RestrictionState", 60f, act.NewState, out var newState, [NewState.Enabled, NewState.Locked, NewState.Disabled],
            i => i switch { NewState.Enabled => "Apply", NewState.Locked => "Lock", _ => "Remove" }, flags: CFlags.NoArrowButton))
            act.NewState = newState;
        CkGui.AttachToolTip("The new state set on the targeted restriction item.");

        if (newState is NewState.Locked)
        {
            ImUtf8.SameLineInner();
            var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
            if (CkGuiUtils.EnumCombo("##PadlockType", 100f, act.Padlock, out var newVal, options, i => i.ToName(), flags: CFlags.NoArrowButton))
                act.Padlock = newVal;

            if (act.Padlock.IsTimerLock())
            {
                CkGui.TextFrameAlignedInline("[TBD]");
            }
        }
        else
        {
            ImUtf8.SameLineInner();
            if (_restrictionCombo.Draw("##RestrictSel", act.RestrictionId, 120f, CFlags.NoArrowButton))
            {
                if (!act.RestrictionId.Equals(_restrictionCombo.Current?.Identifier))
                    act.RestrictionId = _restrictionCombo.Current?.Identifier ?? Guid.Empty;
            }
        }

        ImUtf8.SameLineInner();
        var width = ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.Minus).X;
        if (CkGuiUtils.LayerIdxCombo("##rslayer", width, act.LayerIdx, out int newIdx, 5, true, CFlags.NoArrowButton))
            act.LayerIdx = (newIdx == 5) ? -1 : newIdx;
    }

    public void DrawRestraint(RestraintAction act)
    {
        // State and layer row
        CkGui.FramedIconText(FAI.ListOl);
        CkGui.AttachToolTip("Invokes an interaction with the Restraints module");

        CkGui.ColorTextFrameAlignedInline(GetStateName(act.NewState), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline(act.NewState is NewState.Enabled ? "a restraint set" : "the active restraint set");
        if (act.NewState is NewState.Disabled)
            return;

        // For Apply, do the gag selection, otherwise do the lock selection.
        if (act.NewState is NewState.Enabled)
        {
            CkGui.FramedIconText(FAI.Handcuffs);
            CkGui.AttachToolTip("Indicates what restraint set is applied.");
            CkGui.TextFrameAlignedInline("Using");
            var item = _restraints.Storage.FirstOrDefault(r => r.Identifier == act.RestrictionId);
            CkGui.ColorTextFrameAlignedInline(item is { } re ? $"{re.Label.TrimText(50)}" : "<UNK>", ImGuiColors.TankBlue);
            return;
        }

        // Lock state
        CkGui.FramedIconText(FAI.Lock);
        CkGui.AttachToolTip("Indicates what lock will be applied to the restriction.");
        CkGui.TextFrameAlignedInline("Uses a");
        CkGui.ColorTextFrameAlignedInline(act.Padlock.ToName(), ImGuiColors.TankBlue);
        if (act.Padlock.IsTimerLock() && act.Padlock is not Padlocks.FiveMinutes)
        {
            CkGui.FramedIconText(FAI.HourglassHalf);
            CkGui.TextFrameAlignedInline("Between");
            CkGui.ColorTextFrameAlignedInline(act.LowerBound.ToGsRemainingTime(), ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("and");
            CkGui.ColorTextFrameAlignedInline(act.UpperBound.ToGsRemainingTime(), ImGuiColors.TankBlue);
        }
    }

    public void DrawRestraintRow(RestraintAction act)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Restrained].Handle, new(ImUtf8.FrameHeight));
        CkGui.AttachToolTip("Invokes an interaction with the Restraint module");
        switch (act.NewState)
        {
            case NewState.Enabled:
                var item = _restraints.Storage.FirstOrDefault(r => r.Identifier == act.RestrictionId);
                CkGui.ColorTextFrameAlignedInline("Applies", ImGuiColors.TankBlue);
                CkGui.TextFrameAlignedInline("a");
                CkGui.ColorTextFrameAlignedInline(item is { } re ? $"{re.Label.TrimText(20)}.." : "<UNK>", ImGuiColors.TankBlue);
                CkGui.AttachToolTip(item?.Label, item is null);
                CkGui.TextFrameAlignedInline("if not locked");
                break;
            case NewState.Locked:
                CkGui.TextFrameAligned("Use a");
                CkGui.ColorTextFrameAlignedInline(act.Padlock.ToName(), ImGuiColors.TankBlue);
                CkGui.TextFrameAlignedInline("on an unlocked, applied restraint");
                break;
            case NewState.Disabled:
                CkGui.ColorTextFrameAligned("Removes", ImGuiColors.TankBlue);
                CkGui.TextFrameAlignedInline("a restraint set, if unlocked and present");
                break;
        };


        if (!act.IsValid())
        {
            ImGui.SameLine();
            CkGui.FramedIconText(FAI.ExclamationTriangle, ImGuiColors.DalamudRed);
            CkGui.AttachToolTip("An Invalid Item/Lock/Layer is selected!");
        }
    }

    public void DrawRestraintEditor(RestraintAction act)
    {
        // State and layer row
        CkGui.FramedIconText(FAI.ListOl);
        CkGui.AttachToolTip("Invokes an interaction with the Restraints module");

        var stateW = ImGui.CalcTextSize("removem").X;
        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##restraint-state", stateW, act.NewState, out var newVal, _statesNoUnlock, GetStateName, flags: CFlags.NoArrowButton))
            act.NewState = newVal;

        CkGui.TextFrameAlignedInline(act.NewState is NewState.Enabled ? "a restraint set" : "the active restraint set");
        if (act.NewState is NewState.Disabled)
            return;

        // For Apply, do the gag selection, otherwise do the lock selection.
        if (act.NewState is NewState.Enabled)
        {
            CkGui.FramedIconText(FAI.Handcuffs);
            CkGui.AttachToolTip("Indicates what restraint set will be applied.");
            CkGui.TextFrameAlignedInline("Using");

            ImUtf8.SameLineInner();
            if (_restraintCombo.Draw("##restraints", act.RestrictionId, ImGui.GetContentRegionAvail().X))
                act.RestrictionId = _restraintCombo.Current?.Identifier ?? Guid.Empty;
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                act.RestrictionId = Guid.Empty;
            return;
        }

        // Lock state
        CkGui.FramedIconText(FAI.Lock);
        CkGui.AttachToolTip("Indicates what lock will be applied to the restraint set.");
        CkGui.TextFrameAlignedInline("Using a");

        ImUtf8.SameLineInner();
        var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
        if (CkGuiUtils.EnumCombo("##Padlock", ImGui.GetContentRegionAvail().X, act.Padlock, out var newLock, options, i => i.ToName(), flags: CFlags.NoArrowButton))
            act.Padlock = newLock;
        CkGui.AttachToolTip("The padlock that the trigger attempts to apply");

        // For timer locks we should ask for timer input
        if (act.Padlock.IsTimerLock() && act.Padlock is not Padlocks.FiveMinutes)
        {
            CkGui.FramedIconText(FAI.HourglassStart);
            CkGui.TextFrameAlignedInline("Minimum Lock Time");
            var minTime = _lowerTime ?? act.LowerBound.ToGsRemainingTime();
            var maxTime = _upperTime ?? act.UpperBound.ToGsRemainingTime();
            var length = ImGui.GetContentRegionAvail().X;
            if (CkGui.IconInputText(FAI.Clock, "minTime", "0h0m0s..", ref minTime, 32, length, true))
            {
                if (minTime != act.LowerBound.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(minTime, out var newTime))
                    act.LowerBound = newTime;
                // Clear it
                _lowerTime = null;
            }
            CkGui.AttachToolTip("The lower range when randomly picking a lock time.");

            CkGui.FramedIconText(FAI.HourglassStart);
            CkGui.TextFrameAlignedInline("Maximum Lock Time");
            if (CkGui.IconInputText(FAI.Clock, "minTime", "0h0m0s..", ref minTime, 32, length, true))
            {
                if (minTime != act.UpperBound.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(minTime, out var newTime))
                    act.UpperBound = newTime;
                // Clear it
                _upperTime = null;
            }
            CkGui.AttachToolTip("The upper range when randomly picking a lock time.");
        }
    }

    public void DrawRestraintRowEditor(RestraintAction act)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Restrained].Handle, new(ImUtf8.FrameHeight));
        CkGui.AttachToolTip("Invokes an interaction with the Restraint module");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##RestraintState", 60f, act.NewState, out var newState, [NewState.Enabled, NewState.Locked, NewState.Disabled],
            i => i switch { NewState.Enabled => "Apply", NewState.Locked => "Lock", _ => "Remove" }, flags: CFlags.NoArrowButton))
            act.NewState = newState;
        CkGui.AttachToolTip("The new state set on the chosen restraint set.");

        if (newState is NewState.Locked)
        {
            ImUtf8.SameLineInner();
            var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
            if (CkGuiUtils.EnumCombo("##PadlockType", 100f, act.Padlock, out var newVal, options, i => i.ToName(), flags: CFlags.NoArrowButton))
                act.Padlock = newVal;

            if (act.Padlock.IsTimerLock())
            {
                CkGui.TextFrameAlignedInline("TBD");
                // Implement timer shit later i guess.
            }
        }
        else
        {
            ImUtf8.SameLineInner();
            if (_restraintCombo.Draw("##RestraintSelector", act.RestrictionId, 120f, CFlags.NoArrowButton))
            {
                if (!act.RestrictionId.Equals(_restraintCombo.Current?.Identifier))
                    act.RestrictionId = _restraintCombo.Current?.Identifier ?? Guid.Empty;
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                act.RestrictionId = Guid.Empty;
        }
    }

    public void DrawMoodle(MoodleAction act)
    {
        CkGui.FramedIconText(FAI.WandMagicSparkles);
        CkGui.TextFrameAlignedInline("Applies the");
        CkGui.ColorTextFrameAlignedInline(act.MoodleItem.Type is MoodleType.Preset ? "preset" : "status", ImGuiColors.TankBlue);
        ImGui.SameLine(0, 0);
        CkGui.TextFrameAligned(":");
        if (act.MoodleItem is MoodlePreset p && MoodleCache.IpcData.Presets.TryGetValue(p.Id, out var preset))
        {
            CkGui.ColorTextFrameAlignedInline(preset.Title.StripColorTags(), ImGuiColors.TankBlue);
            if (preset.Statuses.Count > 0)
            {
                CkGui.FramedIconText(FAI.TheaterMasks);
                CkGui.ColorTextFrameAlignedInline("(", ImGuiColors.TankBlue, false);
                ImUtf8.SameLineInner();
                var statuses = MoodleCache.IpcData.StatusList.Where(x => preset.Statuses.Contains(x.GUID));
                _moodles.DrawStatusInfos(statuses.ToList(), MoodleDrawer.IconSizeFramed);
                ImGui.SameLine();
                CkGui.ColorTextFrameAligned(")", ImGuiColors.TankBlue);
            }
        }
        else if (MoodleCache.IpcData.Statuses.TryGetValue(act.MoodleItem.Id, out var status))
        {
            CkGui.ColorTextFrameAlignedInline(status.Title.StripColorTags(), ImGuiColors.TankBlue);

            CkGui.FramedIconText(FAI.TheaterMasks);
            CkGui.ColorTextFrameAlignedInline("(", ImGuiColors.TankBlue, false);
            ImUtf8.SameLineInner();
            _moodles.DrawStatusInfos([status], MoodleDrawer.IconSizeFramed);
            CkGui.ColorTextFrameAlignedInline(")", ImGuiColors.TankBlue);
        }
        else
        {
            CkGui.ColorTextFrameAlignedInline("<INVALID_SETUP>", ImGuiColors.DalamudRed);
        }
    }
    public void DrawMoodleRow(MoodleAction act)
    {
        CkGui.FramedIconText(FAI.TheaterMasks);
        CkGui.AttachToolTip("Invokes an interaction with Moodles");

        if (act.MoodleItem is MoodlePreset p && MoodleCache.IpcData.Presets.TryGetValue(p.Id, out var preset))
        {
            CkGui.TextFrameAlignedInline("Applies preset:");
            CkGui.ColorTextFrameAlignedInline(preset.Title.StripColorTags(), ImGuiColors.TankBlue);
            if (preset.Statuses.Count > 0)
            {
                CkGui.ColorTextFrameAlignedInline("(", ImGuiColors.TankBlue, false);
                ImUtf8.SameLineInner();
                var statuses = MoodleCache.IpcData.StatusList.Where(x => preset.Statuses.Contains(x.GUID));
                _moodles.DrawStatusInfos(statuses.ToList(), MoodleDrawer.IconSizeFramed);
                ImGui.SameLine();
                CkGui.ColorTextFrameAligned(")", ImGuiColors.TankBlue);
            }
        }
        else if (MoodleCache.IpcData.Statuses.TryGetValue(act.MoodleItem.Id, out var status))
        {
            CkGui.TextFrameAlignedInline("Applies status:");
            CkGui.ColorTextFrameAlignedInline(status.Title.StripColorTags(), ImGuiColors.TankBlue);

            CkGui.ColorTextFrameAlignedInline("(", ImGuiColors.TankBlue, false);
            ImUtf8.SameLineInner();
            _moodles.DrawStatusInfos([status], MoodleDrawer.IconSizeFramed);
            CkGui.ColorTextFrameAlignedInline(")", ImGuiColors.TankBlue);
        }
        else
        {
            CkGui.ColorTextFrameAlignedInline("<INVALID_SETUP>", ImGuiColors.DalamudRed);
        }

        if (!act.IsValid())
        {
            ImGui.SameLine();
            CkGui.FramedIconText(FAI.ExclamationTriangle, ImGuiColors.DalamudRed);
            CkGui.AttachToolTip("No Status/Preset Selected!");
        }
    }

    public void DrawMoodleEditor(MoodleAction act)
    {
        CkGui.FramedIconText(FAI.WandMagicSparkles);
        CkGui.TextFrameAlignedInline("Applies the");
        
        ImUtf8.SameLineInner();
        var width = ImGui.CalcTextSize("Statusm").X;
        if (CkGuiUtils.EnumCombo("##M_Type", width, act.MoodleItem.Type, out var newVal))
            act.MoodleItem = newVal is MoodleType.Preset ? new MoodlePreset() : new Moodle();

        if (act.MoodleItem is MoodlePreset preset)
        {
            ImUtf8.SameLineInner();
            if (_presetCombo.Draw("##M_Preset", preset.Id, ImGui.GetContentRegionAvail().X, CFlags.NoArrowButton))
                preset.UpdatePreset(_presetCombo.Current.GUID, _presetCombo.Current.Statuses);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                act.MoodleItem = new MoodlePreset();
        }
        else if (act.MoodleItem is Moodle status)
        {
            ImUtf8.SameLineInner();
            if (_statusCombo.Draw("##M_Status", status.Id, ImGui.GetContentRegionAvail().X, 1.75f, CFlags.NoArrowButton))
                status.UpdateId(_statusCombo.Current.GUID);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                act.MoodleItem = new Moodle();
        }

        // Then the next row.
        CkGui.FramedIconText(FAI.TheaterMasks);
        CkGui.TextFrameAlignedInline("Applied:"); 
        if (act.MoodleItem is MoodlePreset p)
        {
            if (MoodleCache.IpcData.Presets.TryGetValue(p.Id, out var presetData))
            {
                ImUtf8.SameLineInner();
                var statuses = MoodleCache.IpcData.StatusList.Where(x => presetData.Statuses.Contains(x.GUID));
                _moodles.DrawStatusInfos(statuses.ToList(), MoodleDrawer.IconSizeFramed);
            }
        }
        else if (MoodleCache.IpcData.Statuses.TryGetValue(act.MoodleItem.Id, out var statusData))
        {
            ImUtf8.SameLineInner();
            _moodles.DrawStatusInfos([statusData], MoodleDrawer.IconSizeFramed);
        }
        else
        {
            CkGui.ColorTextFrameAlignedInline("<INVALID_SETUP>", ImGuiColors.DalamudRed);
        }
    }

    public void DrawMoodleRowEditor(MoodleAction act, MoodleData ipc)
    {
        CkGui.FramedIconText(FAI.TheaterMasks);
        CkGui.AttachToolTip("Invokes an interaction with Moodles");

        CkGui.TextFrameAlignedInline("Apply");

        ImUtf8.SameLineInner();
        var curType = act.MoodleItem is MoodlePreset p ? MoodleType.Preset : MoodleType.Status;
        if (CkGuiUtils.EnumCombo("##M_Type", 40f, curType, out var newVal))
            act.MoodleItem = newVal is MoodleType.Preset ? new MoodlePreset() : new Moodle();

        if (act.MoodleItem is MoodlePreset preset)
        {
            ImUtf8.SameLineInner();
            if (_presetCombo.Draw("##M_Preset", preset.Id, 100f, CFlags.NoArrowButton))
                preset.UpdatePreset(_presetCombo.Current.GUID, _presetCombo.Current.Statuses);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                act.MoodleItem = new MoodlePreset();

            // Verify a second time incase the item has changed.
            if (preset.StatusIds.Count() > 0)
            {
                ImUtf8.SameLineInner();
                _moodles.DrawStatusInfos([ ..ipc.StatusList.Where(m => preset.StatusIds.Contains(m.GUID)) ], MoodleDrawer.IconSizeFramed);
            }
        }
        else if (act.MoodleItem is Moodle status)
        {
            ImUtf8.SameLineInner();
            var width = ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.Minus).X;
            if (_statusCombo.Draw("##M_Status", status.Id, width, CFlags.NoArrowButton))
                status.UpdateId(_statusCombo.Current.GUID);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                act.MoodleItem = new Moodle();

            // Verify a second time incase the item has changed.
            if (ipc.Statuses.TryGetValue(status.Id, out var match))
            {
                var offset = ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.Minus).X - MoodleDrawer.IconSizeFramed.X - ImUtf8.ItemInnerSpacing.X;
                ImGui.SameLine(offset);
                _moodles.DrawStatusInfos([match], MoodleDrawer.IconSizeFramed);
            }
        }
    }

    public void DrawShock(PiShockAction act)
    {
        CkGui.FramedIconText(FAI.Bolt);
        CkGui.TextFrameAlignedInline("Sends a");
        CkGui.ColorTextFrameAlignedInline(act.ShockInstruction.OpCode.ToString(), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("instruction to the collar");

        CkGui.FramedIconText(FAI.Stopwatch);
        CkGui.TextFrameAlignedInline("Lasting for");
        CkGui.ColorTextFrameAlignedInline($"{act.ShockInstruction.GetDurationFloat()}s", ImGuiColors.TankBlue);

        if (act.ShockInstruction.OpCode is not ShockMode.Beep)
        {
            CkGui.FramedIconText(FAI.Signal);
            CkGui.TextFrameAlignedInline("With");
            CkGui.ColorTextFrameAlignedInline($"{act.ShockInstruction.Intensity}%", ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("intensity");
        }
    }

    public void DrawShockRow(PiShockAction act)
    {
        CkGui.FramedIconText(FAI.Bolt);
        CkGui.AttachToolTip("Invokes a interaction with your Shock Collar");

        CkGui.ColorTextFrameAlignedInline(act.ShockInstruction.OpCode.ToString(), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("for");
        CkGui.ColorTextFrameAlignedInline(act.ShockInstruction.Duration.ToString(), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("ms");

        // display extra information if a vibrator or shock.
        if (act.ShockInstruction.OpCode is not ShockMode.Beep)
        {
            CkGui.TextFrameAlignedInline("at");
            CkGui.ColorTextFrameAlignedInline(act.ShockInstruction.Intensity.ToString(), ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("intensity");
        }
    }

    public void DrawShockEditor(PiShockAction act)
    {
        CkGui.FramedIconText(FAI.Bolt);
        CkGui.TextFrameAlignedInline("Sends a");

        var opWidth = ImGui.CalcTextSize("Vibratem").X;
        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##OpCode", opWidth, act.ShockInstruction.OpCode, out var mode))
            act.ShockInstruction.OpCode = mode;
        CkGui.AttachToolTip("What type of instruction to send to the Shock Collar.");
        
        CkGui.TextFrameAlignedInline("instruction to the collar");

        CkGui.FramedIconText(FAI.Stopwatch);
        CkGui.TextFrameAlignedInline("Lasting for");

        var dur = act.ShockInstruction.GetDurationFloat();
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.SliderFloat("##Duration", ref dur, 0.016f, 15f, "%.3fs"))
            act.ShockInstruction.SetDuration(dur);

        if (act.ShockInstruction.OpCode is not ShockMode.Beep)
        {
            CkGui.FramedIconText(FAI.Signal);
            CkGui.TextFrameAlignedInline("With an intensity of");

            var intensity = act.ShockInstruction.Intensity;
            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.SliderInt("##ShockIntensity", ref intensity, 0, 100))
                act.ShockInstruction.Intensity = intensity;
        }
    }

    public void DrawShockRowEditor(PiShockAction action)
    {
        CkGui.FramedIconText(FAI.Bolt);
        CkGui.AttachToolTip("Invokes a interaction with your Shock Collar");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##OpCodeEdit", 50f, action.ShockInstruction.OpCode, out var mode))
            action.ShockInstruction.OpCode = mode;

        // We love wacky pi-shock API YIPPEEEEE *dies*
        var durationRef = action.ShockInstruction.GetDurationFloat();
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(85f);
        if (ImGui.SliderFloat("##ShockDur", ref durationRef, 0.016f, 15f))
            action.ShockInstruction.SetDuration(durationRef);
        CkGui.AttachToolTip("The duration of the instruction.");

        // display extra information if a vibrator or shock.
        if (action.ShockInstruction.OpCode is not ShockMode.Beep)
        {
            ImUtf8.SameLineInner();
            var intensity = action.ShockInstruction.Intensity;
            ImGui.SetNextItemWidth(85f);
            if (ImGui.SliderInt("##ShockIntensity", ref intensity, 0, 100, "%d%%"))
                action.ShockInstruction.Intensity = intensity;
            CkGui.AttachToolTip("The intensity of the instruction.");
        }
    }

    public void DrawToy(SexToyAction act)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Vibrator].Handle, new(ImUtf8.FrameHeight));
        CkGui.AttachToolTip("How long to delay the invocation on the active devices.");

        CkGui.TextFrameAlignedInline("Starts after");
        CkGui.ColorTextFrameAlignedInline($"{act.StartAfter.Seconds}s {act.StartAfter.Milliseconds}ms", ImGuiColors.TankBlue);

        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Vibrator].Handle, new(ImUtf8.FrameHeight));
        CkGui.AttachToolTip("How long until we stop the action after it starts.");

        CkGui.TextFrameAlignedInline("Ends after");
        CkGui.ColorTextFrameAlignedInline($"{act.EndAfter.Seconds}s {act.EndAfter.Milliseconds}ms", ImGuiColors.TankBlue);

        // Next line.
        CkGui.FramedIconText(FAI.Filter);
        CkGui.TextFrameAlignedInline("Execute a");
        CkGui.ColorTextFrameAlignedInline(act.ActionKind.ToString(), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("to all active toys");

        if (act.ActionKind is ToyActionType.Pattern)
        {
            CkGui.FramedIconText(FAI.Play);
            CkGui.TextFrameAlignedInline("Play the pattern");
            var name = _patterns.Storage.ByIdentifier(act.PatternId)?.Label ?? "<UNK>";
            CkGui.ColorTextFrameAlignedInline(name, ImGuiColors.TankBlue);
        }
        else
        {
            CkGui.FramedIconText(FAI.Signal);
            CkGui.TextFrameAlignedInline("With");
            CkGui.ColorTextFrameAlignedInline($"{act.Intensity}%", ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("intensity");
        }
    }

    public void DrawToyRow(SexToyAction act)
    {
        CkGui.FramedIconText(FAI.WaveSquare);
        CkGui.AttachToolTip("Invokes a interaction with your sex toys");

        // in theory this listing could get pretty expansive so for now just list a summary.
        CkGui.TextFrameAlignedInline("After");
        CkGui.ColorTextFrameAlignedInline($"{act.StartAfter.Seconds}s {act.StartAfter.Milliseconds}ms", ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("toys to vibrate for");
        CkGui.ColorTextFrameAlignedInline($"{act.EndAfter.Seconds}s {act.EndAfter.Milliseconds}ms", ImGuiColors.TankBlue);
    }

    public void DrawToyEditor(SexToyAction act)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Vibrator].Handle, new(ImUtf8.FrameHeight));
        CkGui.TextFrameAlignedInline("Start toys after");
        // Editor for timespan here or something idk.
        var refStart = act.StartAfter;
        ImGui.SameLine();
        CkGuiUtils.DrawTimeSpanLine("ToyActStart", TimeSpan.FromMilliseconds(59999), ref refStart, "ss\\:fff", true);
        act.StartAfter = refStart;

        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Vibrator].Handle, new(ImUtf8.FrameHeight));
        CkGui.TextFrameAlignedInline("Ends after");
        ImGui.SameLine();
        var refEnd = act.EndAfter;
        CkGuiUtils.DrawTimeSpanLine("ToyActEnd", TimeSpan.FromMilliseconds(59999), ref refEnd, "ss\\:fff", true);
        act.EndAfter = refEnd;
      
        // Next line.
        CkGui.FramedIconText(FAI.Filter);
        CkGui.TextFrameAlignedInline("Execute a");

        var typeW = ImGui.CalcTextSize("Vibrationm").X;
        ImGui.SameLine();
        if (CkGuiUtils.EnumCombo("##ToyActionType", typeW, act.ActionKind, out var newVal))
            act.ActionKind = newVal;
        CkGui.AttachToolTip("The kind of action to perform on all active toys.");

        CkGui.TextFrameAlignedInline("to all active toys");

        if (act.ActionKind is ToyActionType.Pattern)
        {
            CkGui.FramedIconText(FAI.Play);
            CkGui.TextFrameAlignedInline("Play the pattern");

            if (_patternCombo.Draw("##ToyPattern", act.PatternId, ImGui.GetContentRegionAvail().X))
                act.PatternId = _patternCombo.Current?.Identifier ?? Guid.Empty;
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                act.PatternId = Guid.Empty;
        }
        else
        {
            CkGui.FramedIconText(FAI.Signal);
            CkGui.TextFrameAlignedInline("With an intensity of");

            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var intensity = act.Intensity;
            if (ImGui.SliderInt("##intensity", ref intensity, 0, 100))
                act.Intensity = intensity;
            CkGui.AttachToolTip("The intensity of the action performed on the toys.");
        }
    }

    public void DrawToyRowEditor(SexToyAction act)
    {
        using var _ = ImRaii.Group();
        CkGui.FramedIconText(FAI.WaveSquare);
        CkGui.AttachToolTip("The action to be executed on the listed toys.");

        // in theory this listing could get pretty expansive so for now just list a summary.
        CkGui.TextFrameAlignedInline("Buzz for");

        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(85f);
        var valueE = (float)act.EndAfter.TotalSeconds;
        if (ImGui.SliderFloat("##EndAfter", ref valueE, 0.016f, 15f))
            act.EndAfter = TimeSpan.FromSeconds(valueE);
        CkGui.AttachToolTip("How long the buzz will go on for.");

        ImGui.SameLine(0, 1);
        CkGui.TextFrameAligned("(");
        ImGui.SameLine(0, 1);

        ImGui.SetNextItemWidth(85f);
        var valueS = (float)act.StartAfter.TotalSeconds;
        if (ImGui.SliderFloat("##StartAfter", ref valueS, 0.016f, 15f))
            act.StartAfter = TimeSpan.FromSeconds(valueS);
        CkGui.AttachToolTip("The time to wait before starting the toy actions.");
        ImGui.SameLine(0, 1);
        CkGui.TextFrameAligned("delay)");
    }
}
