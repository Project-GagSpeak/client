using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Collections.Immutable;

namespace GagSpeak.CkCommons.Gui.Wardrobe;

public class TraitAllowancePanel
{
    private readonly ILogger<TraitAllowancePanel> _logger;
    private readonly TraitAllowanceSelector _selector;
    private readonly TraitsManager _manager;
    private readonly TraitsDrawer _drawer;
    private readonly PairManager _pairs;
    private readonly FavoritesManager _favorites;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public TraitAllowancePanel(
        ILogger<TraitAllowancePanel> logger,
        TraitAllowanceSelector selector,
        TraitsManager manager,
        TraitsDrawer drawer,
        PairManager pairs,
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

    private Module[] Options = new Module[5]
    {
        Module.Restraint,
        Module.Restriction,
        Module.Gag,
        Module.Pattern,
        Module.Trigger
    };

    private Module       _selectedModule = Module.None;
    private ImmutableList<Pair> _allowedPairs = ImmutableList<Pair>.Empty;
    public Module SelectedModule
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

    private IEnumerable<string> GetAllowancesForModule(Module module)
    {
        return _selectedModule switch
        {
            Module.Restraint     => _manager.TraitAllowancesRestraints,
            Module.Restriction   => _manager.TraitAllowancesRestrictions,
            Module.Gag           => _manager.TraitAllowancesGags,
            Module.Pattern       => _manager.TraitAllowancesPatterns,
            Module.Trigger       => _manager.TraitAllowancesTriggers,
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
                using (CkRaii.FramedChildPaddedW("Selector", c.InnerRegion.X, selectorHeight, CkColor.FancyHeaderContrast.Uint()))
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
                using (CkRaii.FramedChildPaddedW("ActList", c.InnerRegion.X, actionsH, CkColor.FancyHeaderContrast.Uint()))
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
            using (CkRaii.FramedChild("PairsInner", ImGui.GetContentRegionAvail(), CkColor.FancyHeaderContrast.Uint(), wFlags: ImGuiWindowFlags.AlwaysUseWindowPadding))
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
        using (CkRaii.FramedChild("AllowanceAction-" + label, size, color))
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



    private bool DrawModuleOption(Module option, Vector2 size, bool selected)
    {
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + size);
        var color = selected
            ? CkColor.ElementBG.Uint()
            : hovering ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkColor.FancyHeaderContrast.Uint();
        using (CkRaii.FramedChild("Module-" + option, size, color))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
            DrawModuleImage(option);
            ImUtf8.TextFrameAligned(option.ToString());
        }
        if(hovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            return true;

        return false;
    }

    private void DrawModuleImage(Module option)
    {
        var img = option switch
        {
            Module.Restraint => _cosmetics.CoreTextures[CoreTexture.RestrainedArmsLegs],
            Module.Restriction => _cosmetics.CoreTextures[CoreTexture.Restrained],
            Module.Gag => _cosmetics.CoreTextures[CoreTexture.Gagged],
            Module.Pattern => _cosmetics.CoreTextures[CoreTexture.Vibrator],
            Module.Trigger => _cosmetics.CoreTextures[CoreTexture.ShockCollar],
            _ => null
        };
        if (img is { } wrap)
        {
            ImGui.Image(wrap.ImGuiHandle, new Vector2(ImGui.GetFrameHeight()));
            ImGui.SameLine();
        }
    }

    private void DrawAllowedPair(Pair pair)
    {
        var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + size);
        var color = hovering ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkColor.FancyHeaderContrast.Uint();
        using (CkRaii.FramedChild("AllowedPair"+pair.UserData.UID, size, color))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
            if(pair.UserData.Tier is { } tier && tier is not CkSupporterTier.NoRole)
            {
                var img = _cosmetics.GetSupporterInfo(pair.UserData);
                if (img.SupporterWrap is { } wrap)
                {
                    ImGui.Image(wrap.ImGuiHandle, new Vector2(ImGui.GetFrameHeight()));
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
