using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility;
using GagSpeak.Services.Textures;
using ImGuiNET;
using OtterGui;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace GagSpeak.Utils;

public static class CkTypeExtentions
{
    /// <summary> Attempts to retrieve the first element from the list that matches the specified predicate. </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to search for an element that matches the predicate.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="item">
    /// When this method returns, contains the first element from the list that matches the predicate, if found; 
    /// otherwise, the default value of <typeparamref name="T" />.
    /// </param>
    /// <returns> <see langword="true"/> if an element that matches the predicate is found; otherwise, <see langword="false"/>. </returns>
    public static bool TryGetItem<T>(this List<T> list, Func<T, bool> predicate, [MaybeNullWhen(false)] out T item)
    {
        item = list.FirstOrDefault(predicate);
        return item is not null;
    }

    /// <summary> Attempts to retrieve the first element from the list that matches the specified predicate. </summary>
    /// <typeparam name="TKey"> The type of the keys in the dictionary. </typeparam>
    /// <typeparam name="TValue"> The type of the values in the dictionary. </typeparam>
    /// <param name="dictionary"> The dictionary to search for an element that matches the predicate. </param>
    /// <param name="key"> The key to search for. </param>
    /// <param name="value"> When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value of <typeparamref name="TValue" />. </param>
    /// <returns> <see langword="true"/> if the dictionary contains an element with the specified key; otherwise, <see langword="false"/>. </returns>
    public static bool TryGetValueOrFirst<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (dictionary.TryGetValue(key, out value))
            return true;

        if (dictionary.Count > 0)
        {
            value = dictionary.First().Value;
            return true;
        }

        value = default;
        return false;
    }
}



public static class UtilsExtensions
{
    public static string ComboEmoteName(this Lumina.Excel.Sheets.Emote emote)
    {
        return emote.Name.ExtractText().Replace("\u00AD", "") + "(" + emote.RowId + ")";
    }
    /// <summary> Draw a game icon display (not icon button or anything) </summary>
    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size, EquipSlot slot, bool doHover = true)
    {
        try
        {
            var isEmpty = item.PrimaryId.Id == 0;
            var (ptr, textureSize, empty) = textures.GetIcon(item, slot);
            if (empty)
            {
                var (bgColor, tint) = isEmpty
                    ? (ImGui.GetColorU32(ImGuiCol.FrameBg), Vector4.One)
                    : (ImGui.GetColorU32(ImGuiCol.FrameBgActive), new Vector4(0.3f, 0.3f, 0.3f, 1f));
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, bgColor, 5 * ImGuiHelpers.GlobalScale);
                if (ptr != nint.Zero)
                    ImGui.Image(ptr, size, Vector2.Zero, Vector2.One, tint);
                else
                    ImGui.Dummy(size);
            }
            else
            {
                ImGui.Image(ptr, size);
                if (doHover) ImGuiUtil.HoverIconTooltip(ptr, size, textureSize);
            }
        }
        catch (Exception e)
        {
            StaticLogger.Logger.LogError(e, "Error drawing icon");
        }
    }

    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size, BonusItemFlag slot)
    {
        var isEmpty = item.PrimaryId.Id == 0;
        var (ptr, textureSize, empty) = textures.GetIcon(item, slot);
        if (empty)
        {
            var (bgColor, tint) = isEmpty
                ? (ImGui.GetColorU32(ImGuiCol.FrameBg), Vector4.One)
                : (ImGui.GetColorU32(ImGuiCol.FrameBgActive), new Vector4(0.3f, 0.3f, 0.3f, 1f));
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, bgColor, 5 * ImGuiHelpers.GlobalScale);
            if (ptr != nint.Zero)
                ImGui.Image(ptr, size, Vector2.Zero, Vector2.One, tint);
            else
                ImGui.Dummy(size);
        }
        else
        {
            ImGuiUtil.HoverIcon(ptr, textureSize, size);
        }
    }

    public static string ExtractText(this SeString seStr, bool onlyFirst = false)
    {
        StringBuilder sb = new();
        foreach (var x in seStr.Payloads)
        {
            if (x is TextPayload tp)
            {
                sb.Append(tp.Text);
                if (onlyFirst) break;
            }
            if (x.Type == PayloadType.Unknown && x.Encode().SequenceEqual<byte>([0x02, 0x1d, 0x01, 0x03]))
            {
                sb.Append(' ');
            }
        }
        return sb.ToString();
    }

    public unsafe static string Read(this Span<byte> bytes)
    {
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 0)
            {
                fixed (byte* ptr = bytes)
                {
                    return Marshal.PtrToStringUTF8((nint)ptr, i);
                }
            }
        }
        fixed (byte* ptr = bytes)
        {
            return Marshal.PtrToStringUTF8((nint)ptr, bytes.Length);
        }
    }

    public static TimeSpan GetTimespanFromTimespanString(this string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return TimeSpan.Zero;

        if (TimeSpan.TryParseExact(pattern, "hh\\:mm\\:ss", null, out var timeSpan) && timeSpan.TotalHours >= 1)
        {
            return timeSpan;
        }
        else if (TimeSpan.TryParseExact(pattern, "mm\\:ss", null, out timeSpan))
        {
            return timeSpan;
        }
        return TimeSpan.Zero;
    }

    public static string GetNameWithWorld(this IPlayerCharacter pc)
        => pc.Name.ToString() + "@" + pc.HomeWorld.Value.Name.ToString();

    public static string StripColorTags(this string input)
    {
        if(string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Define a regex pattern to match any [color=...] and [/color] tags
        string pattern = @"\[\/?(color|glow)(=[^\]]*)?\]";

        // Use Regex.Replace to remove the tags
        string result = Regex.Replace(input, pattern, string.Empty);

        return result;
    }


    /// <summary>
    /// Not my code, pulled from:
    /// https://github.com/PunishXIV/PunishLib/blob/8cea907683c36fd0f9edbe700301a59f59b6c78e/PunishLib/ImGuiMethods/ImGuiEx.cs
    /// </summary>
    public static readonly Dictionary<string, float> CenteredLineWidths = new();
    public static void ImGuiLineCentered(string id, Action func)
    {
        if (CenteredLineWidths.TryGetValue(id, out var dims))
        {
            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X / 2 - dims / 2);
        }
        var oldCur = ImGui.GetCursorPosX();
        func();
        ImGui.SameLine(0, 0);
        CenteredLineWidths[id] = ImGui.GetCursorPosX() - oldCur;
        ImGui.NewLine(); // Use NewLine to finalize the line instead of Dummy
    }

    /// <summary> encapsulates the puppeteer command within '(' and ')' </summary>
    public static SeString GetSubstringWithinParentheses(this SeString str, char startBracket = '(', char EndBracket = ')')
    {
        int startIndex = str.TextValue.IndexOf(startBracket);
        int endIndex = str.TextValue.IndexOf(EndBracket);

        // If both brackets are found and the end bracket is after the start bracket
        if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
            return str.TextValue.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();

        return str;
    }

    /// <summary> Converts square brackets to angle brackets </summary>
    public static SeString ConvertSquareToAngleBrackets(this SeString str)
        => str.TextValue.Replace("[", "<").Replace("]", ">");

}
