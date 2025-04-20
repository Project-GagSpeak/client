using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Gui;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Components;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui;
using OtterGui.Text;

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
        var headerH = ImGui.GetFrameHeight();
        // Draw out the header on top.
        var drawRegions = DrawerHelpers.FlatHeaderWithCurve(CkColor.FancyHeader.Uint(), headerH, headerH);

        // Draw the header items.
        ImGui.SetCursorScreenPos(drawRegions.Top.Pos);
        using (ImRaii.Child("Thumbnail_UI_Header", drawRegions.Top.Size))
            DrawHeaderItems(drawRegions.Bottom.Size);

        // Draw the file browser on the bottom.
        ImGui.SetCursorScreenPos(drawRegions.Bottom.Pos);
        using (ImRaii.Child("Thumbnail_UI_Footer", drawRegions.Bottom.Size))
        {
            if (_imageImport.ImportedImage is null)
                DrawFileBrowser();
            else
                DrawFileImporter();
        }
    }

    private void DrawHeaderItems(Vector2 botRegionSize)
    {
        var buttonW = CkGui.IconButtonSize(FAI.GroupArrowsRotate).X;
        var clipboardImportW = CkGui.IconTextButtonSize(FAI.Clipboard, "From Clipboard");
        var fileImportW = CkGui.IconTextButtonSize(FAI.FileImport, "From File");
        var tooltip = "Browse for a thumbnail to use.";

        var searchWidth = ImGui.GetContentRegionAvail().X/3;
        DrawerHelpers.FancySearchFilter("Filter", searchWidth, tooltip, ref _searchString, 128, buttonW, RefreshButton);
        ImGui.SameLine();

        var remainingWidth = ImGui.GetContentRegionAvail().X;
        var size = _config.Config.FileIconScale;

        ImGui.SetNextItemWidth(remainingWidth - clipboardImportW - fileImportW - ImGui.GetStyle().ItemInnerSpacing.X * 3);
        if (ImGui.SliderFloat("##icon_scaler", ref size, 0.5f, 2.0f, $"Scale: %.2fx"))
            _config.Config.FileIconScale = size;
        if (ImGui.IsItemDeactivatedAfterEdit())
            _config.Save();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Scalar: {size}x (Size: {ItemWidth}px)");

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.FileImport, "From File"))
            _imageImport.ImportFromFile(FolderName, IconSizeBase, botRegionSize);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Clipboard, "From Clipboard"))
            _imageImport.ImportFromClipboard(FolderName, IconSizeBase, botRegionSize);

        // For refreshing files.
        void RefreshButton()
        {
            if (CkGui.IconButton(FAI.Sync))
                TryRefresh(true);
        }
    }

    private void DrawFileBrowser()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10);
        using (ImRaii.Child("Folder_Entries", ImGui.GetContentRegionAvail(), false, WFlags.NoScrollbar))
            DrawAllEntries();
    }

    public void DrawFileImporter()
    {
        using (ImRaii.Child("File_Import", ImGui.GetContentRegionAvail()))
            _imageImport.DrawImportContent();
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
        if (entry.Icon is not null)
        {
            ImGui.GetWindowDrawList().AddDalamudImageRounded(entry.Icon, pos, imgSpace, 10);
        }
        ImGui.Dummy(imgSpace);
        ImGuiUtil.TextWrapped(entry.FileName);

    }

    private void SelectAndClose()
    {
        _logger.LogInformation($"Selecting Thumbnail file: {_toOpen?.FileName} and closing window.");
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
