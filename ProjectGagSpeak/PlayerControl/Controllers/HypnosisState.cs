namespace GagSpeak.Services.Controller;

/// <summary>
///     Variables that dictate the current state of the Hypnosis effect.
/// </summary>
public class HypnosisState
{
    public uint   ImageColor = 0xFFFFFFFF;
    public float  ImageOpacity = 0f;
    public float  SpinSpeed = 1f;
    public float  Rotation = 0f;

    public string CurrentText = string.Empty;
    public int    LastTextIndex = 0;
    public float  TextScale = 1f;
    public float  TextOpacity = 1f;

    // Only set for linear text scaling.
    public Vector2 TextOffsetStart = Vector2.Zero;
    public Vector2 TextOffsetEnd = Vector2.Zero;
    public float TextScaleProgress = 0f;
}
