using CkCommons.Helpers;
using CkCommons.RichText;
using CkCommons.Textures;
using GagSpeak.Gui.Components;
using GagSpeak.Services;
using GagSpeak.State.Caches;
using Dalamud.Bindings.ImGui;
using OtterGui.Extensions;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Editor;

public sealed class MoodlePresetCombo : CkMoodleComboBase<MoodlePresetInfo>
{
    private int _maxPresetCount => MoodleCache.IpcData.Presets.Values.Max(x => x.Statuses.Count);
    private Guid _currentItem;
    private float IconWithPadding => IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
    public MoodlePresetCombo(ILogger log, float iconScale)
        : base(log, iconScale, () => [ .. MoodleCache.IpcData.Presets.Values.OrderBy(x => x.Title)])
    {
        SearchByParts = false;
    }


    protected override string ToString(MoodlePresetInfo obj)
        => obj.Title;

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
        => Draw(label, current, width, CFlags.None, searchBg);

    public bool Draw(string label, Guid current, float width, CFlags flags, uint? searchBg = null)
    {
        InnerWidth = width + IconWithPadding * _maxPresetCount;
        _currentItem = current;
        // Maybe there is a faster way to know this, but atm I do not know.
        var currentTitle = Items.FirstOrDefault(i => i.GUID == _currentItem).Title?.StripColorTags() ?? string.Empty;
        var preview = currentTitle.IsNullOrWhitespace() ? "Select Moodle Preset..." : currentTitle;
        return Draw($"##preset{label}", preview, string.Empty, width, MoodleDrawer.IconSize.Y, flags);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var moodlePreset = Items[globalIdx];
        var size = new Vector2(GetFilterWidth(), IconSize.Y);
        var iconsSpace = (IconWithPadding * moodlePreset.Statuses.Count);
        var titleSpace = size.X - iconsSpace;
        var ret = ImGui.Selectable($"##{moodlePreset.Title}", selected, ImGuiSelectableFlags.None, size);

        if (moodlePreset.Statuses.Count <= 0)
            return ret;

        ImGui.SameLine(titleSpace);
        for (int i = 0, iconsDrawn = 0; i < moodlePreset.Statuses.Count; i++)
        {
            var status = moodlePreset.Statuses[i];
            if (!MoodleCache.IpcData.Statuses.TryGetValue(status, out var info))
            {
                ImGui.SameLine(0, IconWithPadding);
                continue;
            }

            MoodleIcon.DrawMoodleIcon(info.IconID, info.Stacks, IconSize);
            DrawItemTooltip(info);

            if (++iconsDrawn < moodlePreset.Statuses.Count)
                ImUtf8.SameLineInner();
        }

        ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
        var pos = ImGui.GetCursorPosY();
        ImGui.SetCursorPosY(pos + (size.Y - SelectableTextHeight) * 0.5f);
        using (UiFontService.Default150Percent.Push())
            CkRichText.Text(titleSpace, moodlePreset.Title);
        return ret;
    }
}
