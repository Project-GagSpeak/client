namespace GagspeakAPI.Enums;

public enum PermissionType 
{
    Global, // Use GlobalPerms & PairPermEditAccess
    UniquePairPerm, // Use UniquePairPerms & PairPermEditAccess
    UniquePairPermEditAccess, // Used to validate the edit access checkbox catagory, not used in sensarios.
    Hardcore // Special case where Action is GlobalPerm, and edit access is UniquePairPerm
};
