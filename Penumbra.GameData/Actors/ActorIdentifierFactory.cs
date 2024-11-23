using System.Collections.Frozen;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using ImGuiNET;
using OtterGui;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using Penumbra.String;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace Penumbra.GameData.Actors;

/// <summary> Creation of ActorIdentifiers. </summary>
public class ActorIdentifierFactory(ObjectManager _objects, IFramework _framework, NameDicts _data, CutsceneResolver _toParentIdx)
{
    /// <summary> Expose the _toParentIdx function for convenience. </summary>
    /// <returns> The parent index for a cutscene object or -1 if no parent exists. </returns>
    public short ToCutsceneParent(ushort index)
        => _toParentIdx.Invoke(index);

    /// <summary> Used in construction from user strings. </summary>
    public class IdentifierParseError(string reason) : Exception(reason);

    /// <summary> Create an ImGui Tooltip for user strings. </summary>
    public static void WriteUserStringTooltip(bool withIndex)
    {
        using var tt   = ImRaii.Tooltip();
        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        ImGui.TextUnformatted("Valid formats for an Identifier String are:");

        const uint typeColor    = 0xFF40FF40;
        const uint nameColor    = 0xFF00D0D0;
        const uint keyColor     = 0xFFFFD060;
        const uint npcTypeColor = 0xFFFF40FF;
        const uint npcNameColor = 0xFF4040FF;
        const uint indexColor   = 0xFFA0A0A0;

        ImGui.Bullet();
        ImGui.SameLine();
        ImGuiUtil.DrawColoredText(("P", typeColor), (" | ", keyColor), ("[Player Name]@<World Name>", nameColor));

        ImGui.Bullet();
        ImGui.SameLine();
        ImGuiUtil.DrawColoredText(("R", typeColor), (" | ", keyColor), ("[Retainer Name]", nameColor));

        ImGui.Bullet();
        ImGui.SameLine();
        ImGuiUtil.DrawColoredText(("N", typeColor), (" | ", keyColor), ("[NPC Type]", npcTypeColor), (" : ", keyColor),
            ("[Npc Name]", npcNameColor));
        if (withIndex)
        {
            ImGui.SameLine(0, 0);
            ImGuiUtil.DrawColoredText(("@", keyColor), ("<Object Index>", indexColor));
        }

        ImGui.Bullet();
        ImGui.SameLine();
        ImGuiUtil.DrawColoredText(("All [] or <> brackets are not to be included but are for placeholders, all", 0),
            (" bright blue key symbols", keyColor), (" are relevant.", 0));

        ImGui.Bullet();
        ImGui.SameLine();
        ImGuiUtil.DrawColoredText(("O", typeColor), (" | ", keyColor), ("[NPC Type]", npcTypeColor), (" : ", keyColor),
            ("[Npc Name]", npcNameColor), (" | ", keyColor), ("[Player Name]@<World Name>", nameColor));

        ImGui.NewLine();
        ImGui.Bullet();
        ImGui.SameLine();
        ImGuiUtil.DrawColoredText(("[P]", typeColor), ("layer, ", 0), ("[R]", typeColor), ("etainer, ", 0), ("[N]", typeColor), ("PC, or ", 0),
            ("[O]", typeColor), ("wned describe the identifier type.", 0));

        ImGui.NewLine();
        ImGui.Bullet();
        ImGui.SameLine();
        ImGuiUtil.DrawColoredText(("[Player Name]", nameColor), (" and ", 0), ("[Retainer Name]", nameColor),
            (" must agree with naming rules.", 0));

        ImGui.Bullet();
        ImGui.SameLine();
        ImGuiUtil.DrawColoredText(("<World Name>", nameColor), (" is optional (", 0), ("Any World", nameColor),
            (" when not provided), but must be a valid world otherwise.", 0));

        ImGui.Bullet();
        ImGui.SameLine();
        ImGuiUtil.DrawColoredText(("[NPC Type]", npcTypeColor), (" can be ", 0), ("[M]", npcTypeColor), ("ount, ", 0), ("[C]", npcTypeColor),
            ("ompanion, ", 0), ("[A]", npcTypeColor), ("ccessory, ", 0), ("[E]", npcTypeColor), ("vent NPC, or", 0), ("[B]", npcTypeColor),
            ("attle NPC.", 0));

        ImGui.Bullet();
        ImGui.SameLine();
        ImGuiUtil.DrawColoredText(("[NPC Name]", npcNameColor), (" must be a valid, known NPC name for the chosen type.", 0));

        if (withIndex)
        {
            ImGui.Bullet();
            ImGui.SameLine();
            ImGuiUtil.DrawColoredText(("<Object Index>", indexColor),
                (" is optional and must be a non-negative index into the object table.", 0));
        }
    }

    /// <summary> Convert a string into a list of ActorIdentifiers. </summary>
    /// <param name="userString"> The input string. </param>
    /// <param name="allowIndex"> Whether the conversion allows and respects game object indices. </param>
    /// <returns> All matching actor identifiers. </returns>
    /// <exception cref="IdentifierParseError"> Any failure to convert throws an exception with the failure reason. </exception>
    /// <remarks>
    /// Valid formats are:
    /// <list type="bullet">
    ///     <item><code>p|[Player Name]@{World Name}</code> players of name from world, world is optional</item>
    ///     <item><code>r|[Retainer Name]</code> retainers</item>
    ///     <item><code>n|[NPC Type]:[NPC Name]@{Object Index}</code> NPCs of type MCAEB and a given name, at an optional index which may be disallowed.</item>
    ///     <item><code>o|[NPC Type]:[NPC Name]|[Player Name]@{World Name}</code> Owned NPCs.</item>
    /// </list>
    /// </remarks>
    public ActorIdentifier[] FromUserString(string userString, bool allowIndex)
    {
        if (userString.Length == 0)
            throw new IdentifierParseError("The identifier string was empty.");

        var split = userString.Split('|', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (split.Length < 2)
            throw new IdentifierParseError($"The identifier string {userString} does not contain a type and a value.");

        switch (split[0].ToLowerInvariant())
        {
            case "p":
            case "player":
            {
                var (playerName, worldId) = ParsePlayer(split[1]);
                return [CreateIndividualUnchecked(IdentifierType.Player, playerName, worldId.Id, 0, 0)];
            }
            case "r":
            case "retainer":
            {
                if (!VerifyRetainerName(split[1]))
                    throw new IdentifierParseError($"{split[1]} is not a valid player name.");
                if (!ByteString.FromString(split[1], out var playerName))
                    throw new IdentifierParseError($"The retainer string {split[1]} contains invalid symbols.");

                return [CreateIndividualUnchecked(IdentifierType.Retainer, playerName, 0, 0, 0)];
            }
            case "n":
            case "npc":
            {
                var (kind, objectIds, worldId) = ParseNpc(split[1], allowIndex);
                return objectIds.Select(i => CreateIndividualUnchecked(IdentifierType.Npc, ByteString.Empty, worldId.Id, kind, i)).ToArray();
            }
            case "o":
            case "owned":
            {
                if (split.Length < 3)
                    throw new IdentifierParseError(
                        "Owned NPCs need a NPC and a player, separated by '|', but only one was provided.");

                var (kind, objectIds, _)  = ParseNpc(split[1], allowIndex);
                var (playerName, worldId) = ParsePlayer(split[2]);
                return objectIds.Select(i => CreateIndividualUnchecked(IdentifierType.Owned, playerName, worldId.Id, kind, i)).ToArray();
            }
            default:
                throw new IdentifierParseError(
                    $"{split[0]} is not a valid identifier type. Valid types are [P]layer, [R]etainer, [N]PC, or [O]wned");
        }
    }

    /// <summary> Compute an ActorIdentifier from a GameObject. </summary>
    /// <param name="actor"> The game object. Can be null. </param>
    /// <param name="owner"> A returned owner if the game object has an owner. </param>
    /// <param name="allowPlayerNpc"> Allow to treat npcs as players in certain cases for Anamnesis compatibility.</param>
    /// <param name="check"> Check obtained data for validity. </param>
    /// <param name="withoutIndex"> Skip the game object index for unowned NPCs. </param>
    /// <returns> An actor identifier for that object. </returns>
    public unsafe ActorIdentifier FromObject(Actor actor, out Actor owner, bool allowPlayerNpc, bool check, bool withoutIndex)
    {
        owner = Actor.Null;
        if (!actor.Valid)
            return ActorIdentifier.Invalid;

        actor = HandleCutscene(actor);
        var idx = actor.Index;
        if (idx.Index is >= (ushort)ScreenActor.CharacterScreen and <= (ushort)ScreenActor.Card8)
            return CreateIndividualUnchecked(IdentifierType.Special, ByteString.Empty, idx.Index, ObjectKind.None, uint.MaxValue);

        var kind = (ObjectKind)actor.AsObject->ObjectKind;
        return kind switch
        {
            ObjectKind.Player    => CreatePlayerFromObject(actor, check),
            ObjectKind.BattleNpc => CreateBNpcFromObject(actor, out owner, check, allowPlayerNpc, withoutIndex),
            ObjectKind.EventNpc  => CreateENpcFromObject(actor, check, withoutIndex),
            ObjectKind.MountType => CreateCompanionFromObject(actor, out owner, kind, check),
            ObjectKind.Companion => CreateCompanionFromObject(actor, out owner, kind, check),
            ObjectKind.Ornament  => CreateCompanionFromObject(actor, out owner, kind, check),
            ObjectKind.Retainer  => CreateRetainerFromObject(actor, check),
            _                    => CreateUnkFromObject(actor, withoutIndex),
        };
    }

    /// <inheritdoc cref="FromObject(Actor,out Actor,bool,bool,bool)"/>
    public ActorIdentifier FromObject(Dalamud.Game.ClientState.Objects.Types.IGameObject? actor,
        out Actor owner, bool allowPlayerNpc, bool check, bool withoutIndex)
        => FromObject(actor?.Address ?? nint.Zero, out owner, allowPlayerNpc, check, withoutIndex);

    /// <inheritdoc cref="FromObject(Actor,out Actor,bool,bool,bool)"/>
    public ActorIdentifier FromObject(Dalamud.Game.ClientState.Objects.Types.IGameObject? actor, bool allowPlayerNpc, bool check,
        bool withoutIndex)
        => FromObject(actor, out _, allowPlayerNpc, check, withoutIndex);

    /// <summary> Create an individual from existing data, which is checked for correctness. </summary>
    /// <param name="type"> The type of actor. </param>
    /// <param name="name"> The name of the actor for players, retainers and owned objects. </param>
    /// <param name="value"> WorldId, ScreenActor, ObjectIndex or RetainerType. </param>
    /// <param name="kind"> The object kind for NPCs. </param>
    /// <param name="dataId"> The data id for NPCs. </param>
    /// <returns> An identifier for that data. </returns>
    public ActorIdentifier CreateIndividual(IdentifierType type, ByteString name, ushort value, ObjectKind kind, NpcId dataId)
        => type switch
        {
            IdentifierType.Player    => CreatePlayer(name, value),
            IdentifierType.Retainer  => CreateRetainer(name, (ActorIdentifier.RetainerType)value),
            IdentifierType.Owned     => CreateOwned(name, value, kind, dataId),
            IdentifierType.Special   => CreateSpecial((ScreenActor)value),
            IdentifierType.Npc       => CreateNpc(kind, dataId, value),
            IdentifierType.UnkObject => CreateIndividualUnchecked(IdentifierType.UnkObject, name, value, ObjectKind.None, 0),
            _                        => ActorIdentifier.Invalid,
        };

    /// <inheritdoc cref="CreateIndividual"/>
    /// <summary> Create an individual from existing data, which is left unchecked. </summary>
    /// <remarks> Only use this if you are sure the data is valid. </remarks>
    public ActorIdentifier CreateIndividualUnchecked(IdentifierType type, ByteString name, ushort value, ObjectKind kind, NpcId dataId)
        => new(type, kind, (ObjectIndex)value, dataId, name);

    /// <summary> Create a player from name and home world. Input is checked for correctness. </summary>
    public ActorIdentifier CreatePlayer(ByteString name, WorldId homeWorld)
    {
        if (!VerifyWorld(homeWorld) || !VerifyPlayerName(name.Span))
            return ActorIdentifier.Invalid;

        return new ActorIdentifier(IdentifierType.Player, ObjectKind.Player, homeWorld, 0, name);
    }

    /// <summary> Create a retainer from name and retainer type. Input is checked for correctness. </summary>
    public ActorIdentifier CreateRetainer(ByteString name, ActorIdentifier.RetainerType type)
    {
        if (!VerifyRetainerName(name.Span))
            return ActorIdentifier.Invalid;

        return new ActorIdentifier(IdentifierType.Retainer, ObjectKind.Retainer, type, 0, name);
    }

    /// <summary> Create a special actor identifier from special input. Input is checked for correctness. </summary>
    public ActorIdentifier CreateSpecial(ScreenActor actor)
    {
        if (!VerifySpecial(actor))
            return ActorIdentifier.Invalid;

        return new ActorIdentifier(IdentifierType.Special, ObjectKind.Player, (ObjectIndex)(uint)actor, 0, ByteString.Empty);
    }

    /// <summary> Create an NPC from kind and id. Input is checked for correctness. </summary>
    public ActorIdentifier CreateNpc(ObjectKind kind, NpcId data)
        => CreateNpc(kind, data, ObjectIndex.AnyIndex);

    /// <summary> Create an NPC from kind, id and index. Input is checked for correctness. </summary>
    public ActorIdentifier CreateNpc(ObjectKind kind, NpcId data, ObjectIndex index)
    {
        if (!VerifyIndex(index) || !VerifyNpcData(kind, data))
            return ActorIdentifier.Invalid;

        return new ActorIdentifier(IdentifierType.Npc, kind, index, data, ByteString.Empty);
    }

    /// <summary> Create an owned NPC from kind and id and the owners name and home world. Input is checked for correctness. </summary>
    public ActorIdentifier CreateOwned(ByteString ownerName, WorldId homeWorld, ObjectKind kind, NpcId dataId)
    {
        if (!VerifyWorld(homeWorld) || !VerifyPlayerName(ownerName.Span) || !VerifyOwnedData(kind, dataId))
            return ActorIdentifier.Invalid;

        return new ActorIdentifier(IdentifierType.Owned, kind, homeWorld, dataId, ownerName);
    }

    #region Verification

    /// <summary> Checks SE naming rules. </summary>
    public static bool VerifyPlayerName(ReadOnlySpan<byte> name)
    {
        // Total no more than 20 characters + space.
        if (name.Length is < 5 or > 21)
            return false;

        // Forename and surname, no more spaces.
        var splitIndex = name.IndexOf((byte)' ');
        if (splitIndex < 0 || name[(splitIndex + 1)..].IndexOf((byte)' ') >= 0)
            return false;

        return CheckNamePart(name[..splitIndex], 2, 15) && CheckNamePart(name[(splitIndex + 1)..], 2, 15);
    }

    /// <summary> Checks SE naming rules. </summary>
    public static bool VerifyPlayerName(ReadOnlySpan<char> name)
    {
        // Total no more than 20 characters + space.
        if (name.Length is < 5 or > 21)
            return false;

        // Forename and surname, no more spaces.
        var splitIndex = name.IndexOf(' ');
        if (splitIndex < 0 || name[(splitIndex + 1)..].IndexOf(' ') >= 0)
            return false;

        return CheckNamePart(name[..splitIndex], 2, 15) && CheckNamePart(name[(splitIndex + 1)..], 2, 15);
    }

    /// <summary> Checks SE naming rules. </summary>
    public static bool VerifyRetainerName(ReadOnlySpan<byte> name)
        => CheckNamePart(name, 3, 20);

    /// <summary> Checks SE naming rules. </summary>
    public static bool VerifyRetainerName(ReadOnlySpan<char> name)
        => CheckNamePart(name, 3, 20);

    /// <summary> Checks a single part of a name. </summary>
    private static bool CheckNamePart(ReadOnlySpan<char> part, int minLength, int maxLength)
    {
        // Each name part at least 2 and at most 15 characters for players, and at least 3 and at most 20 characters for retainers.
        if (part.Length < minLength || part.Length > maxLength)
            return false;

        // Each part starting with capitalized letter.
        if (part[0] is < 'A' or > 'Z')
            return false;

        // Every other symbol needs to be lowercase letter, hyphen or apostrophe.
        var last = '\0';
        for (var i = 1; i < part.Length; ++i)
        {
            var current = part[i];
            if (current is not ('\'' or '-' or (>= 'a' and <= 'z')))
                return false;

            // Hyphens can not be used in succession, after or before apostrophes or as the last symbol.
            if (last is '\'' && current is '-')
                return false;
            if (last is '-' && current is '-' or '\'')
                return false;

            last = current;
        }

        return true;
    }

    /// <summary> Checks a single part of a name. </summary>
    private static bool CheckNamePart(ReadOnlySpan<byte> part, int minLength, int maxLength)
    {
        // Each name part at least 2 and at most 15 characters for players, and at least 3 and at most 20 characters for retainers.
        if (part.Length < minLength || part.Length > maxLength)
            return false;

        // Each part starting with capitalized letter.
        if (part[0] is < (byte)'A' or > (byte)'Z')
            return false;

        // Every other symbol needs to be lowercase letter, hyphen or apostrophe.
        var last = (byte)'\0';
        for (var i = 1; i < part.Length; ++i)
        {
            var current = part[i];
            if (current is not ((byte)'\'' or (byte)'-' or (>= (byte)'a' and <= (byte)'z')))
                return false;

            // Hyphens can not be used in succession, after or before apostrophes or as the last symbol.
            if (last is (byte)'\'' && current is (byte)'-')
                return false;
            if (last is (byte)'-' && current is (byte)'-' or (byte)'\'')
                return false;

            last = current;
        }

        return true;
    }

    /// <summary> Checks if the world is a valid public world or ushort.MaxValue (any world). </summary>
    public bool VerifyWorld(WorldId worldId)
        => worldId == WorldId.AnyWorld || _data.Worlds.ContainsKey(worldId.Id);

    /// <summary> Verify that the enum value is a specific actor and return the name if it is. </summary>
    public static bool VerifySpecial(ScreenActor actor)
        => actor is >= ScreenActor.CharacterScreen and <= ScreenActor.Card8;

    /// <summary> Verify that the object index is a valid index for an NPC. </summary>
    public bool VerifyIndex(ObjectIndex index)
    {
        if (index == ObjectIndex.AnyIndex)
            return true;
        if (index < ObjectIndex.GPosePlayer)
            return (index.Index & 1) == 0;
        if (index > ObjectIndex.Card8)
            return index.Index < _objects.TotalCount;

        return index < ObjectIndex.CharacterScreen;
    }

    /// <summary> Verify that the object kind is a valid owned object, and the corresponding data ID. </summary>
    public bool VerifyOwnedData(ObjectKind kind, NpcId dataId)
    {
        return kind switch
        {
            ObjectKind.MountType => _data.Mounts.ContainsKey(dataId.Id),
            ObjectKind.Companion => _data.Companions.ContainsKey(dataId.Id),
            ObjectKind.Ornament  => _data.Ornaments.ContainsKey(dataId.Id),
            ObjectKind.BattleNpc => _data.BNpcs.ContainsKey(dataId.Id),
            _                    => false,
        };
    }

    /// <summary> Verify that the data ID is valid for the object kind. </summary>
    public bool VerifyNpcData(ObjectKind kind, NpcId dataId)
        => kind switch
        {
            ObjectKind.MountType => _data.Mounts.ContainsKey(dataId.Id),
            ObjectKind.Companion => _data.Companions.ContainsKey(dataId.Id),
            ObjectKind.Ornament  => _data.Ornaments.ContainsKey(dataId.Id),
            ObjectKind.BattleNpc => _data.BNpcs.ContainsKey(dataId.Id),
            ObjectKind.EventNpc  => _data.ENpcs.ContainsKey(dataId.Id),
            _                    => false,
        };

    #endregion

    #region FromObjectHelpers

    /// <summary> Create a player from the game object.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ActorIdentifier CreatePlayerFromObject(Actor actor, bool check)
    {
        var name      = actor.Utf8Name;
        var homeWorld = actor.HomeWorld;
        return check
            ? CreatePlayer(name, homeWorld)
            : CreateIndividualUnchecked(IdentifierType.Player, name, homeWorld, ObjectKind.None, uint.MaxValue);
    }

    /// <summary> Create a battle npc from the game object.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private unsafe ActorIdentifier CreateBNpcFromObject(Actor actor, out Actor owner, bool check, bool allowPlayerNpc,
        bool withoutIndex)
    {
        var ownerId = actor.AsObject->OwnerId;
        // 952 -> 780 is a special case for chocobos because they have NameId == 0 otherwise.
        var nameId = actor.AsObject->BaseId == 952 ? 780 : actor.AsCharacter->NameId;
        if (ownerId != 0xE0000000)
        {
            owner = HandleCutscene(_objects.ById(ownerId));
            if (!owner.Valid)
                return ActorIdentifier.Invalid;

            var name      = owner.Utf8Name;
            var homeWorld = owner.HomeWorld;
            return check
                ? CreateOwned(name, homeWorld, ObjectKind.BattleNpc, nameId)
                : CreateIndividualUnchecked(IdentifierType.Owned, name, homeWorld, ObjectKind.BattleNpc, nameId);
        }

        owner = Actor.Null;
        // Hack to support Anamnesis changing ObjectKind for NPC faces.
        if (nameId == 0 && allowPlayerNpc)
        {
            var name = actor.Utf8Name;
            if (!name.IsEmpty)
            {
                var homeWorld = actor.HomeWorld;
                return check
                    ? CreatePlayer(name, homeWorld)
                    : CreateIndividualUnchecked(IdentifierType.Player, name, homeWorld, ObjectKind.None, uint.MaxValue);
            }
        }

        var index = withoutIndex ? ObjectIndex.AnyIndex : actor.Index;
        return check
            ? CreateNpc(ObjectKind.BattleNpc, nameId, index)
            : CreateIndividualUnchecked(IdentifierType.Npc, ByteString.Empty, index.Index, ObjectKind.BattleNpc, nameId);
    }

    /// <summary> Create an event npc from the game object.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private unsafe ActorIdentifier CreateENpcFromObject(Actor actor, bool check, bool withoutIndex)
    {
        var dataId = actor.AsObject->BaseId;
        // Special case for squadron that is also in the game functions, cf. E8 ?? ?? ?? ?? 89 87 ?? ?? ?? ?? 4C 89 BF
        if (dataId == 0xF845D)
            dataId = actor.AsObject->GetNameId();
        if (MannequinIds.Contains(dataId))
        {
            var retainerName = new ByteString(actor.AsObject->Name);
            var actualName   = _framework.IsInFrameworkUpdateThread ? new ByteString(actor.AsObject->GetName()) : ByteString.Empty;
            if (!actualName.Equals(retainerName))
            {
                var ident = check
                    ? CreateRetainer(retainerName, ActorIdentifier.RetainerType.Mannequin)
                    : CreateIndividualUnchecked(IdentifierType.Retainer, retainerName, (ushort)ActorIdentifier.RetainerType.Mannequin,
                        ObjectKind.EventNpc,                             dataId);
                if (ident.IsValid)
                    return ident;
            }
        }

        var index = withoutIndex ? ObjectIndex.AnyIndex : actor.Index;
        return check
            ? CreateNpc(ObjectKind.EventNpc, dataId, index)
            : CreateIndividualUnchecked(IdentifierType.Npc, ByteString.Empty, index.Index, ObjectKind.EventNpc, dataId);
    }

    /// <summary> Create a companion npc from the game object.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ActorIdentifier CreateCompanionFromObject(Actor actor, out Actor owner, ObjectKind kind, bool check)
    {
        owner = HandleCutscene(_objects.CompanionParent(actor));
        if (!owner.Valid)
            return ActorIdentifier.Invalid;

        var dataId    = GetCompanionId(actor, owner);
        var name      = owner.Utf8Name;
        var homeWorld = owner.HomeWorld;
        if (check)
            return CreateOwned(name, homeWorld, kind, dataId);

        if (name.Length > 0)
            return CreateIndividualUnchecked(IdentifierType.Owned, name, homeWorld, kind, dataId);

        return CreateNpc(kind, dataId, actor.Index);
    }

    /// <summary> Create a retainer from the game object.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ActorIdentifier CreateRetainerFromObject(Actor actor, bool check)
    {
        var name = actor.Utf8Name;
        return check
            ? CreateRetainer(name, ActorIdentifier.RetainerType.Bell)
            : CreateIndividualUnchecked(IdentifierType.Retainer, name, (ushort)ActorIdentifier.RetainerType.Bell, ObjectKind.None,
                uint.MaxValue);
    }

    /// <summary> Create an unknown object. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ActorIdentifier CreateUnkFromObject(Actor actor, bool withoutIndex)
    {
        var name  = actor.Utf8Name;
        var index = withoutIndex ? ObjectIndex.AnyIndex : actor.Index;
        return CreateIndividualUnchecked(IdentifierType.UnkObject, name, index.Index, ObjectKind.None, 0);
    }

    #endregion

    /// <summary> Obtain the current companion ID for an object by its actor and owner. </summary>
    private static unsafe NpcId GetCompanionId(Actor actor,
        Actor owner)
    {
        return (ObjectKind)actor.AsObject->ObjectKind switch
        {
            ObjectKind.MountType => owner.AsCharacter->Mount.MountId,
            ObjectKind.Ornament  => owner.AsCharacter->OrnamentData.OrnamentId,
            ObjectKind.Companion => actor.AsObject->BaseId,
            _                    => actor.AsObject->BaseId,
        };
    }

    /// <summary> Handle owned cutscene actors. </summary>
    private Actor HandleCutscene(Actor main)
    {
        if (!main.Valid)
            return Actor.Null;

        if (main.Index.Index is < (ushort)ScreenActor.CutsceneStart or >= (ushort)ScreenActor.CutsceneEnd)
            return main;

        var parentIdx = _toParentIdx.Invoke(main.Index.Index);
        var parent    = _objects[parentIdx];
        return parent.Valid ? parent : main;
    }

    /// <summary> The existing IDs that correspond to mannequins. </summary>
    private static readonly FrozenSet<ENpcId> MannequinIds = FrozenSet.ToFrozenSet<ENpcId>(
    [
        1026228u,
        1026229u,
        1026986u,
        1026987u,
        1026988u,
        1026989u,
        1032291u,
        1032292u,
        1032293u,
        1032294u,
        1033046u,
        1033047u,
        1033658u,
        1033659u,
        1007137u,
    ]);

    /// <summary> Parse a user string for player identifier data. </summary>
    private (ByteString, WorldId) ParsePlayer(string player)
    {
        var parts = player.Split('@', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!VerifyPlayerName(parts[0]))
            throw new IdentifierParseError($"{parts[0]} is not a valid player name.");
        if (!ByteString.FromString(parts[0], out var p))
            throw new IdentifierParseError($"The player string {parts[0]} contains invalid symbols.");

        var world = parts.Length == 2
            ? _data.ToWorldId(parts[1])
            : ushort.MaxValue;

        if (!VerifyWorld(world))
            throw new IdentifierParseError($"{parts[1]} is not a valid world name.");

        return (p, world);
    }

    /// <summary> Parse a user string for npc identifier data. </summary>
    private (ObjectKind, NpcId[], WorldId) ParseNpc(string npc, bool allowIndex)
    {
        var indexString = allowIndex ? "@<Index>" : string.Empty;
        var split2      = npc.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (split2.Length != 2)
            throw new IdentifierParseError($"NPCs need to be specified by '[Object Type]:[NPC Name]{indexString}'.");

        var split3 = allowIndex
            ? split2[1].Split('@', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [split2[1]];

        return split2[0].ToLowerInvariant() switch
        {
            "m" or "mount" => FindDataId(split3[0], _data.Mounts, out var id)
                ? (ObjectKind.MountType, mountId: id, GetIndex())
                : throw new IdentifierParseError($"Could not identify a Mount named {split2[1]}."),
            "c" or "companion" or "minion" or "mini" => FindDataId(split3[0], _data.Companions, out var id)
                ? (ObjectKind.Companion, cId: id, GetIndex())
                : throw new IdentifierParseError($"Could not identify a Minion named {split2[1]}."),
            "a" or "o" or "accessory" or "ornament" => FindDataId(split3[0], _data.Ornaments, out var id)
                ? (ObjectKind.Ornament, id, GetIndex())
                : throw new IdentifierParseError($"Could not identify an Accessory named {split2[1]}."),
            "e" or "enpc" or "eventnpc" or "event npc" => FindDataId(split3[0], _data.ENpcs, out var id)
                ? (ObjectKind.EventNpc, id, GetIndex())
                : throw new IdentifierParseError($"Could not identify an Event NPC named {split2[1]}."),
            "b" or "bnpc" or "battlenpc" or "battle npc" => FindDataId(split3[0], _data.BNpcs, out var id)
                ? (ObjectKind.BattleNpc, id, GetIndex())
                : throw new IdentifierParseError($"Could not identify a Battle NPC named {split2[1]}."),
            _ => throw new IdentifierParseError($"The argument {split2[0]} is not a valid NPC Type."),
        };

        WorldId GetIndex()
        {
            var idx = WorldId.AnyWorld;
            if (split3.Length != 2)
                return idx;

            if (ushort.TryParse(split3[1], out var intIdx) && intIdx < _objects.TotalCount)
                idx = intIdx;
            else
                throw new IdentifierParseError($"Could not parse index {split3[1]} to valid Index.");

            return idx;
        }

        static bool FindDataId(string name, NameDictionary data, out NpcId[] dataIds)
        {
            dataIds = data.Where(kvp => kvp.Value.Equals(name, StringComparison.OrdinalIgnoreCase)).Select(kvp => kvp.Key).ToArray();
            return dataIds.Length > 0;
        }
    }
}
