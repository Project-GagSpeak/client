using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.String.Functions;

namespace Penumbra.String.Classes;

/// <summary>
/// A combination of a C#-string for filesystem interactions
/// and a lower-case ByteString for FFXIV interactions.
/// Also stores the FFXIV-CRC64 value.
/// </summary>
[JsonConverter(typeof(FullPathConverter))]
public struct FullPath : IComparable, IEquatable<FullPath>
{
    /// <summary> The Unicode string containing the full name of the path with backward-slashes. </summary>
    public readonly string FullName;

    /// <summary> The UTF8 string containing the full name of the path with forward-slashes. </summary>
    public readonly CiByteString InternalName;

    private ulong _crc64;

    /// <summary> The FFXIV specific Crc64 value, i.e. a CRC32 of the file name combined with a CRC32 of the file path. </summary>
    /// <remarks> This value is cached after the first computation. </remarks>
    public ulong Crc64
        => _crc64 == 0 && !InternalName.IsEmpty ? (_crc64 = ByteStringFunctions.ComputeLowerCaseCrc64(InternalName)) : _crc64;

    /// <summary> An empty string. </summary>
    public static readonly FullPath Empty = new(string.Empty);

    /// <summary>
    /// Combine a relative path with a base directory for a full path.
    /// </summary>
    public FullPath(DirectoryInfo baseDir, Utf8RelPath relPath)
        : this(Path.Combine(baseDir.FullName, relPath.ToString()))
    { }

    /// <summary> Create a full path from a given file path. </summary>
    public FullPath(FileInfo file)
        : this(file.FullName)
    { }

    /// <summary> Create a full path from a given string. </summary>
    public FullPath(string s)
    {
        FullName     = s.Replace('/', '\\').Trim();
        InternalName = CiByteString.FromString(FullName.Replace('\\', '/'), out var name) ? name : CiByteString.Empty;
    }

    /// <summary> Create a full path from a given game path.  </summary>
    public FullPath(Utf8GamePath path)
    {
        FullName     = path.ToString().Replace('/', '\\');
        InternalName = path.Path;
    }

    /// <summary> Check whether the file exists on your file system. </summary>
    public bool Exists
        => File.Exists(FullName);

    /// <summary> Get the file extension of the file. </summary>
    public string Extension
        => Path.GetExtension(FullName);

    /// <summary> Get only the file name of the file. </summary>
    public string Name
        => Path.GetFileName(FullName);

    /// <summary> Use the given directory path to obtain a game path. </summary>
    public bool ToGamePath(DirectoryInfo dir, out Utf8GamePath path)
    {
        path = Utf8GamePath.Empty;
        if (!FullName.StartsWith(dir.FullName))
            return false;

        var substring = InternalName.Substring(dir.FullName.Length + 1);

        path = new Utf8GamePath(substring);
        return true;
    }

    /// <summary> Use the given directory path to obtain path relative to it. </summary>
    public bool ToRelPath(DirectoryInfo dir, out Utf8RelPath path)
    {
        path = Utf8RelPath.Empty;
        if (!FullName.StartsWith(dir.FullName))
            return false;

        var dirLength = Encoding.UTF8.GetByteCount(dir.FullName);
        var substring = InternalName.Substring(dirLength + 1);

        path = new Utf8RelPath(((ByteString)substring).Replace((byte)'/', (byte)'\\'));
        return true;
    }

    /// <summary> Compare lexicographically against another FullPath, FileInfo, ByteString or string. </summary>
    /// <remarks> Comparison against FileInfo and string ignore case, comparisons against FullPath and ByteString do not, but use the InternalName. </remarks>
    public int CompareTo(object? obj)
        => obj switch
        {
            FullPath p     => InternalName?.CompareTo(p.InternalName) ?? -1,
            FileInfo f     => string.Compare(FullName, f.FullName, StringComparison.OrdinalIgnoreCase),
            CiByteString u => InternalName?.CompareTo(u) ?? -1,
            string s       => string.Compare(FullName, s, StringComparison.OrdinalIgnoreCase),
            _              => -1,
        };

    /// <summary> Check if two FullPaths are equal. </summary>
    public bool Equals(FullPath other)
    {
        if (FullName.Length == 0 || other.FullName.Length == 0)
            return true;

        return InternalName.Equals(other.InternalName);
    }

    /// <summary> Returns whether the path is rooted. </summary>
    public bool IsRooted
        => new Utf8GamePath(InternalName).IsRooted();

    /// <summary> Return the CRC32 of the InternalName. </summary>
    public override int GetHashCode()
        => InternalName.Crc32;

    /// <summary> Return the FullName. </summary>
    public override string ToString()
        => FullName;

    /// <summary> Return the path with back slashes if it is rooted and with forward-slashes if not. </summary>
    public string ToPath()
        => IsRooted ? FullName : FullName.Replace("\\", "/");

    /// <summary>
    /// Convert from and to string.
    /// </summary>
    private class FullPathConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(FullPath);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader).ToString();
            return new FullPath(token);
        }

        public override bool CanWrite
            => true;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is FullPath p)
                serializer.Serialize(writer, p.ToString());
        }
    }
}
