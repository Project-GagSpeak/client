using FFXIVClientStructs.FFXIV.Component.GUI;

namespace GagSpeak.Game.Readers;
#nullable disable

/// <summary>
///     The AtkReader for Apartment & Private Estate Selections.
/// </summary>
public unsafe class AtkMansionSelectRoomReader(AtkUnitBase* unitBase, int beginOffset = 0) : AtkReaderBase(unitBase, beginOffset)
{
    /// <summary>
    ///     Loading Status of the MansionSelectRoom.
    /// </summary>
    public uint LoadStatus => ReadUInt(0) ?? 0;

    /// <summary>
    ///     If the MansionSelectRoom is loaded.
    /// </summary>
    public bool IsLoaded => LoadStatus == 4;

    /// <summary>
    ///     Which room selection panel we are on.
    /// </summary>
    public int Section => ReadInt(1) ?? -1;

    /// <summary>
    ///     How many sections does this menu have to choose from?
    /// </summary>
    public uint ExistingSectionsCount => ReadUInt(5) ?? 0;

    /// <summary>
    ///     How many rooms exist in the chosen section?
    /// </summary>
    public uint SectionRoomsCount => ReadUInt(41) ?? 0;

    /// <summary>
    ///     Track the internal data of all Room Instances at the 42nd offset, mapping the list of room pointers, each 12 in size, 15 per menu.
    /// </summary>
    public List<RoomInfo> Rooms => Loop<RoomInfo>(42, 12, 15);

    /// <summary>
    ///     RoomInfo pointed to by the MansionSelectRoom to obtain details about each selected room.
    /// </summary>
    public class RoomInfo(nint unitBasePtr, int beginOffset = 0) : AtkReaderBase(unitBasePtr, beginOffset)
    {
        /// <summary>
        ///     The Access state of the room.
        /// </summary>
        public uint AccessState => ReadUInt(0) ?? 0;

        /// <summary>
        ///     Is it a yellow door (private), or blue open door (public) or unoccupied (grey door)?
        /// </summary>
        public int IconID => ReadInt(1) ?? 0;

        /// <summary>
        ///     The Number writen for the room.
        /// </summary>
        public string RoomNumber => ReadString(3);

        /// <summary>
        ///     Who owns the room?
        /// </summary>
        public string RoomOwner => ReadString(4);

        // Additional details about room description, tags, TBD.
    }
}
