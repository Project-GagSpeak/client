using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.String.Functions;

namespace Penumbra.String.Classes;

/// <summary>
/// A relative path that verifies some invariants based on an UTF8 string.
/// Similar to Utf8GamePath just kept for filesystem interaction instead of FFXIV interaction.
/// </summary>
[JsonConverter(typeof(Utf8RelPathConverter))]
public readonly struct Utf8RelPath : IEquatable<Utf8RelPath>, IComparable<Utf8RelPath>, IDisposable
{
    /// <inheritdoc cref="Utf8GamePath.MaxGamePathLength"/>
    public const int MaxRelPathLength = Utf8GamePath.MaxGamePathLength;

    /// <inheritdoc cref="Utf8GamePath.Path"/>
    public readonly ByteString Path;

    /// <inheritdoc cref="Utf8GamePath.Empty"/>
    public static readonly Utf8RelPath Empty = new(ByteString.Empty);

    internal Utf8RelPath(ByteString path)
        => Path = path;

    /// <summary> Explicit conversion from string to Utf8RelPath for migrating old dictionaries. </summary>
    public static explicit operator Utf8RelPath(string s)
    {
        if (!FromString(s, out var p))
            return Empty;

        return new Utf8RelPath(p.Path.AsciiToLower());
    }

    /// <inheritdoc cref="Utf8GamePath.FromString"/>
    public static bool FromString(string? s, out Utf8RelPath path)
    {
        path = Empty;
        if (string.IsNullOrEmpty(s))
            return true;

        var substring = s.Replace('/', '\\').TrimStart('\\').TrimEnd();
        if (substring.Length > MaxRelPathLength)
            return false;

        if (substring.Length == 0)
            return true;

        if (!ByteString.FromString(substring, out var ascii, true))
            return false;

        path = new Utf8RelPath(ascii);
        return true;
    }

    /// <inheritdoc cref="Utf8GamePath.FromFile"/>
    public static bool FromFile(FileInfo file, DirectoryInfo baseDir, out Utf8RelPath path)
    {
        path = Empty;
        if (!file.FullName.StartsWith(baseDir.FullName))
            return false;

        var substring = file.FullName[(baseDir.FullName.Length + 1)..];
        return FromString(substring, out path);
    }

    /// <inheritdoc cref="Utf8GamePath.FromFile"/>
    public static bool FromFile(FullPath file, DirectoryInfo baseDir, out Utf8RelPath path)
    {
        path = Empty;
        if (!file.FullName.StartsWith(baseDir.FullName))
            return false;

        var substring = file.FullName[(baseDir.FullName.Length + 1)..];
        return FromString(substring, out path);
    }

    /// <summary>
    /// Create a new RelPath from a GamePath by replacing all forward slashes with backward slashes.
    /// </summary>
    public Utf8RelPath(Utf8GamePath gamePath)
        => Path = ((ByteString)gamePath.Path).Replace((byte)'/', (byte)'\\');

    /// <summary>
    /// Convert to Utf8GamePath by replacing all backward slashes with forward slashes, and turning to lower case.
    /// </summary>
    /// <param name="skipFolders">Optionally skip the first few folders in the conversion.</param>
    public unsafe Utf8GamePath ToGamePath(int skipFolders = 0)
    {
        var idx = 0;
        while (skipFolders > 0)
        {
            idx = Path.IndexOf((byte)'\\', idx) + 1;
            --skipFolders;
            if (idx <= 0)
                return Utf8GamePath.Empty;
        }

        var length = Path.Length - idx;
        var ptr    = ByteStringFunctions.CopyString(Path.Path + idx, length);
        ByteStringFunctions.Replace(ptr, length, (byte)'\\', (byte)'/');
        var utf = new CiByteString().Setup(ptr, length, null, null, true, true, true, true);
        return new Utf8GamePath(utf);
    }

    /// <inheritdoc cref="ByteString.CompareTo"/>
    public int CompareTo(Utf8RelPath rhs)
        => Path.CompareTo(rhs.Path);

    /// <inheritdoc cref="ByteString.Equals(ByteString?)"/>
    public bool Equals(Utf8RelPath other)
        => Path.Equals(other.Path);

    /// <inheritdoc cref="ByteString.ToString"/>
    public override string ToString()
        => Path.ToString();

    /// <inheritdoc cref="ByteString.Dispose"/>
    public void Dispose()
        => Path.Dispose();

    /// <inheritdoc cref="Utf8GamePath.Utf8GamePathConverter"/>
    private class Utf8RelPathConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(Utf8RelPath);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader).ToString();
            return FromString(token, out var p)
                ? p
                : throw new JsonException($"Could not convert \"{token}\" to {nameof(Utf8RelPath)}.");
        }

        public override bool CanWrite
            => true;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is Utf8RelPath p)
                serializer.Serialize(writer, p.ToString());
        }
    }
}
