
using System.Collections.Immutable;

namespace GagSpeak.UI.Components;

/// <summary>
/// Interface for drawing a dropdown section in the list of paired users
/// </summary>
public interface IDrawFolder
{
    int TotalPairs { get; }
    int OnlinePairs { get; }
    IImmutableList<DrawUserPair> DrawPairs { get; }
    void Draw();
}
