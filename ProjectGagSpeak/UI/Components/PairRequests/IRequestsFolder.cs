using GagspeakAPI.Dto.UserPair;

namespace GagSpeak.UI.Components;

/// <summary>
/// Interface for drawing a dropdown section in the list of paired users
/// </summary>
public interface IRequestsFolder
{
    int TotalOutgoing { get; }
    int TotalIncoming { get; }
    bool HasRequests { get; }

    /// <summary>
    /// Draw the header section of the folder.
    /// </summary>
    void Draw();

}
