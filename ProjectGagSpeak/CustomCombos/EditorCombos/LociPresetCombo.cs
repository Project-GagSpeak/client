using CkCommons.Helpers;
using CkCommons.RichText;
using CkCommons.Textures;
using GagSpeak.Services;
using GagSpeak.State.Caches;
using Dalamud.Bindings.ImGui;
using OtterGui.Extensions;
using OtterGui.Text;
using GagSpeak.Interop.Helpers;

namespace GagSpeak.CustomCombos.Editor;

public sealed class LociPresetCombo : CkLociComboBase<LociPresetInfo>
{
    private int MaxStatuses =>
        LociCache.Data.Presets.Count > 0 ? Math.Clamp(LociCache.Data.Presets.Values.Max(x => x.Statuses.Count), 1, 10): 1;
    private Guid _currentItem;
    private float IconWithPadding => IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
    public LociPresetCombo(ILogger log, float iconScale)
        : base(log, iconScale, () => [ .. LociCache.Data.Presets.Values.OrderBy(x => x.Title)])
    {
        SearchByParts = false;
    }


    protected override string ToString(LociPresetInfo obj)
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
        InnerWidth = width + IconWithPadding * MaxStatuses;
        _currentItem = current;
        // Maybe there is a faster way to know this, but atm I do not know.
        var currentTitle = Items.FirstOrDefault(i => i.GUID == _currentItem).Title?.StripColorTags() ?? string.Empty;
        var preview = currentTitle.IsNullOrWhitespace() ? "Select Loci Preset..." : currentTitle;
        return Draw($"##preset{label}", preview, string.Empty, width, LociIcon.Size.Y, flags);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var lociPreset = Items[globalIdx];
        var size = new Vector2(GetFilterWidth(), IconSize.Y);
        var iconsSpace = (IconWithPadding * lociPreset.Statuses.Count);
        var titleSpace = size.X - iconsSpace;
        var ret = ImGui.Selectable($"##{lociPreset.Title}", selected, ImGuiSelectableFlags.None, size);

        // don't return early if 0 statuses in the preset otherwise the title isn't shown!
        //if (lociPreset.Statuses.Count <= 0)
        //    return ret;

        ImGui.SameLine(titleSpace);
        for (int i = 0, iconsDrawn = 0; i < lociPreset.Statuses.Count; i++)
        {
            var status = lociPreset.Statuses[i];
            if (!LociCache.Data.Statuses.TryGetValue(status, out var info))
            {
                ImGui.SameLine(0, IconWithPadding);
                continue;
            }

            LociIcon.Draw(info.IconID, info.Stacks, IconSize);
            LociHelpers.AttachTooltip(info, LociCache.Data);

            if (++iconsDrawn < lociPreset.Statuses.Count)
                ImUtf8.SameLineInner();
        }

        ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
        var pos = ImGui.GetCursorPosY();
        ImGui.SetCursorPosY(pos + (size.Y - SelectableTextHeight) * 0.5f);
        using (Fonts.Default150Percent.Push())
            CkRichText.Text(titleSpace, lociPreset.Title);
        return ret;
    }
}
