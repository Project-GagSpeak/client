namespace GagSpeak.PlayerState.Components;

/// <summary> Handles the modification of the stored data for the player state. </summary>
public interface IVisualManager
{
    public void LoadServerData();
    public void CleanupData();
}
