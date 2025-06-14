using OtterGui.Text.Widget;

namespace GagSpeak.CkCommons.Classes;
public class IconCheckboxEx(FAI icon, uint colorTrue = 0xFF00FF00, uint colorFalse = 0xFF0000FF) : FontAwesomeCheckbox<bool>
{
    protected override (FAI? Icon, uint? Color) GetIcon(bool value)
        => value ? (icon, colorTrue) : (icon, colorFalse);

    protected override bool NextValue(bool value)
        => !value;

    protected override bool PreviousValue(bool value)
        => !value;
}
