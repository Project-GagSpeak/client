using CkCommons;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui;

// do not make this factory generated you freaking silly girl
public class ThumbnailUI : WindowMediatorSubscriberBase
{
    private readonly ImageImportTool _imageImport;
    private readonly MainConfig _config;
    private readonly UiThumbnailService _service;
    private readonly TutorialService _guides;
    public ThumbnailUI(ILogger<ThumbnailUI> logger, GagspeakMediator mediator,
        ImageImportTool imageImport, MainConfig config, UiThumbnailService service,
        TutorialService guides) : base(logger, mediator, "##Thumbnail_Browser")
    {
        _imageImport = imageImport;
        _config = config;
        _service = service;
        _guides = guides;

        this.SetBoundaries(new Vector2(380, 500), new Vector2(1000, 2000));

        Mediator.Subscribe<ReScanThumbnailFolder>(this, _ => TryRefresh(true));
    }

    protected override void PreDrawInternal()
    { }

    protected override void PostDrawInternal()
    { }

    protected override void DrawInternal()
    {
        var drawSpaces = CkHeader.FlatWithBends(CkColor.FancyHeader.Uint(), ImGui.GetFrameHeight(), ImGui.GetFrameHeight());

        ImGui.SetCursorScreenPos(drawSpaces.Top.Pos);
        using (ImRaii.Child("Thumbnail_UI_Header", drawSpaces.Top.Size))
            // Let the header buttons know for import, how large the draw region is.
            DrawHeaderItems(drawSpaces.Bottom.Size);


        ImGui.SetCursorScreenPos(drawSpaces.Bottom.Pos);
        using (ImRaii.Child("Thumbnail_UI_Footer", drawSpaces.Bottom.Size))
        {
            if (_imageImport.ShouldDrawImportContent(_service.Kind))
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
        FancySearchBar.Draw("Filter", searchWidth, "Browse for a thumbnail to use.", ref _service.SearchString, 128, buttonW, () =>
        {
            if (CkGui.IconButton(FAI.Sync))
                TryRefresh(true);
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.UpdateContents, ImGui.GetWindowPos(), ImGui.GetWindowSize());
            _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.UpdateContents, ImGui.GetWindowPos(), ImGui.GetWindowSize());
        });

        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - clipboardImportW - fileImportW - ImGui.GetStyle().ItemInnerSpacing.X * 3);

        // Let the user control the size of the displayed icons.
        var size = _config.Current.FileIconScale;
        if (ImGui.SliderFloat("##icon_scaler", ref size, 0.5f, 2.0f, $"Scale: %.2fx"))
            _config.Current.FileIconScale = size;
        CkGui.AttachToolTip($"Scalar: {size}x (Size: {_service.ItemSize.X}px)");
        // Save changes only once we deactivate, to avoid spamming the hybrid saver.
        if (ImGui.IsItemDeactivatedAfterEdit())
            _config.Save();
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.DisplayScale, ImGui.GetWindowPos(), ImGui.GetWindowSize());
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.DisplayScale, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        // Let them use File Dialog Manager to import images.
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.FileImport, "From File"))
            _imageImport.ImportFromFile(_service.Kind, _service.DispSize, botRegionSize, KeyMonitor.ShiftPressed());
        CkGui.AttachToolTip("Add a Thumbnail Image from the file browser."
            + "--SEP-- Holding SHIFT will force the image to be re-imported.");
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ImportByFile, ImGui.GetWindowPos(), ImGui.GetWindowSize());
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.ImportingByFile, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        // Add a option to add new images by clipboard pasting.
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Clipboard, "From Clipboard"))
            _imageImport.ImportFromClipboard(_service.Kind, _service.DispSize, botRegionSize, KeyMonitor.ShiftPressed());
        CkGui.AttachToolTip("Add a Thumbnail Image from the contents copied to clipboard."
            + "--SEP-- Holding SHIFT will force the image to be re-imported.");
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ImportByClipboard, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => { });
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.ImportingByClipboard, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => { });
    }

    public void DrawFileImporter()
    {
        using (ImRaii.Child("File_Import", ImGui.GetContentRegionAvail()))
            _imageImport.DrawImportContent();
    }

    private void DrawFileBrowser()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10);
        using (ImRaii.Child("Folder_Entries", ImGui.GetContentRegionAvail(), false, WFlags.NoScrollbar | WFlags.AlwaysUseWindowPadding))
            DrawAllEntries();
    }


    private void DrawAllEntries()
    {
        if (_service.Folder is null)
        {
            CkGui.ColorTextCentered("No Folder Selected", ImGuiColors.DalamudRed);
            return;
        }

        var region = ImGui.GetContentRegionAvail();
        var style = ImGui.GetStyle();

        var spacing = style.ItemSpacing.X;

        // Display spinner if fetching files.
        if (_service.Folder.IsScanning)
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
        if (_service.Folder.AllFiles.IsNullOrEmpty())
        {
            using (UiFontService.GagspeakTitleFont.Push())
                ImGuiUtil.Center("No Thumbnails In Folder");
            return;
        }

        // get item sizes to draw out. (account for case where images are bigger than UI, to make sure 2 per row are shown)
        var imgRatioToUI = Math.Min(region.X / _service.ItemSize.X, region.Y / _service.ItemSize.Y);
        var imgFits = imgRatioToUI >= 1;
        var imgSpace = imgFits ? _service.ItemSize : ((_service.ItemSize * imgRatioToUI) - style.ItemSpacing) / 2;
        var size = imgSpace + new Vector2(0, ImGui.GetTextLineHeight() * 3);
        var rounding = (imgSpace.X * .1f) < ImGui.GetFrameHeight() ? imgSpace.X * .1f : ImGui.GetFrameHeight();

        var columnCount = (int)Math.Floor((region.X + spacing) / (imgSpace.X + spacing));
        var column = 0;
        var index = 0;

        foreach (var entry in _service.Folder.AllFiles)
        {
            DrawThumbnailEntry(entry, index);
            index++;

            if (_service.Folder.IsScanning || _service.Folder is null)
                break;

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

        void DrawThumbnailEntry(ThumbnailFile entry, int id)
        {
            using var _ = ImRaii.Group();
            var isSelected = entry.Equals(_service.Selected);
            uint bgColor = 0;

            using (ImRaii.Child($"##ThumbnailEntry_{id}", size, false, WFlags.NoScrollbar))
            {
                ImGui.InvisibleButton($"##library_entry_{id}", size);
                var rectMin = ImGui.GetItemRectMin();
                var rectMax = ImGui.GetItemRectMax();
                var hovered = ImGui.IsItemHovered();
                bgColor = hovered && ImGui.IsMouseDown(ImGuiMouseButton.Left)
                    ? ImGui.GetColorU32(ImGuiCol.HeaderActive) : hovered
                    ? ImGui.GetColorU32(ImGuiCol.HeaderHovered) : isSelected
                    ? ImGui.GetColorU32(ImGuiCol.Header) : 0;
                var wdl = ImGui.GetWindowDrawList();

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    _service.Selected = entry;
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        _service.PickSelectedThumbnail();
                        return;
                    }
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    ImGui.OpenPopup($"##Rename_{entry.FileName}");

                // Draw out the background for this space.
                ImGui.GetWindowDrawList().AddDalamudImageRounded(entry.Icon, rectMin, imgSpace, rounding);
                ImGui.SetCursorScreenPos(rectMin + new Vector2(0, imgSpace.Y));

                if(!imgFits)
                    using (UiFontService.GagspeakLabelFont.Push())
                        ImGuiUtil.TextWrapped(entry.FileNameNoExtension);
                else
                    ImGuiUtil.TextWrapped(entry.FileNameNoExtension);

                // Summon popup context.
                RenamePopupContext(entry);
            }
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            // Add the rect around the BG.
            var clipRectMin = min - style.FramePadding / 2;
            var clipRectMax = max + style.FramePadding / 2;
            ImGui.GetWindowDrawList().AddRectFilled(clipRectMin, clipRectMax, bgColor, rounding);
        }
    }

    private void RenamePopupContext(ThumbnailFile file)
    {
        using var _ = ImRaii.Popup($"##Rename_{file.FileName}");
        if (!_)
            return;

        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);

        if(ImGui.Selectable("Delete Thumbnail"))
        {
            _service.Folder.Remove(file);
            TryRefresh(true);
            ImGui.CloseCurrentPopup();
        }

        ImGui.Separator();

        var currentName = file.FileNameNoExtension;
        ImGui.TextUnformatted("Rename File:");
        if (ImGui.InputText("##Rename", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            // Rename the file at the directory location, then close the popup and refresh.
            _logger.LogInformation($"Renaming file {file.FileName} to {currentName}.");
            if(file.TryRename(currentName))
                TryRefresh(true);
            
            ImGui.CloseCurrentPopup();
        }
        CkGui.AttachToolTip("Enter a new name for this file.--SEP--The .png is added for you.");
    }

    private void TryRefresh(bool force)
    {
        if (_service.Folder.IsScanning)
            return;

        _logger.LogInformation("Refreshing thumbnail files.");
        _service.ScanFolderFiles();
    }

    public override void OnClose()
        => _service.ClearThumbnailSource();
}
