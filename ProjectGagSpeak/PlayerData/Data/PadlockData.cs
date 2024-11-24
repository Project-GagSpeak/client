using GagspeakAPI.Enums;

namespace GagSpeak.PlayerData.Data;
public struct PadlockData // good for transferring across mediators.
{
    public GagLayer Layer;
    public Padlocks PadlockType;
    public string Password;
    public DateTimeOffset Timer;
    public string Assigner;

    public PadlockData(GagLayer layer, Padlocks padlockType, string password, DateTimeOffset timer, string assigner)
    {
        Layer = layer;
        PadlockType = padlockType;
        Password = password;
        Timer = timer;
        Assigner = assigner;
    }
}
