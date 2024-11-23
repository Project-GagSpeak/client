using Newtonsoft.Json.Linq;
using Penumbra.GameData.Files.Utility;
using Penumbra.String;

namespace Penumbra.GameData.Files.AtchStructs;

public unsafe struct AtchEntry : IEquatable<AtchEntry>
{
    public const int MaxBoneNameLength = 34;

    private fixed byte  _boneName[MaxBoneNameLength + 1];
    private       byte  _boneNameLength;
    public        float Scale;
    public        float OffsetX;
    public        float OffsetY;
    public        float OffsetZ;
    public        float RotationX;
    public        float RotationY;
    public        float RotationZ;

    public Span<byte> FullSpan
    {
        get
        {
            fixed (byte* ptr = _boneName)
            {
                return new Span<byte>(ptr, MaxBoneNameLength);
            }
        }
    }


    public ReadOnlySpan<byte> Bone
    {
        get
        {
            fixed (byte* ptr = _boneName)
            {
                return new ReadOnlySpan<byte>(ptr, _boneNameLength);
            }
        }
    }

    public string BoneAsString()
        => Encoding.UTF8.GetString(Bone);

    public JObject ToJson()
        => new()
        {
            ["Bone"]      = BoneAsString(),
            ["Scale"]     = Scale,
            ["OffsetX"]   = OffsetX,
            ["OffsetY"]   = OffsetY,
            ["OffsetZ"]   = OffsetZ,
            ["RotationX"] = RotationX,
            ["RotationY"] = RotationY,
            ["RotationZ"] = RotationZ,
        };

    public static AtchEntry? FromJson(JObject? obj)
    {
        if (obj == null)
            return null;

        var ret  = new AtchEntry();
        var bone = obj["Bone"]?.ToObject<string>() ?? string.Empty;
        if (bone.Length == 0 || !ret.SetBoneName(bone))
            return null;

        ret.Scale     = obj["Scale"]?.ToObject<float>() ?? 0;
        ret.OffsetX   = obj["OffsetX"]?.ToObject<float>() ?? 0;
        ret.OffsetY   = obj["OffsetY"]?.ToObject<float>() ?? 0;
        ret.OffsetZ   = obj["OffsetZ"]?.ToObject<float>() ?? 0;
        ret.RotationX = obj["RotationX"]?.ToObject<float>() ?? 0;
        ret.RotationY = obj["RotationY"]?.ToObject<float>() ?? 0;
        ret.RotationZ = obj["RotationZ"]?.ToObject<float>() ?? 0;
        return ret;
    }

    public bool SetBoneName(ReadOnlySpan<byte> text)
    {
        if (text.Length > MaxBoneNameLength)
            return false;

        fixed (byte* ptr = _boneName)
        {
            text.CopyTo(new Span<byte>(ptr, MaxBoneNameLength));
        }

        _boneNameLength            = (byte)text.Length;
        _boneName[_boneNameLength] = 0;
        return true;
    }

    public bool SetBoneName(string text)
    {
        try
        {
            var utf8 = Encoding.UTF8.GetBytes(text);
            return SetBoneName(utf8);
        }
        catch
        {
            return false;
        }
    }

    public Vector3 Offset
    {
        get => new(OffsetX, OffsetY, OffsetZ);
        set
        {
            OffsetX = value.X;
            OffsetY = value.Y;
            OffsetZ = value.Z;
        }
    }

    public Vector3 Rotation
    {
        get => new(RotationX, RotationY, RotationZ);
        set
        {
            RotationX = value.X;
            RotationY = value.Y;
            RotationZ = value.Z;
        }
    }


    public static AtchEntry Read(ref SpanBinaryReader reader)
    {
        var stringOffset = reader.ReadInt32();
        var boneName     = reader.ReadByteString(stringOffset);
        var ret          = new AtchEntry();
        if (!ret.SetBoneName(boneName))
            throw new Exception(
                $"Invalid Atch Entry with bone name {ByteString.FromSpanUnsafe(boneName, true, null, true)} longer than max length.");

        ret.Scale     = reader.ReadSingle();
        ret.OffsetX   = reader.ReadSingle();
        ret.OffsetY   = reader.ReadSingle();
        ret.OffsetZ   = reader.ReadSingle();
        ret.RotationX = reader.ReadSingle();
        ret.RotationY = reader.ReadSingle();
        ret.RotationZ = reader.ReadSingle();
        return ret;
    }

    public bool Equals(AtchEntry other)
        => Scale.Equals(other.Scale)
         && OffsetX.Equals(other.OffsetX)
         && OffsetY.Equals(other.OffsetY)
         && OffsetZ.Equals(other.OffsetZ)
         && RotationX.Equals(other.RotationX)
         && RotationY.Equals(other.RotationY)
         && RotationZ.Equals(other.RotationZ)
         && Bone.SequenceEqual(other.Bone);

    public override bool Equals(object? obj)
        => obj is AtchEntry other && Equals(other);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(_boneNameLength);
        hashCode.Add(_boneName[0]);
        hashCode.Add(Scale);
        hashCode.Add(OffsetX);
        hashCode.Add(OffsetY);
        hashCode.Add(OffsetZ);
        hashCode.Add(RotationX);
        hashCode.Add(RotationY);
        hashCode.Add(RotationZ);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(AtchEntry left, AtchEntry right)
        => left.Equals(right);

    public static bool operator !=(AtchEntry left, AtchEntry right)
        => !left.Equals(right);
}
