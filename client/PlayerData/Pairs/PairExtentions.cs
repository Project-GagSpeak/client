using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Textures;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace GagSpeak.Utils;

public static class PairExtensions
{
    public static string ActiveSetName(this Pair pair)
    {
        if(pair.LastWardrobeData?.ActiveSetId.IsEmptyGuid() ?? true)
            return "No Set Active";
        // return the active set.
        return pair.LastLightStorage?.Restraints.FirstOrDefault(x => x.Identifier == pair.LastWardrobeData?.ActiveSetId)?.Name ?? "No Set Active";
    }
}
