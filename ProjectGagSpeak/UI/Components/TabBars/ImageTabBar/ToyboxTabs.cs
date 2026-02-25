using CkCommons.Widgets;
using GagSpeak.Services.Textures;

namespace GagSpeak.Gui.Components;

public class ToyboxTabs : ImageTabBar<ToyboxTabs.SelectedTab>
{
    // Look, its hard to come up with a proper way to word this, it's very confusing in technical terms.
    public enum SelectedTab
    {
        BuzzToys,
        Patterns,
        Alarms,
    }

    protected override bool IsTabDisabled(SelectedTab tab)
    {
        var disable = true;
#if DEBUG
        disable = false;
#endif
        return disable;
    }

    public ToyboxTabs()
    {
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Vibrator], SelectedTab.BuzzToys,
            "Configure your interactable Sex Toy Devices");
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Stimulated], SelectedTab.Patterns,
            "Create, Edit, and playback patterns " +
            "--SEP----COL--[WIP]--COL--" +
            "--NL--- Correctly Integrate into Remotes" +
            "--NL--- Resolve conflicts with Alarms" +
            "--NL--- Resolve conflict with Personal Remote");
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Clock], SelectedTab.Alarms,
            "Set various Alarms that play patterns when triggered" +
            "--SEP----COL--[WIP]--COL--" +
            "--NL--- Try overlapping on patterns without inturruption");
    }

}
