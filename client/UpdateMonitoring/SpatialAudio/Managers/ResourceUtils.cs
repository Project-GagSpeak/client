using GagSpeak.UpdateMonitoring.SpatialAudio.Structs;
using Penumbra.String;

namespace GagSpeak.UpdateMonitoring.SpatialAudio.Managers;

public static unsafe class ResourceUtils
{
    // https://github.com/xivdev/Penumbra/blob/e3a1ae693813eb06d96d311958aabb5b3abfef55/Penumbra/Interop/Hooks/ResourceLoading/ResourceLoader.cs#L269
    public static int ComputeHash(CiByteString path, GetResourceParameters* pGetResParams)
    {
        if (pGetResParams is null || !pGetResParams->IsPartialRead) return path.Crc32;

        return CiByteString.Join(
            (byte)'.',
            path,
            CiByteString.FromString(pGetResParams->SegmentOffset.ToString("x"), out var s1, MetaDataComputation.None) ? s1 : CiByteString.Empty,
            CiByteString.FromString(pGetResParams->SegmentLength.ToString("x"), out var s2, MetaDataComputation.None) ? s2 : CiByteString.Empty
        ).Crc32;
    }

    public static byte[] GetBgCategory(string expansion, string zone)
    {
        var ret = BitConverter.GetBytes(2u);
        if (expansion == "ffxiv") return ret;
        // ex1/03_abr_a2/fld/a2f1/level/a2f1 -> [02 00 03 01]
        // expansion = ex1
        // zone = 03_abr_a2
        var expansionTrimmed = expansion.Replace("ex", "");
        var zoneTrimmed = zone.Split('_')[0];
        ret[2] = byte.Parse(zoneTrimmed);
        ret[3] = byte.Parse(expansionTrimmed);
        return ret;
    }

    public static byte[] GetDatCategory(uint prefix, string expansion)
    {
        var ret = BitConverter.GetBytes(prefix);
        if (expansion == "ffxiv") return ret;
        // music/ex4/BGM_EX4_Field_Ult_Day03.scd
        // 04 00 00 0C
        var expansionTrimmed = expansion.Replace("ex", "");
        ret[3] = byte.Parse(expansionTrimmed);
        return ret;
    }
}
