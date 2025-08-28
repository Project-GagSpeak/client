using GagSpeak.State;

namespace GagSpeak.PlayerControl;

/// <summary>
///     The essential configuration to be provided to the hardcore task manager. <para />
///     You can use <paramref name="Flags"/> to restrict what the player can do during task execution.
/// </summary>
public record HcTaskConfiguration(HcTaskControl Flags = HcTaskControl.None, int TimeoutAt = 30000, bool InnerTimouts = false, bool AbortAllOnTimeout = true)
{
    public static readonly HcTaskConfiguration Short = new(HcTaskControl.None, 7500, false, true);
    public static readonly HcTaskConfiguration Default = new();
    public static readonly HcTaskConfiguration Branch = new(HcTaskControl.None, -1, true, false);
    public static readonly HcTaskConfiguration Collection = new(HcTaskControl.None, -1, true, false);

    // maybe add something that lets us invoke on a timeout.

    // maybe also add an option on how much we abort on a failed call.

    // might need to turn into a class structure, not sure, could also fire mediator event.
}
