using CkCommons;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Textures;
using CkCommons.Widgets;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GagSpeak.Gui.Components;
public class ImageImportTool
{
    private readonly ILogger<ImageImportTool> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly UiFileDialogService _dialogService;
    private readonly CosmeticService _imageService; // might be able to remove idk.

    private Task? FileDialogTask = null;

    public ImageImportTool(ILogger<ImageImportTool> logger, GagspeakMediator mediator,
        UiFileDialogService dialogService, CosmeticService imgService)
    {
        _logger = logger;
        _mediator = mediator;
        _dialogService = dialogService;
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
            var scaledRatio = (lowerRegion.Y - ImGui.GetFrameHeightWithSpacing()) / img.SizeConstraint.Y;
            var adjustedDisplaySize = img.SizeConstraint * scaledRatio;

            // Use a crop to encapsulate the image with its sliders.
            using (ImRaii.Group())
            {
                var vertSliderSize = new Vector2(ImGui.GetFrameHeightWithSpacing(), adjustedDisplaySize.Y);
                if (ImGui.VSliderFloat("##Height", vertSliderSize, ref PanY, img.MaxPanY, img.MinPanY, "%.1f"))
                    UpdateCroppedImagePreview();

                ImUtf8.SameLineInner();
                using (ImRaii.Group())
                {
                    if (img.CroppedDisplay is { } croppedImage)
                    {
                        var pos = ImGui.GetCursorScreenPos();
                        ImGui.GetWindowDrawList().AddDalamudImageRounded(croppedImage, pos, adjustedDisplaySize, 12f);
                        ImGui.GetWindowDrawList().AddRect(pos, pos + adjustedDisplaySize, 0xFFFFFFFF, 12f);
                    }
                    ImGui.Dummy(adjustedDisplaySize);
                    ImGui.SetNextItemWidth(adjustedDisplaySize.X);
                    if (ImGui.SliderFloat("##Width", ref PanX, img.MinPanX, img.MaxPanX, "%.1f"))
                        UpdateCroppedImagePreview();
                }
            }

            ImGui.SameLine();

            using (ImRaii.Group())
            {
                ImGui.SetNextItemWidth(adjustedDisplaySize.X + ImGui.GetFrameHeightWithSpacing());
                if (ImGui.SliderFloat("Rotation", ref Rotation, -180f, 180f))
                    UpdateCroppedImagePreview();

                ImGui.SetNextItemWidth(adjustedDisplaySize.X + ImGui.GetFrameHeightWithSpacing());
                var zoomRef = img.ZoomFactor;
                if (ImGui.SliderFloat("Zoom", ref zoomRef, img.MinZoom, img.MaxZoom, "%.2f"))
                {
                    // increase or decrease the panX and panY values, respective to the increase / decrease of the zoom.
                    var zoomFactor = zoomRef / img.ZoomFactor;
                    PanX *= zoomFactor;
                    PanY *= zoomFactor;

                    img.ZoomFactor = zoomRef;
                    UpdateCroppedImagePreview();
                }

                ImGui.Separator();
                DrawImageInfo(img);
            }
        }
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.VibrantPink.Uint(), FancyTabBar.Rounding, ImDrawFlags.RoundCornersAll, 1.5f);
    }

    private void DrawVerticalLayout(ImportedImage img)
    {
        var region = ImGui.GetContentRegionAvail();
        var subChildWidth = (region.X - ImGui.GetStyle().ItemSpacing.X) / 2;
        var subChildSize = new Vector2(subChildWidth, region.Y);
        var wdl = ImGui.GetWindowDrawList();

        using (ImRaii.Child("ImageTool_Left_Vertical", subChildSize))
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

        ImGui.SameLine();

        using (ImRaii.Child("ImageTool_Right_Vertical", subChildSize, false, WFlags.AlwaysUseWindowPadding))
        {
            var lowerRegion = ImGui.GetContentRegionAvail();
            // we need to calculate a scaled up cropped image display that takes lowerRegion.Y, and the img.SizeConstraint.Y,
            var maxVertImageHeight = lowerRegion.Y - (ImGui.GetFrameHeightWithSpacing() * 6 + ImGui.GetStyle().ItemSpacing.Y * 2);
            var scaledRatio = maxVertImageHeight / img.SizeConstraint.Y;
            var adjustedDisplaySize = img.SizeConstraint * scaledRatio;

            // Use a crop to encapsulate the image with its sliders.
            using (ImRaii.Group())
            {
                if (img.CroppedDisplay is { } croppedImage)
                {
                    var pos = ImGui.GetCursorScreenPos();
                    ImGui.GetWindowDrawList().AddDalamudImageRounded(croppedImage, pos, adjustedDisplaySize, 12f);
                    ImGui.GetWindowDrawList().AddRect(pos, pos + adjustedDisplaySize, 0xFFFFFFFF, 12f);
                }
                ImGui.Dummy(adjustedDisplaySize);

                ImUtf8.SameLineInner();
                var vertSliderSize = new Vector2(ImGui.GetFrameHeightWithSpacing(), adjustedDisplaySize.Y);
                if (ImGui.VSliderFloat("##Height", vertSliderSize, ref PanY, img.MaxPanY, img.MinPanY, "%.1f"))
                    UpdateCroppedImagePreview();

                // Below this, draw the width.
                ImGui.SetNextItemWidth(adjustedDisplaySize.X);
                if (ImGui.SliderFloat("##Width", ref PanX, img.MinPanX, img.MaxPanX, "%.1f"))
                    UpdateCroppedImagePreview();
            }
            ImGui.Separator();

            ImGui.SetNextItemWidth(adjustedDisplaySize.X);
            if (ImGui.SliderFloat("Rotation", ref Rotation, -180f, 180f))
                UpdateCroppedImagePreview();

            ImGui.SetNextItemWidth(adjustedDisplaySize.X);
            var zoomRef = img.ZoomFactor;
            if (ImGui.SliderFloat("Zoom", ref zoomRef, img.MinZoom, img.MaxZoom, "%.2f"))
            {
                // increase or decrease the panX and panY values, respective to the increase / decrease of the zoom.

                img.ZoomFactor = zoomRef;
                UpdateCroppedImagePreview();
            }

            ImGui.Separator();

            DrawImageInfo(img);
        }
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.VibrantPink.Uint(), FancyTabBar.Rounding, ImDrawFlags.RoundCornersAll, 1.5f);
    }

    private void DrawImageInfo(ImportedImage img)
    {
        using var _ = ImRaii.Group();

        ImUtf8.TextFrameAligned("Original Size: " + img.SizeDataOriginal.SizeString);
        ImUtf8.TextFrameAligned("Cropped Size: " + img.SizeDataCropped.SizeString);
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

        if(!Generic.TryGetClipboardImage(out var byteArr, out var imgContext))
        {
            _logger.LogWarning("Clipboard image is not valid.");
            return;
        }

        // Create a new ImportedImage object.
        var newImage = new ImportedImage(sizeConstraint, source);
        newImage.OriginalData = byteArr;
        newImage.SizeDataOriginal = new(imgContext.Width, imgContext.Height, byteArr.Length);
        
        // Get the data about the image.
        var image = Image.Load<Rgba32>(newImage.OriginalData);
        newImage.OriginalImage = image;
        newImage.OriginalDisplay = TextureManager.GetImageFromBytes(newImage.OriginalData);

        // Upscale if the image was too small, otherwise, continue.
        var lesserDimension = Math.Min(image.Width, image.Height);
        var maxSideLength = sizeConstraint.X > sizeConstraint.Y ? sizeConstraint.Y : sizeConstraint.X;
        if (lesserDimension < maxSideLength)
        {
            var scaleFactor = maxSideLength / lesserDimension;
            var adjustedSize = new Size(
                (int)(image.Width * scaleFactor),
                (int)(image.Height * scaleFactor));

            // Resize the image while maintaining the aspect ratio
            var resized = image.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = adjustedSize,
                Mode = ResizeMode.Max
            }));

            // Update the original image to the resized image so that it fits the defined constraints.
            using var ms = new MemoryStream();
            resized.SaveAsPng(ms);
            PanX = PanY = 0f;
            newImage.OriginalImage = resized;
            newImage.OriginalData = ms.ToArray();
            newImage.SizeDataOriginal = new(resized.Width, resized.Height, ms.ToArray().Length);
        }

        UpdateCroppedImagePreview(newImage);
        ImportedImage = newImage;
    }

    private void AccessFileDialog(ImportedImage newImage, Vector2 uiContentRegion)
    {
        _dialogService.OpenSingleFilePicker($"Import A New {newImage.ImageType} Image", ".png", (success, file) =>
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
                        newImage.OriginalDisplay = TextureManager.GetImageFromBytes(newImage.OriginalData);
                        PanX = PanY = 0f;
                    }

                    // Handle necessary logic from any imported image upon selection.
                    using (var image = Image.Load<Rgba32>(fileContent))
                    {
                        var lesserDimension = Math.Min(image.Width, image.Height);
                        var maxSideLength = image.Width > image.Height ? uiContentRegion.Y : uiContentRegion.X;
                        if (lesserDimension < maxSideLength)
                        {
                            _logger.LogWarning("Image is too small, resizing to fit the constraints.");
                            var scaleFactor = maxSideLength / lesserDimension;
                            var adjustedSize = new Size(
                                (int)(image.Width * scaleFactor),
                                (int)(image.Height * scaleFactor));

                            // Resize the image while maintaining the aspect ratio
                            var resized = image.Clone(ctx => ctx.Resize(new ResizeOptions
                            {
                                Size = adjustedSize,
                                Mode = ResizeMode.Max
                            }));

                            // Update the original image to the resized image so that it fits the defined constraints.
                            using var ms = new MemoryStream();
                            resized.SaveAsPng(ms);
                            PanX = PanY = 0f;
                            newImage.OriginalImage = resized;
                            newImage.OriginalData = ms.ToArray();
                            newImage.SizeDataOriginal = new(resized.Width, resized.Height, ms.ToArray().Length);
                        }
                    }

                    // Assign the new image to the imported image.
                    UpdateCroppedImagePreview(newImage);
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
            _mediator.Publish(new ReScanThumbnailFolder());
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

        var targetWidth = (int)imageData.SizeConstraint.X;
        var targetHeight = (int)imageData.SizeConstraint.Y;

        // Step 1: Calculate the scaled size based on the zoom factor
        var scaledWidth = (int)(imageData.OriginalImage.Width * imageData.ZoomFactor);
        var scaledHeight = (int)(imageData.OriginalImage.Height * imageData.ZoomFactor);
        _logger.LogTrace($"Scaled Size: {scaledWidth} x {scaledHeight}", LoggerType.Textures);

        // Step 2: Resize the image based on zoom
        using var scaled = imageData.OriginalImage.Clone(ctx => ctx.Resize(scaledWidth, scaledHeight));

        // Step 3: Calculate corrected pan offset so that pan moves in screen-space (not image-space)
        var radians = -Rotation * (Math.PI / 180.0); // negative to counteract rotation
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var adjustedPanX = PanX * cos - PanY * sin;
        var adjustedPanY = PanX * sin + PanY * cos;
        _logger.LogTrace($"Adjusted Pan: {adjustedPanX} x {adjustedPanY}", LoggerType.Textures);

        // Step 4: Calculate viewport center (the center of the cropped frame in the current view)
        var viewportCenterX = scaledWidth / 2 + (int)PanX;
        var viewportCenterY = scaledHeight / 2 + (int)PanY;
        _logger.LogTrace($"Viewport Center: {viewportCenterX} x {viewportCenterY}", LoggerType.Textures);

        // Step 5: Create a new padded canvas large enough to avoid clipping during rotation
        var paddedSize = (int)(Math.Sqrt(targetWidth * targetWidth + targetHeight * targetHeight) * 1.5); // extra room
        var canvasWidth = paddedSize;
        var canvasHeight = paddedSize;
        _logger.LogTrace($"Canvas Size: {canvasWidth} x {canvasHeight}", LoggerType.Textures);

        // Step 6: Create padded image and draw scaled image onto it, so the desired rotation center is at the canvas center
        using var padded = new Image<Rgba32>(canvasWidth, canvasHeight);
        var drawX = (canvasWidth / 2) - viewportCenterX;
        var drawY = (canvasHeight / 2) - viewportCenterY;
        padded.Mutate(ctx => ctx.DrawImage(scaled, new Point(drawX, drawY), 1f));
        _logger.LogTrace($"Draw Position: {drawX} x {drawY}", LoggerType.Textures);

        // Step 7: Calculate the crop region in the scaled image based on pan and zoom
        var offsetX = (scaledWidth - targetWidth) / 2 + (int)PanX;
        var offsetY = (scaledHeight - targetHeight) / 2 + (int)PanY;
        _logger.LogTrace($"Crop Offset: {offsetX} x {offsetY}", LoggerType.Textures);

        // Ensure the crop rectangle stays within the image bounds
        offsetX = Math.Max(0, Math.Min(offsetX, scaledWidth - targetWidth));
        offsetY = Math.Max(0, Math.Min(offsetY, scaledHeight - targetHeight));
        _logger.LogTrace($"Adjusted Crop Offset: {offsetX} x {offsetY}", LoggerType.Textures);

        var cropRect = new Rectangle(offsetX, offsetY, targetWidth, targetHeight);
        _logger.LogTrace($"Crop Rectangle: {cropRect}", LoggerType.Textures);

        // Step 8: Apply rotation around the center of the cropped region
        var rotated = padded.Clone(ctx =>
        {
            if (Math.Abs(Rotation) > 0.01f)
                // Rotate around the center of the cropped area
                ctx.Rotate(Rotation);
        });
        _logger.LogTrace($"Rotated Image Size: {rotated.Width} x {rotated.Height}", LoggerType.Textures);

        // Step 9: Crop the final result to ensure it's centered within the target size
        var final = rotated.Clone(ctx =>
        {
            var cx = (rotated.Width - targetWidth) / 2;
            var cy = (rotated.Height - targetHeight) / 2;
            ctx.Crop(new Rectangle(cx, cy, targetWidth, targetHeight));
        });
        _logger.LogTrace($"Final Image Size: {final.Width} x {final.Height}", LoggerType.Textures);

        // Convert the processed image to byte array
        using (var ms = new MemoryStream())
        {
            final.SaveAsPng(ms);
            imageData.CroppedData = ms.ToArray();
            imageData.SizeDataCropped = new(final.Width, final.Height, imageData.CroppedData.Length);
            imageData.CroppedDisplay = TextureManager.GetImageFromBytes(imageData.CroppedData);
        }
    }
}
