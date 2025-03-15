using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Classes;
using GagSpeak.Interop.Ipc;
using GagSpeak.Services;
using GagSpeak.UI;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Helpers;

public static class UiElements
{

    public static float ElementHeaderAndPaddingHeight()
    {
        return ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y * 2;
    }

    public static void DummyHeaderSpace()
    {
        //using var font = UiFontService.GagspeakLabelFont.Push();
        ImGui.Dummy(new Vector2(0, ImGui.GetTextLineHeight()));
    }

    /// <summary> Draws out the header label space for a component box. </summary>
    /// <remarks> For different size results, push the text size or font prior to draw. </remarks>
    public static void DrawElementLabel(string text, float width)
    {
        var height = ImGui.GetTextLineHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();
        // Draw out the rect that spans the width and height.
        ImGui.GetWindowDrawList().AddRectFilled(pos, pos + new Vector2(width, height), CkColor.ElementHeader.Uint(), width * 0.05f, ImDrawFlags.RoundCornersTop);
        ImGui.GetWindowDrawList().AddLine(pos + new Vector2(0, height - 2), pos + new Vector2(width, height - 2), CkColor.ElementSplit.Uint(), 2);
        ImGui.GetWindowDrawList().AddText(pos + new Vector2((width - ImGui.CalcTextSize(text).X) / 2, 0), 0xFFFFFFFF, text);
    }

    public static void DrawCheckboxIcon(FontAwesomeIcon icon, ref bool value)
    {
        using var group = ImRaii.Group();
        ImGui.Checkbox("##" + icon.ToString(), ref value);
        ImUtf8.SameLineInner();
        ImGui.AlignTextToFramePadding();
        CkGui.IconText(icon);
    }

    public static void DrawOptionalBoolToggle(string label, OptionalBool currentVal)
    {
    }




    public static void DrawModPresetPreview(ModSettings modSettings, Vector2 drawRegion)
    {
        // Create a child window here with 0 window padding, and draw out the mod settings.

    }
}

