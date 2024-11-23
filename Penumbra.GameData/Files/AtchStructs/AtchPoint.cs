using Penumbra.GameData.Files.Utility;

namespace Penumbra.GameData.Files.AtchStructs;

public record AtchPoint
{
    public AtchType    Type;
    public AtchEntry[] Entries = [];
    public bool        Accessory;

    public void ReadStates(ref SpanBinaryReader reader, ushort numEntries)
    {
        Entries = new AtchEntry[numEntries];
        for (var i = 0; i < numEntries; ++i)
            Entries[i] = AtchEntry.Read(ref reader);
    }

    public AtchPoint(AtchPoint clone)
    {
        Type      = clone.Type;
        Accessory = clone.Accessory;
        Entries   = clone.Entries.ToArray();
    }
}
