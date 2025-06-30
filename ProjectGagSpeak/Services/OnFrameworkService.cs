using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using GagSpeak.CustomCombos;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI.Utils;
using ImGuiNET;
using Microsoft.Extensions.Hosting;
using OtterGui.Classes;
using Penumbra.GameData.Structs;
using PlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace GagSpeak.Services;

/// <summary>
///     The service responsible for handling framework updates and other Dalamud related services.
/// </summary>
public class OnFrameworkService : DisposableMediatorSubscriberBase, IHostedService
{
    // The list of player characters, to associate their hashes with the player character name and addresses. Useful for indicating if they are visible or not.
    private readonly Dictionary<string, (string Name, nint Address)> _playerCharas;
    private readonly List<string> _notUpdatedCharas = [];

    private DateTime _delayedFrameworkUpdateCheck = DateTime.Now;
    // Tracks the start and endpoints of these transitions / activities.
    private uint _lastZone = 0;
    private bool _sentBetweenAreas = false;
    private bool _isInGpose = false;
    private bool _isInCutscene = false;

    public static short LastCommendationsCount = 0;
    
    // Find a better place for this later i guess.
    public static LazyList<KeyValuePair<ushort, string>> WorldData { get; private set; }
    public static WorldCombo WorldCombo;
    public OnFrameworkService(ILogger<OnFrameworkService> logger, GagspeakMediator mediator) 
        : base(logger, mediator)
    {
        _playerCharas = new(StringComparer.Ordinal);

        WorldData = new(() =>
        {
            return Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.World>(Dalamud.Game.ClientLanguage.English)!
                .Where(w => w.IsPublic && !w.Name.IsEmpty)
                .Select(w => new KeyValuePair<ushort, string>((ushort)w.RowId, w.Name.ToString()))
                .OrderBy(c => c.Value)
                .ToList();
        });
        WorldCombo = new(logger);

        // This should probably be moved somewhere else idealy but whatever for now.
        mediator.Subscribe<TargetPairMessage>(this, (msg) =>
        {
            if (PlayerData.IsInPvP) return;
            var name = msg.Pair.PlayerName;
            if (string.IsNullOrEmpty(name)) return;
            var addr = _playerCharas.FirstOrDefault(f => string.Equals(f.Value.Name, name, StringComparison.Ordinal)).Value.Address;
            if (addr == nint.Zero) return;
            _ = RunOnFrameworkThread(() =>
            {
                Svc.Targets.Target = CreateGameObject(addr);
            }).ConfigureAwait(false);
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting OnFrameworkService");
        Svc.Framework.Update += FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogTrace("Stopping OnFrameworkService");
        Svc.Framework.Update -= FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    #region FrameworkMethods

    /// <summary> Ensures that we are running on the games framework thread. Throws exception if we are not. </summary>
    public void EnsureIsOnFramework()
    {
        if (!Svc.Framework.IsInFrameworkUpdateThread) throw new InvalidOperationException("Can only be run on Framework");
    }

    /// <summary> Create a game object based off its pointer address reference </summary>
    /// <param name="reference">The pointer address of the game object</param>
    /// <returns>ClientState.Objects.Types.GameObject, type of Gameobject</returns>
    public IGameObject? CreateGameObject(nint reference)
    {
        // ensure we are on the framework thread
        EnsureIsOnFramework();
        // then createObjectReference
        return Svc.Objects.CreateObjectReference(reference);
    }

    /// <summary> An asyncronous task that create a game object based on its pointer address.</summary>
    /// <param name="reference">The pointer address of the game object</param>
    /// <returns></returns>Task of Dalamud.Game.ClientState.Objects.Types.GameObject, type of Gameobject</returns>
    public async Task<IGameObject?> CreateGameObjectAsync(nint reference)
    {
        return await RunOnFrameworkThread(() => Svc.Objects.CreateObjectReference(reference)).ConfigureAwait(false);
    }

    public IGameObject? SearchObjectTableById(ulong id)
    {
        EnsureIsOnFramework();
        return Svc.Objects.SearchById(id);
    }

    public async Task<IGameObject?> SearchObjectTableByIdAsync(uint id)
    {
        return await RunOnFrameworkThread(() => Svc.Objects.SearchById(id)).ConfigureAwait(false);
    }


    /// <summary> Get the player character from the object table based on the pointer address</summary>
    public IPlayerCharacter? GetIPlayerCharacterFromObjectTable(IntPtr address)
    {
        EnsureIsOnFramework();
        return (IPlayerCharacter?)Svc.Objects.CreateObjectReference(address);
    }

    /// <summary> Get the player character from the object table based on the pointer address asynchronously</summary>
    public async Task<IPlayerCharacter?> GetIPlayerCharacterFromObjectTableAsync(IntPtr address)
    {
        return await RunOnFrameworkThread(() => (IPlayerCharacter?)Svc.Objects.CreateObjectReference(address)).ConfigureAwait(false);
    }

    public List<IPlayerCharacter> GetObjectTablePlayers()
    {
        EnsureIsOnFramework();
        return Svc.Objects.OfType<IPlayerCharacter>().ToList();
    }

    public async Task<List<IPlayerCharacter>> GetObjectTablePlayersAsync()
    {
        return await RunOnFrameworkThread(GetObjectTablePlayers).ConfigureAwait(false);
    }

    /// <summary> Gets the player name hashed </summary>
    /// <returns> The local player character's name hashed </returns>
    public async Task<string> GetPlayerNameHashedAsync()
    {
        return await RunOnFrameworkThread(() => (PlayerData.Name, (ushort)PlayerData.HomeWorldId).GetHash256()).ConfigureAwait(false);
    }

    /// <summary> Gets the player characters ID of the world they are currently in.</summary>
    /// <returns> a <c>uint</c> type for the ID of the current world.</returns>
    public nint GetIPlayerCharacterFromCachedTableByIdent(string characterName)
    {
        if (_playerCharas.TryGetValue(characterName, out var pchar)) return pchar.Address;
        return nint.Zero;
    }


    /// <summary> Run the task on the framework thread </summary>
    /// <param name="act">an action to run if any</param>
    public async Task RunOnFrameworkThread(Action act)
    {
        if (!Svc.Framework.IsInFrameworkUpdateThread)
        {
            await Svc.Framework.RunOnFrameworkThread(act).ContinueWith((_) => Task.CompletedTask).ConfigureAwait(false);
            while (Svc.Framework.IsInFrameworkUpdateThread) // yield the thread again, should technically never be triggered
            {
                Logger.LogTrace("Still on framework");
                await Task.Delay(1).ConfigureAwait(false);
            }
        }
        else
        {
            act();
        }
    }
    /// <summary> Run the task on the framework thread </summary>
    /// <param name="func">a function to run if any</param>"
    public async Task<T> RunOnFrameworkThread<T>(Func<T> func)
    {
        if (!Svc.Framework.IsInFrameworkUpdateThread)
        {
            var result = await Svc.Framework.RunOnFrameworkThread(func).ContinueWith((task) => task.Result).ConfigureAwait(false);
            while (Svc.Framework.IsInFrameworkUpdateThread) // yield the thread again, should technically never be triggered
            {
                Logger.LogTrace("Still on framework");
                await Task.Delay(1).ConfigureAwait(false);
            }
            return result;
        }

        return func.Invoke();
    }

    /// <summary> Try and find a player by their name hash (ident ((identity))</summary>
    /// <param name="ident">The identity (has) of a player character</param>
    /// <returns>The name and address of the player if found (not sure how it finds it)</returns>
    internal (string Name, nint Address) FindPlayerByNameHash(string ident)
    {
        _playerCharas.TryGetValue(ident, out var result);
        return result;
    }

    /// <summary>
    ///     Run An action on the Framework Delayed by a set number of ticks. <para />
    ///     
    ///     Because this is a delayed task, there is a change that after the time this is called,
    ///     upon closing the game, the framework service has been unloaded. <para />
    ///     
    ///     This means that the tick we should execute our function may run it on an unloaded framework,
    ///     throwing a task cancellation exception. So we must catch it.
    /// </summary>
    public async Task RunOnFrameworkTickDelayed(Action act, int ticks)
    {
        try
        {
            await Svc.Framework.RunOnTick(() => act(), delayTicks: ticks);
        }
        catch (TaskCanceledException) { /* CONSUME */ }
        catch (Exception ex)
        {
            // Otherwise, log the exception.
            Logger.LogError($"Safely caught an exception during the delayed framework call by {ticks} ticks: {ex}");
        }
    }

    /// <summary> The method that is called when the framework updates </summary>
    private void FrameworkOnUpdate(IFramework framework) => FrameworkOnUpdateInternal();
    #endregion FrameworkMethods
    /// <summary> the unsafe internal framework update method </summary>
    private unsafe void FrameworkOnUpdateInternal()
    {
        // If the local player is dead or null, return after setting the hasDied flag to true
        if (!PlayerData.Available)
            return;

        // we need to update our stored player characters to know if they are still valid, and to update our pair handlers
        // Begin by adding the range of existing player character keys
        var playerCharacters = Svc.Objects.OfType<IPlayerCharacter>().ToList();
        _notUpdatedCharas.AddRange(_playerCharas.Keys);

        // for each object in the renderable object table
        foreach (var chara in playerCharacters)
        {
            var charaName = chara.Name.ToString();
            var hash = (charaName, ((BattleChara*)chara.Address)->Character.HomeWorld).GetHash256();

            _notUpdatedCharas.Remove(hash);
            _playerCharas[hash] = (charaName, chara.Address);
        }

        foreach (var notUpdatedChara in _notUpdatedCharas)
            _playerCharas.Remove(notUpdatedChara);

        // clear the list of not updated characters
        _notUpdatedCharas.Clear();

        // check if we are in the middle of a delayed framework update
        var isNormalFrameworkUpdate = DateTime.Now < _delayedFrameworkUpdateCheck.AddSeconds(1);

        if (PlayerData.InCutscene && !_isInCutscene)
        {
            Logger.LogDebug("Cutscene start");
            _isInCutscene = true;
            Mediator.Publish(new CutsceneBeginMessage());
        }
        else if (!PlayerData.InCutscene && _isInCutscene)
        {
            Logger.LogDebug("Cutscene end");
            _isInCutscene = false;
            Mediator.Publish(new CutsceneEndMessage());
        }

        if (PlayerData.IsInGPose && !_isInGpose)
        {
            Logger.LogDebug("Gpose start");
            _isInGpose = true;
            Mediator.Publish(new GPoseStartMessage());
        }
        else if (!PlayerData.IsInGPose && _isInGpose)
        {
            Logger.LogDebug("Gpose end");
            _isInGpose = false;
            Mediator.Publish(new GPoseEndMessage());
        }

        // if we are zoning, 
        if (PlayerData.IsZoning)
        {
            // get the zone
            var zone = PlayerContent.TerritoryID;
            // if the zone is different from the last zone
            if (_lastZone != zone)
            {
                // set the last zone to the current zone
                _lastZone = zone;
                // if we are marked as not sent between areas
                if (!_sentBetweenAreas)
                {
                    // we know we are starting a zone switch, so publish it to the mediator and set sent between areas to true
                    Logger.LogDebug("Zone switch start");
                    _sentBetweenAreas = true;
                    Mediator.Publish(new ZoneSwitchStartMessage(_lastZone));
                }
            }
            // do an early return so we dont hit the sentBetweenAreas conditional below
            return;
        }

        // this is called while are zoning between areas has ended
        if (_sentBetweenAreas)
        {
            Logger.LogDebug("Zone switch end");
            _sentBetweenAreas = false;
            Mediator.Publish(new ZoneSwitchEndMessage());
            // if our commendation count is different, update it and invoke the event with the difference.
            var newCommendations = PlayerState.Instance()->PlayerCommendations;
            if (newCommendations != LastCommendationsCount)
            {
                Logger.LogDebug("Our Previous Commendation Count was: " + LastCommendationsCount + " and our new commendation count is: " + newCommendations);
                // publish to mediator if we are logged in
                if (PlayerData.IsLoggedIn)
                    Mediator.Publish(new CommendationsIncreasedMessage(newCommendations - LastCommendationsCount));
                // update the count
                LastCommendationsCount = newCommendations;

            }
        }

        // publish the framework update message
        Mediator.Publish(new FrameworkUpdateMessage());

        // if this is a normal framework update, then return
        if (isNormalFrameworkUpdate)
            return;

        var localPlayer = PlayerData.Object;

        // check if we are at 1 hp, if so, grant the boundgee jumping achievement.
        if (localPlayer?.CurrentHp is 1)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.ClientOneHp);

        // push the delayed framework update message to the mediator for things like the UI and the online player manager
        Mediator.Publish(new DelayedFrameworkUpdateMessage());
        // set the latest framework updatecheck
        _delayedFrameworkUpdateCheck = DateTime.Now;
    }
}

