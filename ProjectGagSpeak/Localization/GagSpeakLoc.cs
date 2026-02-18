using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Tutorial;
using Microsoft.Extensions.Hosting;

namespace GagSpeak;

/// <summary>
/// The service responsible for handling framework updates and other Dalamud related services.
/// </summary>
public class GagSpeakLoc : IHostedService
{
    private readonly ILogger<GagSpeakLoc> _logger;
    private readonly Dalamud.Localization _localization;
    private readonly MainConfig _mainConfig;
    private readonly TutorialService _tutorialService;

    public GagSpeakLoc(ILogger<GagSpeakLoc> logger, Dalamud.Localization localization, MainConfig config, TutorialService tutorial)
    {
        _logger = logger;
        _localization = localization;
        _mainConfig = config;
        _tutorialService = tutorial;
    }

    private void LoadLocalization(string languageCode)
    {
        _logger.LogInformation($"Loading Localization for {languageCode}");
        _localization.SetupWithLangCode(languageCode);
        GSLoc.ReInitialize();
        // re-initialize tutorial strings.
        _tutorialService.InitializeTutorialStrings();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting GagSpeak Localization Service.");
        _localization.SetupWithLangCode(Svc.PluginInterface.UiLanguage);
        GSLoc.ReInitialize();
        // load tutorial strings.
        _tutorialService.InitializeTutorialStrings();

        // subscribe to any localization changes.
        Svc.PluginInterface.LanguageChanged += LoadLocalization;
        _logger.LogInformation("GagSpeak Localization Service started successfully and loaded " + Svc.PluginInterface.UiLanguage + " as language.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping GagSpeak Localization Service.");
        Svc.PluginInterface.LanguageChanged -= LoadLocalization;
        return Task.CompletedTask;
    }
}
