namespace GagSpeak.Interop.IpcHelpers.Moodles;

public interface IMoodlesAssociable
{
    List<Guid> AssociatedMoodles { get; set; }
    Guid AssociatedMoodlePreset { get; set; }
}
