using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Permissions;

/// <summary>
/// Contains functions relative to the paired users permissions for the client user.
/// 
/// Yes its messy, yet it's long, but i functionalized it best i could for the insane 
/// amount of logic being performed without adding too much overhead.
/// </summary>
public partial class PairStickyUI
{
    private int SelectedCPose = 0;
    private void DrawHardcoreActions()
    {
        if(_globals.GlobalPerms is null) return;

        if(MainHub.UID is null)
        {
            _logger.LogWarning("MainHub.UID is null, cannot draw hardcore actions.");
            return;
        }

        // conditions for disabled actions
        var inRange = _monitor.IsPresent && SPair.VisiblePairGameObject is not null 
            && Vector3.Distance(_monitor.ClientPlayer!.Position, SPair.VisiblePairGameObject.Position) < 3;
        // Conditionals for hardcore interactions
        var disableForceFollow = !inRange || !SPair.PairPerms.AllowForcedFollow || !SPair.IsVisible || !SPair.PairGlobals.CanToggleFollow(MainHub.UID);
        var disableForceToStay = !SPair.PairPerms.AllowForcedStay || !SPair.PairGlobals.CanToggleStay(MainHub.UID);
        var disableChatVisibilityToggle = !SPair.PairPerms.AllowHidingChatBoxes || !SPair.PairGlobals.CanToggleChatHidden(MainHub.UID);
        var disableChatInputVisibilityToggle = !SPair.PairPerms.AllowHidingChatInput || !SPair.PairGlobals.CanToggleChatInputHidden(MainHub.UID);
        var disableChatInputBlockToggle = !SPair.PairPerms.AllowChatInputBlocking || !SPair.PairGlobals.CanToggleChatInputBlocked(MainHub.UID);
        var pairlockStates = SPair.PairPerms.PairLockedStates;

        var forceFollowIcon = SPair.PairGlobals.IsFollowing() ? FAI.StopCircle : FAI.PersonWalkingArrowRight;
        var forceFollowText = SPair.PairGlobals.IsFollowing() ? $"Have {PermissionData.DispName} stop following you." : $"Make {PermissionData.DispName} follow you.";
        if (CkGui.IconTextButton(forceFollowIcon, forceFollowText, WindowMenuWidth, true, disableForceFollow))
        {
            var newStr = SPair.PairGlobals.IsFollowing() ? string.Empty : MainHub.UID + (pairlockStates ? Constants.DevotedString : string.Empty);
            _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ForcedFollow", newStr), UpdateDir.Other));
        }
        
        DrawForcedEmoteSection();

        var forceToStayIcon = SPair.PairGlobals.IsStaying() ? FAI.StopCircle : FAI.HouseLock;
        var forceToStayText = SPair.PairGlobals.IsStaying() ? $"Release {PermissionData.DispName}." : $"Lock away {PermissionData.DispName}.";
        if (CkGui.IconTextButton(forceToStayIcon, forceToStayText, WindowMenuWidth, true, disableForceToStay, "##ForcedToStayHardcoreAction"))
        {
            var newStr = SPair.PairGlobals.IsStaying() ? string.Empty : MainHub.UID + (pairlockStates ? Constants.DevotedString : string.Empty);
            _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ForcedStay", newStr), UpdateDir.Other));
        }

        var toggleChatboxIcon = SPair.PairGlobals.IsChatHidden() ? FAI.StopCircle : FAI.CommentSlash;
        var toggleChatboxText = SPair.PairGlobals.IsChatHidden() ? "Make " + PermissionData.DispName + "'s Chat Visible." : "Hide "+PermissionData.DispName+"'s Chat Window.";
        if (CkGui.IconTextButton(toggleChatboxIcon, toggleChatboxText, WindowMenuWidth, true, disableChatVisibilityToggle, "##ForcedChatboxVisibilityHardcoreAction"))
        {
            var newStr = SPair.PairGlobals.IsChatHidden() ? string.Empty : MainHub.UID + (pairlockStates ? Constants.DevotedString : string.Empty);
            _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ChatBoxesHidden", newStr), UpdateDir.Other));
        }

        var toggleChatInputIcon = SPair.PairGlobals.IsChatInputHidden() ? FAI.StopCircle : FAI.CommentSlash;
        var toggleChatInputText = SPair.PairGlobals.IsChatInputHidden() ? "Make " + PermissionData.DispName + "'s Chat Input Visible." : "Hide "+PermissionData.DispName+"'s Chat Input.";
        if (CkGui.IconTextButton(toggleChatInputIcon, toggleChatInputText, WindowMenuWidth, true, disableChatInputVisibilityToggle, "##ForcedChatInputVisibilityHardcoreAction"))
        {
            var newStr = SPair.PairGlobals.IsChatInputHidden() ? string.Empty : MainHub.UID + (pairlockStates ? Constants.DevotedString : string.Empty);
            _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ChatInputHidden", newStr), UpdateDir.Other));
        }

        var toggleChatBlockingIcon = SPair.PairGlobals.IsChatInputBlocked() ? FAI.StopCircle : FAI.CommentDots;
        var toggleChatBlockingText = SPair.PairGlobals.IsChatInputBlocked() ? "Reallow "+PermissionData.DispName+"'s Chat Input." : "Block "+PermissionData.DispName+"'s Chat Input.";
        if (CkGui.IconTextButton(toggleChatBlockingIcon, toggleChatBlockingText, WindowMenuWidth, true, disableChatInputBlockToggle, "##BlockedChatInputHardcoreAction"))
        {
            var newStr = SPair.PairGlobals.IsChatInputBlocked() ? string.Empty : MainHub.UID + (pairlockStates ? Constants.DevotedString : string.Empty);
            _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ChatInputBlocked", newStr), UpdateDir.Other));
        }
        ImGui.Separator();
    }

    private void DrawForcedEmoteSection()
    {
        var canToggleEmoteState = SPair.PairGlobals.CanToggleEmoteState(MainHub.UID);
        var disableForceSit = !SPair.PairPerms.AllowForcedSit || !canToggleEmoteState;
        var disableForceEmoteState = !SPair.PairPerms.AllowForcedEmote || !canToggleEmoteState;

        if(!SPair.PairGlobals.ForcedEmoteState.NullOrEmpty())
        {
            //////////////////// DRAW OUT FOR STOPPING FORCED EMOTE HERE /////////////////////
            if (CkGui.IconTextButton(FAI.StopCircle, "Let "+PermissionData.DispName+" move again.", WindowMenuWidth, true, id: "##ForcedToStayHardcoreAction"))
                _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ForcedEmoteState", string.Empty), UpdateDir.Other));
        }
        else
        {
            var forceEmoteIcon = SPair.PairPerms.AllowForcedEmote ? FAI.PersonArrowDownToLine : FAI.Chair;
            var forceEmoteText = SPair.PairPerms.AllowForcedEmote ? $"Force {PermissionData.DispName} into an Emote State." : $"Force {PermissionData.DispName} to Sit.";
            //////////////////// DRAW OUT FOR FORCING EMOTE STATE HERE /////////////////////
            if (CkGui.IconTextButton(forceEmoteIcon, forceEmoteText, WindowMenuWidth, true, disableForceSit && disableForceEmoteState, "##ForcedEmoteAction"))
            {
                PairCombos.Opened = PairCombos.Opened == InteractionType.ForcedEmoteState ? InteractionType.None : InteractionType.ForcedEmoteState;
            }
            CkGui.AttachToolTip("Force " + PermissionData.DispName + "To Perform any Looped Emote State.");
            if (PairCombos.Opened is InteractionType.ForcedEmoteState)
            {
                using (var actionChild = ImRaii.Child("ForcedEmoteStateActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), false))
                {
                    if (!actionChild) return;

                    var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Force State").X - ImGui.GetStyle().ItemInnerSpacing.X;
                    // Have User select the emote they want.
                    var listToShow = disableForceEmoteState ? EmoteMonitor.SitEmoteComboList : EmoteMonitor.ValidEmotes;
                    _pairCombos.EmoteCombo.Draw("##EmoteCombo" + PermissionData.DispName, WindowMenuWidth, 1.3f, ImGui.GetFrameHeightWithSpacing());
                    // Only allow setting the CPose State if the emote is a sitting one.
                    using (ImRaii.Disabled(!EmoteMonitor.IsAnyPoseWithCyclePose((ushort)_pairCombos.EmoteCombo.Current.RowId)))
                    {
                        // Get the Max CyclePoses for this emote.
                        var maxCycles = EmoteMonitor.EmoteCyclePoses((ushort)_pairCombos.EmoteCombo.Current.RowId);
                        if (maxCycles is 0) SelectedCPose = 0;
                        // Draw out the slider for the enforced cycle pose.
                        ImGui.SetNextItemWidth(width);
                        ImGui.SliderInt("##EnforceCyclePose", ref SelectedCPose, 0, maxCycles);
                    }
                    ImUtf8.SameLineInner();
                    try
                    {
                        if (ImGui.Button("Force State##ForceEmoteStateTo" + PermissionData.DispName))
                        {
                            // Compile the string for sending.
                            var newStr = MainHub.UID + "|" + _pairCombos.EmoteCombo.Current.RowId.ToString() + "|" + SelectedCPose.ToString() + (SPair.PairPerms.PairLockedStates ? Constants.DevotedString : string.Empty);
                            _logger.LogDebug("Sending EmoteState update for emote: " + _pairCombos.EmoteCombo.Current.Name);
                            _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ForcedEmoteState", newStr), UpdateDir.Other));
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
        var AllowShocks = SPair.PairPerms.HasValidShareCode() ? SPair.PairPerms.AllowShocks : SPair.PairGlobals.AllowShocks;
        var AllowVibrations = SPair.PairPerms.HasValidShareCode() ? SPair.PairPerms.AllowVibrations : SPair.PairGlobals.AllowVibrations;
        var AllowBeeps = SPair.PairPerms.HasValidShareCode() ? SPair.PairPerms.AllowBeeps : SPair.PairGlobals.AllowBeeps;
        var MaxIntensity = SPair.PairPerms.HasValidShareCode() ? SPair.PairPerms.MaxIntensity : SPair.PairGlobals.MaxIntensity;
        var maxVibeDuration = SPair.PairPerms.HasValidShareCode() ? SPair.PairPerms.GetTimespanFromDuration() : SPair.PairGlobals.GetTimespanFromDuration();
        var piShockShareCodePref = SPair.PairPerms.HasValidShareCode() ? SPair.PairPerms.PiShockShareCode : SPair.PairGlobals.GlobalShockShareCode;

        if (CkGui.IconTextButton(FAI.BoltLightning, "Shock " + PermissionData.DispName + "'s Shock Collar", WindowMenuWidth, true, !AllowShocks))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.ShockAction ? InteractionType.None : InteractionType.ShockAction;
        }
        CkGui.AttachToolTip("Perform a Shock action to " + PermissionData.DispName + "'s Shock Collar.");

        if (PairCombos.Opened is InteractionType.ShockAction)
        {
            using (var actionChild = ImRaii.Child("ShockCollarActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), false))
            {
                if (!actionChild) return;

                var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Send Shock").X - ImGui.GetStyle().ItemInnerSpacing.X;

                ImGui.SetNextItemWidth(WindowMenuWidth);
                ImGui.SliderInt("##IntensitySliderRef" + PermissionData.DispName, ref Intensity, 0, MaxIntensity, "%d%%", ImGuiSliderFlags.None);
                ImGui.SetNextItemWidth(width);
                ImGui.SliderFloat("##DurationSliderRef" + PermissionData.DispName, ref Duration, 0.0f, ((float)maxVibeDuration.TotalMilliseconds / 1000f), "%.1fs", ImGuiSliderFlags.None);
                ImUtf8.SameLineInner();
                try
                {
                    if (ImGui.Button("Send Shock##SendShockToShockCollar" + PermissionData.DispName))
                    {
                        int newMaxDuration;
                        if (Duration % 1 == 0 && Duration >= 1 && Duration <= 15) { newMaxDuration = (int)Duration; }
                        else { newMaxDuration = (int)(Duration * 1000); }

                        _logger.LogDebug("Sending Shock to Shock Collar with duration: " + newMaxDuration + "(milliseconds)");
                        _ = _hub.UserShockActionOnPair(new PiShockAction(SPair.UserData, 0, Intensity, newMaxDuration));
                        UnlocksEventManager.AchievementEvent(UnlocksEvent.ShockSent);
                        PairCombos.Opened = InteractionType.None;
                    }
                }
                catch (Exception e) { _logger.LogError("Failed to push ShockCollar Shock message: " + e.Message); }
            }
            ImGui.Separator();
        }

        if (CkGui.IconTextButton(FAI.WaveSquare, "Vibrate " + PermissionData.DispName + "'s Shock Collar", WindowMenuWidth, true, false))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.VibrateAction ? InteractionType.None : InteractionType.VibrateAction;
        }
        CkGui.AttachToolTip("Perform a Vibrate action to " + PermissionData.DispName + "'s Shock Collar.");

        if (PairCombos.Opened is InteractionType.VibrateAction)
        {
            using (var actionChild = ImRaii.Child("VibrateCollarActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), false))
            {
                if (!actionChild) return;

                var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Send Vibration").X - ImGui.GetStyle().ItemInnerSpacing.X;

                // draw a slider float that references the duration, going from 0.1f to 15f by a scaler of 0.1f that displays X.Xs
                ImGui.SetNextItemWidth(WindowMenuWidth);
                ImGui.SliderInt("##IntensitySliderRef" + PermissionData.DispName, ref VibrateIntensity, 0, 100, "%d%%", ImGuiSliderFlags.None);
                ImGui.SetNextItemWidth(width);
                ImGui.SliderFloat("##DurationSliderRef" + PermissionData.DispName, ref VibeDuration, 0.0f, ((float)maxVibeDuration.TotalMilliseconds / 1000f), "%.1fs", ImGuiSliderFlags.None);
                ImUtf8.SameLineInner();
                try
                {
                    if (ImGui.Button("Send Vibration##SendVibrationToShockCollar" + PermissionData.DispName))
                    {
                        int newMaxDuration;
                        if (VibeDuration % 1 == 0 && VibeDuration >= 1 && VibeDuration <= 15) { newMaxDuration = (int)VibeDuration; }
                        else { newMaxDuration = (int)(VibeDuration * 1000); }

                        _logger.LogDebug("Sending Vibration to Shock Collar with duration: " + newMaxDuration + "(milliseconds)");
                        _ = _hub.UserShockActionOnPair(new PiShockAction(SPair.UserData, 1, VibrateIntensity, newMaxDuration));
                        PairCombos.Opened = InteractionType.None;
                    }
                }
                catch (Exception e) { _logger.LogError("Failed to push ShockCollar Vibrate message: " + e.Message); }
            }
            ImGui.Separator();
        }

        if (CkGui.IconTextButton(FAI.LandMineOn, "Beep " + PermissionData.DispName + "'s Shock Collar", WindowMenuWidth, true, !AllowBeeps))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.BeepAction ? InteractionType.None : InteractionType.BeepAction;
        }
        CkGui.AttachToolTip("Beep " + PermissionData.DispName + "'s Shock Collar.");

        if (PairCombos.Opened is InteractionType.BeepAction)
        {
            using (var actionChild = ImRaii.Child("BeepCollarActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!actionChild) return;

                var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Send Beep").X - ImGui.GetStyle().ItemInnerSpacing.X;

                // draw a slider float that references the duration, going from 0.1f to 15f by a scaler of 0.1f that displays X.Xs
                ImGui.SetNextItemWidth(width);
                ImGui.SliderFloat("##DurationSliderRef" + PermissionData.DispName, ref VibeDuration, 0.1f, ((float)maxVibeDuration.TotalMilliseconds / 1000f), "%.1fs", ImGuiSliderFlags.None);
                ImUtf8.SameLineInner();
                try
                {
                    if (ImGui.Button("Send Beep##SendBeepToShockCollar" + PermissionData.DispName))
                    {
                        int newMaxDuration;
                        if (VibeDuration % 1 == 0 && VibeDuration >= 1 && VibeDuration <= 15) { newMaxDuration = (int)VibeDuration; }
                        else { newMaxDuration = (int)(VibeDuration * 1000); }
                        _logger.LogDebug("Sending Beep to Shock Collar with duration: " + newMaxDuration + "(note that values between 1 and 15 are full seconds)");
                        _ = _hub.UserShockActionOnPair(new PiShockAction(SPair.UserData, 2, Intensity, newMaxDuration));
                        PairCombos.Opened = InteractionType.None;
                    }
                }
                catch (Exception e) { _logger.LogError("Failed to push ShockCollar Beep message: " + e.Message); }
            }
            ImGui.Separator();
        }
    }
}
