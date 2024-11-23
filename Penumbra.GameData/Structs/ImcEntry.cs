using Newtonsoft.Json;

namespace Penumbra.GameData.Structs;

/// <summary> An Image Change entry associates a variant with a specific material, effects and decals. </summary>
public readonly struct ImcEntry : IEquatable<ImcEntry>
{
    public const int    NumAttributes  = 10;
    public const ushort AttributesMask = (1 << NumAttributes) - 1;

    /// <summary> The material to use with this variant. </summary>
    public byte MaterialId { get; init; }

    /// <summary> An optional decal to use with this variant. 0 means none. </summary>
    public byte DecalId { get; init; }

    /// <summary> Additional attributes and a sound ID packed. </summary>
    [JsonIgnore]
    public readonly ushort AttributeAndSound;

    /// <summary> An optional VFX to use with this variant. 0 means none. </summary>
    public byte VfxId { get; init; }

    /// <summary> An optional animation to use with this variant. 0 means none. </summary>
    public byte MaterialAnimationId { get; init; }

    /// <summary> Additional attributes for this variant. Bit 1-10. </summary>
    public ushort AttributeMask
    {
        get => (ushort)(AttributeAndSound & AttributesMask);
        init => AttributeAndSound = (ushort)((AttributeAndSound & ~AttributesMask) | (value & AttributesMask));
    }

    /// <summary> An optional sound ID to use with this variant. Bit 11-16. </summary>
    public byte SoundId
    {
        get => (byte)(AttributeAndSound >> NumAttributes);
        init => AttributeAndSound = (ushort)(AttributeMask | (value << NumAttributes));
    }

    /// <inheritdoc/>
    public bool Equals(ImcEntry other)
        => MaterialId == other.MaterialId
         && DecalId == other.DecalId
         && AttributeAndSound == other.AttributeAndSound
         && VfxId == other.VfxId
         && MaterialAnimationId == other.MaterialAnimationId;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is ImcEntry other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(MaterialId, DecalId, AttributeAndSound, VfxId, MaterialAnimationId);

    /// <summary> A constructor to use when deserializing from JSON. </summary>
    [JsonConstructor]
    public ImcEntry(byte materialId, byte decalId, ushort attributeMask, byte soundId, byte vfxId, byte materialAnimationId)
    {
        MaterialId          = materialId;
        DecalId             = decalId;
        AttributeAndSound   = 0;
        VfxId               = vfxId;
        MaterialAnimationId = materialAnimationId;
        AttributeMask       = attributeMask;
        SoundId             = soundId;
    }

    public static bool operator ==(ImcEntry left, ImcEntry right)
        => left.Equals(right);

    public static bool operator !=(ImcEntry left, ImcEntry right)
        => !(left == right);

    public override string ToString()
    {
        var sb = new StringBuilder(128);
        sb.Append("Material ")
            .Append(MaterialId);
        if (DecalId != 0)
            sb.Append(", Decal ")
                .Append(DecalId);
        if (VfxId != 0)
            sb.Append(", VFX ")
                .Append(VfxId);
        var sound = SoundId;
        if (sound != 0)
            sb.Append(", Sound ")
                .Append(sound);

        sb.Append(", Attributes ");
        var mask = AttributeMask;
        for (var i = 0; i < NumAttributes; ++i)
        {
            if ((mask & (1 << i)) != 0)
                sb.Append((char) ('A' + i));
        }
        return sb.ToString();
    }
}
