using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;

// Contains common shared methods in both the Marionette and Alias Tabs of Puppeteer
public sealed class AliasTriggerDrawer
{
    private readonly ILogger<AliasTriggerDrawer> _logger;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly PuppeteerManager _manager;
    private readonly MoodleDrawer _moodles;

    // Related combos
    private RestrictionCombo _restrictionCombo;
    private RestraintCombo _restraintCombo;
    private MoodleStatusCombo _statusCombo;
    private MoodlePresetCombo _presetCombo;

    public AliasTriggerDrawer(
        ILogger<AliasTriggerDrawer> logger,
        GagspeakMediator mediator,
        MoodleDrawer moodleDrawer,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        PuppeteerManager manager,
        FavoritesConfig favorites)
    {
        _logger = logger;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _manager = manager;
        _moodles = moodleDrawer;

        _restrictionCombo = new RestrictionCombo(logger, mediator, favorites, () => [
            ..restrictions.Storage.OrderByDescending(p => FavoritesConfig.Restrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        _restraintCombo = new RestraintCombo(logger, mediator, favorites, () => [
            ..restraints.Storage.OrderByDescending(p => FavoritesConfig.Restraints.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        _statusCombo = new MoodleStatusCombo(logger, 1.15f);
        _presetCombo = new MoodlePresetCombo(logger, 1.15f);
    }

    private string TrimDisplay(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max)
            return s;

        return s[..max] + "..";
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

    public void DrawGagRowEditor(GagAction act)
    {
        ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, new(ImUtf8.FrameHeight));
        CkGui.AttachToolTip("Invokes an interaction with the Gags module");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##GagState", 60f, act.NewState, out var newState, [NewState.Enabled, NewState.Locked, NewState.Disabled],
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
        if (CkGuiUtils.LayerIdxCombo("##gagLayer", width, act.LayerIdx, out int newIdx, 3, true, CFlags.NoArrowButton))
            act.LayerIdx = (newIdx == 3) ? -1 : newIdx;
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
                CkGui.ColorTextFrameAlignedInline(item is { } re ? $"{TrimDisplay(re.Label, 20)}" : "<UNK>", ImGuiColors.TankBlue);
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
                CkGui.ColorTextFrameAlignedInline(item is { } re ? $"{TrimDisplay(re.Label, 20)}.." : "<UNK>", ImGuiColors.TankBlue);
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
                _moodles.DrawStatusInfos(statuses, MoodleDrawer.IconSizeFramed);
                CkGui.ColorTextFrameAlignedInline(")", ImGuiColors.TankBlue);
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

    public void DrawMoodleRowEditor(MoodleAction action, MoodleData ipc)
    {
        CkGui.FramedIconText(FAI.TheaterMasks);
        CkGui.AttachToolTip("Invokes an interaction with Moodles");

        CkGui.TextFrameAlignedInline("Apply");

        ImUtf8.SameLineInner();
        var curType = action.MoodleItem is MoodlePreset p ? MoodleType.Preset : MoodleType.Status;
        if (CkGuiUtils.EnumCombo("##M_Type", 40f, curType, out var newVal))
        {
            if (curType != newVal)
                action.MoodleItem = newVal is MoodleType.Preset ? new MoodlePreset() : new Moodle();
        }

        if (action.MoodleItem is MoodlePreset preset)
        {
            ImUtf8.SameLineInner();
            if (_presetCombo.Draw("##M_Preset", preset.Id, 100f, CFlags.NoArrowButton))
                preset.UpdatePreset(_presetCombo.Current.GUID, _presetCombo.Current.Statuses);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                action.MoodleItem = new MoodlePreset();

            // Verify a second time incase the item has changed.
            if (preset.StatusIds.Count() > 0)
            {
                ImUtf8.SameLineInner();
                _moodles.DrawStatusInfos([ ..ipc.StatusList.Where(m => preset.StatusIds.Contains(m.GUID)) ], MoodleDrawer.IconSizeFramed);
            }
        }
        else if (action.MoodleItem is Moodle status)
        {
            ImUtf8.SameLineInner();
            if (_statusCombo.Draw("##M_Status", status.Id, 100f, CFlags.NoArrowButton))
                status.UpdateId(_statusCombo.Current.GUID);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                action.MoodleItem = new Moodle();

            // Verify a second time incase the item has changed.
            if (ipc.Statuses.TryGetValue(status.Id, out var match))
            {
                ImUtf8.SameLineInner();
                _moodles.DrawStatusInfos([match], MoodleDrawer.IconSizeFramed);
            }
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

    public void DrawToyRow(SexToyAction act)
    {
        CkGui.FramedIconText(FAI.WaveSquare);
        CkGui.AttachToolTip("Invokes a interaction with your sex toys");

        // in theory this listing could get pretty expansive so for now just list a summary.
        CkGui.TextFrameAlignedInline("After");
        CkGui.ColorTextFrameAlignedInline(act.StartAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("toys to vibrate for");
        CkGui.ColorTextFrameAlignedInline(act.EndAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);
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
