using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Achievements;
using GagSpeak.GameInternals.Addons;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Services;

// possibly remove this if we dont end up having lots if need for a framework monitor update.
// if its so small, just add it to framework updates.
public class AchievementsService : DisposableMediatorSubscriberBase
{
    private readonly PlayerData _player;
    private readonly OnFrameworkService _frameworkUtils;

    public AchievementsService(ILogger<AchievementsService> logger, GagspeakMediator mediator,
        PlayerData clientMonitor, OnFrameworkService frameworkUtils) : base(logger, mediator)
    {
        _player = clientMonitor;
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

        if(_player.Health is 0 && !ClientIsDead)
        {
            UnlocksEventManager.AchievementEvent(UnlocksEvent.ClientSlain);
            ClientIsDead = true;
        }
        else if (_player.Health is not 0 && ClientIsDead)
            ClientIsDead = false;

        // check if in gold saucer (maybe do something better for this later.
        if (_player.TerritoryId is 144)
        {
            // Check Chocobo Racing Achievement.
            if (_player.IsChocoboRacing)
            {
                var resultMenu = (AtkUnitBase*)AtkHelper.GetAddonByName("RaceChocoboResult");
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
            .Where(player => player != _player.ClientPlayer
            && Vector3.Distance(_player.ClientPlayer?.Position ?? default, player.Position) < 30f)
            .Count();

        if(playersInRange != _lastPlayerCount)
        {
            Logger.LogTrace("(New Update) There are " + playersInRange + " Players nearby", LoggerType.AchievementInfo);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PlayersInProximity, playersInRange);
            _lastPlayerCount = playersInRange;
        }
    }
}
