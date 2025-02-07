using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.StateManagers;
using GagSpeak.UI.Components;
using GagSpeak.UI.Components.Combos;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Text;
using Penumbra.GameData.Files.ShaderStructs;
using System.Numerics;

namespace GagSpeak.UI.UiWardrobe;

public class RestraintSetManager : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerGlamourer _ipcGlamourer;
    private readonly RestraintSetEditor _editor;
    private readonly SetPreviewComponent _setPreview;
    private readonly WardrobeHandler _handler;
    private readonly AppearanceManager _appearance;
    private readonly PairManager _pairs;
    private readonly UiSharedService _uiShared;
    private readonly TutorialService _guides;

    // Our padlock provider display.
    private PadlockRestraintsClient _restraintPadlock;

    public RestraintSetManager(ILogger<RestraintSetManager> logger, GagspeakMediator mediator,
        IpcCallerGlamourer ipcGlamourer, RestraintSetEditor editor, SetPreviewComponent setPreview, 
        WardrobeHandler handler, AppearanceManager appearance, PairManager pairs,
        UiSharedService ui, TutorialService guides) : base(logger, mediator)
    {
        _ipcGlamourer = ipcGlamourer;
        _editor = editor;
        _setPreview = setPreview;
        _handler = handler;
        _appearance = appearance;
        _pairs = pairs;
        _uiShared = ui;
        _guides = guides;

        // setup the padlock provider
        _restraintPadlock = new PadlockRestraintsClient(handler, appearance, logger, ui, "Restraint Set");

        Mediator.Subscribe<TooltipSetItemToRestraintSetMessage>(this, (msg) =>
        {
            if (_handler.ClonedSetForEdit is not null)
            {
                _handler.ClonedSetForEdit.DrawData[msg.Slot].GameItem = msg.Item;
                Logger.LogDebug($"Set [" + msg.Slot + "] to [" + msg.Item.Name + "] on edited set [" + _handler.ClonedSetForEdit.Name + "]", LoggerType.Restraints);
            }
            else
            {
                Logger.LogError("No Restraint Set is currently being edited.");
            }
        });
    }

    private RestraintSet CreatedRestraintSet = new RestraintSet();
    public bool CreatingRestraintSet = false;
    private int LastHoveredIndex = -1; // -1 indicates no item is currently hovered
    private LowerString RestraintSetSearchString = LowerString.Empty;

    private List<RestraintSet> FilteredSetList
        => _handler.GetAllSetsForSearch()
        .Where(set => !set.Enabled && set.Name.Contains(RestraintSetSearchString, StringComparison.OrdinalIgnoreCase))
        .ToList();

    public void DrawManageSets(Vector2 cellPadding)
    {
        // if we are creating a pattern
        if (CreatingRestraintSet)
        {
            DrawRestraintSetCreatingHeader();
            ImGui.Separator();
            _editor.DrawRestraintSetEditor(CreatedRestraintSet, cellPadding);
            return; // perform early returns so we dont access other methods
        }

        // if we are simply viewing the main page       
        if (_handler.ClonedSetForEdit is null)
        {
            DrawSetListing(cellPadding);
            return; // perform early returns so we dont access other methods
        }

        // if we are editing an restraintSet
        if (_handler.ClonedSetForEdit is not null)
        {
            DrawRestraintSetEditorHeader();
            ImGui.Separator();
            if (_handler.RestraintSetCount > 0 && _handler.ClonedSetForEdit is not null)
            {
                _editor.DrawRestraintSetEditor(_handler.ClonedSetForEdit, cellPadding);
            }
        }
    }

    private void DrawSetListing(Vector2 cellPadding)
    {
        var region = ImGui.GetContentRegionAvail();
        var topLeftSideHeight = region.Y;

        using (var managerTable = ImRaii.Table("RestraintsManagerTable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV))
        {
            if (!managerTable) return;
            // setup the columns
            ImGui.TableSetupColumn("SetList", ImGuiTableColumnFlags.WidthFixed, 300f);
            ImGui.TableSetupColumn("PreviewSet", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow(); ImGui.TableNextColumn();

            var regionSize = ImGui.GetContentRegionAvail();

            using (var leftChild = ImRaii.Child($"###SelectableListWardrobe", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
            {
                DrawCreateRestraintSetHeader();
                ImGui.Separator();
                DrawSearchFilter(regionSize.X, ImGui.GetStyle().ItemInnerSpacing.X);
                ImGui.Separator();
                if (_handler.RestraintSetCount > 0)
                {
                    DrawRestraintSetSelectableMenu();
                }
            }


            ImGui.TableNextColumn();
            regionSize = ImGui.GetContentRegionAvail();

            using (ImRaii.Child($"###WardrobeSetPreview", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
            {
                var startYpos = ImGui.GetCursorPosY();
                Vector2 textSize;
                if(_handler.TryGetActiveSet(out var activeSet))
                {
                    using(ImRaii.Group())
                    {
                        var originalCursorPos = ImGui.GetCursorPos();
                        // Move the Y pos down a bit, only for drawing this text
                        ImGui.SetCursorPosY(originalCursorPos.Y + 2.5f);
                        // Draw the text with the desired color
                        UiSharedService.ColorText(activeSet.Name, ImGuiColors.DalamudWhite2);
                    }
                    if(activeSet.IsLocked())
                    {
                        using (ImRaii.Group())
                        {
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2.5f);
                            UiSharedService.ColorText("Locked By:", ImGuiColors.DalamudGrey2);
                            ImGui.SameLine();
                            if (_pairs.TryGetNickAliasOrUid(activeSet.Assigner, out var nick))
                                UiSharedService.ColorText(nick, ImGuiColors.DalamudGrey3);
                            else UiSharedService.ColorText(activeSet.Assigner, ImGuiColors.DalamudGrey3);
                        }
                    }
                    // draw the padlock dropdown
                    _restraintPadlock.DrawPadlockComboSection(regionSize.X, string.Empty, "Lock/Unlock this restraint.");

                    // beside draw the remaining time.
                    if (activeSet.Padlock.ToPadlock().IsTimerLock())
                    {
                        UiSharedService.ColorText("Time Remaining:", ImGuiColors.DalamudGrey2);
                        ImGui.SameLine();
                        UiSharedService.ColorText(activeSet.Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
                    }
                    else
                    {
                        if (ImGuiUtil.DrawDisabledButton("Disable Set", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()), string.Empty, activeSet.IsLocked()))
                            _ = _appearance.DisableRestraintSet(activeSet.RestraintId, MainHub.UID, true, false);
                    }
                    ImGui.Separator();
                    var activePreview = ImGui.GetContentRegionAvail() - ImGui.GetStyle().WindowPadding;
                    _setPreview.DrawRestraintSetPreviewCentered(activeSet, activePreview);
                }
                else
                {
                    using (_uiShared.UidFont.Push()) { textSize = ImGui.CalcTextSize("Set Preview"); }
                    using (ImRaii.Child("PreviewRestraintSetChild", new Vector2(UiSharedService.GetWindowContentRegionWidth(), 47)))
                    {
                        // now calculate it so that the cursors Yposition centers the button in the middle height of the text
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X / 2 - textSize.X / 2));
                        ImGui.SetCursorPosY(startYpos + 3f);
                        _uiShared.BigText("Set Preview");
                    }
                    ImGui.Separator();
                    var previewRegion = new Vector2(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X, ImGui.GetContentRegionAvail().Y);
                    if (LastHoveredIndex != -1 && LastHoveredIndex < FilteredSetList.Count)
                        _setPreview.DrawRestraintSetPreviewCentered(FilteredSetList[LastHoveredIndex], previewRegion);

                }
            }
        }
    }

    private void DrawCreateRestraintSetHeader()
    {
        // use button wrounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize("New Restraint Set");
        }
        var centerYpos = (textSize.Y - iconSize.Y);

        using (ImRaii.Child("CreateRestraintSetHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + (centerYpos - startYpos) * 2)))
        {
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            // draw out the icon button
            if (_uiShared.IconButton(FontAwesomeIcon.Plus))
            {
                // reset the createdRestraintSet to a new restraintSet, and set editing restraintSet to true
                CreatedRestraintSet = new RestraintSet();
                CreatingRestraintSet = true;
            }
            UiSharedService.AttachToolTip("Create a new Restraint Set");
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.AddingNewRestraint, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize, () =>
            {
                CreatedRestraintSet = new RestraintSet();
                CreatingRestraintSet = true;
                _editor._setNextTab = "Info";
            });

            // now next to it we need to draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            _uiShared.BigText("New Restraint Set");


        }
    }

    private void DrawRestraintSetCreatingHeader()
    {
        // use button rounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var importSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.FileImport, "Gear");
        var importCustomizeSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.FileImport, "Customize");
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Save);
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize($"Creating RestraintSet: {CreatedRestraintSet.Name}");
        }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("EditRestraintSetHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + (centerYpos - startYpos) * 2)))
        {
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            // the "fuck go back" button.
            if (_uiShared.IconButton(FontAwesomeIcon.ArrowLeft))
            {
                // reset the createdRestraintSet to a new restraintSet, and set editing restraintSet to true
                CreatedRestraintSet = new RestraintSet();
                CreatingRestraintSet = false;
            }
            UiSharedService.AttachToolTip("Exit to Restraint Set List");
            // now next to it we need to draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            using (_uiShared.UidFont.Push())
            {
                UiSharedService.ColorText(CreatedRestraintSet.Name, ImGuiColors.ParsedPink);
            }

            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            float width = ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - importSize - importCustomizeSize - iconSize.X - ImGui.GetStyle().ItemSpacing.X * 3;
            ImGui.SameLine(width);

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            var currentYpos = ImGui.GetCursorPosY();
            // draw revert button at the same location but right below that button
            if (_uiShared.IconTextButton(FontAwesomeIcon.FileImport, "Gear",
                disabled: !IpcCallerGlamourer.APIAvailable || CreatedRestraintSet is null || !KeyMonitor.ShiftPressed()))
            {
                _ipcGlamourer.SetRestraintEquipmentFromState(CreatedRestraintSet!);
                Logger.LogDebug("EquipmentImported from current State");
            }
            UiSharedService.AttachToolTip("Imports your Actor's Equipment Data from your current appearance.");
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ImportingGear, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            // draw revert button at the same location but right below that button
            if (_uiShared.IconTextButton(FontAwesomeIcon.FileImport, "Customize",
                disabled: !IpcCallerGlamourer.APIAvailable || CreatedRestraintSet is null || !KeyMonitor.ShiftPressed()))
            {
                _ipcGlamourer.SetRestraintCustomizationsFromState(CreatedRestraintSet!);
                Logger.LogDebug("Customizations Imported from current State");
            }
            UiSharedService.AttachToolTip("Imports your Actor's Customization Data from your current appearance.");
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ImportingCustomizations, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            // the "fuck go back" button.
            using (var disabled = ImRaii.Disabled(CreatedRestraintSet!.Name == string.Empty))
            {
                if (_uiShared.IconButton(FontAwesomeIcon.Save))
                {
                    // add the newly created restraintSet to the list of restraintSets
                    _handler.AddNewRestraintSet(CreatedRestraintSet);
                    // reset to default and turn off creating status.
                    CreatedRestraintSet = new RestraintSet();
                    CreatingRestraintSet = false;
                }
                UiSharedService.AttachToolTip("Save and Create Restraint Set");
                _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.AddingNewSet, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
            }
        }
    }

    private void DrawRestraintSetEditorHeader()
    {
        if (_handler.ClonedSetForEdit is null)
        {
            ImGui.Text("Cloned Set for Edit is Null!");
            return;
        }

        // use button wrounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var importSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.FileImport, "Gear");
        var importCustomizeSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.FileImport, "Customize");
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize($"{_handler.ClonedSetForEdit.Name}");
        }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("EditRestraintSetHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + (centerYpos - startYpos) * 2)))
        {
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            // the "fuck go back" button.
            if (_uiShared.IconButton(FontAwesomeIcon.ArrowLeft))
            {
                // reset the createdRestraintSet to a new restraintSet, and set editing restraintSet to true
                _handler.CancelEditingSet();
                return;
            }
            UiSharedService.AttachToolTip("Revert edits and return to Restraint Set List");
            // now next to it we need to draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            using (_uiShared.UidFont.Push())
            {
                UiSharedService.ColorText(_handler.ClonedSetForEdit.Name, ImGuiColors.ParsedPink);
            }

            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            float width = ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - importSize - importCustomizeSize - iconSize.X * 2 - ImGui.GetStyle().ItemSpacing.X * 4;
            ImGui.SameLine(width);

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            var currentYpos = ImGui.GetCursorPosY();
            // draw revert button at the same location but right below that button
            if (_uiShared.IconTextButton(FontAwesomeIcon.FileImport, "Gear",
                disabled: !IpcCallerGlamourer.APIAvailable || _handler.ClonedSetForEdit is null || !KeyMonitor.ShiftPressed()))
            {
                _ipcGlamourer.SetRestraintEquipmentFromState(_handler.ClonedSetForEdit!);
                Logger.LogDebug("EquipmentImported from current State");
            }
            UiSharedService.AttachToolTip("Imports your Actor's Equipment Data from your current appearance.--SEP--Must hold SHIFT for this to be interactable!");

            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            // draw revert button at the same location but right below that button
            if (_uiShared.IconTextButton(FontAwesomeIcon.FileImport, "Customize",
                disabled: !IpcCallerGlamourer.APIAvailable || _handler.ClonedSetForEdit is null || !KeyMonitor.ShiftPressed()))
            {
                _ipcGlamourer.SetRestraintCustomizationsFromState(_handler.ClonedSetForEdit!);
                Logger.LogDebug("Customizations Imported from current State");
            }
            UiSharedService.AttachToolTip("Imports your Actor's Customization Data from your current appearance.--SEP--Must hold SHIFT for this to be interactable!");

            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            // for saving contents
            if (_uiShared.IconButton(FontAwesomeIcon.Save))
            {
                // Save the changes from our edits and apply them to the set we cloned for edits
                _handler.SaveEditedSet();
            }
            UiSharedService.AttachToolTip("Save changes to Restraint Set & Return to the main list");

            // right beside it to the right, we need to draw the delete button
            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            if (_uiShared.IconButton(FontAwesomeIcon.Trash, null, null, !KeyMonitor.CtrlPressed()))
            {
                // reset the createdPattern to a new pattern, and set editing pattern to true
                _handler.RemoveRestraintSet(_handler.ClonedSetForEdit!.RestraintId);
            }
            UiSharedService.AttachToolTip("Delete Restraint Set");
        }
    }

    /// <summary> Draws the search filter for our user pair list (whitelist) </summary>
    public void DrawSearchFilter(float availableWidth, float spacingX)
    {
        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Ban, "Clear");
        ImGui.SetNextItemWidth(availableWidth - buttonSize - spacingX);
        string filter = RestraintSetSearchString;
        if (ImGui.InputTextWithHint("##RestraintFilter", "Search for Restraint Set", ref filter, 255))
        {
            RestraintSetSearchString = filter;
            LastHoveredIndex = -1;
        }
        ImUtf8.SameLineInner();
        using var disabled = ImRaii.Disabled(string.IsNullOrEmpty(RestraintSetSearchString));
        if (_uiShared.IconTextButton(FontAwesomeIcon.Ban, "Clear"))
        {
            RestraintSetSearchString = string.Empty;
            LastHoveredIndex = -1;
        }
    }

    private void DrawRestraintSetSelectableMenu()
    {
        var region = ImGui.GetContentRegionAvail();
        var topLeftSideHeight = region.Y;
        bool anyItemHovered = false;
        using (ImRaii.Child($"###RestraintSetListPreview", region with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
        {
            for (int i = 0; i < FilteredSetList.Count; i++)
            {
                var set = FilteredSetList[i];
                DrawRestraintSetSelectable(set, i);

                if (ImGui.IsItemHovered())
                {
                    anyItemHovered = true;
                    LastHoveredIndex = i;
                }

                // if the item is right clicked, open the popup
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    if (LastHoveredIndex == i && !FilteredSetList[i].Enabled)
                        ImGui.OpenPopup($"RestraintSetContext{i}");
                }
            }

            bool isPopupOpen = LastHoveredIndex != -1 && ImGui.IsPopupOpen($"RestraintSetContext{LastHoveredIndex}");

            if (LastHoveredIndex != -1 && LastHoveredIndex < FilteredSetList.Count)
            {
                if (ImGui.BeginPopup($"RestraintSetContext{LastHoveredIndex}"))
                {
                    if (ImGui.Selectable("Clone Restraint Set") && FilteredSetList[LastHoveredIndex] != null)
                    {
                        _handler.CloneRestraintSet(FilteredSetList[LastHoveredIndex]);
                    }
                    // if you try to access filtered set list anywhere after this you will need to pull this into
                    // a seperate function and do early returns to prevent crashes.
                    if (ImGui.Selectable("Delete Set") && FilteredSetList[LastHoveredIndex] != null)
                    {
                        _handler.RemoveRestraintSet(FilteredSetList[LastHoveredIndex].RestraintId);
                    }
                    ImGui.EndPopup();
                }
            }

            // if no item is hovered, reset the last hovered index
            if (!anyItemHovered && !isPopupOpen) LastHoveredIndex = -1;
        }
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.RestraintSetList, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
    }

    private void DrawRestraintSetSelectable(RestraintSet set, int idx)
    {
        // grab the name of the set
        var name = set.Name;
        // grab the description of the set
        var description = set.Description;

        // define our sizes
        var startYpos = ImGui.GetCursorPosY();
        var toggleSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.ToggleOff);
        var nameTextSize = ImGui.CalcTextSize(set.Name);
        var descriptionTextSize = ImGui.CalcTextSize(set.Description);

        // determine the height of this selection and what kind of selection it is.
        var isActiveSet = (set.Enabled == true);

        // if it is the active set, dont push the color, otherwise push the color

        using var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), !isActiveSet && LastHoveredIndex == idx);
        using (ImRaii.Child($"##RestraintSetHeader{idx}", new Vector2(UiSharedService.GetWindowContentRegionWidth(), ImGui.GetFrameHeight() * 2 - 5f)))
        {
            var maxAllowedWidth = ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - toggleSize.X - ImGui.GetStyle().ItemSpacing.X * 3;
            // create a group for the bounding area
            using (ImRaii.Group())
            {
                // scooch over a bit like 5f
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f);
                var originalCursorPos = ImGui.GetCursorPos();
                // Move the Y pos down a bit, only for drawing this text
                ImGui.SetCursorPosY(originalCursorPos.Y + 2.5f);
                // Draw the text with the desired color
                UiSharedService.ColorText(name, ImGuiColors.DalamudWhite2);
                ImGui.SetCursorPos(originalCursorPos);
            }

            // now draw the lower section out.
            using (ImRaii.Group())
            {
                // scooch over a bit like 5f
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f);
                // if the trimmed descriptions ImGui.CalcTextSize() is larger than the maxAllowedWidth, then trim it.
                var trimmedDescription = description.Length > 50 ? description.Substring(0, 50) + "..." : description;
                // Measure the text size
                var textSize = ImGui.CalcTextSize(trimmedDescription).X;

                // If the text size exceeds the maximum allowed width, trim it further
                while (textSize > maxAllowedWidth && trimmedDescription.Length > 3)
                {
                    trimmedDescription = trimmedDescription.Substring(0, trimmedDescription.Length - 4) + "...";
                    textSize = ImGui.CalcTextSize(trimmedDescription).X;
                }
                // move the Y pos up a bit.
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2.5f);
                UiSharedService.ColorText(trimmedDescription, ImGuiColors.DalamudGrey2);
            }
            // now, head to the sameline of the full width minus the width of the button
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - toggleSize.X - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY((ImGui.GetCursorPosY() - (ImGui.GetFrameHeight() * 2 - toggleSize.Y) / 2) - 2.5f);
            // draw out the icon button
            var currentYpos = ImGui.GetCursorPosY();
            using (var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f))
            {
                bool disabled = FilteredSetList.Any(x => x.IsLocked()) || !_handler.WardrobeEnabled || !_handler.RestraintSetsEnabled;
                string ttText = set.Enabled ? (set.IsLocked() ? "Cannot Disable a Locked Set!" : "Disable Active Restraint Set")
                                            : (!_handler.WardrobeEnabled || !_handler.RestraintSetsEnabled) ? "Wardrobe / Restraint set Permissions not Active."
                                            : (disabled ? "Can't Enable another Set while active Set is Locked!" : "Enable Restraint Set");
                if (_uiShared.IconButton(set.Enabled ? FontAwesomeIcon.ToggleOn : FontAwesomeIcon.ToggleOff, null, set.Name, disabled))
                {
                    _ = _appearance.SwapOrApplyRestraint(set.RestraintId, MainHub.UID, true).ConfigureAwait(false);
                    return;
                }
                UiSharedService.AttachToolTip(ttText);
                if (idx is 0) 
                    _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.TogglingSets, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
            }
        }

        if (!isActiveSet && ImGui.IsItemClicked()) 
            _handler.StartEditingSet(set);
    }
}
