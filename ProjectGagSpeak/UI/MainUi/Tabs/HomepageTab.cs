using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.CkCommons.Gui.Modules.Puppeteer;
using GagSpeak.CkCommons.Gui.Publications;
using GagSpeak.CkCommons.Gui.Toybox;
using GagSpeak.CkCommons.Gui.UiRemote;
using GagSpeak.CkCommons.Gui.Wardrobe;
using ImGuiNET;
using GagSpeak.GameInternals.Addons;
using GagSpeak.GameInternals.Structs;
using OtterGui.Text;
using GagSpeak.Services.Textures;

namespace GagSpeak.CkCommons.Gui.MainWindow;

/// <summary> The homepage will provide the player with links to open up other windows in the plugin via components </summary>
public class HomepageTab
{
    private readonly GagspeakMediator _mediator;
    private readonly OnFrameworkService _framework;

    private int HoveredItemIndex = -1;
    private readonly List<(string Label, FontAwesomeIcon Icon, Type ToggleType)> Modules;

    public HomepageTab(GagspeakMediator mediator, OnFrameworkService framework)
    {
        _mediator = mediator;
        _framework = framework;

        // Define all module information in a single place
        Modules = new List<(string, FontAwesomeIcon, Type)>
        {
            ("Sex Toy Remote", FAI.WaveSquare, typeof(RemotePersonal)),
            ("Wardrobe", FAI.ToiletPortable, typeof(WardrobeUI)),
            ("Puppeteer", FAI.PersonHarassing, typeof(PuppeteerUI)),
            ("Toybox", FAI.BoxOpen, typeof(ToyboxUI)),
            ("Mod Presets", FAI.FileAlt, typeof(ModPresetsUI)),
            ("Trait Allowances", FAI.UserShield, typeof(TraitAllowanceUI)),
            ("Publications", FAI.CloudUploadAlt, typeof(PublicationsUI)),
            ("Achievements", FAI.Trophy, typeof(AchievementsUI)),
        };
    }

    public void DrawHomepageSection()
    {
        using var borderSize = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 4f);
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(6, 1));
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        using var homepageChild = ImRaii.Child("##Homepage", new Vector2(CkGui.GetWindowContentRegionWidth(), 0), false, WFlags.NoScrollbar);

        var sizeFont = CkGui.CalcFontTextSize("Achievements Module", UiFontService.GagspeakLabelFont);
        var selectableSize = new Vector2(CkGui.GetWindowContentRegionWidth(), sizeFont.Y + ImGui.GetStyle().WindowPadding.Y * 2);
        var itemGotHovered = false;

        for (var i = 0; i < Modules.Count; i++)
        {
            var module = Modules[i];
            var isHovered = HoveredItemIndex == i;

            if (HomepageSelectable(module.Label, module.Icon, selectableSize, isHovered))
            {
                _mediator.Publish(new UiToggleMessage(module.ToggleType));
                if (module.ToggleType == typeof(RemotePersonal))
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteOpened);
            }

            if (ImGui.IsItemHovered())
            {
                itemGotHovered = true;
                HoveredItemIndex = i;
            }
        }
        // if itemGotHovered is false, reset the index.
        if(!itemGotHovered)
            HoveredItemIndex = -1;

        // Testing zone for CkRichText.
        float width = ImGui.GetContentRegionAvail().X;
        using (var node = ImRaii.TreeNode("GagSpeak Emotes"))
        {
            if (node)
            {
                // emote row listing.
                var iconSize = new Vector2(ImGui.GetFrameHeight() * 1.5f);
                var emoteKeys = Enum.GetValues<CoreEmoteTexture>();
                var emotesPerRow = (int)(width / iconSize.X);
                using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero))
                {
                    ImGui.Columns(emotesPerRow, "##EmoteColumns", false);
                    foreach (var emote in emoteKeys)
                    {
                        if (CosmeticService.CoreEmoteTextures.GetValueOrDefault(emote) is not { } texture)
                            ImGui.Dummy(iconSize);
                        else
                        {
                            ImGui.Image(texture.ImGuiHandle, iconSize);
                            CkGui.AttachToolTip($"[emote={emote.ToString()}]");
                        }
                        ImGui.NextColumn();
                    }
                    ImGui.Columns(1);
                }
            }
        }
        ImGui.Separator();
        ImGui.Text("Rich String Tester");
        var refString = _richTextTester;
        ImGui.SetNextItemWidth(width - 2 * ImGui.GetFrameHeight());
        if (ImGui.InputText("##RichTextTester", ref refString, 5000, ITFlags.EnterReturnsTrue))
            _richTextTester = refString;
        CkRichText.DrawColorHelpText();

        using (var node = ImRaii.TreeNode("CkRichString Result:"))
        {
            if(node)
                CkRichText.Text(width, _richTextTester);
        }

        ImGui.Separator();

        using (var node = ImRaii.TreeNode("CkRichString Manual Equivalent."))
        {
            if(node)
            {
                ImGui.Text("This is a CkRichText showcase. CkRichText helps allow for");
                CkGui.ColorText("functionality similar to", ImGuiColors.ParsedGold);
                CkGui.ColorTextInline("SeString", ImGuiColors.ParsedPink);
                CkGui.ColorTextInline("but built for ImGui display.", ImGuiColors.ParsedGold);
                CkGui.TextInline("It");
                ImGui.Text("supports");
                ImGui.SameLine(0, 0);
                CkGui.OutlinedFont("TextWrapping", 0xFFFFFFFF, CkGui.Color(ImGuiColors.DalamudRed), 1);
                CkGui.TextInline("multi-color stacking, and text stroke or");
                ImGui.Text("glow.");
                ImGui.Spacing();
                ImGui.Text("You can space things apart into paragraphs, and run CkRichText");
                ImGui.Text("on");
                ImGui.SameLine(0, 0);
                CkGui.OutlinedFont("every font", 0xFF000000, CkGui.Color(ImGuiColors.HealerGreen), 1);
                ImGui.Separator();
                ImGui.Text("CkRichText supports Line Breaks, and an internal cache for all");
                ImGui.Text("defined strings.");
                ImGui.Spacing();
                ImGui.Text("This is a severe improvement over my MoodlesStringParser,");
                ImGui.Text("that had a framework drawtime of");
                CkGui.ColorTextInline("9.3ms", ImGuiColors.ParsedOrange);
                CkGui.TextInline(", with only");
                ImGui.Text("rendering one icon and 3 outline color'ed text components.");
                ImGui.Spacing();
                ImGui.Text("The previous framework even had it easy as it used Monospace,");
                ImGui.Text("while this works with");
                ImGui.SameLine(0, 0);
                CkGui.OutlinedFont("every font", 0xFF000000, CkGui.Color(ImGuiColors.HealerGreen), 1);
                CkGui.TextInline(". CkRichText can also embed");
                ImGui.Text("emotes");
                ImGui.SameLine(0, 0);
                if (CosmeticService.CoreEmoteTextures.GetValueOrDefault(CoreEmoteTexture.CatFlappyLeft) is { } emoteTexture)
                    ImGui.Image(emoteTexture.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight()));
                else
                    ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight()));
                CkGui.TextInline("along with it's text through an cache");
                ImGui.SameLine(0, 0);
                if (CosmeticService.CoreEmoteTextures.GetValueOrDefault(CoreEmoteTexture.Cappie) is { } cappieTexture)
                    ImGui.Image(cappieTexture.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight()));
                else
                    ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight()));
                CkGui.TextInline(", exciting!");
            }
        }
    }

    private string _richTextTester = string.Empty;

    private bool HomepageSelectable(string label, FontAwesomeIcon icon, Vector2 region, bool hovered = false)
    {
        using var bgColor = hovered
            ? ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered))
            : ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        // store the screen position before drawing the child.
        var buttonPos = ImGui.GetCursorScreenPos();
        using (ImRaii.Child($"##HomepageItem{label}", region, true, WFlags.NoInputs | WFlags.NoScrollbar))
        {
            using var group = ImRaii.Group();
            var height = ImGui.GetContentRegionAvail().Y;

            CkGui.GagspeakBigText(label);
            ImGui.SetWindowFontScale(1.5f);

            var size = CkGui.IconSize(FAI.WaveSquare);
            var color = hovered ? ImGuiColors.ParsedGold : ImGuiColors.DalamudWhite;
            ImGui.SameLine(CkGui.GetWindowContentRegionWidth() - size.X - ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (height - size.Y) / 2);
            CkGui.IconText(icon, color);

            ImGui.SetWindowFontScale(1.0f);
        }
        // draw the button over the child.
        ImGui.SetCursorScreenPos(buttonPos);
        if (ImGui.InvisibleButton("##Button-" + label, region))
            return true;

        return false;
    }
}
