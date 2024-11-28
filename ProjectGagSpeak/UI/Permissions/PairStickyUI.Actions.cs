using Dalamud.Interface;
using Dalamud.Utility;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Enums;
using ImGuiNET;

namespace GagSpeak.UI.Permissions;

/// <summary>
/// Contains functions relative to the paired users permissions for the client user.
/// 
/// Yes its messy, yet it's long, but i functionalized it best i could for the insane 
/// amount of logic being performed without adding too much overhead.
/// </summary>
public partial class PairStickyUI
{
    public InteractionType Opened = InteractionType.None;
    public void DrawPairActionFunctions()
    {
        /* ----------- GLOBAL SETTINGS ----------- */
        ImGui.TextUnformatted("Common Pair Functions");

        // draw the common client functions
        DrawCommonClientMenu();

        if (StickyPair != null && StickyPair.IsOnline)
        {
            // Online Pair Actions
            if (StickyPair.LastAppearanceData != null)
            {
                ImGui.TextUnformatted("Gag Actions");
                DrawGagActions();
            }

            if (StickyPair.LastWardrobeData != null)
            {
                ImGui.TextUnformatted("Wardrobe Actions");
                DrawWardrobeActions();
            }

            if (StickyPair.LastAliasData != null)
            {
                ImGui.TextUnformatted("Puppeteer Actions");
                DrawPuppeteerActions();
            }

            if (StickyPair.LastIpcData != null && StickyPair.IsVisible)
            {
                ImGui.TextUnformatted("Moodles Actions");
                DrawMoodlesActions();
            }

            if (StickyPair.LastToyboxData != null)
            {
                ImGui.TextUnformatted("Toybox Actions");
                DrawToyboxActions();
            }

            if (StickyPair.PairPerms.InHardcore)
            {
                ImGui.TextUnformatted("Hardcore Actions");
                DrawHardcoreActions();
            }

            if (StickyPair.PairPerms.InHardcore && (UniqueShockCollarPermsExist() || GlobalShockCollarPermsExist()))
            {
                ImGui.TextUnformatted("Hardcore Shock Collar Actions.");
                DrawHardcoreShockCollarActions();
            }
        }

        // individual Menu
        ImGui.TextUnformatted("Individual Pair Functions");
        DrawIndividualMenu();
    }

    private bool UniqueShockCollarPermsExist() => !StickyPair.PairPerms.ShockCollarShareCode.IsNullOrEmpty() && StickyPair.PairGlobals.MaxIntensity != -1;
    private bool GlobalShockCollarPermsExist() => !StickyPair.PairGlobals.GlobalShockShareCode.IsNullOrEmpty() && StickyPair.PairGlobals.MaxIntensity != -1;

    private void DrawCommonClientMenu()
    {
        if (!StickyPair.IsPaused)
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.User, "Open Profile", WindowMenuWidth, true))
            {
                _displayHandler.OpenProfile(StickyPair);
                ImGui.CloseCurrentPopup();
            }
            UiSharedService.AttachToolTip("Opens the profile for this user in a new window");
        }

        if (!StickyPair.IsPaused)
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.ExclamationTriangle, "Report "+ PairNickOrAliasOrUID +"'s KinkPlate", WindowMenuWidth, true))
            {
                ImGui.CloseCurrentPopup();
                Mediator.Publish(new ReportKinkPlateMessage(StickyPair.UserData));
            }
            UiSharedService.AttachToolTip("Snapshot "+ PairNickOrAliasOrUID+"'s KinkPlate and send it as a reported profile.");
        }

        if (StickyPair.IsPaired)
        {
            var pauseIcon = OwnPerms.IsPaused ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;
            var pauseText = OwnPerms.IsPaused ? "Unpause " + PairNickOrAliasOrUID : "Pause " + PairNickOrAliasOrUID;
            if (_uiShared.IconTextButton(pauseIcon, pauseText, WindowMenuWidth, true))
            {
                _ = _apiHubMain.UserUpdateOwnPairPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("IsPaused", !OwnPerms.IsPaused), UpdateDir.Own));
            }
            UiSharedService.AttachToolTip(!OwnPerms.IsPaused
                ? "Pause pairing with " + PairNickOrAliasOrUID : "Resume pairing with " + PairNickOrAliasOrUID);
        }
        if (StickyPair.IsVisible)
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.Sync, "Reload IPC data", WindowMenuWidth, true))
            {
                StickyPair.ApplyLastIpcData(forced: true);
                ImGui.CloseCurrentPopup();
            }
            UiSharedService.AttachToolTip("This reapplies the latest data from Customize+ and Moodles");
        }

        ImGui.Separator();
    }

    private void DrawPuppeteerActions()
    {
        // draw the Alias List popout ref button. (opens a popout window 
        if (_uiShared.IconTextButton(FontAwesomeIcon.Sync, "Update " + PairNickOrAliasOrUID + " with your Name", WindowMenuWidth, true))
        {
            var name = _clientService.Name;
            var world = _clientService.HomeWorldId;
            var worldName = OnFrameworkService.WorldData.Value[(ushort)world];
            // compile the alias data to send including our own name and world information, along with an empty alias list.
            var dataToPush = new CharaAliasData()
            {
                ListenerName = name + "@" + worldName,
                AliasList = new List<AliasTrigger>()
            };

            _ = _apiHubMain.UserPushPairDataAliasStorageUpdate(new(StickyPair.UserData, MainHub.PlayerUserData, dataToPush, PuppeteerUpdateType.PlayerNameRegistered, UpdateDir.Own));
            _logger.LogDebug("Sent Puppeteer Name to " + PairNickOrAliasOrUID, LoggerType.Permissions);
        }
        UiSharedService.AttachToolTip("Sends your Name & World to this pair so their puppeteer will listen for messages from you.");
        ImGui.Separator();
    }

    private void DrawIndividualMenu()
    {
        if (StickyPair.IndividualPairStatus != IndividualPairStatus.None)
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.Trash, "Unpair Permanently", WindowMenuWidth, true, !KeyMonitor.CtrlPressed()))
            {
                _ = _apiHubMain.UserRemovePair(new(StickyPair.UserData));
            }
            UiSharedService.AttachToolTip("Hold CTRL and click to unpair permanently from " + PairNickOrAliasOrUID);
        }
    }
}
