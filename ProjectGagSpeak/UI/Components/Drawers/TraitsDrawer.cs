using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Raii;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Components;

// Drawing methods for shared IRestriction based interactions without required dependancies.
public class TraitsDrawer
{
    private readonly ILogger<ActiveItemsDrawer> _logger;
    private readonly CosmeticService _textures;
    public TraitsDrawer(ILogger<ActiveItemsDrawer> logger, CosmeticService textures)
    {
        _logger = logger;
        _textures = textures;
    }

    private const string HandsToolTip = "--COL--WARNING: This actually affects what you can execute!--COL--" +
        "--SEP--Blocks all Actions dependent on the use of hands.";
    private const string LegsToolTip = "--COL--WARNING: This actually affects what you can execute!--COL--" +
        "--SEP--Blocks all Actions dependent on the use of legs.";
    private const string GaggedToolTip = "--COL--WARNING: This actually affects what you can execute!--COL--" +
        "--SEP--Blocks all Actions dependent on sound.";
    private const string BlindfoldedToolTip = "--COL--WARNING: This actually affects what you can execute!--COL--" +
        "--SEP--Blocks all Actions dependent on seeing the target you attack.";
    private const string ImmobileToolTip = "--COL--EXTREME WARNING: FULLY PREVENTS MOVEMENT. Be careful using in public instances!!--COL--" +
        "--SEP--Blocks all movement (outside of turning)";
    private const string WeightyToolTip = "--COL--WARNING: Completely forces RP walk! Be careful using in public instances!--COL--" +
        "--SEP--Prevents running, sprinting, and forces RP walk.";

    private const int TraitCount = 6;
    private static Vector2 TraitBoxSize = new Vector2(ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemInnerSpacing.X, ImGui.GetFrameHeight());

    public void DrawOneRowTraits(IAttributeItem traits, float width, Traits disabled, bool disableStim = true)
    {
        using (var child = CkRaii.HeaderChild("Hardcore Traits", new Vector2(width, ImGui.GetFrameHeight()), HeaderFlags.AddPaddingToHeight))
        {
            OneRowTraitsInner(traits, child.InnerRegion.X, disabled, disableStim);
        }
    }

    public void OneRowTraitsInner(IAttributeItem traits, float width, Traits disabled, bool disableStim = true)
    {
        var offsetSpacing = (ImGui.GetContentRegionAvail().X - (TraitBoxSize.X * TraitCount)) / (TraitCount - 1);

        TraitCheckbox("HandsTied", _textures.CoreTextures[CoreTexture.RestrainedArmsLegs], ref traits, Traits.BoundArms, HandsToolTip, disabled.HasAny(Traits.BoundArms));
        ImGui.SameLine(0, offsetSpacing);
        TraitCheckbox("LegsTied", _textures.CoreTextures[CoreTexture.Restrained], ref traits, Traits.BoundLegs, LegsToolTip, disabled.HasAny(Traits.BoundLegs));
        ImGui.SameLine(0, offsetSpacing);
        TraitCheckbox("Gagged", _textures.CoreTextures[CoreTexture.Gagged], ref traits, Traits.Gagged, GaggedToolTip, disabled.HasAny(Traits.Gagged));
        ImGui.SameLine(0, offsetSpacing);
        TraitCheckbox("Blindfolded", _textures.CoreTextures[CoreTexture.Blindfolded], ref traits, Traits.Blindfolded, BlindfoldedToolTip, disabled.HasAny(Traits.Blindfolded));
        ImGui.SameLine(0, offsetSpacing);
        TraitCheckbox("Immobile", _textures.CoreTextures[CoreTexture.ShockCollar], ref traits, Traits.Immobile, ImmobileToolTip, disabled.HasAny(Traits.Immobile));
        ImGui.SameLine(0, offsetSpacing);
        TraitCheckbox("Weighty", _textures.CoreTextures[CoreTexture.Weighty], ref traits, Traits.Weighty, WeightyToolTip, disabled.HasAny(Traits.Weighty));
    }


    public void DrawTwoRowTraits(IAttributeItem traits, float width, Traits disabled, bool disableStim = true)
    {
        using (CkRaii.HeaderChild("Hardcore Traits", new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), HeaderFlags.AddPaddingToHeight))
        {
            var offsetSpacing = (ImGui.GetContentRegionAvail().X - (TraitBoxSize.X * 4)) / 3;

            using (ImRaii.Group())
            {
                TraitCheckbox("HandsTied", _textures.CoreTextures[CoreTexture.RestrainedArmsLegs], ref traits, Traits.BoundArms, HandsToolTip, disabled.HasAny(Traits.BoundArms));
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("LegsTied", _textures.CoreTextures[CoreTexture.Restrained], ref traits, Traits.BoundLegs, LegsToolTip, disabled.HasAny(Traits.BoundLegs));
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("Gagged", _textures.CoreTextures[CoreTexture.Gagged], ref traits, Traits.Gagged, GaggedToolTip, disabled.HasAny(Traits.Gagged));
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("Blindfolded", _textures.CoreTextures[CoreTexture.Blindfolded], ref traits, Traits.Blindfolded, BlindfoldedToolTip, disabled.HasAny(Traits.Blindfolded));
            }
            using (ImRaii.Group())
            {
                TraitCheckbox("Immobile", _textures.CoreTextures[CoreTexture.ShockCollar], ref traits, Traits.Immobile, ImmobileToolTip, disabled.HasAny(Traits.Immobile));
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("Weighty", _textures.CoreTextures[CoreTexture.Weighty], ref traits, Traits.Weighty, WeightyToolTip, disabled.HasAny(Traits.Weighty));
            }
        }
    }

    // Rare sighting of cancerous if else spam.
    public void DrawTraitPreview(Traits itemTraits)
    {
        if (itemTraits is Traits.None)
            return;

        var iconSize = new Vector2(ImGui.GetFrameHeight());
        var spacing = ImGui.GetStyle().ItemSpacing.X / 2;
        var endX = ImGui.GetCursorPosX();
        var currentX = endX;

        if (itemTraits.HasAny(Traits.BoundLegs))
        {
            currentX -= (iconSize.X + spacing);
            ImGui.SameLine(currentX);
            ImGui.Image(_textures.CoreTextures[CoreTexture.Restrained].ImGuiHandle, iconSize);
        }
        if (itemTraits.HasAny(Traits.BoundArms))
        {
            currentX -= (iconSize.X + spacing);
            ImGui.SameLine(currentX);
            ImGui.Image(_textures.CoreTextures[CoreTexture.RestrainedArmsLegs].ImGuiHandle, iconSize);
        }
        if (itemTraits.HasAny(Traits.Gagged))
        {
            currentX -= (iconSize.X + spacing);
            ImGui.SameLine(currentX);
            ImGui.Image(_textures.CoreTextures[CoreTexture.Gagged].ImGuiHandle, iconSize);
        }
        if (itemTraits.HasAny(Traits.Blindfolded))
        {
            currentX -= (iconSize.X + spacing);
            ImGui.SameLine(currentX);
            ImGui.Image(_textures.CoreTextures[CoreTexture.Blindfolded].ImGuiHandle, iconSize);
        }
        if (itemTraits.HasAny(Traits.BoundArms))
        {
            currentX -= (iconSize.X + spacing);
            ImGui.SameLine(currentX);
            ImGui.Image(_textures.CoreTextures[CoreTexture.RestrainedArmsLegs].ImGuiHandle, iconSize);
        }
        if (itemTraits.HasAny(Traits.Immobile))
        {
            currentX -= (iconSize.X + spacing);
            ImGui.SameLine(currentX);
            ImGui.Image(_textures.CoreTextures[CoreTexture.Immobilize].ImGuiHandle, iconSize);
        }
        if (itemTraits.HasAny(Traits.Weighty))
        {
            currentX -= (iconSize.X + spacing);
            ImGui.SameLine(currentX);
            ImGui.Image(_textures.CoreTextures[CoreTexture.Weighty].ImGuiHandle, iconSize);
        }
    }

    public bool TraitCheckbox<T>(string id, IDalamudTextureWrap image, ref T traitHost, Traits toggleTrait, string tt, bool disabled) where T : IAttributeItem
    {
        using var group = ImRaii.Group();
        var changed = false;
        var curState = (traitHost.Traits & toggleTrait) != 0;
        using (ImRaii.Disabled(disabled)) changed = ImGui.Checkbox("##" + id, ref curState);

        if (changed)
            traitHost.Traits = curState ? traitHost.Traits | toggleTrait : traitHost.Traits & ~toggleTrait;
        ImUtf8.SameLineInner();
        if (image is { } wrap)
            ImGui.Image(wrap.ImGuiHandle, new Vector2(ImGui.GetFrameHeight()));
        CkGui.AttachToolTip(tt, color: ImGuiColors.DalamudYellow);

        return changed;
    }
}
