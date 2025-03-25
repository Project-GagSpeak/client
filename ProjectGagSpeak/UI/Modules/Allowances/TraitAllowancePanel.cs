using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Collections.Immutable;

namespace GagSpeak.UI.Wardrobe;

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

    private ModuleSection[] Options = new ModuleSection[5]
    {
        ModuleSection.Restraint,
        ModuleSection.Restriction,
        ModuleSection.Gag,
        ModuleSection.Pattern,
        ModuleSection.Trigger
    };

    private ModuleSection       _selectedModule = ModuleSection.None;
    private ImmutableList<Pair> _allowedPairs = ImmutableList<Pair>.Empty;
    public ModuleSection SelectedModule
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

    private IEnumerable<string> GetAllowancesForModule(ModuleSection module)
    {
        return _selectedModule switch
        {
            ModuleSection.Restraint     => _manager.TraitAllowancesRestraints,
            ModuleSection.Restriction   => _manager.TraitAllowancesRestrictions,
            ModuleSection.Gag           => _manager.TraitAllowancesGags,
            ModuleSection.Pattern       => _manager.TraitAllowancesPatterns,
            ModuleSection.Trigger       => _manager.TraitAllowancesTriggers,
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
        // we want to draw 2 childs here, one for module selection, and another for viewing the active pairs.
        var childWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;
        // Group this to get the full height.
        using (ImRaii.Group())
        {
            using (CkComponents.CenterHeaderChild("ModuleViewer", "Module Management", new Vector2(childWidth, ImGui.GetContentRegionAvail().Y), WFlags.AlwaysUseWindowPadding))
            {
                var selectorHeight = ImGui.GetFrameHeightWithSpacing() * Options.Length - ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2;
                var region = new Vector2(ImGui.GetContentRegionAvail().X, selectorHeight);
                using (CkComponents.FramedChild("ModuleSelector", CkColor.FancyHeaderContrast.Uint(), region, WFlags.AlwaysUseWindowPadding))
                {
                    // We have a mod, so we should grab the presets from it.
                    var itemSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
                    foreach (var moduleOption in Options)
                        if(DrawModuleOption(moduleOption, itemSize, _selectedModule == moduleOption))
                            SelectedModule = moduleOption;
                }

                // Action elements.
                var actionsH = ImGui.GetFrameHeightWithSpacing() * 6 - ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2;
                var actionsRegion = new Vector2(ImGui.GetContentRegionAvail().X, actionsH);
                var childStartY = ImGui.GetContentRegionAvail().Y - actionsH - ImGui.GetTextLineHeightWithSpacing();
                ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() + new Vector2(0, childStartY));
                ImGuiUtil.Center("Module Actions");
                using (CkComponents.FramedChild("ModuleActions", CkColor.FancyHeaderContrast.Uint(), actionsRegion, WFlags.AlwaysUseWindowPadding))
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
        using (CkComponents.CenterHeaderChild("AllowedPairs", "Allowed Pairs", new Vector2(childWidth, ImGui.GetContentRegionAvail().Y), WFlags.AlwaysUseWindowPadding))
        {
            using (CkComponents.FramedChild("PairsInner", CkColor.FancyHeaderContrast.Uint(), ImGui.GetContentRegionAvail(), WFlags.AlwaysUseWindowPadding))
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
        using (CkComponents.FramedChild("AllowanceAction-" + label, color, size))
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



    private bool DrawModuleOption(ModuleSection option, Vector2 size, bool selected)
    {
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + size);
        var color = selected
            ? CkColor.ElementBG.Uint()
            : hovering ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkColor.FancyHeaderContrast.Uint();
        using (CkComponents.FramedChild("ModuleSection-" + option, color, size))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
            DrawModuleImage(option);
            ImUtf8.TextFrameAligned(option.ToString());
        }
        if(hovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            return true;

        return false;
    }

    private void DrawModuleImage(ModuleSection option)
    {
        var img = option switch
        {
            ModuleSection.Restraint => _cosmetics.CoreTextures[CoreTexture.RestrainedArmsLegs],
            ModuleSection.Restriction => _cosmetics.CoreTextures[CoreTexture.Restrained],
            ModuleSection.Gag => _cosmetics.CoreTextures[CoreTexture.Gagged],
            ModuleSection.Pattern => _cosmetics.CoreTextures[CoreTexture.Vibrator],
            ModuleSection.Trigger => _cosmetics.CoreTextures[CoreTexture.ShockCollar],
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
        using (CkComponents.FramedChild("AllowedPair"+pair.UserData.UID, color, size))
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
