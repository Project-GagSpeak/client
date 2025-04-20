using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services.Textures;
using GagspeakAPI.Enums;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.UI.Components;

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

    private static IconCheckboxStimulation StimulationIconCheckbox = new(FAI.VolumeUp, FAI.VolumeDown, FAI.VolumeOff, FAI.VolumeMute, CkGui.Color(ImGuiColors.DalamudGrey), CkColor.FancyHeaderContrast.Uint());
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
    private const string StimulationTooltip = "--COL--WARNING: This affects your GCD/oGCD cooldown usages!--COL--" +
        "--SEP--The higher the Stimulation, the longer the GCD/oGCD increase." +
        "--SEP--Values: [Off: 1x] [Low: 1.125] [Medium: 1.375] [High: 1.875]";

    private const int TraitCount = 7;
    private static Vector2 TraitBoxSize = new Vector2(ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemInnerSpacing.X, ImGui.GetFrameHeight());

    public void DrawOneRowTraits(ITraitHolder traits, float width, Traits disabled, bool disableStim = true)
    {
        var size = new Vector2(width, ImGui.GetFrameHeight());
        using (CkComponents.CenterHeaderChild("HC_Traits", "Hardcore Traits", size, WFlags.AlwaysUseWindowPadding))
        {
            var offsetSpacing = (ImGui.GetContentRegionAvail().X - (TraitBoxSize.X * TraitCount)) / (TraitCount - 1);

            TraitCheckbox("HandsTied", _textures.CoreTextures[CoreTexture.RestrainedArmsLegs], ref traits, Traits.ArmsRestrained, HandsToolTip, disabled.HasAny(Traits.ArmsRestrained));
            ImGui.SameLine(0, offsetSpacing);
            TraitCheckbox("LegsTied", _textures.CoreTextures[CoreTexture.Restrained], ref traits, Traits.LegsRestrained, LegsToolTip, disabled.HasAny(Traits.LegsRestrained));
            ImGui.SameLine(0, offsetSpacing);
            TraitCheckbox("Gagged", _textures.CoreTextures[CoreTexture.Gagged], ref traits, Traits.Gagged, GaggedToolTip, disabled.HasAny(Traits.Gagged));
            ImGui.SameLine(0, offsetSpacing);
            TraitCheckbox("Blindfolded", _textures.CoreTextures[CoreTexture.Blindfolded], ref traits, Traits.Blindfolded, BlindfoldedToolTip, disabled.HasAny(Traits.Blindfolded));
            ImGui.SameLine(0, offsetSpacing);
            TraitCheckbox("Immobile", _textures.CoreTextures[CoreTexture.ShockCollar], ref traits, Traits.Immobile, ImmobileToolTip, disabled.HasAny(Traits.Immobile));
            ImGui.SameLine(0, offsetSpacing);
            TraitCheckbox("Weighty", _textures.CoreTextures[CoreTexture.Weighty], ref traits, Traits.Weighty, WeightyToolTip, disabled.HasAny(Traits.Weighty));
            ImGui.SameLine(0, offsetSpacing);
            StimTraitCheckbox("Stimulated", _textures.CoreTextures[CoreTexture.Vibrator], ref traits, StimulationTooltip, disableStim);
        }
    }

    public void DrawTwoRowTraits(ITraitHolder traits, float width, Traits disabled, bool disableStim = true)
    {
        var size = new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y);
        using (CkComponents.CenterHeaderChild("HC_Traits", "Hardcore Traits", size, WFlags.AlwaysUseWindowPadding))
        {
            var offsetSpacing = (ImGui.GetContentRegionAvail().X - (TraitBoxSize.X * 4)) / 3;

            using (ImRaii.Group())
            {
                TraitCheckbox("HandsTied", _textures.CoreTextures[CoreTexture.RestrainedArmsLegs], ref traits, Traits.ArmsRestrained, HandsToolTip, disabled.HasAny(Traits.ArmsRestrained));
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("LegsTied", _textures.CoreTextures[CoreTexture.Restrained], ref traits, Traits.LegsRestrained, LegsToolTip, disabled.HasAny(Traits.LegsRestrained));
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
                ImGui.SameLine(0, offsetSpacing);
                StimTraitCheckbox("Stimulated", _textures.CoreTextures[CoreTexture.Vibrator], ref traits, StimulationTooltip, disableStim);
            }
        }
    }

    // Rare sighting of cancerous if else spam.
    public void DrawTraitPreview(Traits itemTraits, Stimulation stimulation)
    {
        if (itemTraits is Traits.None && stimulation is Stimulation.None)
            return;

        var iconSize = new Vector2(ImGui.GetFrameHeight());
        var spacing = ImGui.GetStyle().ItemSpacing.X / 2;
        var endX = ImGui.GetCursorPosX();
        var currentX = endX;

        if (itemTraits.HasAny(Traits.LegsRestrained))
        {
            currentX -= (iconSize.X + spacing);
            ImGui.SameLine(currentX);
            ImGui.Image(_textures.CoreTextures[CoreTexture.Restrained].ImGuiHandle, iconSize);
        }
        if (itemTraits.HasAny(Traits.ArmsRestrained))
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
        if (itemTraits.HasAny(Traits.ArmsRestrained))
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

        // Stimulation stuff.
        if (stimulation.HasAny(Stimulation.Any))
        {
            currentX -= (iconSize.X + spacing);
            ImGui.SameLine(currentX);
            ImGui.Image(_textures.CoreTextures[CoreTexture.Vibrator].ImGuiHandle, iconSize);
        }
    }

    public bool TraitCheckbox<T>(string id, IDalamudTextureWrap image, ref T traitHost, Traits toggleTrait, string tt, bool disabled) where T : ITraitHolder
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

    public bool StimTraitCheckbox<T>(string id, IDalamudTextureWrap image, ref T traitHost, string tt, bool disabled) where T : ITraitHolder
    {
        using var group = ImRaii.Group();
        var changed = false;
        var current = traitHost.Stimulation;
        using (ImRaii.Disabled(disabled)) changed = StimulationIconCheckbox.Draw("##" + id, ref current);

        if (changed)
            traitHost.Stimulation = current;

        ImUtf8.SameLineInner();
        if (image is { } wrap)
            ImGui.Image(wrap.ImGuiHandle, new Vector2(ImGui.GetFrameHeight()));
        CkGui.AttachToolTip(tt, color: ImGuiColors.DalamudYellow);

        return changed;
    }


}
