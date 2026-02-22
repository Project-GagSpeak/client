using CkCommons.Helpers;
using CkCommons.RichText;
using CkCommons.Textures;
using GagSpeak.Services;
using GagSpeak.State.Caches;
using Dalamud.Bindings.ImGui;
using OtterGui.Extensions;

namespace GagSpeak.CustomCombos.Editor;

public sealed class MoodleStatusCombo : CkMoodleComboBase<MoodlesStatusInfo>
{
    private Guid _currentItem;
    public MoodleStatusCombo(ILogger log, float iconScale)
        : base(log, iconScale, () => [.. MoodleCache.IpcData.StatusList.OrderBy(x => x.Title)])
    {
        _currentItem = Guid.Empty;
    }

    protected override string ToString(MoodlesStatusInfo obj)
        => obj.Title;

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
    public bool Draw(string label, float width)
    {
        InnerWidth = width + IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
        _currentItem = Current.GUID;
        return Draw(label, _currentItem, width);
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, Guid current, float width, CFlags flags = CFlags.None)
        => Draw(label, current, width, 1.0f, CFlags.None);

    public bool Draw(string label, Guid current, float width, float innerScaler, CFlags flags = CFlags.None)
    {
        InnerWidth = width * innerScaler + IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
        _currentItem = current;
        // Maybe there is a faster way to know this, but atm I do not know.
        var currentTitle = Items.FirstOrDefault(i => i.GUID == _currentItem).Title?.StripColorTags() ?? string.Empty;
        var previewName = currentTitle.IsNullOrWhitespace() ? "Select Moodle Status..." : currentTitle;
        return Draw($"##status{label}", previewName, string.Empty, width, IconSize.Y, flags);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var size = new Vector2(GetFilterWidth(), IconSize.Y);
        var titleSpace = size.X - IconSize.X;
        var moodleStatus = Items[globalIdx];
        var ret = ImGui.Selectable("##"+moodleStatus.Title, selected, ImGuiSelectableFlags.None, size);

        ImGui.SameLine(titleSpace);
        MoodleIcon.DrawMoodleIcon(moodleStatus.IconID, moodleStatus.Stacks, IconSize);
        DrawItemTooltip(moodleStatus);

        ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
        var pos = ImGui.GetCursorPosY();
        ImGui.SetCursorPosY(pos + (size.Y - SelectableTextHeight) * 0.5f);
        using (Fonts.Default150Percent.Push())
            CkRichText.Text(titleSpace, moodleStatus.Title);

        return ret;
    }
}
