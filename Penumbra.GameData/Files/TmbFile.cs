using Dalamud.Memory;

namespace Penumbra.GameData.Files;

public class TmbFile
{
    public int                     FileSize;
    public IReadOnlyList<string>   Paths;
    public IReadOnlyList<TmbEntry> Entries;

    public unsafe TmbFile(ReadOnlySpan<byte> data)
    {
        if (data.Length < 12)
            throw new Exception("TMB has not enough data for basic header.");

        fixed (byte* basePtr = data)
        {
            CheckMagicNumber(*(uint*)basePtr);
            FileSize = *(int*)(basePtr + 4);
            if (FileSize != data.Length)
                throw new Exception($"TMB is corrupt and has different size in data than in bytes.");

            var ptr = basePtr + 12;
            Entries = ReadEntries(*(int*)(basePtr + 8), ref ptr, data.Length);
        }

        Paths = Entries.Where(e => e.Path.Length > 0).Select(e => e.Path).ToList();
    }

    private static unsafe IReadOnlyList<TmbEntry> ReadEntries(int numEntries, ref byte* startPtr, int length)
    {
        var end     = startPtr + length;
        var entries = new TmbEntry[numEntries];
        for (var i = 0; i < entries.Length; ++i)
            entries[i] = TmbEntry.Parse(ref startPtr, end);

        return entries;
    }

    private static void CheckMagicNumber(uint magic)
    {
        if (magic != TmbEntry.MagicTMLB)
            throw new Exception($"Invalid Magic Number {magic:X} for TMB.");
    }

    public unsafe struct TmbEntry
    {
        public const uint MagicTMLB = 'T' | ((uint)'M' << 8) | ((uint)'L' << 16) | ((uint)'B' << 24);
        public const uint MagicTMDH = 'T' | ((uint)'M' << 8) | ((uint)'D' << 16) | ((uint)'H' << 24);
        public const uint MagicTMPP = 'T' | ((uint)'M' << 8) | ((uint)'P' << 16) | ((uint)'P' << 24);
        public const uint MagicTMAL = 'T' | ((uint)'M' << 8) | ((uint)'A' << 16) | ((uint)'L' << 24);
        public const uint MagicTMAC = 'T' | ((uint)'M' << 8) | ((uint)'A' << 16) | ((uint)'C' << 24);
        public const uint MagicTMTR = 'T' | ((uint)'M' << 8) | ((uint)'T' << 16) | ((uint)'R' << 24);
        public const uint MagicC002 = 'C' | ((uint)'0' << 8) | ((uint)'0' << 16) | ((uint)'2' << 24);
        public const uint MagicC006 = 'C' | ((uint)'0' << 8) | ((uint)'0' << 16) | ((uint)'6' << 24);
        public const uint MagicC009 = 'C' | ((uint)'0' << 8) | ((uint)'0' << 16) | ((uint)'9' << 24);
        public const uint MagicC010 = 'C' | ((uint)'0' << 8) | ((uint)'1' << 16) | ((uint)'0' << 24);
        public const uint MagicC011 = 'C' | ((uint)'0' << 8) | ((uint)'1' << 16) | ((uint)'1' << 24);
        public const uint MagicC012 = 'C' | ((uint)'0' << 8) | ((uint)'1' << 16) | ((uint)'2' << 24);
        public const uint MagicC013 = 'C' | ((uint)'0' << 8) | ((uint)'1' << 16) | ((uint)'3' << 24);
        public const uint MagicC014 = 'C' | ((uint)'0' << 8) | ((uint)'1' << 16) | ((uint)'4' << 24);
        public const uint MagicC015 = 'C' | ((uint)'0' << 8) | ((uint)'1' << 16) | ((uint)'5' << 24);
        public const uint MagicC021 = 'C' | ((uint)'0' << 8) | ((uint)'2' << 16) | ((uint)'1' << 24);
        public const uint MagicC031 = 'C' | ((uint)'0' << 8) | ((uint)'3' << 16) | ((uint)'1' << 24);
        public const uint MagicC033 = 'C' | ((uint)'0' << 8) | ((uint)'3' << 16) | ((uint)'3' << 24);
        public const uint MagicC034 = 'C' | ((uint)'0' << 8) | ((uint)'3' << 16) | ((uint)'4' << 24);
        public const uint MagicC042 = 'C' | ((uint)'0' << 8) | ((uint)'4' << 16) | ((uint)'2' << 24);
        public const uint MagicC043 = 'C' | ((uint)'0' << 8) | ((uint)'4' << 16) | ((uint)'3' << 24);
        public const uint MagicC053 = 'C' | ((uint)'0' << 8) | ((uint)'5' << 16) | ((uint)'3' << 24);
        public const uint MagicC063 = 'C' | ((uint)'0' << 8) | ((uint)'6' << 16) | ((uint)'3' << 24);
        public const uint MagicC067 = 'C' | ((uint)'0' << 8) | ((uint)'6' << 16) | ((uint)'7' << 24);
        public const uint MagicC068 = 'C' | ((uint)'0' << 8) | ((uint)'6' << 16) | ((uint)'8' << 24);
        public const uint MagicC075 = 'C' | ((uint)'0' << 8) | ((uint)'7' << 16) | ((uint)'5' << 24);
        public const uint MagicC083 = 'C' | ((uint)'0' << 8) | ((uint)'8' << 16) | ((uint)'3' << 24);
        public const uint MagicC084 = 'C' | ((uint)'0' << 8) | ((uint)'8' << 16) | ((uint)'4' << 24);
        public const uint MagicC088 = 'C' | ((uint)'0' << 8) | ((uint)'8' << 16) | ((uint)'8' << 24);
        public const uint MagicC089 = 'C' | ((uint)'0' << 8) | ((uint)'8' << 16) | ((uint)'9' << 24);
        public const uint MagicC093 = 'C' | ((uint)'0' << 8) | ((uint)'9' << 16) | ((uint)'3' << 24);
        public const uint MagicC094 = 'C' | ((uint)'0' << 8) | ((uint)'9' << 16) | ((uint)'4' << 24);
        public const uint MagicC107 = 'C' | ((uint)'1' << 8) | ((uint)'0' << 16) | ((uint)'7' << 24);
        public const uint MagicC117 = 'C' | ((uint)'1' << 8) | ((uint)'1' << 16) | ((uint)'7' << 24);
        public const uint MagicC118 = 'C' | ((uint)'1' << 8) | ((uint)'1' << 16) | ((uint)'8' << 24);
        public const uint MagicC120 = 'C' | ((uint)'1' << 8) | ((uint)'2' << 16) | ((uint)'0' << 24);
        public const uint MagicC124 = 'C' | ((uint)'1' << 8) | ((uint)'2' << 16) | ((uint)'4' << 24);
        public const uint MagicC125 = 'C' | ((uint)'1' << 8) | ((uint)'2' << 16) | ((uint)'5' << 24);
        public const uint MagicC131 = 'C' | ((uint)'1' << 8) | ((uint)'3' << 16) | ((uint)'1' << 24);
        public const uint MagicC136 = 'C' | ((uint)'1' << 8) | ((uint)'3' << 16) | ((uint)'6' << 24);
        public const uint MagicC139 = 'C' | ((uint)'1' << 8) | ((uint)'3' << 16) | ((uint)'9' << 24);
        public const uint MagicC142 = 'C' | ((uint)'1' << 8) | ((uint)'4' << 16) | ((uint)'2' << 24);
        public const uint MagicC168 = 'C' | ((uint)'1' << 8) | ((uint)'6' << 16) | ((uint)'8' << 24);
        public const uint MagicC173 = 'C' | ((uint)'1' << 8) | ((uint)'7' << 16) | ((uint)'3' << 24);
        public const uint MagicC174 = 'C' | ((uint)'1' << 8) | ((uint)'7' << 16) | ((uint)'4' << 24);
        public const uint MagicC175 = 'C' | ((uint)'1' << 8) | ((uint)'7' << 16) | ((uint)'5' << 24);
        public const uint MagicC177 = 'C' | ((uint)'1' << 8) | ((uint)'7' << 16) | ((uint)'7' << 24);
        public const uint MagicC178 = 'C' | ((uint)'1' << 8) | ((uint)'7' << 16) | ((uint)'8' << 24);
        public const uint MagicC187 = 'C' | ((uint)'1' << 8) | ((uint)'8' << 16) | ((uint)'7' << 24);
        public const uint MagicC188 = 'C' | ((uint)'1' << 8) | ((uint)'8' << 16) | ((uint)'8' << 24);
        public const uint MagicC192 = 'C' | ((uint)'1' << 8) | ((uint)'9' << 16) | ((uint)'2' << 24);
        public const uint MagicC197 = 'C' | ((uint)'1' << 8) | ((uint)'9' << 16) | ((uint)'7' << 24);
        public const uint MagicC198 = 'C' | ((uint)'1' << 8) | ((uint)'9' << 16) | ((uint)'8' << 24);
        public const uint MagicC202 = 'C' | ((uint)'2' << 8) | ((uint)'0' << 16) | ((uint)'2' << 24);
        public const uint MagicC203 = 'C' | ((uint)'2' << 8) | ((uint)'0' << 16) | ((uint)'3' << 24);
        public const uint MagicC204 = 'C' | ((uint)'2' << 8) | ((uint)'0' << 16) | ((uint)'4' << 24);
        public const uint MagicC211 = 'C' | ((uint)'2' << 8) | ((uint)'1' << 16) | ((uint)'1' << 24);
        public const uint MagicC212 = 'C' | ((uint)'2' << 8) | ((uint)'1' << 16) | ((uint)'2' << 24);


        public uint   Magic;
        public int    Size;
        public uint   ExtraSize;
        public string Path = string.Empty;
        public short  Id;
        public short  Time;

        private TmbEntry(uint magic, int size)
        {
            Magic = magic;
            Size  = size;
        }

        public static TmbEntry Parse(ref byte* ptr, byte* endPtr)
        {
            var availableBytes = endPtr - ptr;
            if (availableBytes < 8)
                throw new Exception("Entry has not enough space for its magic number and size.");

            var ret = new TmbEntry(*(uint*)ptr, *(int*)(ptr += 4));
            if (availableBytes < ret.Size)
                throw new Exception("Entry has not enough space for its own reported size.");

            ptr += 4;

            return ret.Magic switch
            {
                MagicTMDH => Tmdh(ref ptr, ret),
                MagicTMPP => Tmpp(ref ptr, ret),
                MagicTMAL => Tmal(ref ptr, ret),
                MagicTMAC => Tmac(ref ptr, ret),
                MagicTMTR => Tmtr(ref ptr, ret),
                MagicC002 => C002(ref ptr, ret),
                MagicC006 => C006(ref ptr, ret),
                MagicC009 => C009(ref ptr, ret),
                MagicC010 => C010(ref ptr, ret),
                MagicC011 => C011(ref ptr, ret),
                MagicC012 => C012(ref ptr, ret),
                MagicC013 => C013(ref ptr, ret),
                MagicC014 => C014(ref ptr, ret),
                MagicC015 => C015(ref ptr, ret),
                MagicC021 => C021(ref ptr, ret),
                MagicC031 => C031(ref ptr, ret),
                MagicC033 => C033(ref ptr, ret),
                MagicC034 => C034(ref ptr, ret),
                MagicC042 => C042(ref ptr, ret),
                MagicC043 => C043(ref ptr, ret),
                MagicC053 => C053(ref ptr, ret),
                MagicC063 => C063(ref ptr, ret),
                MagicC067 => C067(ref ptr, ret),
                MagicC068 => C068(ref ptr, ret),
                MagicC075 => C075(ref ptr, ret),
                MagicC083 => C083(ref ptr, ret),
                MagicC084 => C084(ref ptr, ret),
                MagicC088 => C088(ref ptr, ret),
                MagicC089 => C089(ref ptr, ret),
                MagicC093 => C093(ref ptr, ret),
                MagicC094 => C094(ref ptr, ret),
                MagicC107 => C107(ref ptr, ret),
                MagicC117 => C117(ref ptr, ret),
                MagicC118 => C118(ref ptr, ret),
                MagicC120 => C120(ref ptr, ret),
                MagicC124 => C124(ref ptr, ret),
                MagicC125 => C125(ref ptr, ret),
                MagicC131 => C131(ref ptr, ret),
                MagicC136 => C136(ref ptr, ret),
                MagicC139 => C139(ref ptr, ret),
                MagicC142 => C142(ref ptr, ret),
                MagicC168 => C168(ref ptr, ret),
                MagicC173 => C173(ref ptr, ret),
                MagicC174 => C174(ref ptr, ret),
                MagicC175 => C175(ref ptr, ret),
                MagicC177 => C177(ref ptr, ret),
                MagicC178 => C178(ref ptr, ret),
                MagicC187 => C187(ref ptr, ret),
                MagicC188 => C188(ref ptr, ret),
                MagicC192 => C192(ref ptr, ret),
                MagicC197 => C197(ref ptr, ret),
                MagicC198 => C198(ref ptr, ret),
                MagicC202 => C202(ref ptr, ret),
                MagicC203 => C203(ref ptr, ret),
                MagicC204 => C204(ref ptr, ret),
                MagicC211 => C211(ref ptr, ret),
                MagicC212 => C212(ref ptr, ret),
                _         => Unknown(ref ptr, ret),
            };
        }

        public static TmbEntry Tmdh(ref byte* ptr, TmbEntry entry)
        {
            entry.Id                 = Parse<short>(ref ptr);
            var (unk1, length, unk2) = Parse3<short>(ref ptr);
            CheckSize(8 + 4 * sizeof(short), entry.Size);
            return entry;
        }

        public static TmbEntry Tmpp(ref byte* ptr, TmbEntry entry)
        {
            entry.Path = ParsePathOffset(ptr, ref ptr) + ".pap";
            CheckSize(8 + sizeof(uint), entry.Size);
            return entry;
        }

        public static TmbEntry Tmal(ref byte* ptr, TmbEntry entry)
        {
            var (timelineOffset, timelineCount) = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry Tmac(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)              = Parse2<short>(ref ptr);
            var (abilityDelay, unk1)            = Parse2<int>(ref ptr);
            var (timelineOffset, timelineCount) = Parse2<uint>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int) + 2 * sizeof(uint), entry.Size);
            return entry;
        }

        public static TmbEntry Tmtr(ref byte* ptr, TmbEntry entry)
        {
            var startPosition = ptr;
            (entry.Id, entry.Time)                         = Parse2<short>(ref ptr);
            var (timelineOffset, timelineCount, luaOffset) = Parse3<uint>(ref ptr);
            if (luaOffset != 0)
            {
                var dataPos = startPosition + luaOffset;
                var (unk1, luaCount) = Parse2<uint>(ref dataPos);
                if (unk1 != 8)
                    throw new Exception($"Lua entry is not correct: {unk1} should be 8.");

                entry.ExtraSize = 8 + 12 * luaCount;
            }

            CheckSize(8 + 2 * sizeof(short) + 3 * sizeof(uint), entry.Size);
            return entry;
        }

        public static TmbEntry C002(ref byte* ptr, TmbEntry entry)
        {
            var startPointer = ptr;
            (entry.Id, entry.Time)     = Parse2<short>(ref ptr);
            var (duration, unk1, unk2) = Parse3<int>(ref ptr);
            entry.Path                 = ParsePathOffset(startPointer, ref ptr) + ".tmb";
            CheckSize(8 + 2 * sizeof(short) + 3 * sizeof(int) + sizeof(uint), entry.Size);
            return entry;
        }

        /// <summary> Fly Text </summary>
        public static TmbEntry C006(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)    = Parse2<short>(ref ptr);
            var (enabled, unk1, unk2) = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 3 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Animation (.pap only) </summary>
        public static TmbEntry C009(ref byte* ptr, TmbEntry entry)
        {
            var startPointer = ptr;
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (duration, unk1)   = Parse2<int>(ref ptr);
            entry.Path             = ParsePathOffset(startPointer, ref ptr) + ".pap";
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int) + sizeof(uint), entry.Size);
            return entry;
        }

        /// <summary> Animation </summary>
        public static TmbEntry C010(ref byte* ptr, TmbEntry entry)
        {
            var startPointer = ptr;
            (entry.Id, entry.Time)             = Parse2<short>(ref ptr);
            var (duration, unk1, flags)        = Parse3<int>(ref ptr);
            var (animationStart, animationEnd) = Parse2<float>(ref ptr);
            entry.Path                         = ParsePathOffset(startPointer, ref ptr) + ".pap";
            var unk2 = Parse<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int) + 2 * sizeof(float) + sizeof(uint), entry.Size);
            return entry;
        }

        /// <summary> Fly Text </summary>
        public static TmbEntry C011(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2)       = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> VFX </summary>
        public static TmbEntry C012(ref byte* ptr, TmbEntry entry)
        {
            var startPointer = ptr;
            (entry.Id, entry.Time)                                       = Parse2<short>(ref ptr);
            var (duration, unk1)                                         = Parse2<int>(ref ptr);
            entry.Path                                                   = ParsePathOffset(startPointer, ref ptr);
            var (bindPoint1, bindPoint2, bindPoint3, bindPoint4)         = Parse4<short>(ref ptr);
            var (offsetScale, countScale, offsetRotation, countRotation) = Parse4<uint>(ref ptr);
            var (offsetPosition, countPosition, offsetRgba, countRgba)   = Parse4<uint>(ref ptr);
            entry.ExtraSize                                              = sizeof(float) * (3 + 3 + 3 + 4);
            var (visibility, unk2)                                       = Parse2<int>(ref ptr);
            CheckSize(8 + 6 * sizeof(short) + 4 * sizeof(int) + 9 * sizeof(uint), entry.Size);
            return entry;
        }

        public static TmbEntry C013(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)       = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3, unk4) = Parse4<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Weapon Position </summary>
        public static TmbEntry C014(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)                             = Parse2<short>(ref ptr);
            var (enabled, unk1, objectPosition, objectControl) = Parse4<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Weapon Size </summary>
        public static TmbEntry C015(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)                      = Parse2<short>(ref ptr);
            var (unk1, unk2, weaponSize, objectControl) = Parse4<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C021(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)       = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3, unk4) = Parse4<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Summon Animation </summary>
        public static TmbEntry C031(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)      = Parse2<short>(ref ptr);
            var (duration, unk1)        = Parse2<int>(ref ptr);
            var (animation, targetType) = Parse2<ushort>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int) + 2 * sizeof(ushort), entry.Size);
            return entry;
        }

        /// <summary> Crafting Delay </summary>
        public static TmbEntry C033(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (enabled, unk1)    = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Gathering Delay </summary>
        public static TmbEntry C034(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (enabled, unk1)    = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Footstep </summary>
        public static TmbEntry C042(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)               = Parse2<short>(ref ptr);
            var (enabled, unk1, footId, soundId) = Parse4<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Summon Weapon </summary>
        public static TmbEntry C043(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)     = Parse2<short>(ref ptr);
            var (duration, unk1, unk2) = Parse3<int>(ref ptr);
            var (weaponId, bodyId)     = Parse2<short>(ref ptr);
            var variantId = Parse<int>(ref ptr);
            CheckSize(8 + 4 * sizeof(short) + 4 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Voice Line </summary>
        public static TmbEntry C053(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)                = Parse2<short>(ref ptr);
            var (unk1, unk2)                      = Parse2<int>(ref ptr);
            var (soundId1, soundId2, unk3, flags) = Parse4<short>(ref ptr);
            CheckSize(8 + 6 * sizeof(short) + 2 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Sound </summary>
        public static TmbEntry C063(ref byte* ptr, TmbEntry entry)
        {
            var startPointer = ptr;
            (entry.Id, entry.Time)          = Parse2<short>(ref ptr);
            var (loop, interrupt)           = Parse2<int>(ref ptr);
            entry.Path                      = ParsePathOffset(startPointer, ref ptr);
            var (soundIndex, soundPosition) = Parse2<uint>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int) + sizeof(uint), entry.Size);
            return entry;
        }

        /// <summary> Flinch </summary>
        public static TmbEntry C067(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (enabled, unk2)    = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Shade Color </summary>
        public static TmbEntry C068(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)                                     = Parse2<short>(ref ptr);
            var (unk1, unk2)                                           = Parse2<int>(ref ptr);
            var (offsetColor1, countColor1, offsetColor2, countColor2) = Parse4<uint>(ref ptr);
            entry.ExtraSize                                            = sizeof(float) * 4 * 2;
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int) + 4 * sizeof(uint), entry.Size);
            return entry;
        }

        /// <summary> Terrain VFX </summary>
        public static TmbEntry C075(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)                                       = Parse2<short>(ref ptr);
            var (enabled, unk1, shape)                                   = Parse3<int>(ref ptr);
            var (offsetScale, countScale, offsetRotation, countRotation) = Parse4<uint>(ref ptr);
            var (offsetPosition, countPosition, offsetRgba, countRgba)   = Parse4<uint>(ref ptr);
            var (unk2, unk3)                                             = Parse2<int>(ref ptr);
            entry.ExtraSize                                              = sizeof(float) * (3 + 3 + 3 + 4);
            CheckSize(8 + 2 * sizeof(short) + 5 * sizeof(int) + 8 * sizeof(uint), entry.Size);
            return entry;
        }

        public static TmbEntry C083(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3) = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 3 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C084(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3) = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 3 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C088(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2)       = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C089(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3) = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 3 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Color </summary>
        public static TmbEntry C093(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)                                     = Parse2<short>(ref ptr);
            var (duration, unk1)                                       = Parse2<int>(ref ptr);
            var (color1Offset, color1Count, color2Offset, color2Count) = Parse4<uint>(ref ptr);
            var unk2 = Parse<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 3 * sizeof(int) + 8 * sizeof(uint), entry.Size);
            return entry;
        }

        /// <summary> Invisibility </summary>
        public static TmbEntry C094(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)               = Parse2<short>(ref ptr);
            var (fadeTime, unk1)                 = Parse2<int>(ref ptr);
            var (startVisibility, endVisibility) = Parse2<float>(ref ptr);
            var extraOffset = Parse<uint>(ref ptr);
            entry.ExtraSize = extraOffset == 0 ? 0 : 5u * sizeof(int);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int) + 2 * sizeof(float) + sizeof(uint), entry.Size);
            return entry;
        }

        public static TmbEntry C107(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)       = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3, unk4) = Parse4<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C117(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)       = Parse2<short>(ref ptr);
            var (duration, unk1, tmfcId) = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 3 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C118(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3) = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 3 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C120(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3) = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 3 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C124(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3) = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 3 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Animation Lock </summary>
        public static TmbEntry C125(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (duration, unk1)   = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Animation Cancelled by Movement </summary>
        public static TmbEntry C131(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2)       = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C136(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2)       = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C139(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2)       = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Freeze Position </summary>
        public static TmbEntry C142(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)               = Parse2<short>(ref ptr);
            var (duration, unk1, position, unk2) = Parse4<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C168(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)         = Parse2<short>(ref ptr);
            var (unk1, unk2, tmfcId, unk3) = Parse4<int>(ref ptr);
            var (unk4, unk5, unk6, unk7)   = Parse4<int>(ref ptr);
            var (unk8, unk9, unk10)        = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 11 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> VFX </summary>
        public static TmbEntry C173(ref byte* ptr, TmbEntry entry)
        {
            var startPointer = ptr;
            (entry.Id, entry.Time)       = Parse2<short>(ref ptr);
            var (unk1, unk2)             = Parse2<int>(ref ptr);
            entry.Path                   = ParsePathOffset(startPointer, ref ptr);
            var (bindPoint1, bindPoint2) = Parse2<short>(ref ptr);
            var (unk3, unk4, unk5, unk6) = Parse4<int>(ref ptr);
            var (unk7, unk8, unk9)       = Parse3<int>(ref ptr);
            var (unk10, unk11, unk12)    = Parse3<int>(ref ptr);
            CheckSize(8 + 4 * sizeof(short) + 12 * sizeof(int) + sizeof(uint), entry.Size);
            return entry;
        }

        /// <summary> Object Control </summary>
        public static TmbEntry C174(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)                              = Parse2<short>(ref ptr);
            var (duration, unk1, objectPosition, objectControl) = Parse4<int>(ref ptr);
            var (finalPosition, positionDelay, unk2)            = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 7 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Object Scaling </summary>
        public static TmbEntry C175(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)                    = Parse2<short>(ref ptr);
            var (duration, unk1, unk2, objectControl) = Parse4<int>(ref ptr);
            var (unk5, unk6, unk7)                    = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 7 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C177(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)       = Parse2<short>(ref ptr);
            var (duration, unk1, tmfcId) = Parse3<int>(ref ptr);
            var (unk2, unk3, unk4)       = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 6 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C178(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)       = Parse2<short>(ref ptr);
            var (duration, unk1, tmfcId) = Parse3<int>(ref ptr);
            var (unk2, unk3, unk4)       = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 6 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C187(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3) = Parse3<int>(ref ptr);
            var (unk4, unk5)       = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 5 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C188(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2)       = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 2 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Voice Line </summary>
        public static TmbEntry C192(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)                  = Parse2<short>(ref ptr);
            var (unk1, unk2, voiceLineNumber, unk3) = Parse4<int>(ref ptr);
            var (unk4, unk5, unk6, unk7)            = Parse4<int>(ref ptr);
            var (unk8, unk9, unk10)                 = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 11 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Voice Line </summary>
        public static TmbEntry C197(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)                             = Parse2<short>(ref ptr);
            var (fadeTime, unk1, voiceLineNUmber, bindPointId) = Parse4<int>(ref ptr);
            var (unk2, unk3)                                   = Parse2<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 6 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Lemure </summary>
        public static TmbEntry C198(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)           = Parse2<short>(ref ptr);
            var (duration, unk1, unk2, unk3) = Parse4<int>(ref ptr);
            var unk4 = Parse<int>(ref ptr);
            var (modelId, bodyId) = Parse2<short>(ref ptr);
            var unk5 = Parse<int>(ref ptr);
            CheckSize(8 + 4 * sizeof(short) + 7 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C202(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time) = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3) = Parse3<int>(ref ptr);
            var (unk4, unk5, unk6) = Parse3<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 6 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Summon Weapon Visibility </summary>
        public static TmbEntry C203(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)          = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3, unk4)    = Parse4<int>(ref ptr);
            var (objectControl, unk5, unk6) = Parse3<int>(ref ptr);
            var unk7 = Parse<float>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 7 * sizeof(int) + sizeof(float), entry.Size);
            return entry;
        }

        /// <summary> Reaper Shroud </summary>
        public static TmbEntry C204(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)           = Parse2<short>(ref ptr);
            var (duration, unk1, unk2, unk3) = Parse4<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int), entry.Size);
            return entry;
        }

        /// <summary> Lock Facing Direction </summary>
        public static TmbEntry C211(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)           = Parse2<short>(ref ptr);
            var (duration, unk1, unk2, unk3) = Parse4<int>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int), entry.Size);
            return entry;
        }

        public static TmbEntry C212(ref byte* ptr, TmbEntry entry)
        {
            (entry.Id, entry.Time)       = Parse2<short>(ref ptr);
            var (unk1, unk2, unk3, unk4) = Parse4<int>(ref ptr);
            var unk5 = Parse<float>(ref ptr);
            CheckSize(8 + 2 * sizeof(short) + 4 * sizeof(int) + sizeof(float), entry.Size);
            return entry;
        }

        public static TmbEntry Unknown(ref byte* ptr, TmbEntry entry)
        {
            Debug.Assert(entry.Size > 8);
            ptr += entry.Size - 8;
            return entry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static string ParsePathOffset(byte* startPtr, ref byte* ptr)
        {
            var offset = Parse<uint>(ref ptr);
            return MemoryHelper.ReadStringNullTerminated((nint)(startPtr + offset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static T Parse<T>(ref byte* ptr) where T : unmanaged
        {
            var ret = *(T*)ptr;
            ptr += sizeof(T);
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static (T, T) Parse2<T>(ref byte* ptr) where T : unmanaged
        {
            var ret1 = Parse<T>(ref ptr);
            var ret2 = Parse<T>(ref ptr);
            return (ret1, ret2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static (T, T, T) Parse3<T>(ref byte* ptr) where T : unmanaged
        {
            var ret1 = Parse<T>(ref ptr);
            var ret2 = Parse<T>(ref ptr);
            var ret3 = Parse<T>(ref ptr);
            return (ret1, ret2, ret3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static (T, T, T, T) Parse4<T>(ref byte* ptr) where T : unmanaged
        {
            var ret1 = Parse<T>(ref ptr);
            var ret2 = Parse<T>(ref ptr);
            var ret3 = Parse<T>(ref ptr);
            var ret4 = Parse<T>(ref ptr);
            return (ret1, ret2, ret3, ret4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static T[] ParseN<T>(ref byte* ptr, int n) where T : unmanaged
        {
            var ret = new T[n];
            for (var i = 0; i < n; ++i)
                ret[i] = Parse<T>(ref ptr);
            return ret;
        }

        private static void CheckSize(int expected, int actual)
        {
            if (expected != actual)
                throw new Exception("TMB entry did not have expected size.");
        }
    }
}
