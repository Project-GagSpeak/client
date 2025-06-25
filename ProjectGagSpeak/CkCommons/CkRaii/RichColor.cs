using Dalamud.Interface;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    public static RichColor PushColor(CkRichCol idx, uint color, bool condition = true)
    => new RichColor().Push(idx, color, condition);

    public static RichColor PushColor(CkRichCol idx, Vector4 color, bool condition = true)
        => new RichColor().Push(idx, color, condition);

    /// <summary>
    ///     A baby version of ImRaii.Color that handles both text and text stroke.
    ///     This helps aid in the creation of rich text caching and rendering.
    /// </summary>
    public sealed class RichColor : IDisposable
    {
        // internal cache containing the original color values.
        internal static readonly Dictionary<CkRichCol, Stack<uint>> ColorStack = new();

        public bool Contains(CkRichCol idx)
            => ColorStack.ContainsKey(idx);
        
        /// <summary> Get the color or a fallback if not present. </summary>
        public uint GetColor(CkRichCol kind, uint fallback)
            => ColorStack.TryGetValue(kind, out var c) && c.Count > 0 ? c.Peek() : fallback;

        public RichColor Push(CkRichCol idx, uint color, bool condition = true)
        {
            if (!condition)
                return this;

            if (!ColorStack.TryGetValue(idx, out var colors))
            {
                colors = new Stack<uint>();
                ColorStack[idx] = colors;
            }
            // push it onto the stack.
            colors.Push(color);

            // if text, push the style color.
            if (idx is CkRichCol.Text)
                ImGui.PushStyleColor(ImGuiCol.Text, color);

            return this;
        }

        public RichColor Push(CkRichCol idx, Vector4 color, bool condition = true)
        {
            if (!condition)
                return this;

            if (!ColorStack.TryGetValue(idx, out var colors))
            {
                colors = new Stack<uint>();
                ColorStack[idx] = colors;
            }
            // push it onto the stack.
            var uintCol = ColorHelpers.RgbaVector4ToUint(color);
            colors.Push(uintCol);

            // if text, push the style color.
            if (idx is CkRichCol.Text)
                ImGui.PushStyleColor(ImGuiCol.Text, uintCol);

            return this;
        }

        public void Pop(CkRichCol idx, int num = 1)
        {
            if(!ColorStack.TryGetValue(idx, out var uintStack))
                return;
            
            // ensure we do not pop more than we have.
            num = Math.Min(num, uintStack.Count);

            // if the idx is a text color, pop the style color.
            if (idx is CkRichCol.Text)
                ImGui.PopStyleColor(num);

            // pop off the colors from the stack.
            var colors = uintStack;
            for (var i = 0; i < num; ++i)
                if (colors.Count > 0)
                    colors.Pop();
        }

        public void Dispose()
        {
            // pop all colors for each key.
            foreach (var (key, colorStack) in ColorStack)
                this.Pop(key, colorStack.Count);
        }
    }
}
// Limited color enum for our personal richColor.
public enum CkRichCol : byte
{
    Text,
    Stroke,
}
