using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerClient;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Editor;

public sealed class RestraintCombo : CkFilterComboCache<RestraintSet>
{
    private readonly FavoritesManager _favorites;
    public Guid _currentRestraint { get; private set; }
    public RestraintCombo(ILogger log, FavoritesManager favorites, Func<IReadOnlyList<RestraintSet>> restraintsGenerator)
        : base(restraintsGenerator, log)
    {
        _favorites = favorites;
        _currentRestraint = Guid.Empty;
        SearchByParts = true;
    }

    protected override string ToString(RestraintSet obj)
        => obj.Label;

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current?.Identifier == _currentRestraint)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.Identifier == _currentRestraint);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : null;
        return CurrentSelectionIdx;
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, Guid current, float width, uint? customSearchBg = null)
        => Draw(label, current, width, CFlags.None, customSearchBg);

    public bool Draw(string label, Guid current, float width, CFlags flags, uint? customSearchBg = null)
    {
        InnerWidth = width * 1.25f;
        _currentRestraint = current;
        var preview = Items.FirstOrDefault(i => i.Identifier == current)?.Label ?? "Select Restraint...";
        return Draw(label, preview, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags, customSearchBg);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var restraint = Items[globalIdx];

        if (Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restraint, restraint.Identifier, false) && CurrentSelectionIdx == globalIdx)
        {
            CurrentSelectionIdx = -1;
            Current = default;
        }
        ImUtf8.SameLineInner();
        var ret = ImGui.Selectable(restraint.Label, selected);
        return ret;
    }
    private void DrawThresholdPercent(float width, IThresholdContainer trigger, bool isEditing, string? tt = null, string format = "%d%%")
    {
        var col = isEditing ? 0 : CkColor.FancyHeaderContrast.Uint();
        using (var c = CkRaii.Child("Perc_Thres", new Vector2(width, ImGui.GetFrameHeight()), col, CkStyle.ChildRounding(), ImDrawFlags.RoundCornersAll))
        {
            var healthPercentRef = trigger.ThresholdMinValue;
            if (isEditing)
            {
                ImGui.SetNextItemWidth(c.InnerRegion.X);
                if (ImGui.DragInt("##HealthPercentage", ref healthPercentRef, 0.1f, 0, 100, format))
                    trigger.ThresholdMinValue = healthPercentRef;
            }
            else
            {
                CkGui.CenterTextAligned($"{healthPercentRef}%");
            }
        }
        CkGui.AttachToolTip(tt ?? "Maximum Percent Damage/Heal number to trigger effect.");
    }

    private void DrawThresholds(float width, IThresholdContainer trigger, bool isEditing, string? lowerTT = null,
        string? upperTT = null, string lowerFormat = "%d", string upperFormat = "%d")
    {
        var col = isEditing ? 0 : CkColor.FancyHeaderContrast.Uint();
        var length = (width - ImGui.GetStyle().ItemInnerSpacing.X) / 2;
        using (var c = CkRaii.Child("MinThreshold", new Vector2(length, ImGui.GetFrameHeight()), col, CkStyle.ChildRounding(), ImDrawFlags.RoundCornersAll))
        {
            var minThresRef = trigger.ThresholdMinValue;
            if (isEditing)
            {
                ImGui.SetNextItemWidth(c.InnerRegion.X);
                if (ImGui.DragInt("##MinThresSlider", ref minThresRef, 10.0f, -1, 1000000, lowerFormat))
                    trigger.ThresholdMinValue = minThresRef;
            }
            else
            {
                var displayStr = lowerFormat.Replace("%d", minThresRef.ToString());
                CkGui.CenterTextAligned(displayStr);
            }
        }
        CkGui.AttachToolTip(lowerTT ?? "Minimum Damage/Heal number to trigger effect.\nLeave -1 for any.");

        ImUtf8.SameLineInner();
        using (var c = CkRaii.Child("MaxThreshold", new Vector2(length, ImGui.GetFrameHeight()), col, CkStyle.ChildRounding(), ImDrawFlags.RoundCornersAll))
        {
            var maxThresRef = trigger.ThresholdMaxValue;
            if (isEditing)
            {
                ImGui.SetNextItemWidth(c.InnerRegion.X);
                if (ImGui.DragInt("##MaxThresSlider", ref maxThresRef, 10.0f, -1, 1000000, upperFormat))
                    trigger.ThresholdMaxValue = maxThresRef;
            }
            else
            {
                var displayStr = upperFormat.Replace("%d", maxThresRef.ToString());
                CkGui.CenterTextAligned(displayStr);
            }
        }
        CkGui.AttachToolTip(upperTT ?? "Maximum Damage/Heal number to trigger effect.");
    }
}
