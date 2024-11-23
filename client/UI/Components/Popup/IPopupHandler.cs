using System.Numerics;

namespace GagSpeak.UI.Components.Popup;

/// <summary> A interface for handling the popups in the UI. </summary>
public interface IPopupHandler
{
    /// <summary>
    /// The Size that the Pop-Up should be.
    /// </summary>
    Vector2 PopupSize { get; }

    /// <summary>
    /// If a Closed button should be shown.
    /// </summary>
    bool ShowClosed { get; }
    /// <summary>
    /// For custom displays, this lets us know if the close button is hovered.
    /// </summary>
    bool CloseHovered { get; set; }

    /// <summary>
    /// Spesifies the rounding measurement for the window.
    /// </summary>
    float? WindowRounding { get; }

    /// <summary>
    /// Specifies the padding for the window.
    /// </summary>
    Vector2? WindowPadding { get; }

    /// <summary>
    /// Required to draw the popup contents.
    /// </summary>
    void DrawContent();
}
