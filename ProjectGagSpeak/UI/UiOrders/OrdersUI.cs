using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Components;
using GagSpeak.UI.UiToybox;
using GagSpeak.Utils;
using GagspeakAPI.Data.IPC;
using ImGuiNET;
using System.Numerics;

namespace GagSpeak.UI.UiOrders;

public class OrdersUI : WindowMediatorSubscriberBase
{
    private readonly OrdersTabMenu _tabMenu;
    private readonly OrdersViewActive _activePanel;
    private readonly OrdersCreator _creatorPanel;
    private readonly OrdersAssigner _assignerPanel;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;

    public OrdersUI(ILogger<OrdersUI> logger, GagspeakMediator mediator, 
        OrdersViewActive activePanel, OrdersCreator creatorPanel, 
        OrdersAssigner assignerPanel, CosmeticService cosmetics,
        UiSharedService uiShared) : base(logger, mediator, "Orders UI")
    {
        _activePanel = activePanel;
        _creatorPanel = creatorPanel;
        _assignerPanel = assignerPanel;
        _cosmetics = cosmetics;
        _uiShared = uiShared;

        _tabMenu = new OrdersTabMenu(_uiShared);
        // define initial size of window and to not respect the close hotkey.
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(550, float.MaxValue)
        };
        RespectCloseHotkey = false;
    }
    // perhaps migrate the opened selectable for the UIShared service so that other trackers can determine if they should refresh / update it or not.
    // (this is not yet implemented, but we can modify it later when we need to adapt)

    private bool ThemePushed = false;
    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));

            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        // get information about the window region, its item spacing, and the topleftside height.
        var region = ImGui.GetContentRegionAvail();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var topLeftSideHeight = region.Y;

        // create the draw-table for the selectable and viewport displays
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f * _uiShared.GetFontScalerFloat(), 0));
        try
        {
            using (var table = ImRaii.Table($"OrdersUiWindowTable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV))
            {
                if (!table) return;
                // setup the columns for the table
                ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, 200f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextColumn();

                var regionSize = ImGui.GetContentRegionAvail();
                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));

                using (var leftChild = ImRaii.Child($"###OrdersLeft", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
                {
                    // attempt to obtain an image wrap for it
                    var iconTexture = _cosmetics.CorePluginTextures[CorePluginTexture.Logo256];
                    if (!(iconTexture is { } wrap))
                    {
                        /*_logger.LogWarning("Failed to render image!");*/
                    }
                    else
                    {
                        // aligns the image in the center like we want.
                        UtilsExtensions.ImGuiLineCentered("###OrdersLogo", () =>
                        {
                            ImGui.Image(wrap.ImGuiHandle, new(125f * _uiShared.GetFontScalerFloat(), 125f * _uiShared.GetFontScalerFloat()));
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text($"What's this? A tooltip hidden in plain sight?");
                                ImGui.EndTooltip();
                            }
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                UnlocksEventManager.AchievementEvent(UnlocksEvent.EasterEggFound, "Orders");
                        });
                    }
                    // add separator
                    ImGui.Spacing();
                    ImGui.Separator();
                    // add the tab menu for the left side.
                    _tabMenu.DrawSelectableTabMenu();
                }
                // pop pushed style variables and draw next column.
                ImGui.PopStyleVar();
                ImGui.TableNextColumn();
                // display right half viewport based on the tab selection
                using (var rightChild = ImRaii.Child($"###OrdersRightSide", Vector2.Zero, false))
                {
                    switch (_tabMenu.SelectedTab)
                    {
                        case OrdersTabs.Tabs.ActiveOrders:
                            _activePanel.DrawActiveOrdersPanel();
                            break;
                        case OrdersTabs.Tabs.CreateOrder:
                            _creatorPanel.DrawOrderCreatorPanel();
                            break;
                        case OrdersTabs.Tabs.AssignOrder:
                            _assignerPanel.DrawOrderAssignerPanel();
                            break;
                        default:
                            break;
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex}");
        }
        finally
        {
            ImGui.PopStyleVar();
        }
    }
}
