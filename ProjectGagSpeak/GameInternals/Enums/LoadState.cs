namespace GagSpeak.GameInternals;
public enum LoadState : byte
{
    Constructing = 0x00,
    Constructed = 0x01,
    Async2 = 0x02,
    AsyncRequested = 0x03,
    Async4 = 0x04,
    AsyncLoading = 0x05,
    Async6 = 0x06,
    Success = 0x07,
    Unknown8 = 0x08,
    Failure = 0x09,
    FailedSubResource = 0x0A,
    FailureB = 0x0B,
    FailureC = 0x0C,
    FailureD = 0x0D,
    None = 0xFF,
}
