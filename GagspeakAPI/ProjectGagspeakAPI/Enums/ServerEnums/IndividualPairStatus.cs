namespace GagspeakAPI.Enums;

/// <summary>
/// Enum that determines the state of a current pairing with other clients.
/// Ensures the connection is mutual only when bidirectional.
/// </summary>
public enum IndividualPairStatus
{
    None,
    OneSided,
    Bidirectional
}