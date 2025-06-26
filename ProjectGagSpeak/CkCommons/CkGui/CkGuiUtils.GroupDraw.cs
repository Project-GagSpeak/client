using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Raii;
using ImGuiNET;
using OtterGui.Text;
using OtterGui.Widgets;

namespace GagSpeak.Gui.Utility;

// Mimics the penumbra mod groups from penumbra mod selection.
public static partial class CkGuiUtils
{
    public static void FramedEditDisplay(string id, float width, bool inEdit, string curLabel,
        Action<float> drawAct, uint editorBg = 0, float? height = null)
    {
        var col = inEdit ? editorBg : CkColor.FancyHeaderContrast.Uint();
        using (CkRaii.Child(id + "frameDisp", new Vector2(width, height ?? ImGui.GetFrameHeight()), col, 
            CkStyle.ChildRounding(), DFlags.RoundCornersAll))
        {
            if (inEdit)
                drawAct?.Invoke(width);
            else
                CkGui.CenterTextAligned(curLabel);
        }
    }


    /// <summary> Draw a single group selector as a combo box. (For Previewing) </summary>
    public static void DrawSingleGroupCombo(string groupName, string[] options, string current)
    {
        var comboWidth = ImGui.GetContentRegionAvail().X / 2;
        StringCombo(groupName, comboWidth, current, out _, options.AsEnumerable(), "None Selected...");
    }
    /// <summary> Draw a single group selector as a set of radio buttons. (for Previewing) </summary>
    public static void DrawSingleGroupRadio(string groupName, string[] options, string current)
    {
        var newSelection = current; // Ensure assignment
        using var id = ImUtf8.PushId(groupName);
        var minWidth = Widget.BeginFramedGroup(groupName);

        using (ImRaii.Disabled(false))
        {
            for (var idx = 0; idx < options.Length; ++idx)
            {
                using var i = ImUtf8.PushId(idx);
                var option = options[idx];
                if (ImUtf8.RadioButton(option, current == option))
                    newSelection = option;
            }
        }
        Widget.EndFramedGroup();
    }

    /// <summary> Draw a multi group selector as a bordered set of checkboxes. (for previewing) </summary>
    public static void DrawMultiGroup(string groupName, string[] options, string[] current)
    {
        using var id = ImUtf8.PushId(groupName);
        var minWidth = Widget.BeginFramedGroup(groupName);

        using (ImRaii.Disabled(false))
        {
            for (var idx = 0; idx < options.Length; ++idx)
            {
                using var i = ImUtf8.PushId(idx);
                var option = options[idx];
                var isSelected = current.Contains(option);
                ImUtf8.Checkbox(option, ref isSelected);
            }
        }
        Widget.EndFramedGroup();
    }
}
