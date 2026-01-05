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

public enum DrawAliasTriggerButtonAction
{
    NoAction,
    Revert,
    SaveChanges,
    Delete
}

// Scoped, sealed class to draw the editor and display components of aliasItems.
public sealed class AliasItemDrawer
{
    private readonly ILogger<AliasItemDrawer> _logger;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly PuppeteerManager _manager;
    private readonly MoodleDrawer _moodleDrawer;

    private static readonly string[] ThreeLayerNames = ["Layer 1", "Layer 2", "Layer 3", "Any Layer"];
    private static readonly string[] FiveLayerNames = ["Layer 1", "Layer 2", "Layer 3", "Layer 4", "Layer 5", "Any Layer"];
    private HashSet<Guid> ExpandedTriggers = new HashSet<Guid>();

    private RestrictionCombo _restrictionCombo { get; init; }
    private RestraintCombo _restraintCombo { get; init; }
    private MoodleStatusCombo _statusCombo { get; init; }
    private MoodlePresetCombo _presetCombo { get; init; }

    public AliasItemDrawer(
        ILogger<AliasItemDrawer> logger,
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
        _moodleDrawer = moodleDrawer;

        _restrictionCombo = new RestrictionCombo(logger, mediator, favorites, () => [
            ..restrictions.Storage.OrderByDescending(p => favorites.Restrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        _restraintCombo = new RestraintCombo(logger, mediator, favorites, () => [
            ..restraints.Storage.OrderByDescending(p => favorites.Restraints.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        _statusCombo = new MoodleStatusCombo(logger, 1.15f);
        _presetCombo = new MoodlePresetCombo(logger, 1.15f);
    }

    public void DrawAchievementList(AchievementModuleKind type, Vector2 region)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f);
        using var col = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink)
            .Push(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        var unlocks = ClientAchievements.GetByModule(type);
        if (!unlocks.Any())
            return;

        // filter down the unlocks to searchable results.
        var filteredUnlocks = unlocks
            .Where(goal => goal.Title.Contains(string.Empty, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var size = new Vector2(ImGui.GetContentRegionAvail().X, 96f.AddWinPadY() + ImGui.GetStyle().CellPadding.Y * 2);
        foreach (var achievement in filteredUnlocks.ToList())
            DrawAchievementProgressBox(achievement, size);
    }

    public void DrawAchievementProgressBox(AchievementBase achievementItem, Vector2 size)
    {
        var imageTabWidth = 96 + ImGui.GetStyle().ItemSpacing.X * 2;
        using var _ = CkRaii.FramedChild($"Achievement-{achievementItem.Title}", size, new Vector4(0.25f, 0.2f, 0.2f, 0.4f).ToUint(), CkColor.VibrantPink.Uint(), 5f, 1f);
        using var t = ImRaii.Table($"AchievementTable {achievementItem.Title}", 2, ImGuiTableFlags.RowBg);
        if (!t) return;

        ImGui.TableSetupColumn("##AchievementText", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##AchievementIcon", ImGuiTableColumnFlags.WidthFixed, 96);
        // draw the information about the achievement and its progress bar within the first section.
        // maybe the progress bar could span the bottom if icon image size is too much of a concern idk.
        ImGui.TableNextColumn();
        using (ImRaii.Group())
        {
            var progress = achievementItem.CurrentProgress();
            var icon = achievementItem.IsCompleted ? FAI.Trophy : (progress != 0 ? FAI.Stopwatch : FAI.Trophy);
            var color = achievementItem.IsCompleted ? ImGuiColors.ParsedGold : (progress != 0 ? ImGuiColors.DalamudGrey : ImGuiColors.DalamudGrey3);
            var tooltip = achievementItem.IsCompleted ? "Achievement Completed!" : (progress != 0 ? "Achievement in Progress" : "Achievement Not Started");
            CkGui.IconTextAligned(icon, color);
            CkGui.AttachToolTip(tooltip);

            // beside it, draw out the achievement's Title in white text.
            using (ImRaii.PushFont(UiBuilder.MonoFont))
                CkGui.ColorTextFrameAlignedInline(achievementItem.Title, ImGuiColors.ParsedGold);
            // Split between the title and description
            ImGui.Separator();

            CkGui.IconTextAligned(FAI.InfoCircle, ImGuiColors.TankBlue);

            ImUtf8.SameLineInner();
            ImGui.AlignTextToFramePadding();
            var descText = achievementItem.IsSecretAchievement ? "????" : achievementItem.Description;
            CkGui.TextWrapped(descText);
            if (achievementItem.IsSecretAchievement)
                CkGui.AttachToolTip("Explore GagSpeak's Features or work together with others to uncover how you obtain this Achievement!)");
        }
        // underneath this, we should draw the current progress towards the goal.
        DrawProgressForAchievement(achievementItem);
        if (ImGui.IsItemHovered() && achievementItem is DurationAchievement)
            CkGui.AttachToolTip((achievementItem as DurationAchievement)?.GetActiveItemProgressString() ?? "NO PROGRESS");

        // draw the text in the second column.
        ImGui.TableNextColumn();
        // Ensure its a valid texture wrap
        if (CosmeticService.CoreTextures.Cache[CoreTexture.Icon256Bg] is { } wrap)
            ImGui.Image(wrap.Handle, new(96, 96));
    }


    // Referenced draw-list structure for progress bar from DevUI Bar's and Mare's Progress bar.
    // https://github.com/Penumbra-Sync/client/blob/e35ed1b5297437cbcaa3dca5f5a089033c996020/MareSynchronos/UI/DownloadUi.cs#L138

    private const int Transparency = 100;
    private const int ProgressBarBorder = 1;
    private void DrawProgressForAchievement(AchievementBase achievement)
    {
        var region = ImGui.GetContentRegionAvail(); // content region
        var padding = ImGui.GetStyle().FramePadding; // padding

        // grab progress and milestone to help with drawing the progress bar.
        var completionPercentage = achievement.CurrentProgressPercentage();
        if (completionPercentage > 1f) completionPercentage = 1f;

        // Grab the displaytext for the progress bar.
        var progressBarString = achievement.ProgressString();
        var progressBarStringTextSize = ImGui.CalcTextSize(progressBarString);

        // move the cursor screen pos to the bottom of the content region - the progress bar height.
        ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().X + ImGuiHelpers.GlobalScale, ImGui.GetCursorScreenPos().Y + region.Y - ((int)progressBarStringTextSize.Y + 5)));

        // grab the current cursor screen pos.
        var pos = ImGui.GetCursorScreenPos();

        // define the progress bar height and width for the windows drawlist.
        var progressHeight = (int)progressBarStringTextSize.Y + 2;
        var progressWidth = (int)(region.X - padding.X);

        // mark the starting position of our progress bar in the drawlist.
        var progressBarDrawStart = pos;

        // mark the ending position of the progress bar in the drawlist.
        var progressBarDrawEnd = new Vector2(pos.X + progressWidth, pos.Y + progressHeight);

        // grab the WINDOW draw list
        var drawList = ImGui.GetWindowDrawList();

        // Parsed Pink == (225,104,168,255)


        drawList.AddRectFilled( // The Outer Border of the progress bar
            progressBarDrawStart with { X = progressBarDrawStart.X - ProgressBarBorder - 1, Y = progressBarDrawStart.Y - ProgressBarBorder - 1 },
            progressBarDrawEnd with { X = progressBarDrawEnd.X + ProgressBarBorder + 1, Y = progressBarDrawEnd.Y + ProgressBarBorder + 1 },
            CkGui.Color(0, 0, 0, Transparency),
            25f,
            ImDrawFlags.RoundCornersAll);

        drawList.AddRectFilled( // The inner Border of the progress bar
            progressBarDrawStart with { X = progressBarDrawStart.X - ProgressBarBorder, Y = progressBarDrawStart.Y - ProgressBarBorder },
            progressBarDrawEnd with { X = progressBarDrawEnd.X + ProgressBarBorder, Y = progressBarDrawEnd.Y + ProgressBarBorder },
            CkGui.Color(220, 220, 220, Transparency),
            25f,
            ImDrawFlags.RoundCornersAll);

        drawList.AddRectFilled( // The progress bar background
            progressBarDrawStart,
            progressBarDrawEnd,
            CkGui.Color(0, 0, 0, Transparency),
            25f,
            ImDrawFlags.RoundCornersAll);

        // Do not draw the progress bar fill if it is less than .02% of the progress bar width.
        if (completionPercentage >= 0.025)
        {
            drawList.AddRectFilled( // The progress bar fill
                progressBarDrawStart,
                progressBarDrawEnd with { X = progressBarDrawStart.X + (float)(completionPercentage * (float)progressWidth) },
                CkGui.Color(225, 104, 168, 255),
                45f,
                ImDrawFlags.RoundCornersAll);
        }

        drawList.OutlinedFont(progressBarString,
            pos with { X = pos.X + ((progressWidth - progressBarStringTextSize.X) / 2f) - 1, Y = pos.Y + ((progressHeight - progressBarStringTextSize.Y) / 2f) - 1 },
            CkGui.Color(255, 255, 255, 255),
            CkGui.Color(53, 24, 39, 255),
            1);
    }

    public void DrawAliasTrigger(AliasTrigger aliasItem, MoodleData ipc, out bool startEditing, bool canEdit = true)
    {
        startEditing = false;
        var isContained = ExpandedTriggers.Contains(aliasItem.Identifier);
        var shownActions = isContained ? aliasItem.Actions.Count() : 1;
        var pos = ImGui.GetCursorScreenPos();
        var childH = (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y) * (shownActions + 1);

        using var _ = CkRaii.FramedChildPaddedW($"AliasItem{aliasItem.Identifier}", ImGui.GetContentRegionAvail().X, childH, new Vector4(0.25f, 0.2f, 0.2f, 0.4f).ToUint(), 0);

        using (ImRaii.Group())
        {
            var rightButtonWidth = canEdit
                ? CkGui.IconButtonSize(FAI.Edit).X * 2 + ImGui.GetStyle().ItemInnerSpacing.X
                : CkGui.IconButtonSize(FAI.Edit).X;

            CkGui.BooleanToColoredIcon(aliasItem.Enabled, false);
            if (ImGui.IsItemClicked() && canEdit)
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

            if (canEdit)
            {
                if (CkGui.IconButton(FAI.Edit, inPopup: true))
                    startEditing = true;

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

    public void DrawAliasTriggerEditor(IEnumerable<InvokableActionType> selectableTypes, ref InvokableActionType selected, out DrawAliasTriggerButtonAction result)
    {
        result = DrawAliasTriggerButtonAction.NoAction;
        if (_manager.ItemInEditor is not { } aliasItem)
            return;

        var pos = ImGui.GetCursorScreenPos();
        var childH = aliasItem.Actions.Any(x => x.ActionType == InvokableActionType.SexToy)
            ? (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y) * (aliasItem.Actions.Count + 3)
            : (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y) * (aliasItem.Actions.Count + 2);

        using var _ = CkRaii.FramedChildPaddedW($"AliasItem{aliasItem.Identifier}", ImGui.GetContentRegionAvail().X, childH,
            new Vector4(0.25f, 0.2f, 0.2f, 0.4f).ToUint(), 0, wFlags: ImGuiWindowFlags.NoScrollbar);

        using (ImRaii.Group())
        {
            var comboWidth = 100f;
            var rightWidth = (CkGui.IconButtonSize(FAI.Save).X + ImGui.GetStyle().ItemInnerSpacing.X) * 4 + comboWidth;

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
            ImUtf8.SameLineInner();
            CkGui.AttachToolTip("Selects a new output action kind to add to this Alias Item.");
            if (CkGui.IconButton(FAI.FileCircleMinus, inPopup: true))
            {
                result = DrawAliasTriggerButtonAction.Revert;
            }
            CkGui.AttachToolTip("Click to cancel changes to this Alias Item.--SEP-- This will also close the editor");
            ImUtf8.SameLineInner();
            if (CkGui.IconButton(FAI.Save, inPopup: true))
            {
                result = DrawAliasTriggerButtonAction.SaveChanges;
            }
            CkGui.AttachToolTip("Click to save changes to this Alias Item.--SEP-- This will also close the editor.");

            ImUtf8.SameLineInner();
            if (CkGui.IconButton(FAI.Trash, inPopup: true))
            {
                result = DrawAliasTriggerButtonAction.Delete;
            }
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
                if (!action.RestrictionId.Equals(_restraintCombo.Current?.Identifier))
                    action.RestrictionId = _restraintCombo.Current?.Identifier ?? Guid.Empty;
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                action.RestrictionId = Guid.Empty;
        }
    }

    public void DrawMoodleAction(MoodleAction action, MoodleData ipc)
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

    public void DrawMoodleActionEdit(MoodleAction action, MoodleData ipc)
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
