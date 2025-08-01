using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Network;

namespace GagSpeak.Gui.Remote;

// maybe convert to class, not sure.
public readonly record struct RemoteAccess(
    bool WindowClosable = true,
    bool RemotePower = false,
    bool DeviceSelect = false,
    bool DeviceStates = false,
    bool MotorSelect = false,
    bool MotorFunctions = false,
    bool MotorControl = false
    )
{
    public static readonly RemoteAccess Previewing = new(
        WindowClosable: true,
        RemotePower: false,
        DeviceSelect: true,
        DeviceStates: false,
        MotorSelect: true,
        MotorFunctions: false,
        MotorControl: false
        );

    public static readonly RemoteAccess ForcedPlayback = new(
        WindowClosable: false,
        RemotePower: false,
        DeviceSelect: false,
        DeviceStates: false,
        MotorSelect: false,
        MotorFunctions: false,
        MotorControl: false
        );

    public static readonly RemoteAccess Playback = new(
        WindowClosable: true,
        RemotePower: true,
        DeviceSelect: true,
        DeviceStates: false,
        MotorSelect: true,
        MotorFunctions: false,
        MotorControl: false
        );
    
    // Effectly full access but unable to close window.
    // isolate what interactable devices you want used for the recording.
    public static readonly RemoteAccess RecordingStartup = new(
        WindowClosable: false,
        RemotePower: true,
        DeviceSelect: true,
        DeviceStates: true,
        MotorSelect: true,
        MotorFunctions: true,
        MotorControl: true
        );

    // Allow user to finish recording, select devices and motors, and do motor functions & control.
    // Do not allow them to change the enabled devices states.
    public static readonly RemoteAccess Recording = new(
        WindowClosable: false,
        RemotePower: true,
        DeviceSelect: true,
        DeviceStates: false,
        MotorSelect: true,
        MotorFunctions: true,
        MotorControl: true
        );

    public static readonly RemoteAccess Full = new(
        WindowClosable: true,
        RemotePower: true,
        DeviceSelect: true,
        DeviceStates: true,
        MotorSelect: true,
        MotorFunctions: true,
        MotorControl: true
        );
}
