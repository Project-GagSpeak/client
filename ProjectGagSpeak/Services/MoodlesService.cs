using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.UI;
using GagSpeak.Utils;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Data.Permissions;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OtterGui;
using OtterGui.Text;
using System.Numerics;
namespace GagSpeak.Services;

// helps out with locating and parsing moodles icons and drawing custom dropdowns.
public class MoodlesService
{
    private readonly ILogger<MoodlesService> _logger;
    private readonly IDataManager _dataManager;
    private readonly ITextureProvider _textures;

    private static Dictionary<uint, IconInfo?> IconInfoCache = [];
    private string StatusSearchString = string.Empty;

    public MoodlesService(ILogger<MoodlesService> logger, IDataManager dataManager, ITextureProvider textures)
    {
        _logger = logger;
        _dataManager = dataManager;
        _textures = textures;
        SelectedMoodleComboGuids = new Dictionary<string, Guid>();
    }

    public static readonly Vector2 StatusSize = new(24, 32);
    public Dictionary<string, Guid> SelectedMoodleComboGuids;    // the selected combo items

    // helper function to extract the GUID from the combo label key in the dictionary
    public Guid? GetSelectedItem(string comboLabel)
    {
        if (SelectedMoodleComboGuids.TryGetValue(comboLabel, out var guid))
            return guid;
        return null;
    }


    // Helper function to handle the combo box logic
    private void DrawCombo(string comboLabel, float width, List<MoodlePresetInfo> MoodlesPresets,
        List<MoodlesStatusInfo> RelatedStatuses, Action<Guid?>? onSelected = null, Guid initialSelectedItem = default)
    {
        if (!MoodlesPresets.Any())
        {
            ImGui.SetNextItemWidth(width);
            if (ImGui.BeginCombo("##" + comboLabel, "No Presets Available..."))
                ImGui.EndCombo();
            return;
        }

        if (!SelectedMoodleComboGuids.TryGetValue(comboLabel, out var selectedItem) && selectedItem == Guid.Empty)
        {
            if (!EqualityComparer<Guid>.Default.Equals(initialSelectedItem, default))
            {
                selectedItem = initialSelectedItem;
                SelectedMoodleComboGuids[comboLabel] = selectedItem;
                if (!EqualityComparer<Guid>.Default.Equals(initialSelectedItem, default))
                    onSelected?.Invoke(initialSelectedItem);
            }
            else
            {
                selectedItem = MoodlesPresets.First().GUID;
                SelectedMoodleComboGuids[comboLabel] = selectedItem;
            }
        }

        // Ensure the selected item still exists in the list, otherwise reset it
        if (!MoodlesPresets.Any(item => item.GUID == selectedItem))
        {
            selectedItem = MoodlesPresets.First().GUID;
            SelectedMoodleComboGuids[comboLabel] = selectedItem;
        }

        // Draw the combo box
        ImGui.SetNextItemWidth(width);
        var previewText = MoodlesPresets.FirstOrDefault(x => x.GUID == selectedItem).Title ?? selectedItem.ToString();
        if (ImGui.BeginCombo(comboLabel, previewText))
        {
            foreach (var item in MoodlesPresets)
            {
                bool isSelected = item.Item1 == selectedItem;
                if (ImGui.Selectable(item.Title.ToString(), isSelected))
                {
                    SelectedMoodleComboGuids[comboLabel] = item.Item1;
                    onSelected?.Invoke(selectedItem);
                }

                // Tooltip for moodle names
                if (ImGui.IsItemHovered())
                {
                    var moodleNames = RelatedStatuses
                        .Where(x => item.Statuses.Contains(x.GUID))
                        .Select(x => x.Title)
                        .ToList();
                    ImGui.SetTooltip($"This Preset Enables the Following Moodles:\n" + string.Join(Environment.NewLine, moodleNames));
                }
            }
            ImGui.EndCombo();
        }
        // Check if the item was right-clicked. If so, reset to default value.
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _logger.LogTrace("Right-clicked on " + comboLabel + ". Resetting to default value.", LoggerType.IpcMoodles);
            selectedItem = MoodlesPresets.First().Item1;
            SelectedMoodleComboGuids[comboLabel] = selectedItem!;
            onSelected?.Invoke(selectedItem!);
        }

        return;
    }

    public void DrawMoodlesPresetCombo(string comboLabel, float width, List<MoodlePresetInfo> MoodlesPresets,
        List<MoodlesStatusInfo> RelatedMoodlesStatuses, Action<Guid?>? onSelected = null, Guid initialSelectedItem = default)
    {
        DrawCombo(comboLabel, width, MoodlesPresets, RelatedMoodlesStatuses, onSelected, initialSelectedItem);
    }

    public void DrawMoodlesPresetComboButton(string comboLabel, string buttonLabel, float width,
        List<MoodlePresetInfo> MoodlesPresets,
        List<MoodlesStatusInfo> RelatedMoodlesStatuses,
        bool buttonDisabled = false,
        Action<Guid?>? onSelected = null,
        Action<Guid?>? onButton = null,
        string buttonTT = "")
    {
        var comboWidth = width - ImGuiHelpers.GetButtonSize(buttonLabel).X - ImGui.GetStyle().ItemInnerSpacing.X;

        DrawCombo(comboLabel, comboWidth, MoodlesPresets, RelatedMoodlesStatuses, onSelected);

        ImUtf8.SameLineInner();

        if (ImGuiUtil.DrawDisabledButton(buttonLabel, new Vector2(), string.Empty, buttonDisabled))
            onButton?.Invoke(SelectedMoodleComboGuids[comboLabel]);

        if (!string.IsNullOrEmpty(buttonTT))
            UiSharedService.AttachToolTip(buttonTT);
    }

    #region MoodlesStatusCombo
    public void DrawMoodleStatusCombo(string comboLabel, float width, List<MoodlesStatusInfo> statusList,
        Action<Guid?>? onSelected = null, float sizeScaler = 1f, Guid initialSelectedItem = default)
    {
        DrawStatusComboBox(statusList, comboLabel, width, onSelected, sizeScaler, initialSelectedItem);
    }


    public void DrawMoodleStatusComboButton(
    string comboLabel,
    string buttonLabel,
    float width,
    List<MoodlesStatusInfo> statusList,
    bool buttonDisabled = false,
    Action<Guid?>? onSelected = null,
    Action<Guid?>? onButton = null,
    float sizeScaler = 1f,
    string buttonTT = "")
    {
        // Calculate the available width for the combo box
        var comboWidth = width - ImGui.GetStyle().ItemInnerSpacing.X - ImGuiHelpers.GetButtonSize(buttonLabel).X;

        // Draw the status combo box and get the selected item's GUID
        DrawStatusComboBox(statusList, comboLabel, comboWidth, onSelected, sizeScaler);

        ImUtf8.SameLineInner();
        if (ImGuiUtil.DrawDisabledButton(buttonLabel, new Vector2(), string.Empty, buttonDisabled))
            onButton?.Invoke(SelectedMoodleComboGuids[comboLabel]);

        // Optional: Attach a tooltip to the button
        if (!string.IsNullOrEmpty(buttonTT))
            UiSharedService.AttachToolTip(buttonTT);
    }


    private void DrawStatusComboBox(List<MoodlesStatusInfo> statuses, string comboLabel, float width,
        Action<Guid?>? onSelected = null, float sizeScaler = 1f, Guid initialSelectedItem = default)
    {
        var height = ImGui.GetTextLineHeightWithSpacing() * 10 - ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y;

        // if the statuses list is empty, display an empty status combo box.
        if (!statuses.Any())
        {
            ImGui.SetNextItemWidth(width);
            if (ImGui.BeginCombo("##DummyEmptyComboBox", "No Statuses Available..."))
                ImGui.EndCombo();
            return;
        }

        if (!SelectedMoodleComboGuids.TryGetValue(comboLabel, out var selectedItem) && selectedItem == Guid.Empty)
        {
            if (!EqualityComparer<Guid>.Default.Equals(initialSelectedItem, default))
            {
                selectedItem = initialSelectedItem;
                SelectedMoodleComboGuids[comboLabel] = selectedItem;
                if (!EqualityComparer<Guid>.Default.Equals(initialSelectedItem, default))
                    onSelected?.Invoke(initialSelectedItem);
            }
            else
            {
                selectedItem = statuses.First().GUID;
                SelectedMoodleComboGuids[comboLabel] = selectedItem!;
            }
        }

        // Ensure the selected item still exists in the list, otherwise reset it
        if (!statuses.Any(item => item.GUID == selectedItem) && selectedItem != Guid.Empty)
        {
            selectedItem = statuses.First().Item1;
            SelectedMoodleComboGuids[comboLabel] = selectedItem;
        }

        var statusSelected = statuses.FirstOrDefault(x => x.GUID == selectedItem).Title.StripColorTags();
        string selectedLabel = "None Selected";
        if (!statusSelected.IsNullOrEmpty())
            selectedLabel = statusSelected.StripColorTags();

        ImGui.SetNextItemWidth(width);
        if (ImGui.BeginCombo(comboLabel, selectedLabel))
        {
            // Set up Search filter
            ImGui.SetNextItemWidth(width - 5f);
            ImGui.InputTextWithHint("##filter", "Filter...", ref StatusSearchString, 100);
            var searchText = StatusSearchString.ToLowerInvariant();

            var filteredItems = string.IsNullOrEmpty(searchText)
            ? statuses : statuses.Where(item => item.Title.ToLowerInvariant().Contains(searchText));

            ImGui.SetWindowFontScale(sizeScaler);
            if (ImGui.BeginTable("StatusSelectableTable", 2)) // 2 columns for status name and icon
            {
                // Setup columns based on the format
                ImGui.TableSetupColumn("##StatusName", ImGuiTableColumnFlags.WidthFixed, width - StatusSize.X - ImGui.GetStyle().ItemSpacing.X * 2);
                ImGui.TableSetupColumn("##StatusIcon", ImGuiTableColumnFlags.WidthFixed, StatusSize.X);

                // Display filtered content.
                foreach (var item in filteredItems)
                {
                    // Draw out a selectable spanning the cell.
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(width);
                    bool isSelected = item.GUID == selectedItem;
                    if (ImGui.Selectable(item.Title.StripColorTags(), isSelected))
                    {
                        selectedItem = item.GUID;
                        SelectedMoodleComboGuids[comboLabel] = selectedItem;
                        onSelected?.Invoke(item.GUID);
                        _logger.LogTrace("Selected Item" + item.Title + ".", LoggerType.IpcMoodles);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(item.GUID.ToString());
                    }

                    // Move to the next column.
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();

                    // Draw out the image. (Ensure > 200,000 to account for 7.1 Icon Shifts. A Temp Fix until Moodles Updates)
                    if (item.IconID != 0 && item.IconID > 200000)
                    {
                        var statusIcon = _textures.GetFromGameIcon(new GameIconLookup((uint)((uint)item.IconID + item.Stacks - 1))).GetWrapOrEmpty();
                        if (statusIcon is { } wrap)
                        {
                            ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() - new Vector2(0, 5));
                            ImGui.Image(statusIcon.ImGuiHandle, StatusSize);
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip(item.Description);
                            }
                        }
                    }
                }
                ImGui.EndTable();
            }
            ImGui.SetWindowFontScale(1.0f);
            ImGui.EndCombo();
        }
        // Check if the item was right-clicked. If so, reset to default value.
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _logger.LogTrace("Right-clicked on " + comboLabel + ". Resetting to default value.", LoggerType.IpcMoodles);
            selectedItem = Guid.Empty;
            SelectedMoodleComboGuids[comboLabel] = selectedItem!;
            onSelected?.Invoke(selectedItem!);
        }
        return;
    }
    #endregion MoodlesStatusCombo

    public bool ValidatePermissionForApplication(UserPairPermissions pairPermsForClient, List<MoodlesStatusInfo> statuses)
    {

        if (!pairPermsForClient.AllowPositiveStatusTypes && statuses.Any(statuses => statuses.Type == StatusType.Positive))
        {
            _logger.LogWarning("Client Attempted to apply status(s) with at least one containing a positive status, but they are not allowed to.");
            return false;
        }
        if (!pairPermsForClient.AllowNegativeStatusTypes && statuses.Any(statuses => statuses.Type == StatusType.Negative))
        {
            _logger.LogWarning("Client Attempted to apply status(s) with at least one containing a negative status, but they are not allowed to.");
            return false;
        }
        if (!pairPermsForClient.AllowSpecialStatusTypes && statuses.Any(statuses => statuses.Type == StatusType.Special))
        {
            _logger.LogWarning("Client Attempted to apply status(s) with at least one containing a special status, but they are not allowed to.");
            return false;
        }

        if (!pairPermsForClient.AllowPermanentMoodles && statuses.Any(statuses => statuses.NoExpire))
        {
            _logger.LogWarning("Client Attempted to apply status(s) with at least one containing a permanent status, but they are not allowed to.");
            return false;
        }

        // check the max moodle time exceeding
        if (statuses.Any(status => status.NoExpire == false && // if the status is not permanent, and the time its set for is longer than max allowed time.
            new TimeSpan(status.Days, status.Hours, status.Minutes, status.Seconds) > pairPermsForClient.MaxMoodleTime))
        {
            _logger.LogWarning("Client Attempted to apply status(s) with at least one containing a time exceeding the max allowed time.");
            return false;
        }
        // return true if reached here.
        return true;
    }

    public IconInfo? GetIconInfo(uint iconID)
    {
        if (IconInfoCache.TryGetValue(iconID, out var iconInfo))
        {
            return iconInfo;
        }
        else
        {
            var data = _dataManager.GetExcelSheet<Status>()?.FirstOrDefault(x => x.Icon == iconID);
            if (data == null)
            {
                IconInfoCache[iconID] = null;
                return null;
            }
            var info = new IconInfo()
            {
                Name = data.Value.Name.ToDalamudString().ExtractText(),
                IconID = iconID,
                Type = data.Value.CanIncreaseRewards == 1 ? StatusType.Special : (data.Value.StatusCategory == 2 ? StatusType.Negative : StatusType.Positive),
                ClassJobCategory = data.Value.ClassJobCategory.Value,
                IsFCBuff = data.Value.IsFcBuff,
                IsStackable = data.Value.MaxStacks > 1,
                Description = data.Value.Description.ToDalamudString().ExtractText()

            };
            IconInfoCache[iconID] = info;
            return info;
        }
    }

    public struct IconInfo
    {
        public string Name;
        public uint IconID;
        public StatusType Type;
        public bool IsStackable;
        public ClassJobCategory ClassJobCategory;
        public bool IsFCBuff;
        public string Description;
    }
}
