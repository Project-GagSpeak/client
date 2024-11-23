using Dalamud.Plugin;
using Penumbra.Api.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Penumbra.Api.IpcSubscribers.Legacy;

public sealed class ResolveCharacterPath(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, string>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(ResolveCharacterPath)}";

    public new string Invoke(string gamePath, string characterName)
        => base.Invoke(gamePath, characterName);
}

public sealed class ReverseResolvePath(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, string>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(ReverseResolvePath)}";

    public new string Invoke(string gamePath, string characterName)
        => base.Invoke(gamePath, characterName);
}
