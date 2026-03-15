global using Dalamud.Utility;
global using Newtonsoft.Json;
global using Newtonsoft.Json.Linq;
global using Microsoft.Extensions.Logging;
global using System.Collections.Concurrent;
global using System.Collections;
global using System.Diagnostics;
global using System.Text;
global using System.Numerics;
global using GagspeakAPI.Enums;
global using GagspeakAPI;
global using ITFlags = Dalamud.Bindings.ImGui.ImGuiInputTextFlags;
global using CFlags = Dalamud.Bindings.ImGui.ImGuiComboFlags;
global using DFlags = Dalamud.Bindings.ImGui.ImDrawFlags;
global using WFlags = Dalamud.Bindings.ImGui.ImGuiWindowFlags;
global using FAI = Dalamud.Interface.FontAwesomeIcon;

// MERV! DON'T SUMMON BAGAGWA!
global using Bagagwa = System.Exception;

// Deprecated Moodles data, only preserve for fetched uniformity.
global using DeprecatedPresetInfo = (System.Guid GUID, System.Collections.Generic.List<System.Guid> Statuses, byte ApplicationType, string Title);
global using DeprecatedStatusInfo = (int Ver, System.Guid GUID, int IconID, string Title, string Desc, string Vfx, long ExpireTicks,
    int Type, int Stacks, int Steps, uint Mods, System.Guid ChainedStatus, int ChainCond, string Applier, string Dispeller, bool Perma);



// Enums use the API so they work for transfer.
// The tuples function to bridge Client <-> LociAPI.
// Convert to GagSpeakAPI structs for Client <-> GagspeakAPI <-> Server
global using LociStatusInfo = (
    int Version,
    System.Guid GUID,
    uint IconID,
    string Title,
    string Description,
    string CustomVFXPath,                   // What VFX to show on application.
    long ExpireTicks,                       // Permanent if -1, referred to as 'NoExpire' in LociStatus
    LociApi.Enums.StatusType Type,          // Loci StatusType enum.
    int Stacks,                             // Usually 1 when no stacks are used.
    int StackSteps,                         // How many stacks to add per reapplication.
    int StackToChain,                       // Used for chaining on set stacks
    uint Modifiers,                         // What can be customized, casted to uint from Modifiers (Dalamud IPC Rules)
    System.Guid ChainedGUID,                // What status is chained to this one.
    LociApi.Enums.ChainType ChainType,      // What type of chaining is this for.
    LociApi.Enums.ChainTrigger ChainTrigger,// What triggers the chained status.
    string Applier,                         // Who applied the status.
    string Dispeller                        // When set, only this person can dispel your loci.
);

global using LociPresetInfo = (
    int Version,
    System.Guid GUID,
    System.Collections.Generic.List<System.Guid> Statuses,
    byte ApplicationType,
    string Title,
    string Description
);

// Placeholder for now, additional details in the future, as this is WIP

// For Customize+
global using IPCProfileDataTuple = (
    System.Guid UniqueId,
    string Name,
    string VirtualPath,
    System.Collections.Generic.List<(string Name, ushort WorldId, byte CharacterType, ushort CharacterSubType)> Characters,
    int Priority,
    bool IsEnabled
);

// For Lifestream
global using AddressBookEntryTuple = (
    string Name, 
    int World, 
    int City, 
    int Ward, 
    int PropertyType, 
    int Plot, 
    int Apartment, 
    bool ApartmentSubdivision, 
    bool AliasEnabled, 
    string Alias
);
