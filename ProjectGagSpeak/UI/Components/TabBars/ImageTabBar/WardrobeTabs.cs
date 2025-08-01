using CkCommons.Widgets;
using GagSpeak.Services.Textures;

namespace GagSpeak.Gui.Components;

public class WardrobeTabs : ImageTabBar<WardrobeTabs.SelectedTab>
{
    public enum SelectedTab
    {
        MyRestraints,
        MyRestrictions,
        MyGags,
        MyCursedLoot,
    }

    public WardrobeTabs()
    {
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Restrained], SelectedTab.MyRestraints,
            "Restraints--SEP--Apply, Lock, Unlock, Remove, or Configure your various Restraints");
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.RestrainedArmsLegs], SelectedTab.MyRestrictions,
            "Restrictions--SEP--Apply, Lock, Unlock, Remove, or Configure your various Restrictions");
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged], SelectedTab.MyGags,
            "Gags--SEP--Apply, Lock, Unlock, Remove, or Configure your various Gags");
        AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.CursedLoot], SelectedTab.MyCursedLoot,
            "Cursed Loot--SEP--Configure your Cursed Items, or manage the active Loot Pool.");
    }
}
