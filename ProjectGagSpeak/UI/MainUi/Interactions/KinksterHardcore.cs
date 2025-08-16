using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

// Helper methods for drawing out the hardcore actions.
public class KinksterHardcore
{
    private readonly ILogger<KinksterHardcore> _logger;
    private readonly MainHub _hub;
    private readonly InteractionsService _service;

    public KinksterHardcore(ILogger<KinksterHardcore> logger, MainHub hub, InteractionsService service)
    {
        _logger = logger;
        _hub = hub;
        _service = service;
    }

    public void DrawHardcoreActions(float width, Kinkster k, string dispName)
    {
        ImGui.TextUnformatted("Hardcore Actions");
        var hc = k.PairHardcore;

        var inRange = PlayerData.Available && k.VisiblePairGameObject is { } vo && PlayerData.DistanceTo(vo) < 3;
        var pairlockTag = k.PairPerms.PairLockedStates ? Constants.DevotedString : string.Empty;

        (FAI Icon, string Text) hcLabel = hc.IsEnabled(HcAttribute.Follow)
            ? (FAI.StopCircle, $"Have {dispName} stop following you.") 
            : (FAI.PersonWalkingArrowRight, $"Make {dispName} follow you.");
        if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !inRange || !k.PairPerms.AllowLockedFollowing || !k.IsVisible || !hc.CanChange(HcAttribute.Follow, MainHub.UID), "##HcLockedFollowing"))
        {
            var newStr = hc.IsEnabled(HcAttribute.Follow) ? string.Empty : $"{MainHub.UID}{pairlockTag}";
            _logger.LogWarning("Method not yet implemented for Locked Following!");
            //UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, hc, nameof(GlobalPerms.LockedFollowing), newStr));
        }

        // ForceEmote is a special child...
        DrawLockedEmoteSection(k, width, dispName, pairlockTag);


        //hcLabel = hc.HcConfinedState() ? (FAI.StopCircle, $"Release {dispName}.") : (FAI.HouseLock, $"Lock {dispName} away.");
        //if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !k.PairPerms.AllowIndoorConfinement || !hc.CanChangeHcConfined(MainHub.UID), "##HcForcedStay"))
        //{
        //    var newStr = hc.HcConfinedState() ? string.Empty : $"{MainHub.UID}{pairlockTag}";
        //    UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, kg, nameof(GlobalPerms.IndoorConfinement), newStr));
        //}

        //// Hiding chat message history window, but still allowing typing.
        //hcLabel = hc.HcChatVisState() ? (FAI.StopCircle, $"Make {dispName}'s Chat Visible.") : (FAI.CommentSlash, $"Hide {dispName}'s Chat Window.");
        //if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !k.PairPerms.AllowHidingChatBoxes || !hc.CanChangeHcChatVis(MainHub.UID), "##HcForcedChatVis"))
        //{
        //    var newStr = hc.HcChatVisState() ? string.Empty : $"{MainHub.UID}{pairlockTag}";
        //    UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, kg, nameof(GlobalPerms.ChatBoxesHidden), newStr));
        //}

        //// Hiding Chat input, but still allowing typing.
        //hcLabel = hc.HcChatInputVisState() ? (FAI.StopCircle, $"Make {dispName}'s Chat Input Visible.") : (FAI.CommentSlash, $"Hide {dispName}'s Chat Input.");
        //if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !k.PairPerms.AllowHidingChatInput || !hc.CanChangeHcChatInputVis(MainHub.UID), "##HcForcedChatInputVis"))
        //{
        //    var newStr = hc.HcChatInputVisState() ? string.Empty : $"{MainHub.UID}{pairlockTag}";
        //    UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, kg, nameof(GlobalPerms.ChatInputHidden), newStr));
        //}

        //// Preventing Chat Input at all.
        //hcLabel = hc.HcBlockChatInputState() ? (FAI.StopCircle, $"Reallow {dispName}'s Chat Input.") : (FAI.CommentDots, $"Block {dispName}'s Chat Input.");
        //if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !k.PairPerms.AllowChatInputBlocking || !hc.CanChangeHcBlockChatInput(MainHub.UID), "##HcForcedChatBlocking"))
        //{
        //    var newStr = hc.HcBlockChatInputState() ? string.Empty : $"{MainHub.UID}{pairlockTag}";
        //    UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, kg, nameof(GlobalPerms.ChatInputBlocked), newStr));
        //}

        //ImGui.Separator();
    }

    // USE THESE FOR CHANGES, HARDCORE ACTIONS CANNOT BE GENERALIZED, THEY ARE ALL UNIQUE.
    private void LockedFollowInternal(float width, Kinkster k, string dispName)
    {

    }

    private void IndoorConfinementInternal(float width, Kinkster k, string dispName)
    {

    }

    private void ImprisonmentInternal(float width, Kinkster k, string dispName)
    {

    }

    private void ChatboxHidingInternal(float width, Kinkster k, string dispName)
    {

    }

    private void ChatInputHidingInternal(float width, Kinkster k, string dispName)
    {

    }

    private void ChatInputBlockingInternal(float width, Kinkster k, string dispName)
    {

    }

    private void DrawLockedEmoteSection(Kinkster k, float width, string dispName, string pairlockTag)
    {
        // What to display if we cant do LockedEmote
        if (k.PairHardcore.IsEnabled(HcAttribute.EmoteState))
        {
            if (CkGui.IconTextButton(FAI.StopCircle, $"Let {dispName} move again.", width, true, id: "##HcForcedStay"))
            {
                _logger.LogWarning("NEED TO REIMPLEMENT THIS STILL");
                //UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, k.PairGlobals, nameof(H.LockedEmoteState), string.Empty));
                _service.CloseInteraction();
            }
            CkGui.AttachToolTip($"Release {dispName} from forced emote state.");
            return;
        }

        // If we can do LockedEmote, display options.
        (FAI Icon, string Text) hcLabel = k.PairPerms.AllowLockedEmoting ? (FAI.PersonArrowDownToLine, $"Force {dispName} into an Emote State.") : (FAI.Chair, $"Force {dispName} to Sit.");
        if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !k.PairPerms.AllowLockedSitting && k.PairHardcore.CanChange(HcAttribute.EmoteState, MainHub.UID), "##HcLockedEmote"))
            _service.ToggleInteraction(InteractionType.LockedEmoteState);
        CkGui.AttachToolTip($"Force {dispName} to perform any {(k.PairPerms.AllowLockedEmoting ? "looped emote state" : "sitting or cycle pose state")}.");

        if (_service.OpenItem is not InteractionType.LockedEmoteState)
            return;

        using (ImRaii.Child("LockedEmoteState", new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y)))
        {
            var comboWidth = width - CkGui.IconTextButtonSize(FAI.PersonRays, "Force State") - ImGui.GetStyle().ItemInnerSpacing.X;
            // Handle Emote Stuff.
            var emoteList = k.PairPerms.AllowLockedEmoting ? EmoteExtensions.LoopedEmotes() : EmoteExtensions.SittingEmotes();
            _service.Emotes.Draw("##LockedEmoteCombo", _service.EmoteId, comboWidth, 1.3f);
            // Handle Cycle Poses
            var canSetCyclePose = EmoteService.IsAnyPoseWithCyclePose((ushort)_service.Emotes.Current.RowId);
            var maxCycles = canSetCyclePose ? EmoteService.CyclePoseCount((ushort)_service.Emotes.Current.RowId) : 0;
            if (!canSetCyclePose) _service.CyclePose = 0;
            using (ImRaii.Disabled(!canSetCyclePose))
            {
                ImGui.SetNextItemWidth(comboWidth);
                ImGui.SliderInt("##EnforceCyclePose", ref _service.CyclePose, 0, maxCycles);
                CkGui.AttachToolTip("Select the cycle pose for the forced emote.");
            }
            // the application button thing.
            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.PersonRays, "Force State"))
            {
                var newStr = $"{MainHub.UID}|{_service.Emotes.Current.RowId}|{_service.CyclePose}{pairlockTag}";
                _logger.LogDebug($"Sending EmoteState update for emote: {_service.Emotes.Current.Name}");
                // change how this is handled, should be done through hardcore, and via different operations. Handling it via a single operation is not enough!
                _logger.LogWarning("NEED TO REIMPLEMENT THIS STILL");
                // UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, k.PairGlobals, nameof(HardcoreState.LockedEmoteState), newStr));
                _service.CloseInteraction();
            }
            CkGui.AttachToolTip("Apply the selected forced emote state.");
        }
        ImGui.Separator();
    }
}
