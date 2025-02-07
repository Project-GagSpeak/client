using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Common.Lua;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI.Components.Combos;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.Permissions;

/// <summary>
/// Contains functions relative to the paired users permissions for the client user.
/// 
/// Yes its messy, yet it's long, but i functionalized it best i could for the insane 
/// amount of logic being performed without adding too much overhead.
/// </summary>
public partial class PairStickyUI
{
    private Emote? SelectedEmote = null;
    private int SelectedCPose = 0;
    private void DrawHardcoreActions()
    {
        if(_clientData.GlobalPerms is null) return;

        if(MainHub.UID is null)
        {
            _logger.LogWarning("MainHub.UID is null, cannot draw hardcore actions.");
            return;
        }

        // conditions for disabled actions
        bool inRange = _clientService.IsPresent && StickyPair.VisiblePairGameObject is not null 
            && Vector3.Distance(_clientService.ClientPlayer!.Position, StickyPair.VisiblePairGameObject.Position) < 3;
        // Conditionals for hardcore interactions
        bool disableForceFollow = !inRange || !PairPerms.AllowForcedFollow || !StickyPair.IsVisible || !PairGlobals.CanToggleFollow(MainHub.UID);
        bool disableForceToStay = !PairPerms.AllowForcedToStay || !PairGlobals.CanToggleStay(MainHub.UID);
        bool disableBlindfoldToggle = !PairPerms.AllowBlindfold || !PairGlobals.CanToggleBlindfold(MainHub.UID);
        bool disableChatVisibilityToggle = !PairPerms.AllowHidingChatBoxes || !PairGlobals.CanToggleChatHidden(MainHub.UID);
        bool disableChatInputVisibilityToggle = !PairPerms.AllowHidingChatInput || !PairGlobals.CanToggleChatInputHidden(MainHub.UID);
        bool disableChatInputBlockToggle = !PairPerms.AllowChatInputBlocking || !PairGlobals.CanToggleChatInputBlocked(MainHub.UID);
        bool pairAllowsDevotionalToggles = PairPerms.DevotionalStatesForPair;

        var forceFollowIcon = PairGlobals.IsFollowing() ? FontAwesomeIcon.StopCircle : FontAwesomeIcon.PersonWalkingArrowRight;
        var forceFollowText = PairGlobals.IsFollowing() ? $"Have {PairNickOrAliasOrUID} stop following you." : $"Make {PairNickOrAliasOrUID} follow you.";
        if (_uiShared.IconTextButton(forceFollowIcon, forceFollowText, WindowMenuWidth, true, disableForceFollow))
        {
            string newStr = PairGlobals.IsFollowing() ? string.Empty : MainHub.UID + (pairAllowsDevotionalToggles ? Globals.DevotedString : string.Empty);
            _ = _apiHubMain.UserUpdateOtherGlobalPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ForcedFollow", newStr), UpdateDir.Other));
        }
        
        DrawForcedEmoteSection();

        var forceToStayIcon = PairGlobals.IsStaying() ? FontAwesomeIcon.StopCircle : FontAwesomeIcon.HouseLock;
        var forceToStayText = PairGlobals.IsStaying() ? $"Release {PairNickOrAliasOrUID}." : $"Lock away {PairNickOrAliasOrUID}.";
        if (_uiShared.IconTextButton(forceToStayIcon, forceToStayText, WindowMenuWidth, true, disableForceToStay, "##ForcedToStayHardcoreAction"))
        {
            string newStr = PairGlobals.IsStaying() ? string.Empty : MainHub.UID + (pairAllowsDevotionalToggles ? Globals.DevotedString : string.Empty);
            _ = _apiHubMain.UserUpdateOtherGlobalPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ForcedStay", newStr), UpdateDir.Other));
        }

        var toggleBlindfoldIcon = PairGlobals.IsBlindfolded() ? FontAwesomeIcon.StopCircle : FontAwesomeIcon.Mask;
        var toggleBlindfoldText = PairGlobals.IsBlindfolded() ? $"Remove {PairNickOrAliasOrUID}'s Blindfold." : $"Blindfold {PairNickOrAliasOrUID}.";
        if (_uiShared.IconTextButton(toggleBlindfoldIcon, toggleBlindfoldText, WindowMenuWidth, true, disableBlindfoldToggle, "##ForcedToBeBlindfoldedHardcoreAction"))
        {
            string newStr = PairGlobals.IsBlindfolded() ? string.Empty : MainHub.UID + (pairAllowsDevotionalToggles ? Globals.DevotedString : string.Empty);
            _ = _apiHubMain.UserUpdateOtherGlobalPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ForcedBlindfold", newStr), UpdateDir.Other));
        }

        var toggleChatboxIcon = PairGlobals.IsChatHidden() ? FontAwesomeIcon.StopCircle : FontAwesomeIcon.CommentSlash;
        var toggleChatboxText = PairGlobals.IsChatHidden() ? "Make " + PairNickOrAliasOrUID + "'s Chat Visible." : "Hide "+PairNickOrAliasOrUID+"'s Chat Window.";
        if (_uiShared.IconTextButton(toggleChatboxIcon, toggleChatboxText, WindowMenuWidth, true, disableChatVisibilityToggle, "##ForcedChatboxVisibilityHardcoreAction"))
        {
            string newStr = PairGlobals.IsChatHidden() ? string.Empty : MainHub.UID + (pairAllowsDevotionalToggles ? Globals.DevotedString : string.Empty);
            _ = _apiHubMain.UserUpdateOtherGlobalPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ChatBoxesHidden", newStr), UpdateDir.Other));
        }

        var toggleChatInputIcon = PairGlobals.IsChatInputHidden() ? FontAwesomeIcon.StopCircle : FontAwesomeIcon.CommentSlash;
        var toggleChatInputText = PairGlobals.IsChatInputHidden() ? "Make " + PairNickOrAliasOrUID + "'s Chat Input Visible." : "Hide "+PairNickOrAliasOrUID+"'s Chat Input.";
        if (_uiShared.IconTextButton(toggleChatInputIcon, toggleChatInputText, WindowMenuWidth, true, disableChatInputVisibilityToggle, "##ForcedChatInputVisibilityHardcoreAction"))
        {
            string newStr = PairGlobals.IsChatInputHidden() ? string.Empty : MainHub.UID + (pairAllowsDevotionalToggles ? Globals.DevotedString : string.Empty);
            _ = _apiHubMain.UserUpdateOtherGlobalPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ChatInputHidden", newStr), UpdateDir.Other));
        }

        var toggleChatBlockingIcon = PairGlobals.IsChatInputBlocked() ? FontAwesomeIcon.StopCircle : FontAwesomeIcon.CommentDots;
        var toggleChatBlockingText = PairGlobals.IsChatInputBlocked() ? "Reallow "+PairNickOrAliasOrUID+"'s Chat Input." : "Block "+PairNickOrAliasOrUID+"'s Chat Input.";
        if (_uiShared.IconTextButton(toggleChatBlockingIcon, toggleChatBlockingText, WindowMenuWidth, true, disableChatInputBlockToggle, "##BlockedChatInputHardcoreAction"))
        {
            string newStr = PairGlobals.IsChatInputBlocked() ? string.Empty : MainHub.UID + (pairAllowsDevotionalToggles ? Globals.DevotedString : string.Empty);
            _ = _apiHubMain.UserUpdateOtherGlobalPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ChatInputBlocked", newStr), UpdateDir.Other));
        }
        ImGui.Separator();
    }

    private void DrawForcedEmoteSection()
    {
        var canToggleEmoteState = PairGlobals.CanToggleEmoteState(MainHub.UID);
        bool disableForceSit = !PairPerms.AllowForcedSit || !canToggleEmoteState;
        bool disableForceEmoteState = !PairPerms.AllowForcedEmote || !canToggleEmoteState;

        if(!PairGlobals.ForcedEmoteState.NullOrEmpty())
        {
            //////////////////// DRAW OUT FOR STOPPING FORCED EMOTE HERE /////////////////////
            if (_uiShared.IconTextButton(FontAwesomeIcon.StopCircle, "Let "+PairNickOrAliasOrUID+" move again.", WindowMenuWidth, true, id: "##ForcedToStayHardcoreAction"))
                _ = _apiHubMain.UserUpdateOtherGlobalPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ForcedEmoteState", string.Empty), UpdateDir.Other));
        }
        else
        {
            var forceEmoteIcon = PairPerms.AllowForcedEmote ? FontAwesomeIcon.PersonArrowDownToLine : FontAwesomeIcon.Chair;
            var forceEmoteText = PairPerms.AllowForcedEmote ? $"Force {PairNickOrAliasOrUID} into an Emote State." : $"Force {PairNickOrAliasOrUID} to Sit.";
            //////////////////// DRAW OUT FOR FORCING EMOTE STATE HERE /////////////////////
            if (_uiShared.IconTextButton(forceEmoteIcon, forceEmoteText, WindowMenuWidth, true, disableForceSit && disableForceEmoteState, "##ForcedEmoteAction"))
            {
                PairCombos.Opened = PairCombos.Opened == InteractionType.ForcedEmoteState ? InteractionType.None : InteractionType.ForcedEmoteState;
            }
            UiSharedService.AttachToolTip("Force " + PairNickOrAliasOrUID + "To Perform any Looped Emote State.");
            if (PairCombos.Opened is InteractionType.ForcedEmoteState)
            {
                using (var actionChild = ImRaii.Child("ForcedEmoteStateActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), false))
                {
                    if (!actionChild) return;

                    var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Force State").X - ImGui.GetStyle().ItemInnerSpacing.X;
                    // Have User select the emote they want.
                    var listToShow = disableForceEmoteState ? EmoteMonitor.SitEmoteComboList : EmoteMonitor.EmoteComboList;
                    if(SelectedEmote is null)
                        SelectedEmote = listToShow.First();

                    _uiShared.DrawComboSearchable("EmoteList", WindowMenuWidth, listToShow, chosen => chosen.ComboEmoteName(), 
                        false, (chosen) => SelectedEmote = chosen, SelectedEmote.Value);
                    // Only allow setting the CPose State if the emote is a sitting one.
                    using (ImRaii.Disabled(!EmoteMonitor.IsAnyPoseWithCyclePose((ushort)(SelectedEmote?.RowId ?? 0))))
                    {
                        // Get the Max CyclePoses for this emote.
                        int maxCycles = EmoteMonitor.EmoteCyclePoses((ushort)(SelectedEmote?.RowId ?? 0));
                        if (maxCycles is 0) SelectedCPose = 0;
                        // Draw out the slider for the enforced cycle pose.
                        ImGui.SetNextItemWidth(width);
                        ImGui.SliderInt("##EnforceCyclePose", ref SelectedCPose, 0, maxCycles);
                    }
                    ImUtf8.SameLineInner();
                    try
                    {
                        if (ImGui.Button("Force State##ForceEmoteStateTo" + PairNickOrAliasOrUID))
                        {
                            // Compile the string for sending.
                            string newStr = MainHub.UID + "|" + SelectedEmote?.RowId.ToString() + "|" + SelectedCPose.ToString() + (PairPerms.DevotionalStatesForPair ? Globals.DevotedString : string.Empty);
                            _logger.LogDebug("Sending EmoteState update for emote: " + (SelectedEmote?.Name.ToString()));
                            _ = _apiHubMain.UserUpdateOtherGlobalPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ForcedEmoteState", newStr), UpdateDir.Other));
                            PairCombos.Opened = InteractionType.None;
                        }
                    }
                    catch (Exception e) { _logger.LogError("Failed to push EmoteState Update: " + e.Message); }
                }
                ImGui.Separator();
            }
        }
    }


    private int Intensity = 0;
    private int VibrateIntensity = 0;
    private float Duration = 0;
    private float VibeDuration = 0;
    private void DrawHardcoreShockCollarActions()
    {
        // the permissions to reference.
        bool AllowShocks = PairPerms.HasValidShareCode() ? PairPerms.AllowShocks : StickyPair.PairGlobals.AllowShocks;
        bool AllowVibrations = PairPerms.HasValidShareCode() ? PairPerms.AllowVibrations : StickyPair.PairGlobals.AllowVibrations;
        bool AllowBeeps = PairPerms.HasValidShareCode() ? PairPerms.AllowBeeps : StickyPair.PairGlobals.AllowBeeps;
        int MaxIntensity = PairPerms.HasValidShareCode() ? PairPerms.MaxIntensity : StickyPair.PairGlobals.MaxIntensity;
        TimeSpan maxVibeDuration = PairPerms.HasValidShareCode() ? PairPerms.GetTimespanFromDuration() : StickyPair.PairGlobals.GetTimespanFromDuration();
        string piShockShareCodePref = PairPerms.HasValidShareCode() ? PairPerms.ShockCollarShareCode : StickyPair.PairGlobals.GlobalShockShareCode;

        if (_uiShared.IconTextButton(FontAwesomeIcon.BoltLightning, "Shock " + PairNickOrAliasOrUID + "'s Shock Collar", WindowMenuWidth, true, !AllowShocks))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.ShockAction ? InteractionType.None : InteractionType.ShockAction;
        }
        UiSharedService.AttachToolTip("Perform a Shock action to " + PairUID + "'s Shock Collar.");

        if (PairCombos.Opened is InteractionType.ShockAction)
        {
            using (var actionChild = ImRaii.Child("ShockCollarActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), false))
            {
                if (!actionChild) return;

                var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Send Shock").X - ImGui.GetStyle().ItemInnerSpacing.X;

                ImGui.SetNextItemWidth(WindowMenuWidth);
                ImGui.SliderInt("##IntensitySliderRef" + PairNickOrAliasOrUID, ref Intensity, 0, MaxIntensity, "%d%%", ImGuiSliderFlags.None);
                ImGui.SetNextItemWidth(width);
                ImGui.SliderFloat("##DurationSliderRef" + PairNickOrAliasOrUID, ref Duration, 0.0f, ((float)maxVibeDuration.TotalMilliseconds / 1000f), "%.1fs", ImGuiSliderFlags.None);
                ImUtf8.SameLineInner();
                try
                {
                    if (ImGui.Button("Send Shock##SendShockToShockCollar" + PairNickOrAliasOrUID))
                    {
                        int newMaxDuration;
                        if (Duration % 1 == 0 && Duration >= 1 && Duration <= 15) { newMaxDuration = (int)Duration; }
                        else { newMaxDuration = (int)(Duration * 1000); }

                        _logger.LogDebug("Sending Shock to Shock Collar with duration: " + newMaxDuration + "(milliseconds)");
                        _ = _apiHubMain.UserShockActionOnPair(new ShockCollarActionDto(StickyPair.UserData, 0, Intensity, newMaxDuration));
                        UnlocksEventManager.AchievementEvent(UnlocksEvent.ShockSent);
                        PairCombos.Opened = InteractionType.None;
                    }
                }
                catch (Exception e) { _logger.LogError("Failed to push ShockCollar Shock message: " + e.Message); }
            }
            ImGui.Separator();
        }

        if (_uiShared.IconTextButton(FontAwesomeIcon.WaveSquare, "Vibrate " + PairNickOrAliasOrUID + "'s Shock Collar", WindowMenuWidth, true, false))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.VibrateAction ? InteractionType.None : InteractionType.VibrateAction;
        }
        UiSharedService.AttachToolTip("Perform a Vibrate action to " + PairUID + "'s Shock Collar.");

        if (PairCombos.Opened is InteractionType.VibrateAction)
        {
            using (var actionChild = ImRaii.Child("VibrateCollarActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), false))
            {
                if (!actionChild) return;

                var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Send Vibration").X - ImGui.GetStyle().ItemInnerSpacing.X;

                // draw a slider float that references the duration, going from 0.1f to 15f by a scaler of 0.1f that displays X.Xs
                ImGui.SetNextItemWidth(WindowMenuWidth);
                ImGui.SliderInt("##IntensitySliderRef" + PairNickOrAliasOrUID, ref VibrateIntensity, 0, 100, "%d%%", ImGuiSliderFlags.None);
                ImGui.SetNextItemWidth(width);
                ImGui.SliderFloat("##DurationSliderRef" + PairNickOrAliasOrUID, ref VibeDuration, 0.0f, ((float)maxVibeDuration.TotalMilliseconds / 1000f), "%.1fs", ImGuiSliderFlags.None);
                ImUtf8.SameLineInner();
                try
                {
                    if (ImGui.Button("Send Vibration##SendVibrationToShockCollar" + PairNickOrAliasOrUID))
                    {
                        int newMaxDuration;
                        if (VibeDuration % 1 == 0 && VibeDuration >= 1 && VibeDuration <= 15) { newMaxDuration = (int)VibeDuration; }
                        else { newMaxDuration = (int)(VibeDuration * 1000); }

                        _logger.LogDebug("Sending Vibration to Shock Collar with duration: " + newMaxDuration + "(milliseconds)");
                        _ = _apiHubMain.UserShockActionOnPair(new ShockCollarActionDto(StickyPair.UserData, 1, VibrateIntensity, newMaxDuration));
                        PairCombos.Opened = InteractionType.None;
                    }
                }
                catch (Exception e) { _logger.LogError("Failed to push ShockCollar Vibrate message: " + e.Message); }
            }
            ImGui.Separator();
        }

        if (_uiShared.IconTextButton(FontAwesomeIcon.LandMineOn, "Beep " + PairNickOrAliasOrUID + "'s Shock Collar", WindowMenuWidth, true, !AllowBeeps))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.BeepAction ? InteractionType.None : InteractionType.BeepAction;
        }
        UiSharedService.AttachToolTip("Beep " + PairUID + "'s Shock Collar.");

        if (PairCombos.Opened is InteractionType.BeepAction)
        {
            using (var actionChild = ImRaii.Child("BeepCollarActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!actionChild) return;

                var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Send Beep").X - ImGui.GetStyle().ItemInnerSpacing.X;

                // draw a slider float that references the duration, going from 0.1f to 15f by a scaler of 0.1f that displays X.Xs
                ImGui.SetNextItemWidth(width);
                ImGui.SliderFloat("##DurationSliderRef" + PairNickOrAliasOrUID, ref VibeDuration, 0.1f, ((float)maxVibeDuration.TotalMilliseconds / 1000f), "%.1fs", ImGuiSliderFlags.None);
                ImUtf8.SameLineInner();
                try
                {
                    if (ImGui.Button("Send Beep##SendBeepToShockCollar" + PairNickOrAliasOrUID))
                    {
                        int newMaxDuration;
                        if (VibeDuration % 1 == 0 && VibeDuration >= 1 && VibeDuration <= 15) { newMaxDuration = (int)VibeDuration; }
                        else { newMaxDuration = (int)(VibeDuration * 1000); }
                        _logger.LogDebug("Sending Beep to Shock Collar with duration: " + newMaxDuration + "(note that values between 1 and 15 are full seconds)");
                        _ = _apiHubMain.UserShockActionOnPair(new ShockCollarActionDto(StickyPair.UserData, 2, Intensity, newMaxDuration));
                        PairCombos.Opened = InteractionType.None;
                    }
                }
                catch (Exception e) { _logger.LogError("Failed to push ShockCollar Beep message: " + e.Message); }
            }
            ImGui.Separator();
        }
    }
}
