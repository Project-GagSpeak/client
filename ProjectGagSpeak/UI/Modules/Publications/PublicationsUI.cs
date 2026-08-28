using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Gui.Components;
using Dalamud.Bindings.ImGui;
using GagSpeak.Utils;

namespace GagSpeak.Gui.Publications;

// TODO: Merge this into the main UI or something.
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
                case PublicationTabs.SelectedTab.Loci:
                    _manager.DrawLociPublications();
                    break;
                default:
                    break;
            };
        }
    }
}
