using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;

namespace GagSpeak.CustomCombos.Editor;

/// <summary> Capable of displaying every valid emote, along with its icon and all command variants. </summary>
public sealed class JobCombo : CkFilterComboCache<LightJob>
{
    private readonly MoodleIcons _iconDrawer;
    private float _iconScale = 1.0f;
    public JobType _currentJob { get; private set; }
    public JobCombo(float iconScale, MoodleIcons disp, ILogger log)
        : base([ ..SpellActionService.BattleClassJobs ], log)
    {
        _iconScale = iconScale;
        _iconDrawer = disp;
        SearchByParts = true;
    }

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => filter.IsContained(Items[globalIndex].Name) || filter.IsContained(Items[globalIndex].Abbreviation);

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current.JobId == _currentJob)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.JobId == _currentJob);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return CurrentSelectionIdx;
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string id, JobType current, float width, uint? searchBg = null)
        => Draw(id, current, width, 1f, searchBg);
    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string id, JobType current, float width, float widthScaler = 1f, uint? searchBg = null, CFlags flags = CFlags.None)
    {
        InnerWidth = width * widthScaler;
        _currentJob = current;
        var currentItem = Items.FirstOrDefault(i => i.JobId == current);
        var preview = _currentJob != JobType.ADV ? $"{currentItem.Name} ({currentItem.Abbreviation})" : "Select Job...";
        return Draw(id, preview, string.Empty, width, ImGui.GetFrameHeight() * _iconScale, flags, searchBg);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var parsedJob = Items[globalIdx];

        // Draw a ghost selectable at first.
        var ret = false;
        var pos = ImGui.GetCursorPos();
        using (ImRaii.Group())
        {
            var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight() * _iconScale);
            ret = ImGui.Selectable("##LightJob" + globalIdx, selected, ImGuiSelectableFlags.None, size);
            // Use these positions to go back over and draw it properly this time.
            ImGui.SetCursorPos(pos);
            using (ImRaii.Group())
            {
                ImGui.Image(_iconDrawer.GetGameIconOrEmpty(parsedJob.GetIconId()).ImGuiHandle, new Vector2(size.Y));
                CkGui.TextFrameAlignedInline(parsedJob.Name);
                CkGui.ColorTextFrameAlignedInline($"({parsedJob.Abbreviation})", CkColor.ElementBG.Uint(), false);
            }
        }
        return ret;
    }

    protected override string ToString(LightJob lightJob) => lightJob.Name;

    protected override void OnClosePopup()
    {
        var split = Filter.Text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2 || !ushort.TryParse(split[0], out var setId) || !byte.TryParse(split[1], out var variant))
            return;
    }
}

