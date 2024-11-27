using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Handlers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Enums;
using ImGuiNET;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.UiPuppeteer;

public class PuppeteerComponents
{
    private readonly ILogger<PuppeteerComponents> _logger;
    private readonly MainHub _apiHubMain;
    private readonly AliasTable _aliasTable;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly UiSharedService _uiShared;
    public PuppeteerComponents(ILogger<PuppeteerComponents> logger, MainHub mainHub,
        AliasTable aliasTable, ClientConfigurationManager clientConfigs,
        PuppeteerHandler handler, UiSharedService uiShared)
    {
        _logger = logger;
        _apiHubMain = mainHub;
        _aliasTable = aliasTable;
        _clientConfigs = clientConfigs;
        _uiShared = uiShared;
    }

    /*public void DrawTriggerInfoBoxClient(UserData clientUserData, Action? onEditToggle = null, 

    public void DrawTriggerInfoBox(UserData userData, string nickAliasUID, string listenerName, string triggerPhrase, char startChar, char endChar, bool allowSits, 
        bool allowMotions, bool allowAll, FontAwesomeIcon saveIcon = FontAwesomeIcon.Save, Action? onEditToggle = null)
    {
        bool isClient = nickAliasUID.IsNullOrEmpty();
        // push rounding window corners
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        // push a pink border color for the window border.
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        // push a less transparent very dark grey background color.
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        // create the child window.
        using var child = ImRaii.Child("##TriggerDataFor" + listenerName, new Vector2(ImGui.GetContentRegionAvail().X, 0), true, ImGuiWindowFlags.ChildWindow);

        DrawListenerGroup();

        if (isClient)
        {
            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(_clientConfigs.AliasConfig.AliasStorage[selectedPair.UserData.UID].NameWithWorld ?? "");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.AlignTextToFramePadding();
            UiSharedService.ColorText("Your Trigger Phrases", ImGuiColors.ParsedPink);
        }

        // Handle the case where data is matched.
        var TriggerPhrase = isClient ? (UnsavedTriggerPhrase ?? triggerInfo.TriggerPhrase) : triggerInfo.TriggerPhrase;
        string[] triggers = TriggerPhrase.Split('|');

        ImGui.Spacing();
        if (isEditingTriggerOptions && isClient)
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.InputTextWithHint($"##{displayName}-Trigger", "Leave Blank for none...", ref TriggerPhrase, 64))
                UnsavedTriggerPhrase = TriggerPhrase;
            if (ImGui.IsItemDeactivatedAfterEdit())
                _handler.MarkAsModified();
            UiSharedService.AttachToolTip("You can create multiple trigger phrases by placing a | between phrases.");
        }
        else
        {
            if (!triggers.Any() || triggers[0].IsNullOrEmpty())
            {
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted("No Trigger Phrase Set.");
            }

            foreach (var trigger in triggers)
            {
                if (trigger.IsNullOrEmpty()) continue;

                _uiShared.IconText(FontAwesomeIcon.QuoteLeft, ImGuiColors.ParsedPink);
                ImUtf8.SameLineInner();
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(trigger);
                ImUtf8.SameLineInner();
                _uiShared.IconText(FontAwesomeIcon.QuoteRight, ImGuiColors.ParsedPink);
            }
        }

        using (ImRaii.Group())
        {
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            UiSharedService.ColorText("Custom Brackets:", ImGuiColors.ParsedPink);
            ImGui.SameLine();
            var startChar = isClient ? (UnsavedNewStartChar ?? triggerInfo.StartChar.ToString()) : triggerInfo.StartChar.ToString();
            var endChar = isClient ? (UnsavedNewEndChar ?? triggerInfo.EndChar.ToString()) : triggerInfo.EndChar.ToString();
            if (isEditingTriggerOptions && isClient)
            {
                ImGui.SetNextItemWidth(20 * ImGuiHelpers.GlobalScale);
                if (ImGui.InputText($"##{displayName}sStarChar", ref startChar, 1))
                    UnsavedNewStartChar = startChar;
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    if (string.IsNullOrWhiteSpace(endChar))
                        UnsavedNewEndChar = "(";
                    _handler.MarkAsModified();
                }
            }
            else
            {
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(startChar.ToString());
            }
            UiSharedService.AttachToolTip($"Custom Start Character that replaces the left enclosing bracket." +
                Environment.NewLine + "Replaces the [ ( ] in: [ TriggerPhrase (commandToExecute) ]");

            ImUtf8.SameLineInner();
            _uiShared.IconText(FontAwesomeIcon.GripLinesVertical, ImGuiColors.ParsedPink);
            ImUtf8.SameLineInner();
            if (isEditingTriggerOptions && isClient)
            {
                ImGui.SetNextItemWidth(20 * ImGuiHelpers.GlobalScale);
                if (ImGui.InputText($"##{displayName}sEndChar", ref endChar, 1))
                    UnsavedNewEndChar = endChar;
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    if (string.IsNullOrWhiteSpace(endChar))
                        UnsavedNewEndChar = ")";
                    _handler.MarkAsModified();
                }
            }
            else
            {
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(endChar.ToString());
            }
            UiSharedService.AttachToolTip($"Custom End Character that replaces the right enclosing bracket." +
                Environment.NewLine + "Replaces the [ ) ] in Ex: [ TriggerPhrase (commandToExecute) ]");
        }

        // if no trigger phrase set, return.
        if (TriggerPhrase.IsNullOrEmpty()) return;

        ImGui.Spacing();
        ImGui.Separator();

        if (!displayInRed)
        {
            string charaName = !isClient
                ? $"<YourNameWorld> "
                : $"<{_handler.ClonedAliasStorageForEdit?.CharacterName.Split(' ').First()}" +
                  $"{_handler.ClonedAliasStorageForEdit?.CharacterWorld}> ";
            UiSharedService.ColorText("Example Usage:", ImGuiColors.ParsedPink);
            ImGui.TextWrapped(charaName + triggers[0] + " " +
                selectedPair?.OwnPerms.StartChar +
               " glamour apply Hogtied | p | [me] " +
               selectedPair?.OwnPerms.EndChar);
        }
    }

    public void DrawOwnAliasItem()
    {

    }

    public void DrawPairAliasItem()
    {

    }

    public void DrawAliasItemBox()
    {

    }

    public void DrawListenerGroupClient(
    {
        using (var group = ImRaii.Group())
        {
            // display name, then display the downloads and likes on the other side.
            ImGui.AlignTextToFramePadding();
            UiSharedService.ColorText(isClient ? "Listening To" : "Pair's Trigger Phrases", ImGuiColors.ParsedPink);
            UiSharedService.AttachToolTip(isClient
                ? "The In Game Character that can use your trigger phrases below on you"
                : "The phrases you can say to this Kinkster that will execute their triggers.");

            var remainingWidth = iconSize.X * (isClient ? 5 : 4) - ImGui.GetStyle().ItemInnerSpacing.X * (isClient ? 4 : 3);
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - remainingWidth);
            using (ImRaii.Disabled(!isClient))
            {
                using (ImRaii.PushColor(ImGuiCol.Text, allowSits ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
                {
                    if (_uiShared.IconButton(FontAwesomeIcon.Chair, inPopup: true))
                    {
                        _logger.LogTrace($"Updated own pair permission: AllowSitCommands to {!allowSits}");
                        _ = _apiHubMain.UserUpdateOwnPairPerm(new(selectedPair.UserData,
                            new KeyValuePair<string, object>("AllowSitRequests", !allowSits)));
                    }
                }
            }
            UiSharedService.AttachToolTip(isClient
                ? "Allows " + selectedPair.GetNickAliasOrUid() + " to make you perform /sit and /groundsit (cycle pose included)"
                : selectedPair.GetNickAliasOrUid() + " allows you to make them perform /sit and /groundsit (cycle pose included)");
            using (ImRaii.Disabled(!isClient))
            {
                ImUtf8.SameLineInner();
                using (ImRaii.PushColor(ImGuiCol.Text, allowMotions ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
                {
                    if (_uiShared.IconButton(FontAwesomeIcon.Walking, null, null, false, true))
                    {
                        _logger.LogTrace($"Updated own pair permission: AllowEmotesExpressions to {!allowMotions}");
                        _ = _apiHubMain.UserUpdateOwnPairPerm(new(selectedPair.UserData,
                            new KeyValuePair<string, object>("AllowMotionRequests", !allowMotions)));
                    }
                }
            }
            UiSharedService.AttachToolTip(isClient
                ? "Allows " + selectedPair.GetNickAliasOrUid() + " to make you perform emotes and expressions (cycle Pose included)"
                : selectedPair.GetNickAliasOrUid() + " allows you to make them perform emotes and expressions (cycle Pose included)");
            using (ImRaii.Disabled(!isClient))
            {
                ImUtf8.SameLineInner();
                using (ImRaii.PushColor(ImGuiCol.Text, allowAll ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
                {
                    if (_uiShared.IconButton(FontAwesomeIcon.CheckDouble, null, null, false, true))
                    {
                        _logger.LogTrace($"Updated own pair permission: AllowAllCommands to {!allowAll}");
                        _ = _apiHubMain.UserUpdateOwnPairPerm(new(selectedPair.UserData,
                            new KeyValuePair<string, object>("AllowAllRequests", !allowAll)));
                    }
                }
            }
            UiSharedService.AttachToolTip(isClient
                ? "Allows " + selectedPair.GetNickAliasOrUid() + " to make you perform any command."
                : selectedPair.GetNickAliasOrUid() + " allows you to make them perform any command.");

            if (isClient)
            {
                ImUtf8.SameLineInner();
                using (var color = ImRaii.PushColor(ImGuiCol.Text, (saveIcon == FontAwesomeIcon.Save) ? ImGuiColors.ParsedPink : ImGuiColors.DalamudGrey))
                {
                    if (_uiShared.IconButton(FontAwesomeIcon.Edit, inPopup: true))
                        onEditToggle.Invoke();
                }
                UiSharedService.AttachToolTip((saveIcon == FontAwesomeIcon.Save) ? "Stop Editing your TriggerPhrase Info." : "Modify Your TriggerPhrase Info");
            }
        }
    }

    private void DrawTriggerPhraseDetailBox(TriggerData triggerInfo)
    {
        if (selectedPair is null) 
            return;

        bool isClient = triggerInfo.UID == "Client";
        bool displayInRed = isClient && !_handler.ClonedAliasStorageForEdit!.IsValid;
        var iconSize = isEditingTriggerOptions ? _uiShared.GetIconButtonSize(FontAwesomeIcon.Save) : _uiShared.GetIconButtonSize(FontAwesomeIcon.Edit);
        string displayName = triggerInfo.NickOrAlias.IsNullOrEmpty() ? triggerInfo.UID : triggerInfo.NickOrAlias;


        // push rounding window corners
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        // push a pink border color for the window border.
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, displayInRed ? ImGuiColors.DPSRed : ImGuiColors.ParsedPink);
        // push a less transparent very dark grey background color.
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        // create the child window.
        using (var patternResChild = ImRaii.Child("##TriggerDataFor" + triggerInfo.UID, ImGui.GetContentRegionAvail(), true, ImGuiWindowFlags.ChildWindow))
        {
            if (!patternResChild) return;

            // Handle Case where data is not yet matched.
            if (displayInRed && isClient)
            {
                using (ImRaii.Group())
                {
                    UiSharedService.ColorTextCentered("Not Listening To Pair's Character.", ImGuiColors.DalamudRed);
                    ImGui.Spacing();
                    UiSharedService.ColorTextCentered("This pair must press the action:", ImGuiColors.DalamudRed);
                    ImGui.Spacing();

                    ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth()) / 2
                        - (_uiShared.GetIconTextButtonSize(FontAwesomeIcon.Sync, "Update [UID] with your Name") - 5 * ImGuiHelpers.GlobalScale) / 2);

                    using (ImRaii.Disabled(true))
                    {
                        _uiShared.IconTextButton(FontAwesomeIcon.Sync, "Update [UID] with your Name", null, false);
                    }
                    ImGui.Spacing();
                    UiSharedService.ColorTextCentered("(If you wanna be controlled by them)", ImGuiColors.DalamudRed);
                    return;
                }
            }

            using (var group = ImRaii.Group())
            {
                // display name, then display the downloads and likes on the other side.
                ImGui.AlignTextToFramePadding();
                UiSharedService.ColorText(isClient ? "Listening To" : "Pair's Trigger Phrases", ImGuiColors.ParsedPink);
                UiSharedService.AttachToolTip(isClient
                    ? "The In Game Character that can use your trigger phrases below on you"
                    : "The phrases you can say to this Kinkster that will execute their triggers.");

                var remainingWidth = iconSize.X * (isClient ? 5 : 4) - ImGui.GetStyle().ItemInnerSpacing.X * (isClient ? 4 : 3);
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - remainingWidth);
                using (ImRaii.Disabled(!isClient))
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, triggerInfo.AllowsSits ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
                    {
                        if (_uiShared.IconButton(FontAwesomeIcon.Chair, inPopup: true))
                        {
                            _logger.LogTrace($"Updated own pair permission: AllowSitCommands to {!triggerInfo.AllowsSits}");
                            _ = _apiHubMain.UserUpdateOwnPairPerm(new(selectedPair.UserData,
                                new KeyValuePair<string, object>("AllowSitRequests", !triggerInfo.AllowsSits)));
                        }
                    }
                }
                UiSharedService.AttachToolTip(isClient
                    ? "Allows " + selectedPair.GetNickAliasOrUid() + " to make you perform /sit and /groundsit (cycle pose included)"
                    : selectedPair.GetNickAliasOrUid() + " allows you to make them perform /sit and /groundsit (cycle pose included)");
                using (ImRaii.Disabled(!isClient))
                {
                    ImUtf8.SameLineInner();
                    using (ImRaii.PushColor(ImGuiCol.Text, allowMotions ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
                    {
                        if (_uiShared.IconButton(FontAwesomeIcon.Walking, null, null, false, true))
                        {
                            _logger.LogTrace($"Updated own pair permission: AllowEmotesExpressions to {!allowMotions}");
                            _ = _apiHubMain.UserUpdateOwnPairPerm(new(selectedPair.UserData,
                                new KeyValuePair<string, object>("AllowMotionRequests", !allowMotions)));
                        }
                    }
                }
                UiSharedService.AttachToolTip(isClient
                    ? "Allows " + selectedPair.GetNickAliasOrUid() + " to make you perform emotes and expressions (cycle Pose included)"
                    : selectedPair.GetNickAliasOrUid() + " allows you to make them perform emotes and expressions (cycle Pose included)");
                using (ImRaii.Disabled(!isClient))
                {
                    ImUtf8.SameLineInner();
                    using (ImRaii.PushColor(ImGuiCol.Text, allowAll ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
                    {
                        if (_uiShared.IconButton(FontAwesomeIcon.CheckDouble, null, null, false, true))
                        {
                            _logger.LogTrace($"Updated own pair permission: AllowAllCommands to {!allowAll}");
                            _ = _apiHubMain.UserUpdateOwnPairPerm(new(selectedPair.UserData,
                                new KeyValuePair<string, object>("AllowAllRequests", !allowAll)));
                        }
                    }
                }
                UiSharedService.AttachToolTip(isClient
                    ? "Allows " + selectedPair.GetNickAliasOrUid() + " to make you perform any command."
                    : selectedPair.GetNickAliasOrUid() + " allows you to make them perform any command.");

                if (isClient)
                {
                    ImUtf8.SameLineInner();
                    using (var color = ImRaii.PushColor(ImGuiCol.Text, isEditingTriggerOptions ? ImGuiColors.ParsedPink : ImGuiColors.DalamudGrey))
                    {
                        if (_uiShared.IconButton(FontAwesomeIcon.Edit, inPopup: true))
                            isEditingTriggerOptions = !isEditingTriggerOptions;
                    }
                    UiSharedService.AttachToolTip(isEditingTriggerOptions ? "Stop Editing your TriggerPhrase Info." : "Modify Your TriggerPhrase Info");
                }
            }

            if(isClient)
            {
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(_handler.ClonedAliasStorageForEdit?.NameWithWorld ?? "");
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.AlignTextToFramePadding();
                UiSharedService.ColorText("Your Trigger Phrases", ImGuiColors.ParsedPink);
            }

            // Handle the case where data is matched.
            var TriggerPhrase = isClient ? (UnsavedTriggerPhrase ?? triggerInfo.TriggerPhrase) : triggerInfo.TriggerPhrase;
            string[] triggers = TriggerPhrase.Split('|');

            ImGui.Spacing();
            if (isEditingTriggerOptions && isClient)
            {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputTextWithHint($"##{displayName}-Trigger", "Leave Blank for none...", ref TriggerPhrase, 64))
                    UnsavedTriggerPhrase = TriggerPhrase;
                if (ImGui.IsItemDeactivatedAfterEdit())
                    _handler.MarkAsModified();
                UiSharedService.AttachToolTip("You can create multiple trigger phrases by placing a | between phrases.");
            }
            else
            {
                if (!triggers.Any() || triggers[0].IsNullOrEmpty())
                {
                    using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted("No Trigger Phrase Set.");
                }

                foreach (var trigger in triggers)
                {
                    if (trigger.IsNullOrEmpty()) continue;

                    _uiShared.IconText(FontAwesomeIcon.QuoteLeft, ImGuiColors.ParsedPink);
                    ImUtf8.SameLineInner();
                    using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(trigger);
                    ImUtf8.SameLineInner();
                    _uiShared.IconText(FontAwesomeIcon.QuoteRight, ImGuiColors.ParsedPink);
                }
            }

            using (ImRaii.Group())
            {
                ImGui.Spacing();
                ImGui.AlignTextToFramePadding();
                UiSharedService.ColorText("Custom Brackets:", ImGuiColors.ParsedPink);
                ImGui.SameLine();
                var startChar = isClient ? (UnsavedNewStartChar ?? triggerInfo.StartChar.ToString()) : triggerInfo.StartChar.ToString();
                var endChar = isClient ? (UnsavedNewEndChar ?? triggerInfo.EndChar.ToString()) : triggerInfo.EndChar.ToString();
                if (isEditingTriggerOptions && isClient)
                {
                    ImGui.SetNextItemWidth(20 * ImGuiHelpers.GlobalScale);
                    if (ImGui.InputText($"##{displayName}sStarChar", ref startChar, 1))
                        UnsavedNewStartChar = startChar;
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        if (string.IsNullOrWhiteSpace(endChar))
                            UnsavedNewEndChar = "(";
                        _handler.MarkAsModified();
                    }
                }
                else
                {
                    using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(startChar.ToString());
                }
                UiSharedService.AttachToolTip($"Custom Start Character that replaces the left enclosing bracket." +
                    Environment.NewLine + "Replaces the [ ( ] in: [ TriggerPhrase (commandToExecute) ]");

                ImUtf8.SameLineInner();
                _uiShared.IconText(FontAwesomeIcon.GripLinesVertical, ImGuiColors.ParsedPink);
                ImUtf8.SameLineInner();
                if (isEditingTriggerOptions && isClient)
                {
                    ImGui.SetNextItemWidth(20 * ImGuiHelpers.GlobalScale);
                    if (ImGui.InputText($"##{displayName}sEndChar", ref endChar, 1))
                        UnsavedNewEndChar = endChar;
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        if (string.IsNullOrWhiteSpace(endChar))
                            UnsavedNewEndChar = ")";
                        _handler.MarkAsModified();
                    }
                }
                else
                {
                    using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(endChar.ToString());
                }
                UiSharedService.AttachToolTip($"Custom End Character that replaces the right enclosing bracket." +
                    Environment.NewLine + "Replaces the [ ) ] in Ex: [ TriggerPhrase (commandToExecute) ]");
            }

            // if no trigger phrase set, return.
            if (TriggerPhrase.IsNullOrEmpty()) return;

            ImGui.Spacing();
            ImGui.Separator();

            if (!displayInRed)
            {
                string charaName = !isClient
                    ? $"<YourNameWorld> "
                    : $"<{_handler.ClonedAliasStorageForEdit?.CharacterName.Split(' ').First()}" +
                      $"{_handler.ClonedAliasStorageForEdit?.CharacterWorld}> ";
                UiSharedService.ColorText("Example Usage:", ImGuiColors.ParsedPink);
                ImGui.TextWrapped(charaName + triggers[0] + " " +
                    selectedPair?.OwnPerms.StartChar +
                   " glamour apply Hogtied | p | [me] " +
                   selectedPair?.OwnPerms.EndChar);
            }
        }
    }


    private void DrawAliasItemBox(AliasTrigger aliasItem)
    {
        // push rounding window corners
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        // push a pink border color for the window border.
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        // push a less transparent very dark grey background color.
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        // create the child window.

        float height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2;
        using (var patternResChild = ImRaii.Child("##PatternResult_" + aliasItem.InputCommand + aliasItem.OutputCommand, new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow))
        {
            if (!patternResChild) return;

            using (ImRaii.Group())
            {

                _uiShared.BooleanToColoredIcon(aliasItem.Enabled, false);
                ImUtf8.SameLineInner();
                _uiShared.IconText(FontAwesomeIcon.QuoteLeft, ImGuiColors.ParsedPink);
                ImUtf8.SameLineInner();
                UiSharedService.ColorText(aliasItem.InputCommand, ImGuiColors.ParsedPink);
                UiSharedService.AttachToolTip("The string of words that will trigger the output command.");
                ImUtf8.SameLineInner();
                _uiShared.IconText(FontAwesomeIcon.QuoteRight, ImGuiColors.ParsedPink);
                ImGui.Separator();

                _uiShared.IconText(FontAwesomeIcon.LongArrowAltRight, ImGuiColors.ParsedPink);
                ImUtf8.SameLineInner();
                UiSharedService.TextWrapped(aliasItem.OutputCommand);
                UiSharedService.AttachToolTip("The command that will be executed when the input phrase is said by the pair.");
            }
        }
    }*/
}
