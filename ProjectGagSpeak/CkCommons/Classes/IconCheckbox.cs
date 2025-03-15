using Dalamud.Interface;
using OtterGui.Text.Widget;

namespace GagSpeak.CkCommons.Classes;

public class IconCheckboxEx(FontAwesomeIcon icon, uint colorTrue, uint colorFalse) : FontAwesomeCheckbox<bool>
{
    protected override (FontAwesomeIcon? Icon, uint? Color) GetIcon(bool value)
        => value ? (icon, colorTrue) : (icon, colorFalse);

    protected override bool NextValue(bool value)
        => !value;

    protected override bool PreviousValue(bool value)
        => !value;
}
