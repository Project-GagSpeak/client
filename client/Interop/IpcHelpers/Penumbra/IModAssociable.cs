namespace GagSpeak.Interop.IpcHelpers.Moodles;

public interface IModAssociable
{
    List<Guid> AssociatedMoodles { get; set; }
    Guid AssociatedMoodlePreset { get; set; }
}
