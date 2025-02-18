using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Numerics;

namespace GagSpeak.UI.Components;

public class OrderTabs : IconTabBarBase<OrderTabs.SelectedTab>
{
    public enum SelectedTab
    {
        ActiveOrders,
        OrderCreator,
        OrderMonitor
    }

    public OrderTabs(UiSharedService uiSharedService) : base(uiSharedService)
    {
        AddDrawButton(FontAwesomeIcon.CommentDots, SelectedTab.ActiveOrders, "Your Orders");
        AddDrawButton(FontAwesomeIcon.CommentDots, SelectedTab.OrderCreator, "Create Order");
        AddDrawButton(FontAwesomeIcon.CommentDots, SelectedTab.OrderMonitor, "Assign Order");
    }

    public override void Draw(float availableWidth)
    {
        if (_tabButtons.Count == 0)
            return;

        var spacing = ImGui.GetStyle().ItemSpacing;
        var buttonX = (availableWidth - (spacing.X * (_tabButtons.Count - 1))) / _tabButtons.Count;
        var buttonY = UiSharedService.GetIconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);
        var drawList = ImGui.GetWindowDrawList();
        var btncolor = ImRaii.PushColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0)));

        ImGuiHelpers.ScaledDummy(spacing.Y / 2f);

        foreach (var tab in _tabButtons)
            DrawTabButton(tab, buttonSize, spacing, drawList);

        // advance to the new line and dispose of the button color.
        ImGui.NewLine();
        btncolor.Dispose();

        ImGuiHelpers.ScaledDummy(3f);
        ImGui.Separator();
    }

}
