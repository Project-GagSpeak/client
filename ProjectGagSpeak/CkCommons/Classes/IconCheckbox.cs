using GagspeakAPI.Attributes;
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

public class IconCheckboxStimulation(FAI iconHigh, FAI iconMild, FAI iconLight, FAI iconOff, uint colorOn, uint colorOff)
    : FontAwesomeCheckbox<Stimulation>
{
    protected override (FAI? Icon, uint? Color) GetIcon(Stimulation value)
        => value switch
        {
            Stimulation.None => (iconOff, colorOff),
            Stimulation.Light => (iconLight, colorOn),
            Stimulation.Mild => (iconMild, colorOn),
            Stimulation.Heavy => (iconHigh, colorOn),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    protected override Stimulation NextValue(Stimulation value)
        => value switch
        {
            Stimulation.None => Stimulation.Light,
            Stimulation.Light => Stimulation.Mild,
            Stimulation.Mild => Stimulation.Heavy,
            Stimulation.Heavy => Stimulation.None,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    protected override Stimulation PreviousValue(Stimulation value)
        => value switch
        {
            Stimulation.None => Stimulation.Heavy,
            Stimulation.Light => Stimulation.None,
            Stimulation.Mild => Stimulation.Light,
            Stimulation.Heavy => Stimulation.Mild,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
}
