using Dalamud.Interface;
using Dalamud.Interface.Utility;
using GagSpeak.CkCommons.Gui;
using GagSpeak.Localization;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace GagSpeak.CkCommons.Widgets;

/// <summary> Widget drawing out a group of tags from a csv. </summary>
public class TagCollection
{
    private const string HELP_TEXT = $"Keybinds:--SEP--"+
        "--NL----COL--[SHIFT + L-Click]--COL-- -> Shift entry to the left." +
        "--NL----COL--[SHIFT + R-Click]--COL-- -> Shift entry to the right." +
        "--NL----COL--[CTRL + R-Click]--COL-- -> Remove entry." +
        "--NL----COL--[L-Click]--COL-- -> Edit entry.";

    private const string HELP_TEXT_SHORT =
        "--COL--[SHIFT + L-Click]--COL-- -> Shift left\n" +
        "--NL----COL--[SHIFT + R-Click]--COL-- -> Shift right\n" +
        "--NL----COL--[CTRL + R-Click]--COL-- -> Remove\n" +
        "--NL----COL--[L-Click]--COL-- -> Edit";

    private string _latestString = string.Empty;
    private List<string> _latestStringTags = new List<string>();

    private string _currentTag = string.Empty;
    private int    _editIdx    = -1;
    private bool   _setFocus;

    /// <summary>
    ///     Updates the latest string and tag collection if the tags have changed.
    /// </summary>
    private void UpdateOrSetLatest(string csvString)
    {
        if (_latestString == csvString)
            return;

        _latestString = csvString;
        _latestStringTags = GetTagCollection(csvString).ToList();
    }

    /// <summary>
    ///     Updates the latest string and tag collection if the tags have changed.
    /// </summary>
    private void UpdateOrSetLatest(IEnumerable<string> tags)
    {
        var tagList = GetTagCollection(tags).ToList();
        if (_latestStringTags.SequenceEqual(tagList))
            return;

        _latestStringTags = tagList;
        _latestString = string.Join(", ", tagList);
    }

    /// <summary> 
    ///     Returns a collection of tags from a csv string.
    /// </summary>
    public IEnumerable<string> GetTagCollection(string csvString)
        => csvString.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0);

    /// <summary>
    ///     Returns a collection of tags from an array.
    /// </summary>
    public IEnumerable<string> GetTagCollection(IEnumerable<string> tags)
        => tags
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim());

    public void DrawTagsPreview(string id, string csvString)
    {
        UpdateOrSetLatest(csvString);
        DrawPreviewBase(id);
    }

    public void DrawTagsPreview(string id, IReadOnlyCollection<string> tags)
    {
        UpdateOrSetLatest(tags);
        DrawPreviewBase(id);
    }

    private void DrawPreviewBase(string id)
    {
        // provide unique ID
        using var _ = ImRaii.PushId(id);
        // Encapsulate all in a group.
        using var group = ImRaii.Group();
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

    public bool DrawHelpButtons(string csvString, out string updatedCsvString, bool showSort)
    {
        bool change = false;

        // Update the tags.
        UpdateOrSetLatest(csvString);

        // do the draws.
        var pos = ImGui.GetCursorPos();
        var available = ImGui.GetContentRegionAvail();
        var bottomRightPos = pos + available;
        var width = showSort ? ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.X : ImGui.GetFrameHeight();
        var iconSize = new Vector2(width, ImGui.GetFrameHeight());
        // Move and draw the icons.
        ImGui.SetCursorPos(bottomRightPos - iconSize);
        if (showSort)
        {
            if (CkGui.IconButton(FontAwesomeIcon.SortAlphaDown, inPopup: true))
            {
                _latestStringTags.Sort();
                change = true;
            }
            CkGui.AttachToolTip("Sort Tags Alphabetically");
        }

        ImGui.SameLine();
        CkGui.HelpText(HELP_TEXT, CkColor.VibrantPink.Uint());

        updatedCsvString = string.Join(", ", _latestStringTags);
        return change;
    }

    public bool DrawHelpButtons(IReadOnlyCollection<string> tags, out List<string> updatedTags, bool showSort)
    {
        bool change = false;

        // Update the tags.
        UpdateOrSetLatest(tags);

        // do the draws.
        var pos = ImGui.GetCursorPos();
        var available = ImGui.GetContentRegionAvail();
        var bottomRightPos = pos + available;
        var width = showSort ? ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.X : ImGui.GetFrameHeight();
        var iconSize = new Vector2(width, ImGui.GetFrameHeight());
        // Move and draw the icons.
        ImGui.SetCursorPos(bottomRightPos - iconSize);
        if(showSort)
        {
            if (CkGui.IconButton(FontAwesomeIcon.SortAlphaDown, inPopup: true))
            {
                _latestStringTags.Sort();
                change = true;
            }
            CkGui.AttachToolTip("Sort Tags Alphabetically");
        }

        ImGui.SameLine();
        CkGui.HelpText(HELP_TEXT, CkColor.VibrantPink.Uint());

        updatedTags = _latestStringTags;
        return change;
    }

    public bool DrawTagsEditor(string id, IReadOnlyCollection<string> tags, out List<string> updatedTags)
    {
        using var _ = ImRaii.PushId(id);
        using var group = ImRaii.Group();

        var color = ImGui.GetColorU32(ImGuiCol.Button);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 4 * ImGuiHelpers.GlobalScale });
        using var c = ImRaii.PushColor(ImGuiCol.ButtonHovered, color, false)
            .Push(ImGuiCol.ButtonActive, color, false)
            .Push(ImGuiCol.Button, color);

        UpdateOrSetLatest(tags);

        var x = ImGui.GetCursorPosX();
        ImGui.SetCursorPosX(x);
        var rightEndOffset = 4 * ImGuiHelpers.GlobalScale;

        var changed = DrawEditorCore(x, rightEndOffset);
        updatedTags = _latestStringTags.ToList();
        return changed;
    }

    public bool DrawTagsEditor(string id, string csvString, out string updatedCsvString)
    {
        using var _ = ImRaii.PushId(id);
        using var group = ImRaii.Group();

        var color = ImGui.GetColorU32(ImGuiCol.Button);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 4 * ImGuiHelpers.GlobalScale });
        using var c = ImRaii.PushColor(ImGuiCol.ButtonHovered, color, false)
            .Push(ImGuiCol.ButtonActive, color, false)
            .Push(ImGuiCol.Button, color);

        UpdateOrSetLatest(csvString);

        var x = ImGui.GetCursorPosX();
        ImGui.SetCursorPosX(x);
        var rightEndOffset = 4 * ImGuiHelpers.GlobalScale;

        var changed = DrawEditorCore(x, rightEndOffset);
        updatedCsvString = string.Join(", ", _latestStringTags);
        return changed;
    }

    private bool DrawEditorCore(float x, float rightEndOffset)
    {
        bool changeOccurred = false;
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
                    changeOccurred = true;
                }
            }
            else
            {
                SetPosButton(tag, x, rightEndOffset);
                Button(tag, idx, true);
                var lClicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);
                var rClicked = ImGui.IsItemClicked(ImGuiMouseButton.Right);
                CkGui.AttachToolTip(HELP_TEXT_SHORT, color: CkColor.VibrantPink.Vec4());
                // Rearrangement.
                if(ImGui.IsItemHovered() && KeyMonitor.ShiftPressed())
                {
                    if (idx > 0 && lClicked)
                    {
                        (_latestStringTags[idx], _latestStringTags[idx - 1]) = (_latestStringTags[idx - 1], _latestStringTags[idx]);
                        changeOccurred = true;
                    }
                    else if (idx < _latestStringTags.Count - 1 && rClicked)
                    {
                        (_latestStringTags[idx], _latestStringTags[idx + 1]) = (_latestStringTags[idx + 1], _latestStringTags[idx]);
                        changeOccurred = true;
                    }
                }
                // Handle Delete
                if (KeyMonitor.CtrlPressed() && rClicked)
                {
                    _latestStringTags.RemoveAt(idx);
                    _editIdx = -1;
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
            bool textChanged = ImGui.InputText($"##addEdit{_editIdx}", ref _currentTag, 128);
            bool tabPressed = ImGui.IsItemActive() && ImGui.IsKeyPressed(ImGuiKey.Tab, false);

            // Handle TAB press immediately, not on deactivation
            if (tabPressed)
            {
                if (!_latestStringTags.Contains(_currentTag, StringComparer.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(_currentTag))
                {
                    _latestStringTags.Add(_currentTag);
                    changeOccurred = true;
                }
                // Stay in add mode for next entry
                _editIdx = _latestStringTags.Count;
                _setFocus = true;
                _currentTag = string.Empty;
                return changeOccurred;
            }

            // Handle deactivation (e.g., when clicking outside or pressing Enter)
            if (ImGui.IsItemDeactivated())
            {
                if (_latestStringTags.Contains(_currentTag, StringComparer.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(_currentTag))
                {
                    _editIdx = -1;
                    _currentTag = string.Empty;
                    changeOccurred = false;
                }
                else
                {
                    _latestStringTags.Add(_currentTag);
                    _editIdx = -1;
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
        }

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
