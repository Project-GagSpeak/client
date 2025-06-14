using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Editor;

public sealed class MoodlePresetCombo : CkMoodleComboBase<MoodlePresetInfo>
{
    private int _maxPresetCount => MoodleCache.IpcData.Presets.Values.Max(x => x.Statuses.Count);
    private Guid _currentItem;
    private float MaxIconWidth => IconWithPadding * (_maxPresetCount - 1);
    private float IconWithPadding => IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
    public MoodlePresetCombo(float iconScale, MoodleIcons monitor, ILogger log)
        : base(iconScale, monitor, log, () => [ .. MoodleCache.IpcData.Presets.Values.OrderBy(x => x.Title)])
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
    public bool Draw(string label, Guid current, float width, uint? searchBg = null)
        => Draw(label, current, width, ImGuiComboFlags.None, searchBg);

    public bool Draw(string label, Guid current, float width, CFlags flags, uint? searchBg = null)
    {
        InnerWidth = (width * 1.25f) + MaxIconWidth;
        _currentItem = current;
        // Maybe there is a faster way to know this, but atm I do not know.
        var currentTitle = Items.FirstOrDefault(i => i.GUID == _currentItem).Title?.StripColorTags() ?? string.Empty;
        var preview = currentTitle.IsNullOrWhitespace() ? "Select Moodle Preset..." : currentTitle;
        return Draw($"##preset{label}", preview, string.Empty, width, MoodleDrawer.IconSize.Y, flags);
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
            if (!MoodleCache.IpcData.Statuses.TryGetValue(status, out var info))
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
