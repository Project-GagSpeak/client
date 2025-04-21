using Dalamud.Interface.Textures.TextureWraps;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace GagSpeak.UI.Components;

public readonly record struct ImageSizeData(int Width, int Height, int FileLength)
{
    public Vector2 Size => new(Width, Height);
    public string SizeString => $"{FileLength / 1024.0:F2} KB ({Width}px by {Height}px)";
    public static ImageSizeData Empty => new(0, 0, 0);
}

public class ImportedImage : IDisposable
{
    // Core properties for handling image data and operations
    public ImageDataType ImageType { get; init; }
    public Vector2 SizeConstraint { get; init; }
    public string FileName { get; set; } = string.Empty;
    public float ZoomFactor { get; set; } = 1f;


    // Image data properties, using ImageSharp to handle image manipulation
    public Image<Rgba32> OriginalImage { get; set; }
    public byte[] OriginalData { get; set; } = Array.Empty<byte>();
    public byte[] CroppedData { get; set; } = Array.Empty<byte>();
    public IDalamudTextureWrap? OriginalDisplay { get; set; }
    public IDalamudTextureWrap? CroppedDisplay { get; set; }
    public ImageSizeData SizeDataOriginal { get; set; } = ImageSizeData.Empty;
    public ImageSizeData SizeDataCropped { get; set; } = ImageSizeData.Empty;

    // Zoom and Pan values based on original and constrained sizes
    public float MinZoom => Math.Min(SizeConstraint.X / SizeDataOriginal.Width, SizeConstraint.Y / SizeDataOriginal.Height);
    public float MaxZoom => SizeDataOriginal.Width <= SizeDataOriginal.Height
        ? SizeDataOriginal.Width / SizeConstraint.X
        : SizeDataOriginal.Height / SizeConstraint.Y;

    // Min/Max Pan values to ensure valid bounds for panning
    public float MinPanX => -MaxPanX;
    public float MaxPanX => Math.Max(0, (SizeDataOriginal.Width * ZoomFactor - SizeConstraint.X) / 2f);
    public float MinPanY => -MaxPanY;
    public float MaxPanY => Math.Max(0, (SizeDataOriginal.Height * ZoomFactor - SizeConstraint.Y) / 2f);

    // Property to check if image data is valid
    public bool HasValidData => OriginalImage != null;

    // Constructor for initializing ImportedImage with a size constraint and image type
    public ImportedImage(Vector2 sizeConstraint, ImageDataType imageType)
    {
        ImageType = imageType;
        SizeConstraint = sizeConstraint;
    }

    public void Dispose()
    {
        OriginalDisplay?.Dispose();
        CroppedDisplay?.Dispose();
        OriginalDisplay = null;
        CroppedDisplay = null;
    }
}
