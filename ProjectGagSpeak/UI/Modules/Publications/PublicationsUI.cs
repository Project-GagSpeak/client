using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Gui.Components;
using Dalamud.Bindings.ImGui;
using GagSpeak.Utils;

namespace GagSpeak.Gui.Publications;

public class PublicationsUI : WindowMediatorSubscriberBase
{
    private readonly PublicationTabs _tabMenu;
    private readonly PublicationsManager _manager;
    private readonly CosmeticService _cosmetics;

    public PublicationsUI(ILogger<PublicationsUI> logger, GagspeakMediator mediator, PublicationTabs tabs,
        PublicationsManager manager, CosmeticService cosmetics)
        : base(logger, mediator, "My Publications")
    {
        _tabMenu = tabs;
        _manager = manager;
        _cosmetics = cosmetics;

        // define initial size of window and to not respect the close hotkey.
        this.SetBoundaries(new Vector2(525, 450), ImGui.GetIO().DisplaySize);
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
        _tabMenu.Draw(region.X);

        using (ImRaii.Child("##PublicationsPanel", Vector2.Zero, false))
        {
            switch (_tabMenu.TabSelection)
            {
                case PublicationTabs.SelectedTab.Patterns:
                    _manager.DrawPatternPublications();
                    break;
                case PublicationTabs.SelectedTab.Moodles:
                    _manager.DrawMoodlesPublications();
                    break;
                default:
                    break;
            };
        }
    }
}
