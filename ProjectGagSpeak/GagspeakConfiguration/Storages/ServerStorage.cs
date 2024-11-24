namespace GagSpeak.GagspeakConfiguration.Models;

[Serializable]
public class ServerStorage
{
    public List<Authentication> Authentications { get; set; } = []; // the authentications we have for this client
    public bool FullPause { get; set; } = false;                    // if client is disconnected from the server (not integrated yet)
    public bool ToyboxFullPause { get; set; } = false;               // if client is disconnected from the toybox server (not integrated yet)
    public string ServerName { get; set; } = string.Empty;          // name of the server client is connected to
    public string ServiceUri { get; set; } = string.Empty;           // address of the server the client is connected to
}
