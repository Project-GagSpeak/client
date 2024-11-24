using NAudio.Wave;

namespace GagSpeak.Toybox.SimulatedVibe;
// Loops the audio played back so that it can be played back indefinitely until stopped.
public class VibeSimLooper : WaveStream, IDisposable
{
    WaveStream sourceStream;
    public bool EnableLooping { get; set; }
    public override WaveFormat WaveFormat => sourceStream.WaveFormat;
    public override long Length => sourceStream.Length;
    public override long Position
    {
        get => sourceStream.Position;
        set => sourceStream.Position = value;
    }

    public VibeSimLooper(WaveStream sourceStream)
    {
        this.sourceStream = sourceStream;
        this.EnableLooping = true;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;

        while (totalBytesRead < count)
        {
            int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
            if (bytesRead == 0)
            {
                if (sourceStream.Position == 0 || !EnableLooping)
                {
                    // something wrong with the source stream
                    break;
                }
                // loop
                sourceStream.Position = 0;
            }
            totalBytesRead += bytesRead;
        }
        return totalBytesRead;
    }

    protected override void Dispose(bool disposing)
    {
        sourceStream.Dispose();
        base.Dispose(disposing);
    }
}
