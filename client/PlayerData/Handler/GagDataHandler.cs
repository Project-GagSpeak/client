using Dalamud.Plugin;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;

namespace GagSpeak.PlayerData.Handlers;
/// <summary> Service for managing the gags. </summary>
public class GagDataHandler : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly IDalamudPluginInterface _pi;
    private Dictionary<string, Dictionary<string, PhonemeProperties>> _gagData;
    public List<GagData> _gagTypes;

    public GagDataHandler(ILogger<GagDataHandler> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfiguration,
        IDalamudPluginInterface pi) : base(logger, mediator)
    {
        _clientConfigs = clientConfiguration;
        _pi = pi;
        _gagTypes = new List<GagData>();

        // Try to read the JSON file and de-serialize it into the obj dictionary
        try
        {
            string jsonFilePath = Path.Combine(_pi.AssemblyLocation.Directory?.FullName!, "MufflerCore\\GagData\\gag_data.json");
            string json = File.ReadAllText(jsonFilePath);
            _gagData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, PhonemeProperties>>>(json) ?? new Dictionary<string, Dictionary<string, PhonemeProperties>>();
        }
        catch (FileNotFoundException)
        {
            Logger.LogDebug($"[IPA Parser] File does not exist", LoggerType.GagHandling);
            _gagData = new Dictionary<string, Dictionary<string, PhonemeProperties>>();
        }
        catch (Exception ex)
        {
            Logger.LogDebug($"[IPA Parser] An error occurred while reading the file: {ex.Message}", LoggerType.GagHandling);
            _gagData = new Dictionary<string, Dictionary<string, PhonemeProperties>>();
        }

        // create our gag listings
        CreateGags();

        // subscribe to the language changed message, to refresh and update the gag data when received.
        Mediator.Subscribe<MufflerLanguageChanged>(this, (_) =>
        {
            _gagTypes.Clear();
            CreateGags();
        });
    }

    private void CreateGags()
    {
        List<string> masterList;
        switch (_clientConfigs.GagspeakConfig.LanguageDialect)
        {
            case "IPA_UK": masterList = GagPhonetics.MasterListEN_UK; break;
            case "IPA_US": masterList = GagPhonetics.MasterListEN_US; break;
            case "IPA_SPAIN": masterList = GagPhonetics.MasterListSP_SPAIN; break;
            case "IPA_MEXICO": masterList = GagPhonetics.MasterListSP_MEXICO; break;
            case "IPA_FRENCH": masterList = GagPhonetics.MasterListFR_FRENCH; break;
            case "IPA_QUEBEC": masterList = GagPhonetics.MasterListFR_QUEBEC; break;
            case "IPA_JAPAN": masterList = GagPhonetics.MasterListJP; break;
            default: throw new Exception("Invalid language");
        }

        // Assuming you want to reset the list each time you create gags

        foreach (var gagEntry in _gagData)
        {
            var gagName = gagEntry.Key;
            var phonemes = gagEntry.Value;

            var gag = new GagData(gagName, phonemes);
            _gagTypes.Add(gag);
        }
    }
}
