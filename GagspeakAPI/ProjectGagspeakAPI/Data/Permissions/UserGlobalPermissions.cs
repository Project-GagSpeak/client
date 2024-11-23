using GagspeakAPI.Extensions;
using MessagePack;

namespace GagspeakAPI.Data.Permissions;

[MessagePackObject(keyAsPropertyName: true)]
public record UserGlobalPermissions
{
    // main global permissions
    public bool SafewordUsed { get; set; } = false;             // DO NOT ALLOW THIS TO BE MODIFABLE
    public bool HardcoreSafewordUsed { get; set; } = false;     // DO NOT ALLOW THIS TO BE MODIFABLE
    public bool LiveChatGarblerActive { get; set; } = false;    // if the live chat garbler is active
    public bool LiveChatGarblerLocked { get; set; } = false;    // if the live chat garbler is locked in an active state.


    // wardrobe global modifiable permissions
    public bool WardrobeEnabled { get; set; } = false;          // PROFILE VIEWABLE OPT-IN || If the user's wardrobe component is active
    public bool ItemAutoEquip { get; set; } = false;            // if the user allows items to be auto-equipped
    public bool RestraintSetAutoEquip { get; set; } = false;    // if the user allows restraint sets to be auto-equipped


    // global puppeteer modifiable permissions.
    public bool PuppeteerEnabled { get; set; } = false;         // PROFILE VIEWABLE OPT-IN || If the user's puppeteer component is active
    public string GlobalTriggerPhrase { get; set; } = "";       // PROFILE VIEWABLE OPT-IN || Global trigger phrase for the user
    public bool GlobalAllowSitRequests { get; set; } = false;   // PROFILE VIEWABLE OPT-IN || If user allows sit requests
    public bool GlobalAllowMotionRequests { get; set; } = false;// PROFILE VIEWABLE OPT-IN || If the user allows motion requests
    public bool GlobalAllowAllRequests { get; set; } = false;   // PROFILE VIEWABLE OPT-IN || READONLY || If the user allows all requests

    // global moodles modifiable permissions
    public bool MoodlesEnabled { get; set; } = false;           // PROFILE VIEWABLE OPT-IN || If the user's moodles component is active

    // global toybox modifiable permissions
    public bool ToyboxEnabled { get; set; } = false;            // PROFILE VIEWABLE OPT-IN || If the user's toybox component is active
    public bool LockToyboxUI { get; set; } = false;             // if the user's toybox UI is locked
    public bool ToyIsActive { get; set; } = false;              // if the user's toy is active
    public bool ShockCollarIsActive { get; set; } = false;      // if the user's shock collar is active
    public int ToyIntensity { get; set; } = 0;                  // the intensity of the user's toy
    public bool SpatialVibratorAudio { get; set; } = false;     // if the user's toybox local audio is active

    // global hardcore permissions (readonly for everyone)
    // Contains the UID who applied it when active. If Devotional, will have    |pairlocked    appended.
    public string ForcedFollow { get; set; } = string.Empty;
    public string ForcedEmoteState { get; set; } = string.Empty;
    public string ForcedStay { get; set; } = string.Empty;
    public string ForcedBlindfold { get; set; } = string.Empty;
    public string ChatBoxesHidden { get; set; } = string.Empty;
    public string ChatInputHidden { get; set; } = string.Empty;
    public string ChatInputBlocked { get; set; } = string.Empty;


    // Global PiShock Permissions & Helpers.
    public string GlobalShockShareCode { get; set; } = "";
    public bool AllowShocks { get; set; } = false;
    public bool AllowVibrations { get; set; } = false;
    public bool AllowBeeps { get; set; } = false;
    public int MaxIntensity { get; set; } = -1;
    public int MaxDuration { get; set; } = -1;
    public TimeSpan GlobalShockVibrateDuration { get; set; } = TimeSpan.Zero;

    public bool HasValidShareCode() => !GlobalShockShareCode.NullOrEmpty() && MaxDuration > 0;
    public TimeSpan GetTimespanFromDuration()
    {
        if (MaxDuration > 15 && MaxDuration < 100)
        {
            return TimeSpan.Zero; // invalid range.
        }
        else if (MaxDuration >= 100 && MaxDuration <= 15000)
        {
            return TimeSpan.FromMilliseconds(MaxDuration); // convert to milliseconds
        }
        else
        {
            return TimeSpan.FromSeconds(MaxDuration); // convert to seconds
        }
    }

}
