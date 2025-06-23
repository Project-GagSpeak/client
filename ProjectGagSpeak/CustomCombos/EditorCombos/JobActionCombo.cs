using GagSpeak.CkCommons.Gui;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui;
using OtterGui.Raii;
using GagSpeak.CkCommons;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;

namespace GagSpeak.CustomCombos.Editor;

/// <summary> Capable of displaying every valid emote, along with its icon and all command variants. </summary>
public sealed class JobActionCombo : CkFilterComboCache<ParsedActionRow>
{
    private readonly MoodleIcons _iconDrawer;
    private float _iconScale = 1.0f;
    private uint _currentActionId;

    // Need a generator to make sure it reflects the selected job and stuffies.
    public JobActionCombo(float iconScale, MoodleIcons disp, ILogger log, 
        Func<IReadOnlyList<ParsedActionRow>> gen) : base(gen, log)
    {
        _iconScale = iconScale;
        _iconDrawer = disp;
        SearchByParts = true;
    }

    public void RefreshActionList()
        => RefreshCombo();

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => filter.IsContained(Items[globalIndex].Name) 
        || filter.IsContained(Items[globalIndex].ActionID.ToString());

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current.ActionID == _currentActionId)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.ActionID == _currentActionId);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return CurrentSelectionIdx;
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string id, uint current, float width, uint? searchBg = null)
        => Draw(id, current, width, 1f, searchBg);
    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string id, uint current, float width, float widthScaler = 1f, uint? searchBg = null, CFlags flags = CFlags.None)
    {
        InnerWidth = width * widthScaler;
        _currentActionId = current;
        var itemIdx = Items.IndexOf(i => i.ActionID == _currentActionId);
        var preview = itemIdx >= 0 ? $"{Items[itemIdx].Name} ({_currentActionId})" : "Select Action...";
        return Draw(id, preview, string.Empty, width, ImGui.GetFrameHeight() * _iconScale, flags, searchBg);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var parsedJobAction = Items[globalIdx];

        // Draw a ghost selectable at first.
        // Draw a ghost selectable at first.
        var ret = false;
        var pos = ImGui.GetCursorPos();
        using (ImRaii.Group())
        {
            var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight() * _iconScale);
            ret = ImGui.Selectable("##JobAction" + globalIdx, selected, ImGuiSelectableFlags.None, size);
            // Use these positions to go back over and draw it properly this time.
            ImGui.SetCursorPos(pos);
            using (ImRaii.Group())
            {
                ImGui.Image(_iconDrawer.GetGameIconOrEmpty(parsedJobAction.IconID).ImGuiHandle, new Vector2(size.Y));
                CkGui.TextFrameAlignedInline(parsedJobAction.Name);
                CkGui.ColorTextFrameAlignedInline($"({parsedJobAction.ActionID})", CkColor.ElementBG.Uint(), false);
            }
        }
        return ret;
    }

    protected override string ToString(ParsedActionRow jobAct)
        => jobAct.Name;

    protected override void OnClosePopup()
    {
        var split = Filter.Text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2 || !ushort.TryParse(split[0], out var setId) || !byte.TryParse(split[1], out var variant))
            return;
    }
}

