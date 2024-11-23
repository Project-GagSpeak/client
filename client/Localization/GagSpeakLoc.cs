using Dalamud.Plugin;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.Localization;
using GagSpeak.Services.Tutorial;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.UpdateMonitoring;

/// <summary>
/// The service responsible for handling framework updates and other Dalamud related services.
/// </summary>
public class GagSpeakLoc : IDisposable, IHostedService
{
    private readonly ILogger<GagSpeakLoc> _logger;
    private readonly Dalamud.Localization _localization;
    private readonly GagspeakConfigService _mainConfig;
    private readonly TutorialService _tutorialService;
    private readonly IDalamudPluginInterface _pi;

    public GagSpeakLoc(ILogger<GagSpeakLoc> logger, Dalamud.Localization localization,
        GagspeakConfigService configService, TutorialService tutorialService, IDalamudPluginInterface pi)
    {
        _logger = logger;
        _localization = localization;
        _mainConfig = configService;
        _tutorialService = tutorialService;
        _pi = pi;

        // load tutorial strings.
        _tutorialService.InitializeTutorialStrings();

        // subscribe to any localization changes.
        _pi.LanguageChanged += LoadLocalization;
    }

    public void Dispose()
    {
        _pi.LanguageChanged -= LoadLocalization;
    }

    private void LoadLocalization(string languageCode)
    {
        _logger.LogInformation($"Loading Localization for {languageCode}");
        _localization.SetupWithLangCode(languageCode);
        GSLoc.ReInitialize();

        // Update our forced stay entries as well.
        _mainConfig.Current.ForcedStayPromptList.CheckAndInsertRequired();
        _mainConfig.Current.ForcedStayPromptList.PruneEmpty();
        _mainConfig.Save();
        // re-initialize tutorial strings.
        _tutorialService.InitializeTutorialStrings();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting GagSpeak Localization Service.");
        _localization.SetupWithLangCode(_pi.UiLanguage);
        GSLoc.ReInitialize();

        // Update our forced stay entries as well.
        _mainConfig.Current.ForcedStayPromptList.CheckAndInsertRequired();
        _mainConfig.Current.ForcedStayPromptList.PruneEmpty();
        _mainConfig.Save();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping GagSpeak Localization Service.");
        return Task.CompletedTask;
    }
}
