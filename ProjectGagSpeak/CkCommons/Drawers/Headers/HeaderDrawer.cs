using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons;
using ImGuiNET;
using OtterGui.Text;
using System.Runtime.InteropServices;

namespace GagSpeak.UI.Components;

/// <summary> Helper for all functions related to drawing the header section of respective UI's </summary>
/// <remarks> Contains functions for icon row display, filters, and more. </remarks>
public class DrawerHelpers
{
    /// <summary> A struct that stores the draw region of a header component. </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct HeaderVec(float PosX, float PosY, float SizeX, float SizeY)
    {
        public Vector2 Pos  => new(PosX, PosY);
        public Vector2 Size => new(SizeX, SizeY);
        public Vector2 Max  => Pos + Size;
        public HeaderVec(Vector2 pos, Vector2 size)
            : this(pos.X, pos.Y, size.X, size.Y)
        { }
    }

    /// <summary> A struct to contain the 4 corner content regions provided around the header. </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct CkHeaderDrawRegions(HeaderVec Topleft, HeaderVec TopRight, HeaderVec BotLeft, HeaderVec BotRight);

    /// <summary> Helper function that draws a flat header title past the window padding to the window edge. </summary>
    public static CkHeaderDrawRegions FlatHeader(uint color, Vector2 innerSize, float leftWidth, float splitWidth)
    {
        var wdl = ImGui.GetWindowDrawList();
        var winPadding = ImGui.GetStyle().WindowPadding;
        var winClipX = ImGui.GetWindowContentRegionMin().X / 2;
        var minPos = wdl.GetClipRectMin();
        var maxPos = wdl.GetClipRectMax();
        var outerXoffset = Math.Abs(winClipX - winPadding.X);
        var paddedSize = innerSize + winPadding * 2;

        var expandedMin = minPos - new Vector2(winClipX, 0); // Extend the min boundary to include the padding
        var expandedMax = maxPos + new Vector2(winClipX, 0); // Extend the max boundary to include the padding
        wdl.PushClipRect(expandedMin, expandedMax, false);

        // Draw the base header, and the left region positions.
        wdl.AddRectFilled(expandedMin, expandedMin + paddedSize, color, 0, ImDrawFlags.None);
        var topLeftPos = minPos + new Vector2(outerXoffset, winPadding.Y);
        var botLeftPos = topLeftPos + new Vector2(0, paddedSize.Y);
        var botRegionH = maxPos.Y - botLeftPos.Y - winPadding.Y;

        // define the midpoint positions, and also our right positions after we know the divider.
        var splitPos = botLeftPos + new Vector2(leftWidth + winPadding.X, 0);
        var topRightPos = new Vector2(splitPos.X + splitWidth + winPadding.X, topLeftPos.Y);
        var botRightPos = topRightPos with { Y = botLeftPos.Y };

        wdl.AddRectFilled(splitPos, new Vector2(splitPos.X + splitWidth, maxPos.Y - winPadding.Y), CkColor.FancyHeader.Uint());
        wdl.PopClipRect();

        // we need to return the content region struct, so create our end result content regions below.
        var topLeft = new HeaderVec(topLeftPos, new Vector2(leftWidth, innerSize.Y));
        var botLeft = new HeaderVec(botLeftPos, new Vector2(leftWidth, botRegionH));
        var topRight = new HeaderVec(topRightPos, new Vector2(maxPos.X - outerXoffset - topRightPos.X, innerSize.Y));
        var botRight = new HeaderVec(botRightPos, new Vector2(maxPos.X - outerXoffset - botRightPos.X, botRegionH));
        return new(topLeft, topRight, botLeft, botRight);
    }

    /// <summary> Draws a flat-header beyond window padding with inverted rounded curves at the bottom. </summary>
    /// <remarks> This will ALWAYS span the width of the content region entirely. </remarks>
    public static (HeaderVec Top, HeaderVec Bottom) FlatHeaderWithCurve(uint color, float height, float curveRadius)
    {
        var wdl = ImGui.GetWindowDrawList();
        var winPadding = ImGui.GetStyle().WindowPadding;
        var winClipX = ImGui.GetWindowContentRegionMin().X / 2;
        var minPos = wdl.GetClipRectMin();
        var maxPos = wdl.GetClipRectMax();
        var outerXoffset = Math.Abs(winClipX - winPadding.X);
        
        var expandedMin = minPos - new Vector2(winClipX, 0); // Extend the min boundary to include the padding
        var expandedMax = maxPos + new Vector2(winClipX, 0); // Extend the max boundary to include the padding
        wdl.PushClipRect(expandedMin, expandedMax, false);

        // Get necessary positions.
        var paddedHeight = height + winPadding.Y * 2;
        var midpoint = (maxPos.X - minPos.X) / 2;
        var topLeftContentPos = minPos + new Vector2(outerXoffset, winPadding.Y);
        var botLeftContentPos = topLeftContentPos + new Vector2(0, paddedHeight);
        var topRightPos = expandedMax with { Y = expandedMin.Y };
        var circleLeftCenter = expandedMin + new Vector2(curveRadius, paddedHeight + curveRadius);
        var circleRightCenter = topRightPos + new Vector2(-curveRadius, paddedHeight + curveRadius);

        // Draw the left convex shape.
        wdl.PathClear();
        wdl.PathLineTo(expandedMin);
        wdl.PathArcTo(circleLeftCenter, curveRadius, float.Pi, 3 * float.Pi / 2);
        wdl.PathLineTo(expandedMin + new Vector2(midpoint, paddedHeight));
        wdl.PathLineTo(expandedMin + new Vector2(midpoint, 0));
        wdl.PathFillConvex(color);

        // Draw the right convex shape.
        wdl.PathClear();
        wdl.PathLineTo(topRightPos);
        wdl.PathArcTo(circleRightCenter, curveRadius, 2 * float.Pi, 3 * float.Pi / 2);
        wdl.PathLineTo(expandedMin + new Vector2(midpoint, paddedHeight));
        wdl.PathLineTo(expandedMin + new Vector2(midpoint, 0));
        wdl.PathFillConvex(color);

        wdl.PopClipRect();

        // prepare exports.
        var topContent = new HeaderVec(topLeftContentPos, new Vector2(maxPos.X - minPos.X, height));
        var botContent = new HeaderVec(botLeftContentPos, (maxPos - new Vector2(outerXoffset, winPadding.Y)) - botLeftContentPos);
        return (topContent, botContent);
    }


    /// <summary> A helper function that draws out the fancy curved header (not to be used for restraint sets) </summary>
    public static CkHeaderDrawRegions CurvedHeader(bool isEditing, uint color, Vector2 leftSizeInner, float splitWidth)
    {
        // Grab the window padding that is currently set.
        var wdl = ImGui.GetWindowDrawList();
        var winPadding = ImGui.GetStyle().WindowPadding;
        var winClipX = ImGui.GetWindowContentRegionMin().X / 2;
        var minPos = wdl.GetClipRectMin();
        var maxPos = wdl.GetClipRectMax();
        var outerXoffset = Math.Abs(winClipX - winPadding.X);
        var paddedLeftSize = leftSizeInner + winPadding * 2;
        var curveRadius = splitWidth / 2;
        var clipedOffset = new Vector2(outerXoffset, winPadding.Y);

        var expandedMin = minPos - new Vector2(winPadding.X / 2, 0); // Extend the min boundary to include the padding
        var expandedMax = maxPos + new Vector2(winPadding.X / 2, 0); // Extend the max boundary to include the padding
        wdl.PushClipRect(expandedMin, expandedMax, false);

        wdl.PathClear();
        // topright
        wdl.PathLineTo(expandedMax with { Y = expandedMin.Y });
        // top left.
        wdl.PathLineTo(expandedMin);
        // bottom left.
        var pointTwoPos = expandedMin + new Vector2(0, paddedLeftSize.Y);
        wdl.PathLineTo(pointTwoPos);

        var topLeftContentPos = minPos + clipedOffset;
        var botLeftContentPos = topLeftContentPos + new Vector2(0, paddedLeftSize.Y);
        var botContentH = maxPos.Y - winPadding.Y - botLeftContentPos.Y;

        //var pointThreePos = pointTwoPos + new Vector2(leftSize.X - splitWidth / 2, 0);
        var circleOneCenter = expandedMin + paddedLeftSize + new Vector2(-curveRadius, curveRadius);
        var circleTwoCenter = circleOneCenter + new Vector2(splitWidth, 0);

        // define the midpoint positions, and also our right positions after we know the divider.
        var splitPos = botLeftContentPos + new Vector2(leftSizeInner.X + winPadding.X, 0);
        var topRightContentPos = new Vector2(splitPos.X + curveRadius + winPadding.X, topLeftContentPos.Y);
        var botRightContentPos = topRightContentPos + new Vector2(0, leftSizeInner.Y + splitWidth + winPadding.Y + curveRadius);

        // left center curve.
        wdl.PathArcTo(circleOneCenter, curveRadius, -float.Pi / 2, 0, 16);
        wdl.PathArcTo(circleTwoCenter, curveRadius, float.Pi, float.Pi / 2, 16);

        // bottom right curve.
        var circleThreeCenter = new Vector2(expandedMax.X - splitWidth, pointTwoPos.Y + splitWidth*2);
        wdl.PathArcTo(circleThreeCenter, splitWidth, -float.Pi / 2, 0);
        wdl.PathLineTo(expandedMax with { Y = expandedMin.Y });
        wdl.PathFillConvex(color);

        // if we are not editing, draw the splitter.
        if (!isEditing)
        {
            // clear the path.
            wdl.PathClear();
            var circleFourCenter = circleTwoCenter + new Vector2(0, curveRadius);
            var originPoint = new Vector2(circleOneCenter.X + curveRadius, expandedMax.Y - winPadding.Y);
            // bottom left
            wdl.PathLineTo(originPoint);
            // top left? (maybe unessisary)
            //wdl.PathLineTo(originPoint with { Y = circleTwoCenter.Y + splitWidth / 4 });
            // draw an arc going from the left of the unit circle to the bottom.
            wdl.PathArcTo(circleFourCenter, curveRadius, float.Pi, float.Pi / 2);
            // bottom right
            wdl.PathLineTo(originPoint + new Vector2(curveRadius, 0));
            wdl.PathFillConvex(color);
        }

        wdl.PopClipRect();

        // we need to return the content region struct, so create our end result content regions below.
        var topLeft = new HeaderVec(topLeftContentPos, leftSizeInner);
        var botLeft = new HeaderVec(botLeftContentPos, new Vector2(leftSizeInner.X, botContentH));
        var topRight = new HeaderVec(topRightContentPos, new Vector2(maxPos.X - outerXoffset - topRightContentPos.X, leftSizeInner.Y + splitWidth));
        var botRight = new HeaderVec(botRightContentPos, maxPos - clipedOffset - botRightContentPos);
        return new(topLeft, topRight, botLeft, botRight);
    }

    // WIP - At the moment the clear text does not appear to do much, unsure why currently. Look into how otter clears text probably.
    public unsafe static bool FancySearchFilter(string id, float width, string tt, ref string str, uint textLen, float rWidth = 0f, Action? rButtons = null)
    {
        var needsFocus = false;
        var height = ImGui.GetTextLineHeight() + (ImGui.GetStyle().FramePadding.Y * 2);
        var searchWidth = width - CkGui.IconButtonSize(FAI.TimesCircle).X -
            ((rButtons is not null) ? (rWidth + ImGui.GetStyle().ItemInnerSpacing.X * 2) : ImGui.GetStyle().ItemSpacing.X*2);
        var size = new Vector2(width, height);
        var ret = false;

        using var group = ImRaii.Group();
        var pos = ImGui.GetCursorScreenPos();
        // Mimic a child window, becaquse if we use one, any button actions are blocked, and wont display the popups.
        ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, CkColor.FancyHeaderContrast.Uint(), 9f);

        if (!str.IsNullOrEmpty())
        {
            // push the color for the button to have an invisible bg.
            if (CkGui.IconButton(FAI.TimesCircle, inPopup: true))
            {
                str = string.Empty;
                needsClear = true;
                needsFocus = true;
            }
        }
        else
        {
            using (ImRaii.Disabled(true))
            {
                CkGui.IconButton(FAI.Search, inPopup: true);
            }
        }

        // String input
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(searchWidth);

        if (needsFocus)
        {
            ImGui.SetKeyboardFocusHere();
            needsFocus = false;
        }

        // the return value
        var localSearchStr = str;

        using (ImRaii.PushColor(ImGuiCol.FrameBg, 0x000000))
        {
            var flags = ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.NoUndoRedo | ImGuiInputTextFlags.CallbackAlways;
            ret = ImGui.InputText("##" + id, ref localSearchStr, textLen, flags, (data) =>
            {
                if (needsClear)
                {
                    needsClear = false;
                    localSearchStr = string.Empty;
                    // clear the search input buffer
                    data->BufTextLen = 0;
                    data->BufSize = 0;
                    data->CursorPos = 0;
                    data->SelectionStart = 0;
                    data->SelectionEnd = 0;
                    data->BufDirty = 1;
                }
                return 1;
            });
            CkGui.AttachToolTip(tt);
        }

        if (rButtons is not null)
        {
            ImUtf8.SameLineInner();
            rButtons();
        }

        str = localSearchStr;
        return ret;
    }

    public static bool needsClear = false;


    /*    private unsafe void DrawFilteredSearch(string id, float searchWidth, ref string searchString)
        {
            float height = ImGui.GetTextLineHeight() + (ImGui.GetStyle().FramePadding.Y * 2);
            Vector2 size = new Vector2(searchWidth, height);
            Vector2 pos = ImGui.GetCursorScreenPos();

            using var bg = ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBg));
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding);
            using var child = ImRaii.Child(id, size, false, WFlags.NoScrollbar | WFlags.NoScrollWithMouse);

            try
            {
                // if we want to draw the selected tag filters, we should probably draw them here.
                if (!searchString.IsNullOrEmpty())
                {
                    // push the color for the button to have an invisible bg.
                    if (CkGui.IconButton(FAI.TimesCircle, inPopup: true))
                    {
                        ClearSearchText();
                        _searchFilter.Clear();
                        TagFilter.Clear();
                        _searchNeedsFocus = true;
                        TryRefresh(true);
                    }

                    ImGui.PopStyleColor();
                }
                else
                {
                    using (ImRaii.Disabled(true)) CkGui.IconButton(FAI.Search, inPopup: true);
                }

                // Tags
                if (TagFilter.Tags != null)
                {
                    Tag? toRemove = null;
                    foreach (Tag tag in TagFilter.Tags)
                    {
                        ImGui.SameLine();
                        ImGui.SetCursorPosY(0);

                        if (ImBrio.DrawTag(tag))
                        {
                            toRemove = tag;
                        }
                    }

                    if (toRemove != null)
                    {
                        TagFilter.Tags.Remove(toRemove);
                        _searchNeedsFocus = true;
                        TryRefresh(true);
                    }
                }

                // String input
                ImGui.SameLine();
                ImGui.SetCursorPosY(0);
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

                if (_searchNeedsFocus)
                {
                    ImGui.SetKeyboardFocusHere();
                    _searchNeedsFocus = false;
                }

                using (ImRaii.PushColor(ImGuiCol.FrameBg, 0x000000))
                {
                    if (ImGui.InputText("###library_search_input", ref _searchText, 256,
                        ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.NoUndoRedo
                        | ImGuiInputTextFlags.CallbackAlways,
                        OnSearchFunc))
                    {
                        if (string.IsNullOrEmpty(_searchText))
                        {
                            _searchFilter.Query = null;
                        }
                        else
                        {
                            _searchFilter.Query = SearchUtility.ToQuery(_searchText);
                        }

                        TryRefresh(true);
                    }
                }

                _isSearchFocused = ImGui.IsItemActive();

                // TODO: Try to capture backspace keys to remove tags.

                if (!_isSearchFocused)
                {
                    _searchLostFocus++;
                }
                else
                {
                    _searchLostFocus = 0;
                }

                _searchSuggestPos = new Vector2(searchBarPosition.X, searchBarPosition.Y + searchBarHeight);
                _searchSuggestSize = new Vector2(searchBarWidth, searchBarHeight);
            }
            catch(Exception ex)
            {
                GagSpeak.StaticLog.Error("Error drawing search bar: " + ex);
            }
        }

        private void ClearSearchText()
        {
            _searchText = string.Empty;
            _searchTextNeedsClear = true;
        }

        private unsafe int OnSearchFunc(ImGuiInputTextCallbackData* data)
        {
            if (_searchTextNeedsClear)
            {
                _searchTextNeedsClear = false;
                _searchText = string.Empty;

                // clear the search input buffer
                data->BufTextLen = 0;
                data->BufSize = 0;
                data->CursorPos = 0;
                data->SelectionStart = 0;
                data->SelectionEnd = 0;
                data->BufDirty = 1;
            }

            return 1;
        }

        private void DrawSearchSuggest()
        {
            if ((_searchSuggestPos is null || _searchSuggestSize is null))
                return;

            if (_isSearchFocused || _isSearchSuggestFocused)
                _isSearchSuggestWindowOpen = true;

            if (_isSearchSuggestWindowOpen is false)
                return;

            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(7, 7)))
            {
                List<Tag> availableTags = GetAvailableTags(SearchUtility.ToQuery(_searchText));

                int trimmedTags = 0;
                if (availableTags.Count > MaxTagsInSuggest)
                {
                    trimmedTags = availableTags.Count - MaxTagsInSuggest;
                    availableTags = availableTags.GetRange(0, MaxTagsInSuggest);
                }

                int lineCount = 2;
                int itemsinLine = 0;
                foreach (var tag in availableTags)
                {
                    itemsinLine++;

                    float itemWidth = ImGui.CalcTextSize(tag.DisplayName).X + 10;
                    float nextX = itemsinLine * itemWidth;

                    if (nextX > _searchSuggestPos.Value.X)
                    {
                        lineCount++;
                        itemsinLine = 0;
                    }
                }

                ImGui.SetNextWindowPos(_searchSuggestPos.Value);

                using var window = ImRaii.Child("##library_search_suggest", new Vector2(_searchSuggestSize.Value.X, (_searchSuggestSize.Value.Y * lineCount) + 10), true,
                WFlags.NoTitleBar | WFlags.NoMove | WFlags.NoResize | WFlags.Tooltip |
                WFlags.NoFocusOnAppearing | WFlags.ChildWindow);
                {
                    if (window.Success)
                    {
                        bool hasContent = false;
                        // click tags
                        if (availableTags.Count > 0)
                        {
                            Tag? selected = ImBrio.DrawTags(availableTags);
                            if (selected != null)
                            {
                                TagFilter.Add(selected);
                                _searchNeedsFocus = true;

                                ClearSearchText();
                                TryRefresh(true);
                            }

                            if (trimmedTags > 0)
                            {
                                ImBrio.Text($"plus {trimmedTags} more tags...", 0x88FFFFFF);
                            }

                            hasContent = true;
                        }

                        // quick tag
                        if (availableTags.Count >= 1)
                        {
                            ImBrio.Text($"Press TAB to filter by tag \"{availableTags[0].DisplayName}\"", 0x88FFFFFF);
                            hasContent = true;

                            if (ImGui.IsKeyPressed(ImGuiKey.Tab))
                            {
                                TagFilter.Add(availableTags[0]);
                                ClearSearchText();
                                TryRefresh(true);
                            }
                        }

                        if (!hasContent)
                        {
                            ImBrio.Text($"Start typing to search...", 0x88FFFFFF);
                        }
                    }

                    _isSearchSuggestFocused = ImGui.IsWindowHovered();
                    _isSearchSuggestFocused = ImGui.IsWindowFocused();
                }
            }

            if (_searchLostFocus > 10 && !_searchNeedsFocus)
            {
                _isSearchSuggestWindowOpen = false;
            }
        }

        private List<Tag> GetAvailableTags(string[] query)
        {
            List<Tag> results = [];
            foreach (var tag in _allTags)
            {
                if (TagFilter.Tags?.Contains(tag) == true)
                    continue;

                if (query == null || tag.Search(query))
                {
                    results.Add(tag);
                }
            }

            return results;
        }*/
}
