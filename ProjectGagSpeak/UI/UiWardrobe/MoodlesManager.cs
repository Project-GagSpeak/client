using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.IPC;
using ImGuiNET;
using System.Numerics;
using System.Xml.Linq;

namespace GagSpeak.UI.UiWardrobe;

public class MoodlesManager : MediatorSubscriberBase
{
    private readonly UiSharedService _uiShared;
    private readonly PairManager _pairManager;
    private readonly MoodlesService _moodlesService;
    private enum InspectType { Status, Preset }
    public MoodlesManager(ILogger<MoodlesManager> logger,
        GagspeakMediator mediator, UiSharedService uiSharedService,
        PairManager pairManager, MoodlesService moodlesService) : base(logger, mediator)
    {
        _uiShared = uiSharedService;
        _pairManager = pairManager;
        _moodlesService = moodlesService;

        Mediator.Subscribe<IpcDataCreatedMessage>(this, (msg) => LastCreatedCharacterData = msg.CharaIPCData);
    }

    // Private accessor vars for list management.
    private InspectType CurrentType = InspectType.Status;

    // Info related to the person we are inspecting.
    private CharaIPCData LastCreatedCharacterData = null!;
    private string PairSearchString = string.Empty;
    private Pair? PairToInspect = null;
    private int SelectedExamineIndex = 0;
    private int SelectedPresetIndex = 0;

    private const string MoodleManagerStatusComboLabel = "##MoodlesManagerStatusSelector";
    private const string MoodleManagerPresetComboLabel = "##MoodlesManagerPresetSelector";

    private void MoodlesHeader()
    {
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        Vector2 textSize;
        using (_uiShared.UidFont.Push()) { textSize = ImGui.CalcTextSize(CurrentType == InspectType.Status ? "View Statuses" : "View Presets"); }
        var statusesSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Search, "Statuses");
        var presetsSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Search, "Presets");
        var centerYpos = (textSize.Y - ImGui.GetFrameHeight());

        using (ImRaii.Child("MoodlesManagerHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), ImGui.GetFrameHeight() + (centerYpos - startYpos) * 2)))
        {
            // now next to it we need to draw the header text
            ImGui.SameLine(ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            using (_uiShared.UidFont.Push())
            {
                switch (CurrentType)
                {
                    case InspectType.Status:
                        UiSharedService.ColorText("View Statuses", ImGuiColors.ParsedPink);
                        break;
                    case InspectType.Preset:
                        UiSharedService.ColorText("View Presets", ImGuiColors.ParsedPink);
                        break;
                }
            }

            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - statusesSize - presetsSize - 175f - ImGui.GetStyle().ItemSpacing.X * 4);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            var PairList = _pairManager.GetOnlineUserPairs()
                .Where(pair => pair.PairPerms.PairCanApplyYourMoodlesToYou is true) // forbid people not allowed to apply a pairs moodles from seeing them.
                .Where(pair => pair.LastIpcData != null
                && (string.IsNullOrEmpty(PairSearchString)
                || pair.UserData.AliasOrUID.Contains(PairSearchString, StringComparison.OrdinalIgnoreCase)
                || (pair.GetNickname() != null && pair.GetNickname()!.Contains(PairSearchString, StringComparison.OrdinalIgnoreCase))))
                .OrderByDescending(p => p.IsVisible) // Prioritize users with Visible == true
                .ThenBy(p => p.GetNickname() ?? p.UserData.AliasOrUID, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Add a special option for "Client Player" at the top of the list
            PairList.Insert(0, null!);

            _uiShared.DrawComboSearchable("##InspectPair", 175f, PairList, (pair) => pair == null ? "Examine Self" : pair.GetNickname() ?? pair.UserData.AliasOrUID, false,
                (pair) =>
                {
                    int idxOfSelected;
                    if (pair == null)
                    {
                        idxOfSelected = 0;
                        // reset the indexes
                        SelectedPresetIndex = 0;
                    }
                    else
                    {
                        idxOfSelected = PairList.IndexOf(pair);
                        PairToInspect = pair;
                        // reset the indexes
                        SelectedPresetIndex = 0;
                    }
                    SelectedExamineIndex = idxOfSelected;
                });

            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - statusesSize - presetsSize - ImGui.GetStyle().ItemSpacing.X * 3);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            var currentYpos = ImGui.GetCursorPosY();
            // draw revert button at the same location but right below that button
            if (_uiShared.IconTextButton(FontAwesomeIcon.Search, "Statuses", null, false, CurrentType == InspectType.Status))
            {
                CurrentType = InspectType.Status;
            }
            UiSharedService.AttachToolTip("View the list of Moodles Statuses");

            // draw revert button at the same location but right below that button
            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            if (_uiShared.IconTextButton(FontAwesomeIcon.Search, "Presets", null, false, CurrentType == InspectType.Preset))
            {
                CurrentType = InspectType.Preset;
                // reset the index to 0
                SelectedPresetIndex = 0;
            }
            UiSharedService.AttachToolTip("View the list of Moodles Presets.");
        }
    }


    public void DrawMoodlesManager(Vector2 cellPadding)
    {
        MoodlesHeader();
        ImGui.Separator();
        var DataToDisplay = SelectedExamineIndex == 0 ? LastCreatedCharacterData : PairToInspect?.LastIpcData;

        if (CurrentType == InspectType.Status)
        {
            DrawMoodles(DataToDisplay, cellPadding);
        }
        else
        {
            DrawPresets(DataToDisplay, cellPadding);
        }
    }

    private void DrawMoodles(CharaIPCData? DataToDisplay, Vector2 cellPadding)
    {
        if (IpcCallerMoodles.APIAvailable == false)
        {
            _uiShared.BigText("You do not Currently have Moodles enabled!");
            _uiShared.BigText("Enable Moodles to view own Statuses");
            return;
        }
        if (DataToDisplay == null)
        {
            _uiShared.BigText("The IPC Data is currently Null!");
            return;
        }
        // if they player has no presets, print they do not and return.
        if (DataToDisplay.MoodlesStatuses.Count == 0)
        {
            _uiShared.BigText(SelectedExamineIndex == 0 ? "You have no Statuses Set yet" : "Pair has no Statuses set.");
            return;
        }

        var length = ImGui.GetContentRegionAvail().X;

        _moodlesService.DrawMoodleStatusCombo(MoodleManagerStatusComboLabel, length, DataToDisplay.MoodlesStatuses,
            (i) => Logger.LogDebug("Selected Status: " + i), 1.25f);
        ImGui.Separator();

        using var child = ImRaii.Child("MoodlesInspectionWindow", -Vector2.One, false);
        if (!child) return;

        // draw the moodleInfo
        var cursorPos = new Vector2(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - MoodlesService.StatusSize.X * 2, ImGui.GetCursorPosY()) - new Vector2(20, 5);
        _uiShared.BigText("Moodle Information");

        var selectedStatus = _moodlesService.GetSelectedItem(MoodleManagerStatusComboLabel);
        if (DataToDisplay.MoodlesStatuses.Any(x => x.GUID == selectedStatus))
        {
            PrintMoodleInfoExtended(DataToDisplay.MoodlesStatuses.First(x => x.GUID == selectedStatus), cellPadding, cursorPos);
        }
    }

    private void DrawPresets(CharaIPCData? DataToDisplay, Vector2 cellPadding)
    {
        if (IpcCallerMoodles.APIAvailable == false)
        {
            _uiShared.BigText("You do not Currently have Moodles enabled!");
            _uiShared.BigText("Enable Moodles to view own Presets");
            return;
        }
        if (DataToDisplay == null)
        {
            _uiShared.BigText("The IPC Data is currently Null!");
            return;
        }
        // if they player has no presets, print they do not and return.
        if (DataToDisplay.MoodlesPresets.Count == 0)
        {
            _uiShared.BigText(SelectedExamineIndex == 0 ? "You have no Presets yet" : "Pair has no Presets set.");
            return;
        }

        var length = ImGui.GetContentRegionAvail().X;
        _uiShared.DrawComboSearchable("##PresetSelector", length, DataToDisplay!.MoodlesPresets, (preset) => preset.Title, false,
            (preset) => { SelectedPresetIndex = DataToDisplay.MoodlesPresets.IndexOf(preset); });

        ImGui.Separator();
        using var child = ImRaii.Child("PresetsInspectionWindow", -Vector2.One, false);
        if (!child) return;

        // draw the moodleInfo
        _uiShared.BigText("Preset Information");
        if (SelectedPresetIndex == -1) return;

        foreach (var moodle in DataToDisplay!.MoodlesPresets[SelectedPresetIndex].Item2)
        {
            int idx = DataToDisplay.MoodlesStatuses.FindIndex(m => m.GUID == moodle);
            if (idx == -1)
            {
                ImGui.CollapsingHeader("Moodle Not Found");
            }
            else
            {
                //var cursorPos = new Vector2(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - StatusSize.X * 2, ImGui.GetCursorPosY()) - new Vector2(20, 5);

                if (ImGui.CollapsingHeader(DataToDisplay.MoodlesStatuses[idx].Title + " (" + DataToDisplay.MoodlesStatuses[idx].GUID + ")"))
                {
                    PrintMoodleInfoExtended(DataToDisplay.MoodlesStatuses[idx], cellPadding, Vector2.Zero);
                }
            }
        }
    }

    /// <summary>
    /// This function and all others below were stripped from Moodles purely for the intent of making the visual display of a moodles details
    /// easily recognizable to the end user in a common format.
    /// https://github.com/kawaii/Moodles/blob/main/Moodles/Gui/TabMoodles.cs 
    /// </summary>
    private void PrintMoodleInfoExtended(MoodlesStatusInfo moodle, Vector2 cellPadding, Vector2 iconPos)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, cellPadding);
        //using var child = ImRaii.Child("##moodleInfo"+moodle.GUID, -Vector2.One, false);
        using (var table = ImRaii.Table("##moodles", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
        {
            if (!table) return;

            ImGui.TableSetupColumn("Status Config Item", ImGuiTableColumnFlags.WidthFixed, 135f);
            ImGui.TableSetupColumn("Value Set", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Title:");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputText("##name", ref moodle.Title, 150, ImGuiInputTextFlags.ReadOnly);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Icon:");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var selinfo = _moodlesService.GetIconInfo((uint)moodle.IconID);
            string iconText = $"Icon: #{moodle.IconID} {selinfo?.Name}";
            ImGui.InputText("##iconNameParsed", ref iconText, 160, ImGuiInputTextFlags.ReadOnly);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Custom VFX Path:");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputText("##customPath", ref moodle.CustomVFXPath, 150, ImGuiInputTextFlags.ReadOnly);


            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Stacks:");

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var maxStacks = moodle.Stacks.ToString();
            ImGui.InputText("##stacks", ref maxStacks, 3, ImGuiInputTextFlags.ReadOnly);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var cpx = ImGui.GetCursorPosX();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Description:");

            ImGui.TableNextColumn();
            UiSharedService.InputTextWrapMultiline("##desc", ref moodle.Description, 500, 3, ImGui.GetContentRegionAvail().X);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Category:");

            ImGui.TableNextColumn();
            using (ImRaii.Disabled())
            {
                ImGui.RadioButton("Positive", moodle.Type == StatusType.Positive);
                ImGui.SameLine();
                ImGui.RadioButton("Negative", moodle.Type == StatusType.Negative);
                ImGui.SameLine();
                ImGui.RadioButton("Special", moodle.Type == StatusType.Special);
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Duration:");
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            if (moodle.NoExpire)
            {
                ImGui.TextUnformatted("Permanent");
            }
            else
            {
                ImGui.TextUnformatted($"{moodle.Days}d {moodle.Hours}h {moodle.Minutes}m {moodle.Seconds}s");
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Persistent (Sticky):");
            ImGui.TableNextColumn();
            using (ImRaii.Disabled()) ImGui.Checkbox("##sticky", ref moodle.AsPermanent);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Imply Dispellable:");
            ImGui.TableNextColumn();
            using (ImRaii.Disabled()) ImGui.Checkbox("##dispel", ref moodle.Dispelable);

            // Stack on Reapply.
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Stack on Reapply:");
            ImGui.TableNextColumn();
            using (ImRaii.Disabled()) ImGui.Checkbox("##stackOnReapply", ref moodle.StackOnReapply);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Applicant:");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputText("##applier", ref moodle.Applier, 150, ImGuiInputTextFlags.ReadOnly);
        }

        if (moodle.IconID != 0 && iconPos != Vector2.Zero)
        {
            var statusIcon = _uiShared.GetGameStatusIcon((uint)((uint)moodle.IconID + moodle.Stacks - 1));

            if (statusIcon is { } wrap)
            {
                ImGui.SetCursorPos(iconPos);
                ImGui.Image(statusIcon.ImGuiHandle, MoodlesService.StatusSize * 2);
            }
        }
    }
}
