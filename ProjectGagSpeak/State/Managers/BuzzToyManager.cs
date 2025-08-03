using Buttplug.Client;
using CkCommons;
using CkCommons.Helpers;
using CkCommons.HybridSaver;
using GagSpeak.FileSystems;
using GagSpeak.Interop;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;

namespace GagSpeak.State.Managers;

// handles the management of the connected devices or simulated vibrator.
public class BuzzToyManager : IDisposable, IHybridSavable
{
    private readonly ILogger<BuzzToyManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly IpcCallerIntiface _ipc;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private BuzzToyStorage _storage = new();
    private StorageItemEditor<VirtualBuzzToy> _itemEditor = new();

    // Maybe make nullable if running into issues.
    private Task? _batteryCheckTask = null;
    private CancellationTokenSource _batteryCTS;

    public BuzzToyManager(ILogger<BuzzToyManager> logger, GagspeakMediator mediator,
        IpcCallerIntiface ipc, ConfigFileProvider fileNames, HybridSaveService saver)
    {
        _logger = logger;
        _mediator = mediator;
        _ipc = ipc;
        _fileNames = fileNames;
        _saver = saver;
        Load();
    }

    public BuzzToyStorage Storage => _storage;
    public BuzzToy? ItemInEditor => _itemEditor.ItemInEditor;
    public IEnumerable<BuzzToy> InteractableToys => _storage.Values.Where(st => st.Interactable);
    public List<ToyBrandName> ValidToysForRemotes =>
        _storage.Values.Where(st => st.ValidForRemotes).Select(st => st.FactoryName).Distinct().ToList();

    public void Dispose()
    {
        _batteryCTS.SafeCancel();
        if (_batteryCheckTask != null)
            _batteryCheckTask?.Wait();

        _batteryCheckTask?.Dispose();
        _batteryCTS.SafeDispose();
    }

    public VirtualBuzzToy CreateNew(ToyBrandName deviceName)
    {
        var newItem = new VirtualBuzzToy(deviceName);
        _storage.TryAdd(newItem.Id, newItem);
        _saver.Save(this);

        _mediator.Publish(new ConfigSexToyChanged(StorageChangeType.Created, newItem, null));
        return newItem;
    }

    // Yes this is intentionally distinct.
    public void AddDevice(VirtualBuzzToy newToy)
    {
        // try to add it first.
        if (_storage.TryAdd(newToy.Id, newToy))
        {
            _logger.LogInformation($"Added new virtual toy [{newToy.FactoryName}] ({newToy.LabelName}) to connected toys.", LoggerType.Toys);
            _saver.Save(this);
            _mediator.Publish(new ConfigSexToyChanged(StorageChangeType.Created, newToy, null));
            return;
        }
        else
        {
            // it existed, so update it.
            _logger.LogInformation($"Updating existing virtual toy [{newToy.FactoryName}] ({newToy.LabelName}) in connected toys.", LoggerType.Toys);
            _storage[newToy.Id] = newToy;
            _saver.Save(this);
            _mediator.Publish(new ConfigSexToyChanged(StorageChangeType.Modified, newToy, null));
        }
    }

    // Yes this is intentionally distinct.
    public void AddOrUpdateDevice(ButtplugClientDevice newToy)
    {
        // see if a toy exists that matches, but potentially has a different index.
        foreach (var toy in _storage.Values.OfType<IntifaceBuzzToy>())
        {
            if (toy.FactoryName != ToyExtensions.ToBrandName(newToy.Name))
            {
                _logger.LogDebug($"Skipping toy [{toy.FactoryName}] ({toy.LabelName}) as it does not match the new device attributes: " +
                    $"Factory: {toy.FactoryName} vs {newToy.Name}");
                continue;
            }

            // Found a match, update and break.
            _logger.LogInformation($"Updating [{toy.FactoryName}] ({toy.LabelName}) with new device info.", LoggerType.Toys);
            toy.UpdateDevice(newToy);
            _saver.Save(this);
            _mediator.Publish(new ConfigSexToyChanged(StorageChangeType.Modified, toy, null));
            return;
        }

        // if we reached here it means no existing toy matched, so we create a new one.
        var created = new IntifaceBuzzToy(newToy);
        if(_storage.TryAdd(created.Id, created))
        {
            // Successfully added the new toy.
            _logger.LogInformation($"Added new Intiface toy [{created.FactoryName}] ({created.LabelName}) to connected toys.", LoggerType.Toys);
            _saver.Save(this);
            _mediator.Publish(new ConfigSexToyChanged(StorageChangeType.Created, created, null));

            // If the battery task is not yet running, we should begin it.
            if (_batteryCheckTask is null || _batteryCheckTask.IsCompleted)
            {
                _logger.LogInformation("Starting Battery Check Loop for Intiface Toys.", LoggerType.Toys);
                StartBatteryCheck();
            }
        }
    }

    public void ToggleInteractableState(BuzzToy device)
    {
        if (_storage.Values.Contains(device))
        {
            device.Interactable = !device.Interactable;
            _mediator.Publish(new ConfigSexToyChanged(StorageChangeType.Modified, device));
            _saver.Save(this);
        }
    }
    public void Rename(BuzzToy device, string newName)
    {
        var oldName = device.LabelName;
        if (oldName == newName || string.IsNullOrWhiteSpace(newName))
            return;

        device.LabelName = newName;
        _saver.Save(this);
        _logger.LogDebug($"Renamed device {device.LabelName} ({device.Id})");
        _mediator.Publish(new ConfigSexToyChanged(StorageChangeType.Renamed, device, oldName));
    }

    public void RemoveDevice(BuzzToy device)
    {
        if(_storage.TryRemove(device.Id, out var removedDevice))
        {
            removedDevice.Dispose();
            _logger.LogInformation($"Removed device {removedDevice.LabelName} ({removedDevice.Id}) from connected toys.", LoggerType.Toys);
            _saver.Save(this);
            _mediator.Publish(new ConfigSexToyChanged(StorageChangeType.Deleted, removedDevice, null));
        }

    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(VirtualBuzzToy device) => _itemEditor.StartEditing(_storage, device);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the BuzzToy and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            _logger.LogTrace("Saved changes to Edited Sex Toy Device.");
            _mediator.Publish(new ConfigSexToyChanged(StorageChangeType.Modified, sourceItem));
            _saver.Save(this);
        }
    }

    public void StartBatteryCheck()
    {
        _batteryCTS = _batteryCTS.SafeCancelRecreate();
        _batteryCheckTask = BatteryCheckLoop(_batteryCTS.Token);
    }

    public void StopBatteryCheck()
    {
        _batteryCTS.SafeCancel();
        _batteryCheckTask?.Wait();
        _batteryCheckTask?.Dispose();
        _batteryCTS.SafeDispose();
    }


    private async Task BatteryCheckLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && IpcCallerIntiface.IsConnected)
            {
                _logger.LogTrace("Scheduled Battery Check on connected devices", LoggerType.Toys);
                if (!IpcCallerIntiface.IsConnected)
                    break;

                // maybe parallel foreach this if it's becoming an issue, otherwise keep as is.
                foreach (var toy in _storage.Values.OfType<IntifaceBuzzToy>())
                    await toy.UpdateBattery().ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromSeconds(120), ct).ConfigureAwait(false);
            }
        }
        catch (TaskCanceledException) { /* Consume */ }
        catch (Bagagwa ex)
        {
            _logger.LogError(ex, "Error in Battery Check Loop");
        }
    }

    #region HybridSavable
    public void Save() => _saver.Save(this);
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.BuzzToys).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        // we need to iterate through our list of trigger objects and serialize them.
        var items = JArray.FromObject(_storage.Values.Select(a => a.Serialize()));
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["SexToys"] = items,
        }.ToString(Formatting.Indented);
    }

    public void Load()
    {
        var file = _fileNames.BuzzToys;
        _logger.LogInformation($"Loading in StoredToys Config for file: {file}");

        _storage.Clear();
        if (!File.Exists(file))
        {
            _logger.LogWarning($"No StoredToys Config file found at {file}");
            _saver.Save(this);
            return;
        }

        // Read the json from the file.
        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);

        var version = jObject["Version"]?.Value<int>() ?? 0;

        switch (version)
        {
            case 0:
                LoadV0(jObject["SexToys"]);
                break;
            default:
                _logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
        _mediator.Publish(new ReloadFileSystem(GagspeakModule.SexToys));
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JArray storedToys)
            return;

        foreach (var toyToken in storedToys)
        {
            try
            {
                // Identify the type of restriction
                if (!Enum.TryParse(toyToken["Type"]?.Value<string>(), out SexToyType toyType))
                    throw new Exception("Invalid SexToyType in stored toys data.");

                // Create the appropriate BuzzToy based on the type.
                BuzzToy parsedToy = toyType switch
                {
                    SexToyType.Real => IntifaceBuzzToy.FromToken(toyToken),
                    SexToyType.Simulated => VirtualBuzzToy.FromToken(toyToken),
                    _ => throw new Exception($"Unknown SexToyType: {toyType}")
                };
                _storage.TryAdd(parsedToy.Id, parsedToy);
            }
            catch (Bagagwa ex)
            {
                _logger.LogWarning($"Error deserializing SexToy, skipping this item!: {ex}");
            }
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }

    #endregion HybridSavable
}





