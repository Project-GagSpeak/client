using CkCommons;
using CkCommons.HybridSaver;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI;
using NAudio.Wave;
using TerraFX.Interop.Windows;

namespace GagSpeak.PlayerClient;

public interface IAudioConfig<T> where T : IAudioConfigData
{
    /// <summary>
    ///   The current ConfigData for this profile/account/server.
    /// </summary>
    public T Data { get; }

    /// <summary>
    ///   Saves the config file
    /// </summary>
    public void Save();

    /// <summary>
    ///   Gets If the Alert audio is valid
    /// </summary>
    public bool IsAudioReady();

    /// <summary>
    ///   Plays the stored audio, if valid, based on the selected type.
    /// </summary>
    public bool PlaySound();

    /// <summary>
    ///   Disposes of any invalid audio and reloads the latest audio from the current data.
    /// </summary>
    public void UpdateAudio();
}

public interface IAudioConfigData
{
    /// <summary>
    ///   How the alert should be processed.
    /// </summary>
    public AlertKind AlertKind { get; set; }

    /// <summary>
    ///   The file path to the custom audio file.
    /// </summary>
    public string AlertCustomPath { get; set; }

    /// <summary>
    ///   How loud the custom audio is played.
    /// </summary>
    public float AlertVolume { get; set; }

    /// <summary>
    ///   The native game soundbyte value.
    /// </summary>
    public Sounds AlertSoundbyte { get; set; }

    /// <summary>
    ///   If we use the custom audio over the native one.
    /// </summary>
    public bool AlertIsCustom { get; set; }
}
