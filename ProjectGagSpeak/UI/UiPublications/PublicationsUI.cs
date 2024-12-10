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

namespace GagSpeak.UI.UiPublications;

public class PublicationsUI : WindowMediatorSubscriberBase
{
    private readonly PublicationsTabMenu _tabMenu;
    private readonly PublicationsManager _publicationsPanel;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;

    public PublicationsUI(ILogger<PublicationsUI> logger, GagspeakMediator mediator,
        PublicationsManager publicationsPanel, CosmeticService cosmetics,
        UiSharedService uiShared) : base(logger, mediator, "My Publications UI")
    {
        _publicationsPanel = publicationsPanel;
        _cosmetics = cosmetics;
        _uiShared = uiShared;

        _tabMenu = new PublicationsTabMenu(_uiShared);
        // define initial size of window and to not respect the close hotkey.
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(525, 450),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
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
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(5f * _uiShared.GetFontScalerFloat(), 0));

        using (ImRaii.Table($"###PublicationsUiWindowTable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, 175f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextColumn();

            var regionSize = ImGui.GetContentRegionAvail();
            using (ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f)))
            {
                using (ImRaii.Child("##PublicationsLeft", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
                {
                    // attempt to obtain an image wrap for it
                    var iconTexture = _cosmetics.CorePluginTextures[CorePluginTexture.Logo256];
                    if (iconTexture is { } wrap)
                    {
                        // aligns the image in the center like we want.
                        UtilsExtensions.ImGuiLineCentered("###PublicationsLogo", () =>
                        {
                            ImGui.Image(wrap.ImGuiHandle, new(125f * _uiShared.GetFontScalerFloat(), 125f * _uiShared.GetFontScalerFloat()));
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text($"What's this? A tooltip hidden in plain sight?");
                                ImGui.EndTooltip();
                            }
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                UnlocksEventManager.AchievementEvent(UnlocksEvent.EasterEggFound, "Publications");
                        });
                    }
                    // add separator
                    ImGui.Spacing();
                    ImGui.Separator();
                    // add the tab menu for the left side.
                    _tabMenu.DrawSelectableTabMenu();
                }
            }
            ImGui.TableNextColumn();
            // display right half viewport based on the tab selection
            using (var rightChild = ImRaii.Child($"###PublicationsRightSide", Vector2.Zero, false))
            {
                switch (_tabMenu.SelectedTab)
                {
                    case PublicationsTabs.Tabs.ManagePatterns:
                        _publicationsPanel.DrawPatternManager();
                        break;
                    case PublicationsTabs.Tabs.ManageMoodles:
                        _publicationsPanel.DrawMoodlesManager();
                        break;
                    default:
                        break;
                };
            }
        }
    }
}
