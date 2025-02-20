using GagSpeak.PlayerState.Visual;

namespace GagSpeak.UI.Components;
// This class will automate the drawing of checkboxes, buttons, sliders and more used across the various UI elements through a modular approach.
public class ModPresetDrawer
{
    private readonly ModSettingPresetManager _manager;
    private readonly UiSharedService _uiShared;

    public ModPresetDrawer(ModSettingPresetManager manager, UiSharedService uiShared)
    {
        _manager = manager;
        _uiShared = uiShared;
    }
}
