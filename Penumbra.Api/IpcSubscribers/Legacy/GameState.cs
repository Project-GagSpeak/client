using Dalamud.Plugin;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Penumbra.Api.IpcSubscribers.Legacy;

public sealed class GetDrawObjectInfo(IDalamudPluginInterface pi)
    : FuncSubscriber<nint, (nint, string)>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetDrawObjectInfo)}";

    public new (nint GameObjectAddress, string CollectionName) Invoke(nint drawObjectAddress)
        => base.Invoke(drawObjectAddress);
}

public sealed class SetCutsceneParentIndex(IDalamudPluginInterface pi)
    : FuncSubscriber<int, int, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(SetCutsceneParentIndex)}";

    public new PenumbraApiEc Invoke(int cutsceneObjectIndex, int newParentIndex)
        => base.Invoke(cutsceneObjectIndex, newParentIndex);
}

public static class CreatingCharacterBase
{
    public const string Label = $"Penumbra.{nameof(CreatingCharacterBase)}";

    public static EventSubscriber<nint, string, nint, nint, nint> Subscriber(IDalamudPluginInterface pi,
        params Action<nint, string, nint, nint, nint>[] actions)
        => new(pi, Label, actions);
}

public static class CreatedCharacterBase
{
    public const string Label = $"Penumbra.{nameof(CreatedCharacterBase)}";

    public static EventSubscriber<nint, string, nint> Subscriber(IDalamudPluginInterface pi,
        params Action<nint, string, nint>[] actions)
        => new(pi, Label, actions);
}
