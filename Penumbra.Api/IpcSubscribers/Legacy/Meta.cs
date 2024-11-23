using Dalamud.Plugin;
using Penumbra.Api.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Penumbra.Api.IpcSubscribers.Legacy;

public sealed class GetMetaManipulations(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetMetaManipulations)}";

    public new string Invoke(string objectName)
        => base.Invoke(objectName);
}

public sealed class GetGameObjectMetaManipulations(IDalamudPluginInterface pi)
    : FuncSubscriber<int, string>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetGameObjectMetaManipulations)}";

    public new string Invoke(int objectIndex)
        => base.Invoke(objectIndex);
}
