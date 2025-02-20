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
    public void DrawPairActionFunctions()
    {
        /* ----------- GLOBAL SETTINGS ----------- */
        ImGui.TextUnformatted("Common Pair Functions");

        // draw the common client functions
        DrawCommonClientMenu();

        if (StickyPair != null && StickyPair.IsOnline)
        {
            // Online Pair Actions
            if (StickyPair.LastGagData != null)
            {
                ImGui.TextUnformatted("Gag Actions");
                DrawGagActions();
            }
            else if (StickyPair.LastRestrictionsData != null)
            {
                ImGui.TextUnformatted("Restrictions Actions");
                // DrawRestrictionsActions();
            }
            else if (StickyPair.LastRestraintData != null)
            {
                ImGui.TextUnformatted("Wardrobe Actions");
                DrawWardrobeActions();
            }
            else if (StickyPair.LastIpcData != null && StickyPair.IsVisible)
            {
                ImGui.TextUnformatted("Moodles Actions");
                DrawMoodlesActions();
            }
            else if (StickyPair.LastToyboxData != null)
            {
                ImGui.TextUnformatted("Toybox Actions");
                DrawToyboxActions();
            }
            else if (StickyPair.PairPerms.InHardcore)
            {
                ImGui.TextUnformatted("Hardcore Actions");
                DrawHardcoreActions();
            }
            else if (StickyPair.PairPerms.InHardcore && (UniqueShockCollarPermsExist() || GlobalShockCollarPermsExist()))
            {
                ImGui.TextUnformatted("Hardcore Shock Collar Actions.");
                DrawHardcoreShockCollarActions();
            }
        }

        // individual Menu
        ImGui.TextUnformatted("Individual Pair Functions");
        DrawIndividualMenu();
    }

    private bool UniqueShockCollarPermsExist() => !StickyPair.PairPerms.HasValidShareCode();
    private bool GlobalShockCollarPermsExist() => !StickyPair.PairGlobals.HasValidShareCode();

    private void DrawCommonClientMenu()
    {
        if (!StickyPair.IsPaused)
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.User, "Open Profile", WindowMenuWidth, true))
            {
                Mediator.Publish(new KinkPlateOpenStandaloneMessage(StickyPair));
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

        if (StickyPair.IsOnline)
        {
            var pauseIcon = OwnPerms.IsPaused ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;
            var pauseText = OwnPerms.IsPaused ? "Unpause " + PairNickOrAliasOrUID : "Pause " + PairNickOrAliasOrUID;
            if (_uiShared.IconTextButton(pauseIcon, pauseText, WindowMenuWidth, true))
            {
                _hub.UserUpdateOwnPairPerm(new(StickyPair.UserData, MainHub.PlayerUserData,
                    new KeyValuePair<string, object>("IsPaused", !OwnPerms.IsPaused), UpdateDir.Own)).ConfigureAwait(false);
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

    private void DrawIndividualMenu()
    {
        if (_uiShared.IconTextButton(FontAwesomeIcon.Trash, "Unpair Permanently", WindowMenuWidth, true, !KeyMonitor.CtrlPressed()))
            _hub.UserRemovePair(new(StickyPair.UserData)).ConfigureAwait(false);
        UiSharedService.AttachToolTip("Hold CTRL and click to unpair permanently from " + PairNickOrAliasOrUID);
    }
}
