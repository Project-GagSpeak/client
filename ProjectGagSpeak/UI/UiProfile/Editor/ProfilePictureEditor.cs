using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Textures.TextureWraps; // This is discouraged, try and look into better way to do it later.
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using ImGuiNET;
using Microsoft.VisualBasic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace GagSpeak.UI.Profile;

public class ProfilePictureEditor : WindowMediatorSubscriberBase
{
    private readonly MainHub _apiHubMain;
    private readonly FileDialogManager _fileDialogManager;
    private readonly KinkPlateService _KinkPlateManager;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;
    public ProfilePictureEditor(ILogger<ProfilePictureEditor> logger, GagspeakMediator mediator,
        MainHub apiHubMain, FileDialogManager fileDialogManager, KinkPlateService KinkPlateManager,
        CosmeticService cosmetics, UiSharedService uiSharedService)
        : base(logger, mediator, "Edit KinkPlate Profile Picture###KinkPlateProfilePictureUI")
    {
        IsOpen = false;
        Size = new(768, 600);
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar;
        AllowClickthrough = false;
        AllowPinning = false;
        _apiHubMain = apiHubMain;
        _fileDialogManager = fileDialogManager;
        _KinkPlateManager = KinkPlateManager;
        _cosmetics = cosmetics;
        _uiShared = uiSharedService;

        Mediator.Subscribe<MainHubDisconnectedMessage>(this, (_) => IsOpen = false);
    }

    private bool _showFileDialogError = false;

    // Store the original image data of our imported file.
    private byte[] _uploadedImageData;
    private IDalamudTextureWrap? _uploadedImageToShow;

    // Determine if we are using a compressed image.
    private bool _useCompressedImage = false; // Default to using compressed image
    private byte[] _compressedImageData;
    private IDalamudTextureWrap? _compressedImageToShow;

    // hold a temporary image data of the cropped image area without affecting the original or compressed image.
    private byte[] _scopedData = null!;
    private byte[] _croppedImageData;
    private IDalamudTextureWrap? _croppedImageToShow;

    // the other values for movement, rotation, and scaling.
    private float _cropX = 0.5f; // Center by default
    private float _cropY = 0.5f; // Center by default
    private float _rotationAngle = 0.0f; // Rotation angle in degrees
    private float _zoomFactor = 1.0f; // Zoom factor, 1.0 means no zoom
    private float _minZoomFactor = 1.0f;
    private float _maxZoomFactor = 3.0f;

    // Store file size references for both debug metrics and to show the user how optimized their images is.
    public string OriginalFileSize { get; private set; } = string.Empty;
    public string ScaledFileSize { get; private set; } = string.Empty;
    public string CroppedFileSize { get; private set; } = string.Empty;

    protected override void PreDrawInternal() { }
    protected override void PostDrawInternal() { }
    protected override void DrawInternal()
    {
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        // grab our profile.
        var profile = _KinkPlateManager.GetKinkPlate(new UserData(MainHub.UID));

        // check if flagged
        if (profile.KinkPlateInfo.Flagged)
        {
            UiSharedService.ColorTextWrapped(profile.KinkPlateInfo.Description, ImGuiColors.DalamudRed);
            return;
        }

        // grab our profile image and draw the baseline.
        var pfpWrap = profile.GetCurrentProfileOrDefault();
        if (pfpWrap != null)
        {
            ImGui.Image(pfpWrap.ImGuiHandle, ImGuiHelpers.ScaledVector2(pfpWrap.Width, pfpWrap.Height));
        }
        // scoot over to the right 256px + spacing
        ImGuiHelpers.ScaledRelativeSameLine(256, spacing);
        if (pfpWrap != null)
        {
            // then the rounded image.
            var currentPosition = ImGui.GetCursorPos();
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddImageRounded(pfpWrap.ImGuiHandle, pos, pos + pfpWrap.Size, Vector2.Zero, Vector2.One, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), 128f);
            ImGui.SetCursorPos(new Vector2(currentPosition.X, currentPosition.Y + pfpWrap.Height));
        }
        ImGuiHelpers.ScaledRelativeSameLine(256, spacing);

        // we need here to draw the group for content.
        using (ImRaii.Group())
        {
            _uiShared.GagspeakTitleText("Current Image");
            ImGui.Separator();
            UiSharedService.ColorText("Square Image Preview:", ImGuiColors.ParsedGold);
            UiSharedService.TextWrapped("Meant to display the original display of the stored image data.");
            ImGui.Spacing();
            UiSharedService.ColorText("Rounded Image Preview:", ImGuiColors.ParsedGold);
            UiSharedService.TextWrapped("This is what's seen in the account page, and inside of KinkPlates™");
            ImGui.Spacing();

            var width = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Trash, "Clear uploaded profile picture");
            // move down to the newline and draw the buttons for adding and removing images
            if (_uiShared.IconTextButton(FontAwesomeIcon.FileUpload, "Upload new profile picture", width))
                HandleFileDialog();
            UiSharedService.AttachToolTip("Select and upload a new profile picture");

            // let them clean their image too if they desire.
            if (_uiShared.IconTextButton(FontAwesomeIcon.Trash, "Clear uploaded profile picture", width, disabled: !KeyMonitor.ShiftPressed()))
            {
                _uploadedImageData = null!;
                _croppedImageData = null!;
                _uploadedImageToShow = null;
                _croppedImageToShow = null;
                _useCompressedImage = false;
                _ = _apiHubMain.UserSetKinkPlatePicture(new UserKinkPlatePictureDto(new UserData(MainHub.UID), string.Empty));
            }
            UiSharedService.AttachToolTip("Clear your currently uploaded profile picture--SEP--Must be holding SHIFT to clear.");

            // show file dialog error if we had one.
            if (_showFileDialogError)
                UiSharedService.ColorTextWrapped("The profile picture must be a PNG file", ImGuiColors.DalamudRed);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // draw out the editor settings.
        if (_uploadedImageData != null)
            DrawNewProfileDisplay(profile);
    }

    private void HandleFileDialog()
    {
        _fileDialogManager.OpenFileDialog("Select new Profile picture", ".png", (success, file) =>
        {
            if (!success)
            {
                _logger.LogWarning("Failed to open file dialog.");
                return;
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Attempting to upload new profile picture.");
                    var fileContent = File.ReadAllBytes(file);
                    using (MemoryStream ms = new(fileContent))
                    {
                        var format = await Image.DetectFormatAsync(ms).ConfigureAwait(false);
                        if (!format.FileExtensions.Contains("png", StringComparer.OrdinalIgnoreCase))
                        {
                            _showFileDialogError = true;
                            return;
                        }
                        // store the original file size
                        OriginalFileSize = $"{fileContent.Length / 1024.0:F2} KB";
                        _uploadedImageData = ms.ToArray();
                    }

                    // Load and process the image
                    using (var image = Image.Load<Rgba32>(fileContent))
                    {
                        // Calculate the scale factor to ensure the smallest dimension is 256 pixels
                        var scale = 256f / Math.Min(image.Width, image.Height);
                        var scaledSize = new Size((int)(image.Width * scale), (int)(image.Height * scale));

                        InitializeZoomFactors(image.Width, image.Height);

                        // Resize the image while maintaining the aspect ratio
                        var resizedImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
                        {
                            Size = scaledSize,
                            Mode = ResizeMode.Max
                        }));

                        // Convert the processed image to byte array
                        using (var ms = new MemoryStream())
                        {
                            resizedImage.SaveAsPng(ms);
                            _scopedData = ms.ToArray();

                            // Initialize cropping parameters
                            _cropX = 0.5f;
                            _cropY = 0.5f;

                            _uploadedImageToShow = _cosmetics.GetProfilePicture(_uploadedImageData);
                            ScaledFileSize = $"{_scopedData.Length / 1024.0:F2} KB";

                            // Update the preview image
                            UpdateCroppedImagePreview();
                        }
                    }

                    _showFileDialogError = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload new profile picture.");
                }
            });
        });
    }

    private void DrawNewProfileDisplay(KinkPlate profile)
    {
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        if (_uploadedImageData != null)
        {
            // display the respective info on the line below.
            if (_croppedImageData != null)
            {
                // ensure the wrap for the data is not yet null
                if (_croppedImageToShow != null)
                {
                    ImGui.Image(_croppedImageToShow.ImGuiHandle, ImGuiHelpers.ScaledVector2(_croppedImageToShow.Width, _croppedImageToShow.Height), Vector2.Zero, Vector2.One, ImGuiColors.DalamudWhite, ImGuiColors.DalamudWhite);

                    ImGuiHelpers.ScaledRelativeSameLine(256, spacing);
                    var currentPosition = ImGui.GetCursorPos();
                    var pos = ImGui.GetCursorScreenPos();
                    ImGui.GetWindowDrawList().AddImageRounded(_croppedImageToShow.ImGuiHandle, pos, pos + _croppedImageToShow.Size, Vector2.Zero, Vector2.One, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), 128f);
                    ImGui.SetCursorPos(new Vector2(currentPosition.X, currentPosition.Y + _croppedImageToShow.Height));
                }
            }
        }
        // draw the slider and the update buttons
        ImGuiHelpers.ScaledRelativeSameLine(256, spacing);
        using (ImRaii.Group())
        {
            _uiShared.GagspeakTitleText("Image Editor");
            ImGui.Separator();
            if (_croppedImageData != null)
            {
                var cropXref = _cropX;
                ImGui.SetNextItemWidth(150f);
                if (ImGui.SliderFloat("Width", ref _cropX, 0.0f, 1.0f, "%.2f"))
                    if (cropXref != _cropX)
                        UpdateCroppedImagePreview();

                var cropYref = _cropY;
                ImGui.SetNextItemWidth(150f);
                if (ImGui.SliderFloat("Height", ref _cropY, 0.0f, 1.0f, "%.2f"))
                    if (cropYref != _cropY)
                        UpdateCroppedImagePreview();

                // Add rotation slider
                var rotationRef = _rotationAngle;
                ImGui.SetNextItemWidth(150f);
                if (ImGui.SliderFloat("Rotation", ref _rotationAngle, 0.0f, 360.0f, "%.2f"))
                    if (rotationRef != _rotationAngle)
                        UpdateCroppedImagePreview();
                UiSharedService.AttachToolTip("DOES NOT WORK YET!");

                // Add zoom slider
                var zoomRef = _zoomFactor;
                ImGui.SetNextItemWidth(150f);
                if (ImGui.SliderFloat("Zoom", ref _zoomFactor, _minZoomFactor, _maxZoomFactor, "%.2f"))
                    if (zoomRef != _zoomFactor)
                        UpdateCroppedImagePreview();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Original Image Size: " + OriginalFileSize);

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Scaled Image Size: " + ScaledFileSize);

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Cropped Image Size: " + CroppedFileSize);

            // draw the compress & upload.
            if (_uiShared.IconTextButton(FontAwesomeIcon.Compress, "Compress"))
                CompressImage();
            UiSharedService.AttachToolTip("Shrinks the image to a 512x512 ratio for better performance");

            ImGui.SameLine();

            if (_uiShared.IconTextButton(FontAwesomeIcon.Upload, "Upload to Server", disabled: _croppedImageData is null))
                _ = UploadToServer(profile);
        }
    }

    private async Task UploadToServer(KinkPlate profile)
    {
        // grab the _croppedImageData and upload it to the server.
        if (_croppedImageData is null)
            return;

        // update the cropped image data to the 256x256 standard it should be.
        using (var image = Image.Load<Rgba32>(_uploadedImageData))
        {
            // Resize the zoomed area to 512x512
            var resizedImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(256, 256),
                Mode = ResizeMode.Max
            }));


            // Convert the processed image to byte array
            using (var ms = new MemoryStream())
            {
                resizedImage.SaveAsPng(ms);
                _compressedImageData = ms.ToArray();
                _logger.LogTrace("New Image File Size: " + _compressedImageData.Length / 1024.0 + " KB");
                _logger.LogDebug($"Sending Image to server with: {resizedImage.Width}x{resizedImage.Height} [Original: {image.Width}x{image.Height}]");
            }
        }
        try
        {
            await _apiHubMain.UserSetKinkPlatePicture(new(MainHub.PlayerUserData, Convert.ToBase64String(_croppedImageData))).ConfigureAwait(false);
            _logger.LogInformation("Image Sent to server successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send image to server.");
        }
    }

    private void InitializeZoomFactors(int width, int height)
    {
        var lesserDimension = Math.Min(width, height);
        _minZoomFactor = 1.0f;
        _maxZoomFactor = lesserDimension / 256.0f; // Ensure the minimum zoomed area is 256x256
    }

    public void CompressImage()
    {
        if (_uploadedImageData == null) return;

        using (var image = Image.Load<Rgba32>(_uploadedImageData))
        {
            // Calculate the lesser dimension of the original image
            var lesserDimension = Math.Min(image.Width, image.Height);

            // Calculate the cropping rectangle to make the image square
            var cropRectangle = new Rectangle(0, 0, lesserDimension, lesserDimension);
            cropRectangle.X = (image.Width - lesserDimension) / 2;
            cropRectangle.Y = (image.Height - lesserDimension) / 2;

            // Crop the image to the lesser dimension
            var croppedImage = image.Clone(ctx => ctx.Crop(cropRectangle));

            var desiredSize = new Size(512, 512);

            // Resize the zoomed area to 512x512
            var resizedImage = croppedImage.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = desiredSize,
                Mode = ResizeMode.Max
            }));


            // Convert the processed image to byte array
            using (var ms = new MemoryStream())
            {
                resizedImage.SaveAsPng(ms);
                _compressedImageData = ms.ToArray();

                // Load the cropped image for preview
                _compressedImageToShow = _cosmetics.GetProfilePicture(_compressedImageData);
                _useCompressedImage = true;
                // _logger.LogDebug($"New Image width and height is: {resizedImage.Width}x{resizedImage.Height} from {croppedImage.Width}x{croppedImage.Height}");
                CroppedFileSize = $"{_croppedImageData.Length / 1024.0:F2} KB";

                InitializeZoomFactors(resizedImage.Width, resizedImage.Height);
            }
        }
    }

    private void UpdateCroppedImagePreview()
    {
        // Ensure the image data is not 
        if (_uploadedImageData == null) return;


        using (var image = Image.Load<Rgba32>(_useCompressedImage ? _compressedImageData : _uploadedImageData))
        {
            var desiredSize = new Size(256, 256);
            // Calculate the lesser dimension of the original image
            var lesserDimension = Math.Min(image.Width, image.Height);

            // Calculate the size of the zoomed area based on the zoom factor
            var zoomedWidth = (int)(lesserDimension / _zoomFactor);
            var zoomedHeight = (int)(lesserDimension / _zoomFactor);

            // Ensure the zoomed area is at least 256x256
            zoomedWidth = Math.Max(zoomedWidth, 256);
            zoomedHeight = Math.Max(zoomedHeight, 256);

            // Ensure the zoomed area does not exceed the lesser dimension of the original image
            zoomedWidth = Math.Min(zoomedWidth, lesserDimension);
            zoomedHeight = Math.Min(zoomedHeight, lesserDimension);

            // Calculate the cropping rectangle based on the user's alignment selection
            var cropRectangle = new Rectangle(0, 0, zoomedWidth, zoomedHeight);
            cropRectangle.X = Math.Max(0, Math.Min((int)((image.Width - zoomedWidth) * _cropX), image.Width - zoomedWidth));
            cropRectangle.Y = Math.Max(0, Math.Min((int)((image.Height - zoomedHeight) * _cropY), image.Height - zoomedHeight));

            // Ensure the crop rectangle is within the image bounds
            cropRectangle.Width = Math.Min(cropRectangle.Width, image.Width - cropRectangle.X);
            cropRectangle.Height = Math.Min(cropRectangle.Height, image.Height - cropRectangle.Y);

            var zoomedImage = image.Clone(ctx => ctx.Crop(cropRectangle));

            // Resize the zoomed area to 256x256
            var croppedImage = zoomedImage.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = desiredSize,
                Mode = ResizeMode.Max
            }));


            // Convert the processed image to byte array
            using (var ms = new MemoryStream())
            {
                croppedImage.SaveAsPng(ms);
                _croppedImageData = ms.ToArray();

                // Load the cropped image for preview
                _croppedImageToShow = _cosmetics.GetProfilePicture(_croppedImageData);
                CroppedFileSize = $"{_croppedImageData.Length / 1024.0:F2} KB";
                // _logger.LogInformation($"Cropped image to {cropRectangle}");
            }
        }
    }

}
