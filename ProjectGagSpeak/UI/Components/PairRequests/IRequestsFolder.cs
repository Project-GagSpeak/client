namespace GagSpeak.Gui.Components;

/// <summary>
///     Interface for drawing a dropdown section in the list of paired users
/// </summary>
public interface IRequestsFolder
{
    int TotalOutgoing { get; }
    int TotalIncoming { get; }

    /// <summary>
    ///     Draw the header section of the folder.
    /// </summary>
    void Draw();

}
