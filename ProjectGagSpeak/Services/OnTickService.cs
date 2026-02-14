using CkCommons;
using Dalamud.Game.ClientState;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Hosting;
using Penumbra.GameData.Structs;
using TerraFX.Interop.Windows;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.DataCenterHelper;
using LuminaWorld = Lumina.Excel.Sheets.World;

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

    public static readonly Dictionary<ResidentialArea, string> ResidentialNames = new()
    {
        [ResidentialArea.None] = "None",
        [ResidentialArea.LavenderBeds] = "Lavender Beds",
        [ResidentialArea.Mist] = "Mist",
        [ResidentialArea.Goblet] = "Goblet",
        [ResidentialArea.Shirogane] = "Shirogane",
        [ResidentialArea.Empyreum] = "Empyreum",
    };

    public OnTickService(ILogger<OnTickService> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

        // Conditions we want to track.
    public static unsafe short  Commendations = PlayerState.Instance()->PlayerCommendations;
    public static bool          InGPose     { get; private set; } = false;
    public static bool          InCutscene  { get; private set; } = false;
    // Maybe an IsInitialized here to make sure that we have valid data.
    public static LocationEntry Previous    { get; private set; } = new LocationEntry();
    public static LocationEntry Current     { get; private set; } = new LocationEntry();
    public static byte          DataCenterId { get; private set; } = 0;
    public static ushort        WorldId     { get; private set; } = 0;

    public static string CurrZoneName => Current.TerritoryName;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting OnFrameworkService");
        Svc.Framework.Update += OnTick;
        Svc.ClientState.Login += SetInitialData;
        Svc.ClientState.Logout += OnLogout;
        Svc.ClientState.ZoneInit += OnZoneInit;

        if (Svc.ClientState.IsLoggedIn)
            SetInitialData();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Stopping OnFrameworkService");
        Svc.Framework.Update -= OnTick;
        Svc.ClientState.Login -= SetInitialData;
        Svc.ClientState.Logout -= OnLogout;
        Svc.ClientState.ZoneInit -= OnZoneInit;

        return Task.CompletedTask;
    }

    private async void SetInitialData()
    {
        await GagspeakEx.WaitForPlayerLoading();
        // Initialize the DC & World to avoid unessisary extra wait on future zone changes.
        DataCenterId = (byte)PlayerData.CurrentDataCenter.RowId;
        WorldId = PlayerData.CurrentWorldId;
        // Then update the current zone for the area.
        Current = GetEntryForArea();
        _mediator.Publish(new TerritoryChanged(0, Current.TerritoryId));
    }

    private async void OnLogout(int type, int code)
    {
        // Clear location data.
        Previous = new LocationEntry();
        Current = new LocationEntry();
    }

    public void TriggerUpdate()
        => _mediator.Publish(new TerritoryChanged(Previous.TerritoryId, Current.TerritoryId));


    private async void OnZoneInit(ZoneInitEventArgs args)
    {
        // Ignore territories from login zone / title screen (if any even exist)
        if (!Svc.ClientState.IsLoggedIn)
            return;

        // Upon a zone init, we are always not loaded in. As such, await for player to continue processing.
        _logger.LogInformation($"Zone initialized: {args.ToString()}");
        _logger.LogDebug($"Territory changed to: {args.TerritoryType.RowId} ({PlayerContent.GetTerritoryName((ushort)args.TerritoryType.RowId)})");
        Previous = Current;

        // Await for the player to be loaded.
        // This also ensures that by the time this is fired, all visible users will also be visible.
        await GagspeakEx.WaitForPlayerLoading();
        _logger.LogDebug("Player Finished Loading, updating location data.");
        Current = GetEntryForArea();
        _mediator.Publish(new TerritoryChanged(Previous.TerritoryId, Current.TerritoryId));

        // Also note the differences in other things like commendations
        unsafe
        {
            var newCommendations = PlayerState.Instance()->PlayerCommendations;
            if (newCommendations != Commendations)
            {
                _logger.LogDebug($"Commendations changed from {Commendations} to {newCommendations} on zone change.");
                if (PlayerData.IsLoggedIn)
                    _mediator.Publish(new CommendationsIncreasedMessage(newCommendations - Commendations));
                Commendations = newCommendations;
            }
        }

        // Reset cutscene and gpose states on territory change.
        InCutscene = false;
        InGPose = false;
    }

    private ResidentialArea GetAreaByTerritory(uint id)
        => Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(id) is { } territory ? GetAreaByRowRef(territory) : ResidentialArea.None;

    private ResidentialArea GetAreaByRowRef(TerritoryType territory)
        => territory.PlaceNameRegion is { } placeRegion
        ? placeRegion.RowId switch
        {
            2402 => ResidentialArea.Shirogane,
            25 => ResidentialArea.Empyreum,
            23 => ResidentialArea.LavenderBeds,
            24 => ResidentialArea.Goblet,
            22 => ResidentialArea.Mist,
            _ => ResidentialArea.None,
        } : ResidentialArea.None;


    public unsafe LocationEntry GetEntryForArea()
    {
        var entry = new LocationEntry()
        {
            DataCenterId = DataCenterId,
            WorldId = WorldId,
            IntendedUse = PlayerContent.TerritoryIntendedUse,
            TerritoryId = PlayerContent.TerritoryIdInstanced,
        };
        try
        {
            var houseMgr = HousingManager.Instance();
            var housingType = houseMgr->GetCurrentHousingTerritoryType();
            entry.HousingType = housingType;
            if (housingType != HousingTerritoryType.None)
            {
                // Get the housing area.
                entry.HousingArea = GetAreaByTerritory(entry.TerritoryId);
                // Get the housing details.
                entry.Ward = houseMgr->GetCurrentWard();
                entry.Plot = houseMgr->GetCurrentPlot();
                entry.ApartmentDivision = houseMgr->GetCurrentDivision();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get location entry: {ex}");
        }
        return entry;
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

        // Placeholder while we figure out hardcore handling further after 2.0 launch.
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

#region Location Tracking
/// <summary>
///     Determines how a location entry will match with another.
/// </summary>
public enum LocationScope : sbyte
{
    None = 0,
    DataCenter = 1,
    World = 2,
    IntendedUse = 3,
    Territory = 4,
    HousingDistrict = 5,
    HousingWard = 6,
    HousingPlot = 7,
    Indoor = 8,
}

public enum ResidentialArea : sbyte
{
    None = 0,
    LavenderBeds = 1,
    Mist = 2,
    Goblet = 3,
    Shirogane = 4,
    Empyreum = 5,
}

// Can make interchangable with AddressBookEntry maybe, or maybe not...
public class LocationEntry
{
    public byte DataCenterId = 0;
    public ushort WorldId = 0;
    public IntendedUseEnum IntendedUse = (IntendedUseEnum)byte.MaxValue;
    public ushort TerritoryId = 0;
    // Housing (This would be indoors if the scope was indoors)
    public HousingTerritoryType HousingType = HousingTerritoryType.None;
    public ResidentialArea HousingArea = ResidentialArea.None;
    public sbyte Ward = 0; // Always -1 the actual plot value. (0 == ward 1)
    public sbyte Plot = 0; // Always -1 the actual plot value. (0 == plot 1)
    // public short RoomNumber = -1;
    public byte ApartmentDivision = 0;

    // Helpers.
    [JsonIgnore] public RowRef<WorldDCGroupType> DataCenter => PlayerData.CreateRef<WorldDCGroupType>(DataCenterId);
    [JsonIgnore] public string DataCenterName => DataCenter.ValueNullable?.Name.ToString() ?? "Unkown DC";
    [JsonIgnore] public RowRef<LuminaWorld> World => PlayerData.CreateRef<LuminaWorld>(WorldId);
    [JsonIgnore] public string WorldName => World.ValueNullable?.Name.ToString() ?? "Unknown World";
    [JsonIgnore] public RowRef<TerritoryType> Territory => PlayerData.CreateRef<TerritoryType>(TerritoryId);
    [JsonIgnore] public string TerritoryName => PlayerContent.GetTerritoryName(TerritoryId);
    [JsonIgnore] public bool IsInHousing => HousingType != HousingTerritoryType.None;
    [JsonIgnore] public bool IsIndoors => HousingType is HousingTerritoryType.Indoor;

    public LocationEntry Clone()
        => new LocationEntry()
        {
            DataCenterId = DataCenterId,
            WorldId = WorldId,
            IntendedUse = IntendedUse,
            TerritoryId = TerritoryId,
            HousingType = HousingType,
            HousingArea = HousingArea,
            Ward = Ward,
            Plot = Plot,
            ApartmentDivision = ApartmentDivision,
        };
}
#endregion Location Tracking
