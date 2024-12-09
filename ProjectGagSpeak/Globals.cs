

/// Global Usings
global using Newtonsoft.Json;
global using Newtonsoft.Json.Linq;
global using Microsoft.Extensions.Logging;
global using System.Collections.Concurrent;
global using System.Collections;
global using System.Diagnostics;
global using System.Text;
global using GagspeakAPI.Enums;
global using GagspeakAPI;

/// Global Tuples
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
    bool AsPermanent,
    System.Guid StatusOnDispell,
    string CustomVFXPath,
    bool StackOnReapply
    );

global using MoodlePresetInfo = (
    System.Guid GUID,
    System.Collections.Generic.List<System.Guid> Statuses,
    GagspeakAPI.Data.IPC.PresetApplicationType ApplicationType,
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

// See later https://github.com/xivdev/Penumbra/blob/5c5e45114f25f9429d8757b6edf852ecc37173c9/Penumbra/UI/LaunchButton.cs#L27

