using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Penumbra.String.Classes;

/// <summary>
/// A GamePath that verifies some invariants based on a CiByteString.
/// The Invariants are being smaller than <see cref="MaxGamePathLength"/>,
/// and containing forward-slashes as separators.
/// </summary>
[JsonConverter(typeof(Utf8GamePathConverter))]
public readonly struct Utf8GamePath : IEquatable<Utf8GamePath>, IComparable<Utf8GamePath>, IDisposable
{
    /// <summary>
    /// The maximum length Penumbra accepts.
    /// </summary>
    public const int MaxGamePathLength = 2 << 10;

    /// <summary>
    /// Return the original path.
    /// </summary>
    public readonly CiByteString Path;

    /// <summary> An empty path. </summary>
    public static readonly Utf8GamePath Empty = new(CiByteString.Empty);

    internal Utf8GamePath(CiByteString s)
        => Path = s;

    /// <inheritdoc cref="CiByteString.Length"/>
    public int Length
        => Path.Length;

    /// <inheritdoc cref="CiByteString.IsEmpty"/>
    public bool IsEmpty
        => Path.IsEmpty;

    /// <summary>
    /// Create a new Utf8GamePath from a pointer.
    /// </summary>
    /// <param name="ptr">The data.</param>
    /// <param name="path">The resulting game path if successful, an empty path otherwise.</param>
    /// <param name="flags">The metadata that should be pre-computed.</param>
    /// <returns>Whether the given pointer is a valid Utf8GamePath.</returns>
    public static unsafe bool FromPointer(byte* ptr, MetaDataComputation flags, out Utf8GamePath path)
    {
        var utf = new CiByteString(ptr, flags);
        return ReturnChecked(utf, out path);
    }

    /// <summary>
    /// Same as <see cref="FromPointer"/> just with known length.
    /// </summary>
    public static bool FromSpan(ReadOnlySpan<byte> data, MetaDataComputation flags, out Utf8GamePath path)
    {
        var utf = new CiByteString(data, flags);
        return ReturnChecked(utf, out path);
    }

    /// <summary>
    /// Does not check for Forward/Backslashes due to assuming that SE-strings use the correct one.
    /// Does not check for initial slashes either, since they are assumed to be by choice.
    /// Checks for maxlength and lowercase.
    /// </summary>
    /// <param name="utf"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private static bool ReturnChecked(CiByteString utf, out Utf8GamePath path)
    {
        path = Empty;
        if (utf.Length > MaxGamePathLength)
            return false;

        path = new Utf8GamePath(utf);
        return true;
    }

    /// <inheritdoc cref="ByteString.Clone"/>
    public Utf8GamePath Clone()
        => new(Path.Clone());

    /// <summary>
    /// Create a new path from a string.
    /// </summary>
    /// <param name="s">The given string.</param>
    /// <param name="path">The converted path or an empty path on failure.</param>
    /// <returns>False if the string is too long, or can not be converted to UTF8.</returns>
    public static bool FromString(string? s, out Utf8GamePath path)
    {
        path = Empty;
        if (string.IsNullOrEmpty(s))
            return true;

        var substring = s.Replace('\\', '/').TrimStart('/').Trim();
        if (substring.Length > MaxGamePathLength)
            return false;

        if (substring.Length == 0)
            return true;

        if (!CiByteString.FromString(substring, out var ascii))
            return false;

        path = new Utf8GamePath(ascii);
        return true;
    }

    /// <summary>
    /// Create a new path from a string and check its length.
    /// </summary>
    /// <param name="s">The given string.</param>
    /// <param name="path">The string as UTF8GamePath, empty string on failure or null input.</param>
    /// <returns>False if the string is too long.</returns>
    public static bool FromByteString(CiByteString? s, out Utf8GamePath path)
    {
        if (s is null)
        {
            path = Empty;
            return true;
        }

        if (s.Length > MaxGamePathLength)
        {
            path = Empty;
            return false;
        }

        path = new Utf8GamePath(s);
        return true;
    }

    /// <summary>
    /// Create a new path from a file and a base directory.
    /// </summary>
    /// <param name="file">The file path to convert.</param>
    /// <param name="baseDir">The directory to which the file path should be seen as relative. </param>
    /// <param name="path">The converted path or an empty path on failure.</param>
    /// <returns>False if the file does not lie inside the base directory or the string conversion fails.</returns>
    public static bool FromFile(FileInfo file, DirectoryInfo baseDir, out Utf8GamePath path)
    {
        path = Empty;
        if (!file.FullName.StartsWith(baseDir.FullName))
            return false;

        var substring = file.FullName[(baseDir.FullName.Length + 1)..];
        return FromString(substring, out path);
    }

    /// <summary>
    /// Get the non-owned substring of the file name of the path.
    /// </summary>
    public CiByteString Filename()
    {
        var idx = Path.LastIndexOf((byte)'/');
        return idx == -1 ? Path : Path.Substring(idx + 1);
    }

    /// <summary>
    /// Get the non-owned substring of the extension of the path.
    /// </summary>
    public CiByteString Extension()
    {
        var idx = Path.LastIndexOf((byte)'.');
        return idx == -1 ? CiByteString.Empty : Path.Substring(idx);
    }

    /// <inheritdoc cref="ByteString.Equals(ByteString?)"/>
    public bool Equals(Utf8GamePath other)
        => Path.Equals(other.Path);

    /// <inheritdoc cref="ByteString.GetHashCode"/>
    public override int GetHashCode()
        => Path.GetHashCode();

    /// <inheritdoc cref="ByteString.CompareTo"/>
    public int CompareTo(Utf8GamePath other)
        => Path.CompareTo(other.Path);

    /// <inheritdoc cref="ByteString.ToString"/>
    public override string ToString()
        => Path.ToString();

    /// <inheritdoc cref="ByteString.Dispose"/>
    public void Dispose()
        => Path.Dispose();

    /// <inheritdoc cref="IsRooted(CiByteString)"/>
    public bool IsRooted()
        => IsRooted(Path);

    /// <summary>
    /// Return whether the path is rooted.
    /// </summary>
    public static bool IsRooted(CiByteString path)
        => path.Length >= 1 && (path[0] == '/' || path[0] == '\\')
         || path.Length >= 2
         && (path[0] >= 'A' && path[0] <= 'Z' || path[0] >= 'a' && path[0] <= 'z')
         && path[1] == ':';

    /// <summary>
    /// Conversion from and to string.
    /// </summary>
    private class Utf8GamePathConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(Utf8GamePath);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader).ToString();
            return FromString(token, out var p)
                ? p
                : throw new JsonException($"Could not convert \"{token}\" to {nameof(Utf8GamePath)}.");
        }

        public override bool CanWrite
            => true;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is Utf8GamePath p)
                serializer.Serialize(writer, p.ToString());
        }
    }
}
