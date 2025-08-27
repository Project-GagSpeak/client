using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

// Helper methods for drawing out the hardcore actions.
public class KinksterHardcore(InteractionsService service)
{
    public void DrawHardcoreActions(float width, Kinkster k, string dispName)
    {
        ImGui.TextUnformatted("Hardcore Actions");
        var hc = k.PairHardcore;
        var enactingString = k.PairPerms.DevotionalLocks ? $"{MainHub.UID}{Constants.DevotedString}" : MainHub.UID;

        // ------ Locked Following ------
        var followEnabled = hc.LockedFollowing.Length > 0;
        var followIcon = followEnabled ? FAI.StopCircle : FAI.PersonWalkingArrowRight;
        var followText = followEnabled ? $"Have {dispName} stop following you." : $"Make {dispName} follow you.";
        var inRange = PlayerData.Available && k.VisiblePairGameObject is { } vo && PlayerData.DistanceTo(vo) < 5;
        var followDis = !inRange || !k.PairPerms.AllowLockedFollowing || !k.IsVisible || !hc.CanChange(HcAttribute.Follow, MainHub.UID);
        var followTT = followEnabled ? $"Allow {dispName} to stop following you." : $"Force {dispName} to follow you.--NL----COL--Effect expires when idle for over 6 seconds.--COL--";
        DrawColoredExpander(InteractionType.LockedFollow, followIcon, followText, followEnabled, followDis, followTT);
        UniqueHcChild(InteractionType.LockedFollow, followEnabled, ImGui.GetFrameHeight(), () =>
        {
            if (ImGuiUtil.DrawDisabledButton("Enable Locked Follow", new Vector2(width, ImGui.GetFrameHeight()), string.Empty, followDis))
                service.TryEnableHardcoreAction(HcAttribute.Follow);
            CkGui.AttachToolTip($"Force {dispName} to follow you! (--COL--{dispName} must be within 5 yalms--COL--)", ImGuiColors.ParsedPink);
        });

        // ------ Locked Emote State ------
        var emoteActive = hc.LockedEmoteState.Length > 0;
        var emoteInfo = emoteActive ? (FAI.StopCircle, $"Free {dispName}'s Locked Emote State.") : k.PairPerms.AllowLockedEmoting 
            ? (FAI.PersonArrowDownToLine, $"Force {dispName}'s Emote State.") : (FAI.Chair, $"Force {dispName} to Sit.");
        var emoteDis = (!k.PairPerms.AllowLockedSitting && !k.PairPerms.AllowLockedEmoting) || !hc.CanChange(HcAttribute.EmoteState, MainHub.UID);
        DrawColoredExpander(InteractionType.LockedEmoteState, emoteInfo.Item1, emoteInfo.Item2, emoteActive, emoteDis, emoteInfo.Item2);
        UniqueHcChild(InteractionType.LockedEmoteState, emoteActive, CkStyle.TwoRowHeight(), () => DrawEmoteChild(width, k, dispName, emoteDis));

        // ------ Locked Confinement ------
        var confinementActive = hc.IndoorConfinement.Length > 0;
        var confinementInfo = confinementActive ? (FAI.StopCircle, $"Release {dispName} from Confinement.") : (FAI.HouseLock, $"Lock {dispName} away indoors.");
        var confinementDis = !k.PairPerms.AllowIndoorConfinement || !hc.CanChange(HcAttribute.Confinement, MainHub.UID);
        var confinementTT = confinementActive ? $"Allow {dispName} to leave their indoor confinement." : $"Confinement {dispName} indoors.";
        DrawColoredExpander(InteractionType.Confinement, confinementInfo.Item1, confinementInfo.Item2, confinementActive, confinementDis, confinementTT);
        UniqueHcChild(InteractionType.Confinement, confinementActive, CkStyle.TwoRowHeight(), () =>
        {
            DrawTimerButtonRow(InteractionType.Confinement, ref service.ConfinementTimer, confinementDis);
            CkGui.CenterColorTextAligned("LifeStream Address Setup or Nearest Node.", ImGuiColors.DalamudRed);
        });

        // ------ Locked Imprisonment ------
        var imprisonmentActive = hc.Imprisonment.Length > 0;
        var imprisonmentInfo = imprisonmentActive ? (FAI.StopCircle, $"Release {dispName} from Imprisonment.") : (FAI.HouseLock, $"Imprison {dispName} indoors.");
        var imprisonmentDis = !k.PairPerms.AllowImprisonment || !hc.CanChange(HcAttribute.Imprisonment, MainHub.UID);
        var imprisonmentTT = imprisonmentActive ? $"Allow {dispName} to leave their imprisonment." : $"Imprison {dispName} indoors.";
        DrawColoredExpander(InteractionType.Imprisonment, imprisonmentInfo.Item1, imprisonmentInfo.Item2, imprisonmentActive, imprisonmentDis, imprisonmentTT);
        UniqueHcChild(InteractionType.Imprisonment, imprisonmentActive, CkStyle.TwoRowHeight(), () =>
        {
            DrawTimerButtonRow(InteractionType.Imprisonment, ref service.ImprisonTimer, imprisonmentDis);
            var rightW = CkGui.IconTextButtonSize(FAI.Upload, "Enable State");
            if (CkGui.IconButton(FAI.MapPin, disabled: !PlayerData.Available))
                service.ImprisonPos = PlayerData.Position;
            CkGui.AttachToolTip("Anchor Cage to your current position.");

            ImUtf8.SameLineInner();
            var playerIsTarget = k.VisiblePairGameObject is not null && k.VisiblePairGameObject.Equals(Svc.Targets.Target);
            var inRange = playerIsTarget && PlayerData.DistanceTo(k.VisiblePairGameObject) < 12;
            if (CkGui.IconButton(FAI.Bullseye, disabled: !inRange))
                service.ImprisonPos = k.VisiblePairGameObject!.Position;
            CkGui.AttachToolTip("Anchor Cage to the targeted Kinkster's position.");

            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - rightW - ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.SliderFloat("##FreedomRadius", ref service.ImprisonRadius, 1f, 10f);
            CkGui.AttachToolTip($"Set the radius {dispName} can move within their cage. Be careful of pathing!");

            ImUtf8.SameLineInner();
            using (CkRaii.FramedChild("CageAnchor", new Vector2(rightW, ImGui.GetFrameHeight()), 0, CkColor.VibrantPink.Uint(), CkStyle.ListItemRounding(), CkStyle.ThinThickness()))
                CkGui.CenterTextAligned($"{service.ImprisonPos:F1}");
            CkGui.AttachToolTip("The current cage anchor position.");
        });

        // ------ Chat Box Hiding ------
        var chatHideActive = hc.ChatBoxesHidden.Length > 0;
        var chatHideInfo = chatHideActive ? (FAI.StopCircle, $"Make {dispName}'s Chat Visible.") : (FAI.CommentSlash, $"Hide {dispName}'s Chat Window.");
        var chatHideDis = !k.PairPerms.AllowHidingChatBoxes || !hc.CanChange(HcAttribute.HiddenChatBox, MainHub.UID);
        var chatHideTT = chatHideActive ? $"Restore {dispName}'s chatbox visibility." : $"Conceal {dispName}'s ChatBox from their UI.";
        DrawColoredExpander(InteractionType.ChatBoxHiding, chatHideInfo.Item1, chatHideInfo.Item2, chatHideActive, chatHideDis, chatHideTT);
        GenericHcChild(InteractionType.ChatBoxHiding, ref service.ChatBoxHideTimer, chatHideActive, chatHideDis);

        // ------ Chat Input Hiding ------
        var chatIptHideActive = hc.ChatInputHidden.Length > 0;
        var chatIptHideInfo = chatIptHideActive ? (FAI.StopCircle, $"Make {dispName}'s Chat Input Visible.") : (FAI.CommentSlash, $"Hide {dispName}'s Chat Input.");
        var chatIptHideDis = !k.PairPerms.AllowHidingChatInput || !hc.CanChange(HcAttribute.HiddenChatInput, MainHub.UID);
        var chatIptHideTT = chatIptHideActive ? $"Restore {dispName}'s chat input visibility." : $"Conceal {dispName}'s chat input." +
            $"--NL----COL--NOTE:--COL-- {dispName} can still type, just can't see it~";
        DrawColoredExpander(InteractionType.ChatInputHiding, chatIptHideInfo.Item1, chatIptHideInfo.Item2, chatIptHideActive, chatIptHideDis, chatIptHideTT);
        GenericHcChild(InteractionType.ChatInputHiding, ref service.ChatInputHideTimer, chatIptHideActive, chatIptHideDis);

        // ------ Chat Input Blocking ------
        var chatIptBlockActive = hc.ChatInputBlocked.Length > 0;
        var chatIptBlockInfo = chatIptBlockActive ? (FAI.StopCircle, $"Reallow {dispName}'s Chat Input.") : (FAI.CommentDots, $"Block {dispName}'s Chat Input.");
        var chatIptBlockDis = !k.PairPerms.AllowChatInputBlocking || !hc.CanChange(HcAttribute.BlockedChatInput, MainHub.UID);
        var chatIptBlockTT = chatIptBlockActive ? $"Unblock {dispName}'s chat access." : $"Block {dispName}'s chat access." +
            $"--NL----COL--WARNING:--COL-- This prevents ANY TYPING." +
            $"--SEP----COL--CTRL+ALT+BACKSPACE--COL-- is the emergency safeword!";
        DrawColoredExpander(InteractionType.ChatInputBlocking, chatIptBlockInfo.Item1, chatIptBlockInfo.Item2, chatIptBlockActive, chatIptBlockDis, chatIptBlockTT);
        GenericHcChild(InteractionType.ChatInputBlocking, ref service.ChatInputBlockTimer, chatIptBlockActive, chatIptBlockDis);


        // >> Helpers Below 
        void DrawColoredExpander(InteractionType type, FAI icon, string text, bool showCol, bool disabled, string tooltip)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, showCol ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudWhite))
                if (CkGui.IconTextButton(icon, text, width, true, disabled))
                    service.ToggleInteraction(type);
            CkGui.AttachToolTip(tooltip, color: ImGuiColors.ParsedPink);
        }

        void UniqueHcChild(InteractionType type, bool curState, float enableChildH, Action enabledDraw)
        {
            if (service.OpenItem != type)
                return;

            using (ImRaii.Child($"{type}Child", new Vector2(width, curState ? ImGui.GetFrameHeight() : enableChildH)))
            {
                if (curState)
                    DrawDisableRow(type);
                else
                    enabledDraw();
            }
            ImGui.Separator();
        }

        // can make variant of this with custom input height and custom enabled draw action.
        void GenericHcChild(InteractionType type, ref string timerStr, bool curState, bool blockEnable)
        {
            if (service.OpenItem != type)
                return;
            
            using (ImRaii.Child($"{type}Child", new Vector2(width, ImGui.GetFrameHeight())))
            {
                if (curState)
                    DrawDisableRow(type);
                else
                    DrawTimerButtonRow(type, ref timerStr, blockEnable);
            }
            ImGui.Separator();
        }

        void DrawTimerButtonRow(InteractionType type, ref string timerStr, bool disabled)
        {
            var buttonW = CkGui.IconTextButtonSize(FAI.Upload, "Enable State");
            var txtWidth = width - buttonW - ImGui.GetStyle().ItemInnerSpacing.X;
            CkGui.IconInputText($"##Timer{type}{k.UserData.UID}", txtWidth, FAI.Clock, "Ex: 2h8m43s..", ref timerStr, 12);
            CkGui.AttachToolTip("Define a time to enable this state for (or blank to make permanent)" +
                "--NL--When the timer expires, the state is automatically disabled." +
                "--NL--You can also disable this early manually.");

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Upload, "Enable State", buttonW, disabled: disabled))
                service.TryEnableHardcoreAction(type.ToHcAttribute());
            CkGui.AttachToolTip($"Enable Hardcore State for {dispName}.");
        }

        void DrawDisableRow(InteractionType type)
        {
            if (ImGuiUtil.DrawDisabledButton($"Disable {type.ToName()}", new Vector2(width, ImGui.GetFrameHeight()), string.Empty, false))
                service.TryDisableHardcoreAction(type.ToHcAttribute());
        }
    }

    private void DrawEmoteChild(float width, Kinkster k, string dispName, bool disable)
    {
        using (ImRaii.Child("LockedEmoteChild", new Vector2(width, CkStyle.TwoRowHeight())))
        {
            // Timer & Button Row.
            var buttonW = CkGui.IconTextButtonSize(FAI.PersonRays, "Force State");
            var txtWidth = width - buttonW - ImGui.GetStyle().ItemInnerSpacing.X;
            CkGui.IconInputText($"##EmoteTimer-{k.UserData.UID}", txtWidth, FAI.Clock, "Ex: 2h8m43s..", ref service.EmoteTimer, 12);
            CkGui.AttachToolTip($"Define how long {dispName} will be locked in the selected looping emote state for." +
                "--SEP--LockedEmoteState automatically disables when the timer expires. (leave blank for permanent)" +
                "--NL--You can also disable this early manually.");

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Upload, "Force State", buttonW, disabled: disable))
                service.TryEnableHardcoreAction(HcAttribute.EmoteState);
            CkGui.AttachToolTip($"Force {dispName} to perform any {(k.PairPerms.AllowLockedEmoting ? "looped emote state" : "sitting or cycle pose state")}." +
            $"--SEP--If providing a timer, {dispName} can move once it expires.");

            // Draw the combo and slider row.
            if (EmoteService.IsAnyPoseWithCyclePose((ushort)service.Emotes.Current.RowId))
            {
                var sliderW = ImGui.GetFrameHeight() * 2;
                var comboW = width - sliderW - ImGui.GetStyle().ItemInnerSpacing.X;
                if (service.Emotes.Draw("##LockedEmoteCombo", service.EmoteId, comboW, 1.3f))
                {
                    service.EmoteId = service.Emotes.Current.RowId;
                    Svc.Logger.Information($"Changed EmoteID to {service.EmoteId} for {dispName}.");
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    service.EmoteId = service.Emotes.Items.FirstOrDefault().RowId;
                ImUtf8.SameLineInner();
                ImGui.SetNextItemWidth(sliderW);
                ImGui.SliderInt("##EnforceCyclePose", ref service.CyclePose, 0, EmoteService.CyclePoseCount((ushort)service.Emotes.Current.RowId));
                CkGui.AttachToolTip("Select the cycle pose for the forced emote.");
            }
            else
            {
                // reset cycle pose back to 0 if the emote doesn't have it.
                service.CyclePose = 0;
                if(service.Emotes.Draw("##LockedLoopEmoteCombo", service.EmoteId, width, 1.3f))
                {
                    service.EmoteId = service.Emotes.Current.RowId;
                    Svc.Logger.Information($"Changed EmoteID to {service.EmoteId} for {dispName}.");
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    service.EmoteId = service.Emotes.Items.FirstOrDefault().RowId;
            }
        }
    }
}
