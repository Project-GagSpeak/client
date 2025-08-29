using GagSpeak.State;

namespace GagSpeak.PlayerControl;

/// <summary>
///     The essential configuration to be provided to the hardcore task manager. <para />
///     You can use <paramref name="Flags"/> to restrict what the player can do during task execution.
/// </summary>
public record HcTaskConfiguration(HcTaskControl Flags = HcTaskControl.None, int TimeoutAt = 30000, bool InnerTimouts = false)
{
    public Action? OnEnd { get; set; } = null;

    public static readonly HcTaskConfiguration Rapid = new(HcTaskControl.None, 1000, false);
    public static readonly HcTaskConfiguration Quick = new(HcTaskControl.None, 3000, false);
    public static readonly HcTaskConfiguration Short = new(HcTaskControl.None, 10000, false);
    public static readonly HcTaskConfiguration Default = new();
    public static readonly HcTaskConfiguration Branch = new(HcTaskControl.None, -1, true);
    public static readonly HcTaskConfiguration Collection = new(HcTaskControl.None, -1, true);
}
