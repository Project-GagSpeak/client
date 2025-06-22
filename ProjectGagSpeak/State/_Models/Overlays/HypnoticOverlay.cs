using GagSpeak.Services.Configs;
using GagspeakAPI.Data;

namespace GagSpeak.State.Models;
public class HypnoticOverlay : IOverlayEffect
{
    public bool ForceFirstPerson { get; set; } = false;
    public string OverlayPath { get; set; } = string.Empty;
    public HypnoticEffect Effect { get; set; } = new();

    public HypnoticOverlay()
    { }

    public HypnoticOverlay(string path)
    {
        OverlayPath = path;
    }

    public HypnoticOverlay(HypnoticOverlay other)
    {
        ForceFirstPerson = other.ForceFirstPerson;
        OverlayPath = other.OverlayPath;
    }

    public bool IsValid() => File.Exists(
        Path.Combine(ConfigFileProvider.ThumbnailDirectory, ImageDataType.Hypnosis.ToString(), OverlayPath));

    public HypnoticOverlay Clone() => new HypnoticOverlay(this);
}
