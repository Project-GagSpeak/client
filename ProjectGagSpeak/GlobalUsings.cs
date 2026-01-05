/// Global Usings
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

/// Global Tuple
global using MoodlesStatusInfo = (
    int Version,
    System.Guid GUID,
    int IconID,
    string Title,
    string Description,
    string CustomVFXPath,               // What VFX to show on application.
    long ExpireTicks,                   // Permanent if -1, referred to as 'NoExpire' in MoodleStatus
    GagspeakAPI.StatusType Type,        // Moodles StatusType enum.
    int Stacks,                         // Usually 1 when no stacks are used.
    int StackSteps,                     // How many stacks to add per reapplication.
    GagspeakAPI.Modifiers Modifiers,    // What can be customized, casted to uint from Modifiers (Dalamud IPC Rules)
    System.Guid ChainedStatus,          // What status is chained to this one.
    GagspeakAPI.ChainTrigger ChainTrigger, // What triggers the chained status.
    string Applier,                     // Who applied the moodle.
    string Dispeller,                   // When set, only this person can dispel your moodle.
    bool Permanent                      // Referred to as 'Sticky' in the Moodles UI
);


global using MoodlePresetInfo = (
    System.Guid GUID,
    System.Collections.Generic.List<System.Guid> Statuses,
    GagspeakAPI.PresetApplicationType ApplicationType,
    string Title
);

// The IPC Tuple used to define MoodleAccess permission between recipient and client.
global using IPCMoodleAccessTuple = (
    GagspeakAPI.Enums.MoodleAccess OtherAccess, long OtherMaxTime,
    GagspeakAPI.Enums.MoodleAccess CallerAccess, long CallerMaxTime
);

// Dalamud's Newtonsoft-based converter for objects does not play nice with nested [Flag] Enums in tuples, inside dictionaries.
global using ProviderMoodleAccessTuple = (
    short OtherAccessFlags, long OtherMaxTime, 
    short CallerAccessFlags, long CallerMaxTime
);

global using IPCProfileDataTuple = (
    System.Guid UniqueId,
    string Name,
    string VirtualPath,
    System.Collections.Generic.List<(string Name, ushort WorldId, byte CharacterType, ushort CharacterSubType)> Characters,
    int Priority,
    bool IsEnabled
);

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
