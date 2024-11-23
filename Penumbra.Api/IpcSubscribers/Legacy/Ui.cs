using Dalamud.Plugin;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Penumbra.Api.IpcSubscribers.Legacy;

public sealed class OpenMainWindow(IDalamudPluginInterface pi)
    : FuncSubscriber<TabType, string, string, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(OpenMainWindow)}";

    public new PenumbraApiEc Invoke(TabType tab, string modName, string modDirectory = "")
        => base.Invoke(tab, modName, modDirectory);
}
