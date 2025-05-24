using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
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
    private uint _chosenEmoteId = 0;
    private int _chosenCyclePose = 0;
    private void DrawHardcoreActions()
    {
        if(_globals.GlobalPerms is null || MainHub.UID is null)
        {
            _logger.LogWarning("GlobalPerms or MainHub.UID is null, cannot draw hardcore actions.");
            return;
        }

        // Required Close-Ranged Hardcore commands must be in range
        var inRange = _monitor.IsPresent && SPair.VisiblePairGameObject is { } validObj && Vector3.Distance(_monitor.ClientPlayer!.Position, validObj.Position) < 3;

        var pairlockStateStr = SPair.PairPerms.PairLockedStates ? Constants.DevotedString : string.Empty;

        var forceFollowIcon = SPair.PairGlobals.IsFollowing() ? FAI.StopCircle : FAI.PersonWalkingArrowRight;
        var forceFollowText = SPair.PairGlobals.IsFollowing() ? $"Have {PermissionData.DispName} stop following you." : $"Make {PermissionData.DispName} follow you.";
        var disableForceFollow = !inRange || !SPair.PairPerms.AllowForcedFollow || !SPair.IsVisible || !SPair.PairGlobals.CanToggleFollow(MainHub.UID);
        if (CkGui.IconTextButton(forceFollowIcon, forceFollowText, WindowMenuWidth, true, disableForceFollow))
        {
            var newStr = SPair.PairGlobals.IsFollowing() ? string.Empty : MainHub.UID + pairlockStateStr;
            _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(nameof(UserGlobalPermissions.ForcedFollow), newStr), UpdateDir.Other));
        }
        
        DrawForcedEmoteSection();


        var forceToStayIcon = SPair.PairGlobals.IsStaying() ? FAI.StopCircle : FAI.HouseLock;
        var forceToStayText = SPair.PairGlobals.IsStaying() ? $"Release {PermissionData.DispName}." : $"Lock away {PermissionData.DispName}.";
        var disableForceToStay = !SPair.PairPerms.AllowForcedStay || !SPair.PairGlobals.CanToggleStay(MainHub.UID);
        if (CkGui.IconTextButton(forceToStayIcon, forceToStayText, WindowMenuWidth, true, disableForceToStay, "##ForcedToStayHCA"))
        {
            var newStr = SPair.PairGlobals.IsStaying() ? string.Empty : MainHub.UID + pairlockStateStr;
            _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(nameof(UserGlobalPermissions.ForcedStay), newStr), UpdateDir.Other));
        }

        // Hiding chat message history window, but still allowing typing.
        var toggleChatboxIcon = SPair.PairGlobals.IsChatHidden() ? FAI.StopCircle : FAI.CommentSlash;
        var toggleChatboxText = SPair.PairGlobals.IsChatHidden() ? "Make " + PermissionData.DispName + "'s Chat Visible." : "Hide "+PermissionData.DispName+"'s Chat Window.";
        var disableChatToggle = !SPair.PairPerms.AllowHidingChatBoxes || !SPair.PairGlobals.CanToggleChatHidden(MainHub.UID);
        if (CkGui.IconTextButton(toggleChatboxIcon, toggleChatboxText, WindowMenuWidth, true, disableChatToggle, "##ForcedChatboxVisibilityHCA"))
        {
            var newStr = SPair.PairGlobals.IsChatHidden() ? string.Empty : MainHub.UID + pairlockStateStr;
            _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(nameof(UserGlobalPermissions.ChatBoxesHidden), newStr), UpdateDir.Other));
        }

        // Hiding Chat input, but still allowing typing.
        var toggleChatInputIcon = SPair.PairGlobals.IsChatInputHidden() ? FAI.StopCircle : FAI.CommentSlash;
        var toggleChatInputText = SPair.PairGlobals.IsChatInputHidden() ? "Make " + PermissionData.DispName + "'s Chat Input Visible." : "Hide "+PermissionData.DispName+"'s Chat Input.";
        var disableChatInputRenderToggle = !SPair.PairPerms.AllowHidingChatInput || !SPair.PairGlobals.CanToggleChatInputHidden(MainHub.UID);
        if (CkGui.IconTextButton(toggleChatInputIcon, toggleChatInputText, WindowMenuWidth, true, disableChatInputRenderToggle, "##ForcedChatInputVisibilityHCA"))
        {
            var newStr = SPair.PairGlobals.IsChatInputHidden() ? string.Empty : MainHub.UID + pairlockStateStr;
            _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(nameof(UserGlobalPermissions.ChatInputHidden), newStr), UpdateDir.Other));
        }

        // Preventing Chat Input at all.
        var toggleChatBlockingIcon = SPair.PairGlobals.IsChatInputBlocked() ? FAI.StopCircle : FAI.CommentDots;
        var toggleChatBlockingText = SPair.PairGlobals.IsChatInputBlocked() ? "Reallow "+PermissionData.DispName+"'s Chat Input." : "Block "+PermissionData.DispName+"'s Chat Input.";
        var disableChatInputBlockToggle = !SPair.PairPerms.AllowChatInputBlocking || !SPair.PairGlobals.CanToggleChatInputBlocked(MainHub.UID);
        if (CkGui.IconTextButton(toggleChatBlockingIcon, toggleChatBlockingText, WindowMenuWidth, true, disableChatInputBlockToggle, "##BlockedChatInputHCA"))
        {
            var newStr = SPair.PairGlobals.IsChatInputBlocked() ? string.Empty : MainHub.UID + pairlockStateStr;
            _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(nameof(UserGlobalPermissions.ChatInputBlocked), newStr), UpdateDir.Other));
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
                _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(nameof(UserGlobalPermissions.ForcedEmoteState), string.Empty), UpdateDir.Other));
        }
        else
        {
            var forceEmoteIcon = SPair.PairPerms.AllowForcedEmote ? FAI.PersonArrowDownToLine : FAI.Chair;
            var forceEmoteText = SPair.PairPerms.AllowForcedEmote ? $"Force {PermissionData.DispName} into an Emote State." : $"Force {PermissionData.DispName} to Sit.";
            if (CkGui.IconTextButton(forceEmoteIcon, forceEmoteText, WindowMenuWidth, true, disableForceSit && disableForceEmoteState, "##ForcedEmoteAction"))
                OpenOrClose(InteractionType.ForcedEmoteState);
            CkGui.AttachToolTip($"Force {PermissionData.DispName} to Perform any {(SPair.PairPerms.AllowForcedEmote ? "Looped Emote State." : "Sitting or Cycle Pose States.")}");

            if (OpenedInteraction is InteractionType.ForcedEmoteState)
            {
                using (ImRaii.Child("ForcedEmoteStateActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), false))
                {
                    var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Force State").X - ImGui.GetStyle().ItemInnerSpacing.X;
                    
                    // Have User select the emote they want.
                    var listToShow = disableForceEmoteState ? EmoteExtensions.SittingEmotes() : EmoteExtensions.LoopedEmotes();
                    _emoteCombo.Draw("##EmoteComboPairPerm", _chosenEmoteId, WindowMenuWidth, 1.3f);
                    // Only allow setting the CPose State if the emote is a sitting one.
                    using (ImRaii.Disabled(!EmoteService.IsAnyPoseWithCyclePose((ushort)_emoteCombo.Current.RowId)))
                    {
                        // Get the Max CyclePoses for this emote.
                        var maxCycles = EmoteService.CyclePoseCount((ushort)_emoteCombo.Current.RowId);
                        
                        // Ensure CyclePose count is valid.
                        if (maxCycles is 0)
                            _chosenCyclePose = 0;

                        // Draw out the slider for the enforced cycle pose.
                        ImGui.SetNextItemWidth(width);
                        ImGui.SliderInt("##EnforceCyclePose", ref _chosenCyclePose, 0, maxCycles);
                    }

                    ImUtf8.SameLineInner();
                    if (ImGui.Button("Force State##ForceEmoteStateTo" + PermissionData.DispName))
                    {
                        // Compile the string for sending.
                        var newStr = MainHub.UID + "|" + _emoteCombo.Current.RowId.ToString() + "|" + _chosenCyclePose.ToString() + (SPair.PairPerms.PairLockedStates ? Constants.DevotedString : string.Empty);
                        _logger.LogDebug("Sending EmoteState update for emote: " + _emoteCombo.Current.Name);
                        _ = _hub.UserUpdateOtherGlobalPerm(new(SPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(nameof(UserGlobalPermissions.ForcedEmoteState), newStr), UpdateDir.Other));
                        CloseInteraction();
                    }
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
        bool usePairOverGlobal = SPair.PairPerms.HasValidShareCode();
        var MaxIntensity = usePairOverGlobal ? SPair.PairPerms.MaxIntensity : SPair.PairGlobals.MaxIntensity;
        var maxVibeDuration = usePairOverGlobal ? SPair.PairPerms.GetTimespanFromDuration() : SPair.PairGlobals.GetTimespanFromDuration();
        var piShockShareCodePref = usePairOverGlobal ? SPair.PairPerms.PiShockShareCode : SPair.PairGlobals.GlobalShockShareCode;

        // Shock Expander
        var AllowShocks = usePairOverGlobal ? SPair.PairPerms.AllowShocks : SPair.PairGlobals.AllowShocks;
        if (CkGui.IconTextButton(FAI.BoltLightning, "Shock " + PermissionData.DispName + "'s Shock Collar", WindowMenuWidth, true, !AllowShocks))
            OpenOrClose(InteractionType.ShockAction);
        CkGui.AttachToolTip("Perform a Shock action to " + PermissionData.DispName + "'s Shock Collar.");

        if (OpenedInteraction is InteractionType.ShockAction)
        {
            using (ImRaii.Child("ShockCollarActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), false))
            {
                var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Send Shock").X - ImGui.GetStyle().ItemInnerSpacing.X;

                ImGui.SetNextItemWidth(WindowMenuWidth);
                ImGui.SliderInt("##IntensitySliderRef" + PermissionData.DispName, ref Intensity, 0, MaxIntensity, "%d%%", ImGuiSliderFlags.None);
                
                ImGui.SetNextItemWidth(width);
                ImGui.SliderFloat("##DurationSliderRef" + PermissionData.DispName, ref Duration, 0.0f, (float)maxVibeDuration.TotalMilliseconds / 1000f, "%.1fs", ImGuiSliderFlags.None);
                
                ImUtf8.SameLineInner();
                if (ImGui.Button("Send Shock##SendShockToShockCollar" + PermissionData.DispName))
                {
                    int newMaxDuration;
                    if (Duration % 1 == 0 && Duration >= 1 && Duration <= 15) { newMaxDuration = (int)Duration; }
                    else { newMaxDuration = (int)(Duration * 1000); }

                    _logger.LogDebug("Sending Shock to Shock Collar with duration: " + newMaxDuration + "(milliseconds)");
                    _ = _hub.UserShockActionOnPair(new ShockCollarAction(SPair.UserData, 0, Intensity, newMaxDuration));
                    UnlocksEventManager.AchievementEvent(UnlocksEvent.ShockSent);
                    CloseInteraction();
                }
            }
            ImGui.Separator();
        }


        // Vibrate Expander
        var AllowVibrations = usePairOverGlobal ? SPair.PairPerms.AllowVibrations : SPair.PairGlobals.AllowVibrations;
        if (CkGui.IconTextButton(FAI.WaveSquare, "Vibrate " + PermissionData.DispName + "'s Shock Collar", WindowMenuWidth, true, false))
            OpenOrClose(InteractionType.VibrateAction);
        CkGui.AttachToolTip("Perform a Vibrate action to " + PermissionData.DispName + "'s Shock Collar.");

        if (OpenedInteraction is InteractionType.VibrateAction)
        {
            using (ImRaii.Child("VibrateCollarActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), false))
            {
                var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Send Vibration").X - ImGui.GetStyle().ItemInnerSpacing.X;

                // draw a slider float that references the duration, going from 0.1f to 15f by a scaler of 0.1f that displays X.Xs
                ImGui.SetNextItemWidth(WindowMenuWidth);
                ImGui.SliderInt("##IntensitySliderRef" + PermissionData.DispName, ref VibrateIntensity, 0, 100, "%d%%", ImGuiSliderFlags.None);
                
                ImGui.SetNextItemWidth(width);
                ImGui.SliderFloat("##DurationSliderRef" + PermissionData.DispName, ref VibeDuration, 0.0f, ((float)maxVibeDuration.TotalMilliseconds / 1000f), "%.1fs", ImGuiSliderFlags.None);
                
                ImUtf8.SameLineInner();
                if (ImGui.Button("Send Vibration##SendVibrationToShockCollar" + PermissionData.DispName))
                {
                    int newMaxDuration = (VibeDuration % 1 == 0 && VibeDuration >= 1 && VibeDuration <= 15)
                        ? (int)VibeDuration : (int)(VibeDuration * 1000);

                    _logger.LogDebug("Sending Vibration to Shock Collar with duration: " + newMaxDuration + "(milliseconds)");
                    _ = _hub.UserShockActionOnPair(new ShockCollarAction(SPair.UserData, 1, VibrateIntensity, newMaxDuration));
                    CloseInteraction();
                }
            }
            ImGui.Separator();
        }


        // Beep Expander
        var AllowBeeps = usePairOverGlobal ? SPair.PairPerms.AllowBeeps : SPair.PairGlobals.AllowBeeps;
        if (CkGui.IconTextButton(FAI.LandMineOn, "Beep " + PermissionData.DispName + "'s Shock Collar", WindowMenuWidth, true, !AllowBeeps))
            OpenOrClose(InteractionType.BeepAction);
        CkGui.AttachToolTip("Beep " + PermissionData.DispName + "'s Shock Collar.");

        if (OpenedInteraction is InteractionType.BeepAction)
        {
            using (ImRaii.Child("BeepCollarActionChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                var width = WindowMenuWidth - ImGuiHelpers.GetButtonSize("Send Beep").X - ImGui.GetStyle().ItemInnerSpacing.X;

                // draw a slider float that references the duration, going from 0.1f to 15f by a scaler of 0.1f that displays X.Xs
                var max = ((float)maxVibeDuration.TotalMilliseconds / 1000f);
                ImGui.SetNextItemWidth(width);
                ImGui.SliderFloat("##DurationSliderRef" + PermissionData.DispName, ref VibeDuration, 0.1f, max, "%.1fs", ImGuiSliderFlags.None);
                
                ImUtf8.SameLineInner();
                if (ImGui.Button("Send Beep##SendBeepToShockCollar" + PermissionData.DispName))
                {
                    int newMaxDuration = (VibeDuration % 1 == 0 && VibeDuration >= 1 && VibeDuration <= 15)
                        ? (int)VibeDuration
                        : (int)(VibeDuration * 1000);

                    _logger.LogDebug("Sending Beep to Shock Collar with duration: " + newMaxDuration + "(note that values between 1 and 15 are full seconds)");
                    _ = _hub.UserShockActionOnPair(new ShockCollarAction(SPair.UserData, 2, Intensity, newMaxDuration));
                    CloseInteraction();
                }
            }
            ImGui.Separator();
        }
    }
}
