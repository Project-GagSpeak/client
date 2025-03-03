using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Visual;
using GagSpeak.UI.Handlers;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.UI.Puppeteer;

public class PuppeteerComponents
{
    private readonly ILogger<PuppeteerComponents> _logger;
    private readonly RestrictionManager _restrictions;
    private readonly RestrictionManager _restraints;
    private readonly VisualApplierMoodles _moodles;
    private readonly PuppeteerManager _manager;

    private readonly UserPairListHandler _pairList;
    public PuppeteerComponents(ILogger<PuppeteerComponents> logger, PuppeteerManager manager,
        RestrictionManager restraints, RestrictionManager restrictions, VisualApplierMoodles moodles,
        UserPairListHandler pairList)
    {
        _logger = logger;
        _restraints = restraints;
        _restrictions = restrictions;
        _moodles = moodles;
        _manager = manager;
        _pairList = pairList;
    }

    /// <summary> An internal cache used for expanded alias items to hold the combo drawers of all respective item types. </summary>
    /// <remarks> When an alias item is closed in the editor, all the combos are garbage collected as the object is destroyed. </remarks>
    public sealed class ExpandedAliasCache
    {
        public          InvokableActionType? SelectedActionType;
        public readonly RestrictionCombo    RestrictionCombo;
        public readonly RestraintCombo      RestraintCombo;
        public readonly MoodleStatusCombo   StatusCombo;
        public readonly MoodlePresetCombo   PresetCombo;

        public ExpandedAliasCache(AliasTrigger aliasItem, RestrictionCombo restrictionCombo,
            RestraintCombo restraintCombo,MoodleStatusCombo statusCombo, MoodlePresetCombo presetCombo)
        {
            SelectedActionType = aliasItem.Executions.Any() ? aliasItem.Executions.First().Key : null;
            RestrictionCombo = restrictionCombo;
            RestraintCombo = restraintCombo;
            StatusCombo = statusCombo;
            PresetCombo = presetCombo;
        }
    }

    public void DrawListenerClientGroup(bool isEditing, Action<bool>? onSitsChange = null, Action<bool>? onMotionChange = null,
        Action<bool>? onAliasChange = null, Action<bool>? onAllChange = null, Action<bool>? onEditToggle = null)
    {
        if(_pairList.SelectedPair is not { } pair)
            return;

        using var group = ImRaii.Group();

        ImGui.AlignTextToFramePadding();
        CkGui.ColorText("Listening To", ImGuiColors.ParsedPink);

        var remainingWidth = CkGui.IconButtonSize(FontAwesomeIcon.Save).X * 5 + ImGui.GetStyle().ItemInnerSpacing.X * 4;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - remainingWidth);

        // so they let sits?
        using (ImRaii.PushColor(ImGuiCol.Text, pair.OwnPerms.PuppetPerms.HasFlag(PuppetPerms.Sit) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            if (CkGui.IconButton(FontAwesomeIcon.Chair, inPopup: true))
                onSitsChange?.Invoke(!pair.OwnPerms.PuppetPerms.HasFlag(PuppetPerms.Sit));
        CkGui.AttachToolTip("Allows " + pair.GetNickAliasOrUid() + " to make you perform /sit and /groundsit (cycle pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, pair.OwnPerms.PuppetPerms.HasFlag(PuppetPerms.Emotes) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            if (CkGui.IconButton(FontAwesomeIcon.Walking, inPopup: true))
                onMotionChange?.Invoke(!pair.OwnPerms.PuppetPerms.HasFlag(PuppetPerms.Emotes));
        CkGui.AttachToolTip("Allows " + pair.GetNickAliasOrUid() + " to make you perform emotes and expressions (cycle Pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, pair.OwnPerms.PuppetPerms.HasFlag(PuppetPerms.Alias) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            if (CkGui.IconButton(FontAwesomeIcon.Scroll, inPopup: true))
                onAliasChange?.Invoke(!pair.OwnPerms.PuppetPerms.HasFlag(PuppetPerms.Alias));
        CkGui.AttachToolTip("Allows " + pair.GetNickAliasOrUid() + " to execute any of your Pair Alias Triggers.");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, pair.OwnPerms.PuppetPerms.HasFlag(PuppetPerms.All) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            if (CkGui.IconButton(FontAwesomeIcon.CheckDouble, inPopup: true))
                onAllChange?.Invoke(!pair.OwnPerms.PuppetPerms.HasFlag(PuppetPerms.All));
        CkGui.AttachToolTip("Allows " + pair.GetNickAliasOrUid() + " to make you perform any command.");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, isEditing ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudGrey))
            if (CkGui.IconButton(isEditing ? FontAwesomeIcon.Save : FontAwesomeIcon.Edit, inPopup: true))
                onEditToggle?.Invoke(!isEditing);
        CkGui.AttachToolTip(isEditing ? "Stop Editing your TriggerPhrase Info." : "Modify Your TriggerPhrase Info");
    }

    public void DrawListenerPairGroup(Action? onSendName = null)
    {
        if (_pairList.SelectedPair is not { } pair)
            return;

        bool pairHasName = pair.LastAliasData.HasNameStored;
        using var group = ImRaii.Group();

        // display name, then display the downloads and likes on the other side.
        var ButtonWidth = CkGui.IconButtonSize(FontAwesomeIcon.Save).X * 5 - ImGui.GetStyle().ItemInnerSpacing.X * 4;
        using (ImRaii.PushColor(ImGuiCol.Text, pairHasName ? ImGuiColors.DalamudGrey : ImGuiColors.ParsedGold))
        {
            var isDisabled = !pair.IsOnline || (pairHasName && !KeyMonitor.ShiftPressed());
            if (CkGui.IconTextButton(FontAwesomeIcon.CloudUploadAlt, "Send Name", ImGui.GetContentRegionAvail().X - ButtonWidth, true, isDisabled))
                onSendName?.Invoke();
        }
        CkGui.AttachToolTip("Send this Pair your In-Game Character Name.\nThis allows them to listen to you for triggers!" +
        "--SEP--Hold SHIFT to Resend Name.");

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ButtonWidth);
        using (ImRaii.Disabled())
        using (ImRaii.PushColor(ImGuiCol.Text, pair.PairPerms.PuppetPerms.HasFlag(PuppetPerms.Sit) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            CkGui.IconButton(FontAwesomeIcon.Chair, inPopup: true);
        CkGui.AttachToolTip(pair.GetNickAliasOrUid() + " allows you to make them perform /sit and /groundsit (cycle pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled())
        using (ImRaii.PushColor(ImGuiCol.Text, pair.PairPerms.PuppetPerms.HasFlag(PuppetPerms.Emotes) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            CkGui.IconButton(FontAwesomeIcon.Walking, inPopup: true);
        CkGui.AttachToolTip(pair.GetNickAliasOrUid() + " allows you to make them perform emotes and expressions (cycle Pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled())
        using (ImRaii.PushColor(ImGuiCol.Text, pair.PairPerms.PuppetPerms.HasFlag(PuppetPerms.Alias) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            CkGui.IconButton(FontAwesomeIcon.Scroll, inPopup: true);
        CkGui.AttachToolTip(pair.GetNickAliasOrUid() + " allows you to execute any of their Alias Triggers.");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled())
        using (ImRaii.PushColor(ImGuiCol.Text, pair.PairPerms.PuppetPerms.HasFlag(PuppetPerms.All) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            CkGui.IconButton(FontAwesomeIcon.CheckDouble, inPopup: true);
        CkGui.AttachToolTip(pair.GetNickAliasOrUid() + " allows you to make them perform any command.");
    }

    public void DrawEditingTriggersWindow(ref string tempTriggers, ref string tempSartChar, ref string tempEndChar)
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.AlignTextToFramePadding();
        CkGui.ColorText("Your Trigger Phrases", ImGuiColors.ParsedPink);

        ImGui.Spacing();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputTextWithHint("##TriggerPhrase", "Leave Blank for none...", ref tempTriggers, 64);
        CkGui.AttachToolTip("You can create multiple trigger phrases by placing a | between phrases.");

        using (ImRaii.Group())
        {
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            CkGui.ColorText("Custom Brackets:", ImGuiColors.ParsedPink);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(20 * ImGuiHelpers.GlobalScale);
            ImGui.InputText("##sStarChar", ref tempSartChar, 1);
            if (ImGui.IsItemDeactivatedAfterEdit())
                if (string.IsNullOrWhiteSpace(tempSartChar)) tempSartChar = "(";
            CkGui.AttachToolTip($"Custom Start Character that replaces the left enclosing bracket." +
                Environment.NewLine + "Replaces the [ ( ] in: [ TriggerPhrase (commandToExecute) ]");

            ImUtf8.SameLineInner();
            CkGui.IconText(FontAwesomeIcon.GripLinesVertical, ImGuiColors.ParsedPink);
            ImUtf8.SameLineInner();

            ImGui.SetNextItemWidth(20 * ImGuiHelpers.GlobalScale);
            ImGui.InputText("##sEndChar", ref tempEndChar, 1);
            if (ImGui.IsItemDeactivatedAfterEdit())
                if (string.IsNullOrWhiteSpace(tempEndChar)) tempEndChar = ")";
            CkGui.AttachToolTip($"Custom End Character that replaces the right enclosing bracket." +
                Environment.NewLine + "Replaces the [ ) ] in Ex: [ TriggerPhrase (commandToExecute) ]");
        }
    }

    public void DrawTriggersWindow(string triggerPhrases, string startChar, string endChar)
    {
        if(_pairList.SelectedPair is not { } pair)
            return;

        var TriggerPhrase = triggerPhrases;
        var triggers = TriggerPhrase.Split('|');

        using (ImRaii.Group())
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.AlignTextToFramePadding();
            CkGui.ColorText("Your Trigger Phrases", ImGuiColors.ParsedPink);

            if (!triggers.Any() || triggers[0].IsNullOrEmpty())
            {
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted("No Trigger Phrase Set.");
            }

            foreach (var trigger in triggers)
            {
                if (trigger.IsNullOrEmpty())
                    continue;

                CkGui.IconText(FontAwesomeIcon.QuoteLeft, ImGuiColors.ParsedPink);

                ImUtf8.SameLineInner();
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(trigger);

                ImUtf8.SameLineInner();
                CkGui.IconText(FontAwesomeIcon.QuoteRight, ImGuiColors.ParsedPink);
            }
        }

        using (ImRaii.Group())
        {
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            CkGui.ColorText("Custom Brackets:", ImGuiColors.ParsedPink);
            ImGui.SameLine();
            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(startChar);
            CkGui.AttachToolTip($"Custom Start Character that replaces the left enclosing bracket." +
                Environment.NewLine + "Replaces the [ ( ] in: [ TriggerPhrase (commandToExecute) ]");

            ImUtf8.SameLineInner();
            CkGui.IconText(FontAwesomeIcon.GripLinesVertical, ImGuiColors.ParsedPink);
            ImUtf8.SameLineInner();

            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(endChar);
            CkGui.AttachToolTip($"Custom End Character that replaces the right enclosing bracket." +
                Environment.NewLine + "Replaces the [ ) ] in Ex: [ TriggerPhrase (commandToExecute) ]");
        }

        if (triggerPhrases.IsNullOrEmpty())
            return;

        ImGui.Spacing();
        ImGui.Separator();

        var charaName = $"<YourNameWorld> ";
        CkGui.ColorText("Example Usage:", ImGuiColors.ParsedPink);
        ImGui.TextWrapped(charaName + triggers[0] + " " + pair.OwnPerms.StartChar + " glamour apply Hogtied | p | [me] " + pair.PairPerms.EndChar);

    }

    public void DrawAliasItemBox(string id, AliasTrigger aliasItem)
    {
        /*// if the id is not present in the dictionary, add it.
        if (!ExpandedAliasItems.ContainsKey(aliasItem.Identifier.ToString()))
            ExpandedAliasItems.Add(id, false);

        var storedOutput = aliasItem.Executions.Keys;
        var storedOutputSize = ExpandedAliasItems[id] ? (storedOutput.Any() ? storedOutput.Count() : 1) : (storedOutput.Any() ? 1 : 0);
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var winFramePadHeight = ImGui.GetStyle().WindowPadding.Y * 2 + ImGui.GetStyle().FramePadding.Y * 2;
        float height = winFramePadHeight + (ImGui.GetFrameHeight() * (storedOutputSize + 2)) + (itemSpacing.Y * (storedOutputSize + 1));

        using var child = ImRaii.Child("##PatternResult_" + aliasItem.Identifier, new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.NoScrollbar);
        if (!child) return;

        using (ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            CkGui.BooleanToColoredIcon(aliasItem.Enabled, false);
            CkGui.AttachToolTip("If the Alias is currently Enabled or Disabled." +
                "--SEP--Click this while in edit mode to toggle the state!");

            // beside this, draw out the alias items label name.
            ImGui.SameLine();
            string text = aliasItem.Label.IsNullOrEmpty() ? "<No Alias Name Set!>" : aliasItem.Label;
            ImGui.TextUnformatted(text);

            CkGui.AttachToolTip("The Alias Label given to help with searching and organization.");

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
            if (CkGui.IconButton(ExpandedAliasItems[id] ? FontAwesomeIcon.ChevronUp : FontAwesomeIcon.ChevronDown, inPopup: true))
                ExpandedAliasItems[id] = !ExpandedAliasItems[id];
            CkGui.AttachToolTip(ExpandedAliasItems[id] ? "Collapse the Alias Item." : "Expand the Alias Item.");

            // cast a seperator Line here.
            ImGui.Separator();


            ImGui.AlignTextToFramePadding();
            CkGui.IconText(FontAwesomeIcon.Eye);
            CkGui.AttachToolTip("The text to scan for (Input String)");

            ImGui.SameLine();
            using (ImRaii.Disabled())
            {
                var txt = aliasItem.InputCommand.IsNullOrEmpty() ? "<Undefined Input!>" : aliasItem.InputCommand;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.InputText("##InputPreview_" + aliasItem.Identifier, ref txt, 128, ImGuiInputTextFlags.ReadOnly);
            }
            CkGui.AttachToolTip("The text to scan for (Input String)");


            var totalDisplayed = 0;


            // Handle Text
            if (aliasItem.Executions.TryGetValue(InvokableActionType.TextOutput, out var act) && act is TextAction textAction)
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    CkGui.IconText(FontAwesomeIcon.Font);

                    ImGui.SameLine();
                    var txt = textAction.OutputCommand.IsNullOrEmpty() ? "<Undefined Output!>" : textAction.OutputCommand;
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    CkGui.ColorText("/" + txt, ImGuiColors.TankBlue);
                }
                CkGui.AttachToolTip("What is fired as a command." +
                    "--SEP--Do not include the '/' in your output.");

                // End viewing the rest if we meet the condition here.
                if (!ExpandedAliasItems[id] && totalDisplayed >= storedOutputSize)
                    return;
                totalDisplayed++;
            }

            // Handle Gag
            if (aliasItem.Executions.TryGetValue(InvokableActionType.Gag, out var actGag) && actGag is GagAction gagAction)
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    CkGui.IconText(FontAwesomeIcon.Comment);
                    CkGui.AttachToolTip("The Following Gag State that will be applied to the Kinkster.");

                    // the following gag
                    ImGui.SameLine();
                    ImGui.TextUnformatted("The");
                    ImGui.SameLine();
                    CkGui.ColorText(gagAction.GagType.ToString(), ImGuiColors.TankBlue);
                    CkGui.AttachToolTip("The Gag to perform the new state on." +
                        "--SEP--If NONE is selected for the gag, it will perform the action on any Gag." +
                        "--SEP--Target Priority is always [Outer > Central > Inner]");

                    ImGui.SameLine();
                    ImGui.TextUnformatted("gets set to");
                    ImGui.SameLine();
                    CkGui.ColorText(gagAction.NewState.ToString(), ImGuiColors.TankBlue);
                    CkGui.AttachToolTip("The new state set on the targetted gag.");
                }
                // End viewing the rest if we meet the condition here.
                if (!ExpandedAliasItems[id] && totalDisplayed >= storedOutputSize)
                    return;
                totalDisplayed++;
            }
            // Handle Restriction
            if (aliasItem.Executions.TryGetValue(InvokableActionType.Restriction, out var actRes) && actRes is RestrictionAction bindAction)
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    CkGui.IconText(FontAwesomeIcon.ToiletPortable);
                    CkGui.AttachToolTip("The new state of the restraint set, and which is being applied.");

                    // if applying, list both, otherwise, list only newState.
                    if (bindAction.NewState is NewState.Enabled)
                    {
                        ImGui.SameLine();
                        ImGui.TextUnformatted("Applies Restriction:");
                        ImGui.SameLine();
                        var set = sets.FirstOrDefault(x => x.Id == bindAction.RestrictionId);
                        var setName = set is not null ? set.Label : "Set No Longer Exists";
                        CkGui.ColorText(setName, set is not null ? ImGuiColors.TankBlue : ImGuiColors.DalamudRed);
                        // display the set on hover if valid.
                        // if (set is not null) _setPreview.DrawLightRestrictionOnHover(set);
                    }
                    // any other type, list here.
                    else
                    {
                        ImGui.SameLine();
                        CkGui.ColorText("Disables", ImGuiColors.TankBlue);
                        CkGui.AttachToolTip("The new state applied to the restraint set.");

                        ImGui.SameLine();
                        ImGui.TextUnformatted("On Active Restriction Set if allowed.");
                        CkGui.AttachToolTip("Will not interact with restrainted locked with types we cannot unlock.");
                    }
                }
                // End viewing the rest if we meet the condition here.
                if (!ExpandedAliasItems[id] && totalDisplayed >= storedOutputSize)
                    return;
                totalDisplayed++;
            }
            // Handle Moodles
            if (aliasItem.Executions.TryGetValue(InvokableActionType.Moodle, out var actMoodle) && actMoodle is MoodleAction statusAction)
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    CkGui.IconText(FontAwesomeIcon.WandMagicSparkles);
                    CkGui.AttachToolTip("Displays the Identity of the moodle status/preset applied.");

                    // the following gag
                    ImGui.SameLine();
                    ImGui.TextUnformatted("Type is");
                    ImGui.SameLine();
                    CkGui.ColorText(statusAction.MoodleItem is MoodlePresetApi ? "Preset" : "Status", ImGuiColors.TankBlue);

                    ImGui.SameLine();
                    var moodleInfo = "DummyText";
                    ImGui.TextUnformatted("and applies");
                    ImGui.SameLine();
                    CkGui.ColorText(moodleInfo, ImGuiColors.TankBlue);
                }
                // End viewing the rest if we meet the condition here.
                if (!ExpandedAliasItems[id] && totalDisplayed >= storedOutputSize)
                    return;
                totalDisplayed++;
            }
            // Handle PiShock
            if (aliasItem.Executions.TryGetValue(InvokableActionType.ShockCollar, out var actShock) && actShock is PiShockAction shockAction)
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    CkGui.IconText(FontAwesomeIcon.Bolt);
                    CkGui.AttachToolTip("The shock collar instruction executed when the input command is detected.");

                    ImGui.SameLine();
                    ImGui.TextUnformatted("Instructs:");
                    ImGui.SameLine();
                    CkGui.ColorText(shockAction.ShockInstruction.OpCode.ToString(), ImGuiColors.TankBlue);

                    ImGui.SameLine();
                    ImGui.TextUnformatted("for");
                    ImGui.SameLine();
                    CkGui.ColorText(shockAction.ShockInstruction.Duration.ToString(), ImGuiColors.TankBlue);
                    ImGui.SameLine();
                    ImGui.TextUnformatted("milliseconds");

                    // display extra information if a vibrator or shock.
                    if (shockAction.ShockInstruction.OpCode is not ShockMode.Beep)
                    {
                        ImGui.SameLine();
                        ImGui.TextUnformatted("at");
                        ImGui.SameLine();
                        CkGui.ColorText(shockAction.ShockInstruction.Intensity.ToString(), ImGuiColors.TankBlue);
                        ImGui.SameLine();
                        ImGui.TextUnformatted("intensity");
                    }
                }
                // End viewing the rest if we meet the condition here.
                if (!ExpandedAliasItems[id] && totalDisplayed >= storedOutputSize)
                    return;
                totalDisplayed++;
            }
            // Handle SexToy
            if (aliasItem.Executions.TryGetValue(InvokableActionType.SexToy, out var actToy) && actToy is SexToyAction toyAction)
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    CkGui.IconText(FontAwesomeIcon.WaveSquare);
                    CkGui.AttachToolTip("The action to be executed on the listed toys.");

                    // in theory this listing could get pretty expansive so for now just list a summary.
                    ImGui.SameLine();
                    ImGui.TextUnformatted("After");
                    ImGui.SameLine();
                    CkGui.ColorText(toyAction.StartAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);

                    ImGui.SameLine();
                    ImGui.TextUnformatted(", actives");
                    ImGui.SameLine();
                    CkGui.ColorText(toyAction.DeviceActions.Count.ToString(), ImGuiColors.TankBlue);

                    ImGui.SameLine();
                    ImGui.TextUnformatted("toys to perform vibrations or patterns for the next");
                    ImGui.SameLine();
                    CkGui.ColorText(toyAction.EndAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);
                }
            }
            // Handle no outputs.
            if (!aliasItem.Executions.Any())
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    CkGui.IconText(FontAwesomeIcon.ExclamationTriangle, ImGuiColors.DalamudYellow);
                    ImGui.SameLine();
                    CkGui.ColorText("No Output Types Added! Output won't execute correctly!", ImGuiColors.DalamudYellow);
                }
            }
        }*/
    }

    /// <summary>
    /// Draws the editor for an alias item.
    /// </summary>
    /// <returns>True if an element was modified, false otherwise.</returns>
    public bool DrawAliasItemEditBox(AliasTrigger aliasItem, out bool shouldRemove)
    {
        shouldRemove = false;
        /*// Assume we are not removing, and have made no modifications.
        var wasModified = false;
        shouldRemove = false;

        // pre-calculations.
        var storedOutputTypes = aliasItem.Executions.Keys;
        var storedOutputSize = storedOutputTypes.Any() ? storedOutputTypes.Count() : 1;
        var deleteButtonSize = CkGui.IconButtonSize(FontAwesomeIcon.TrashAlt);
        var addTypeButtonSize = CkGui.IconButtonSize(FontAwesomeIcon.Plus);
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var comboSize = 125f;
        // merged pre-calcs
        var winFramePadHeight = ImGui.GetStyle().WindowPadding.Y * 2 + ImGui.GetStyle().FramePadding.Y * 2;
        var topRowButtonLength = deleteButtonSize.X + addTypeButtonSize.X + comboSize + itemSpacing.X;
        float height = winFramePadHeight + (ImGui.GetFrameHeight() * (storedOutputSize + 2)) + (itemSpacing.Y * (storedOutputSize + 1));

        using var child = ImRaii.Child("##AliasEditor_" + aliasItem.Identifier, new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow);
        if (!child) return wasModified;

        using var group = ImRaii.Group();

        ImGui.AlignTextToFramePadding();
        CkGui.BooleanToColoredIcon(aliasItem.Enabled, false);
        if (ImGui.IsItemClicked())
        {
            aliasItem.Enabled = !aliasItem.Enabled;
            wasModified = true;
        }
        CkGui.AttachToolTip("If the Alias is currently Enabled or Disabled." +
            "--SEP--Click this while in edit mode to toggle the state!");

        ImGui.SameLine();
        var tempName = aliasItem.Label;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - topRowButtonLength - itemSpacing.X * 4);
        if (ImGui.InputTextWithHint("##AliasName_" + aliasItem.Identifier, "Give Alias a Label...", ref tempName, 70))
        {
            aliasItem.Label = tempName;
            wasModified = true;
        }
        CkGui.AttachToolTip("The Alias Label given to help with searching and organization.");

        // scoot over to the far right where everything else is.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - topRowButtonLength);

        var selectedType = aliasItem.UnregisteredTypes().Contains(SelectedActionType) ? SelectedActionType : aliasItem.UnregisteredTypes().FirstOrDefault();
        if (ImGuiUtil.GenericEnumCombo("##AliasTypeCombo" + aliasItem.Identifier, comboSize, SelectedActionType, out InvokableActionType selectedAction,
            aliasItem.UnregisteredTypes(), (item) => item.ToString()))
        {
            SelectedActionType = selectedAction;
        }
        CkGui.AttachToolTip("Selects a new output action kind to add to this Alias Item.");

        ImUtf8.SameLineInner();
        if (CkGui.IconButton(FontAwesomeIcon.Plus, disabled: aliasItem.UnregisteredTypes().IsNullOrEmpty()))
        {
            aliasItem.AddActionForType(selectedType);
            wasModified = true;
        }
        CkGui.AttachToolTip("Adds the item from the dropdown to the list of active output types.--SEP--Only 1 of each type can be added maximum");

        ImUtf8.SameLineInner();
        if (CkGui.IconButton(FontAwesomeIcon.TrashAlt, disabled: !KeyMonitor.ShiftPressed()))
        {
            shouldRemove = true;
            return false;
        }
        CkGui.AttachToolTip("Deletes this Alias Item from the list.--SEP--Hold Shift to confirm deletion.");

        // ------------- SEPERATOR FOR INPUT COMMAND ------------ //
        ImGui.Separator();
        ImGui.AlignTextToFramePadding();
        CkGui.IconText(FontAwesomeIcon.Eye);
        CkGui.AttachToolTip("The text to scan for (Input String)");

        ImGui.SameLine();
        var inputCommand = aliasItem.InputCommand;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
        if (ImGui.InputTextWithHint("##InputCommand_" + aliasItem.Identifier, "Enter Text To Scan For...", ref inputCommand, 256))
        {
            aliasItem.InputCommand = inputCommand;
            wasModified = true;
        }
        CkGui.AttachToolTip("The text to scan for (Input String)");

        // Handle Text Output Display
        if (aliasItem.Executions.TryGetValue(InvokableActionType.TextOutput, out var act) && act is TextAction textAction)
        {
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FontAwesomeIcon.Font);
                CkGui.AttachToolTip("What text command you will output when the input text is read from this Kinkster.");
                using (ImRaii.PushFont(UiBuilder.MonoFont))
                {
                    var outputText = textAction.OutputCommand;
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
                    if (ImGui.InputText("##TextOutput_" + aliasItem.Identifier, ref outputText, 256))
                    {
                        textAction.OutputCommand = outputText;
                        wasModified = true;
                    }
                }
            }

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
            DrawRemoveIcon("remove-text-output-alias", () => aliasItem.Executions.Remove(InvokableActionType.TextOutput));
        }
        // Handle Gag Action Display
        if (aliasItem.Executions.TryGetValue(InvokableActionType.Gag, out var actGag) && actGag is GagAction gagAction)
        {
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FontAwesomeIcon.Comment);
                CkGui.AttachToolTip("The Following Gag State that will be applied to the Kinkster.");

                // Set the NewState to:
                ImGui.SameLine();
                ImGui.TextUnformatted("Invoke");
                ImGui.SameLine();
                if(ImGuiUtil.GenericEnumCombo("AliasGagState" + aliasItem.Identifier, 60f, gagAction.NewState, out NewState newState, [ NewState.Enabled, NewState.Locked, NewState.Disabled ])) 
                {
                    gagAction.NewState = newState;
                    wasModified = true;
                }
                CkGui.AttachToolTip("The new state set on the targeted gag.");

                ImGui.SameLine();
                ImGui.TextUnformatted("state for");
                ImGui.SameLine();
                if(ImGuiUtil.GenericEnumCombo("AliasGagType" + aliasItem.Identifier, 150f, gagAction.GagType, out GagType gagType))
                {
                    gagAction.GagType = gagType;
                    wasModified = true;
                }
                CkGui.AttachToolTip("Selecting NONE will serve as a wildcard during removal, otherwise, it will remove the first matching GagType.");
            }

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
            DrawRemoveIcon("remove-gag-output-alias", () => aliasItem.Executions.Remove(InvokableActionType.Gag));
        }
        // Handle Restriction Action Display.
        if (aliasItem.Executions.TryGetValue(InvokableActionType.Restriction, out var actRestriction) && actRestriction is RestrictionAction restrictionAction)
        {

        }
        // Handle Restriction Action Display
        if (aliasItem.Executions.TryGetValue(InvokableActionType.Restriction, out var actRestriction) && actRestriction is RestrictionAction restraintAction)
        {
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FontAwesomeIcon.ToiletPortable);
                CkGui.AttachToolTip("The new state of the restraint set, and which is being applied." +
                    "--SEP--Be Aware this may or may not be valid if view another Kinkster.");

                ImGui.SameLine();
                ImGui.TextUnformatted("Invoke");
                ImGui.SameLine();
                if (ImGuiUtil.GenericEnumCombo("RestrictionState" + aliasItem.Identifier, 60f, restraintAction.NewState, out NewState newState,
                    [NewState.Enabled, NewState.Locked, NewState.Disabled]))
                {
                    restraintAction.NewState = newState;
                    wasModified = true;
                }
                CkGui.AttachToolTip("The new state set on the specified restraint.");

                ImGui.SameLine();
                ImGui.TextUnformatted(restraintAction.NewState is NewState.Enabled ? "state on" : "state for the current set.");

                if (restraintAction.NewState is NewState.Enabled)
                {
                    ImGui.SameLine();
                    var defaultSet = sets.FirstOrDefault(x => x.Id == restraintAction.RestrictionId) ?? sets.FirstOrDefault();
                    CkGui.DrawCombo("AliasRestrictionSet" + aliasItem.Identifier, 150f, sets, (item) => item.Label, (i) =>
                    {
                        bindAction.RestrictionId = i?.Id ?? Guid.Empty;
                        wasModified = true;
                    }, defaultSet, false, ImGuiComboFlags.NoArrowButton);
                }

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
                DrawRemoveIcon("remove-restraint-output-alias", () => aliasItem.Executions.Remove(InvokableActionType.Restriction));
            }
        }
        // Handle Moodle Action Display
        if (aliasItem.Executions.TryGetValue(InvokableActionType.Moodle, out var actMoodle) && actMoodle is MoodleAction statusAction)
        {
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FontAwesomeIcon.WandMagicSparkles);
                CkGui.AttachToolTip("The moodle status/preset applied.");

                // the following gag
                ImGui.SameLine();
                ImGui.TextUnformatted("Apply");
                ImGui.SameLine();
                CkGui.DrawCombo("AliasMoodleType" + aliasItem.Identifier, 90f, Enum.GetValues<MoodleType>(), (item) => item.ToString(), (i) =>
                {
                    statusAction.Type = i;
                    wasModified = true;
                }, statusAction.Type, false, ImGuiComboFlags.NoArrowButton);

                if (statusAction.Type is MoodleType.Status)
                {
                    ImGui.SameLine();
                    ImGui.TextUnformatted("using");
                }
                else
                {
                    ImGui.SameLine();
                    ImGui.TextUnformatted("on preset");
                }
            }

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
            DrawRemoveIcon("remove-moodle-output-alias", () => aliasItem.Executions.Remove(InvokableActionType.Moodle));

        }
        // Handle Shock Collar Action Display
        if (aliasItem.Executions.TryGetValue(InvokableActionType.ShockCollar, out var actShock) && actShock is PiShockAction shockAction)
        {
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FontAwesomeIcon.Bolt);
                CkGui.AttachToolTip("The shock collar instruction executed when the input command is detected.");

                ImGui.SameLine();
                CkGui.DrawCombo("ShockOpCode" + aliasItem.Identifier, 60f, Enum.GetValues<ShockMode>(), (mode) => mode.ToString(), (i) =>
                {
                    shockAction.ShockInstruction.OpCode = i;
                    wasModified = true;
                }, shockAction.ShockInstruction.OpCode, false, ImGuiComboFlags.NoArrowButton);

                ImGui.SameLine();
                ImGui.TextUnformatted("for");
                ImGui.SameLine();
                var duration = shockAction.ShockInstruction.Duration;
                var timeSpanFormat = (duration > 15 && duration < 100)
                    ? TimeSpan.Zero // invalid range.
                    : (duration >= 100 && duration <= 15000)
                        ? TimeSpan.FromMilliseconds(duration) // convert to milliseconds
                        : TimeSpan.FromSeconds(duration); // convert to seconds
                var value = (float)timeSpanFormat.TotalSeconds + (float)timeSpanFormat.Milliseconds / 1000;

                ImGui.SetNextItemWidth(85f);
                if (ImGui.SliderFloat("##ShockDuration" + aliasItem.Identifier, ref value, 0.016f, 15f))
                {
                    int newMaxDuration;
                    if (value % 1 == 0 && value >= 1 && value <= 15) { newMaxDuration = (int)value; }
                    else { newMaxDuration = (int)(value * 1000); }
                    shockAction.ShockInstruction.Duration = newMaxDuration;
                    wasModified = true;
                }

                // display extra information if a vibrator or shock.
                if (shockAction.ShockInstruction.OpCode is not ShockMode.Beep)
                {
                    ImGui.SameLine();
                    ImGui.TextUnformatted("at");
                    ImGui.SameLine();
                    var intensity = shockAction.ShockInstruction.Intensity;
                    ImGui.SetNextItemWidth(85f);
                    if (ImGui.SliderInt("##ShockIntensity" + aliasItem.Identifier, ref intensity, 0, 100))
                    {
                        shockAction.ShockInstruction.Intensity = intensity;
                        wasModified = true;
                    }

                    ImGui.SameLine();
                    ImGui.TextUnformatted("intensity.");
                }
            }

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
            DrawRemoveIcon("remove-shock-output-alias", () => aliasItem.Executions.Remove(InvokableActionType.ShockCollar));
        }
        // Handle SexToy Action display
        if (aliasItem.Executions.TryGetValue(InvokableActionType.SexToy, out var actToy) && actToy is SexToyAction toyAction)
        {
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FontAwesomeIcon.WaveSquare);
                CkGui.AttachToolTip("The action to be executed on the listed toys.");

                // in theory this listing could get pretty expansive so for now just list a summary.
                ImGui.SameLine();
                ImGui.TextUnformatted("After");
                ImGui.SameLine();
                CkGui.ColorText(toyAction.StartAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);

                ImGui.SameLine();
                ImGui.TextUnformatted(", actives");
                ImGui.SameLine();
                CkGui.ColorText(toyAction.DeviceActions.Count.ToString(), ImGuiColors.TankBlue);

                ImGui.SameLine();
                ImGui.TextUnformatted("toys to perform vibrations or patterns for the next");
                ImGui.SameLine();
                CkGui.ColorText(toyAction.EndAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);
            }

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
            DrawRemoveIcon("remove-toy-output-alias", () => aliasItem.Executions.Remove(InvokableActionType.SexToy));
        }
        // Handle case where we had no ouput actions.
        if (!storedOutputTypes.Any())
        {
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FontAwesomeIcon.ExclamationTriangle, ImGuiColors.DalamudYellow);
                ImGui.SameLine();
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted("No Output types set for this Alias!");
            }
        }
*/
        return false;
    }

    private void DrawRemoveIcon(string id, Action onClick)
    {
        using var col = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
        if (CkGui.IconButton(FontAwesomeIcon.Minus, id: id, inPopup: true))
            onClick();
    }
}
