using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.CustomCombos.Editor;

public sealed class MoodleStatusCombo : CkMoodleComboBase<MoodlesStatusInfo>
{
    private Guid _currentItem;
    public MoodleStatusCombo(float iconScale, MoodleIcons displayer, ILogger log)
        : base(iconScale, displayer, log, () => [.. MoodleCache.IpcData.StatusList.OrderBy(x => x.Title)])
    {
        _currentItem = Guid.Empty;
    }

    protected override string ToString(MoodlesStatusInfo obj)
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

    /// <summary> An override that does not provide a pre-determined value. Instead this value is selected after creation and maintained. </summary>
    /// <remarks> Not stored anywhere but in the combo itself. </remarks>
    public bool Draw(string label, float width, uint? searchBg = null)
    {
        InnerWidth = width * 1.5f;
        _currentItem = Current.GUID;
        return Draw(label, _currentItem, width, searchBg);
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, Guid current, float width, uint? searchBg = null)
        => Draw(label, current, width, CFlags.None, searchBg);

    public bool Draw(string label, Guid current, float width, CFlags flags, uint? searchBg = null)
    {
        InnerWidth = width * 1.5f;
        _currentItem = current;
        // Maybe there is a faster way to know this, but atm I do not know.
        var currentTitle = Items.FirstOrDefault(i => i.GUID == _currentItem).Title?.StripColorTags() ?? string.Empty;
        var previewName = currentTitle.IsNullOrWhitespace() ? "Select Moodle Status..." : currentTitle;
        return Draw($"##status{label}", previewName, string.Empty, width, MoodleDrawer.IconSize.Y, flags);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var moodleStatus = Items[globalIdx];
        var ret = ImGui.Selectable("##"+moodleStatus.Title, selected, ImGuiSelectableFlags.None, new Vector2(GetFilterWidth(), IconSize.Y));

        if (moodleStatus.IconID > 200000)
        {
            ImGui.SameLine(2, 2);
            _displayer.DisplayMoodleTitle(moodleStatus.Title, ImGui.GetContentRegionAvail().X);
            ImGui.SameLine();
            var offset = ImGui.GetContentRegionAvail().X - IconSize.X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            _displayer.DrawMoodleIcon(moodleStatus.IconID, moodleStatus.Stacks, IconSize);
            DrawItemTooltip(moodleStatus);
        }
        return ret;
    }
}
