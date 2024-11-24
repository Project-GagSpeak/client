using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using System.Numerics;

namespace GagSpeak.Achievements.Services;

// possibly remove this if we dont end up having lots if need for a framework monitor update.
// if its so small, just add it to framework updates.
public class AchievementsService : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ClientMonitorService _clientService;
    private readonly OnFrameworkService _frameworkUtils;

    public AchievementsService(ILogger<AchievementsService> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, ClientMonitorService clientService,
        OnFrameworkService frameworkUtils) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _clientService = clientService;
        _frameworkUtils = frameworkUtils;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, _ => CheckAchievementConditions());
    }

    DateTime _lastCheck = DateTime.UtcNow;
    DateTime _lastPlayerCheck = DateTime.UtcNow;
    int _lastPlayerCount = 0;
    bool ClientIsDead = false;

    private unsafe void CheckAchievementConditions()
    {
        // only process this every 5 seconds.
        if ((DateTime.UtcNow - _lastCheck).TotalSeconds < 5)
            return;

        _lastCheck = DateTime.UtcNow;

        if(_clientService.Health is 0 && !ClientIsDead)
        {
            UnlocksEventManager.AchievementEvent(UnlocksEvent.ClientSlain);
            ClientIsDead = true;
        }
        else if (_clientService.Health is not 0 && ClientIsDead)
            ClientIsDead = false;

        // check if in gold saucer (maybe do something better for this later.
        if (_clientService.TerritoryId is 144)
        {
            // Check Chocobo Racing Achievement.
            if (_clientService.IsChocoboRacing)
            {
                var resultMenu = (AtkUnitBase*)AtkFuckery.GetAddonByName("RaceChocoboResult");
                if (resultMenu != null)
                {
                    if (resultMenu->RootNode->IsVisible())
                        UnlocksEventManager.AchievementEvent(UnlocksEvent.ChocoboRaceFinished);
                }
            }
        }

        // if 15 seconds has passed since the last player check, check the player.
        if ((DateTime.UtcNow - _lastPlayerCheck).TotalSeconds < 15)
            return;

        // update player count
        _lastPlayerCheck = DateTime.UtcNow;

        // we should get the current player object count that is within the range required for crowd pleaser.
        var playersInRange = _frameworkUtils.GetObjectTablePlayers()
            .Where(player => player != _clientService.ClientPlayer
            && Vector3.Distance(_clientService.ClientPlayer?.Position ?? default, player.Position) < 30f)
            .Count();

        if(playersInRange != _lastPlayerCount)
        {
            Logger.LogTrace("(New Update) There are " + playersInRange + " Players nearby", LoggerType.AchievementInfo);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PlayersInProximity, playersInRange);
            _lastPlayerCount = playersInRange;
        }
    }
}
