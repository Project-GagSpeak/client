using GagSpeak.PlayerState.Visual;

namespace GagSpeak.UI.Components;
// This class will automate the drawing of checkboxes, buttons, sliders and more used across the various UI elements through a modular approach.
public class OptionDrawer
{
    private readonly UiSharedService _uiShared;

    public OptionDrawer(UiSharedService uiShared)
    {
        _uiShared = uiShared;
    }
}
