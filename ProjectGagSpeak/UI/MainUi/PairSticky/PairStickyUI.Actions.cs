using Dalamud.Interface;
using Dalamud.Utility;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.Components;
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

        if (SPair != null && SPair.IsOnline)
        {
            // Online Pair Actions
            if (SPair.LastGagData != null)
            {
                ImGui.TextUnformatted("Gag Actions");
                DrawGagActions();
            }
            else if (SPair.LastRestrictionsData != null)
            {
                ImGui.TextUnformatted("Restrictions Actions");
                // DrawRestrictionsActions();
            }
            else if (SPair.LastRestraintData != null)
            {
                ImGui.TextUnformatted("Wardrobe Actions");
                DrawWardrobeActions();
            }
            else if (SPair.LastIpcData != null && SPair.IsVisible)
            {
                ImGui.TextUnformatted("Moodles Actions");
                DrawMoodlesActions();
            }
            else if (SPair.LastToyboxData != null)
            {
                ImGui.TextUnformatted("Toybox Actions");
                DrawToyboxActions();
            }
            else if (SPair.PairPerms.InHardcore)
            {
                ImGui.TextUnformatted("Hardcore Actions");
                DrawHardcoreActions();
            }
            else if (SPair.PairPerms.InHardcore && (UniqueShockCollarPermsExist() || GlobalShockCollarPermsExist()))
            {
                ImGui.TextUnformatted("Hardcore Shock Collar Actions.");
                DrawHardcoreShockCollarActions();
            }
        }

        // individual Menu
        ImGui.TextUnformatted("Individual Pair Functions");
        DrawIndividualMenu();
    }

    private bool UniqueShockCollarPermsExist() => !SPair.PairPerms.HasValidShareCode();
    private bool GlobalShockCollarPermsExist() => !SPair.PairGlobals.HasValidShareCode();

    private void DrawCommonClientMenu()
    {
        if (!SPair.IsPaused)
        {
            if (_ui.IconTextButton(FontAwesomeIcon.User, "Open Profile", WindowMenuWidth, true))
            {
                Mediator.Publish(new KinkPlateOpenStandaloneMessage(SPair));
                ImGui.CloseCurrentPopup();
            }
            UiSharedService.AttachToolTip("Opens the profile for this user in a new window");
        }

        if (!SPair.IsPaused)
        {
            if (_ui.IconTextButton(FontAwesomeIcon.ExclamationTriangle, "Report "+ PermissionData.DispName +"'s KinkPlate", WindowMenuWidth, true))
            {
                ImGui.CloseCurrentPopup();
                Mediator.Publish(new ReportKinkPlateMessage(SPair.UserData));
            }
            UiSharedService.AttachToolTip("Snapshot "+ PermissionData.DispName+"'s KinkPlate and send it as a reported profile.");
        }

        if (SPair.IsOnline)
        {
            var pauseIcon = SPair.OwnPerms.IsPaused ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;
            var pauseText = SPair.OwnPerms.IsPaused ? "Unpause " + PermissionData.DispName : "Pause " + PermissionData.DispName;
            if (_ui.IconTextButton(pauseIcon, pauseText, WindowMenuWidth, true))
            {
                _hub.UserUpdateOwnPairPerm(new(SPair.UserData, MainHub.PlayerUserData,
                    new KeyValuePair<string, object>("IsPaused", !SPair.OwnPerms.IsPaused), UpdateDir.Own)).ConfigureAwait(false);
            }
            UiSharedService.AttachToolTip(!SPair.OwnPerms.IsPaused
                ? "Pause pairing with " + PermissionData.DispName : "Resume pairing with " + PermissionData.DispName);
        }
        if (SPair.IsVisible)
        {
            if (_ui.IconTextButton(FontAwesomeIcon.Sync, "Reload IPC data", WindowMenuWidth, true))
            {
                SPair.ApplyLastIpcData(forced: true);
                ImGui.CloseCurrentPopup();
            }
            UiSharedService.AttachToolTip("This reapplies the latest data from Customize+ and Moodles");
        }

        ImGui.Separator();
    }

    private void DrawIndividualMenu()
    {
        if (_ui.IconTextButton(FontAwesomeIcon.Trash, "Unpair Permanently", WindowMenuWidth, true, !KeyMonitor.CtrlPressed()))
            _hub.UserRemovePair(new(SPair.UserData)).ConfigureAwait(false);
        UiSharedService.AttachToolTip("Hold CTRL and click to unpair permanently from " + PermissionData.DispName);
    }
}
