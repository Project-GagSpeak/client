using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.UiRemote;
using GagSpeak.Interop;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.Gui.Toybox;

public class ToysPanel
{
    private readonly ILogger<ToysPanel> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly BuzzToyFileSelector _selector;
    private readonly BuzzToyManager _manager;
    private readonly IpcCallerIntiface _ipc;

    //private readonly GlobalPermissions _globals;
    //private readonly MainConfig _clientConfigs;
    //private readonly ServerConfigService _serverConfigs;
    private readonly TutorialService _guides;

    public ToysPanel(
        ILogger<ToysPanel> logger,
        GagspeakMediator mediator,
        BuzzToyFileSelector selector,
        BuzzToyManager manager,
        IpcCallerIntiface ipc,
        TutorialService guides)
    {
        _logger = logger;
        _mediator = mediator;
        _selector = selector;
        _manager = manager;
        _ipc = ipc;
        _guides = guides;

        // grab path to the intiface
        if (IntifaceCentral.AppPath == string.Empty)
            IntifaceCentral.GetApplicationPath();
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, ToyboxTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("BuzzToysTL", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("BuzzToysBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("BuzzToysTR", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        // Draw the selected Item
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawSelectedToyInfo(drawRegions.BotRight, curveSize);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));

        // Shift down and draw the Active items
        var verticalShift = new Vector2(0, ImGui.GetItemRectSize().Y + ImGui.GetStyle().WindowPadding.Y * 3);
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos + verticalShift);
        DrawActiveToys(drawRegions.BotRight.Size - verticalShift);
    }

    private void DrawSelectedToyInfo(CkHeader.DrawRegion drawRegion, float rounding)
    {
        var wdl = ImGui.GetWindowDrawList();
        var height = ImGui.GetTextLineHeightWithSpacing() * 6;
        var region = new Vector2(drawRegion.Size.X, height);
        var notSelected = _selector.Selected is null;
        var labelText = notSelected ? "No Item Selected!" : $"{_selector.Selected!.LabelName} ({_selector.Selected!.FactoryName})";
        var tooltipAct = notSelected ? "No item selected!" : "Double Click to begin editing!";

        using var c = CkRaii.ChildLabelButton(region, .6f, labelText, ImGui.GetFrameHeight(), BeginEdits, tooltipAct, ImDrawFlags.RoundCornersRight, LabelFlags.AddPaddingToHeight);

        var pos = ImGui.GetItemRectMin();
        // Draw the left items.
        if (_selector.Selected is not null)
            DrawSelectedInner();

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is ImGuiMouseButton.Left && _selector.Selected is VirtualBuzzToy vbt)
                _manager.StartEditing(vbt);
        }
    }

    private void DrawSelectedInner()
    {
        using var _ = ImRaii.Child("SelectedChildInner", ImGui.GetContentRegionAvail());

        if (_selector.Selected is not { } selected)
            return;

        if(selected is IntifaceBuzzToy ibt)
            ImGui.Text($"Intiface Idx: {ibt.DeviceIdx}");
        
        ImGui.Text("Factory (Default) Name: " + selected.FactoryName);
        ImGui.Text("Display Name: " + selected.LabelName);
        ImGui.Text("Battery Level: " + selected.BatteryLevel);
        ImGui.Text("Can Interact: " + selected.CanInteract);

        if (selected.CanVibrate)
        {
            ImGui.Text("Vibe Motors:");
            for (var i = 0; i < selected.VibeMotorCount; i++)
            {
                ImUtf8.SameLineInner();
                using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
                    ImUtf8.TextFrameAligned($" #{i} ");
                CkGui.AttachToolTip($"--COL--Step Count:--COL-- {selected.VibeMotors[i].StepCount}" +
                    $"--NL----COL--Interval:--COL-- {selected.VibeMotors[i].Interval}" +
                    $"--NL----COL--Current Intensity:--COL-- {selected.VibeMotors[i].Intensity}", color: ImGuiColors.ParsedGold);
            }
        }

        if (selected.CanRotate)
        {
            ImGui.Text("Rotate Motors:");
            for (var i = 0; i < selected.RotateMotorCount; i++)
            {
                ImUtf8.SameLineInner();
                using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
                    ImUtf8.TextFrameAligned($" #{i} ");
                CkGui.AttachToolTip(
                    $"--COL--Step Count:--COL-- {selected.RotateMotors[i].StepCount}" +
                    $"--COL--Interval:--COL-- {selected.RotateMotors[i].Interval}" +
                    $"--COL--Current Intensity:--COL-- {selected.RotateMotors[i].Intensity}", color: ImGuiColors.ParsedGold);
            }
        }

        if (selected.CanOscillate)
        {
            ImGui.Text("Oscillation Motors:");
            for (var i = 0; i < selected.OscillateMotorCount; i++)
            {
                ImUtf8.SameLineInner();
                using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
                    ImUtf8.TextFrameAligned($" #{i} ");
                CkGui.AttachToolTip(
                    $"--COL--Step Count:--COL-- {selected.OscillateMotors[i].StepCount}" +
                    $"--COL--Interval:--COL-- {selected.OscillateMotors[i].Interval}" +
                    $"--COL--Current Intensity:--COL-- {selected.OscillateMotors[i].Intensity}", color: ImGuiColors.ParsedGold);
            }
        }
    }

    private void DrawActiveToys(Vector2 region)
    {
        using var child = CkRaii.Child("ActiveToys", region, WFlags.NoScrollbar | WFlags.AlwaysUseWindowPadding);

        ImGui.Text("Active Toys Listed here and stuff yes yes.");

        if (CkGui.IconTextButton(FAI.TabletAlt, "Personal Remote", 125f))
        {
            // open the personal remote window
            _mediator.Publish(new UiToggleMessage(typeof(RemotePersonal)));
        }
        CkGui.HelpText("Open Personal Remote");

        // temp placeholder connection stuff.
        var windowPadding = ImGui.GetStyle().WindowPadding;
        // push the style var to supress the Y window padding.
        var intifaceOpenIcon = FAI.ArrowUpRightFromSquare;
        var intifaceIconSize = CkGui.IconButtonSize(intifaceOpenIcon);
        var connectedIcon = IpcCallerIntiface.IsConnected ? FAI.Link : FAI.Unlink;
        var buttonSize = CkGui.IconButtonSize(FAI.Link);
        var buttplugServerAddr = IpcCallerIntiface.ClientName;
        var addrSize = ImGui.CalcTextSize(buttplugServerAddr);

        var intifaceConnectionStr = "Intiface Central Connection";

        var addrTextSize = ImGui.CalcTextSize(intifaceConnectionStr);
        var totalHeight = ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;

        // create a table
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
        using (ImRaii.Table("IntifaceStatusUI", 3))
        {
            // define the column lengths.
            ImGui.TableSetupColumn("##openIntiface", ImGuiTableColumnFlags.WidthFixed, intifaceIconSize.X);
            ImGui.TableSetupColumn("##serverState", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##connectionButton", ImGuiTableColumnFlags.WidthFixed, buttonSize.X);

            // draw the add user button
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (totalHeight - intifaceIconSize.Y) / 2);
            if (CkGui.IconButton(intifaceOpenIcon, inPopup: true))
                IntifaceCentral.OpenIntiface(true);
            CkGui.AttachToolTip("Opens Intiface Central on your PC for connection.\nIf application is not detected, opens a link to installer.");

            // in the next column, draw the centered status.
            ImGui.TableNextColumn();

            if (IpcCallerIntiface.IsConnected)
            {
                // fancy math shit for clean display, adjust when moving things around
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth()) / 2 - (addrSize.X) / 2);
                ImGui.TextColored(ImGuiColors.ParsedGreen, buttplugServerAddr);
            }
            else
            {
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth()) / 2 - (ImGui.CalcTextSize("No Client Connection").X) / 2);
                ImGui.TextColored(ImGuiColors.DalamudRed, "No Client Connection");
            }

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth()) / 2 - addrTextSize.X / 2);
            ImGui.TextUnformatted(intifaceConnectionStr);

            // draw the connection link button
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (totalHeight - intifaceIconSize.Y) / 2);
            // we need to turn the button from the connected link to the disconnected link.
            using (ImRaii.PushColor(ImGuiCol.Text, CkGui.GetBoolColor(IpcCallerIntiface.IsConnected)))
            {
                if (CkGui.IconButton(connectedIcon, inPopup: true))
                {
                    if (IpcCallerIntiface.IsConnected)
                        _ipc.Disconnect().ConfigureAwait(false);
                    else
                        _ipc.Connect().ConfigureAwait(false);
                }
            }
            CkGui.AttachToolTip(IpcCallerIntiface.IsConnected ? "Disconnect from Intiface Central" : "Connect to Intiface Central");
        }
    }
}
