using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.Remote;
using GagSpeak.Interop;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.Gui.Toybox;

public class ToysPanel
{
    private readonly ILogger<ToysPanel> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly BuzzToyFileSelector _selector;
    private readonly IpcCallerIntiface _ipc;
    private readonly BuzzToyManager _manager;
    private readonly RemoteService _service;
    private readonly TutorialService _guides;

    public ToysPanel(
        ILogger<ToysPanel> logger,
        GagspeakMediator mediator,
        BuzzToyFileSelector selector,
        IpcCallerIntiface ipc,
        BuzzToyManager manager,
        RemoteService service,
        TutorialService guides)
    {
        _logger = logger;
        _mediator = mediator;
        _selector = selector;
        _ipc = ipc;
        _manager = manager;
        _service = service;
        _guides = guides;

        // grab path to the intiface
        if (IntifaceCentral.AppPath == string.Empty)
            IntifaceCentral.GetApplicationPath();
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, ToyboxTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("BuzzToysTL", drawRegions.TopLeft.Size))
            DrawConnectionState(drawRegions.TopLeft);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("BuzzToysBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("BuzzToysTR", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawSelectedToyInfo(drawRegions.BotRight, curveSize);
    }

    private void DrawConnectionState(CkHeader.DrawRegion drawRegion)
    {
        using var _ = ImRaii.Group();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 10f);
        using var color = ImRaii.PushColor(ImGuiCol.Button, CkColor.FancyHeaderContrast.Uint());

        var spacing = ImGui.GetStyle().ItemSpacing;
        var sideButtonLengths = CkGui.IconButtonSize(FAI.ArrowUpRightFromSquare).X + spacing.X;
        var centerChildWidth = drawRegion.SizeX - sideButtonLengths * 2;

        // Draw the leftmost button, the personal remote.
        if (CkGui.IconButton(FAI.TabletAlt))
            _mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI)));
        CkGui.AttachToolTip("Open Personal Remote");

        // Draw the center child with the connection state.
        ImGui.SameLine();
        using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint(), 45f))
        {
            // Draw the intiface opener button.
            if (CkGui.IconButton(FAI.ArrowUpRightFromSquare, inPopup: true))
                IntifaceCentral.OpenIntiface(true);
            CkGui.AttachToolTip("Opens Intiface Central on your PC for connection.\nIf application is not detected, opens a link to installer.");

            // Now we need to draw our connection state, centered to the remaining width.
            ImGui.SameLine();
            using (ImRaii.Child("ConnectionStateChild", new Vector2(centerChildWidth - sideButtonLengths * 2, drawRegion.SizeY)))
            {
                CkGui.CenterColorTextAligned(
                    IpcCallerIntiface.IsConnected ? "Connected To Intiface" : "Intiface Not Connected",
                    CkGui.GetBoolColor(IpcCallerIntiface.IsConnected));
            }

            ImGui.SameLine();
            if (CkGui.IconButton(IpcCallerIntiface.IsConnected ? FAI.Link : FAI.Unlink, inPopup: true))
            {
                if (IpcCallerIntiface.IsConnected)
                    UiService.SetUITask(_ipc.Disconnect);
                else
                    UiService.SetUITask(_ipc.Connect);
            }
            CkGui.AttachToolTip(IpcCallerIntiface.IsConnected ? "Disconnect from Intiface Central" : "Connect to Intiface Central");
        }

        // Draw the rightmost button, the Intiface Central link.
        ImGui.SameLine();
        if (CkGui.IconButton(FAI.Plus))
            ImGui.OpenPopup("##NewSexToy");
        CkGui.AttachToolTip("Create a new SexToy.");

        _selector.DrawPopups();
    }

    private void DrawSelectedToyInfo(CkHeader.DrawRegion region, float curveSize)
    {
        DrawSelectedDisplay(region.Size);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawSelectedDisplay(Vector2 region)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        var item = _selector.Selected;
        var editorItem = _manager.ItemInEditor;
        var editingAnyDevice = editorItem is not null;
        var editingSelectedDevice = item != null && editingAnyDevice && item.Id.Equals(editorItem!.Id);

        var tooltip = (item is null) ? "No item selected!" : $"Double Click to {(editingSelectedDevice ? "Edit" : "Save ")} this Device.--SEP--Right Click to cancel and exit Editor.";

        using (var c = CkRaii.ChildLabelCustomButton("##PatternSel", region, ImGui.GetFrameHeight(), DrawLabel, BeginEdits, tooltip, DFlags.RoundCornersRight, LabelFlags.SizeIncludesHeader))
        {
            if (item is null)
                return;

            // Draw the image preview for the selected item if valid.
            if (item.FactoryName is not ToyBrandName.Unknown)
            {
                var availWidth = (c.InnerNoLabel.X - ImGui.GetItemRectSize().X);
                var imgSize = new Vector2(availWidth - ImGui.GetFrameHeight());
                var drawPos = ImGui.GetItemRectMax() + new Vector2((availWidth - imgSize.X) /2, -ImGui.GetFrameHeight() / 2);
                ImGui.GetWindowDrawList().AddDalamudImage(CosmeticService.IntifaceTextures.Cache[item.FactoryName.FromBrandName()], drawPos, imgSize);
                if (item is IntifaceBuzzToy ibt)
                    CkGui.AttachToolTipRect(drawPos, drawPos + imgSize, $"Intiface DeviceIdx: {ibt.DeviceIdx}");
            }

            if (editingSelectedDevice)
                DrawSelectedInner(editorItem!, true);
            else
                DrawSelectedInner(item, false);
        }

        void DrawLabel()
        {
            using var c = CkRaii.Child("##DeviceSelLabel", new Vector2(region.X * .7f, ImGui.GetFrameHeight()));
            ImGui.Spacing();
            var label = item is null ? "No Item Selected!" : item.LabelName;
            CkGui.TextFrameAlignedInline(label);

            ImGui.SameLine(c.InnerRegion.X - ImGui.GetFrameHeight() * 1.5f);
            CkGui.FramedIconText(editingSelectedDevice ? FAI.Save : FAI.Edit);
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is not ImGuiMouseButton.Left || item is null)
                return;

            // discard if we are editing an item but it's not the one we are on.
            if (editingAnyDevice && !editingSelectedDevice)
                return;

            if (editingSelectedDevice)
                _manager.SaveChangesAndStopEditing();
            else if (item is VirtualBuzzToy vbt)
                _manager.StartEditing(vbt);
        }
    }

    private void DrawSelectedInner(BuzzToy device, bool isEditorItem)
    {
        using var col = ImRaii.PushColor(ImGuiCol.FrameBg, CkColor.FancyHeaderContrast.Uint());

        using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
        {
            CkGui.BooleanToColoredIcon(_selector.Selected!.Interactable, false);
            CkGui.TextFrameAlignedInline($"Interactable  ");
        }
        if (ImGui.IsItemHovered() && ImGui.IsItemClicked())
            _manager.ToggleInteractableState(_selector.Selected);
        
        ImGui.Spacing();
        DrawFactoryName(device, isEditorItem);

        CkGui.Separator();
        DrawBatteryHealth(device);

        CkGui.Separator();
        DrawAssociatedMotors(device, isEditorItem);

        DrawFooter(device);
    }

    private void DrawFactoryName(BuzzToy device, bool isEditing)
    {
        using var _ = ImRaii.Group();

        CkGui.ColorText("Brand Name", ImGuiColors.ParsedGold);
        var comboW = ImGui.GetContentRegionAvail().X * .6f;
        var childSize = new Vector2(comboW, ImGui.GetFrameHeight());
        if(isEditing && device is VirtualBuzzToy vbt)
        {
            if (CkGuiUtils.EnumCombo("##NameSelector", comboW, vbt.FactoryName, out var newVal, _ => _.ToName(), "Choose Brand Name..", flags: CFlags.None))
            {
                // pass in spatial audio to correctly aquire and play the audio items.
                vbt.SetFactoryName(newVal);
                vbt.LabelName = newVal.ToName();
            }
        }
        else
        {
            using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
            {
                ImGui.Dummy(childSize);
                ImGui.SetCursorScreenPos(ImGui.GetItemRectMin());
                CkGui.InlineSpacingInner();
                ImUtf8.TextFrameAligned(device.FactoryName.ToName());
            }
        }
    }

    private void DrawBatteryHealth(BuzzToy device)
    {
        using var _ = ImRaii.Group();
        CkGui.ColorText("Battery Health", ImGuiColors.ParsedGold);
        var batteryLevel = Math.Clamp(device.BatteryLevel, 0, 1);

        var region = ImGui.GetContentRegionAvail();
        var drawList = ImGui.GetWindowDrawList();
        ImGui.Dummy(new Vector2(region.X, ImGui.GetTextLineHeight()));
        var label = $"{batteryLevel * 100:F0}%";
        var labelSize = ImGui.CalcTextSize(label);
        var barHeight = (int)labelSize.Y + 2;
        var barWidth = (int)(region.X - ImGui.GetStyle().FramePadding.X);
        var start = ImGui.GetItemRectMin();
        var end = ImGui.GetItemRectMax();

        // Outer Border
        drawList.AddRectFilled(start - Vector2.One, end + Vector2.One, CkGui.Color(0, 0, 0, 100), 25f, ImDrawFlags.RoundCornersAll);
        // Inner Border
        drawList.AddRectFilled(start - Vector2.One, end + Vector2.One, CkGui.Color(220, 220, 220, 100), 25f, ImDrawFlags.RoundCornersAll);
        // Background
        drawList.AddRectFilled(start, end, CkGui.Color(0, 0, 0, 100), 25f, ImDrawFlags.RoundCornersAll);
        // Fill (skip if negligible)
        if (batteryLevel >= 0.025)
        {
            var fillEnd = start + new Vector2((float)(batteryLevel * barWidth), 0);
            drawList.AddRectFilled(start, new Vector2(fillEnd.X, end.Y), CkGui.Color(225, 104, 168, 255), 45f, ImDrawFlags.RoundCornersAll);
        }
        // Centered text
        var textPos = start + new Vector2((barWidth - labelSize.X) / 2f - 1, (barHeight - labelSize.Y) / 2f - 1);
        drawList.OutlinedFont(label, textPos, CkGui.Color(255, 255, 255, 255), CkGui.Color(53, 24, 39, 255), 1);
    }

    private void DrawAssociatedMotors(BuzzToy device, bool isEditing)
    {
        var wdl = ImGui.GetWindowDrawList();
        var imgCache = CosmeticService.IntifaceTextures.Cache;
        var imgSize = new Vector2(ImGui.GetFrameHeight());
        foreach (var (type, motors) in device.MotorTypeMap)
        {
            CkGui.ColorText($"{type} Motors", ImGuiColors.ParsedGold);
            foreach (var motor in motors)
            {
                using (ImRaii.Group())
                {
                    ImGui.Dummy(imgSize);
                    wdl.AddDalamudImage(imgCache[CoreIntifaceElement.MotorVibration], ImGui.GetItemRectMin(), imgSize);

                    ImUtf8.SameLineInner();
                    using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
                    {
                        CkGui.InlineSpacingInner();
                        CkGui.TextFrameAlignedInline("Steps:");
                        CkGui.ColorTextFrameAlignedInline($"{motor.StepCount}  ", CkGui.Color(ImGuiColors.ParsedGold));
                    }
                    ImUtf8.SameLineInner();
                    using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
                    {
                        CkGui.InlineSpacingInner();
                        CkGui.TextFrameAlignedInline("Interval:");
                        CkGui.ColorTextFrameAlignedInline($"{motor.Interval}  ", CkGui.Color(ImGuiColors.ParsedGold));
                    }
                    ImUtf8.SameLineInner();
                    using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
                    {
                        CkGui.InlineSpacingInner();
                        CkGui.TextFrameAlignedInline("Intensity:");
                        CkGui.ColorTextFrameAlignedInline($"{motor.Intensity.ToString("F2")}  ", CkGui.Color(ImGuiColors.ParsedGold));
                    }
                }
            }
        }
    }

    private void DrawFooter(BuzzToy device)
    {
        using var _ = ImRaii.Group();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 10f);
        using var color = ImRaii.PushColor(ImGuiCol.Button, CkColor.FancyHeaderContrast.Uint());

        var regionLeftover = ImGui.GetContentRegionAvail().Y;
        // Determine how to space the footer.
        if (regionLeftover < (CkGui.GetSeparatorHeight() + ImGui.GetFrameHeight()))
            CkGui.Separator();
        else
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + regionLeftover - ImGui.GetFrameHeight());

        // Draw it.
        using (ImRaii.Group())
        {
            // Draw the leftmost button, the personal remote.
            if (CkGui.IconButton(FAI.SatelliteDish))
                UiService.SetUITask(_ipc.DeviceScannerTask());
            CkGui.AttachToolTip("Perform 2.5s Scan for Devices");

            CkGui.TextFrameAlignedInline("ID:");
            CkGui.TextFrameAlignedInline(device.Id.ToString());
        }
    }
}
