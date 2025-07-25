namespace GagSpeak.PlayerControl;

/// <summary>
///     The essential configuration to be provided to the hardcore task manager.
/// </summary>
public record HcTaskConfiguration(int MaxTaskTime = 30000, bool AbortOnTimeout = true);
