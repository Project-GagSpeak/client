using ImGuiNET;

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Create a non-existent item taking up the given width. </summary>
    /// <param name="width"> The width. </param>
    public static void Dummy(float width)
        => ImGuiNative.igDummy(new Vector2(width, 0));

    /// <summary> Create a non-existent item taking up the given width and height. </summary>
    /// <param name="width"> The width. </param>
    /// <param name="height"> The height. </param>
    public static void Dummy(float width, float height)
        => ImGuiNative.igDummy(new Vector2(width, height));

    /// <summary> Create a non-existent item taking up the given width and height. </summary>
    /// <param name="size"> The size. </param>
    public static void Dummy(Vector2 size)
        => ImGuiNative.igDummy(size);

    /// <summary> Create a non-existent item taking up the given width scaled by the global scale. </summary>
    /// <param name="width"> The unscaled width. </param>
    public static void ScaledDummy(float width)
        => ImGuiNative.igDummy(new Vector2(width * GlobalScale, 0));

    /// <summary> Create a non-existent item taking up the given width and height scaled by the global scale. </summary>
    /// <param name="width"> The unscaled width. </param>
    /// <param name="height"> The unscaled height. </param>
    public static void ScaledDummy(float width, float height)
        => ImGuiNative.igDummy(new Vector2(width * GlobalScale, height * GlobalScale));

    /// <summary> Create a non-existent item taking up the given width and height scaled by the global scale. </summary>
    /// <param name="size"> The unscaled size. </param>
    public static void ScaledDummy(Vector2 size)
        => ImGuiNative.igDummy(size * GlobalScale);
}
