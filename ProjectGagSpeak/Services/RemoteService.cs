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
using GagspeakAPI.Network;
using ImGuiNET;
using OtterGui;
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
    public RemoteService(ILogger<RemoteService> logger, GagspeakMediator mediator,
        MainConfig config, BuzzToyManager toyManager, VibeLobbyManager lobbyManager)
        : base(logger, mediator)
    {
        _config = config;
        _toyManager = toyManager;
        _lobbyManager = lobbyManager;
        // set an initial data, this will initially be a empty string, but rectified upon connection.
        ClientData = new ClientPlotedDevices(mediator, new(new(MainHub.UID), _config.Current.NicknameInVibeRooms));

        /// Monitors for changes to the client players devices.
        Mediator.Subscribe<ConfigSexToyChanged>(this, (msg) => OnClientToyChange(msg.Type, msg.Item));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) =>
        {
            if (msg.Module is GagspeakModule.SexToys)
                OnUpdateClientDevice();
        });

        // whenever we connect, we should do a full cleanup of the client devices, then append the current toys.
        Mediator.Subscribe<MainHubConnectedMessage>(this, _ =>
        {
            Logger.LogInformation("Reconnected to GagSpeak. Setting Client Devices.");
            ClientData = new ClientPlotedDevices(mediator, new(new(MainHub.UID), _config.Current.NicknameInVibeRooms));
            OnUpdateClientDevice();
            SelectedKey = MainHub.UID;
        });

        // Begin the service update loop.
        _updateLoopCTS = _updateLoopCTS.SafeCancelRecreate();
        _updateLoopTask = UpdateLoopTask();

        // not whenever we disconnect, but whenever we logout, we should clear the mainHub key.
        Svc.ClientState.Logout += (_, _) => OnLogout();
    }

    public ClientPlotedDevices ClientData { get; private set; }

    public string SelectedKey = string.Empty;
    public bool IsClientBeingBuzzed => ClientData.UserIsBeingBuzzed;
    public bool CanBeginRecording => !ClientData.RecordingData && !_lobbyManager.IsInVibeRoom;
    public bool TryEnableRecordingMode()
    {
        if (!CanBeginRecording)
            return false;

        ClientData.RecordingData = true;
        Logger.LogInformation("Enabled Recording Mode for Client.");
        return true;
    }

    public bool TryGetRemoteData([NotNullWhen(true)] out UserPlotedDevices? data)
    {
        if (SelectedKey == MainHub.UID)
        {
            data = ClientData;
            return true;
        }
        else
        {
            if (_participantData.TryGetValue(SelectedKey, out var participantData))
            {
                data = participantData;
                return true;
            }
        }
        data = null;
        return false;
    }

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
        switch (changeType)
        {
            case StorageChangeType.Created:
                AddClientDevice(item);
                break;

            case StorageChangeType.Deleted:
                RemoveClientDevice(item);
                break;

            case StorageChangeType.Modified:
                OnUpdateClientDevice();
                break;
        }
    }

    private void OnUpdateClientDevice()
    {
        foreach (var toy in _toyManager.InteractableToys)
        {
            if (ClientData.Devices.FirstOrDefault(d => d.FactoryName.Equals(toy.FactoryName)) is { } match && !toy.ValidForRemotes)
                ClientData.RemoveDevice(match);
            else if (toy.ValidForRemotes)
                ClientData.AddDevice(new(toy));
        }
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
        {
            Logger.LogWarning($"Device {device.LabelName} is not valid for remotes, cannot add to Client.");
            return;
        }

        // Attempt to add the device to the remoteData list
        if (!ClientData.AddDevice(new(device)))
            Logger.LogWarning($"Failed to add device {device.LabelName} for Client.");
        else
            Logger.LogInformation($"Added device {device.LabelName} for Client.");
    }

    // Can cleanup once we are finished debug logging things.
    private void RemoveClientDevice(BuzzToy device)
    {
        if (ClientData.Devices.FirstOrDefault(d => d.FactoryName == device.FactoryName) is not { } match)
            return;

        if (!ClientData.RemoveDevice(match))
            Logger.LogWarning($"Failed to remove device {device.LabelName} for Client.");
        else
            Logger.LogInformation($"Removed device {device.LabelName} for Client.");
    }

    public void AndVibeRoomParticipant(RoomParticipant participant)
    {
        // create a new entry in the dictionary for this kinkster.
        var newPlottedDevicesData = new ParticipantPlotedDevices(Mediator, participant);
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

        using (ImRaii.Table("##overview", 10, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {

            ImGuiUtil.DrawTableColumn("BrandName");
            ImGuiUtil.DrawTableColumn(toy.FactoryName.ToString());
            ImGui.TableNextRow();

            foreach (var (key, value) in toy.MotorDotMap)
            {
                ImGuiUtil.DrawTableColumn($"MotorIdx {key}");
                ImGuiUtil.DrawTableColumn(value.Motor.Type.ToString());

                ImGui.TableNextColumn();
                CkGui.ColorTextBool("Dragging", value.IsDragging);

                ImGui.TableNextColumn();
                CkGui.ColorTextBool("Floating", value.IsFloating);

                ImGui.TableNextColumn();
                CkGui.ColorTextBool("Looping", value.IsLooping);

                ImGui.TableNextColumn();
                CkGui.ColorTextBool("Visible", value.Visible);

                ImGuiUtil.DrawTableColumn($"{value.Motor.StepCount} steps");
                ImGuiUtil.DrawTableColumn($"{value.Motor.Interval} interval");
                ImGuiUtil.DrawTableColumn($"{value.Motor.Intensity.ToString("F2")} intensity");
                ImGui.TableNextRow();
            }
            ImGui.TableNextRow();
        }
    }
    #endregion Debug Helper
}
