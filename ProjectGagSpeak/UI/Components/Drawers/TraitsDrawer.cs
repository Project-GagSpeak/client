using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services.Textures;
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
        "--SEP--Blocks all Actions dependant on the use of hands.";
    private const string LegsToolTip = "--COL--WARNING: This actually affects what you can execute!--COL--" +
        "--SEP--Blocks all Actions dependant on the use of legs.";
    private const string GaggedToolTip = "--COL--WARNING: This actually affects what you can execute!--COL--" +
        "--SEP--Blocks all Actions dependant on sound.";
    private const string BlindfoldedToolTip = "--COL--WARNING: This actually affects what you can execute!--COL--" +
        "--SEP--Blocks all Actions dependant on seeing the target you attack.";
    private const string ImmobileToolTip = "--COL--EXTREME WARNING: FULLY PREVENTS MOVEMENT. Be careful using in public instances!!--COL--" +
        "--SEP--Blocks all movement (outside of turning)";
    private const string WeightyToolTip = "--COL--WARNING: Completely forces RP walk! Be careful using in public instances!--COL--" +
        "--SEP--Prevents running, sprinting, and forces RP walk.";
    private const string StimulationTooltip = "--COL--WARNING: This affects your GCD/oGCD cooldown usages!--COL--" +
        "--SEP--The higher the Stimulation, the longer the GCD/oGCD increase." +
        "--SEP--Values: [Off: 1x] [Low: 1.125] [Medium: 1.375] [High: 1.875]";
    
    public void DrawHardcoreTraits(GarblerRestriction itemSource, float width)
    {
        // construct a child object here.
        var style = ImGui.GetStyle();
        var winSize = new Vector2(width, (ImGui.GetFrameHeight() * 2 + style.ItemSpacing.Y));
        using (CkComponents.CenterHeaderChild("HardcoreTraits", "Hardcore Traits", winSize, WFlags.AlwaysUseWindowPadding))
        {
            var innerPos = ImGui.GetCursorScreenPos();
            var innerRegion = ImGui.GetContentRegionAvail();
            // calculate the size of the checkboxicons.
            var iconCheckboxSize = new Vector2(ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemInnerSpacing.X, ImGui.GetFrameHeight());
            // get the remaining spacing after drawing 7 of these, and divide by 6 to get our offset spacing.
            var offsetSpacing = (innerRegion.X - (iconCheckboxSize.X * 4)) / 3;

            using (ImRaii.Group())
            {
                TraitCheckbox("HandsTied", _textures.CoreTextures[CoreTexture.RestrainedArmsLegs], ref itemSource, Traits.ArmsRestrained, HandsToolTip, true);
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("LegsTied", _textures.CoreTextures[CoreTexture.Restrained], ref itemSource, Traits.LegsRestrained, LegsToolTip, true);
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("Gagged", _textures.CoreTextures[CoreTexture.Gagged], ref itemSource, Traits.Gagged, GaggedToolTip, false);
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("Blindfolded", _textures.CoreTextures[CoreTexture.Blindfolded], ref itemSource, Traits.Blindfolded, BlindfoldedToolTip, false);
            }
            using (ImRaii.Group())
            {
                TraitCheckbox("Immobile", _textures.CoreTextures[CoreTexture.ShockCollar], ref itemSource, Traits.Immobile, ImmobileToolTip, true);
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("Weighty", _textures.CoreTextures[CoreTexture.Weighty], ref itemSource, Traits.Weighty, WeightyToolTip, true);
                ImGui.SameLine(0, offsetSpacing);
                StimTraitCheckbox("Stimulated", _textures.CoreTextures[CoreTexture.Vibrator], ref itemSource, StimulationTooltip, true);
            }
        }
    }

    public void DrawHardcoreTraits(RestrictionItem itemSource, float width)
    {
        var isGag = itemSource.Type is RestrictionType.Gag;
        var isCollar = itemSource.Type is RestrictionType.Collar;
        var isBlindfold = itemSource.Type is RestrictionType.Blindfold;
        var isAnySpecial = itemSource.Type is not RestrictionType.Normal;

        // construct a child object here.
        var style = ImGui.GetStyle();
        var winSize = new Vector2(width, (ImGui.GetFrameHeight() * 2 + style.ItemSpacing.Y));
        using (CkComponents.CenterHeaderChild("HardcoreTraits", "Hardcore Traits", winSize, WFlags.AlwaysUseWindowPadding))
        {
            var innerPos = ImGui.GetCursorScreenPos();
            var innerRegion = ImGui.GetContentRegionAvail();
            // calculate the size of the checkboxicons.
            var iconCheckboxSize = new Vector2(ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemInnerSpacing.X, ImGui.GetFrameHeight());
            // get the remaining spacing after drawing 7 of these, and divide by 6 to get our offset spacing.
            var offsetSpacing = (innerRegion.X - (iconCheckboxSize.X * 4)) / 3;

            using (ImRaii.Group())
            {
                TraitCheckbox("HandsTied", _textures.CoreTextures[CoreTexture.RestrainedArmsLegs], ref itemSource, Traits.ArmsRestrained, HandsToolTip, isAnySpecial);
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("LegsTied", _textures.CoreTextures[CoreTexture.Restrained], ref itemSource, Traits.LegsRestrained, LegsToolTip, isAnySpecial);
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("Gagged", _textures.CoreTextures[CoreTexture.Gagged], ref itemSource, Traits.Gagged, GaggedToolTip, (isCollar || isBlindfold));
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("Blindfolded", _textures.CoreTextures[CoreTexture.Blindfolded], ref itemSource, Traits.Blindfolded, BlindfoldedToolTip, isCollar);
            }
            using (ImRaii.Group())
            {
                TraitCheckbox("Immobile", _textures.CoreTextures[CoreTexture.ShockCollar], ref itemSource, Traits.Immobile, ImmobileToolTip, isAnySpecial);
                ImGui.SameLine(0, offsetSpacing);
                TraitCheckbox("Weighty", _textures.CoreTextures[CoreTexture.Weighty], ref itemSource, Traits.Weighty, WeightyToolTip, isAnySpecial);
                ImGui.SameLine(0, offsetSpacing);
                StimTraitCheckbox("Stimulated", _textures.CoreTextures[CoreTexture.Vibrator], ref itemSource, StimulationTooltip, (isGag || isBlindfold));
            }
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
