using Dalamud.Plugin;
using Penumbra.Api.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Penumbra.Api.IpcSubscribers.Legacy;

public class ApiVersions(IDalamudPluginInterface pi)
    : FuncSubscriber<(int Breaking, int Features)>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(ApiVersions)}";

    public new (int Breaking, int Features) Invoke()
        => base.Invoke();
}
