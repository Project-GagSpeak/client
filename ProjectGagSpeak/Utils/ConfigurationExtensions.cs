using GagSpeak.GagspeakConfiguration.Configurations;

namespace GagSpeak.GagspeakConfiguration;

/// <summary>
/// This class is used to verify that someone who has installed the plugin has agreed
/// to the terms and has completed the initial scan.
/// </summary>
public static class ConfigurationExtensions
{
    public static bool HasValidSetup(this GagspeakConfig configuration)
    {
        return configuration.AcknowledgementUnderstood && configuration.AccountCreated;
    }
}
