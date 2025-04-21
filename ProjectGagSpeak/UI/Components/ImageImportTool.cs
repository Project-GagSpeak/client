using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.ImageHandling;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Textures;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace GagSpeak.UI.Components;
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
    public ImportedImage? ImportedImage { get; private set; } = null;

    private float PanX = 0;
    private float PanY = 0;
    private float Rotation = 0.0f;

    public bool ShouldDrawImportContent(ImageDataType src)
        => ImportedImage is not null && ImportedImage.ImageType == src;

    public void DrawImportContent()
    {
        if (ImportedImage is null || !ImportedImage.HasValidData)
        {
            ImGui.Text("InvalidImageObject.");
            ImGui.Text("Type: " + ImportedImage?.ImageType);
            ImGui.Text("SizeConstraint: " + ImportedImage?.SizeConstraint);
            ImGui.Text("FileName: " + ImportedImage?.FileName);
            ImGui.Text("OriginalImageSize: " + ImportedImage?.SizeDataOriginal.SizeString);
            ImGui.Text("CroppedImageSize: " + ImportedImage?.SizeDataCropped.SizeString);
            return;
        }

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
        var wdl = ImGui.GetWindowDrawList();

        using (ImRaii.Child("ImageTool_Top_Horizontal", subChildSize))
        {
            if (img.OriginalDisplay is { } fullDisplayImage)
            {
                // ensure image fits fully inside inner region, preserving aspect ratio
                var widthRatio = subChildSize.X / fullDisplayImage.Width;
                var heightRatio = subChildSize.Y / fullDisplayImage.Height;
                var finalScale = Math.Min(widthRatio, heightRatio);

                var adjustedSize = fullDisplayImage.Size * finalScale;

                // Center the image horizontally if there's extra space
                var shiftX = Math.Max(0, (subChildSize.X - adjustedSize.X) / 2);
                var shiftY = Math.Max(0, (subChildSize.Y - adjustedSize.Y) / 2);
                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(shiftX, shiftY));

                // get the pos and draw the image.
                var pos = ImGui.GetCursorScreenPos();
                wdl.AddDalamudImageRounded(fullDisplayImage, pos, adjustedSize, 12f);
                ImGui.Dummy(adjustedSize);
            }
            else
            {
                ImGui.Dummy(subChildSize);
            }
        }
        wdl.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.VibrantPink.Uint(), FancyTabBar.Rounding, ImDrawFlags.RoundCornersAll, 1.5f);

        using (ImRaii.Child("ImageTool_Bottom_Horizontal", subChildSize, false, WFlags.AlwaysUseWindowPadding))
        {
            var lowerRegion = ImGui.GetContentRegionAvail();

            // we need to calculate a scaled up cropped image display that takes lowerRegion.Y, and the img.SizeConstraint.Y,
            var scaledRatio = lowerRegion.Y / img.SizeConstraint.Y;
            var adjustedDisplaySize = img.SizeConstraint * scaledRatio;
            if (img.CroppedDisplay is { } croppedImage)
            {
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddDalamudImageRounded(croppedImage, pos, croppedImage.Size, 12f);
                ImGui.GetWindowDrawList().AddRect(pos, pos + croppedImage.Size, 0xFFFFFFFF, 12f);
            }
            ImGui.Dummy(adjustedDisplaySize);

            ImGui.SameLine();
            DrawImageSliders(img);
        }
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.VibrantPink.Uint(), FancyTabBar.Rounding, ImDrawFlags.RoundCornersAll, 1.5f);
    }

    private void DrawVerticalLayout(ImportedImage img)
    {
        using (ImRaii.Group())
        {
            var leftRegion = new Vector2(ImGui.GetContentRegionAvail().X / 2, ImGui.GetContentRegionAvail().Y);
            var spacing = ImGui.GetStyle().ItemSpacing.X;

            if (img.OriginalDisplay is { } fullDisplayImage)
            {
                var leftImgSize = fullDisplayImage.Size;
                // determine the comparison between the left region and the constrained to know how far down to draw the image so it is centered.
                var shiftHeight = Math.Max(0, (leftRegion.Y - leftImgSize.Y) / 2);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + shiftHeight);
                // get the pos and draw the image.
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddDalamudImageRounded(fullDisplayImage, pos, leftImgSize, 12f);
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

        ImGui.SetNextItemWidth(250f);
        if (ImGui.SliderFloat("Width", ref PanX, img.MinPanX, img.MaxPanX, "%.1f"))
            UpdateCroppedImagePreview();

        ImGui.SetNextItemWidth(250f);
        if (ImGui.SliderFloat("Height", ref PanY, img.MinPanY, img.MaxPanY, "%.1f"))
            UpdateCroppedImagePreview();

        ImGui.SetNextItemWidth(250f);
        if (ImGui.SliderFloat("Rotation", ref Rotation, -180f, 180f))
            UpdateCroppedImagePreview();
        CkGui.AttachToolTip("DOES NOT WORK YET!");

        // Add zoom slider
        ImGui.SetNextItemWidth(250f);
        var zoomRef = img.ZoomFactor;
        if (ImGui.SliderFloat("Zoom", ref zoomRef, img.MinZoom, img.MaxZoom, "%.2f"))
        {
            img.ZoomFactor = zoomRef;
            UpdateCroppedImagePreview();
        }
        ImGui.Separator();

        ImUtf8.TextFrameAligned("Original Size: " + img.SizeDataOriginal.SizeString);
        ImUtf8.TextFrameAligned("Cropped Size: " + img.SizeDataCropped.SizeString);

        ImUtf8.TextFrameAligned("MinZoom: Size: " + img.MinZoom);
        ImUtf8.TextFrameAligned("MaxZoom: Size: " + img.MaxZoom);
        ImUtf8.TextFrameAligned("MinPan: Size: " + img.MinPanX + " - " + img.MinPanY);
        ImUtf8.TextFrameAligned("MaxPan: Size: " + img.MaxPanX + " - " + img.MaxPanY);

        /*        // draw the compress & upload.
                if (CkGui.IconTextButton(FAI.Compress, img.UseScaledData ? "Using Compressed" : "Non-Compressed", disabled: img.OriginalSmallerThanScaled))
                {
                    img.UseScaledData = !img.UseScaledData;
                    SetupZoomFactors(img);
                    UpdateCroppedImagePreview();
                }
                CkGui.AttachToolTip("Toggles on if the cropped image references the scaled data or original data." +
                    "--SEP-- Using scaled data increases performance.");*/

        ImGui.SameLine();
        if (CkGui.IconTextButton(FAI.CheckCircle, "Finalize Import", disabled: img.CroppedData.Length <= 0))
            CompressAndSaveFile();
    }


    public void ImportFromFile(ImageDataType source, Vector2 sizeConstraint, Vector2 importWinContentSize, bool force = false)
    {
        if(FileDialogTask is not null && !FileDialogTask.IsCompleted)
        {
            _logger.LogWarning("Source is currently: {source}, and the file dialog is still open.", source);
            return;
        }

        // Prevent Re-Import if no source match or force.
        if (ImportedImage is not null && (force is false || ImportedImage.ImageType != source))
        {
            _logger.LogWarning("Re-Importing is not allowed, you must force this action to allow it.");
            return;
        }

        var newImage = new ImportedImage(sizeConstraint, source);
        AccessFileDialog(newImage, importWinContentSize);
    }

    public void ImportFromClipboard(ImageDataType source, Vector2 sizeConstraint, Vector2 uiContentRegion, bool force = false)
    {
        if (ImportedImage is not null && (force is false || ImportedImage.ImageType != source))
        {
            _logger.LogWarning("Cannot Re-Import without doing so forcefully. Otherwise, this occurred due to invalid type match");
            return;
        }

        if(!ImageDataHandling.TryGetClipboardImage(out var byteArr, out var imgContext))
        {
            _logger.LogWarning("Clipboard image is not valid.");
            return;
        }

        // Create a new ImportedImage object.
        var newImage = new ImportedImage(sizeConstraint, source);
        newImage.OriginalData = byteArr;
        newImage.SizeDataOriginal = new(imgContext.Width, imgContext.Height, byteArr.Length);

        // process the image for our likings.
        using (var image = Image.Load<Rgba32>(newImage.OriginalData))
        {
            // Get MinSize constraint.
            var minLength = Math.Min(sizeConstraint.X, sizeConstraint.Y);
            // Calculate factor to ensure the smallest dimension is the smaller of the sizeConstraint
            var factor = minLength / Math.Min(image.Width, image.Height);
            var adjustedSize = new Size((int)(image.Width * factor), (int)(image.Height * factor));

            // Resize the image while maintaining the aspect ratio
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
                PanX = 0.0f;
                PanY = 0.0f;
/*
                newImage.ConstraintScaledData = ms.ToArray();
                newImage.SizeDataConstraintScaled = new(resizedImage.Width, resizedImage.Height, newImage.ConstraintScaledData.Length);
                newImage.ConstraintScaledDisplay = _imageService.GetImageFromBytes(newImage.ConstraintScaledData);*/
                UpdateCroppedImagePreview(newImage);
            }
        }

        // Assign the new image to the imported image.
        ImportedImage = newImage;
    }

    private void AccessFileDialog(ImportedImage newImage, Vector2 uiContentRegion)
    {
        _fileDialogImport.OpenFileDialog($"Import A New {newImage.ImageType} Image", ".png", (success, file) =>
        {
            if (!success)
                return;

            // Assign the task to run.
            FileDialogTask = Task.Run(() =>
            {
                try
                {
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
                        newImage.OriginalImage = Image.Load<Rgba32>(fileContent);
                        newImage.OriginalData = ms.ToArray();
                        newImage.SizeDataOriginal = new ImageSizeData(info.Width, info.Height, fileContent.Length);
                        newImage.OriginalDisplay = _imageService.GetImageFromBytes(newImage.OriginalData);
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
                            PanX = 0f;
                            PanY = 0f;

/*                            newImage.ConstraintScaledData = ms.ToArray();
                            newImage.SizeDataConstraintScaled = new(resizedImage.Width, resizedImage.Height, newImage.ConstraintScaledData.Length);
                            newImage.ConstraintScaledDisplay = _imageService.GetImageFromBytes(newImage.ConstraintScaledData);*/
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

    public void CompressAndSaveFile()
    {
        if (ImportedImage is null || ImportedImage.OriginalData.IsNullOrEmpty())
            return;

        using (var image = Image.Load<Rgba32>(ImportedImage.CroppedData))
        {
            // Resize the constraint area to save on space.
            var resizedImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size((int)ImportedImage.SizeConstraint.X, (int)ImportedImage.SizeConstraint.Y),
                Mode = ResizeMode.Max
            }));


            // Convert the processed image to byte array
            var finalizedByteData = Array.Empty<byte>();
            using (var ms = new MemoryStream())
            {
                resizedImage.SaveAsPng(ms);
                finalizedByteData = ms.ToArray();
            }

            // Ensure a unique name is present first.
            var generatedName = ImportedImage.FileName.IsNullOrEmpty() ? Guid.NewGuid().ToString() : ImportedImage.FileName;

            var folderFileNames = Directory
                .GetFiles(Path.Combine(ConfigFileProvider.ThumbnailDirectory, ImportedImage.ImageType.ToString()))
                .Where(f => Path.GetExtension(f).Equals(".png", StringComparison.OrdinalIgnoreCase))
                .Select(f => Path.GetFileName(f))
                .Where(name => name is not null)
                .ToList();

            var finalName = RegexEx.EnsureUniqueName(generatedName, folderFileNames, name => name);
            var savePath = Path.Combine(ConfigFileProvider.ThumbnailDirectory, ImportedImage.ImageType.ToString(), finalName + ".png");
            File.WriteAllBytes(savePath, finalizedByteData);
            _logger.LogInformation($"Saved image with name {finalName} to {savePath}!");
            // Dispose of the image data
            ImportedImage.Dispose();
            ImportedImage = null;
        }
    }

    private void UpdateCroppedImagePreview()
        => UpdateCroppedImagePreview(ImportedImage);

    private void UpdateCroppedImagePreview(ImportedImage? imageData)
    {
        if (imageData is null)
            return;

        // Update all other variables.
        PanX = Math.Clamp(PanX, imageData.MinPanX, imageData.MaxPanX);
        PanY = Math.Clamp(PanY, imageData.MinPanY, imageData.MaxPanY);
        Rotation = Math.Clamp(Rotation, -180f, 180f);
        imageData.ZoomFactor = Math.Clamp(imageData.ZoomFactor, imageData.MinZoom, imageData.MaxZoom);

        _logger.LogInformation("---------------------");
        var image = imageData.OriginalImage;

        int targetWidth = (int)imageData.SizeConstraint.X;
        int targetHeight = (int)imageData.SizeConstraint.Y;

        // Step 1: Calculate the scaled size based on the zoom factor
        int scaledWidth = (int)(image.Width * imageData.ZoomFactor);
        int scaledHeight = (int)(image.Height * imageData.ZoomFactor);
        _logger.LogInformation($"Scaled Size: {scaledWidth} x {scaledHeight}");

        // Step 2: Resize the image based on zoom
        using var scaled = image.Clone(ctx => ctx.Resize(scaledWidth, scaledHeight));

        // Step 3: Calculate viewport center (the center of the cropped frame in the current view)
        int viewportCenterX = scaledWidth / 2 + (int)PanX;
        int viewportCenterY = scaledHeight / 2 + (int)PanY;
        _logger.LogInformation($"Viewport Center: {viewportCenterX} x {viewportCenterY}");

        // Step 4: Create a new padded canvas large enough to avoid clipping during rotation
        int paddedSize = (int)(Math.Sqrt(targetWidth * targetWidth + targetHeight * targetHeight) * 1.5); // extra room
        int canvasWidth = paddedSize;
        int canvasHeight = paddedSize;
        _logger.LogInformation($"Canvas Size: {canvasWidth} x {canvasHeight}");

        // Step 5: Create padded image and draw scaled image onto it, so the desired rotation center is at the canvas center
        var padded = new Image<Rgba32>(canvasWidth, canvasHeight);
        int drawX = (canvasWidth / 2) - viewportCenterX;
        int drawY = (canvasHeight / 2) - viewportCenterY;
        _logger.LogInformation($"Draw Position: {drawX} x {drawY}");

        padded.Mutate(ctx => ctx.DrawImage(scaled, new Point(drawX, drawY), 1f));

        // Step 6: Calculate the crop region in the scaled image based on pan and zoom
        int offsetX = (scaledWidth - targetWidth) / 2 + (int)PanX;
        int offsetY = (scaledHeight - targetHeight) / 2 + (int)PanY;
        _logger.LogInformation($"Crop Offset: {offsetX} x {offsetY}");

        // Ensure the crop rectangle stays within the image bounds
        offsetX = Math.Max(0, Math.Min(offsetX, scaledWidth - targetWidth));
        offsetY = Math.Max(0, Math.Min(offsetY, scaledHeight - targetHeight));
        _logger.LogInformation($"Adjusted Crop Offset: {offsetX} x {offsetY}");

        var cropRect = new Rectangle(offsetX, offsetY, targetWidth, targetHeight);
        _logger.LogInformation($"Crop Rectangle: {cropRect}");

        // Step 8: Apply rotation around the center of the cropped region
        var rotated = padded.Clone(ctx =>
        {
            if (Math.Abs(Rotation) > 0.01f)
            {
                // Rotate around the center of the cropped area
                ctx.Rotate(Rotation);
            }
        });
        _logger.LogInformation($"Rotated Image Size: {rotated.Width} x {rotated.Height}");

        // Step 9: Crop the final result to ensure it's centered within the target size
        var final = rotated.Clone(ctx =>
        {
            int cx = (rotated.Width - targetWidth) / 2;
            int cy = (rotated.Height - targetHeight) / 2;
            ctx.Crop(new Rectangle(cx, cy, targetWidth, targetHeight));
        });
        _logger.LogInformation($"Final Image Size: {final.Width} x {final.Height}");

        // Convert the processed image to byte array
        using (var ms = new MemoryStream())
        {
            final.SaveAsPng(ms);
            imageData.CroppedData = ms.ToArray();
            imageData.SizeDataCropped = new(final.Width, final.Height, imageData.CroppedData.Length);
            imageData.CroppedDisplay = _imageService.GetImageFromBytes(imageData.CroppedData);
        }
    }
}
