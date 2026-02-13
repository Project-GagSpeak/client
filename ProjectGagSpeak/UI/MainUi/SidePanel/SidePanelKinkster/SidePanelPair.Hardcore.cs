using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Watchers;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

// Helper methods for drawing out the hardcore actions.
public partial class SidePanelPair
{
    public void DrawHardcoreActions(KinksterInfoCache cache, Kinkster k, string dispName, float width)
    {
        ImGui.TextUnformatted("Hardcore Actions");
        var hc = k.PairHardcore;
        var enactingString = k.PairPerms.DevotionalLocks ? $"{MainHub.UID}{Constants.DevotedString}" : MainHub.UID;
        var isTarget = k.IsRendered && CharaObjectWatcher.TargetAddress.Equals(k.PlayerAddress);
        var inFollowRange = isTarget && k.DistanceToPlayer() < 5;
        var inImprisonRange = k.IsRendered && k.DistanceToPlayer() < 12;

        // ------ Locked Following ------
        var followEnabled = hc.LockedFollowing.Length > 0;
        var followIcon = followEnabled ? FAI.StopCircle : FAI.PersonWalkingArrowRight;
        var followText = followEnabled ? $"Have {dispName} stop following you." : $"Make {dispName} follow you.";
        var followAllowed = k.PairPerms.AllowLockedFollowing && k.IsRendered && hc.CanChange(HcAttribute.Follow, MainHub.UID) && inFollowRange;
        var followTT = followEnabled ? $"Allow {dispName} to stop following you." : $"Force {dispName} to follow you.--NL----COL--Effect expires when idle for over 6 seconds.--COL--";
        DrawColoredExpander(InteractionType.LockedFollow, followIcon, followText, followEnabled, !followAllowed, followTT);
        UniqueHcChild(InteractionType.LockedFollow, followEnabled, ImGui.GetFrameHeight(), () =>
        {
            if (ImGuiUtil.DrawDisabledButton("Enable Locked Follow", new Vector2(width, ImGui.GetFrameHeight()), string.Empty, !followAllowed))
                cache.TryEnableHardcoreAction(HcAttribute.Follow);
            CkGui.AttachToolTip($"Force {dispName} to follow you! (--COL--{dispName} must be within 5 yalms--COL--)", ImGuiColors.ParsedPink);
        });

        // ------ Locked Emote State ------
        var emoteActive = hc.LockedEmoteState.Length > 0;
        var emoteInfo = emoteActive ? (FAI.StopCircle, $"Free {dispName}'s Locked Emote State.") : k.PairPerms.AllowLockedEmoting
            ? (FAI.PersonArrowDownToLine, $"Force {dispName}'s Emote State.") : (FAI.Chair, $"Force {dispName} to Sit.");
        var emoteDis = (!k.PairPerms.AllowLockedSitting && !k.PairPerms.AllowLockedEmoting) || !hc.CanChange(HcAttribute.EmoteState, MainHub.UID);
        DrawColoredExpander(InteractionType.LockedEmoteState, emoteInfo.Item1, emoteInfo.Item2, emoteActive, emoteDis, emoteInfo.Item2);
        UniqueHcChild(InteractionType.LockedEmoteState, emoteActive, CkStyle.TwoRowHeight(), () => DrawEmoteChild(cache, k, dispName, width, emoteDis));

#if DEBUG
        // TODO: enable in release when confinement works
        // ------ Locked Confinement ------
        var confinementActive = hc.IndoorConfinement.Length > 0;
        var confinementInfo = confinementActive ? (FAI.StopCircle, $"Release {dispName} from Confinement.") : (FAI.HouseLock, $"Lock {dispName} away indoors.");
        var confinementAllowed = k.PairPerms.AllowIndoorConfinement && hc.CanChange(HcAttribute.Confinement, MainHub.UID);
        var confinementTT = confinementActive ? $"End {dispName}'s confinement period." : $"Confine {dispName} indoors.";
        DrawColoredExpander(InteractionType.Confinement, confinementInfo.Item1, confinementInfo.Item2, confinementActive, !confinementAllowed, confinementTT);
        UniqueHcChild(InteractionType.Confinement, confinementActive, CkStyle.GetFrameRowsHeight(4).AddWinPadY(), () =>
        {
            DrawTimerButtonRow(InteractionType.Confinement, ref cache.ConfinementTimer, "Confine", !confinementAllowed);
            DrawAddressConfig(cache, k, dispName, width);
        });
#endif

        // ------ Locked Imprisonment ------
        var imprisonmentActive = hc.Imprisonment.Length > 0;
        var imprisonmentInfo = imprisonmentActive ? (FAI.StopCircle, $"Release {dispName} from Imprisonment.") : (FAI.HouseLock, $"Imprison {dispName} indoors.");
        var imprisonmentAllowed = k.PairPerms.AllowImprisonment && hc.CanChange(HcAttribute.Imprisonment, MainHub.UID) && inImprisonRange;
        var imprisonmentTT = imprisonmentActive ? $"Allow {dispName} to leave their imprisonment." : $"Imprison {dispName} indoors.";
        DrawColoredExpander(InteractionType.Imprisonment, imprisonmentInfo.Item1, imprisonmentInfo.Item2, imprisonmentActive, !imprisonmentAllowed, imprisonmentTT);
        UniqueHcChild(InteractionType.Imprisonment, imprisonmentActive, CkStyle.TwoRowHeight(), () =>
        {
            DrawTimerButtonRow(InteractionType.Imprisonment, ref cache.ImprisonTimer, "Imprison", !imprisonmentAllowed || !inImprisonRange);
            var rightW = CkGui.IconTextButtonSize(FAI.Upload, "Enable State");
            if (CkGui.IconButton(FAI.MapPin, disabled: !PlayerData.Available))
                cache.ImprisonPos = PlayerData.Position;
            CkGui.AttachToolTip("Anchor Cage to your current position.");

            ImUtf8.SameLineInner();
            if (CkGui.IconButton(FAI.Bullseye, disabled: !isTarget))
                cache.ImprisonPos = k.PlayerPosition;
            CkGui.AttachToolTip("Anchor Cage to the targeted Kinkster's position.");

            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - rightW - ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.SliderFloat("##FreedomRadius", ref cache.ImprisonRadius, 1f, 10f);
            CkGui.AttachToolTip($"Set the radius {dispName} can move within their cage. Be careful of pathing!");

            ImUtf8.SameLineInner();
            var clientInAnchorRange = PlayerData.DistanceTo(cache.ImprisonPos) <= cache.ImprisonRadius;
            var frameCol = clientInAnchorRange ? CkCol.TriStateCheck.Vec4().ToUint() : CkCol.TriStateCross.Vec4().ToUint();
            using (CkRaii.FramedChild("CageAnchor", new Vector2(rightW, ImGui.GetFrameHeight()), 0, frameCol, CkStyle.ListItemRounding(), CkStyle.ThinThickness()))
                CkGui.CenterTextAligned($"{cache.ImprisonPos:F1}");
            CkGui.AttachToolTip("The current cage anchor position." +
                "--SEP----COL--Note:--COL--0,0,0 Anchor uses the Kinkster's position.", ImGuiColors.ParsedPink);
        });

        // ------ Chat Box Hiding ------
        var chatHideActive = hc.ChatBoxesHidden.Length > 0;
        var chatHideInfo = chatHideActive ? (FAI.StopCircle, $"Make {dispName}'s Chat Visible.") : (FAI.CommentSlash, $"Hide {dispName}'s Chat Window.");
        var chatHideDis = !k.PairPerms.AllowHidingChatBoxes || !hc.CanChange(HcAttribute.HiddenChatBox, MainHub.UID);
        var chatHideTT = chatHideActive ? $"Restore {dispName}'s chatbox visibility." : $"Conceal {dispName}'s ChatBox from their UI.";
        DrawColoredExpander(InteractionType.ChatBoxHiding, chatHideInfo.Item1, chatHideInfo.Item2, chatHideActive, chatHideDis, chatHideTT);
        GenericHcChild(InteractionType.ChatBoxHiding, ref cache.ChatBoxHideTimer, "Hide Chat", chatHideActive, chatHideDis);

        // ------ Chat Input Hiding ------
        var chatIptHideActive = hc.ChatInputHidden.Length > 0;
        var chatIptHideInfo = chatIptHideActive ? (FAI.StopCircle, $"Make {dispName}'s Chat Input Visible.") : (FAI.CommentSlash, $"Hide {dispName}'s Chat Input.");
        var chatIptHideDis = !k.PairPerms.AllowHidingChatInput || !hc.CanChange(HcAttribute.HiddenChatInput, MainHub.UID);
        var chatIptHideTT = chatIptHideActive ? $"Restore {dispName}'s chat input visibility." : $"Conceal {dispName}'s chat input." +
            $"--NL----COL--NOTE:--COL-- {dispName} can still type, just can't see it~";
        DrawColoredExpander(InteractionType.ChatInputHiding, chatIptHideInfo.Item1, chatIptHideInfo.Item2, chatIptHideActive, chatIptHideDis, chatIptHideTT);
        GenericHcChild(InteractionType.ChatInputHiding, ref cache.ChatInputHideTimer, "Hide Input", chatIptHideActive, chatIptHideDis);

        // ------ Chat Input Blocking ------
        var chatIptBlockActive = hc.ChatInputBlocked.Length > 0;
        var chatIptBlockInfo = chatIptBlockActive ? (FAI.StopCircle, $"Reallow {dispName}'s Chat Input.") : (FAI.CommentDots, $"Block {dispName}'s Chat Input.");
        var chatIptBlockDis = !k.PairPerms.AllowChatInputBlocking || !hc.CanChange(HcAttribute.BlockedChatInput, MainHub.UID);
        var chatIptBlockTT = chatIptBlockActive ? $"Unblock {dispName}'s chat access." : $"Block {dispName}'s chat access." +
            $"--NL----COL--WARNING:--COL-- This prevents ANY TYPING." +
            $"--SEP----COL--CTRL+ALT+BACKSPACE--COL-- is the emergency safeword!";
        DrawColoredExpander(InteractionType.ChatInputBlocking, chatIptBlockInfo.Item1, chatIptBlockInfo.Item2, chatIptBlockActive, chatIptBlockDis, chatIptBlockTT);
        GenericHcChild(InteractionType.ChatInputBlocking, ref cache.ChatInputBlockTimer, "Block Input", chatIptBlockActive, chatIptBlockDis);


        // >> Helpers Below 
        void DrawColoredExpander(InteractionType type, FAI icon, string text, bool showCol, bool disabled, string tooltip)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, showCol ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudWhite))
                if (CkGui.IconTextButton(icon, text, width, true, disabled))
                    cache.ToggleInteraction(type);
            CkGui.AttachToolTip(tooltip, color: ImGuiColors.ParsedPink);
        }

        void UniqueHcChild(InteractionType type, bool curState, float enableChildH, Action enabledDraw)
        {
            if (cache.OpenItem != type)
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
        void GenericHcChild(InteractionType type, ref string timerStr, string enableText, bool curState, bool blockEnable)
        {
            if (cache.OpenItem != type)
                return;

            using (ImRaii.Child($"{type}Child", new Vector2(width, ImGui.GetFrameHeight())))
            {
                if (curState)
                    DrawDisableRow(type);
                else
                    DrawTimerButtonRow(type, ref timerStr, enableText, blockEnable);
            }
            ImGui.Separator();
        }

        void DrawTimerButtonRow(InteractionType type, ref string timerStr, string enableText, bool disabled)
        {
            var buttonW = CkGui.IconTextButtonSize(FAI.Upload, enableText);
            var txtWidth = width - buttonW - ImGui.GetStyle().ItemInnerSpacing.X;
            CkGui.IconInputText($"##Timer{type}{k.UserData.UID}", txtWidth, FAI.Clock, "Ex: 2h8m43s..", ref timerStr, 12);
            CkGui.AttachToolTip("Define a time to enable this state for (or blank to make permanent)" +
                "--NL--When the timer expires, the state is automatically disabled." +
                "--NL--You can also disable this early manually.");

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Upload, enableText, buttonW, disabled: disabled))
                cache.TryEnableHardcoreAction(type.ToHcAttribute());
            CkGui.AttachToolTip($"Enable Hardcore State for {dispName}.");
        }

        void DrawDisableRow(InteractionType type)
        {
            if (ImGuiUtil.DrawDisabledButton($"Disable {type.ToName()}", new Vector2(width, ImGui.GetFrameHeight()), string.Empty, false))
                cache.TryDisableHardcoreAction(type.ToHcAttribute());
        }
    }

    private void DrawEmoteChild(KinksterInfoCache cache, Kinkster k, string dispName, float width, bool disable)
    {
        using (ImRaii.Child("LockedEmoteChild", new Vector2(width, CkStyle.TwoRowHeight())))
        {
            // Timer & Button Row.
            var buttonW = CkGui.IconTextButtonSize(FAI.PersonRays, "Force State");
            var txtWidth = width - buttonW - ImGui.GetStyle().ItemInnerSpacing.X;
            CkGui.IconInputText($"##EmoteTimer-{k.UserData.UID}", txtWidth, FAI.Clock, "Ex: 2h8m43s..", ref cache.EmoteTimer, 12);
            CkGui.AttachToolTip($"Define how long {dispName} will be locked in the selected looping emote state for." +
                "--SEP--LockedEmoteState automatically disables when the timer expires. (leave blank for permanent)" +
                "--NL--You can also disable this early manually.");

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Upload, "Force State", buttonW, disabled: disable))
                cache.TryEnableHardcoreAction(HcAttribute.EmoteState);
            CkGui.AttachToolTip($"Force {dispName} to perform any {(k.PairPerms.AllowLockedEmoting ? "looped emote state" : "sitting or cycle pose state")}." +
            $"--SEP--If providing a timer, {dispName} can move once it expires.");

            // Draw the combo and slider row.
            if (EmoteService.IsAnyPoseWithCyclePose((ushort)cache.Emotes.Current.RowId))
            {
                var sliderW = ImGui.GetFrameHeight() * 2;
                var comboW = width - sliderW - ImGui.GetStyle().ItemInnerSpacing.X;
                if (cache.Emotes.Draw("##LockedEmoteCombo", cache.EmoteId, comboW, 1.3f))
                {
                    cache.EmoteId = cache.Emotes.Current.RowId;
                    Svc.Logger.Information($"Changed EmoteID to {cache.EmoteId} for {dispName}.");
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    cache.EmoteId = cache.Emotes.Items.FirstOrDefault().RowId;
                ImUtf8.SameLineInner();
                ImGui.SetNextItemWidth(sliderW);
                ImGui.SliderInt("##EnforceCyclePose", ref cache.CyclePose, 0, EmoteService.CyclePoseCount((ushort)cache.Emotes.Current.RowId));
                CkGui.AttachToolTip("Select the cycle pose for the forced emote.");
            }
            else
            {
                // reset cycle pose back to 0 if the emote doesn't have it.
                cache.CyclePose = 0;
                if (cache.Emotes.Draw("##LockedLoopEmoteCombo", cache.EmoteId, width, 1.3f))
                {
                    cache.EmoteId = cache.Emotes.Current.RowId;
                    Svc.Logger.Information($"Changed EmoteID to {cache.EmoteId} for {dispName}.");
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    cache.EmoteId = cache.Emotes.Items.FirstOrDefault().RowId;
            }
        }
    }

    private void DrawAddressConfig(KinksterInfoCache cache, Kinkster k, string dispName, float width)
    {
        using var c = CkRaii.FramedChildPaddedWH("##AddressConfig", new(width, CkStyle.GetFrameRowsHeight(3).AddWinPadY()), 0, GsCol.VibrantPink.Uint());

        CkGui.FramedIconText(FAI.Home);
        ImUtf8.SameLineInner();
        var widthMinusFrame = ImGui.GetContentRegionAvail().X;
        var triItemWidth = (widthMinusFrame - ImGui.GetStyle().ItemInnerSpacing.X * 2) / 3f;

        // over the next line, have 3 buttons for the various property type states.
        using (ImRaii.Disabled(cache.Address.PropertyType is PropertyType.House))
            if (ImGui.Button("House", new Vector2(triItemWidth, ImGui.GetFrameHeight())))
            {
                cache.Address.PropertyType = PropertyType.House;
                cache.Address.ApartmentSubdivision = false;
            }
        CkGui.AttachToolTip($"Confining {dispName} in a house.");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled(cache.Address.PropertyType is PropertyType.Apartment))
            if (ImGui.Button("Apartment", new Vector2(triItemWidth, ImGui.GetFrameHeight())))
                cache.Address.PropertyType = PropertyType.Apartment;
        CkGui.AttachToolTip($"Confining {dispName} to an apartment room.");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled(cache.Address.PropertyType is PropertyType.PrivateChambers))
            if (ImGui.Button("Chambers", new Vector2(triItemWidth, ImGui.GetFrameHeight())))
            {
                cache.Address.PropertyType = PropertyType.PrivateChambers;
                cache.Address.ApartmentSubdivision = false;
            }
        CkGui.AttachToolTip($"Confining {dispName} the private chambers of an FC.");

        CkGui.FramedIconText(FAI.MapMarkedAlt);
        ImUtf8.SameLineInner();

        if (cache.Worlds.Draw(cache.Address.World, triItemWidth, CFlags.NoArrowButton))
            cache.Address.World = cache.Worlds.Current.Key.Id;
        CkGui.AttachToolTip($"The World {dispName} will be confined to.");

        ImUtf8.SameLineInner();
        CkGuiUtils.ResidentialAetheryteCombo($"##resdis", triItemWidth, ref cache.Address.City);
        CkGui.AttachToolTip($"The District {dispName} will be confined to.");

        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.DragInt($"##ward", ref cache.Address.Ward, .5f, 1, 30, "Ward %d");
        CkGui.AttachToolTip($"The Ward {dispName} will be confined to.");

        var propType = cache.Address.PropertyType;
        using (ImRaii.Disabled(propType is not PropertyType.Apartment))
            ImGui.Checkbox("##SubdivisionCheck", ref cache.Address.ApartmentSubdivision);
        CkGui.AttachToolTip("If the apartment is in the ward's subdivision.");

        // draw out the sliders based on the property type.
        ImUtf8.SameLineInner();
        var sliderW = propType is not PropertyType.PrivateChambers
            ? ImGui.GetContentRegionAvail().X : (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X) / 2;
        switch (propType)
        {
            case PropertyType.House:
                ImGui.SetNextItemWidth(sliderW);
                ImGui.SliderInt("##plot", ref cache.Address.Plot, 1, 60, "Plot %d");
                CkGui.AttachToolTip($"The plot # of the home {dispName} will be confined to.");
                break;
            case PropertyType.Apartment:
                ImGui.SetNextItemWidth(sliderW);
                ImGui.SliderInt("##room", ref cache.Address.Apartment, 1, 100, "Room %d");
                CkGui.AttachToolTip($"The apartment room # {dispName} will be confined to.");
                break;
            case PropertyType.PrivateChambers:
                ImGui.SetNextItemWidth(sliderW);
                ImGui.SliderInt("##plot", ref cache.Address.Plot, 1, 60, "Plot %d");
                CkGui.AttachToolTip($"The plot # of the home {dispName} will be confined to.");

                ImUtf8.SameLineInner();
                ImGui.SetNextItemWidth(sliderW);
                ImGui.SliderInt("##chambers", ref cache.Address.Apartment, 1, 100, "Chamber %d");
                CkGui.AttachToolTip($"The private chambers # {dispName} will be confined to.");
                break;
            default:
                CkGui.ColorTextCentered("UNKNOWN PLOT TYPE", ImGuiColors.DalamudRed);
                break;
        }
    }
}
