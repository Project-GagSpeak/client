using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Text;
using System.Collections.Immutable;

namespace GagSpeak.Gui.Wardrobe;

public class TraitAllowancePanel
{
    private readonly ILogger<TraitAllowancePanel> _logger;
    private readonly TraitAllowanceSelector _selector;
    private readonly TraitAllowanceManager _manager;
    private readonly AttributeDrawer _drawer;
    private readonly KinksterManager _pairs;
    private readonly FavoritesManager _favorites;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public TraitAllowancePanel(
        ILogger<TraitAllowancePanel> logger,
        TraitAllowanceSelector selector,
        TraitAllowanceManager manager,
        AttributeDrawer drawer,
        KinksterManager pairs,
        FavoritesManager favorites,
        CosmeticService cosmetics,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _manager = manager;
        _drawer = drawer;
        _pairs = pairs;
        _favorites = favorites;
        _cosmetics = cosmetics;
        _guides = guides;
    }

    private GagspeakModule[] Options = [
        GagspeakModule.Restraint,
        GagspeakModule.Restriction,
        GagspeakModule.Gag,
        GagspeakModule.Pattern,
        GagspeakModule.Trigger
        ];

    private GagspeakModule _selectedModule = GagspeakModule.None;
    private ImmutableList<Kinkster> _allowedPairs = ImmutableList<Kinkster>.Empty;
    public GagspeakModule SelectedModule
    {
        get => _selectedModule;
        set
        {
            if (_selectedModule == value)
                return;

            _selectedModule = value;
            RefreshAllowedList();
        }
    }

    private IEnumerable<string> GetAllowancesForModule(GagspeakModule module)
    {
        return _selectedModule switch
        {
            GagspeakModule.Restraint     => _manager.TraitAllowancesRestraints,
            GagspeakModule.Restriction   => _manager.TraitAllowancesRestrictions,
            GagspeakModule.Gag           => _manager.TraitAllowancesGags,
            GagspeakModule.Pattern       => _manager.TraitAllowancesPatterns,
            GagspeakModule.Trigger       => _manager.TraitAllowancesTriggers,
            _ => Enumerable.Empty<string>()
        };
    }

    private void RefreshAllowedList()
    {
        var allowedUids = GetAllowancesForModule(_selectedModule);
        _allowedPairs = _pairs.DirectPairs
            .Where(pair => allowedUids.Contains(pair.UserData.UID))
            .ToImmutableList();
    }

    public void DrawModuleTitle()
    {
        //using var font = UiFontService.GagspeakLabelFont.Push();
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
                using (CkRaii.FramedChildPaddedW("Selector", c.InnerRegion.X, selectorHeight, CkColor.FancyHeaderContrast.Uint(), 0))
                {
                    // We have a mod, so we should grab the presets from it.
                    var itemSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
                    foreach (var moduleOption in Options)
                        if(DrawModuleOption(moduleOption, itemSize, _selectedModule == moduleOption))
                            SelectedModule = moduleOption;
                }

                // Action elements.
                var actionsH = ImGui.GetFrameHeightWithSpacing() * 6 - ImGui.GetStyle().ItemSpacing.Y;
                var childStartY = ImGui.GetContentRegionAvail().Y - actionsH.AddWinPadY() - ImGui.GetTextLineHeightWithSpacing();
                ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() + new Vector2(0, childStartY));
                ImGuiUtil.Center("Module Actions");
                using (CkRaii.FramedChildPaddedW("ActList", c.InnerRegion.X, actionsH, CkColor.FancyHeaderContrast.Uint(), 0))
                {
                    // We have a mod, so we should grab the presets from it.
                    var itemSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
                    // Create a framed child for then allow selected button.
                    DrawAllowanceAction(FAI.SearchPlus, "Allow Filtered", itemSize, () =>
                    {
                        _manager.AddAllowance(_selectedModule, _selector.FilteredPairs
                            .Select(pair => pair.UserData.UID));
                        RefreshAllowedList();
                    });
                    DrawAllowanceAction(FAI.Plus, "Allow Selected", itemSize, () =>
                    {
                        _manager.AddAllowance(_selectedModule, _selector.SelectedPairs
                            .Select(pair => pair.UserData.UID));
                        RefreshAllowedList();
                    });
                    DrawAllowanceAction(FAI.HeartCirclePlus, "Allow Favorites", itemSize, () =>
                    {
                        _manager.AddAllowance(_selectedModule, _selector.FilteredPairs
                            .Where(p => _favorites._favoriteKinksters.Contains(p.UserData.UID))
                            .Select(pair => pair.UserData.UID));
                        RefreshAllowedList();
                    });
                    // Disallow
                    DrawAllowanceAction(FAI.SearchMinus, "Disallow Filtered", itemSize, () =>
                    {
                        _manager.RemoveAllowance(_selectedModule, _selector.FilteredPairs
                            .Select(pair => pair.UserData.UID));
                        RefreshAllowedList();
                    });
                    DrawAllowanceAction(FAI.Minus, "Disallow Selected", itemSize, () =>
                    {
                        _manager.RemoveAllowance(_selectedModule, _selector.SelectedPairs
                            .Select(pair => pair.UserData.UID));
                        RefreshAllowedList();
                    });
                    DrawAllowanceAction(FAI.HeartCircleMinus, "Disallow Favorites", itemSize, () =>
                    {
                        _manager.RemoveAllowance(_selectedModule, _selector.FilteredPairs
                            .Where(p => _favorites._favoriteKinksters.Contains(p.UserData.UID))
                            .Select(pair => pair.UserData.UID));
                        RefreshAllowedList();
                    });
                }
            }
        }
        // then draw out the allowed pairs list.
        ImGui.SameLine();
        using (CkRaii.HeaderChild("Allowed Pairs", new Vector2(childWidth, ImGui.GetContentRegionAvail().Y), HeaderFlags.SizeIncludesHeader))
        {
            using (CkRaii.FramedChild("PairsInner", ImGui.GetContentRegionAvail(), CkColor.FancyHeaderContrast.Uint(), 0, wFlags: ImGuiWindowFlags.AlwaysUseWindowPadding))
            {
                if (_allowedPairs.Count <= 0)
                    return;
                // Clip draw for performance GAINS
                ImGuiClip.ClippedDraw(_allowedPairs, DrawAllowedPair, ImGui.GetFrameHeight());
            }
        }
    }

    private void DrawAllowanceAction(FAI icon, string label, Vector2 size, Action onClick)
    {
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + size);
        var color = hovering ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkColor.FancyHeaderContrast.Uint();
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



    private bool DrawModuleOption(GagspeakModule option, Vector2 size, bool selected)
    {
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + size);
        var color = selected
            ? CkColor.ElementBG.Uint()
            : hovering ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkColor.FancyHeaderContrast.Uint();
        using (CkRaii.FramedChild("Module-" + option, size, color, 0))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
            DrawModuleImage(option);
            ImUtf8.TextFrameAligned(option.ToString());
        }
        if(hovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            return true;

        return false;
    }

    private void DrawModuleImage(GagspeakModule option)
    {
        var img = option switch
        {
            GagspeakModule.Restraint => CosmeticService.CoreTextures.Cache[CoreTexture.RestrainedArmsLegs],
            GagspeakModule.Restriction => CosmeticService.CoreTextures.Cache[CoreTexture.Restrained],
            GagspeakModule.Gag => CosmeticService.CoreTextures.Cache[CoreTexture.Gagged],
            GagspeakModule.Pattern => CosmeticService.CoreTextures.Cache[CoreTexture.Vibrator],
            GagspeakModule.Trigger => CosmeticService.CoreTextures.Cache[CoreTexture.ShockCollar],
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
        var color = hovering ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkColor.FancyHeaderContrast.Uint();
        using (CkRaii.FramedChild("AllowedPair"+pair.UserData.UID, size, color, 0))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
            if(pair.UserData.Tier is { } tier && tier is not CkSupporterTier.NoRole)
            {
                var img = _cosmetics.GetSupporterInfo(pair.UserData);
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
                _manager.RemoveAllowance(_selectedModule, pair.UserData.UID);
            }
        }
    }
}
