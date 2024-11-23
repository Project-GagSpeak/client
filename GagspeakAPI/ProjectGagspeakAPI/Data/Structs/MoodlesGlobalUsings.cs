
// A global tuple statement for moodle status info.
global using MoodlesStatusInfo = (
    System.Guid GUID,
    int IconID,
    string Title,
    string Description,
    GagspeakAPI.Data.IPC.StatusType Type,
    string Applier,
    bool Dispelable,
    int Stacks,
    bool Persistent,
    int Days,
    int Hours,
    int Minutes,
    int Seconds,
    bool NoExpire,
    bool AsPermanent
);

global using IPCProfileDataTuple = (System.Guid UniqueId, string Name, string VirtualPath, string CharacterName, bool IsEnabled);

