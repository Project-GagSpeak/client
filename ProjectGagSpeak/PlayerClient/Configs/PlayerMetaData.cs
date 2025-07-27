using CkCommons;
using CkCommons.HybridSaver;
using GagSpeak.Interop.Helpers;
using GagSpeak.Services.Configs;
using GagspeakAPI.Data;

namespace GagSpeak.State.Managers;

/// <summary>
///     Contains all the sneaky client-side data for each player.
/// </summary>
public class PlayerMetaData : IHybridSavable
{
    private readonly HybridSaveService _saver;
    [JsonIgnore] public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    [JsonIgnore] public HybridSaveType SaveType => HybridSaveType.Json;
    public int ConfigVersion => 0;
    public string GetFileName(ConfigFileProvider files, out bool upa) 
        => (upa = true, files.MetaData).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        // take the metadata, and serialize it. (Only do this if we need to!)
        //var json = JsonConvert.SerializeObject(_metaData);
        //var compressed = json.Compress(6);
        //var base64Data = Convert.ToBase64String(compressed);
        // store that string as the MetaData's Value.
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["MetaData"] = JObject.FromObject(_metaData),
        }.ToString(Formatting.Indented);

        // compress and write out to the file.
    }

    public PlayerMetaData(HybridSaveService saver)
    {
        _saver = saver;
    }

    public void Save() 
        => _saver.Save(this);
    public void Load()
    {
        var file = _saver.FileNames.MetaData;
        Svc.Logger.Information("Loading in Config for file: " + file);

        try
        {
            // If no file exists yet, save a new instance.
            if (!File.Exists(file))
            {
                Svc.Logger.Warning($"No MetaData found for current User. Creating fresh file!" +
                    "IF YOU ARE SEEING THIS AFTER GENERATING A FILE, YOUR METASTATE IS BROKEN. " +
                    "I am not responcible for any achievement progress lost by this!");
                // create a new file with default values.
                _saver.Save(this);
                return;
            }
            else
            {
                var jsonText = File.ReadAllText(file);
                var jObject = JObject.Parse(jsonText);

                // Read the json from the file.
                var version = jObject["Version"]?.Value<int>() ?? 0;
                _metaData = jObject["MetaData"]?.ToObject<MetaDataState>() ?? new MetaDataState();

                //// Load and decode the compressed, encoded metadata string
                //var encodedData = jObject["MetaData"]?.Value<string>();
                //if (!string.IsNullOrEmpty(encodedData))
                //{
                //    var bytes = Convert.FromBase64String(encodedData);
                //    var ver = bytes[0];
                //    ver = bytes.DecompressToString(out var decompressed);
                //    _metaData = JsonConvert.DeserializeObject<MetaDataState>(decompressed) ?? new MetaDataState();
                //}
                //else
                //{
                //    Svc.Logger.Warning("No valid MetaData found in file. Initializing default state.");
                //    _metaData = new MetaDataState();
                //}
                Svc.Logger.Information("Config loaded.");
                Save();
            }
        }
        catch (Bagagwa ex)
        {
            Svc.Logger.Error("Failed to load config." + ex);
        }
    }

    private MetaDataState _metaData { get; set; } = new();

    /// <summary> Readonly accessor for metadata. </summary>
    public IReadonlyMetaData MetaData => _metaData;

    /// <summary> Updates the confinement Entry. </summary>
    public void SetConfinementAddress(AddressBookEntry entry)
    {
        _metaData.ConfinementAddress = entry;
        Save();
    }

    /// <summary> Clears the confinement entry. </summary>
    public void ClearConfinementAddress()
    {
        _metaData.ConfinementAddress = null;
        Save();
    }

    // Anchors the client to a spesific position.
    public void AnchorToPosition(Vector3 Position, int CageRadius)
    {
        // safely obtain our world and territory.
        var worldId = PlayerData.CurrentWorldIdInstanced;
        var territoryId = PlayerContent.TerritoryIdInstanced;
        _metaData.AnchoredCageWorldId = worldId;
        _metaData.AnchoredCageTerritoryId = territoryId;
        // fallback to current position if the provided was not a valid location.
        _metaData.AnchoredCagePos = Position == Vector3.Zero ? PlayerData.PositionInstanced : Position;
        _metaData.AnchoredCageRadius = CageRadius;
        Save();
    }

    public void ClearCageAnchor()
    {
        _metaData.AnchoredCageWorldId = ushort.MaxValue;
        _metaData.AnchoredCageTerritoryId = uint.MaxValue;
        _metaData.AnchoredCagePos = Vector3.Zero;
        _metaData.AnchoredCageRadius = 1;
        Save();
    }

    public void SetHypnoEffect(HypnoticEffect effect, DateTimeOffset appliedTime, TimeSpan duration, string? base64ImageData = null)
    {
        _metaData.HypnoEffectInfo = effect;
        _metaData.AppliedTimeUTC = appliedTime;
        _metaData.AppliedDuration = duration;
        _metaData.Base64CustomImageData = base64ImageData;
        Save();
    }

    public void ClearHypnoEffect()
    {
        _metaData.HypnoEffectInfo = null;
        _metaData.AppliedTimeUTC = DateTimeOffset.MinValue;
        _metaData.AppliedDuration = TimeSpan.Zero;
        _metaData.Base64CustomImageData = null;
        Save();
    }

    public class MetaDataState : IReadonlyMetaData
    {
        // MetaData related to hypnosis Effects applied by other Kinksters.
        public HypnoticEffect? HypnoEffectInfo { get; set; } = null;
        public DateTimeOffset AppliedTimeUTC { get; set; } = DateTimeOffset.MinValue;
        public TimeSpan AppliedDuration { get; set; } = TimeSpan.Zero;
        public string? Base64CustomImageData { get; set; } = null;

        // MetaData related to Indoor Confinement.
        public AddressBookEntry? ConfinementAddress { get; set; } = null;

        // MetaData related to Imprisonment.
        public ushort AnchoredCageWorldId { get; set; } = ushort.MaxValue;
        public uint AnchoredCageTerritoryId { get; set; } = uint.MaxValue;
        public Vector3 AnchoredCagePos { get; set; } = Vector3.Zero;
        public int AnchoredCageRadius { get; set; } = 1;
    }

    public interface IReadonlyMetaData
    {
        /// <summary>
        ///     Contains all information about the custom hypnosis effect applied to the client
        ///     by another player.
        /// </summary>
        public HypnoticEffect? HypnoEffectInfo { get; }

        /// <summary>
        ///     When the hypnosis effect was applied to the client.
        /// </summary>
        public DateTimeOffset AppliedTimeUTC { get; }

        /// <summary>
        ///     How long the hypnosis effect is intended to last.
        /// </summary>
        public TimeSpan AppliedDuration { get; }

        /// <summary>
        ///     Custom image applied by the effect (only allowed by Kinksters you are in Hardcore
        ///     mode with.
        /// </summary>
        public string? Base64CustomImageData { get; }



        /// <summary>
        ///     If the player is currently processing an indoorConfinement operation, we want
        ///     to internally store the destination address until they arrive. <para />
        ///     This way, if they try to change worlds, or use other means to escape confinement,
        ///     we can drag them back to where they belong.
        /// </summary>
        /// <remarks> If nullable, no address is set. </remarks>
        public AddressBookEntry? ConfinementAddress { get; }

        //     If the player is currently under a state of imprisonment, we want to store their imprisonment zone.
        //     this lets us know if they are not where they belong or are meant to be. <para />
        //     This way, if they change worlds, and their position is not the same as the imprisonment zone, we
        //     can prevent trying to force them into a location that does not exist in their new area.

        /// <summary> Current WorldID Imprisonment is intended for. </summary>
        public ushort AnchoredCageWorldId { get; }

        /// <summary> Current Territory (zone) ID Imprisonment is intended for. </summary>
        public uint AnchoredCageTerritoryId { get; }

        /// <summary> Current precise position to anchor player to. </summary>
        public Vector3 AnchoredCagePos { get; }

        /// <summary> How much freedom the player has. </summary>
        public int AnchoredCageRadius { get; }
    }
}
