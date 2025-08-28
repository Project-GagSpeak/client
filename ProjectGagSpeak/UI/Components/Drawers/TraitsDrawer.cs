using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;
using CkCommons.Gui;

namespace GagSpeak.Gui.Components;

// Drawing methods for shared IRestriction based interactions without required dependancies.
public class AttributeDrawer
{
    private readonly ILogger<ActiveItemsDrawer> _logger;
    private readonly CosmeticService _textures;
    public AttributeDrawer(ILogger<ActiveItemsDrawer> logger, CosmeticService textures)
    {
        _logger = logger;
        _textures = textures;
    }

    private static readonly (Traits Trait, CoreTexture Icon, string Tooltip, Vector4 Color)[] _displayInfo =
    [
        (Traits.BoundArms, CoreTexture.RestrainedArmsLegs, HandsToolTip, ImGuiColors.DalamudYellow),
        (Traits.BoundLegs, CoreTexture.Restrained, LegsToolTip, ImGuiColors.DalamudYellow),
        (Traits.Gagged, CoreTexture.Gagged, GaggedToolTip, ImGuiColors.DalamudYellow),
        (Traits.Blindfolded, CoreTexture.Blindfolded, BlindfoldedToolTip, ImGuiColors.DalamudYellow),
        (Traits.Immobile, CoreTexture.Immobilize, ImmobileToolTip, ImGuiColors.DalamudRed),
        (Traits.Weighty, CoreTexture.Weighty, WeightyToolTip, ImGuiColors.DalamudRed),
    ];

    private const string HandsToolTip = "--COL--WARNING: Affects what combat actions can be used!--COL--" +
        "--SEP--Blocks all Actions dependent on the use of hands.";
    private const string LegsToolTip = "--COL--WARNING: Affects what combat actions can be used!--COL--" +
        "--SEP--Blocks all Actions dependent on the use of legs.";
    private const string GaggedToolTip = "--COL--WARNING: Affects what combat actions can be used!--COL--" +
        "--SEP--Blocks all Actions dependent on sound.";
    private const string BlindfoldedToolTip = "--COL--WARNING: Affects what combat actions can be used!--COL--" +
        "--SEP--Blocks all Actions dependent on seeing the target you attack.";
    private const string ImmobileToolTip = "--COL--EXTREME WARNING: FULLY PREVENTS MOVEMENT. Be careful using in public instances!!--COL--" +
        "--SEP--Blocks all movement (outside of turning)";
    private const string WeightyToolTip = "--COL--WARNING: Forces RP walk! Be careful using in public instances!--COL--" +
        "--SEP--Prevents running, sprinting, and forces RP walk.";

    private static Vector2 TraitBoxSize = new Vector2(ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemInnerSpacing.X, ImGui.GetFrameHeight());

    public void DrawAttributesChild(IAttributeItem attributes, float width, int maxPerRow, Traits toShow = Traits.All)
    {
        if (toShow is Traits.None)
            return;

        var totalTraits = toShow.ActiveCount();
        var rows = (int)Math.Ceiling((float)totalTraits / maxPerRow);
        var height = ImGui.GetFrameHeight() * rows + ImGui.GetStyle().ItemSpacing.Y * (rows - 1);

        using (var c = CkRaii.HeaderChild("Attributes", new Vector2(width, height), HeaderFlags.AddPaddingToHeight))
            DrawAttributesInternal(attributes, toShow, c.InnerRegion.X, maxPerRow);
    }

    private void DrawAttributesInternal(IAttributeItem attributes, Traits toDraw, float width, int maxPerRow)
    {
        var traitData = _displayInfo.Where(t => toDraw.HasAny(t.Trait)).ToArray();
        var spacing = (width - TraitBoxSize.X * maxPerRow) / (maxPerRow - 1);

        for (int i = 0; i < traitData.Length; i++)
        {
            var (trait, icon, tooltip, col) = traitData[i];
            TraitCheckbox(trait.ToString(), CosmeticService.CoreTextures.Cache[icon], ref attributes, trait);
            CkGui.AttachToolTip(tooltip, color: col);

            // Only SameLine if not the last in the row
            if ((i + 1) % maxPerRow != 0 || i == traitData.Length - 1)
                ImGui.SameLine(0, spacing);
        }

        if (CkGuiUtils.EnumCombo("##Arousal", ImGui.GetContentRegionAvail().X, attributes.Arousal, out var newVal, 
            i => i.ToName(), "Arousal Strength.."))
        {
            attributes.Arousal = newVal;
        }
        CkGui.AttachToolTip("How much this item arouses you.");
    }

    public void DrawTraitPreview(Traits itemTraits)
    {
        if (itemTraits == Traits.None)
            return;

        var iconSize = new Vector2(ImGui.GetFrameHeight());
        var spacing = ImGui.GetStyle().ItemSpacing.X / 2f;

        // Collect icons to draw
        var icons = new List<(CoreTexture tex, string tooltip)>();

        if (itemTraits.HasAny(Traits.BoundArms))   icons.Add((CoreTexture.RestrainedArmsLegs, "[Bound Arms] Attribute is attached"));
        if (itemTraits.HasAny(Traits.BoundLegs))   icons.Add((CoreTexture.Restrained, "[Bound Legs] Attribute is attached"));
        if (itemTraits.HasAny(Traits.Gagged))      icons.Add((CoreTexture.Gagged, "[Gagged] Attribute is attached"));
        if (itemTraits.HasAny(Traits.Blindfolded)) icons.Add((CoreTexture.Blindfolded, "[Blindfolded] Attribute is attached"));
        if (itemTraits.HasAny(Traits.Immobile))    icons.Add((CoreTexture.Immobilize, "[Immobile] Attribute is attached"));
        if (itemTraits.HasAny(Traits.Weighty))     icons.Add((CoreTexture.Weighty, "[Weighty] Attribute is attached"));

        if (icons.Count == 0)
            return;

        //float totalWidth = icons.Count * iconSize.X + (icons.Count - 1) * spacing;
        //float startX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - totalWidth;
        //ImGui.SetCursorPosX(startX);

        for (int i = 0; i < icons.Count; i++)
        {
            if (i > 0) ImUtf8.SameLineInner();
            ImGui.Image(CosmeticService.CoreTextures.Cache[icons[i].tex].Handle, iconSize);
            CkGui.AttachToolTip(icons[i].tooltip);
        }
    }

    public void TraitCheckbox<T>(string id, IDalamudTextureWrap image, ref T traitHost, Traits toggleTrait) where T : IAttributeItem
    {
        using var group = ImRaii.Group();

        var curState = traitHost.Traits.HasAny(toggleTrait);
        if (ImGui.Checkbox("##" + id, ref curState))
            traitHost.Traits = curState ? (traitHost.Traits | toggleTrait) : (traitHost.Traits & ~toggleTrait);

        ImUtf8.SameLineInner();
        if (image is { } wrap)
            ImGui.Image(wrap.Handle, new Vector2(ImGui.GetFrameHeight()));
    }
}
