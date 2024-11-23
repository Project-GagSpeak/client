using Penumbra.String.Functions;
using ByteStringFunctions = Penumbra.String.Functions.ByteStringFunctions;

namespace Penumbra.String;

public sealed unsafe partial class ByteString
{
    /// <summary>
    /// Create a C# UTF16 string from this string.
    /// </summary>
    public override string ToString()
    {
        if (Length == 0)
            return string.Empty;

        // If the string is known to be pure ASCII, use that encoding, otherwise UTF8.
        if ((_length & AsciiFlag) != 0)
            return Encoding.ASCII.GetString(_path, Length);

        return Encoding.UTF8.GetString(_path, Length);
    }


    /// <summary> Convert the ASCII portion of the string to lowercase. </summary>
    /// <remarks>Only creates a new string and copy if the string is not already known to be lower case.</remarks>
    public ByteString AsciiToLower()
        => (_length & AsciiLowerFlag) == 0
            ? new ByteString().Setup(ByteStringFunctions.AsciiToLower(_path, Length), Length, null, true, true, true, IsAsciiInternal)
            : this;

    /// <summary>
    /// Convert the ascii portion of the string to mixed case (i.e. capitalize every first letter in a word)
    /// </summary>
    /// <remarks>Always creates an owned copy of the string.</remarks>
    public ByteString AsciiToMixed()
    {
        var length = Length;
        if (length == 0)
            return Empty;

        var ret                = Clone();
        var previousWhitespace = true;
        var end                = ret.Path + length;
        for (var ptr = ret.Path; ptr < end; ++ptr)
        {
            if (previousWhitespace)
                *ptr = ByteStringFunctions.AsciiToUpper(*ptr);

            previousWhitespace = char.IsWhiteSpace((char)*ptr);
        }

        return ret;
    }

    /// <summary> Convert the ASCII portion of the string to lowercase. </summary>
    /// <remarks>Guaranteed to create an owned copy.</remarks>
    public ByteString AsciiToLowerClone()
        => (_length & AsciiLowerFlag) == 0
            ? new ByteString().Setup(ByteStringFunctions.AsciiToLower(_path, Length), Length, null, true, true, true, IsAsciiInternal)
            : Clone();

    /// <summary> Create an owned copy of the given string. </summary>
    public ByteString Clone()
    {
        if (IsEmpty)
            return Empty;

        var ret = new ByteString();
        ret._length = _length | OwnedFlag | NullTerminatedFlag;
        ret._path   = ByteStringFunctions.CopyString(Path, Length);
        ret._crc32  = Crc32;
        return ret;
    }

    /// <summary> Create a non-owning substring from the given position. </summary>
    /// <param name="from">The starting position.</param>
    /// <remarks>If from is negative or too large, the returned string will be the empty string.</remarks>
    public ByteString Substring(int from)
        => (uint)from < Length
            ? FromByteStringUnsafe(_path + from, Length - from, IsNullTerminated, IsAsciiLowerInternal, IsAsciiInternal)
            : Empty;

    /// <summary>
    /// Create a non-owning substring from the given position of the given length.
    /// </summary>
    /// <param name="from">The starting position.</param>
    /// <param name="length">The total length.</param>
    /// <remarks> If from is negative or too large, the returned string will be the empty string. <br/>
    /// If from + length is too large, it will be the same as if length was not specified.</remarks>
    public ByteString Substring(int from, int length)
    {
        var maxLength = Length - (uint)from;
        if (maxLength <= 0)
            return Empty;

        return length < maxLength
            ? FromByteStringUnsafe(_path + from, length, false, IsAsciiLowerInternal, IsAsciiInternal)
            : Substring(from);
    }

    /// <summary> Trim all ascii whitespace characters from the front. </summary>
    public ByteString TrimFront()
    {
        if (IsEmpty)
            return Empty;

        var ptr = _path;
        var end = _path + Length;
        while (*ptr < 0x7F && char.IsWhiteSpace((char)*ptr) && ptr++ < end)
        { }

        if (ptr == _path)
            return this;

        if (ptr == end)
            return Empty;

        return FromByteStringUnsafe(ptr, (int)(end - ptr), IsNullTerminated, IsAsciiLowerInternal, IsAsciiInternal);
    }

    /// <summary> Trim all ascii whitespace characters at the end. </summary>
    public ByteString TrimEnd()
    {
        if (IsEmpty)
            return Empty;

        var ptr = _path + Length - 1;
        var end = _path;
        while (*ptr < 0x7F && char.IsWhiteSpace((char)*ptr) && ptr-- >= end)
        { }

        if (ptr == _path + Length - 1)
            return this;

        if (ptr < end)
            return Empty;

        return FromByteStringUnsafe(_path, (int)(ptr - _path), false, IsAsciiLowerInternal, IsAsciiInternal);
    }

    /// <summary> Trim all ascii whitespace characters from the beginning or end of the string. </summary>
    public ByteString Trim()
        => TrimFront().TrimEnd();

    /// <summary>
    /// Create a owned copy of the string and replace all occurrences of from with to in it.
    /// </summary>
    /// <param name="from">The byte to replace.</param>
    /// <param name="to">The byte <paramref name="from"/> is to be replaced with.</param>
    public ByteString Replace(byte from, byte to)
    {
        var length      = Length;
        var newPtr      = ByteStringFunctions.CopyString(_path, length);
        var numReplaced = ByteStringFunctions.Replace(newPtr, length, from, to);
        return new ByteString().Setup(newPtr, length, numReplaced == 0 ? _crc32 : null, true, true, IsAsciiLowerInternal, IsAsciiInternal);
    }

    /// <summary>
    /// Join a number of strings with a given byte between them.
    /// </summary>
    /// <param name="splitter">The byte to insert between all strings.</param>
    /// <param name="strings">The list of strings to join.</param>
    /// <remarks>No <paramref name="splitter"/> is inserted before the first or after the last string.</remarks>
    public static ByteString Join(byte splitter, params ByteString[] strings)
    {
        var length = strings.Sum(s => s.Length) + strings.Length;
        var data   = PenumbraStringMemory.Allocate(length);

        var   ptr     = data;
        bool? isLower = ByteStringFunctions.AsciiIsLower(splitter);
        bool? isAscii = splitter < 128;
        foreach (var s in strings)
        {
            MemoryUtility.MemCpyUnchecked(ptr, s.Path, s.Length);
            ptr     += s.Length;
            *ptr++  =  splitter;
            isLower =  Combine(isLower, s.IsAsciiLowerInternal);
            isAscii &= s.IsAscii;
        }

        --length;
        data[length] = 0;
        var ret = FromByteStringUnsafe(data, length, true, isLower, isAscii);
        ret._length |= OwnedFlag;
        return ret;
    }

    /// <summary>
    /// Join a number of strings with a given byte between them.
    /// </summary>
    /// <param name="strings">The list of strings to join.</param>
    public static ByteString Join(params ByteString[] strings)
    {
        var length = strings.Sum(s => s.Length);
        var data   = PenumbraStringMemory.Allocate(length + 1);

        var   ptr     = data;
        bool? isLower = true;
        bool? isAscii = true;
        foreach (var s in strings)
        {
            MemoryUtility.MemCpyUnchecked(ptr, s.Path, s.Length);
            ptr     += s.Length;
            isLower =  Combine(isLower, s.IsAsciiLowerInternal);
            isAscii &= s.IsAscii;
        }

        data[length] = 0;
        var ret = FromByteStringUnsafe(data, length, true, isLower, isAscii);
        ret._length |= OwnedFlag;
        return ret;
    }

    /// <summary>
    /// Split a string and return a list of the substrings delimited by b.
    /// </summary>
    /// <param name="b"></param>
    /// <param name="maxSplits">An optional maximum number of splits - if the maximum is reached, the last substring may contain delimiters.</param>
    /// <param name="removeEmpty">Remove all empty substrings between delimiters, they are also not counted for <paramref name="maxSplits"/>.</param>
    /// <remarks>The returned substrings are not owned.</remarks>
    public List<ByteString> Split(byte b, int maxSplits = int.MaxValue, bool removeEmpty = true)
    {
        var ret   = new List<ByteString>();
        var start = 0;
        for (var idx = IndexOf(b, start); idx >= 0; idx = IndexOf(b, start))
        {
            if (start != idx || !removeEmpty)
                ret.Add(Substring(start, idx - start));

            start = idx + 1;
            if (ret.Count == maxSplits - 1)
                break;
        }

        ret.Add(Substring(start));
        return ret;
    }

    /// Combine two optional bools into a new optional bool.
    private static bool? Combine(bool? val1, bool? val2)
    {
        return (val1, val2) switch
        {
            (null, null)   => null,
            (null, true)   => null,
            (null, false)  => false,
            (true, null)   => null,
            (true, true)   => true,
            (true, false)  => false,
            (false, null)  => false,
            (false, true)  => false,
            (false, false) => false,
        };
    }
}
