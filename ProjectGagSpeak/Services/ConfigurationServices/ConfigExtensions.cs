using GagSpeak.PlayerData.Storage;
using GagSpeak.Services.Configs;

namespace GagSpeak.Services;
public static class ConfigExtensions
{
    public static bool HasValidSetup(this GagspeakConfig configuration)
    {
        return configuration.AcknowledgementUnderstood;
    }

    public static bool HasValidSetup(this ServerStorage configuration)
    {
        return configuration.Authentications.Count > 0;
    }
}

