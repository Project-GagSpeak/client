using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.Raii;
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
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Components;

// Scoped, sealed class to draw the editor and display components of aliasItems.
public sealed class AliasItemDrawer
{
    private readonly ILogger<AliasItemDrawer> _logger;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly PuppeteerManager _manager;
    private readonly MoodleDrawer _moodleDrawer;

    private static readonly string[] ThreeLayerNames = [ "Layer 1", "Layer 2", "Layer 3", "Any Layer" ];
    private static readonly string[] FiveLayerNames = [ "Layer 1", "Layer 2", "Layer 3", "Layer 4", "Layer 5", "Any Layer" ];
    private HashSet<Guid> ExpandedTriggers = new HashSet<Guid>();

    private RestrictionCombo _restrictionCombo { get; init; }
    private RestraintCombo _restraintCombo { get; init; }
    private MoodleStatusCombo _statusCombo { get; init; }
    private MoodlePresetCombo _presetCombo { get; init; }
    public AliasItemDrawer(
        ILogger<AliasItemDrawer> logger,
        GagspeakMediator mediator,
        MoodleDrawer moodleDrawer,
        MoodleIcons moodleDisplayer,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        PuppeteerManager manager,
        FavoritesManager favorites)
    {
        _logger = logger;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _manager = manager;
        _moodleDrawer = moodleDrawer;

        _restrictionCombo = new RestrictionCombo(logger, mediator, favorites, () => [
            ..restrictions.Storage.OrderByDescending(p => favorites._favoriteRestrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        _restraintCombo = new RestraintCombo(logger, mediator, favorites, () => [
            ..restraints.Storage.OrderByDescending(p => favorites._favoriteRestraints.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        _statusCombo = new MoodleStatusCombo(1.15f, moodleDisplayer, logger);
        _presetCombo = new MoodlePresetCombo(1.15f, moodleDisplayer, logger);
    }

    public void DrawAliasTrigger(AliasTrigger aliasItem, CharaIPCData ipc, bool canEdit = true)
    {
        var isContained = ExpandedTriggers.Contains(aliasItem.Identifier);
        var shownActions = isContained ? aliasItem.Actions.Count() : 1;
        var pos = ImGui.GetCursorScreenPos();
        var childH = (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y) * (shownActions + 1);

        using var _ = CkRaii.FramedChildPaddedW($"AliasItem{aliasItem.Identifier}", ImGui.GetContentRegionAvail().X, childH,
            CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f)));

        using (ImRaii.Group())
        {
            var rightButtonWidth = canEdit
                ? CkGui.IconButtonSize(FAI.Edit).X * 2 + ImGui.GetStyle().ItemInnerSpacing.X
                : CkGui.IconButtonSize(FAI.Edit).X;

            CkGui.BooleanToColoredIcon(aliasItem.Enabled, false);
            if (ImGui.IsItemClicked())
                _manager.ToggleState(aliasItem);
            CkGui.AttachToolTip("Click to toggle the AliasTriggers state!--SEP--Current State is: " + (aliasItem.Enabled ? "Enabled" : "Disabled"));

            // Draw out the name.
            CkGui.TextFrameAlignedInline(aliasItem.Label.IsNullOrEmpty() ? "<No Alias Name Set!>" : aliasItem.Label);

            // Draw out the quote marks with the phrase.
            ImGui.SameLine();
            CkGui.FramedIconText(FAI.QuoteLeft, ImGuiColors.DalamudGrey2);

            using (ImRaii.PushFont(UiBuilder.MonoFont))
                CkGui.TextFrameAlignedInline(aliasItem.InputCommand, true);
            CkGui.AttachToolTip("The text to scan for (Input String)");

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.QuoteRight, ImGuiColors.DalamudGrey2);

            // Draw out the dropdown button.
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - rightButtonWidth);

            if(canEdit)
            {
                if (CkGui.IconButton(FAI.Edit, inPopup: true))
                    _manager.StartEditing(aliasItem);

                ImUtf8.SameLineInner();
            }

            var isExpanded = ExpandedTriggers.Contains(aliasItem.Identifier);
            if (CkGui.IconButton(isExpanded ? FAI.ChevronUp : FAI.ChevronDown, inPopup: true))
            {
                if (isExpanded)
                    ExpandedTriggers.Remove(aliasItem.Identifier);
                else
                    ExpandedTriggers.Add(aliasItem.Identifier);
            }
            CkGui.AttachToolTip(isExpanded ? "Collapse" : "Expand");
        }

        if (shownActions > 0)
            ImGui.Separator();

        // Get the actions to show.
        var actionsToShow = isContained ? aliasItem.Actions : aliasItem.Actions.Take(1);

        // Draw out the actions.
        foreach (var triggerAction in actionsToShow)
        {
            switch (triggerAction)
            {
                case TextAction ta: DrawOutputTextAction(ta); break;
                case GagAction ga: DrawGagAction(ga); break;
                case RestrictionAction rsa: DrawRestrictionAction(rsa); break;
                case RestraintAction rta: DrawRestraintAction(rta); break;
                case MoodleAction ma: DrawMoodleAction(ma, ipc); break;
                case PiShockAction ps: DrawShockAction(ps); break;
                case SexToyAction sta: DrawSexToyAction(sta); break;

                default: throw new InvalidOperationException($"Bad Type: {triggerAction.ActionType}");
            }
        }

        void DrawOutputTextAction(TextAction ta)
        {
            using var _ = ImRaii.Group();
            CkGui.FramedIconText(FAI.Font);

            var txt = ta.OutputCommand.IsNullOrEmpty() ? "<Undefined Output!>" : ta.OutputCommand;
            CkGui.ColorTextFrameAlignedInline("/" + txt, ImGuiColors.TankBlue);
            CkGui.AttachToolTip("What command you execute when the above alias string is said." +
                "--SEP-- TIP: Do not include the '/' in your output.");

            if (!isContained && aliasItem.Actions.Count() > 1)
            {
                ImGui.SameLine();
                CkGui.RightFrameAlignedColor($"+{aliasItem.Actions.Count() - 1} more", ImGuiColors.TankBlue);
            }
        }
    }

    public void DrawAliasTriggerEditor(IEnumerable<InvokableActionType> selectableTypes, ref InvokableActionType selected)
    {
        if (_manager.ItemInEditor is not { } aliasItem)
            return;

        var pos = ImGui.GetCursorScreenPos();
        var childH = aliasItem.Actions.Any(x => x.ActionType == InvokableActionType.SexToy)
            ? (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y) * (aliasItem.Actions.Count + 3)
            : (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y) * (aliasItem.Actions.Count + 2);

        using var _ = CkRaii.FramedChildPaddedW($"AliasItem{aliasItem.Identifier}", ImGui.GetContentRegionAvail().X, childH,
            CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f)), wFlags: ImGuiWindowFlags.NoScrollbar);

        using (ImRaii.Group())
        {
            var comboWidth = 100f;
            var rightWidth = (CkGui.IconButtonSize(FAI.Save).X + ImGui.GetStyle().ItemInnerSpacing.X) * 3 + comboWidth;

            // Label editor.
            var tempName = aliasItem.Label;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (rightWidth + ImGui.GetFrameHeight()));
            if (ImGui.InputTextWithHint($"##label_{aliasItem.Identifier}", "Give Alias a Label...", ref tempName, 70))
                aliasItem.Label = tempName;
            CkGui.AttachToolTip("The Alias Label given to help with searching and organization.");

            // draw out the combo and buttons.
            var currentTypes = aliasItem.Actions.Select(x => x.ActionType);
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - rightWidth);

            var comboOptions = selectableTypes.Except(currentTypes);

            if (CkGui.IconButton(FAI.Plus, disabled: comboOptions.Count() <= 0, inPopup: true))
            {
                // Attempt to add the new type.
                aliasItem.Actions.Add(selected switch
                {
                    InvokableActionType.TextOutput => new TextAction(),
                    InvokableActionType.Gag => new GagAction(),
                    InvokableActionType.Restriction => new RestrictionAction(),
                    InvokableActionType.Restraint => new RestraintAction(),
                    InvokableActionType.Moodle => new MoodleAction(),
                    InvokableActionType.ShockCollar => new PiShockAction(),
                    InvokableActionType.SexToy => new SexToyAction(),
                    _ => throw new ArgumentOutOfRangeException(nameof(selected), selected, null)
                });
                // sort the order of the actions.
                aliasItem.Actions = aliasItem.Actions.OrderBy(x => x.ActionType).ToHashSet();
                // reset the selected type.
                selected = selectableTypes.Except(aliasItem.Actions.Select(x => x.ActionType)).FirstOrDefault();
            }
            CkGui.AttachToolTip("Click to add a new action of the selected type to this Alias Item." +
                "--SEP-- The new action will be added to the end of the list.");

            ImUtf8.SameLineInner();
            using (ImRaii.Disabled(comboOptions.Count() <= 0))
            {
                if (CkGuiUtils.EnumCombo("##Types", 100f, selected, out var newVal, selectableTypes.Except(currentTypes), i => i.ToName(), "All In Use", CFlags.NoArrowButton))
                    selected = newVal;
            }
            CkGui.AttachToolTip("Selects a new output action kind to add to this Alias Item.");

            ImUtf8.SameLineInner();
            if (CkGui.IconButton(FAI.Save, inPopup: true))
                _manager.SaveChangesAndStopEditing();
            CkGui.AttachToolTip("Click to save changes to this Alias Item.--SEP-- This will also close the editor.");

            ImUtf8.SameLineInner();
            if (CkGui.IconButton(FAI.Trash, inPopup: true))
                _manager.StopEditing();
            CkGui.AttachToolTip("Delete this Alias Item.--SEP-- This will also close the editor.");
        }

        ImGui.Separator();

        using (ImRaii.Group())
        {
            CkGui.FramedIconText(FAI.Eye);
            CkGui.AttachToolTip("What phrase to listen for in chat to execute this alias.");

            ImUtf8.SameLineInner();
            var inputText = aliasItem.InputCommand;
            using (ImRaii.PushFont(UiBuilder.MonoFont))
                if (ImGui.InputTextWithHint("##TextInputEdit", "What to listen for..", ref inputText, 64))
                    aliasItem.InputCommand = inputText;
        }

        var ipc = MoodleCache.IpcData;
        foreach (var triggerAction in aliasItem.Actions)
        {
            switch (triggerAction)
            {
                case TextAction ta: DrawOutputTextActionEdit(ta); break;
                case GagAction ga: DrawGagActionEdit(ga); break;
                case RestrictionAction rsa: DrawRestrictionActionEdit(rsa); break;
                case RestraintAction rta: DrawRestraintActionEdit(rta); break;
                case MoodleAction ma: DrawMoodleActionEdit(ma, ipc); break;
                case PiShockAction ps: DrawShockActionEdit(ps); break;
                case SexToyAction sta: DrawSexToyActionEdit(sta); break;

                default: throw new InvalidOperationException($"Bad Type: {triggerAction.ActionType}");
            }

            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
            {
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.Minus).X);
                if (CkGui.IconButton(FAI.Minus, id: $"##remove_{aliasItem.Identifier}_{triggerAction.ActionType}", inPopup: true))
                {
                    // remove the action type from the list and break out of the loop early.
                    aliasItem.Actions.Remove(triggerAction);
                    selected = selectableTypes.Except(aliasItem.Actions.Select(x => x.ActionType)).FirstOrDefault();
                    break;
                }
                CkGui.AttachToolTip("Click to remove this action from the Alias Item.");
            }
        }
    }

    public void DrawOutputTextAction(TextAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.Font);

        var txt = action.OutputCommand.IsNullOrEmpty() ? "<Undefined Output!>" : action.OutputCommand;
        CkGui.ColorTextFrameAlignedInline("/" + txt, ImGuiColors.TankBlue);
        CkGui.AttachToolTip("What command you execute when the above alias string is said." +
            "--SEP-- TIP: Do not include the '/' in your output.");
    }

    public void DrawOutputTextActionEdit(TextAction act)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.Font);
        CkGui.AttachToolTip("What text command you will output when the input text is read from this Kinkster." +
            "--SEP--The / is appended for you.");

        ImUtf8.SameLineInner();
        var outputText = act.OutputCommand;
        using (ImRaii.PushFont(UiBuilder.MonoFont))
            if (ImGui.InputTextWithHint("##TextOutputEdit", "output text response", ref outputText, 256))
                act.OutputCommand = outputText;
    }

    public void DrawGagAction(GagAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.Comment);
        CkGui.AttachToolTip("An applied Gag state change.");

        var isPadlockAct = action.NewState is NewState.Locked or NewState.Unlocked;
        var layerText = action.LayerIdx is -1 ? "On any open layer" : $"On layer {action.LayerIdx}";
        CkGui.TextFrameAlignedInline($"{layerText}, a");
        CkGui.ColorTextFrameAlignedInline(isPadlockAct ? action.GagType.GagName() : action.Padlock.ToName(), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("will be");
        CkGui.ColorTextFrameAlignedInline(action.NewState.ToString(), ImGuiColors.TankBlue);
    }

    public void DrawGagActionEdit(GagAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.Comment);
        CkGui.AttachToolTip("The Following Gag State that will be applied to the Kinkster.");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##GagState", 60f, action.NewState, out var newState, [NewState.Enabled, NewState.Locked, NewState.Disabled],
            i => i switch { NewState.Enabled => "Apply", NewState.Locked => "Lock", _ => "Remove" }, flags: CFlags.NoArrowButton))
            action.NewState = newState;
        CkGui.AttachToolTip("The new state set on the targeted gag.");

        CkGui.TextFrameAlignedInline("a");

        if (newState is NewState.Locked)
        {
            ImUtf8.SameLineInner();
            var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
            if (CkGuiUtils.EnumCombo("##PadlockType", 100f, action.Padlock, out var newVal, options, i => i.ToName(), flags: CFlags.NoArrowButton))
                action.Padlock = newVal;

            if (action.Padlock.IsTimerLock())
            {
                CkGui.TextFrameAlignedInline("for");
                // Implement timer shit later i guess.
            }
        }
        else
        {
            ImUtf8.SameLineInner();
            if (CkGuiUtils.EnumCombo("##GagType", 100f, action.GagType, out var newVal, i => i switch { GagType.None => "Any Gag", _ => i.GagName() }, flags: CFlags.NoArrowButton))
                action.GagType = newVal;
        }

        CkGui.TextFrameAlignedInline("on");

        ImUtf8.SameLineInner();
        var tmpIdx = action.LayerIdx;
        ImGui.SetNextItemWidth(65f * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("##gagLayer", ref tmpIdx, ThreeLayerNames, 4))
            action.LayerIdx = (tmpIdx == 3) ? -1 : tmpIdx;
    }

    public void DrawRestrictionAction(RestrictionAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.Handcuffs);
        CkGui.AttachToolTip("An applied Restriction Item state change.");

        // the following gag
        var isPadlockAct = action.NewState is NewState.Locked or NewState.Unlocked;
        var layerText = action.LayerIdx is -1 ? "On any open layer" : $"On layer {action.LayerIdx}";
        CkGui.TextFrameAlignedInline($"{layerText}, a");
        var name = _restrictions.Storage.FirstOrDefault(r => r.Identifier == action.RestrictionId) is { } r ? r.Label : "<UNDEFINED ITEM>";
        if (isPadlockAct)
        {
            CkGui.ColorTextFrameAlignedInline(action.Padlock.ToName(), ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("will get");
            CkGui.ColorTextFrameAlignedInline(action.NewState is NewState.Locked ? "locked" : "unlocked", ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("on the restriction");
            CkGui.ColorTextFrameAlignedInline(name, ImGuiColors.TankBlue);
        }
        else
        {
            CkGui.ColorTextFrameAlignedInline(name, ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("will be");
            CkGui.ColorTextFrameAlignedInline(action.NewState.ToString(), ImGuiColors.TankBlue);
        }
    }

    public void DrawRestrictionActionEdit(RestrictionAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.Handcuffs);
        CkGui.AttachToolTip("The Restriction Action performed to the Kinkster.");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##RestrictionState", 60f, action.NewState, out var newState, [NewState.Enabled, NewState.Locked, NewState.Disabled],
            i => i switch { NewState.Enabled => "Apply", NewState.Locked => "Lock", _ => "Remove" }, flags: CFlags.NoArrowButton))
            action.NewState = newState;
        CkGui.AttachToolTip("The new state set on the targeted restriction item.");

        CkGui.TextFrameAlignedInline("a");
        if (newState is NewState.Locked)
        {
            ImUtf8.SameLineInner();
            var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
            if (CkGuiUtils.EnumCombo("##PadlockType", 100f, action.Padlock, out var newVal, options, i => i.ToName(), flags: CFlags.NoArrowButton))
                action.Padlock = newVal;

            if (action.Padlock.IsTimerLock())
            {
                CkGui.TextFrameAlignedInline("for");
                // Implement timer shit later i guess.
            }
        }
        else
        {
            ImUtf8.SameLineInner();
            if (_restrictionCombo.Draw("##RestrictSel", action.RestrictionId, 120f, CFlags.NoArrowButton))
            {
                if (!action.RestrictionId.Equals(_restrictionCombo.Current?.Identifier))
                    action.RestrictionId = _restrictionCombo.Current?.Identifier ?? Guid.Empty;
            }
        }
        
        CkGui.TextFrameAlignedInline("on");

        ImGui.SameLine();
        var tmpIdx = action.LayerIdx;
        ImGui.SetNextItemWidth(65f * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("##restrictionLayer", ref tmpIdx, FiveLayerNames, 4))
            action.LayerIdx = (tmpIdx == 5) ? -1 : tmpIdx;
    }

    public void DrawRestraintAction(RestraintAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.ToiletPortable);
        CkGui.AttachToolTip("An applied Restriction Item state change.");

        var isPadlockAct = action.NewState is NewState.Locked or NewState.Unlocked;
        CkGui.TextFrameAlignedInline("A");

        var name = _restraints.Storage.FirstOrDefault(r => r.Identifier == action.RestrictionId) is { } r ? r.Label : "<UNDEFINED SET>";
        if (isPadlockAct)
        {
            CkGui.ColorTextFrameAlignedInline(action.Padlock.ToName(), ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("will get");
            CkGui.ColorTextFrameAlignedInline(action.NewState is NewState.Locked ? "locked" : "unlocked", ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("on the set");
            CkGui.ColorTextFrameAlignedInline(name, ImGuiColors.TankBlue);
        }
        else
        {
            CkGui.ColorTextFrameAlignedInline(name, ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("will be");
            CkGui.ColorTextFrameAlignedInline(action.NewState.ToString(), ImGuiColors.TankBlue);
        }
    }

    public void DrawRestraintActionEdit(RestraintAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.ToiletPortable);
        CkGui.AttachToolTip("The Restraint Set Action performed to the Kinkster.");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##RestraintState", 60f, action.NewState, out var newState, [NewState.Enabled, NewState.Locked, NewState.Disabled],
            i => i switch { NewState.Enabled => "Apply", NewState.Locked => "Lock", _ => "Remove" }, flags: CFlags.NoArrowButton))
            action.NewState = newState;
        CkGui.AttachToolTip("The new state set on the chosen restraint set.");

        CkGui.TextFrameAlignedInline("a");

        if (newState is NewState.Locked)
        {
            ImUtf8.SameLineInner();
            var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
            if (CkGuiUtils.EnumCombo("##PadlockType", 100f, action.Padlock, out var newVal, options, i => i.ToName(), flags: CFlags.NoArrowButton))
                action.Padlock = newVal;

            if (action.Padlock.IsTimerLock())
            {
                CkGui.TextFrameAlignedInline("for");
                // Implement timer shit later i guess.
            }
        }
        else
        {
            ImUtf8.SameLineInner();
            if (_restraintCombo.Draw("##RestraintSelector", action.RestrictionId, 120f, CFlags.NoArrowButton))
            {
                if (!action.RestrictionId.Equals(_restrictionCombo.Current?.Identifier))
                    action.RestrictionId = _restrictionCombo.Current?.Identifier ?? Guid.Empty;
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                action.RestrictionId = Guid.Empty;
        }
    }

    public void DrawMoodleAction(MoodleAction action, CharaIPCData ipc)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.WandMagicSparkles);
        CkGui.AttachToolTip("Displays the Identity of the moodle status/preset applied.");

        if (action.MoodleItem is MoodlePreset p && ipc.Presets.TryGetValue(p.Id, out var preset))
        {
            CkGui.TextFrameAlignedInline("The Moodle preset ");
            CkGui.ColorTextFrameAlignedInline(preset.Title.StripColorTags(), ImGuiColors.TankBlue);
            if (preset.Statuses.Count > 0)
            {
                CkGui.TextFrameAlignedInline("applies the moodles");
                ImUtf8.SameLineInner();
                var statuses = ipc.StatusList.Where(x => preset.Statuses.Contains(x.GUID));
                _moodleDrawer.DrawStatusInfos(statuses, MoodleDrawer.IconSizeFramed);
            }
            else
                CkGui.TextFrameAlignedInline("is applied.");
        }
        else if (ipc.Statuses.TryGetValue(action.MoodleItem.Id, out var status))
        {
            CkGui.TextFrameAlignedInline("The Moodle ");
            CkGui.ColorTextFrameAlignedInline(status.Title.StripColorTags(), ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("is applied.");
            ImUtf8.SameLineInner();
            _moodleDrawer.DrawStatusInfos([status], MoodleDrawer.IconSizeFramed);
        }
        else
        {
            CkGui.ColorTextFrameAlignedInline("Moodle Status/Preset is not currently set.", ImGuiColors.TankBlue);
        }
    }

    public void DrawMoodleActionEdit(MoodleAction action, CharaIPCData ipc)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.WandMagicSparkles);
        CkGui.AttachToolTip("The moodle status/preset applied.");

        CkGui.TextFrameAlignedInline("Applies Moodle");

        ImUtf8.SameLineInner();
        var curType = action.MoodleItem is MoodlePreset p ? MoodleType.Preset : MoodleType.Status;
        if (CkGuiUtils.EnumCombo("##M_Type", 40f, curType, out var newVal))
        {
            if (curType != newVal)
                action.MoodleItem = newVal is MoodleType.Preset ? new MoodlePreset() : new Moodle();
        }

        CkGui.TextFrameAlignedInline("item");

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
                ImGui.SameLine();
                _moodleDrawer.DrawStatusInfos([
                    ..ipc.StatusList.Where(m => preset.StatusIds.Contains(m.GUID)).ToList()
                ], MoodleDrawer.IconSizeFramed);
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
                _moodleDrawer.DrawStatusInfos([match], MoodleDrawer.IconSizeFramed);
            }
        }
    }

    public void DrawShockAction(PiShockAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.Bolt);
        CkGui.AttachToolTip("The shock collar instruction executed when the input command is detected.");

        CkGui.ColorTextFrameAlignedInline(action.ShockInstruction.OpCode.ToString(), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("for");
        CkGui.ColorTextFrameAlignedInline(action.ShockInstruction.Duration.ToString(), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("ms");

        // display extra information if a vibrator or shock.
        if (action.ShockInstruction.OpCode is not ShockMode.Beep)
        {
            CkGui.TextFrameAlignedInline("at");
            CkGui.ColorTextFrameAlignedInline(action.ShockInstruction.Intensity.ToString(), ImGuiColors.TankBlue);
            CkGui.TextFrameAlignedInline("intensity");
        }
    }

    public void DrawShockActionEdit(PiShockAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.Bolt);
        CkGui.AttachToolTip("The shock collar instruction executed when the input command is detected.");

        CkGui.TextFrameAlignedInline("Instructs:");

        ImGui.SameLine();
        if (CkGuiUtils.EnumCombo("##OpCodeEdit", 60f, action.ShockInstruction.OpCode, out var mode))
            action.ShockInstruction.OpCode = mode;

        CkGui.TextFrameAlignedInline("for");

        // We love wacky pi-shock API YIPPEEEEE *dies*
        var durationRef = action.ShockInstruction.GetDurationFloat();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(85f);
        if (ImGui.SliderFloat("##ShockDur", ref durationRef, 0.016f, 15f))
            action.ShockInstruction.SetDuration(durationRef);
        CkGui.AttachToolTip("The duration of the shock in seconds or milliseconds.");

        // display extra information if a vibrator or shock.
        if (action.ShockInstruction.OpCode is not ShockMode.Beep)
        {
            CkGui.TextFrameAlignedInline("at");

            var intensity = action.ShockInstruction.Intensity;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(85f);
            if (ImGui.SliderInt("##ShockIntensity", ref intensity, 0, 100))
                action.ShockInstruction.Intensity = intensity;

            CkGui.TextFrameAlignedInline("intensity.");
        }
    }

    public void DrawSexToyAction(SexToyAction action)
    {
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(FAI.WaveSquare);
        CkGui.AttachToolTip("The action to be executed on the listed toys.");

        // in theory this listing could get pretty expansive so for now just list a summary.
        CkGui.TextFrameAlignedInline("After");
        CkGui.ColorTextFrameAlignedInline(action.StartAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);
        CkGui.TextFrameAlignedInline("toys to vibrate for");
        CkGui.ColorTextFrameAlignedInline(action.EndAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);
    }

    public void DrawSexToyActionEdit(SexToyAction action)
    {
        using var _ = ImRaii.Group();
        CkGui.FramedIconText(FAI.WaveSquare);
        CkGui.AttachToolTip("The action to be executed on the listed toys.");

        // in theory this listing could get pretty expansive so for now just list a summary.
        CkGui.TextFrameAlignedInline("Actives devices for");

        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(85f);
        var valueE = (float)action.EndAfter.TotalSeconds;
        if (ImGui.SliderFloat("##EndAfter", ref valueE, 0.016f, 15f))
            action.EndAfter = TimeSpan.FromSeconds(valueE);
        CkGui.AttachToolTip("The time to wait before stopping the toy actions.");

        CkGui.TextFrameAlignedInline("seconds,");

        ImGuiHelpers.ScaledDummy(ImGui.GetFrameHeight());
        CkGui.TextFrameAlignedInline("after a delay of");

        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(85f);
        var valueS = (float)action.StartAfter.TotalSeconds;
        if (ImGui.SliderFloat("##StartAfter", ref valueS, 0.016f, 15f))
            action.StartAfter = TimeSpan.FromSeconds(valueS);
        CkGui.AttachToolTip("The time to wait before starting the toy actions.");
    }
}
