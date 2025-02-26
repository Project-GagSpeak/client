using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils.Enums;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Hosting;
using GameAction = Lumina.Excel.Sheets.Action;
using StructsPlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace GagSpeak.UpdateMonitoring;

/// <summary> Responsible for handling information revolving around ClientState and interactions from it. </summary>
public class ClientMonitor : IHostedService
{
    private readonly ILogger<ClientMonitor> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly IClientState _clientState;
    private readonly ICondition _condition;
    private readonly IDataManager _gameData;
    private readonly IFramework _framework;
    private readonly IGameGui _gameGui;
    private readonly IPartyList _partyList;
    public ClientMonitor(ILogger<ClientMonitor> logger, GagspeakMediator mediator,
        IClientState clientState, ICondition condition, IDataManager gameData, IFramework framework,
        IGameGui gameGui, IPartyList partyList)
    {
        try
        {
            _logger = logger;
        }
        catch (Exception e)
        {
            GagSpeak.StaticLog.Error("Failed to initialize ClientMonitor" + e.StackTrace);
            throw;
        }
        _mediator = mediator;
        _clientState = clientState;
        _condition = condition;
        _gameData = gameData;
        _framework = framework;
        _gameGui = gameGui;
        _partyList = partyList;

        // Lets just say fuck it and cache things on startup so we dont need to worry about them later.
        ClassJobs = _gameData.GetExcelSheet<ClassJob>().ToList();
        LoadedActions = new Dictionary<int, List<ActionRowLight>>();

        var generalGameActions = _gameData.GetExcelSheet<GameAction>()
            .Where(r => r.IsPlayerAction && r.ClassJob.ValueNullable.HasValue);

        // load all of the actions from the excel sheet that are valid for player actions into the dictionary.
        var battleJobIds = ClassJobs.Where(x => x.Role != 0).Select(x => x.RowId);
        // I hope this doesnt make the plugin initializer cry on people with low PC's.
        var stopwatch = Stopwatch.StartNew();
        foreach (var action in generalGameActions)
        {
            // if the action contains any of the job ids, then append it to the respective id.
            if (battleJobIds.Contains(action.ClassJob.Value.RowId))
            {
                if (LoadedActions.ContainsKey((int)action.ClassJob.Value.RowId))
                    LoadedActions[(int)action.ClassJob.Value.RowId].Add(new ActionRowLight(action));
                else
                    LoadedActions[(int)action.ClassJob.Value.RowId] = new List<ActionRowLight> { new ActionRowLight(action) };
            }
        }
        // Stop the performance timer.
        stopwatch.Stop();

        _logger.LogInformation($"Cached {generalGameActions.Count()} actions in {stopwatch.ElapsedMilliseconds}ms " +
            $"for {ClassJobs.Count()} Jobs.");

        _clientState.ClassJobChanged += OnJobChanged;
        _clientState.Login += OnLogin;
        _clientState.Logout += OnLogout;

        // if the client is already logged in, we need to trigger the login event manually
        if (_clientState.IsLoggedIn)
            OnLogin();
    }

    public readonly struct ActionRowLight
    {
        public readonly int    RowId;
        public readonly string Name;
        public readonly ushort Icon;
        public readonly byte   CooldownGroup;

        public ActionRowLight(GameAction action)
        {
            RowId = (int)action.RowId;
            Name = action.Name.ToString();
            Icon = action.Icon;
            CooldownGroup = action.CooldownGroup;
        }

        public uint GetIconId() => Icon;
    }

    public static IEnumerable<ClassJob> ClassJobs { get; private set; }
    public static IEnumerable<ClassJob> BattleClassJobs => ClassJobs.Where(x => x.Role != 0).ToList();
    public static Dictionary<int, List<ActionRowLight>> LoadedActions { get; private set; }


    public static readonly int MaxLevel = 100;
    public static IntPtr ClientPlayerAddress { get; private set; } = IntPtr.Zero; // Only use this if we run into problems with the normal one.
    public static unsafe short Commondations => StructsPlayerState.Instance()->PlayerCommendations;
    public static unsafe bool IsInDuty => GameMain.Instance()->CurrentContentFinderConditionId is not 0; // alternative method from IDutyState
    public static unsafe bool IsOnIsland => MJIManager.Instance()->IsPlayerInSanctuary is 1;
    public ClientLanguage ClientLanguage => _clientState.ClientLanguage;
    public IPlayerCharacter? ClientPlayer => _clientState.LocalPlayer;
    public bool IsPresent => _clientState.LocalPlayer is not null && _clientState.LocalPlayer.IsValid();
    public async Task<bool> IsPresentAsync() => await RunOnFrameworkThread(() => IsPresent).ConfigureAwait(false);
    public ulong ContentId => _clientState.LocalContentId;
    public async Task<ulong> ContentIdAsync() => await RunOnFrameworkThread(() => ContentId).ConfigureAwait(false);
    public IntPtr Address => _clientState.LocalPlayer?.Address ?? IntPtr.Zero;
    public string Name => _clientState.LocalPlayer.GetName();
    public async Task<string> NameAsync() => await RunOnFrameworkThread(() => Name).ConfigureAwait(false);
    public uint HomeWorldId => _clientState.LocalPlayer.HomeWorldId();
    public async Task<uint> HomeWorldIdAsync() => await RunOnFrameworkThread(() => HomeWorldId).ConfigureAwait(false);
    public string HomeWorldName => _clientState.LocalPlayer.HomeWorldName();

    public byte Level => _clientState.LocalPlayer?.Level ?? 0;
    public bool IsDead => _clientState.LocalPlayer?.IsDead ?? true;
    public ulong ObjectId => _clientState.LocalPlayer?.GameObjectId ?? ulong.MaxValue;
    public ushort ObjectTableIndex => _clientState.LocalPlayer?.ObjectIndex ?? ushort.MaxValue;
    public uint Health => _clientState.LocalPlayer?.CurrentHp ?? 0;
    public ulong TargetObjectId => _clientState.LocalPlayer?.TargetObjectId ?? ulong.MaxValue;

    public bool IsLoggedIn => _clientState.IsLoggedIn;
    public bool InQuestEvent => _condition[ConditionFlag.OccupiedInQuestEvent];
    public bool IsChocoboRacing => _condition[ConditionFlag.ChocoboRacing];
    public bool IsZoning => _condition[ConditionFlag.BetweenAreas] || _condition[ConditionFlag.BetweenAreas51];
    public bool InDungeonDuty => _condition[ConditionFlag.BoundByDuty] || _condition[ConditionFlag.BoundByDuty56] || _condition[ConditionFlag.BoundByDuty95] || _condition[ConditionFlag.InDeepDungeon];
    public bool InPvP => _clientState.IsPvP;
    public bool InGPose => _clientState.IsGPosing;
    public bool InCutscene => !InDungeonDuty && _condition[ConditionFlag.OccupiedInCutSceneEvent] || _condition[ConditionFlag.WatchingCutscene78];
    public bool InMainCity => _gameData.GetExcelSheet<Aetheryte>()?.Any(x => x.IsAetheryte && x.Territory.RowId == _clientState.TerritoryType && x.Territory.Value.TerritoryIntendedUse.Value.RowId is 0) ?? false;
    public string MainCityName => _gameData.GetExcelSheet<Aetheryte>()?.FirstOrDefault(x => x.IsAetheryte && x.Territory.RowId == _clientState.TerritoryType && x.Territory.Value.TerritoryIntendedUse.Value.RowId is 0).PlaceName.ToString() ?? "Unknown";
    public ushort TerritoryId => _clientState.TerritoryType;
    public TerritoryType TerritoryType => _gameData.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(TerritoryId) ?? default;
    public TerritoryIntendedUseEnum TerritoryIntendedUse => (TerritoryIntendedUseEnum)(_gameData.GetExcelSheet<TerritoryType>().GetRowOrDefault(TerritoryId)?.TerritoryIntendedUse.ValueNullable?.RowId ?? default);

    public int PartySize => _partyList.Length;
    public bool InSoloParty => _partyList.Length <= 1 && IsInDuty;

    public void OpenMapWithMapLink(MapLinkPayload mapLink) => _gameGui.OpenMapWithMapLink(mapLink);
    public bool TryGetAction(uint actionId, out GameAction action)
    {
        action = _gameData.GetExcelSheet<GameAction>()!.GetRowOrDefault(actionId) ?? default;
        return action.RowId != 0;
    }

    public ClassJob? GetClientClassJob() => ClassJobs.FirstOrDefault(x => x.RowId == ClientPlayer.ClassJobId());
    public DeepDungeonType? GetDeepDungeonType()
    {
        if (_gameData.GetExcelSheet<TerritoryType>()?.GetRow(_clientState.TerritoryType) is { } territoryInfo)
        {
            return territoryInfo switch
            {
                { TerritoryIntendedUse.Value.RowId: 31, ExVersion.RowId: 0 or 1 } => DeepDungeonType.PalaceOfTheDead,
                { TerritoryIntendedUse.Value.RowId: 31, ExVersion.RowId: 2 } => DeepDungeonType.HeavenOnHigh,
                { TerritoryIntendedUse.Value.RowId: 31, ExVersion.RowId: 4 } => DeepDungeonType.EurekaOrthos,
                _ => null
            };
        }
        return null;
    }

    private void OnJobChanged(uint jobId)
    {
        if (!_clientState.IsLoggedIn) return;
        _mediator.Publish(new JobChangeMessage(jobId));
    }

    private void OnLogin()
    {
        _logger.LogInformation("Logged in");
        unsafe { OnFrameworkService.LastCommendationsCount = StructsPlayerState.Instance()->PlayerCommendations; }
        _mediator.Publish(new DalamudLoginMessage());
    }

    private void OnLogout(int type, int code)
    {
        GagSpeak.StaticLog.Information("Player Logged out from their client with type: " + type + " and code: " + code);
        _mediator.Publish(new DalamudLogoutMessage(type, code));
    }

    /// <summary> 
    /// Helper function to ensure some actions called off the framework thread, 
    /// happen on the framework thread. 
    /// </summary>
    public async Task<T> RunOnFrameworkThread<T>(Func<T> func)
    {
        if (!_framework.IsInFrameworkUpdateThread)
        {
            var result = await _framework.RunOnFrameworkThread(func).ContinueWith((task) => task.Result).ConfigureAwait(false);
            while (_framework.IsInFrameworkUpdateThread)
            {
                _logger.LogTrace("Still on framework");
                await Task.Delay(1).ConfigureAwait(false);
            }
            return result;
        }
        return func.Invoke();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        GagSpeak.StaticLog.Information("Starting ClientMonitor");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        GagSpeak.StaticLog.Information("Stopping ClientMonitor");
        return Task.CompletedTask;
    }
}

public static class PlayerCharacterExtensions
{
    public static nint GetPointer(this IPlayerCharacter? pc) => pc?.Address ?? IntPtr.Zero;
    public static string GetName(this IPlayerCharacter? pc) => pc?.Name.ToString() ?? string.Empty;
    public static uint HomeWorldId(this IPlayerCharacter? pc) => pc?.HomeWorld.Value.RowId ?? 0;
    public static string HomeWorldName(this IPlayerCharacter? pc) => pc?.HomeWorld.Value.Name.ToString() ?? string.Empty;
    public static uint CurrentWorldId(this IPlayerCharacter? pc) => pc?.CurrentWorld.Value.RowId ?? 0;
    public static string CurrentWorldName(this IPlayerCharacter? pc) => pc?.CurrentWorld.Value.Name.ToString() ?? string.Empty;
    public static string NameWithWorld(this IPlayerCharacter? pc) => pc is null ? string.Empty : (pc.Name.ToString() + "@" + pc.HomeWorld.ValueNullable?.Name.ToString());
    public static uint ClassJobId(this IPlayerCharacter? pc) => pc?.ClassJob.Value.RowId ?? 0;
    public static ActionRoles ClassJobRole(this IPlayerCharacter? pc) => (ActionRoles)(pc?.ClassJob.Value.Role ?? 0);
    public static float GetTargetDistance(this IGameObject player, IGameObject target)
    {
        Vector2 position = new(target.Position.X, target.Position.Z);
        Vector2 selfPosition = new(player.Position.X, player.Position.Z);
        return Math.Max(0, Vector2.Distance(position, selfPosition) - target.HitboxRadius - player.HitboxRadius);
    }

}
