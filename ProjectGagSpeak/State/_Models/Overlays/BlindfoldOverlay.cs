using GagSpeak.Services.Configs;
using System.IO;

namespace GagSpeak.State.Models;
public class BlindfoldOverlay : IOverlayEffect
{
    public bool ForceFirstPerson { get; set; } = false;
    public string OverlayPath { get; set; } = string.Empty;

    public BlindfoldOverlay()
    { }

    public BlindfoldOverlay(string path)
    {
        OverlayPath = path;
    }

    public BlindfoldOverlay(BlindfoldOverlay other)
    {
        ForceFirstPerson = other.ForceFirstPerson;
        OverlayPath = other.OverlayPath;
    }

    public bool IsValid() => File.Exists(
        Path.Combine(ConfigFileProvider.ThumbnailDirectory, ImageDataType.Blindfolds.ToString(), OverlayPath));
    
    public BlindfoldOverlay Clone() => new BlindfoldOverlay(this);
}
