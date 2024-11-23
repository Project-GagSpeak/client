using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Api.Api;
using Glamourer.Api.Enums;
using Glamourer.Api.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Glamourer.Api.IpcSubscribers.Legacy;

public sealed class SetItem(IDalamudPluginInterface pi)
    : FuncSubscriber<ICharacter?, byte, ulong, byte, uint, int>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(SetItem)}";

    public new GlamourerApiEc Invoke(ICharacter? character, byte slot, ulong itemId, byte stainId, uint key)
        => (GlamourerApiEc)base.Invoke(character, slot, itemId, stainId, key);
}

public sealed class SetItemOnce(IDalamudPluginInterface pi)
    : FuncSubscriber<ICharacter?, byte, ulong, byte, uint, int>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(SetItemOnce)}";

    public new GlamourerApiEc Invoke(ICharacter? character, byte slot, ulong itemId, byte stainId, uint key)
        => (GlamourerApiEc)base.Invoke(character, slot, itemId, stainId, key);
}

public sealed class SetItemByActorName(IDalamudPluginInterface pi)
    : FuncSubscriber<string, byte, ulong, byte, uint, int>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(SetItemByActorName)}";

    public new GlamourerApiEc Invoke(string actorName, byte slot, ulong itemId, byte stainId, uint key)
        => (GlamourerApiEc)base.Invoke(actorName, slot, itemId, stainId, key);
}

public sealed class SetItemOnceByActorName(IDalamudPluginInterface pi)
    : FuncSubscriber<string, byte, ulong, byte, uint, int>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(SetItemOnceByActorName)}";

    public new GlamourerApiEc Invoke(string actorName, byte slot, ulong itemId, byte stainId, uint key)
        => (GlamourerApiEc)base.Invoke(actorName, slot, itemId, stainId, key);
}

public sealed class SetItemV2(IDalamudPluginInterface pi)
    : FuncSubscriber<int, byte, ulong, byte, uint, ulong, int>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(SetItem)}.V2";

    public GlamourerApiEc Invoke(int objectIndex, ApiEquipSlot slot, ulong itemId, byte stain, uint key = 0, ApplyFlag flags = ApplyFlag.Once)
        => (GlamourerApiEc)Invoke(objectIndex, (byte)slot, itemId, stain, key, (ulong)flags);
}

public sealed class SetItemName(IDalamudPluginInterface pi)
    : FuncSubscriber<string, byte, ulong, byte, uint, ulong, int>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(SetItemName)}";

    public GlamourerApiEc Invoke(string objectName, ApiEquipSlot slot, ulong itemId, byte stain, uint key = 0, ApplyFlag flags = ApplyFlag.Once)
        => (GlamourerApiEc)Invoke(objectName, (byte)slot, itemId, stain, key, (ulong)flags);

    public static FuncProvider<string, byte, ulong, byte, uint, ulong, int> Provider(IDalamudPluginInterface pi, IGlamourerApiItems api)
        => new(pi, Label, (a, b, c, d, e, f) => (int)api.SetItemName(a, (ApiEquipSlot)b, c, [d], e, (ApplyFlag)f));
}
