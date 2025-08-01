namespace GagSpeak.Gui.Remote;

// a shared index reference class to hold pattern data that can be ref'd in all associated motors.
public class RemotePlaybackRef
{
    public Guid PatternId { get; set; } = Guid.Empty;
    public int Idx { get; set; } = -1;
    public int Length { get; set; } = -1;
    public bool Looping { get; set; } = false;

    public void Reset()
    {
        PatternId = Guid.Empty;
        Idx = -1;
        Length = -1;
        Looping = false;
    }

    public void SetPattern(Guid patternId, int length, bool looping = false)
    {
        // do not set if the idx is not -1.
        if (Idx != -1)
            return;

        PatternId = patternId;
        Idx = 0;
        Length = length;
        Looping = looping;
    }
}
