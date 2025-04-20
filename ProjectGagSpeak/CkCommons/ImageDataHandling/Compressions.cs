using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GagSpeak.CkCommons.ImageHandling;
public static class ImageDataHandling
{
    /// <summary> Returns an image currently on your clipboard to raw image byte[] </summary>
    /// <returns> The image data in a byte array. </returns>
    /// <remarks> This method is not operatable on linux to my knowledge. </remarks>
    public static byte[] GetClipboardImageBytes()
    {
        try
        {
            // Attempt to retrieve an image from the clipboard.
            if(Clipboard.GetImage() is not { } bitmapFormattedImage)
                throw new ExternalException("No image found in clipboard.");

            using var memoryStream = new MemoryStream();
            bitmapFormattedImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

            // Reset the stream's position to the beginning before detecting the format
            memoryStream.Seek(0, SeekOrigin.Begin);

            // Ensure that it properly went to PNG.
            var format = Image.DetectFormat(memoryStream);
            if (!format.FileExtensions.Contains("png", StringComparer.OrdinalIgnoreCase))
                throw new Exception("Error: Image is not in PNG format.");

            // Return the raw image data.
            return (memoryStream.ToArray());
        }
        catch (ExternalException ex)
        {
            GagSpeak.StaticLog.Error("Error: " + ex);
            return Array.Empty<byte>();
        }
    }

    public static Image<Rgba32>? GetClipboardImage()
    {
        try
        {
            // Attempt to retrieve an image from the clipboard.
            if (Clipboard.GetImage() is not { } bitmapFormattedImage)
                throw new ExternalException("No image found in clipboard.");

            // Convert the bitmap to a byte array
            using var memoryStream = new MemoryStream();
            bitmapFormattedImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

            memoryStream.Seek(0, SeekOrigin.Begin);

            var imageData = memoryStream.ToArray();
            // Ensure that it properly went to PNG.
            var format = Image.DetectFormat(memoryStream);
            if (!format.FileExtensions.Contains("png", StringComparer.OrdinalIgnoreCase))
                throw new Exception("Error: Image is not in PNG format.");

            // return the image as a Image<Rgba32> object
            return Image.Load<Rgba32>(memoryStream);
        }
        catch (ExternalException ex)
        {
            GagSpeak.StaticLog.Error("Error: " + ex);
            return null;
        }
    }

}
