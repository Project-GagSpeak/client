using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.DrawSystem;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class AllowancesUI : WindowMediatorSubscriberBase
{
    private static readonly GSModule[] Options = [GSModule.Restraint, GSModule.Restriction, GSModule.Gag, GSModule.Pattern, GSModule.Trigger];
    private bool ThemePushed = false;

    private readonly AllowancesConfig _config;
    private readonly AllowancesDrawer _drawer;
    private readonly KinksterManager _kinksters;
    private readonly TutorialService _guides;

    public AllowancesUI(ILogger<AllowancesUI> logger, GagspeakMediator mediator,
        AllowancesConfig config, AllowancesDrawer drawer, KinksterManager kinksters,
        TutorialService guides) 
        : base(logger, mediator, "Trait Allowances")
    {
        _config = config;
        _drawer = drawer;
        _kinksters = kinksters;
        _guides = guides;

        this.PinningClickthroughFalse();
        this.SetBoundaries(new(550, 470), ImGui.GetIO().DisplaySize);

        this.TitleBarButtons = new TitleBarButtonBuilder().Build(); // No tutorial yet.

        RespectCloseHotkey = false;
    }

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .403f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.428f));
            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }

    // THE FOLLOWING IS A TEMPORARY PLACEHOLDER UI DESIGN MADE TO SIMPLY VERIFY THINGS ACTUALLY CAN BUILD. DESIGN LATER.
    protected override void DrawInternal()
    {
        var winPadding = ImGui.GetStyle().WindowPadding;
        var headerInner = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        var splitterSize = ImGui.GetFrameHeight() / 4;

        // Draw a flat header.
        var drawRegions = CkHeader.Flat(CkCol.CurvedHeader.Uint(), headerInner, 175f * ImUtf8.GlobalScale, splitterSize);

        // Create a child for each region, drawn to the size.
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("TraitsTL", drawRegions.TopLeft.Size))
            _drawer.DrawFilterRow(drawRegions.TopLeft.SizeX, 50);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("TraitsBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _drawer.DrawAllKinkstersFolder(drawRegions.BotLeft.SizeX, true);

        // Then the allowance contents.
        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("TraitsTR", drawRegions.TopRight.Size))
            DrawModuleTitle();

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("TraitsBR", drawRegions.BotRight.Size))
            DrawAllowancesEditor();
    }

    public void DrawModuleTitle()
    {
        //using var font = Fonts.GagspeakLabelFont.Push();
        var offset = (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Trait Allowance Manager").X) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextUnformatted("Trait Allowance Manager");
    }

    public void DrawAllowancesEditor()
    {
        // we want to draw 2 child containers here, one for module selection, and another for viewing the active pairs.
        var childWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;
        // Group this to get the full height.
        using (ImRaii.Group())
        {
            using (var c = CkRaii.HeaderChild("Module Management", new Vector2(childWidth, ImGui.GetContentRegionAvail().Y), HeaderFlags.SizeIncludesHeader))
            {
                var selectorHeight = ImGui.GetFrameHeightWithSpacing() * Options.Length - ImGui.GetStyle().ItemSpacing.Y;
                using (CkRaii.FramedChildPaddedW("Selector", c.InnerRegion.X, selectorHeight, CkCol.CurvedHeaderFade.Uint(), 0))
                {
                    // We have a mod, so we should grab the presets from it.
                    var itemSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
                    foreach (var moduleOption in Options)
                        if (DrawModuleOption(moduleOption, itemSize, _drawer.CurModule == moduleOption))
                            _drawer.CurModule = moduleOption;
                }

                // Action elements.
                var actionsH = ImGui.GetFrameHeightWithSpacing() * 6 - ImGui.GetStyle().ItemSpacing.Y;
                var childStartY = ImGui.GetContentRegionAvail().Y - actionsH.AddWinPadY() - ImGui.GetTextLineHeightWithSpacing();
                ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() + new Vector2(0, childStartY));
                ImGuiUtil.Center("Module Actions");
                using (CkRaii.FramedChildPaddedW("ActList", c.InnerRegion.X, actionsH, CkCol.CurvedHeaderFade.Uint(), 0))
                {
                    // We have a mod, so we should grab the presets from it.
                    var itemSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
                    // Create a framed child for then allow selected button.

                    // Migrate all of this into the allowances drawer later.
                    DrawAllowanceAction(FAI.SearchPlus, "Allow Filtered", itemSize, _drawer.AllowFiltered);
                    DrawAllowanceAction(FAI.Plus, "Allow Selected", itemSize, _drawer.AllowSelected);
                    DrawAllowanceAction(FAI.HeartCirclePlus, "Allow Favorites", itemSize, _drawer.AllowFavorites);
                    DrawAllowanceAction(FAI.SearchMinus, "Disallow Filtered", itemSize, _drawer.DisallowFiltered);
                    DrawAllowanceAction(FAI.Minus, "Disallow Selected", itemSize, _drawer.DisallowSelected);
                    DrawAllowanceAction(FAI.HeartCircleMinus, "Disallow Favorites", itemSize, _drawer.DisallowFavorites);
                }
            }
        }
        // then draw out the allowed pairs list.
        ImGui.SameLine();
        using (CkRaii.HeaderChild("Allowed Pairs", new Vector2(childWidth, ImGui.GetContentRegionAvail().Y), HeaderFlags.SizeIncludesHeader))
        {
            using (CkRaii.FramedChild("PairsInner", ImGui.GetContentRegionAvail(), CkCol.CurvedHeaderFade.Uint(), 0, wFlags: ImGuiWindowFlags.AlwaysUseWindowPadding))
            {
                if (_drawer.AllowedPairs.Count <= 0)
                    return;
                // Clip draw for performance GAINS
                ImGuiClip.ClippedDraw(_drawer.AllowedPairs, DrawAllowedPair, ImGui.GetFrameHeight());
            }
        }
    }

    private void DrawAllowanceAction(FAI icon, string label, Vector2 size, Action onClick)
    {
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + size);
        var color = hovering ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkCol.CurvedHeaderFade.Uint();
        using (CkRaii.FramedChild("AllowanceAction-" + label, size, color, 0))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.AlignTextToFramePadding();
            CkGui.IconText(icon);
            ImUtf8.SameLineInner();
            ImGui.TextUnformatted(label);
        }
        if (hovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            onClick();
    }



    private bool DrawModuleOption(GSModule option, Vector2 size, bool selected)
    {
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + size);
        var color = selected
            ? CkCol.LChildBg.Uint()
            : hovering ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkCol.CurvedHeaderFade.Uint();
        using (CkRaii.FramedChild("Module-" + option, size, color, 0))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
            DrawModuleImage(option);
            ImUtf8.TextFrameAligned(option.ToString());
        }
        if (hovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            return true;

        return false;
    }

    private void DrawModuleImage(GSModule option)
    {
        var img = option switch
        {
            GSModule.Restraint => CosmeticService.CoreTextures.Cache[CoreTexture.RestrainedArmsLegs],
            GSModule.Restriction => CosmeticService.CoreTextures.Cache[CoreTexture.Restrained],
            GSModule.Gag => CosmeticService.CoreTextures.Cache[CoreTexture.Gagged],
            GSModule.Pattern => CosmeticService.CoreTextures.Cache[CoreTexture.Vibrator],
            GSModule.Trigger => CosmeticService.CoreTextures.Cache[CoreTexture.ShockCollar],
            _ => null
        };
        if (img is { } wrap)
        {
            ImGui.Image(wrap.Handle, new Vector2(ImGui.GetFrameHeight()));
            ImGui.SameLine();
        }
    }

    private void DrawAllowedPair(Kinkster pair)
    {
        var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + size);
        var color = hovering ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkCol.CurvedHeaderFade.Uint();
        using (CkRaii.FramedChild("AllowedPair" + pair.UserData.UID, size, color, 0))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
            if (pair.UserData.Tier is { } tier && tier is not CkSupporterTier.NoRole)
            {
                var img = CosmeticService.GetSupporterInfo(pair.UserData);
                if (img.SupporterWrap is { } wrap)
                {
                    ImGui.Image(wrap.Handle, new Vector2(ImGui.GetFrameHeight()));
                    CkGui.AttachToolTip(img.Tooltip);
                    ImGui.SameLine();
                }
            }
            ImUtf8.TextFrameAligned(pair.GetNickAliasOrUid());
            // draw eraser.
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemInnerSpacing.X);
            if (CkGui.IconButton(FAI.Eraser, inPopup: true))
            {
                _config.RemoveAllowance(_drawer.CurModule, pair.UserData.UID);
            }
        }
    }

}
