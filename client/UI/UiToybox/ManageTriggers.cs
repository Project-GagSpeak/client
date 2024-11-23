using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.ChatMessages;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Toybox.Controllers;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.UiToybox;

public class ToyboxTriggerManager
{
    private readonly ILogger<ToyboxTriggerManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly UiSharedService _uiShared;
    private readonly PairManager _pairManager;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PlayerCharacterData _playerManager;
    private readonly DeviceService _deviceController;
    private readonly TriggerHandler _handler;
    private readonly PatternHandler _patternHandler;
    private readonly ClientMonitorService _clientService;
    private readonly MoodlesService _moodlesService;
    private readonly TutorialService _guides;

    public ToyboxTriggerManager(ILogger<ToyboxTriggerManager> logger, GagspeakMediator mediator,
        UiSharedService uiSharedService, PairManager pairManager, ClientConfigurationManager clientConfigs,
        PlayerCharacterData playerManager, DeviceService deviceController, TriggerHandler handler,
        PatternHandler patternHandler, ClientMonitorService clientService, MoodlesService moodlesService,
        TutorialService guides)
    {
        _logger = logger;
        _mediator = mediator;
        _uiShared = uiSharedService;
        _pairManager = pairManager;
        _clientConfigs = clientConfigs;
        _playerManager = playerManager;
        _deviceController = deviceController;
        _handler = handler;
        _patternHandler = patternHandler;
        _clientService = clientService;
        _moodlesService = moodlesService;
        _guides = guides;
    }

    private Trigger? CreatedTrigger = new ChatTrigger();
    public bool CreatingTrigger = false;
    private List<Trigger> FilteredTriggerList
        => _handler.Triggers
            .Where(pattern => pattern.Name.Contains(TriggerSearchString, StringComparison.OrdinalIgnoreCase))
            .ToList();

    private int LastHoveredIndex = -1; // -1 indicates no item is currently hovered
    private LowerString TriggerSearchString = LowerString.Empty;
    private uint SelectedJobId = 1;
    private string SelectedDeviceName = LowerString.Empty;
    private int SelectedMoodleIdx = 0;

    public void DrawTriggersPanel()
    {
        var regionSize = ImGui.GetContentRegionAvail();

        // if we are creating a pattern
        if (CreatingTrigger)
        {
            DrawTriggerCreatingHeader();
            ImGui.Separator();
            DrawTriggerTypeSelector(regionSize.X);
            ImGui.Separator();
            DrawTriggerEditor(CreatedTrigger);
            return; // perform early returns so we dont access other methods
        }

        if (_handler.ClonedTriggerForEdit is null)
        {
            DrawCreateTriggerHeader();
            ImGui.Separator();
            DrawSearchFilter(regionSize.X, ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.Separator();
            if (_handler.TriggerCount > 0)
                DrawTriggerSelectableMenu();

            return; // perform early returns so we dont access other methods
        }

        // if we are editing an trigger
        if (_handler.ClonedTriggerForEdit is not null)
        {
            DrawTriggerEditorHeader();
            ImGui.Separator();
            if (_handler.TriggerCount > 0 && _handler.ClonedTriggerForEdit is not null)
                DrawTriggerEditor(_handler.ClonedTriggerForEdit);
        }
    }

    private void DrawCreateTriggerHeader()
    {
        // use button wrounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize("New Trigger");
        }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("CreateTriggerHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + (centerYpos - startYpos) * 2)))
        {
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            // draw out the icon button
            if (_uiShared.IconButton(FontAwesomeIcon.Plus))
            {
                // reset the createdTrigger to a new trigger, and set editing trigger to true
                CreatedTrigger = new ChatTrigger();
                CreatingTrigger = true;
            }
            _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.CreatingTriggers, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () =>
            {
                CreatedTrigger = new ChatTrigger();
                CreatingTrigger = true;
            });

            // now next to it we need to draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            _uiShared.BigText("New Trigger");
        }
    }

    private void DrawTriggerCreatingHeader()
    {
        // use button wrounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize($"Create Trigger");
        }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("CreatingTriggerHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + (centerYpos - startYpos) * 2)))
        {
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            // the "fuck go back" button.
            if (_uiShared.IconButton(FontAwesomeIcon.ArrowLeft))
            {
                // reset the createdTrigger to a new trigger, and set editing trigger to true
                CreatedTrigger = new ChatTrigger();
                CreatingTrigger = false;
            }
            // now next to it we need to draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            using (_uiShared.UidFont.Push())
            {
                UiSharedService.ColorText("Create Trigger", ImGuiColors.ParsedPink);
            }

            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - iconSize.X - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            // the "fuck go back" button.
            if (_uiShared.IconButton(FontAwesomeIcon.Save, null, null, CreatedTrigger is null))
            {
                // add the newly created trigger to the list of triggers
                _handler.AddNewTrigger(CreatedTrigger!);
                // reset to default and turn off creating status.
                CreatedTrigger = new ChatTrigger();
                CreatingTrigger = false;
            }
            UiSharedService.AttachToolTip(CreatedTrigger == null ? "Must choose trigger type before saving!" : "Save Trigger");
            _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.SavingTriggers, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () =>
            {
                _handler.AddNewTrigger(CreatedTrigger!);
                CreatedTrigger = new ChatTrigger();
                CreatingTrigger = false;
            });
        }
    }

    private void DrawTriggerEditorHeader()
    {
        // use button wrounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        Vector2 textSize;
        using (_uiShared.UidFont.Push()) { textSize = ImGui.CalcTextSize("Edit Trigger"); }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("EditTriggerHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + (centerYpos - startYpos) * 2)))
        {
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            // the "fuck go back" button.
            if (_uiShared.IconButton(FontAwesomeIcon.ArrowLeft))
            {
                _handler.CancelEditingTrigger();
                return;
            }
            // now next to it we need to draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            using (_uiShared.UidFont.Push())
            {
                UiSharedService.ColorText("Edit Trigger", ImGuiColors.ParsedPink);
            }

            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - iconSize.X * 2 - ImGui.GetStyle().ItemSpacing.X * 2);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            var currentYpos = ImGui.GetCursorPosY();
            // for saving contents
            if (_uiShared.IconButton(FontAwesomeIcon.Save))
            {
                // reset the createdTrigger to a new trigger, and set editing trigger to true
                _handler.SaveEditedTrigger();
            }
            UiSharedService.AttachToolTip("Save changes to Pattern & Return to Pattern List");

            // right beside it to the right, we need to draw the delete button
            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            if (_uiShared.IconButton(FontAwesomeIcon.Trash, disabled: !KeyMonitor.ShiftPressed()))
            {
                // reset the createdPattern to a new pattern, and set editing pattern to true
                _handler.RemoveTrigger(_handler.ClonedTriggerForEdit!);
            }
            UiSharedService.AttachToolTip("Delete Trigger--SEP--Must be holding SHIFT");
        }
    }

    public void DrawTriggerTypeSelector(float availableWidth)
    {
        ImGui.SetNextItemWidth(availableWidth);
        _uiShared.DrawCombo("##TriggerTypeSelector", availableWidth, Enum.GetValues<TriggerKind>(), (triggerType) => triggerType.TriggerKindToString(),
        (i) =>
        {
            switch (i)
            {
                case TriggerKind.Chat: CreatedTrigger = new ChatTrigger(); break;
                case TriggerKind.SpellAction: CreatedTrigger = new SpellActionTrigger(); break;
                case TriggerKind.HealthPercent: CreatedTrigger = new HealthPercentTrigger(); break;
                case TriggerKind.RestraintSet: CreatedTrigger = new RestraintTrigger(); break;
                case TriggerKind.GagState: CreatedTrigger = new GagTrigger(); break;
                case TriggerKind.SocialAction: CreatedTrigger = new SocialTrigger(); break;
            }
        }, CreatedTrigger?.Type ?? default);
        _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.SelectingTriggerType, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () =>
        { 
            CreatedTrigger = new ChatTrigger();
            _uiShared._selectedComboItems["##TriggerTypeSelector"] = TriggerKind.Chat;
            _setNextTab = "ChatText"; 
        });
    }

    /// <summary> Draws the search filter for the triggers. </summary>
    public void DrawSearchFilter(float availableWidth, float spacingX)
    {
        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Ban, "Clear");
        ImGui.SetNextItemWidth(availableWidth - buttonSize - spacingX);
        string filter = TriggerSearchString;
        if (ImGui.InputTextWithHint("##TriggerSearchStringFilter", "Search for a Trigger", ref filter, 255))
        {
            TriggerSearchString = filter;
        }
        ImUtf8.SameLineInner();
        using var disabled = ImRaii.Disabled(string.IsNullOrEmpty(TriggerSearchString));
        if (_uiShared.IconTextButton(FontAwesomeIcon.Ban, "Clear"))
        {
            TriggerSearchString = string.Empty;
        }
    }

    private void DrawTriggerSelectableMenu()
    {
        var region = ImGui.GetContentRegionAvail();
        var topLeftSideHeight = region.Y;
        bool anyItemHovered = false;

        using (var rightChild = ImRaii.Child($"###TriggerListPreview", region with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
        {
            // if list size has changed, refresh the list of hovered items
            for (int i = 0; i < FilteredTriggerList.Count; i++)
            {
                var set = FilteredTriggerList[i];
                DrawTriggerSelectable(set, i);

                if (ImGui.IsItemHovered())
                {
                    anyItemHovered = true;
                    LastHoveredIndex = i;
                }

                // if the item is right clicked, open the popup
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && LastHoveredIndex == i && !FilteredTriggerList[i].Enabled)
                {
                    ImGui.OpenPopup($"TriggerDataContext{i}");
                }
            }

            // if no item is hovered, reset the last hovered index
            if (!anyItemHovered) LastHoveredIndex = -1;

            if (LastHoveredIndex != -1 && LastHoveredIndex < FilteredTriggerList.Count)
            {
                if (ImGui.BeginPopup($"TriggerDataContext{LastHoveredIndex}"))
                {
                    if (ImGui.Selectable("Delete Trigger") && FilteredTriggerList[LastHoveredIndex] is not null)
                    {
                        _handler.RemoveTrigger(FilteredTriggerList[LastHoveredIndex]);
                    }
                    ImGui.EndPopup();
                }
            }
        }
        _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.TriggerList, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);
    }

    private void DrawTriggerSelectable(Trigger trigger, int idx)
    {
        // store the type of trigger, to be displayed as bigtext
        string triggerType = trigger.Type switch
        {
            TriggerKind.Chat => "Chat",
            TriggerKind.SpellAction => "Action",
            TriggerKind.HealthPercent => "Health%",
            TriggerKind.RestraintSet => "Restraint",
            TriggerKind.GagState => "Gag",
            TriggerKind.SocialAction => "Social",
            _ => "UNK"
        };

        // store the trigger name to store beside it
        string triggerName = trigger.Name;

        // display priority of trigger.
        string priority = "Priority: " + trigger.Priority.ToString();

        // display the intended vibrationtypes.
        string devicesUsed = string.Join(", ", trigger.TriggerAction.Select(x => x.DeviceName.ToString()));

        // define our sizes
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var toggleSize = _uiShared.GetIconButtonSize(trigger.Enabled ? FontAwesomeIcon.ToggleOn : FontAwesomeIcon.ToggleOff);

        Vector2 triggerTypeTextSize;
        var nameTextSize = ImGui.CalcTextSize(trigger.Name);
        var priorityTextSize = ImGui.CalcTextSize(priority);
        var devicesUsedTextSize = ImGui.CalcTextSize(devicesUsed);
        using (_uiShared.UidFont.Push()) { triggerTypeTextSize = ImGui.CalcTextSize(triggerType); }

        using var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), LastHoveredIndex == idx);
        using (ImRaii.Child($"##EditTriggerHeader{trigger.TriggerIdentifier}", new Vector2(UiSharedService.GetWindowContentRegionWidth(), 65f)))
        {
            // create a group for the bounding area
            using (var group = ImRaii.Group())
            {
                // scooch over a bit like 5f
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f);
                _uiShared.BigText(triggerType);
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((triggerTypeTextSize.Y - nameTextSize.Y) / 2) + 5f);
                UiSharedService.ColorText(triggerName, ImGuiColors.DalamudGrey2);
            }

            // now draw the lower section out.
            using (var group = ImRaii.Group())
            {
                // scooch over a bit like 5f
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f);
                UiSharedService.ColorText(priority, ImGuiColors.DalamudGrey3);
                ImGui.SameLine();
                UiSharedService.ColorText("| " + devicesUsed, ImGuiColors.DalamudGrey3);
            }

            // now, head to the sameline of the full width minus the width of the button
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - toggleSize.X - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (65f - toggleSize.Y) / 2);
            // draw out the icon button
            if (_uiShared.IconButton(trigger.Enabled ? FontAwesomeIcon.ToggleOn : FontAwesomeIcon.ToggleOff))
            {
                // set the enabled state of the trigger based on its current state so that we toggle it
                if (trigger.Enabled)
                    _handler.DisableTrigger(trigger);
                else
                    _handler.EnableTrigger(trigger);
                // toggle the state & early return so we dont access the child clicked button
                return;
            }
            if (idx is 0) _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.TogglingTriggers, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);
        }
        if (ImGui.IsItemClicked())
            _handler.StartEditingTrigger(trigger);
    }

    // create a new timespan object that is set to 60seconds.
    private TimeSpan triggerSliderLimit = new TimeSpan(0, 0, 0, 59, 999);
    public string _setNextTab = "Info"; // Default selected tab
    private void DrawTriggerEditor(Trigger? triggerToCreate)
    {
        if (triggerToCreate == null) return;

        ImGui.Spacing();

        // draw out the content details for the kind of trigger we are drawing.
        if (ImGui.BeginTabBar("TriggerEditorTabBar"))
        {
            // Define tabs and their corresponding actions
            var tabs = new Dictionary<string, System.Action>
            {
                { "Info", () => DrawInfoSettings(triggerToCreate) },
                { "Trigger Action", () => DrawTriggerActions(triggerToCreate) }
            };

            // Add the second tab dynamically based on the type of the trigger
            if (triggerToCreate is ChatTrigger)
                tabs["ChatText"] = () => DrawChatTriggerEditor((ChatTrigger)triggerToCreate);
            else if (triggerToCreate is SpellActionTrigger)
                tabs["Spells/Action"] = () => DrawSpellActionTriggerEditor((SpellActionTrigger)triggerToCreate);
            else if (triggerToCreate is HealthPercentTrigger)
                tabs["Health %"] = () => DrawHealthPercentTriggerEditor((HealthPercentTrigger)triggerToCreate);
            else if (triggerToCreate is RestraintTrigger)
                tabs["RestraintState"] = () => DrawRestraintTriggerEditor((RestraintTrigger)triggerToCreate);
            else if (triggerToCreate is GagTrigger)
                tabs["GagState"] = () => DrawGagTriggerEditor((GagTrigger)triggerToCreate);
            else if (triggerToCreate is SocialTrigger)
                tabs["Social"] = () => DrawSocialTriggerEditor((SocialTrigger)triggerToCreate);

            // Loop through the tabs and draw them
            foreach (var tab in tabs)
            {
                using (var open = ImRaii.TabItem(tab.Key, _setNextTab == tab.Key ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                {
                    if (_setNextTab == tab.Key) _setNextTab = string.Empty;

                    // Tutorials logic (example for the "Info" and "Trigger Action" tabs)
                    switch (tab.Key)
                    {
                        case "Info":
                            _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.InfoTab, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);
                            break;
                        case "Trigger Action":
                            _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.ToTriggerActions, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () => _setNextTab = "Trigger Action");
                            break;
                        case "ChatText":
                            _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.ToChatText, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () =>
                            { 
                                CreatedTrigger = new SpellActionTrigger(); 
                                _uiShared._selectedComboItems["##TriggerTypeSelector"] = TriggerKind.SpellAction;
                                _setNextTab = "Spells/Action"; 
                            });
                            break;
                        case "Spells/Action":
                            _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.ToActionTrigger, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () =>
                            { 
                                CreatedTrigger = new HealthPercentTrigger();
                                _uiShared._selectedComboItems["##TriggerTypeSelector"] = TriggerKind.HealthPercent;
                                _setNextTab = "Health %"; });
                            break;
                        case "Health %":
                            _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.ToHealthTrigger, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () =>
                            {
                                CreatedTrigger = new RestraintTrigger(); 
                                _uiShared._selectedComboItems["##TriggerTypeSelector"] = TriggerKind.RestraintSet;
                                _setNextTab = "RestraintState"; 
                            });
                            break;
                        case "RestraintState":
                            _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.ToRestraintTrigger, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () =>
                            { 
                                CreatedTrigger = new GagTrigger();
                                _uiShared._selectedComboItems["##TriggerTypeSelector"] = TriggerKind.GagState;
                                _setNextTab = "GagState";
                            });
                            break;
                        case "GagState":
                            _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.ToGagTrigger, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () =>
                            { 
                                CreatedTrigger = new SocialTrigger(); 
                                _uiShared._selectedComboItems["##TriggerTypeSelector"] = TriggerKind.SocialAction;
                                _setNextTab = "Social";
                            });
                            break;
                        case "Social":
                            _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.ToSocialTrigger, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);
                            break;
                    }

                    if (open) tab.Value.Invoke();
                }
            }
        }
        ImGui.EndTabBar();
    }

    private void DrawInfoSettings(Trigger triggerToCreate)
    {
        // draw out the details for the base of the abstract type.
        string name = triggerToCreate.Name;
        UiSharedService.ColorText("Name", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(225f);
        if (ImGui.InputTextWithHint("##NewTriggerName", "Enter Trigger Name", ref name, 40))
        {
            triggerToCreate.Name = name;
        }

        string desc = triggerToCreate.Description;
        UiSharedService.ColorText("Description", ImGuiColors.ParsedGold);
        if (UiSharedService.InputTextWrapMultiline("##NewTriggerDescription", ref desc, 100, 3, 225f))
        {
            triggerToCreate.Description = desc;
        }

        var startAfterRef = triggerToCreate.StartAfter;
        UiSharedService.ColorText("Start After (seconds : Milliseconds)", ImGuiColors.ParsedGold);
        _uiShared.DrawTimeSpanCombo("##Start Delay (seconds)", triggerSliderLimit, ref startAfterRef, UiSharedService.GetWindowContentRegionWidth() / 2, "ss\\:fff", false);
        triggerToCreate.StartAfter = startAfterRef;

        var runFor = triggerToCreate.EndAfter;
        UiSharedService.ColorText("Run For (seconds : Milliseconds)", ImGuiColors.ParsedGold);
        _uiShared.DrawTimeSpanCombo("##Execute for (seconds)", triggerSliderLimit, ref runFor, UiSharedService.GetWindowContentRegionWidth() / 2, "ss\\:fff", false);
        triggerToCreate.EndAfter = runFor;
    }


    private void DrawChatTriggerEditor(ChatTrigger chatTrigger)
    {
        string playerName = chatTrigger.FromPlayerName;
        UiSharedService.ColorText("Triggered By", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("Must follow the format Player Name@World." + Environment.NewLine + "Example: Y'shtola Rhul@Mateus");
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputTextWithHint("##FromPlayerName", "Player Name@World", ref playerName, 72))
        {
            chatTrigger.FromPlayerName = playerName;
        }
        string triggerText = chatTrigger.ChatText;
        UiSharedService.ColorText("Trigger Text", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("What the above player must say to activate this trigger.");

        if (UiSharedService.InputTextWrapMultiline("##TriggerTextString", ref triggerText, 100, 3, 200f))
        {
            chatTrigger.ChatText = triggerText;
        }


        UiSharedService.ColorText("Can be Triggered in the following Channels:", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("If none are selected, it will accept the trigger text from any channel.");
        DrawChatTriggerChannels(chatTrigger);
    }

    private void DrawSpellActionTriggerEditor(SpellActionTrigger spellActionTrigger)
    {
        // pre-display the correctly chosen action here.

        if (!CanDrawSpellActionTriggerUI()) return;

        UiSharedService.ColorText("Action Type", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("The type of action to monitor for.");

        _uiShared.DrawCombo("##ActionKindCombo", 150f, Enum.GetValues<LimitedActionEffectType>(), (ActionKind) => ActionKind.EffectTypeToString(),
        (i) => spellActionTrigger.ActionKind = i, spellActionTrigger.ActionKind);

        // the name of the action to listen to.
        UiSharedService.ColorText("Action Name", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("Action To listen for." + Environment.NewLine + Environment.NewLine
            + "NOTE: Effects Divine Benison or regen, that cast no heal value, do not count as heals.");

        bool anyChecked = spellActionTrigger.ActionID == uint.MaxValue;
        if (ImGui.Checkbox("Any", ref anyChecked))
        {
            spellActionTrigger.ActionID = anyChecked ? uint.MaxValue : 0;
        }
        _uiShared.DrawHelpText("If checked, will listen for any action from any class for this type.");

        using (var disabled = ImRaii.Disabled(anyChecked))
        {
            _uiShared.DrawComboSearchable("##ActionJobSelectionCombo", 85f, _clientService.BattleClassJobs,
            (job) => job.Name.ToString(), false, (i) =>
            {
                _logger.LogTrace($"Selected Job ID for Trigger: {i.RowId}");
                SelectedJobId = i.RowId;
                _clientService.CacheJobActionList(i.RowId);
            }, flags: ImGuiComboFlags.NoArrowButton);

            ImUtf8.SameLineInner();
            var loadedActions = _clientService.LoadedActions[SelectedJobId];
            _uiShared.DrawComboSearchable("##ActionToListenTo" + SelectedJobId, 150f, loadedActions, (action) => action.Name.ToString(),
            false, (i) => spellActionTrigger.ActionID = i.RowId, defaultPreviewText: "Select Job Action..");
        }

        // Determine how we draw out the rest of this based on the action type:
        switch (spellActionTrigger.ActionKind)
        {
            case LimitedActionEffectType.Miss:
            case LimitedActionEffectType.Attract1:
            case LimitedActionEffectType.Knockback:
                DrawDirection(spellActionTrigger);
                return;
            case LimitedActionEffectType.BlockedDamage:
            case LimitedActionEffectType.ParriedDamage:
            case LimitedActionEffectType.Damage:
            case LimitedActionEffectType.Heal:
                DrawDirection(spellActionTrigger);
                DrawThresholds(spellActionTrigger);
                return;
        }
    }

    private void DrawDirection(SpellActionTrigger spellActionTrigger)
    {
        UiSharedService.ColorText("Direction", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("Determines how the trigger is fired. --SEP--" +
            "From Self ⇒ ActionType was performed BY YOU (Target can be anything)--SEP--" +
            "Self to Others ⇒ ActionType was performed by you, and the target was NOT you--SEP--" +
            "From Others ⇒ ActionType was performed by someone besides you. (Target can be anything)--SEP--" +
            "Others to You ⇒ ActionType was performed by someone else, and YOU were the target.--SEP--" +
            "Any ⇒ Skips over the Direction Filter. Source and Target can be anyone.");

        // create a dropdown storing the enum values of TriggerDirection
        _uiShared.DrawCombo("##DirectionSelector", 150f, Enum.GetValues<TriggerDirection>(),
        (direction) => direction.DirectionToString(), (i) => spellActionTrigger.Direction = i, spellActionTrigger.Direction);
    }

    private void DrawThresholds(SpellActionTrigger spellActionTrigger)
    {
        UiSharedService.ColorText("Threshold Min Value: ", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("Minimum Damage/Heal number to trigger effect.\nLeave -1 for any.");
        var minVal = spellActionTrigger.ThresholdMinValue;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputInt("##ThresholdMinValue", ref minVal))
        {
            spellActionTrigger.ThresholdMinValue = minVal;
        }

        UiSharedService.ColorText("Threshold Max Value: ", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("Maximum Damage/Heal number to trigger effect.");
        var maxVal = spellActionTrigger.ThresholdMaxValue;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputInt("##ThresholdMaxValue", ref maxVal))
        {
            spellActionTrigger.ThresholdMaxValue = maxVal;
        }
    }

    private void DrawHealthPercentTriggerEditor(HealthPercentTrigger healthPercentTrigger)
    {
        string playerName = healthPercentTrigger.PlayerToMonitor;
        UiSharedService.ColorText("Track Health % of:", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputTextWithHint("##PlayerToTrackHealthOf", "Player Name@World", ref playerName, 72))
        {
            healthPercentTrigger.PlayerToMonitor = playerName;
        }
        _uiShared.DrawHelpText("Must follow the format Player Name@World." + Environment.NewLine + "Example: Y'shtola Rhul@Mateus");

        UiSharedService.ColorText("Use % Threshold: ", ImGuiColors.ParsedGold);
        var usePercentageHealth = healthPercentTrigger.UsePercentageHealth;
        if (ImGui.Checkbox("##Use Percentage Health", ref usePercentageHealth))
        {
            healthPercentTrigger.UsePercentageHealth = usePercentageHealth;
        }
        _uiShared.DrawHelpText("When Enabled, will watch for when health goes above or below a specific %" +
            Environment.NewLine + "Otherwise, listens for when it goes above or below a health range.");

        UiSharedService.ColorText("Pass Kind: ", ImGuiColors.ParsedGold);
        _uiShared.DrawCombo("##PassKindCombo", 150f, Enum.GetValues<ThresholdPassType>(), (passKind) => passKind.ToString(),
            (i) => healthPercentTrigger.PassKind = i, healthPercentTrigger.PassKind);
        _uiShared.DrawHelpText("If the trigger should fire when the health passes above or below the threshold.");

        if (healthPercentTrigger.UsePercentageHealth)
        {
            UiSharedService.ColorText("Health % Threshold: ", ImGuiColors.ParsedGold);
            int minHealth = healthPercentTrigger.MinHealthValue;
            if (ImGui.SliderInt("##HealthPercentage", ref minHealth, 0, 100, "%d%%"))
            {
                healthPercentTrigger.MinHealthValue = minHealth;
            }
            _uiShared.DrawHelpText("The Health % that must be crossed to activate the trigger.");
        }
        else
        {
            UiSharedService.ColorText("Min Health Range Threshold: ", ImGuiColors.ParsedGold);
            int minHealth = healthPercentTrigger.MinHealthValue;
            if (ImGui.InputInt("##MinHealthValue", ref minHealth))
            {
                healthPercentTrigger.MinHealthValue = minHealth;
            }
            _uiShared.DrawHelpText("Lowest HP Value the health should be if triggered upon going below");

            UiSharedService.ColorText("Max Health Range Threshold: ", ImGuiColors.ParsedGold);
            int maxHealth = healthPercentTrigger.MaxHealthValue;
            if (ImGui.InputInt("##MaxHealthValue", ref maxHealth))
            {
                healthPercentTrigger.MaxHealthValue = maxHealth;
            }
            _uiShared.DrawHelpText("Highest HP Value the health should be if triggered upon going above");
        }
    }

    private void DrawRestraintTriggerEditor(RestraintTrigger restraintTrigger)
    {
        UiSharedService.ColorText("Restraint Set to Monitor", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("The Restraint Set to listen to for this trigger.");

        ImGui.SetNextItemWidth(200f);
        var init = _clientConfigs.StoredRestraintSets.FirstOrDefault(x => x.RestraintId == restraintTrigger.RestraintSetId)?.ToLightData() ?? new LightRestraintData();

        var setList = _clientConfigs.StoredRestraintSets.Select(x => x.ToLightData()).ToList();
        _uiShared.DrawCombo("EditRestraintSetCombo" + restraintTrigger.TriggerIdentifier, 200f, setList, (setItem) => setItem.Name,
            (i) => restraintTrigger.RestraintSetId = i?.Identifier ?? Guid.Empty, init, false, ImGuiComboFlags.None, "No Set Selected...");

        UiSharedService.ColorText("Restraint State that fires Trigger", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        var allowedStates = new List<NewState>() { NewState.Enabled, NewState.Locked };
        _uiShared.DrawCombo("RestraintStateToMonitor" + restraintTrigger.TriggerIdentifier, 200f, allowedStates, (state) => state.ToString(),
            (i) => restraintTrigger.RestraintState = i, restraintTrigger.RestraintState, false, ImGuiComboFlags.None, "No State Selected");
    }

    private void DrawGagTriggerEditor(GagTrigger gagTrigger)
    {
        UiSharedService.ColorText("Gag to Monitor", ImGuiColors.ParsedGold);
        var gagTypes = Enum.GetValues<GagType>().Where(gag => gag != GagType.None).ToArray();
        ImGui.SetNextItemWidth(200f);
        _uiShared.DrawComboSearchable("GagTriggerGagType" + gagTrigger.TriggerIdentifier, 250, gagTypes, (gag) => gag.GagName(), false, (i) => gagTrigger.Gag = i, gagTrigger.Gag);
        _uiShared.DrawHelpText("The Gag to listen to for this trigger.");

        UiSharedService.ColorText("Gag State that fires Trigger", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        _uiShared.DrawCombo("GagStateToMonitor" + gagTrigger.TriggerIdentifier, 200f, Enum.GetValues<NewState>(), (state) => state.ToString(),
            (i) => gagTrigger.GagState = i, gagTrigger.GagState, false, ImGuiComboFlags.None, "No Layer Selected");
        _uiShared.DrawHelpText("Trigger should be fired when the gag state changes to this.");
    }

    private void DrawSocialTriggerEditor(SocialTrigger socialTrigger)
    {
        UiSharedService.ColorText("Social Action to Monitor", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        _uiShared.DrawCombo("SocialActionToMonitor", 200f, Enum.GetValues<SocialActionType>(), (action) => action.ToString(),
            (i) => socialTrigger.SocialType = i, socialTrigger.SocialType, false, ImGuiComboFlags.None, "Select a Social Type..");

    }

    private void DrawTriggerActions(Trigger trigger)
    {
        UiSharedService.ColorText("Trigger Action Kind", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("The kind of action to perform when the trigger is activated.");

        var allowedKinds = trigger is RestraintTrigger
            ? Enum.GetValues<TriggerActionKind>().Where(x => x != TriggerActionKind.Restraint).ToArray()
            : trigger is GagTrigger
                ? Enum.GetValues<TriggerActionKind>().Where(x => x != TriggerActionKind.Gag).ToArray()
                : Enum.GetValues<TriggerActionKind>();
        _uiShared.DrawCombo("##TriggerActionTypeCombo" + trigger.TriggerIdentifier, 175f, allowedKinds,
            (triggerActionKind) => triggerActionKind.ToName(), (i) => trigger.TriggerActionKind = i, trigger.TriggerActionKind, false);
        _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.TriggerActionKind, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        ImGui.Separator();

        switch (trigger.TriggerActionKind)
        {
            case TriggerActionKind.SexToy:
                DrawVibeActionSettings(trigger);
                break;
            case TriggerActionKind.ShockCollar:
                DrawShockCollarActionSettings(trigger);
                break;
            case TriggerActionKind.Restraint:
                DrawRestraintActionSettings(trigger);
                break;
            case TriggerActionKind.Gag:
                DrawGagActionSettings(trigger);
                break;
            case TriggerActionKind.Moodle:
                DrawMoodleStatusActionSettings(trigger);
                break;
            case TriggerActionKind.MoodlePreset:
                DrawMoodlePresetActionSettings(trigger);
                break;
        }
    }

    private void DrawVibeActionSettings(Trigger trigger)
    {
        try
        {
            float width = ImGui.GetContentRegionAvail().X - _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus).X - ImGui.GetStyle().ItemInnerSpacing.X;
            // had to call it this way instead of just via connectedDevices, because otherwise, it would cause a crash due to referencing from values that are null.
            var deviceNames = _deviceController.ConnectedDevices?
                .Where(device => device != null && !string.IsNullOrEmpty(device.DeviceName))
                .Select(device => device.DeviceName)
                .ToList() ?? new List<string>();

            UiSharedService.ColorText("Select and Add a Device", ImGuiColors.ParsedGold);

            _uiShared.DrawCombo("VibeDeviceTriggerSelector" + trigger.TriggerIdentifier, width, deviceNames, (device) => device,
            (i) => { SelectedDeviceName = i ?? string.Empty; }, default, false, ImGuiComboFlags.None, "No Devices Connected");
            ImUtf8.SameLineInner();
            if (_uiShared.IconButton(FontAwesomeIcon.Plus, null, null, SelectedDeviceName == string.Empty))
            {
                // attempt to find the device by its name.
                var connectedDevice = _deviceController.GetDeviceByName(SelectedDeviceName);
                if (connectedDevice == null)
                {
                    _logger.LogWarning("Could not find device by name: " + SelectedDeviceName);
                }
                else
                {
                    trigger.TriggerAction.Add(new(connectedDevice.DeviceName, connectedDevice.VibeMotors, connectedDevice.RotateMotors));
                }
            }

            ImGui.Separator();

            if (trigger.TriggerAction.Count == 0) return;

            // draw a collapsible header for each of the selected devices.
            for (var i = 0; i < trigger.TriggerAction.Count; i++)
            {
                if (ImGui.CollapsingHeader("Settings for Device: " + trigger.TriggerAction[i].DeviceName))
                {
                    DrawDeviceActions(trigger.TriggerAction[i], i);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error drawing VibeActionSettings");
        }
    }

    private void DrawShockCollarActionSettings(Trigger trigger)
    {
        // determine the opCode
        UiSharedService.ColorText("Shock Collar Action", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("What kind of action to inflict on the shock collar.");

        _uiShared.DrawCombo("ShockCollarActionType" + trigger.TriggerIdentifier, 100f, Enum.GetValues<ShockMode>(), (shockMode) => shockMode.ToString(),
            (i) => trigger.ShockTriggerAction.OpCode = i, trigger.ShockTriggerAction.OpCode, false, ImGuiComboFlags.None, "Select Action...");

        if (trigger.ShockTriggerAction.OpCode != ShockMode.Beep)
        {
            ImGui.Spacing();
            // draw the intensity slider
            UiSharedService.ColorText(trigger.ShockTriggerAction.OpCode + " Intensity", ImGuiColors.ParsedGold);
            _uiShared.DrawHelpText("Adjust the intensity level that will be sent to the shock collar.");

            int intensity = trigger.ShockTriggerAction.Intensity;
            if (ImGui.SliderInt("##ShockCollarIntensity" + trigger.TriggerIdentifier, ref intensity, 0, 100))
            {
                trigger.ShockTriggerAction.Intensity = intensity;
            }
        }

        ImGui.Spacing();
        // draw the duration slider
        UiSharedService.ColorText(trigger.ShockTriggerAction.OpCode + " Duration", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("Adjust the Duration the action is played for on the shock collar.");

        var duration = trigger.ShockTriggerAction.Duration;
        TimeSpan timeSpanFormat = (duration > 15 && duration < 100)
            ? TimeSpan.Zero // invalid range.
            : (duration >= 100 && duration <= 15000)
                ? TimeSpan.FromMilliseconds(duration) // convert to milliseconds
                : TimeSpan.FromSeconds(duration); // convert to seconds
        float value = (float)timeSpanFormat.TotalSeconds + (float)timeSpanFormat.Milliseconds / 1000;
        if (ImGui.SliderFloat("##ShockCollarDuration" + trigger.TriggerIdentifier, ref value, 0.016f, 15f))
        {
            int newMaxDuration;
            if (value % 1 == 0 && value >= 1 && value <= 15) { newMaxDuration = (int)value; }
            else { newMaxDuration = (int)(value * 1000); }
            trigger.ShockTriggerAction.Duration = newMaxDuration;
        }
    }

    private void DrawRestraintActionSettings(Trigger trigger)
    {
        UiSharedService.ColorText("Apply Restraint Set", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        List<LightRestraintData> lightRestraintItems = _clientConfigs.StoredRestraintSets.Select(x => x.ToLightData()).ToList();
        _uiShared.DrawCombo("ApplyRestraintSetActionCombo" + trigger.TriggerIdentifier, 200f, lightRestraintItems,
            toName: (lightRestraint) => lightRestraint.Name, (i) =>
            {
                if (i is null)
                {
                    _logger.LogWarning("Selected Restraint was null!");
                    return;
                }
                trigger.RestraintTriggerAction = i;
            }, lightRestraintItems.FirstOrDefault() ?? new LightRestraintData(), false, ImGuiComboFlags.None, "No Set Selected...");
        _uiShared.DrawHelpText("Apply restraint set to your character when the trigger is fired.");
    }

    private void DrawGagActionSettings(Trigger trigger)
    {
        UiSharedService.ColorText("Apply Gag Type", ImGuiColors.ParsedGold);
        var gagTypes = Enum.GetValues<GagType>().Where(gag => gag != GagType.None).ToArray();
        ImGui.SetNextItemWidth(200f);
        _uiShared.DrawComboSearchable("GagActionGagType" + trigger.TriggerIdentifier, 250, gagTypes, (gag) => gag.GagName(), false, (i) =>
        {
            _logger.LogTrace($"Selected Gag Type for Trigger: {i}");
            trigger.GagTypeAction = i;
        }, trigger.GagTypeAction, "No Gag Type Selected");
        _uiShared.DrawHelpText("Apply this Gag to your character when the trigger is fired.");
    }

    private void DrawMoodleStatusActionSettings(Trigger trigger)
    {
        if (!IpcCallerMoodles.APIAvailable || _playerManager.LastIpcData == null)
        {
            UiSharedService.ColorText("Moodles is not currently active!", ImGuiColors.DalamudRed);
            return;
        }

        // reset the index if its out of bounds.
        if (SelectedMoodleIdx >= _playerManager.LastIpcData.MoodlesStatuses.Count) SelectedMoodleIdx = 0;

        UiSharedService.ColorText("Moodle Status to Apply", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);

        _moodlesService.DrawMoodleStatusCombo("##MoodleStatusTriggerAction" + trigger.TriggerIdentifier, ImGui.GetContentRegionAvail().X,
        statusList: _playerManager.LastIpcData.MoodlesStatuses,
        onSelected: (i) =>
        {
            _logger.LogTrace($"Selected Moodle Status for Trigger: {i}");
            trigger.MoodlesIdentifier = i ?? Guid.Empty;
        }, initialSelectedItem: trigger.MoodlesIdentifier);
        _uiShared.DrawHelpText("This Moodle will be applied when the trigger is fired.");
    }

    private void DrawMoodlePresetActionSettings(Trigger trigger)
    {
        if (!IpcCallerMoodles.APIAvailable || _playerManager.LastIpcData == null)
        {
            UiSharedService.ColorText("Moodles is not currently active!", ImGuiColors.DalamudRed);
            return;
        }

        UiSharedService.ColorText("Moodle Preset to Apply", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        _moodlesService.DrawMoodlesPresetCombo("##MoodlePresetTriggerAction" + trigger.TriggerIdentifier, ImGui.GetContentRegionAvail().X,
            _playerManager.LastIpcData.MoodlesPresets,
            _playerManager.LastIpcData.MoodlesStatuses,
            (i) => trigger.MoodlesIdentifier = i ?? Guid.Empty);
        _uiShared.DrawHelpText("This Moodle Preset will be applied when the trigger is fired.");
    }

    private void DrawDeviceActions(DeviceTriggerAction deviceAction, int idx)
    {
        if (deviceAction.VibrateMotorCount == 0) return;

        bool vibrates = deviceAction.Vibrate;
        if (ImGui.Checkbox("##Vibrate Device" + deviceAction.DeviceName, ref vibrates))
        {
            deviceAction.Vibrate = vibrates;
        }
        ImUtf8.SameLineInner();
        UiSharedService.ColorText("Vibrate Device", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("Determines if this device will have its vibration motors activated.");

        using (ImRaii.Disabled(!vibrates))
            for (var i = 0; i < deviceAction.VibrateMotorCount; i++)
            {
                DrawMotorAction(deviceAction, i);
            }
    }

    private void DrawMotorAction(DeviceTriggerAction deviceAction, int motorIndex)
    {
        var motor = deviceAction.VibrateActions.FirstOrDefault(x => x.MotorIndex == motorIndex);
        bool enabled = motor != null;

        ImGui.AlignTextToFramePadding();
        UiSharedService.ColorText("Motor " + (motorIndex + 1), ImGuiColors.ParsedGold);
        ImGui.SameLine();

        ImGui.AlignTextToFramePadding();
        if (ImGui.Checkbox("##Motor" + motorIndex + deviceAction.DeviceName, ref enabled))
        {
            if (enabled)
            {
                deviceAction.VibrateActions.Add(new MotorAction((uint)motorIndex));
            }
            else
            {
                deviceAction.VibrateActions.RemoveAll(x => x.MotorIndex == motorIndex);
            }
        }
        UiSharedService.AttachToolTip("Enable/Disable Motor Activation on trigger execution");

        if (motor == null)
        {
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Motor not Enabled");
            return;
        }

        ImUtf8.SameLineInner();
        _uiShared.DrawCombo("##ActionType" + deviceAction.DeviceName + motorIndex, ImGui.CalcTextSize("Vibration").X + ImGui.GetStyle().FramePadding.X * 2,
            Enum.GetValues<TriggerActionType>(), type => type.ToName(), (i) => motor.ExecuteType = i, motor.ExecuteType, false, ImGuiComboFlags.NoArrowButton);
        UiSharedService.AttachToolTip("What should be played to this motor?");


        ImUtf8.SameLineInner();
        if (motor.ExecuteType == TriggerActionType.Vibration)
        {
            int intensity = motor.Intensity;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.SliderInt("##MotorSlider" + deviceAction.DeviceName + motorIndex, ref intensity, 0, 100))
            {
                motor.Intensity = (byte)intensity;
            }
        }
        else
        {
            _uiShared.DrawComboSearchable("PatternSelector" + deviceAction.DeviceName + motorIndex, ImGui.GetContentRegionAvail().X, _patternHandler.Patterns,
                pattern => pattern.Name, false, (i) =>
            {
                motor.PatternIdentifier = i?.UniqueIdentifier ?? Guid.Empty;
                motor.StartPoint = i?.StartPoint ?? TimeSpan.Zero;
            }, default, "No Pattern Selected");
        }
    }


    private bool CanDrawSpellActionTriggerUI()
    {
        // if the selected job id is the max value and the client is logged in, set it to the client class job.
        if (!_clientService.LoadedActions.ContainsKey(SelectedJobId))
        {
            _clientService.CacheJobActionList(SelectedJobId);

            ImGui.Text("SelectedJobID: " + SelectedJobId);
            ImGui.Text("Loading Actions, please wait.");
            ImGui.Text("Current ClassJob size: " + _clientService.ClassJobs.Count);
            ImGui.Text("If this doesnt go away, its an error. Report it!");
            return false;
        }
        return true;
    }

    private void DrawChatTriggerChannels(ChatTrigger chatTrigger)
    {
        var i = 0;
        foreach (var e in ChatChannel.GetOrderedChannels())
        {
            // See if it is already enabled by default
            var enabled = chatTrigger.AllowedChannels.Contains(e);

            // Create a new line after every 4 columns
            if (i != 0 && (i == 4 || i == 7 || i == 11 || i == 15 || i == 19)) ImGui.NewLine();

            // Move to the next row if it is LS1 or CWLS1
            if (e is ChatChannel.Channels.LS1 or ChatChannel.Channels.CWL1) ImGui.Separator();

            if (ImGui.Checkbox($"{e}", ref enabled))
            {
                if (enabled && !chatTrigger.AllowedChannels.Contains(e))
                {
                    // ensure that it is not already in the list first
                    chatTrigger.AllowedChannels.Add(e);
                }
                else
                {
                    chatTrigger.AllowedChannels.Remove(e);
                }
            }

            ImGui.SameLine();
            i++;
        }
    }
}
