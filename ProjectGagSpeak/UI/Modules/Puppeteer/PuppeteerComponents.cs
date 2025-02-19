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
using OtterGui.Text;

namespace GagSpeak.UI.Puppeteer;

public class PuppeteerComponents
{
    private readonly ILogger<PuppeteerComponents> _logger;
    private readonly PuppeteerManager _manager;
    private readonly UiSharedService _uiShared;
    private readonly UserPairListHandler _pairList;
    public PuppeteerComponents(ILogger<PuppeteerComponents> logger, PuppeteerManager manager, UiSharedService uiShared,
        UserPairListHandler pairList)
    {
        _logger = logger;
        _manager = manager;
        _uiShared = uiShared;
        _pairList = pairList;
    }

    private readonly MoodleStatusCombo _statusCombo;
    private readonly MoodlePresetCombo _presetCombo;

    public Dictionary<string, bool> ExpandedAliasItems { get; set; } = new();

    public void DrawListenerClientGroup(bool isEditing, Action<bool>? onSitsChange = null, Action<bool>? onMotionChange = null,
        Action<bool>? onAliasChange = null, Action<bool>? onAllChange = null, Action<bool>? onEditToggle = null)
    {
        if(_pairList.SelectedPair is not { } pair)
            return;

        using var group = ImRaii.Group();

        ImGui.AlignTextToFramePadding();
        UiSharedService.ColorText("Listening To", ImGuiColors.ParsedPink);

        var remainingWidth = _uiShared.GetIconButtonSize(FontAwesomeIcon.Save).X * 5 + ImGui.GetStyle().ItemInnerSpacing.X * 4;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - remainingWidth);

        // so they let sits?
        using (ImRaii.PushColor(ImGuiCol.Text, pair.OwnPerms.PuppetPerms.HasFlag(PuppeteerPerms.Sit) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            if (_uiShared.IconButton(FontAwesomeIcon.Chair, inPopup: true))
                onSitsChange?.Invoke(!pair.OwnPerms.PuppetPerms.HasFlag(PuppeteerPerms.Sit));
        UiSharedService.AttachToolTip("Allows " + pair.GetNickAliasOrUid() + " to make you perform /sit and /groundsit (cycle pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, pair.OwnPerms.PuppetPerms.HasFlag(PuppeteerPerms.Emotes) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            if (_uiShared.IconButton(FontAwesomeIcon.Walking, inPopup: true))
                onMotionChange?.Invoke(!pair.OwnPerms.PuppetPerms.HasFlag(PuppeteerPerms.Emotes));
        UiSharedService.AttachToolTip("Allows " + pair.GetNickAliasOrUid() + " to make you perform emotes and expressions (cycle Pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, pair.OwnPerms.PuppetPerms.HasFlag(PuppeteerPerms.Alias) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            if (_uiShared.IconButton(FontAwesomeIcon.Scroll, inPopup: true))
                onAliasChange?.Invoke(!pair.OwnPerms.PuppetPerms.HasFlag(PuppeteerPerms.Alias));
        UiSharedService.AttachToolTip("Allows " + pair.GetNickAliasOrUid() + " to execute any of your Pair Alias Triggers.");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, pair.OwnPerms.PuppetPerms.HasFlag(PuppeteerPerms.All) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            if (_uiShared.IconButton(FontAwesomeIcon.CheckDouble, inPopup: true))
                onAllChange?.Invoke(!pair.OwnPerms.PuppetPerms.HasFlag(PuppeteerPerms.All));
        UiSharedService.AttachToolTip("Allows " + pair.GetNickAliasOrUid() + " to make you perform any command.");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, isEditing ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudGrey))
            if (_uiShared.IconButton(isEditing ? FontAwesomeIcon.Save : FontAwesomeIcon.Edit, inPopup: true))
                onEditToggle?.Invoke(!isEditing);
        UiSharedService.AttachToolTip(isEditing ? "Stop Editing your TriggerPhrase Info." : "Modify Your TriggerPhrase Info");
    }

    public void DrawListenerPairGroup(Action? onSendName = null)
    {
        if (_pairList.SelectedPair is not { } pair)
            return;

        bool pairHasName = pair.LastAliasData.HasNameStored;
        using var group = ImRaii.Group();

        // display name, then display the downloads and likes on the other side.
        var ButtonWidth = _uiShared.GetIconButtonSize(FontAwesomeIcon.Save).X * 5 - ImGui.GetStyle().ItemInnerSpacing.X * 4;
        using (ImRaii.PushColor(ImGuiCol.Text, pairHasName ? ImGuiColors.DalamudGrey : ImGuiColors.ParsedGold))
        {
            var isDisabled = !pair.IsOnline || (pairHasName && !KeyMonitor.ShiftPressed());
            if (_uiShared.IconTextButton(FontAwesomeIcon.CloudUploadAlt, "Send Name", ImGui.GetContentRegionAvail().X - ButtonWidth, true, isDisabled))
                onSendName?.Invoke();
        }
        UiSharedService.AttachToolTip("Send this Pair your In-Game Character Name.\nThis allows them to listen to you for triggers!" +
        "--SEP--Hold SHIFT to Resend Name.");

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ButtonWidth);
        using (ImRaii.Disabled())
        using (ImRaii.PushColor(ImGuiCol.Text, pair.PairPerms.PuppetPerms.HasFlag(PuppeteerPerms.Sit) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            _uiShared.IconButton(FontAwesomeIcon.Chair, inPopup: true);
        UiSharedService.AttachToolTip(pair.GetNickAliasOrUid() + " allows you to make them perform /sit and /groundsit (cycle pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled())
        using (ImRaii.PushColor(ImGuiCol.Text, pair.PairPerms.PuppetPerms.HasFlag(PuppeteerPerms.Emotes) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            _uiShared.IconButton(FontAwesomeIcon.Walking, inPopup: true);
        UiSharedService.AttachToolTip(pair.GetNickAliasOrUid() + " allows you to make them perform emotes and expressions (cycle Pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled())
        using (ImRaii.PushColor(ImGuiCol.Text, pair.PairPerms.PuppetPerms.HasFlag(PuppeteerPerms.Alias) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            _uiShared.IconButton(FontAwesomeIcon.Scroll, inPopup: true);
        UiSharedService.AttachToolTip(pair.GetNickAliasOrUid() + " allows you to execute any of their Alias Triggers.");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled())
        using (ImRaii.PushColor(ImGuiCol.Text, pair.PairPerms.PuppetPerms.HasFlag(PuppeteerPerms.All) ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            _uiShared.IconButton(FontAwesomeIcon.CheckDouble, inPopup: true);
        UiSharedService.AttachToolTip(pair.GetNickAliasOrUid() + " allows you to make them perform any command.");
    }

    public void DrawEditingTriggersWindow(ref string tempTriggers, ref string tempSartChar, ref string tempEndChar)
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.AlignTextToFramePadding();
        UiSharedService.ColorText("Your Trigger Phrases", ImGuiColors.ParsedPink);

        ImGui.Spacing();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputTextWithHint("##TriggerPhrase", "Leave Blank for none...", ref tempTriggers, 64);
        UiSharedService.AttachToolTip("You can create multiple trigger phrases by placing a | between phrases.");

        using (ImRaii.Group())
        {
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            UiSharedService.ColorText("Custom Brackets:", ImGuiColors.ParsedPink);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(20 * ImGuiHelpers.GlobalScale);
            ImGui.InputText("##sStarChar", ref tempSartChar, 1);
            if (ImGui.IsItemDeactivatedAfterEdit())
                if (string.IsNullOrWhiteSpace(tempSartChar)) tempSartChar = "(";
            UiSharedService.AttachToolTip($"Custom Start Character that replaces the left enclosing bracket." +
                Environment.NewLine + "Replaces the [ ( ] in: [ TriggerPhrase (commandToExecute) ]");

            ImUtf8.SameLineInner();
            _uiShared.IconText(FontAwesomeIcon.GripLinesVertical, ImGuiColors.ParsedPink);
            ImUtf8.SameLineInner();

            ImGui.SetNextItemWidth(20 * ImGuiHelpers.GlobalScale);
            ImGui.InputText("##sEndChar", ref tempEndChar, 1);
            if (ImGui.IsItemDeactivatedAfterEdit())
                if (string.IsNullOrWhiteSpace(tempEndChar)) tempEndChar = ")";
            UiSharedService.AttachToolTip($"Custom End Character that replaces the right enclosing bracket." +
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
            UiSharedService.ColorText("Your Trigger Phrases", ImGuiColors.ParsedPink);

            if (!triggers.Any() || triggers[0].IsNullOrEmpty())
            {
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted("No Trigger Phrase Set.");
            }

            foreach (var trigger in triggers)
            {
                if (trigger.IsNullOrEmpty())
                    continue;

                _uiShared.IconText(FontAwesomeIcon.QuoteLeft, ImGuiColors.ParsedPink);

                ImUtf8.SameLineInner();
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(trigger);

                ImUtf8.SameLineInner();
                _uiShared.IconText(FontAwesomeIcon.QuoteRight, ImGuiColors.ParsedPink);
            }
        }

        using (ImRaii.Group())
        {
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            UiSharedService.ColorText("Custom Brackets:", ImGuiColors.ParsedPink);
            ImGui.SameLine();
            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(startChar);
            UiSharedService.AttachToolTip($"Custom Start Character that replaces the left enclosing bracket." +
                Environment.NewLine + "Replaces the [ ( ] in: [ TriggerPhrase (commandToExecute) ]");

            ImUtf8.SameLineInner();
            _uiShared.IconText(FontAwesomeIcon.GripLinesVertical, ImGuiColors.ParsedPink);
            ImUtf8.SameLineInner();

            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(endChar);
            UiSharedService.AttachToolTip($"Custom End Character that replaces the right enclosing bracket." +
                Environment.NewLine + "Replaces the [ ) ] in Ex: [ TriggerPhrase (commandToExecute) ]");
        }

        if (triggerPhrases.IsNullOrEmpty())
            return;

        ImGui.Spacing();
        ImGui.Separator();

        var charaName = $"<YourNameWorld> ";
        UiSharedService.ColorText("Example Usage:", ImGuiColors.ParsedPink);
        ImGui.TextWrapped(charaName + triggers[0] + " " + pair.OwnPerms.StartChar + " glamour apply Hogtied | p | [me] " + pair.PairPerms.EndChar);

    }

    public void DrawAliasItemBox(string id, AliasTrigger aliasItem, LightRestraintSet[] sets, CharaIPCData moodles)
    {
        // if the id is not present in the dictionary, add it.
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
            _uiShared.BooleanToColoredIcon(aliasItem.Enabled, false);
            UiSharedService.AttachToolTip("If the Alias is currently Enabled or Disabled." +
                "--SEP--Click this while in edit mode to toggle the state!");

            // beside this, draw out the alias items label name.
            ImGui.SameLine();
            string text = aliasItem.Label.IsNullOrEmpty() ? "<No Alias Name Set!>" : aliasItem.Label;
            ImGui.TextUnformatted(text);

            UiSharedService.AttachToolTip("The Alias Label given to help with searching and organization.");

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
            if (_uiShared.IconButton(ExpandedAliasItems[id] ? FontAwesomeIcon.ChevronUp : FontAwesomeIcon.ChevronDown, inPopup: true))
                ExpandedAliasItems[id] = !ExpandedAliasItems[id];
            UiSharedService.AttachToolTip(ExpandedAliasItems[id] ? "Collapse the Alias Item." : "Expand the Alias Item.");

            // cast a seperator Line here.
            ImGui.Separator();


            ImGui.AlignTextToFramePadding();
            _uiShared.IconText(FontAwesomeIcon.Eye);
            UiSharedService.AttachToolTip("The text to scan for (Input String)");

            ImGui.SameLine();
            using (ImRaii.Disabled())
            {
                var txt = aliasItem.InputCommand.IsNullOrEmpty() ? "<Undefined Input!>" : aliasItem.InputCommand;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.InputText("##InputPreview_" + aliasItem.Identifier, ref txt, 128, ImGuiInputTextFlags.ReadOnly);
            }
            UiSharedService.AttachToolTip("The text to scan for (Input String)");


            var totalDisplayed = 0;


            // Handle Text
            if (aliasItem.Executions.TryGetValue(InvokableActionType.TextOutput, out var act) && act is TextAction textAction)
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    _uiShared.IconText(FontAwesomeIcon.Font);

                    ImGui.SameLine();
                    var txt = textAction.OutputCommand.IsNullOrEmpty() ? "<Undefined Output!>" : textAction.OutputCommand;
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    UiSharedService.ColorText("/" + txt, ImGuiColors.TankBlue);
                }
                UiSharedService.AttachToolTip("What is fired as a command." +
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
                    _uiShared.IconText(FontAwesomeIcon.Comment);
                    UiSharedService.AttachToolTip("The Following Gag State that will be applied to the Kinkster.");

                    // the following gag
                    ImGui.SameLine();
                    ImGui.TextUnformatted("The");
                    ImGui.SameLine();
                    UiSharedService.ColorText(gagAction.GagType.ToString(), ImGuiColors.TankBlue);
                    UiSharedService.AttachToolTip("The Gag to perform the new state on." +
                        "--SEP--If NONE is selected for the gag, it will perform the action on any Gag." +
                        "--SEP--Target Priority is always [Outer > Central > Inner]");

                    ImGui.SameLine();
                    ImGui.TextUnformatted("gets set to");
                    ImGui.SameLine();
                    UiSharedService.ColorText(gagAction.NewState.ToString(), ImGuiColors.TankBlue);
                    UiSharedService.AttachToolTip("The new state set on the targetted gag.");
                }
                // End viewing the rest if we meet the condition here.
                if (!ExpandedAliasItems[id] && totalDisplayed >= storedOutputSize)
                    return;
                totalDisplayed++;
            }
            // Handle Restraint
            if (aliasItem.Executions.TryGetValue(InvokableActionType.Restraint, out var actRes) && actRes is RestraintAction bindAction)
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    _uiShared.IconText(FontAwesomeIcon.ToiletPortable);
                    UiSharedService.AttachToolTip("The new state of the restraint set, and which is being applied.");

                    // if applying, list both, otherwise, list only newState.
                    if (bindAction.NewState is NewState.Enabled)
                    {
                        ImGui.SameLine();
                        ImGui.TextUnformatted("Applies Restraint:");
                        ImGui.SameLine();
                        var set = sets.FirstOrDefault(x => x.Id == bindAction.RestrictionId);
                        var setName = set is not null ? set.Label : "Set No Longer Exists";
                        UiSharedService.ColorText(setName, set is not null ? ImGuiColors.TankBlue : ImGuiColors.DalamudRed);
                        // display the set on hover if valid.
                        // if (set is not null) _setPreview.DrawLightRestraintOnHover(set);
                    }
                    // any other type, list here.
                    else
                    {
                        ImGui.SameLine();
                        UiSharedService.ColorText("Disables", ImGuiColors.TankBlue);
                        UiSharedService.AttachToolTip("The new state applied to the restraint set.");

                        ImGui.SameLine();
                        ImGui.TextUnformatted("On Active Restraint Set if allowed.");
                        UiSharedService.AttachToolTip("Will not interact with restrainted locked with types we cannot unlock.");
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
                    _uiShared.IconText(FontAwesomeIcon.WandMagicSparkles);
                    UiSharedService.AttachToolTip("Displays the Identity of the moodle status/preset applied.");

                    // the following gag
                    ImGui.SameLine();
                    ImGui.TextUnformatted("Type is");
                    ImGui.SameLine();
                    UiSharedService.ColorText(statusAction.MoodleItem is MoodlePresetApi ? "Preset" : "Status", ImGuiColors.TankBlue);

                    ImGui.SameLine();
                    var moodleInfo = "DummyText";
                    ImGui.TextUnformatted("and applies");
                    ImGui.SameLine();
                    UiSharedService.ColorText(moodleInfo, ImGuiColors.TankBlue);
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
                    _uiShared.IconText(FontAwesomeIcon.Bolt);
                    UiSharedService.AttachToolTip("The shock collar instruction executed when the input command is detected.");

                    ImGui.SameLine();
                    ImGui.TextUnformatted("Instructs:");
                    ImGui.SameLine();
                    UiSharedService.ColorText(shockAction.ShockInstruction.OpCode.ToString(), ImGuiColors.TankBlue);

                    ImGui.SameLine();
                    ImGui.TextUnformatted("for");
                    ImGui.SameLine();
                    UiSharedService.ColorText(shockAction.ShockInstruction.Duration.ToString(), ImGuiColors.TankBlue);
                    ImGui.SameLine();
                    ImGui.TextUnformatted("milliseconds");

                    // display extra information if a vibrator or shock.
                    if (shockAction.ShockInstruction.OpCode is not ShockMode.Beep)
                    {
                        ImGui.SameLine();
                        ImGui.TextUnformatted("at");
                        ImGui.SameLine();
                        UiSharedService.ColorText(shockAction.ShockInstruction.Intensity.ToString(), ImGuiColors.TankBlue);
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
                    _uiShared.IconText(FontAwesomeIcon.WaveSquare);
                    UiSharedService.AttachToolTip("The action to be executed on the listed toys.");

                    // in theory this listing could get pretty expansive so for now just list a summary.
                    ImGui.SameLine();
                    ImGui.TextUnformatted("After");
                    ImGui.SameLine();
                    UiSharedService.ColorText(toyAction.StartAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);

                    ImGui.SameLine();
                    ImGui.TextUnformatted(", actives");
                    ImGui.SameLine();
                    UiSharedService.ColorText(toyAction.DeviceActions.Count.ToString(), ImGuiColors.TankBlue);

                    ImGui.SameLine();
                    ImGui.TextUnformatted("toys to perform vibrations or patterns for the next");
                    ImGui.SameLine();
                    UiSharedService.ColorText(toyAction.EndAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);
                }
            }
            // Handle no outputs.
            if (!aliasItem.Executions.Any())
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    _uiShared.IconText(FontAwesomeIcon.ExclamationTriangle, ImGuiColors.DalamudYellow);
                    ImGui.SameLine();
                    UiSharedService.ColorText("No Output Types Added! Output won't execute correctly!", ImGuiColors.DalamudYellow);
                }
            }
        }
    }

    /// <summary>
    /// Draws the editor for an alias item.
    /// </summary>
    /// <returns>True if an element was modified, false otherwise.</returns>
    public bool DrawAliasItemEditBox(AliasTrigger aliasItem, LightRestraintSet[] sets, CharaIPCData moodleData, out bool shouldRemove)
    {
        // Assume we are not removing, and have made no modifications.
        var wasModified = false;
        shouldRemove = false;

        // pre-calculations.
        var storedOutputTypes = aliasItem.Executions.Keys;
        var storedOutputSize = storedOutputTypes.Any() ? storedOutputTypes.Count() : 1;
        var deleteButtonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.TrashAlt);
        var addTypeButtonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var comboSize = 125f;
        // merged pre-calcs
        var winFramePadHeight = ImGui.GetStyle().WindowPadding.Y * 2 + ImGui.GetStyle().FramePadding.Y * 2;
        var topRowButtonLength = deleteButtonSize.X + addTypeButtonSize.X + comboSize + itemSpacing.X;
        float height = winFramePadHeight + (ImGui.GetFrameHeight() * (storedOutputSize + 2)) + (itemSpacing.Y * (storedOutputSize + 1));

        using var child = ImRaii.Child("##AliasEditor_" + aliasItem.Identifier, new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow);
        if (!child) return wasModified;

        using (ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            _uiShared.BooleanToColoredIcon(aliasItem.Enabled, false);
            if (ImGui.IsItemClicked())
            {
                aliasItem.Enabled = !aliasItem.Enabled;
                wasModified = true;
            }
            UiSharedService.AttachToolTip("If the Alias is currently Enabled or Disabled." +
                "--SEP--Click this while in edit mode to toggle the state!");

            ImGui.SameLine();
            var tempName = aliasItem.Label;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - topRowButtonLength - itemSpacing.X * 4);
            if (ImGui.InputTextWithHint("##AliasName_" + aliasItem.Identifier, "Give Alias a Label...", ref tempName, 70))
            {
                aliasItem.Label = tempName;
                wasModified = true;
            }
            UiSharedService.AttachToolTip("The Alias Label given to help with searching and organization.");

            // scoot over to the far right where everything else is.
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - topRowButtonLength);
            // draw the add type button. (i really need to make a more modular combo class structure one day but today is not that day)
            var canUseButton = _uiShared._selectedComboItems.TryGetValue("AliasTypeCombo" + aliasItem.Identifier, out var selectedType)
                && selectedType is not null && !aliasItem.HasActionType((InvokableActionType)selectedType);

            _uiShared.DrawCombo("AliasTypeCombo" + aliasItem.Identifier, comboSize, aliasItem.UnregisteredTypes(), (item) => item.ToName(),
                initialSelectedItem: aliasItem.UnregisteredTypes().FirstOrDefault(), shouldShowLabel: false, defaultPreviewText: "Can't Add More!");
            UiSharedService.AttachToolTip("Selects a new output action kind to add to this Alias Item.");

            ImUtf8.SameLineInner();
            if (_uiShared.IconButton(FontAwesomeIcon.Plus, disabled: !canUseButton))
            {
                aliasItem.AddActionForType((InvokableActionType)selectedType!);
                wasModified = true;
                // change the selected combo value to the new default.
                _uiShared._selectedComboItems["AliasTypeCombo" + aliasItem.Identifier] = aliasItem.UnregisteredTypes().FirstOrDefault();
            }
            UiSharedService.AttachToolTip("Adds the item from the dropdown to the list of active output types." +
                "--SEP--Only 1 of each type can be added maximum");

            ImUtf8.SameLineInner();
            if (_uiShared.IconButton(FontAwesomeIcon.TrashAlt, disabled: !KeyMonitor.ShiftPressed()))
            {
                shouldRemove = true;
                return false;
            }
            UiSharedService.AttachToolTip("Deletes this Alias Item from the list." +
                "--SEP--Hold Shift to confirm deletion.");

            // ------------- SEPERATOR FOR INPUT COMMAND ------------ //
            ImGui.Separator();
            ImGui.AlignTextToFramePadding();
            _uiShared.IconText(FontAwesomeIcon.Eye);
            UiSharedService.AttachToolTip("The text to scan for (Input String)");

            ImGui.SameLine();
            var inputCommand = aliasItem.InputCommand;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
            if (ImGui.InputTextWithHint("##InputCommand_" + aliasItem.Identifier, "Enter Text To Scan For...", ref inputCommand, 256))
            {
                aliasItem.InputCommand = inputCommand;
                wasModified = true;
            }
            UiSharedService.AttachToolTip("The text to scan for (Input String)");

            // Handle Text Output Display
            if (aliasItem.Executions.TryGetValue(InvokableActionType.TextOutput, out var act) && act is TextAction textAction)
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    _uiShared.IconText(FontAwesomeIcon.Font);
                    UiSharedService.AttachToolTip("What text command you will output when the input text is read from this Kinkster.");
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
                    _uiShared.IconText(FontAwesomeIcon.Comment);
                    UiSharedService.AttachToolTip("The Following Gag State that will be applied to the Kinkster.");

                    // Set the NewState to:
                    ImGui.SameLine();
                    ImGui.TextUnformatted("Invoke");
                    ImGui.SameLine();
                    _uiShared.DrawCombo("AliasGagState" + aliasItem.Identifier, 60f, new[] { NewState.Enabled, NewState.Disabled }, (item) => item.ToString(), (i) =>
                    {
                        gagAction.NewState = i;
                        wasModified = true;
                    }, gagAction.NewState, false, ImGuiComboFlags.NoArrowButton);

                    ImGui.SameLine();
                    ImGui.TextUnformatted("state for");
                    ImGui.SameLine();
                    _uiShared.DrawCombo("AliasGagType" + aliasItem.Identifier, 150f, Enum.GetValues<GagType>(), (item) => item.GagName(), (i) =>
                    {
                        gagAction.GagType = i;
                        wasModified = true;
                    }, gagAction.GagType, false, ImGuiComboFlags.NoArrowButton);
                    UiSharedService.AttachToolTip("Selecting NONE will serve as a wildcard during removal, otherwise, it will remove the first matching GagType.");
                }

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
                DrawRemoveIcon("remove-gag-output-alias", () => aliasItem.Executions.Remove(InvokableActionType.Gag));
            }
            // Handle Restraint Action Display
            if (aliasItem.Executions.TryGetValue(InvokableActionType.Restraint, out var actRes) && actRes is RestraintAction bindAction)
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    _uiShared.IconText(FontAwesomeIcon.ToiletPortable);
                    UiSharedService.AttachToolTip("The new state of the restraint set, and which is being applied." +
                        "--SEP--Be Aware this may or may not be valid if view another Kinkster.");

                    ImGui.SameLine();
                    ImGui.TextUnformatted("Invoke");
                    ImGui.SameLine();
                    _uiShared.DrawCombo("AliasRestraintState" + aliasItem.Identifier, 60f, new[] { NewState.Enabled, NewState.Disabled }, (item) => item.ToString(), (i) =>
                    {
                        bindAction.NewState = i;
                        wasModified = true;
                    }, bindAction.NewState, false, ImGuiComboFlags.NoArrowButton);

                    ImGui.SameLine();
                    ImGui.TextUnformatted(bindAction.NewState is NewState.Enabled ? "state on" : "state for the current set.");

                    if (bindAction.NewState is NewState.Enabled)
                    {
                        ImGui.SameLine();
                        var defaultSet = sets.FirstOrDefault(x => x.Id == bindAction.RestrictionId) ?? sets.FirstOrDefault();
                        _uiShared.DrawCombo("AliasRestraintSet" + aliasItem.Identifier, 150f, sets, (item) => item.Label, (i) =>
                        {
                            bindAction.RestrictionId = i?.Id ?? Guid.Empty;
                            wasModified = true;
                        }, defaultSet, false, ImGuiComboFlags.NoArrowButton);
                    }

                    ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
                    DrawRemoveIcon("remove-restraint-output-alias", () => aliasItem.Executions.Remove(InvokableActionType.Restraint));
                }
            }
            // Handle Moodle Action Display
            if (aliasItem.Executions.TryGetValue(InvokableActionType.Moodle, out var actMoodle) && actMoodle is MoodleAction statusAction)
            {
                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    _uiShared.IconText(FontAwesomeIcon.WandMagicSparkles);
                    UiSharedService.AttachToolTip("The moodle status/preset applied.");

                    // the following gag
                    ImGui.SameLine();
                    ImGui.TextUnformatted("Apply");
                    ImGui.SameLine();
                    _uiShared.DrawCombo("AliasMoodleType" + aliasItem.Identifier, 90f, Enum.GetValues<MoodleType>(), (item) => item.ToString(), (i) =>
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
                    _uiShared.IconText(FontAwesomeIcon.Bolt);
                    UiSharedService.AttachToolTip("The shock collar instruction executed when the input command is detected.");

                    ImGui.SameLine();
                    _uiShared.DrawCombo("ShockOpCode" + aliasItem.Identifier, 60f, Enum.GetValues<ShockMode>(), (mode) => mode.ToString(), (i) =>
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
                    _uiShared.IconText(FontAwesomeIcon.WaveSquare);
                    UiSharedService.AttachToolTip("The action to be executed on the listed toys.");

                    // in theory this listing could get pretty expansive so for now just list a summary.
                    ImGui.SameLine();
                    ImGui.TextUnformatted("After");
                    ImGui.SameLine();
                    UiSharedService.ColorText(toyAction.StartAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);

                    ImGui.SameLine();
                    ImGui.TextUnformatted(", actives");
                    ImGui.SameLine();
                    UiSharedService.ColorText(toyAction.DeviceActions.Count.ToString(), ImGuiColors.TankBlue);

                    ImGui.SameLine();
                    ImGui.TextUnformatted("toys to perform vibrations or patterns for the next");
                    ImGui.SameLine();
                    UiSharedService.ColorText(toyAction.EndAfter.ToString("ss\\:fff"), ImGuiColors.TankBlue);
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
                    _uiShared.IconText(FontAwesomeIcon.ExclamationTriangle, ImGuiColors.DalamudYellow);
                    ImGui.SameLine();
                    using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted("No Output types set for this Alias!");
                }
            }
        }
        return wasModified;
    }

    private void DrawRemoveIcon(string id, Action onClick)
    {
        using var col = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
        if (_uiShared.IconButton(FontAwesomeIcon.Minus, id: id, inPopup: true))
            onClick();
    }
}
