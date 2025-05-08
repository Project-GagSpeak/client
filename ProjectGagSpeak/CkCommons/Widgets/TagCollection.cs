using Dalamud.Interface;
using Dalamud.Interface.Utility;
using GagSpeak.CkCommons.Gui;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace GagSpeak.CkCommons.Widgets;

/// <summary> Widget drawing out a group of tags from a csv. </summary>
public class TagCollection
{
    private string _latestString = string.Empty;
    private List<string> _latestStringTags = new List<string>();

    private string _currentTag = string.Empty;
    private int    _editIdx    = -1;
    private bool   _setFocus;

    private void UpdateOrSetLatest(string csvString)
    {
        if (_latestString == csvString)
            return;

        _latestString = csvString;
        _latestStringTags = GetTagCollection(csvString).ToList();
    }

    public string CurrentString => _latestString;

    /// <summary> 
    ///     Returns a collection of tags from a csv string.
    /// </summary>
    public IEnumerable<string> GetTagCollection(string csvString)
        => csvString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).Order();

    public void DrawTagsPreview(string uniqueId, string csvString)
    {
        // provide unique ID
        using var id = ImRaii.PushId(uniqueId);
        // Encapsulate all in a group.
        using var group = ImRaii.Group();

        // update latest.
        UpdateOrSetLatest(csvString);

        // Grab the correct x position.
        var x = ImGui.GetCursorPosX();
        ImGui.SetCursorPosX(x);

        var color = ImGui.GetColorU32(ImGuiCol.FrameBg);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 4 * ImGuiHelpers.GlobalScale });
        using var c = ImRaii.PushColor(ImGuiCol.ButtonHovered, color)
            .Push(ImGuiCol.ButtonActive, color)
            .Push(ImGuiCol.Button, color);

        // Add some padding to the right end offset.
        var rightEndOffset = 4 * ImGuiHelpers.GlobalScale;

        // Draw the tags.
        foreach (var (tag, idx) in _latestStringTags.WithIndex())
        {
            using var id2 = ImRaii.PushId(idx);

            SetPosButton(tag, x, rightEndOffset);
            Button(tag, idx, false);
            ImGui.SameLine();
        }
    }


    /// <summary> Draws the editor variant of the tag collection. </summary>
    /// <returns> If anything was updated. </returns>
    public bool DrawTagsEditor(string uniqueId, string csvStringRef, out string updatedCsvString)
    {
        // provide unique ID
        using var id  = ImRaii.PushId(uniqueId);
        // Encapsulate all in a group.
        using var group = ImRaii.Group();

        var color = ImGui.GetColorU32(ImGuiCol.Button);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 4 * ImGuiHelpers.GlobalScale });
        using var c = ImRaii.PushColor(ImGuiCol.ButtonHovered, color, false)
            .Push(ImGuiCol.ButtonActive, color, false)
            .Push(ImGuiCol.Button, color);

        // update latest.
        UpdateOrSetLatest(csvStringRef);
        updatedCsvString = _latestString;

        // Grab the correct x position.
        var x = ImGui.GetCursorPosX();
        ImGui.SetCursorPosX(x);
        var rightEndOffset = 4 * ImGuiHelpers.GlobalScale;

        bool changeOccurred = false;

        // Draw the tags.
        var tagGroupClone = _latestStringTags.ToList();
        foreach (var (tag, idx) in tagGroupClone.WithIndex())
        {
            using var id2 = ImRaii.PushId(idx);
            if (_editIdx == idx)
            {
                var width = SetPosText(_currentTag, x);
                SetFocus();

                ImGui.SetNextItemWidth(width);
                ImGui.InputText("##edit", ref _currentTag, 128);
                if (ImGui.IsItemDeactivated())
                {
                    _latestStringTags[idx] = _currentTag;
                    _editIdx = -1;
                    updatedCsvString = string.Join(", ", _latestStringTags);
                    changeOccurred = true;
                }
            }
            else
            {
                SetPosButton(tag, x, rightEndOffset);
                Button(tag, idx, true);
                CkGui.AttachToolTip("Left-Click to modify entry." +
                    "--SEP--CTRL + Right-Click to delete entry.");

                if(ImGui.GetIO().KeyCtrl && ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    _latestStringTags.RemoveAt(idx);
                    updatedCsvString = string.Join(", ", _latestStringTags);
                    changeOccurred = true;
                }
            }

            ImGui.SameLine();
        }

        if (_editIdx == _latestStringTags.Count)
        {
            var width = SetPosText(_currentTag, x);
            SetFocus();

            ImGui.SetNextItemWidth(width);
            ImGui.InputText("##addEdit", ref _currentTag, 128);
            if (ImGui.IsItemDeactivated())
            {
                // do not add if duplicate.
                if (_latestStringTags.Contains(_currentTag, StringComparer.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(_currentTag))
                {
                    _editIdx = -1;
                    _currentTag = string.Empty;
                    changeOccurred = false;
                }
                else
                {
                    _latestStringTags.Add(_currentTag);
                    _latestStringTags.Sort();
                    _editIdx = -1;
                    updatedCsvString = string.Join(", ", _latestStringTags);
                    changeOccurred = true;
                }
            }
        }
        else
        {
            SetPos(ImGui.GetFrameHeight(), x, rightEndOffset);
            if (CkGui.IconButton(FontAwesomeIcon.Plus, inPopup: true))
            {
                _editIdx = _latestStringTags.Count;
                _setFocus = true;
                _currentTag = string.Empty;
            }
            CkGui.AttachToolTip("Add Tag...");

            if (changeOccurred)
                _latestString = string.Join(", ", _latestStringTags);

            return changeOccurred;
        }

        if(changeOccurred)
            _latestString = string.Join(", ", _latestStringTags);
        
        return changeOccurred;
    }

    private void SetFocus()
    {
        if (!_setFocus)
            return;

        ImGui.SetKeyboardFocusHere();
        _setFocus = false;
    }

    private static float SetPos(float width, float x, float rightEndOffset = 0)
    {
        if (width + ImGui.GetStyle().ItemSpacing.X >= ImGui.GetContentRegionAvail().X - rightEndOffset)
        {
            ImGui.NewLine();
            ImGui.SetCursorPosX(x);
        }

        return width;
    }

    private static float SetPosButton(string tag, float x, float rightEndOffset = 0)
        => SetPos(ImGui.CalcTextSize(tag).X + ImGui.GetStyle().FramePadding.X * 2, x, rightEndOffset);

    private static float SetPosText(string tag, float x)
        => SetPos(ImGui.CalcTextSize(tag).X + ImGui.GetStyle().FramePadding.X * 2 + 15 * ImGuiHelpers.GlobalScale, x);

    private void Button(string tag, int idx, bool editable)
    {
        if (!ImGui.Button(tag) || !editable)
            return;

        _editIdx    = idx;
        _setFocus   = true;
        _currentTag = tag;
    }
}
