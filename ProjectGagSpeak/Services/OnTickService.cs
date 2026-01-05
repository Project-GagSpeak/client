using CkCommons;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services;

/// <summary>
///     Only value is for delayed framework updates and other small things now. 
///     Has no other purpose.
/// </summary>
public class OnTickService : IHostedService
{
    private readonly ILogger<OnTickService> _logger;
    private readonly GagspeakMediator _mediator;

    private DateTime _delayedFrameworkUpdateCheck = DateTime.Now;
    
    // Conditions we want to track.
    public static unsafe short  Commendations = PlayerState.Instance()->PlayerCommendations;
    public static bool          InGPose     { get; private set; } = false;
    public static bool          InCutscene  { get; private set; } = false;
    public static ushort        CurrZone    { get; private set; } = 0;

    public OnTickService(ILogger<OnTickService> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public static string CurrZoneName => PlayerContent.GetTerritoryName(CurrZone);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting OnFrameworkService");
        Svc.Framework.Update += OnTick;
        Svc.ClientState.Login += OnLogin;
        Svc.ClientState.TerritoryChanged += OnTerritoryChanged;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Stopping OnFrameworkService");
        Svc.Framework.Update -= OnTick;
        Svc.ClientState.Login -= OnLogin;
        Svc.ClientState.TerritoryChanged -= OnTerritoryChanged;
        return Task.CompletedTask;
    }

    private async void OnLogin()
    {
        await GsExtensions.WaitForPlayerLoading();
        CurrZone = PlayerContent.TerritoryIdInstanced;
        _mediator.Publish(new TerritoryChanged(0, CurrZone));
    }

    private unsafe void OnTerritoryChanged(ushort newTerritory)
    {
        // Ignore territories from login zone / title screen (if any even exist)
        if (!Svc.ClientState.IsLoggedIn)
            return;

        // Could make this samethread if we want idk.
        _mediator.Publish(new TerritoryChanged(CurrZone, newTerritory));

        var newCommendations = PlayerState.Instance()->PlayerCommendations;
        if (newCommendations != Commendations)
        {
            _logger.LogDebug($"Commendations changed from {Commendations} to {newCommendations} on zone change.");
            if (PlayerData.IsLoggedIn)
                _mediator.Publish(new CommendationsIncreasedMessage(newCommendations - Commendations));
            Commendations = newCommendations;
        }

        // Reset cutscene and gpose states on territory change.
        InCutscene = false;
        InGPose = false;
    }


    private void OnTick(IFramework framework)
    {
        if (!PlayerData.Available)
            return;

        // Can just process some basic stuff and then notify the mediators.
        var isNormal = DateTime.Now < _delayedFrameworkUpdateCheck.AddSeconds(1);

        // Check for cutscene changes, but there is probably an event for this somewhere.
        if (PlayerData.InCutscene && !InCutscene)
        {
            _logger.LogDebug("Cutscene start");
            InCutscene = true;
            _mediator.Publish(new CutsceneBeginMessage());
        }
        else if (!PlayerData.InCutscene && InCutscene)
        {
            _logger.LogDebug("Cutscene end");
            InCutscene = false;
            _mediator.Publish(new CutsceneEndMessage());
        }

        // Check for GPose changes (this also is likely worthless.
        if (PlayerData.InGPose && !InGPose)
        {
            _logger.LogDebug("Gpose start");
            InGPose = true;
            _mediator.Publish(new GPoseStartMessage());
        }
        else if (!PlayerData.InGPose && InGPose)
        {
            _logger.LogDebug("Gpose end");
            InGPose = false;
            _mediator.Publish(new GPoseEndMessage());
        }

        _mediator.Publish(new FrameworkUpdateMessage());

        if (isNormal)
            return;

        // check if we are at 1 hp, if so, grant the boundgee jumping achievement.
        if (PlayerData.CurrentHp is 1)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.ClientOneHp);

        _mediator.Publish(new DelayedFrameworkUpdateMessage());
        _delayedFrameworkUpdateCheck = DateTime.Now;
    }
}
