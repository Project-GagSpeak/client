using CkCommons;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class PuppeteerUI : WindowMediatorSubscriberBase
{
    // Revamp this later.
    private static bool THEME_PUSHED = false;
    
    private readonly PuppeteerManager _manager;
    private readonly TutorialService _guides;

    public PuppeteerUI(ILogger<PuppeteerUI> logger, GagspeakMediator mediator, PuppeteerManager manager, 
        TutorialService guides, AliasesTab aliases, PuppeteersTab puppeteers, MarionettesTab marionettes)
        : base(logger, mediator, "Puppeteer Interface")
    {
        _manager = manager;
        _guides = guides;

        PuppeteerTabs = [ aliases, puppeteers, marionettes ];

        this.PinningClickthroughFalse();
        this.SetBoundaries(new(640, 490), ImGui.GetIO().DisplaySize);
        TitleBarButtons = new TitleBarButtonBuilder().AddTutorial(_guides, TutorialType.Puppeteer).Build();
    }

    public static IFancyTab[] PuppeteerTabs;

    protected override void PreDrawInternal()
    {
        if (!THEME_PUSHED)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .403f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.428f));
            THEME_PUSHED = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (THEME_PUSHED)
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(2);
            THEME_PUSHED = false;
        }
    }

    protected override void DrawInternal()
    {
        var regions = CkHeader.FlatWithBends(CkCol.CurvedHeader.Uint(), ImUtf8.FrameHeight / 2, ImUtf8.ItemSpacing.X, ImUtf8.FrameHeight);

        // Idk what to put on the top section outside of it being a stylized header area.

        ImGui.SetCursorScreenPos(regions.BotLeft.Pos);
        using (ImRaii.Child("PuppeteerContent", regions.BotSize, false, WFlags.AlwaysUseWindowPadding))
            DrawTabBarContent();
    }

    private void DrawHeader(Vector2 region)
    {
        // Empty for now.
    }

    private void DrawTabBarContent()
    {
        using var _ = CkRaii.TabBarChild("PuppeteerTabs", GsCol.VibrantPink.Uint(), GsCol.VibrantPinkHovered.Uint(), CkCol.CurvedHeader.Uint(),
                LabelFlags.PadInnerChild | LabelFlags.SizeIncludesHeader, out var selected, PuppeteerTabs);
        // Draw the selected tab's contents.
        selected?.DrawContents(_.InnerRegion.X);
    }
}
