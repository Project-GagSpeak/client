using Dalamud.Plugin;
using Glamourer.Api.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Glamourer.Api.IpcSubscribers.Legacy;

public sealed class ApiVersions(IDalamudPluginInterface pi)
    : FuncSubscriber<(int, int)>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApiVersions)}";

    public new (int Major, int Minor) Invoke()
        => base.Invoke();
}
