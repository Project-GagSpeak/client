using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Gui;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Components;
using GagSpeak.Utils;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui;
using OtterGui.Text;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace GagSpeak.UI;

public class ThumbnailUI : WindowMediatorSubscriberBase
{
    private readonly ImageImportTool _imageImport;
    private readonly GagspeakConfigService _config;
    public ThumbnailUI(ILogger<ThumbnailUI> logger, GagspeakMediator mediator,
        ImageImportTool imageImport, GagspeakConfigService config, 
        CosmeticService cosmetics, ImageDataType type) : base(logger, mediator, $"Thumbnails##Thumbnail_Browser_{type}")
    {
        _imageImport = imageImport;
        _config = config;

        IsOpen = true;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(380, 500),
            MaximumSize = new Vector2(1000, 2000),
        };

        Folder = new ThumbnailFolder(cosmetics, type);
        FolderName = type;
    }

    public ThumbnailFolder Folder { get; init; }
    public ImageDataType FolderName { get; init; }
    private string _searchString = string.Empty;
    private ThumbnailFile? _toOpen = null;
    private ThumbnailFile? _selected = null;

    private const float IconWidthBase = 120;
    private Vector2 IconSizeBase => new Vector2((IconWidthBase * 2), (IconWidthBase * 2) * ItemHeightScaler);

    private float ItemWidth => IconWidthBase * _config.Config.FileIconScale;
    private float ItemHeightScaler => FolderName is ImageDataType.Restrictions ? 1f : 1.25f;
    private float ItemHeightFooter => ImGui.GetTextLineHeightWithSpacing() * 2;

    protected override void PreDrawInternal() { }

    protected override void PostDrawInternal() { }

    protected override void DrawInternal()
    {
        var drawSpaces = DrawerHelpers.FlatHeaderWithCurve(CkColor.FancyHeader.Uint(), ImGui.GetFrameHeight(), ImGui.GetFrameHeight());

        ImGui.SetCursorScreenPos(drawSpaces.Top.Pos);
        using (ImRaii.Child("Thumbnail_UI_Header", drawSpaces.Top.Size))
            // Let the header buttons know for import, how large the draw region is.
            DrawHeaderItems(drawSpaces.Bottom.Size);


        ImGui.SetCursorScreenPos(drawSpaces.Bottom.Pos);
        using (ImRaii.Child("Thumbnail_UI_Footer", drawSpaces.Bottom.Size))
        {
            if (_imageImport.ShouldDrawImportContent(FolderName))
                DrawFileImporter();
            else
                DrawFileBrowser();
        }
    }

    private void DrawHeaderItems(Vector2 botRegionSize)
    {
        var buttonW = CkGui.IconButtonSize(FAI.GroupArrowsRotate).X;
        var clipboardImportW = CkGui.IconTextButtonSize(FAI.Clipboard, "From Clipboard");
        var fileImportW = CkGui.IconTextButtonSize(FAI.FileImport, "From File");

        var searchWidth = ImGui.GetContentRegionAvail().X / 3;
        DrawerHelpers.FancySearchFilter("Filter", searchWidth, "Browse for a thumbnail to use.", ref _searchString, 128, buttonW, () =>
        {
            if (CkGui.IconButton(FAI.Sync))
                TryRefresh(true);
        });

        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - clipboardImportW - fileImportW - ImGui.GetStyle().ItemInnerSpacing.X * 3);

        // Let the user control the size of the displayed icons.
        var size = _config.Config.FileIconScale;
        if (ImGui.SliderFloat("##icon_scaler", ref size, 0.5f, 2.0f, $"Scale: %.2fx"))
            _config.Config.FileIconScale = size;
        CkGui.AttachToolTip($"Scalar: {size}x (Size: {ItemWidth}px)");
        // Save changes only once we deactivate, to avoid spamming the hybrid saver.
        if (ImGui.IsItemDeactivatedAfterEdit())
            _config.Save();

        // Let them use File Dialog Manager to import images.
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.FileImport, "From File"))
            _imageImport.ImportFromFile(FolderName, IconSizeBase, botRegionSize, KeyMonitor.ShiftPressed());
        CkGui.AttachToolTip("Add a Thumbnail Image from the file browser."
            + "--SEP-- Holding SHIFT will force the image to be re-imported.");

        // Add a option to add new images by clipboard pasting.
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Clipboard, "From Clipboard"))
            _imageImport.ImportFromClipboard(FolderName, IconSizeBase, botRegionSize, KeyMonitor.ShiftPressed());
        CkGui.AttachToolTip("Add a Thumbnail Image from the contents copied to clipboard."
        + "--SEP-- Holding SHIFT will force the image to be re-imported.");
    }

    public void DrawFileImporter()
    {
        using (ImRaii.Child("File_Import", ImGui.GetContentRegionAvail()))
            _imageImport.DrawImportContent();
    }

    private void DrawFileBrowser()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10);
        using (ImRaii.Child("Folder_Entries", ImGui.GetContentRegionAvail(), false, WFlags.NoScrollbar))
            DrawAllEntries();
    }


    private void DrawAllEntries()
    {
        var region = ImGui.GetContentRegionAvail();

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var columnCount = (int)Math.Floor((region.X + spacing) / (ItemWidth + spacing));
        var column = 0;
        var index = 0;

        // Display spinner if fetching files.
        if(Folder.IsScanning)
        {
            var radius = 100 * ImUtf8.GlobalScale;
            var thickness = (int)(20 * ImUtf8.GlobalScale);
            var offsetX = region.X / 2 - radius;
            var offsetY = region.Y / 2 - radius;
            ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(offsetX, offsetY));
            ImUtf8.Spinner("##spinner"u8, radius, thickness, ImGui.GetColorU32(ImGuiCol.Text));
            return;
        }

        // Otherwise, scan for them if our current entries are null.
        if(Folder.AllFiles.IsNullOrEmpty())
        {
            TryRefresh(true);
            return;
        }

        // Assuming both are passed, draw out the remaining entries.
        foreach (var entry in Folder.AllFiles)
        {
            DrawThumbnailEntry(entry, index);
            index++;

            column++;
            if (column >= columnCount)
            {
                column = 0;
                ImGui.Spacing();
            }
            else
            {
                ImGui.SameLine();
            }
        }

        // If we have a selected entry, open it.
        if (_toOpen is not null)
            SelectAndClose();
    }

    /// <summary> Displays an individual Thumbnail Icon, along with its name. </summary>
    /// <remarks> Double clicking an icon will select it, and close the window. </remarks>
    private void DrawThumbnailEntry(ThumbnailFile entry, int id)
    {
        var size = new Vector2(ItemWidth, (ItemWidth * ItemHeightScaler) + ItemHeightFooter);
        var pos = ImGui.GetCursorScreenPos();

        var selected = _selected == entry;
        if (ImGui.Selectable($"##library_entry_{id}", ref selected, ImGuiSelectableFlags.AllowDoubleClick, size))
        {
            _selected = entry;

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _toOpen = entry;
                _logger.LogInformation($"Opening thumbnail {entry.FileName} in editor.");
            }
        }

        // go back to the start then draw the contents itself.
        ImGui.SetCursorScreenPos(pos);
        using var _ = ImRaii.Child($"library_entry_{id}", size, false, WFlags.NoInputs | WFlags.NoScrollbar);
        var imgSpace = new Vector2(ItemWidth, ItemWidth * ItemHeightScaler);

        if (entry.Icon is { } img)
            ImGui.GetWindowDrawList().AddDalamudImageRounded(img, pos, imgSpace, 10);

        ImGui.Dummy(imgSpace);
        ImGuiUtil.TextWrapped(entry.FileName);

    }

    private void SelectAndClose()
    {
        _logger.LogInformation($"Selecting Thumbnail file: {_toOpen?.FileName} and closing window.");
        // Fire some mediator event here?
        this.IsOpen = false;
    }

    private void TryRefresh(bool force)
    {
        if (Folder.IsScanning)
            return;

        _logger.LogInformation("Refreshing thumbnail files.");
        Folder.ScanFiles();
    }

    public override void OnClose()
    {
        Folder.Dispose();
        Mediator.Publish(new RemoveWindowMessage(this));
    }
}
