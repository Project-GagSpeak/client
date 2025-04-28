using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Widgets;

public class FancySearchBar
{
    // WIP - At the moment the clear text does not appear to do much, unsure why currently. Look into how otter clears text probably.
    public unsafe static bool Draw(string id, float width, string tt, ref string str, uint textLen, float rWidth = 0f, Action? rButtons = null)
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
}
