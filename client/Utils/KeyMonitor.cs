using PInvoke;
using System.Windows.Forms;

namespace GagSpeak.Utils;

/// <summary> A class for all of the UI helpers, including basic functions for drawing repetative yet unique design elements </summary>
public static class KeyMonitor
{
    /// <summary>
    /// Checks to see if a key is pressed
    /// </summary>
    public static bool IsKeyPressed(int vKey)
    {
        // if it isnt any key just return false
        if (vKey is 0)
            return false;

        return IsBitSet(User32.GetAsyncKeyState(vKey), 15);
    }

    // see if the key bit is set
    public static bool IsBitSet(short b, int pos) => (b & (1 << pos)) != 0;

    public static bool ShiftPressed() => (IsKeyPressed(0xA1) || IsKeyPressed(0xA0));
    public static bool CtrlPressed() => (IsKeyPressed(0xA2) || IsKeyPressed(0xA3));
    public static bool AltPressed() => (IsKeyPressed(0xA4) || IsKeyPressed(0xA5));
    public static bool BackPressed() => IsKeyPressed(0x08);
    public static bool Numpad0Pressed() => IsKeyPressed(0x60);

    public static bool RightMouseButtonDown() => IsKeyPressed(0x02);
    public static bool MiddleMouseButtonDown() => IsKeyPressed(0x04);
    public static bool IsBothMouseButtonsPressed() => IsKeyPressed((int)Keys.LButton) && IsKeyPressed((int)Keys.RButton);

}
