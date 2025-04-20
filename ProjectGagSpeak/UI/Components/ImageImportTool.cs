using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.ImageHandling;
using GagSpeak.Services.Textures;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
    public byte[] OriginalData { get; set; } = Array.Empty<byte>();
    public byte[] ConstraintScaledData { get; set; } = Array.Empty<byte>();
    public byte[] CroppedData { get; set; } = Array.Empty<byte>();

    public IDalamudTextureWrap? ConstraintScaledDisplay { get; set; } = null;
    public IDalamudTextureWrap? CroppedDisplay { get; set; } = null;

    public ImageSizeData SizeDataOriginal = ImageSizeData.Empty;
    public ImageSizeData SizeDataConstraintScaled = ImageSizeData.Empty;
    public ImageSizeData SizeDataCropped = ImageSizeData.Empty;
    public bool HasValidData => OriginalData.Length > 0 && CroppedData.Length > 0;
    public bool UseScaledData = false;

    // Used by the compress function before finalization.
    public Vector2 SizeConstraint { get; init; }

    public ImageSizeData SizeDataToUse => UseScaledData ? SizeDataConstraintScaled : SizeDataOriginal;

    public ImportedImage(Vector2 sizeConstraint)
    {
        SizeConstraint = sizeConstraint;
    }

    public void Dispose()
    {
        ConstraintScaledDisplay?.Dispose();
        CroppedDisplay?.Dispose();
        ConstraintScaledDisplay = null;
        CroppedDisplay = null;
    }
}

// UI - Based scoped class that handles how imported images are imported.
public class ImageImportTool
{
    private readonly ILogger<ImageImportTool> _logger;
    private readonly FileDialogManager _fileDialogImport;
    private readonly CosmeticService _imageService; // might be able to remove idk.

    private Task? FileDialogTask = null;

    public ImageImportTool(ILogger<ImageImportTool> logger, FileDialogManager fileImport, CosmeticService imgService)
    {
        _logger = logger;
        _fileDialogImport = fileImport;
        _imageService = imgService;
    }

    /// <summary> Dictates which file import interface has precedence. </summary>
    public ImageDataType CurrentType { get; private set; } = ImageDataType.None;
    public ImportedImage? ImportedImage { get; private set; } = null;

    private float MinZoom = 1.0f;
    private float MaxZoom = 3.0f;
    private float CropX = 0.5f;
    private float CropY = 0.5f;
    private float Rotation = 0.0f;
    private float ZoomFactor = 1.0f;

    public void DrawImportContent()
    {
        if (ImportedImage is null || !ImportedImage.HasValidData)
            return;

        if (ImportedImage.SizeDataOriginal.Width > ImportedImage.SizeDataOriginal.Height)
            DrawHorizontalLayout(ImportedImage);
        else
            DrawVerticalLayout(ImportedImage);
    }

    private void DrawHorizontalLayout(ImportedImage img)
    {
        var region = ImGui.GetContentRegionAvail();
        var subChildHeight = (region.Y - ImGui.GetStyle().ItemSpacing.Y) / 2;
        var subChildSize = new Vector2(region.X, subChildHeight);
        using (ImRaii.Child("ImageTool_Top_Horizontal", subChildSize))
        {
            if (img.ConstraintScaledDisplay is { } fullDisplayImage)
            {
                var imgRatio = img.SizeDataConstraintScaled.Size.Y / img.SizeDataConstraintScaled.Size.X;
                // calculate the size for this display.
                var topImgSize = new Vector2((subChildHeight / imgRatio), subChildHeight);
                // determine the comparison between the left region and the constrained to know how far down to draw the image so it is centered.
                var shiftWidth = Math.Max(0, (subChildSize.X - topImgSize.X) / 2);
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + shiftWidth);
                // get the pos and draw the image.
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddDalamudImageRounded(img.ConstraintScaledDisplay, pos, topImgSize, 12f);
            }
            ImGui.Dummy(subChildSize);
        }
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.VibrantPink.Uint(), FancyTabBar.Rounding, ImDrawFlags.RoundCornersAll, 1.5f);

        using (ImRaii.Child("ImageTool_Bottom_Horizontal", subChildSize, false, WFlags.AlwaysUseWindowPadding))
        {
            var lowerRegion = ImGui.GetContentRegionAvail();

            // we need to calculate a scaled up cropped image display that takes lowerRegion.Y, and the img.SizeConstraint.Y,
            var scaledRatio = lowerRegion.Y / img.SizeConstraint.Y;
            var adjustedDisplaySize = img.SizeConstraint * scaledRatio;
            if (img.CroppedDisplay is { } croppedImage)
            {
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddDalamudImageRounded(croppedImage, pos, adjustedDisplaySize, 12f);
                ImGui.GetWindowDrawList().AddRect(pos, pos + adjustedDisplaySize, 0xFFFFFFFF, 12f);
            }
            ImGui.Dummy(adjustedDisplaySize);

            ImGui.SameLine();
            DrawImageSliders(img);

            ImGui.SameLine();
            if (FinalizedDisplay is { } wrap)
            {
                using (ImRaii.Group())
                {
                    ImGui.Image(wrap.ImGuiHandle, wrap.Size);
                    ImGui.Text($"Finalized Size: {FinalizedFileSize}");
                }
            }
        }
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.VibrantPink.Uint(), FancyTabBar.Rounding, ImDrawFlags.RoundCornersAll, 1.5f);
    }

    private void DrawVerticalLayout(ImportedImage img)
    {
        using (ImRaii.Group())
        {
            var leftRegion = new Vector2(ImGui.GetContentRegionAvail().X / 2, ImGui.GetContentRegionAvail().Y);
            var spacing = ImGui.GetStyle().ItemSpacing.X;

            if (img.ConstraintScaledDisplay is { } fullDisplayImage)
            {
                var leftImgSize = img.SizeDataConstraintScaled.Size;
                // determine the comparison between the left region and the constrained to know how far down to draw the image so it is centered.
                var shiftHeight = Math.Max(0, (leftRegion.Y - leftImgSize.Y) / 2);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + shiftHeight);
                // get the pos and draw the image.
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddDalamudImageRounded(img.ConstraintScaledDisplay, pos, leftImgSize, 12f);
                ImGui.GetWindowDrawList().AddRect(pos, pos + leftImgSize, 0xFFFFFFFF, 12f);
            }

            ImGui.Dummy(leftRegion);
        }

        ImGui.SameLine();

        using (ImRaii.Group())
        {
            // center the image to the width of our sizeConstraint.
            var rightRegion = ImGui.GetContentRegionAvail();
            var rightImgSize = img.SizeDataCropped.Size;

            var shiftWidth = Math.Max(0, (rightRegion.X - rightImgSize.X) / 2);

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + shiftWidth);

            if (img.CroppedDisplay is { } croppedImage)
            {
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddDalamudImageRounded(croppedImage, pos, rightImgSize, 12f);
                ImGui.GetWindowDrawList().AddRect(pos, pos + rightImgSize, 0xFFFFFFFF, 12f);
            }
            ImGui.Dummy(new Vector2(rightRegion.X, rightImgSize.Y));

            ImGui.Separator();
            DrawImageSliders(img);
        }
    }

    private void DrawImageSliders(ImportedImage img)
    {
        using var _ = ImRaii.Group();

        ImGui.SetNextItemWidth(150f);
        if (ImGui.SliderFloat("Width", ref CropX, 0.0f, 1.0f, "%.2f"))
            UpdateCroppedImagePreview();

        ImGui.SetNextItemWidth(150f);
        if (ImGui.SliderFloat("Height", ref CropY, 0.0f, 1.0f, "%.2f"))
            UpdateCroppedImagePreview();

        // Add rotation slider
        ImGui.SetNextItemWidth(150f);
        if (ImGui.SliderFloat("Rotation", ref Rotation, 0.0f, 360.0f, "%.2f"))
            UpdateCroppedImagePreview();
        CkGui.AttachToolTip("DOES NOT WORK YET!");

        // Add zoom slider
        ImGui.SetNextItemWidth(150f);
        if (ImGui.SliderFloat("Zoom", ref ZoomFactor, MinZoom, MaxZoom, "%.2f"))
            UpdateCroppedImagePreview();

        ImGui.Separator();

        ImUtf8.TextFrameAligned("Original Size: " + img.SizeDataOriginal.SizeString);
        ImUtf8.TextFrameAligned("Constrained Size: " + img.SizeDataConstraintScaled.SizeString);
        ImUtf8.TextFrameAligned("Cropped Size: " + img.SizeDataCropped.SizeString);

        // draw the compress & upload.
        if (CkGui.IconTextButton(FAI.Compress, img.UseScaledData ? "Using Compressed" : "Non-Compressed"))
        {
            img.UseScaledData = !img.UseScaledData;
            SetupZoomFactors(img);
            UpdateCroppedImagePreview();
        }
        CkGui.AttachToolTip("Toggles on if the cropped image references the scaled data or original data." +
            "--SEP-- Using scaled data increases performance.");

        ImGui.SameLine();
        if (CkGui.IconTextButton(FAI.CheckCircle, "Finalize Import", disabled: img.CroppedData.Length <= 0))
            CompressAndSaveFile(img);
    }


    public void ImportFromFile(ImageDataType source, Vector2 sizeConstraint, Vector2 importWinContentSize)
    {
        if(FileDialogTask is not null && !FileDialogTask.IsCompleted)
        {
            _logger.LogWarning("Source is currently: {source}, and the file dialog is still open.", source);
            return;
        }

        // Declare the new source.
        _logger.LogError("Setting CurrentType to: {source}", source);
        CurrentType = source;
        AccessFileDialog(source, sizeConstraint, importWinContentSize);
    }

    public void ImportFromClipboard(ImageDataType source, Vector2 sizeConstraint, Vector2 uiContentRegion)
    {
        if (ImportedImage is not null || source is not ImageDataType.None)
            return;

        var byteArr = ImageDataHandling.GetClipboardImageBytes();
        if (byteArr.IsNullOrEmpty())
            return;

        // Declare the new source.
        CurrentType = source;

        // Create a new ImportedImage object.
        var newImage = new ImportedImage(sizeConstraint);
        // store the original file size
        newImage.OriginalData = byteArr;
        newImage.SizeDataOriginal = new(0, 0, byteArr.Length);

        // process the image for our likings.
        using (var image = Image.Load<Rgba32>(newImage.OriginalData))
        {
            // Get MinSize constraint.
            var minLength = Math.Min(sizeConstraint.X, sizeConstraint.Y);
            // Calculate factor to ensure the smallest dimension is the smaller of the sizeConstraint
            var factor = minLength / Math.Min(image.Width, image.Height);
            var adjustedSize = new Size((int)(image.Width * factor), (int)(image.Height * factor));

            // Resize the image while maintaining the aspect ratio
            SetupZoomFactors(newImage);
            var resizedImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = adjustedSize,
                Mode = ResizeMode.Max
            }));

            // Convert the processed image to byte array
            using (var ms = new MemoryStream())
            {
                // I dont think any of this is needed, but we can see.
                resizedImage.SaveAsPng(ms);
                CropX = 0.5f;
                CropY = 0.5f;

                newImage.ConstraintScaledData = ms.ToArray();
                newImage.SizeDataConstraintScaled = new(resizedImage.Width, resizedImage.Height, newImage.ConstraintScaledData.Length);
                UpdateCroppedImagePreview();
            }
        }

        // Assign the new image to the imported image.
        ImportedImage = newImage;
    }

    private void AccessFileDialog(ImageDataType source, Vector2 sizeConstraint, Vector2 uiContentRegion)
    {
        _fileDialogImport.OpenFileDialog($"Import A New {source} Image", ".png", (success, file) =>
        {
            if (!success)
                return;

            // Assign the task to run.
            FileDialogTask = Task.Run(() =>
            {
                try
                {
                    // Create a new ImportedImage object.
                    var newImage = new ImportedImage(sizeConstraint);

                    // Read the content, and parse it from the stream.
                    var fileContent = File.ReadAllBytes(file);
                    using (MemoryStream ms = new(fileContent))
                    {
                        // Ensure that it properly went to PNG.
                        var format = Image.DetectFormat(ms);
                        if (!format.FileExtensions.Contains("png", StringComparer.OrdinalIgnoreCase))
                            throw new Exception("Error: Image is not in PNG format.");

                        // store the original file size
                        var info = Image.Identify(ms);
                        newImage.OriginalData = ms.ToArray();
                        newImage.SizeDataOriginal = new ImageSizeData(info.Width, info.Height, fileContent.Length);
                    }

                    // Handle necessary logic from any imported image upon selection.
                    using (var image = Image.Load<Rgba32>(fileContent))
                    {
                        // for starters, we need to know if the image's width or length is larger.
                        // This will help us identify the max we can display the item as.
                        // For example, if the original image has an aspect ratio of 16:9,
                        // we would want to display the full image on the top-half of the region,
                        // and the bottom half would display the scaled image constrained such that its dimensions match the height of the bottom segment.
                        var maxSideLength = (image.Width > image.Height) ? (uiContentRegion.Y) : (uiContentRegion.X);
                        var scaleFactor = maxSideLength / Math.Min(image.Width, image.Height);
                        var adjustedSize = new Size((int)(image.Width * scaleFactor), (int)(image.Height * scaleFactor));

                        // Resize the image while maintaining the aspect ratio
                        SetupZoomFactors(newImage);
                        var resizedImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
                        {
                            Size = adjustedSize,
                            Mode = ResizeMode.Max
                        }));

                        // Convert the processed image to byte array
                        using (var ms = new MemoryStream())
                        {
                            // I dont think any of this is needed, but we can see.
                            resizedImage.SaveAsPng(ms);
                            CropX = 0.5f;
                            CropY = 0.5f;

                            newImage.ConstraintScaledData = ms.ToArray();
                            newImage.SizeDataConstraintScaled = new(resizedImage.Width, resizedImage.Height, newImage.ConstraintScaledData.Length);
                            newImage.ConstraintScaledDisplay = _imageService.GetImageFromBytes(newImage.ConstraintScaledData);
                            UpdateCroppedImagePreview(newImage);
                        }
                    }

                    // Assign the new image to the imported image.
                    ImportedImage = newImage;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error importing image: {ex}");
                }
            });
        });
    }

    private void SetupZoomFactors(ImportedImage img)
    {
        MinZoom = 1.0f;

        var sizeDataMax = img.UseScaledData ? img.SizeDataConstraintScaled : img.SizeDataOriginal;
        // --- Aspect-ratio aware zoom calculation ---
        if (sizeDataMax.Width <= sizeDataMax.Height)
            // Portrait or square: anchor width
            MaxZoom = sizeDataMax.Width / img.SizeConstraint.X;
        
        else
            // Landscape: anchor height
            MaxZoom = sizeDataMax.Height / img.SizeConstraint.Y;

        // Correct ZoomFactor.
        if(ZoomFactor > MaxZoom)
            ZoomFactor = MaxZoom;
    }

    public string FinalizedFileSize = string.Empty;
    public byte[] FinalizedData = Array.Empty<byte>();
    public IDalamudTextureWrap? FinalizedDisplay;

    public void CompressAndSaveFile(ImportedImage img)
    {
        if (img.OriginalData.IsNullOrEmpty())
            return;

        using (var image = Image.Load<Rgba32>(img.CroppedData))
        {
            // Resize the constraint area to save on space.
            var resizedImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size((int)img.SizeConstraint.X, (int)img.SizeConstraint.Y),
                Mode = ResizeMode.Max
            }));


            // Convert the processed image to byte array
            using (var ms = new MemoryStream())
            {
                resizedImage.SaveAsPng(ms);
                FinalizedData = ms.ToArray();
                FinalizedFileSize = $"{FinalizedData.Length / 1024.0:F2} KB ({resizedImage.Width}px by {resizedImage.Height}px)";
                FinalizedDisplay = _imageService.GetImageFromBytes(FinalizedData);
            }
        }
    }

    private void UpdateCroppedImagePreview()
        => UpdateCroppedImagePreview(ImportedImage);

    private void UpdateCroppedImagePreview(ImportedImage? imageData)
    {
        if (imageData is null)
            return;

        // Ensure the image is loaded and cropped
        var data = imageData.UseScaledData ? imageData.ConstraintScaledData : imageData.OriginalData;
        using (var image = Image.Load<Rgba32>(data))
        {
            // Calculate the lesser dimension of the original image
            _logger.LogInformation("------------------");
            _logger.LogInformation($"Image Width: {image.Width}, Image Height: {image.Height}");
            
            // the ConstraintSize
            var constraintSize = new Size((int)imageData.SizeConstraint.X, (int)imageData.SizeConstraint.Y);
            _logger.LogInformation($"Constraint Size: {constraintSize}");


            // Calculate the target aspect ratio from the constraint size (e.g., 9:16 or 16:9)
            var constraintAspectRatio = imageData.SizeConstraint.Y / imageData.SizeConstraint.X;
            _logger.LogInformation($"Constraint Aspect Ratio: {constraintAspectRatio}");

            int zoomedWidth, zoomedHeight;

            // --- Aspect-ratio aware zoom calculation ---
            if (image.Width <= image.Height)
            {
                // Portrait or square: anchor width
                zoomedWidth = (int)(image.Width / ZoomFactor);
                zoomedHeight = (int)(zoomedWidth * constraintAspectRatio);
            }
            else
            {
                // Landscape: anchor height
                zoomedHeight = (int)(image.Height / ZoomFactor);
                zoomedWidth = (int)(zoomedHeight / constraintAspectRatio);
            }

            _logger.LogInformation($"Zoomed Size Before Clamp: {zoomedWidth}x{zoomedHeight}");

            // Ensure the zoomed area is at least the constraint size
            zoomedWidth = Math.Max(zoomedWidth, constraintSize.Width);
            zoomedHeight = Math.Max(zoomedHeight, constraintSize.Height);
            //_logger.LogInformation($"Adjusted Zoomed Width: {zoomedWidth}, Adjusted Zoomed Height: {zoomedHeight}");

            // Ensure the zoomed area does not exceed the dimension of the original image
            zoomedWidth = Math.Min(zoomedWidth, image.Width);
            zoomedHeight = Math.Min(zoomedHeight, image.Height);
            _logger.LogInformation($"Final Zoomed Width: {zoomedWidth}, Final Zoomed Height: {zoomedHeight}");

            // Calculate the cropping rectangle based on the user's alignment selection
            var cropRectangle = new Rectangle(0, 0, zoomedWidth, zoomedHeight);
            cropRectangle.X = Math.Max(0, Math.Min((int)((image.Width - zoomedWidth) * CropX), image.Width - zoomedWidth));
            cropRectangle.Y = Math.Max(0, Math.Min((int)((image.Height - zoomedHeight) * CropY), image.Height - zoomedHeight));
            // Make sure we dont go beyond the image bounds.
            cropRectangle.Height = Math.Min(cropRectangle.Height, image.Height - cropRectangle.Y);
            cropRectangle.Width = Math.Min(cropRectangle.Width, image.Width - cropRectangle.X);
            _logger.LogInformation($"Crop Rectangle: {cropRectangle} Ratio: {cropRectangle.Height / (float)cropRectangle.Width}");

            // Create a clone of the image data at the new location.
            var zoomedImage = image.Clone(ctx => ctx.Crop(cropRectangle));
            _logger.LogInformation($"Zoomed Image Size: {zoomedImage.Width} x {zoomedImage.Height}");


            // Convert the processed image to byte array
            using (var ms = new MemoryStream())
            {
                zoomedImage.SaveAsPng(ms);
                imageData.CroppedData = ms.ToArray();
                imageData.SizeDataCropped = new(zoomedImage.Width, zoomedImage.Height, imageData.CroppedData.Length);
                imageData.CroppedDisplay = _imageService.GetImageFromBytes(imageData.CroppedData);
            }
        }
    }

    private void RotateCroppedImagePreview()
        => RotateCroppedImagePreview(ImportedImage);

    private void RotateCroppedImagePreview(ImportedImage? imageData)
    {
        if (imageData is null)
            return;

        var data = imageData.UseScaledData ? imageData.ConstraintScaledData : imageData.OriginalData;
        using (var image = Image.Load<Rgba32>(data))
        {
            // Rotate the image
            var rotatedImage = image.Clone(ctx => ctx.Rotate(Rotation));

            // Convert the processed image to byte array
            using (var ms = new MemoryStream())
            {
                rotatedImage.SaveAsPng(ms);
                imageData.CroppedData = ms.ToArray();
                imageData.SizeDataCropped = new(imageData.SizeDataCropped.Width, imageData.SizeDataCropped.Height, imageData.CroppedData.Length);
                imageData.CroppedDisplay = _imageService.GetImageFromBytes(imageData.CroppedData);
            }
        }
    }
}
