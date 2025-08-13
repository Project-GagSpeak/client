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
    System.Guid GUID,
    int IconID,
    string Title,
    string Description,
    GagspeakAPI.Enums.StatusType Type,
    string Applier,
    bool Dispelable,
    int Stacks,
    bool Persistent,
    int Days,
    int Hours,
    int Minutes,
    int Seconds,
    bool NoExpire,
    bool AsPermanent,
    System.Guid StatusOnDispell,
    string CustomVFXPath,
    bool StackOnReapply,
    int StacksIncOnReapply
    );

global using MoodlePresetInfo = (
    System.Guid GUID,
    System.Collections.Generic.List<System.Guid> Statuses,
    GagspeakAPI.Enums.PresetApplicationType ApplicationType,
    string Title
);

global using MoodlesGSpeakPairPerms = (
    bool AllowPositive,
    bool AllowNegative,
    bool AllowSpecial,
    bool AllowApplyingOwnMoodles,
    bool AllowApplyingPairsMoodles,
    System.TimeSpan MaxDuration,
    bool AllowPermanent,
    bool AllowRemoval
    );

global using IPCCharacterDataTuple = (string Name, ushort WorldId, byte CharacterType, ushort CharacterSubType);

global using IPCProfileDataTuple = (
    System.Guid UniqueId,
    string Name,
    string VirtualPath,
    System.Collections.Generic.List<(string Name, ushort WorldId, byte CharacterType, ushort CharacterSubType)> Characters,
    int Priority,
    bool IsEnabled);

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
