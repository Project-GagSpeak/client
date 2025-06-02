using ImGuiNET;
using OtterGui.Raii;

namespace GagSpeak.CkCommons.Gui.Clip;

// ClippedDraw Methods are taken from OtterGui's ImGuiClip func, and modified to allow for a width parameter.
public static partial class CkGuiClip
{
    /// <summary>
    ///     A variant of ImGuiClip that accepts the width paramater to define drawlength.
    /// </summary>
    public static int FilteredClippedDraw<T>(IEnumerable<T> data, int skips, Func<T, bool> checkFilter, Action<T, float> draw, float? width = null)
        => ClippedDraw(data.Where(checkFilter), skips, draw, width);

    /// <summary>
    ///    A variant of ImGuiClip that accepts the width paramater to define drawlength.
    /// </summary>
    public static int ClippedDraw<T>(IEnumerable<T> data, int skips, Action<T, float> draw, float? width = null)
    {
        using IEnumerator<T> enumerator = data.GetEnumerator();
        bool flag = false;
        int num = 0;
        float usedWidth = width ?? ImGui.GetContentRegionAvail().X;
        while (enumerator.MoveNext())
        {
            if (num >= skips)
            {
                using ImRaii.IEndObject endObject = ImRaii.Group();
                draw(enumerator.Current, usedWidth);
                endObject.Dispose();
                if (!ImGui.IsItemVisible())
                {
                    if (flag)
                    {
                        int num2 = 0;
                        while (enumerator.MoveNext())
                        {
                            num2++;
                        }

                        return num2;
                    }
                }
                else
                {
                    flag = true;
                }
            }

            num++;
        }

        return ~num;
    }

}
