using CkCommons.Widgets;
using GagSpeak.Services.Textures;

namespace GagSpeak.Gui.Components;

public class ToyboxTabs : ImageTabBar<ToyboxTabs.SelectedTab>
{
    // Look, its hard to come up with a proper way to word this, it's very confusing in technical terms.
    public enum SelectedTab
    {
        BuzzToys,
        VibeLobbies,
        Patterns,
        Alarms,
        Triggers
    }

    protected override bool IsTabDisabled(SelectedTab tab)
    {
        var disable = tab is SelectedTab.VibeLobbies;
#if DEBUG
        disable = false;
#endif
        return disable;
    }

    public ToyboxTabs()
    {
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Vibrator], SelectedTab.BuzzToys,
            "Configure your interactable Sex Toy Devices");
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.VibeLobby], SelectedTab.VibeLobbies,
            "Invite, Join, or create Vibe Rooms to play with others");
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Stimulated], SelectedTab.Patterns,
            "Create, Edit, and playback patterns");
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Clock], SelectedTab.Alarms,
            "Set various Alarms that play patterns when triggered");
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.CircleDot], SelectedTab.Triggers,
            "Create various kinds of Triggers");
    }

}
