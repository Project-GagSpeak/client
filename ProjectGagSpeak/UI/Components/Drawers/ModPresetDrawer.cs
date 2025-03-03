using GagSpeak.PlayerState.Visual;

namespace GagSpeak.UI.Components;
// This class will automate the drawing of checkboxes, buttons, sliders and more used across the various UI elements through a modular approach.
public class ModPresetDrawer
{
    private readonly ModSettingPresetManager _manager;

    public ModPresetDrawer(ModSettingPresetManager manager, CkGui uiShared)
    {
        _manager = manager;
    }
}
