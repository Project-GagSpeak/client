namespace GagSpeak.State;

/// <summary>
///     The reasons for why an IPC Call should be blocked.
/// </summary>
[Flags]
public enum IpcBlockReason : byte
{
    None = 0x00,
    SemaphoreTask = 0x01, // Semaphore is running, block IPC calls.
    Gearset = 0x02, // Gearset is being applied, block IPC calls.
    SameGearset = 0x04, // Gearset was applied, but was the same as current one.
}
