using CkCommons;
using CkCommons.Gui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using GagSpeak.FileSystems;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Gui.Remote;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using ImGuiNET;
using OtterGui;
using OtterGui.Services;
using OtterGui.Text;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Services;

/// <summary>
///     Service to maintain the active devices selected for recording, 
///     and cache their recorded states.
/// </summary>
public sealed class RemoteService : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly BuzzToyManager _toyManager; // Client Toy Data
    private readonly VibeLobbyManager _lobbyManager; // Vibe Room Data

    private static readonly TimeSpan CompileInterval = TimeSpan.FromMilliseconds(2000);

    private Dictionary<string, ParticipantPlotedDevices> _participantData = new();
    private CancellationTokenSource _updateLoopCTS = new();
    private Task? _updateLoopTask = null;
    private string _selectedKey = string.Empty;
    private UserPlotedDevices? _selectedData = null;

    public RemoteService(ILogger<RemoteService> logger, GagspeakMediator mediator,
        MainConfig config, BuzzToyManager toyManager, VibeLobbyManager lobbyManager)
        : base(logger, mediator)
    {
        _config = config;
        _toyManager = toyManager;
        _lobbyManager = lobbyManager;
        // set an initial data, this will initially be a empty string, but rectified upon connection.
        ClientData = new ClientPlotedDevices(Logger, mediator, new(new(MainHub.UID), _config.Current.NicknameInVibeRooms), RemoteAccess.Full);

        /// Monitors for changes to the client players devices.
        Mediator.Subscribe<ConfigSexToyChanged>(this, (msg) => OnClientToyChange(msg.Type, msg.Item));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) =>
        {
            if (msg.Module is GagspeakModule.SexToys)
                UpdateClientDevices();
        });

        // whenever we connect, we should do a full cleanup of the client devices, then append the current toys.
        Mediator.Subscribe<MainHubConnectedMessage>(this, _ =>
        {
            Logger.LogInformation("Reconnected to GagSpeak. Setting Client Devices.");
            ClientData = new ClientPlotedDevices(Logger, mediator, new(new(MainHub.UID), _config.Current.NicknameInVibeRooms), RemoteAccess.Full);
            UpdateClientDevices();
            SelectedKey = MainHub.UID;
        });

        // Begin the service update loop.
        _updateLoopCTS = _updateLoopCTS.SafeCancelRecreate();
        _updateLoopTask = UpdateLoopTask();

        // not whenever we disconnect, but whenever we logout, we should clear the mainHub key.
        Svc.ClientState.Logout += (_, _) => OnLogout();
    }

    public ClientPlotedDevices ClientData { get; private set; }
    public bool IsClientBeingBuzzed => ClientData.UserIsBeingBuzzed;
    public bool CanRecord => !ClientData.InRecordingMode && !_lobbyManager.IsInVibeRoom;
    public string SelectedKey
    {
        get => _selectedKey;
        set
        {
            if (_selectedKey == value)
                return;
            // update the data.
            _selectedKey = value;
            _selectedData = GetSelectedOrDefault();
            // if the selected key is not present, default to clientData.
            if (_selectedData is null)
            {
                _selectedKey = MainHub.UID;
                _selectedData = ClientData;
            }
            // Notify the change to mediator subscribers.
            Mediator.Publish(new RemoteSelectedKeyChanged(_selectedData));
        }
    } 

    public string GetRemoteUiName()
        => _lobbyManager.IsInVibeRoom ? $"Vibe Room - {_lobbyManager.CurrentRoomName}" : 
            ClientData.InRecordingMode ? "Pattern Recorder" : "Personal Remote";

    public UserPlotedDevices? GetSelectedOrDefault()
        => _selectedKey == MainHub.UID ? ClientData : _participantData.GetValueOrDefault(SelectedKey);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.ClientState.Logout -= (_, _) => OnLogout();
        _updateLoopCTS.SafeCancel();
    }

    private void OnLogout()
    {
        Logger.LogInformation("Disconnected from GagSpeak. Removing all Client Devices.");
        ClientData.RemoveAll();
        foreach (var participant in _participantData.Values)
            participant.RemoveAll();
        _participantData.Clear();
        SelectedKey = string.Empty;
        Logger.LogInformation($"Removed all devices for Client.");
    }

    private void OnClientToyChange(StorageChangeType changeType, BuzzToy item)
    {
        var validItems = _toyManager.InteractableToys.Where(t => t.ValidForRemotes).Select(t => t.FactoryName).ToHashSet();
        switch (changeType)
        {
            case StorageChangeType.Created:
                AddClientDevice(item);
                break;

            case StorageChangeType.Deleted:
                RemoveClientDevice(item);
                break;

            case StorageChangeType.Modified:
                UpdateClientDevices();
                break;
        }
        var postValid = _toyManager.InteractableToys.Where(t => t.ValidForRemotes).Select(t => t.FactoryName);
        validItems.SymmetricExceptWith(postValid);
        if (validItems.Count > 0)
        {
            Logger.LogDebug($"Valid Devices changed from {string.Join(", ", validItems)} to {string.Join(", ", postValid)}.");
            Mediator.Publish(new ValidToysChangedMessage(postValid.ToList()));
        }
    }

    private void UpdateClientDevices()
    {
        var toysToCheck = _toyManager.InteractableToys.Where(t => t.ValidForRemotes).ToList();
        foreach (var device in ClientData.Devices)
        {
            // check if there is a valid toy in the list for this device.
            if (toysToCheck.FirstOrDefault(t => t.FactoryName.Equals(device.FactoryName)) is { } match)
                // this is a match, so this device is already valid and we can remove it from the list of toys to check.
                toysToCheck.Remove(match);
            else
                // otherwise, remove the device from the client data, as it no longer exists.
                ClientData.RemoveDevice(device);
        }
        
        // for each of the remaining toys left, we should add them.
        foreach (var toyToAdd in toysToCheck)
            ClientData.AddDevice(new(toyToAdd));
    }

    public void SetUserRemotePower(string key, bool newState, string enactor)
    {
        if (key == MainHub.UID && ClientData.TrySetRemotePower(newState, enactor))
            Logger.LogInformation($"{enactor} Powered remote for {ClientData.Owner.DisplayName} to new state {newState}.");
        else if (_participantData.TryGetValue(key, out var pd) && pd.TrySetRemotePower(newState, enactor))
        {
            if (newState && !_controllingVibeRoomUser)
                _controllingVibeRoomUser = true;
            else if (!newState && _participantData.Values.All(p => !p.UserIsBeingBuzzed))
                _controllingVibeRoomUser = false;
            Logger.LogInformation($"{enactor} Powered on remote for {pd.Owner.DisplayName}.");
        }
    }

    private bool _controllingVibeRoomUser = false;
    private async Task UpdateLoopTask()
    {
        await Generic.Safe(async () =>
        {
            var lastCompileTime = DateTime.Now;
            while (!_updateLoopCTS.IsCancellationRequested)
            {
                // perform an update tick on the client data if active.
                if (ClientData.UserIsBeingBuzzed)
                    ClientData.OnUpdateTick();

                // compile update tick.
                foreach (var participant in _participantData.Values)
                {
                    if (!participant.UserIsBeingBuzzed)
                        continue;
                    // perform the update tick.
                    participant.OnUpdateTick();
                }

                // if we are not controlling anyone, return.
                if (_controllingVibeRoomUser)
                {
                    // if the interval is reached.
                    if (DateTime.Now - lastCompileTime > CompileInterval)
                    {
                        var dataToSend = _participantData.Values
                            .Where(p => p.UserIsBeingBuzzed)
                            .Select(p => p.CompileFromRecordingForUser())
                            .ToArray();
                        if (dataToSend.Length > 0)
                            Mediator.Publish(new VibeRoomSendDataStream(new(dataToSend, DateTime.UtcNow.Ticks)));
                        // doesnt madder, still update it to avoid doxing the server.
                        lastCompileTime = DateTime.Now;
                    }
                } 
                await Task.Delay(20, _updateLoopCTS.Token);
            }
        });
    }

    private void AddClientDevice(BuzzToy device)
    {
        if (!device.ValidForRemotes)
            return;

        // Attempt to add the device to the remoteData list
        if (ClientData.AddDevice(new(device)))
            Logger.LogInformation($"Added device {device.LabelName} for Client.");
    }

    // Can cleanup once we are finished debug logging things.
    private void RemoveClientDevice(BuzzToy device)
    {
        if (ClientData.Devices.FirstOrDefault(d => d.FactoryName == device.FactoryName) is not { } match)
            return;

        if (ClientData.RemoveDevice(match))
            Logger.LogInformation($"Removed device {device.LabelName} for Client.");
    }

    public void AndVibeRoomParticipant(RoomParticipant participant)
    {
        // create a new entry in the dictionary for this kinkster.
        var newPlottedDevicesData = new ParticipantPlotedDevices(Logger, Mediator, participant);
        _participantData[participant.User.UID] = newPlottedDevicesData;

        // its ok to be a little performance heavy since it's only done once on creation.
        foreach (var device in participant.Devices)
            if(!newPlottedDevicesData.AddDevice(new DeviceDot(device)))
            {
                Logger.LogWarning($"Failed to add device {device.BrandName} for Vibe Room user {participant.DisplayName} with UID {participant.User.UID}. " +
                                  $"This may be due to the device not being valid for remotes or already existing in the remote data.");
            }

        // log the addition of the participant.
        Logger.LogInformation($"Added Vibe Room user {participant.DisplayName} with UID {participant.User.UID} and {participant.Devices.Count()} devices.");
    }

    public void AddVibeRoomParticipants(IEnumerable<RoomParticipant> participants)
    {
        // maybe a parallel foreach if the operation is heavy idk.
        foreach (var participant in participants)
            AndVibeRoomParticipant(participant);
        Logger.LogInformation($"Added {participants.Count()} Vibe Room users to the remote service.");
    }

    public void RemoveVibeRoomParticipant(string userUid)
    {
        // get the participant we are looking for.
        if (_participantData.Remove(userUid, out var removed))
        {
            // Cleanup data.
            removed.RemoveAll();
            Logger.LogInformation($"Removed Vibe Room user with UID, and their devices.");
            return;
        }
        
        Logger.LogWarning($"Failed to remove Vibe Room user with UID, they may not exist.");
    }

    public void RemoveVibeRoomParticipants(IEnumerable<string> participantUids)
    {
        // Remove all users in the list.
        foreach (var uid in participantUids)
            RemoveVibeRoomParticipant(uid);
        Logger.LogInformation($"Removed {participantUids.Count()} Vibe Room users from the remote service.");
    }

    #region DebugHelper
    public void DrawCacheTable()
    {
        using var _ = ImRaii.Group();

        ImGui.Text("Selected Key:");
        CkGui.ColorTextInline(SelectedKey, CkColor.VibrantPink.Uint());
        DrawClient();
        DrawParticipants();
    }

    private void DrawClient()
    {
        using var node = ImRaii.TreeNode("Client RemoteDataCache");
        if (!node)
            return;

        ImGui.Text($"Owner: {ClientData.Owner.DisplayName} ({ClientData.Owner.User.UID})");

        ImGui.Text("Being Controlled?");
        ImUtf8.SameLineInner();
        CkGui.ColorTextBool(ClientData.UserIsBeingBuzzed ? "Yes" : "No", ClientData.UserIsBeingBuzzed);

        ImGui.Text("ControlTime:");
        ImUtf8.SameLineInner();
        CkGui.ColorText(ClientData.TimeAlive.ToString(), CkColor.VibrantPink.Uint());

        ImGui.Separator();
        ImGui.Text("Devices:");
        foreach (var device in ClientData.Devices)
        {
            using var deviceNode = ImRaii.TreeNode($"{device.FactoryName} (TYPE-UNKNOWN)");
            if (!deviceNode)
                continue;
            DrawDevicePlotState(device);
        }
    }

    private void DrawParticipants()
    {
        foreach (var (key, remoteData) in _participantData)
        {
            using var node = ImRaii.TreeNode($"{key}'s RemoteDataCache");
            if (!node)
                continue;

            ImGui.Text($"Owner: {remoteData.Owner.DisplayName} ({remoteData.Owner.User.UID})");

            ImGui.Text("Being Controlled?");
            ImUtf8.SameLineInner();
            CkGui.ColorTextBool(remoteData.UserIsBeingBuzzed ? "Yes" : "No", remoteData.UserIsBeingBuzzed);

            ImGui.Text("ControlTime:");
            ImUtf8.SameLineInner();
            CkGui.ColorText(remoteData.TimeAlive.ToString(), CkColor.VibrantPink.Uint());

            ImGui.Separator();
            ImGui.Text("Devices:");
            foreach (var device in remoteData.Devices)
            {
                using var deviceNode = ImRaii.TreeNode($"{device.FactoryName}");
                if (!deviceNode)
                    continue;

                DrawDevicePlotState(device);
            }
            ImGui.Separator();
        }
    }

    private void DrawDevicePlotState(DeviceDot toy)
    {
        CkGui.ColorTextBool(toy.IsEnabled ? "Enabled" : "Disabled", toy.IsEnabled);
        CkGui.TextInline($"| BrandName: {toy.FactoryName.ToString()} ({toy.FactoryName.ToName()})");
        using (ImRaii.Table("##overview", 11, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("MotorIdx");
            ImGui.TableSetupColumn("Type");
            ImGui.TableSetupColumn("Dragging");
            ImGui.TableSetupColumn("Floating");
            ImGui.TableSetupColumn("Looping");
            ImGui.TableSetupColumn("Visible");
            ImGui.TableSetupColumn("Steps/Interval");
            ImGui.TableSetupColumn("Intensity");
            ImGui.TableSetupColumn("Data Recorded");
            ImGui.TableSetupColumn("Ref Idx/Length/Loop");
            ImGui.TableHeadersRow();

            foreach (var (key, value) in toy.MotorDotMap)
            {
                ImGuiUtil.DrawTableColumn(key.ToString());
                ImGuiUtil.DrawTableColumn(value.Motor.Type.ToString());
                ImGuiUtil.DrawTableColumn($"{value.IsDragging} ({value.DragLoopStartPos})");
                ImGuiUtil.DrawTableColumn(value.IsFloating.ToString());
                ImGuiUtil.DrawTableColumn(value.IsLooping.ToString());
                ImGuiUtil.DrawTableColumn(value.Visible.ToString());
                ImGuiUtil.DrawTableColumn($"{value.Motor.StepCount} / {value.Motor.Interval}");
                ImGuiUtil.DrawTableColumn($"{value.Motor.Intensity.ToString("F2")}");
                ImGuiUtil.DrawTableColumn(value.RecordedData.Count().ToString());
                ImGuiUtil.DrawTableColumn($"{value.PlaybackRef.Idx} / {value.PlaybackRef.Length} / {value.PlaybackRef.Looping}");
                ImGui.TableNextColumn();
                CkGui.FramedHoverIconText(FAI.InfoCircle, CkColor.VibrantPink.Uint());
                CkGui.AttachToolTip(string.Join(", ", value.RecordedData.Select(d => d.ToString("F2"))));
                ImGui.TableNextRow();
            }
        }
    }
    #endregion Debug Helper
}
