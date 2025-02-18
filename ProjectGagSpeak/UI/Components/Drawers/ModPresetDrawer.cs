using GagSpeak.PlayerState.Visual;

namespace GagSpeak.UI.Components;
public class ModPresetDrawer
{
    private readonly ModSettingPresetManager _modSettingManager;
    private readonly UiSharedService _uiShared;

    public ModPresetDrawer(ModSettingPresetManager manager, UiSharedService uiShared)
    {
        _modSettingManager = manager;
        _uiShared = uiShared;
    }
}
