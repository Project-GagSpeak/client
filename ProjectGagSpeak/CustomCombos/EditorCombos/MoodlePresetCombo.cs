using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerState.Visual;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class MoodlePresetCombo : CkMoodleComboBase<MoodlePresetInfo>
{
    private int longestPresetCount => VisualApplierMoodles.LatestIpcData.MoodlesPresets.Max(x => x.Statuses.Count);
    private Guid _currentItem;
    private float MaxIconWidth => IconWithPadding * (longestPresetCount - 1);
    private float IconWithPadding => IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
    public MoodlePresetCombo(float iconScale, MoodlesDisplayer monitor, ILogger log)
        : base(iconScale, monitor, log, () => [ .. VisualApplierMoodles.LatestIpcData.MoodlesPresets.OrderBy(x => x.Title)])
    {
        SearchByParts = false;
    }


    protected override string ToString(MoodlePresetInfo obj)
        => obj.Title;

    protected override void DrawList(float width, float itemHeight)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));
        try
        {
            ImGui.SetWindowFontScale(_iconScale);
            base.DrawList(width, itemHeight);
            if (NewSelection != null && Items.Count > NewSelection.Value)
                Current = Items[NewSelection.Value];
        }
        finally
        {
            ImGui.SetWindowFontScale(1.0f);
        }
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current.GUID == _currentItem)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.GUID == _currentItem);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, Guid currentPreset, float width)
        => Draw(label, currentPreset, width, ImGuiComboFlags.None);

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, Guid currentPreset, float width, ImGuiComboFlags flags)
    {
        // update the inner width.
        InnerWidth = (width * 1.25f) + MaxIconWidth;
        _currentItem = currentPreset;
        // Maybe there is a faster way to know this, but atm I do not know.
        var currentTitle = Items.FirstOrDefault(i => i.GUID == _currentItem).Title?.StripColorTags() ?? string.Empty;
        var previewName = currentTitle.IsNullOrWhitespace() ? "Select Moodle Preset..." : currentTitle;
        return Draw($"##preset{label}", previewName, string.Empty, width, MoodleDrawer.IconSize.Y, flags);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var moodlePreset = Items[globalIdx];
        var ret = ImGui.Selectable(moodlePreset.Title.StripColorTags(), selected, ImGuiSelectableFlags.None, new Vector2(GetFilterWidth(), MoodleDrawer.IconSize.Y));

        if (moodlePreset.Statuses.Count <= 0)
            return ret;

        ImGui.SameLine();
        var offset = ImGui.GetContentRegionAvail().X - (IconWithPadding * moodlePreset.Statuses.Count);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        for (int i = 0, iconsDrawn = 0; i < moodlePreset.Statuses.Count; i++)
        {
            var status = moodlePreset.Statuses[i];
            var info = VisualApplierMoodles.LatestIpcData.MoodlesStatuses.FirstOrDefault(x => x.GUID == status);

            if (EqualityComparer<MoodlesStatusInfo>.Default.Equals(info, default))
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + IconWithPadding);
                continue;
            }

            _displayer.DrawMoodleIcon(info.IconID, info.Stacks, IconSize);
            DrawItemTooltip(info);

            if (++iconsDrawn < moodlePreset.Statuses.Count)
                ImUtf8.SameLineInner();
        }
        return ret;
    }
}
