using GagSpeak.State;

namespace GagSpeak.PlayerControl;

/// <summary>
///     The essential configuration to be provided to the hardcore task manager. <para />
///     You can use <paramref name="ControlFlags"/> to restrict what the player can do during task execution.
/// </summary>
public record HcTaskConfiguration(HcTaskControl ControlFlags = HcTaskControl.None, int MaxTaskTime = 30000, bool AbortAllOnTimeout = true)
{
    public static readonly HcTaskConfiguration Default = new();
}
