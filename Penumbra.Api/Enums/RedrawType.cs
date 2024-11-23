namespace Penumbra.Api.Enums;

/// <summary>
/// The way a specific game object shall be redrawn.
/// Actors can be redrawn immediately or after GPose.
/// </summary>
public enum RedrawType
{
    Redraw,
    AfterGPose,
}
