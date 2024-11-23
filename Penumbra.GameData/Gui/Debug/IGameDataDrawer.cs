using OtterGui.Services;

namespace Penumbra.GameData.Gui.Debug;

/// <summary> An interface for any class drawing debugging information for data. </summary>
public interface IGameDataDrawer : IUiService
{
    /// <summary> The name of the data collection. </summary>
    public string Label { get; }

    /// <summary> The draw routine. </summary>
    public void Draw();

    /// <summary> Whether the data is available. </summary>
    public bool Disabled { get; }
}
