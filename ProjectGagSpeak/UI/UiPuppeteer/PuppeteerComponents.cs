using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.Localization;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI;
using GagSpeak.UI.Handlers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Enums;
using ImGuiNET;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.UiPuppeteer;

public class PuppeteerComponents
{
    private readonly ILogger<PuppeteerComponents> _logger;
    private readonly AliasTable _aliasTable;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PuppeteerHandler _handler;
    private readonly UiSharedService _uiShared;
    public PuppeteerComponents(ILogger<PuppeteerComponents> logger, AliasTable aliasTable, 
        ClientConfigurationManager clientConfigs, PuppeteerHandler handler, UiSharedService uiShared)
    {
        _logger = logger;
        _aliasTable = aliasTable;
        _clientConfigs = clientConfigs;
        _handler = handler;
        _uiShared = uiShared;
    }

    private UserPairPermissions OwnPerms => _handler.SelectedPair?.OwnPerms ?? new UserPairPermissions();
    private UserEditAccessPermissions OwnEditPerms => _handler.SelectedPair?.OwnPermAccess ?? new UserEditAccessPermissions();
    private UserPairPermissions PairPerms => _handler.SelectedPair?.PairPerms ?? new UserPairPermissions();
    private UserEditAccessPermissions PairEditPerms => _handler.SelectedPair?.PairPermAccess ?? new UserEditAccessPermissions();

    public void DrawOwnAliasItem()
    {

    }

    public void DrawPairAliasItem()
    {

    }

    public void DrawAliasItemBox()
    {

    }

    public void DrawListenerClientGroup(bool isEditing, Action<bool>? onSitsChange = null, Action<bool>? onMotionChange = null, Action<bool>? onAllChange = null, Action<bool>? onEditToggle = null)
    {
        using var group = ImRaii.Group();

        ImGui.AlignTextToFramePadding();
        UiSharedService.ColorText("Listening To", ImGuiColors.ParsedPink);

        var remainingWidth = _uiShared.GetIconButtonSize(FontAwesomeIcon.Save).X * 5 - ImGui.GetStyle().ItemInnerSpacing.X * 4;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - remainingWidth);
        
        // so they let sits?
        using (ImRaii.PushColor(ImGuiCol.Text, OwnPerms.AllowSitRequests ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            if (_uiShared.IconButton(FontAwesomeIcon.Chair, inPopup: true))
                onSitsChange?.Invoke(!OwnPerms.AllowSitRequests);
        UiSharedService.AttachToolTip("Allows " + _handler.SelectedPair?.GetNickAliasOrUid() + " to make you perform /sit and /groundsit (cycle pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, OwnPerms.AllowMotionRequests ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            if (_uiShared.IconButton(FontAwesomeIcon.Walking, inPopup: true))
                onMotionChange?.Invoke(!OwnPerms.AllowMotionRequests);
        UiSharedService.AttachToolTip("Allows " + _handler.SelectedPair?.GetNickAliasOrUid() + " to make you perform emotes and expressions (cycle Pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, OwnPerms.AllowAllRequests ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            if (_uiShared.IconButton(FontAwesomeIcon.CheckDouble, inPopup: true))
                onAllChange?.Invoke(!OwnPerms.AllowAllRequests);
        UiSharedService.AttachToolTip("Allows " + _handler.SelectedPair?.GetNickAliasOrUid() + " to make you perform any command.");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, isEditing ? ImGuiColors.ParsedPink : ImGuiColors.DalamudGrey))
            if (_uiShared.IconButton(FontAwesomeIcon.Edit, inPopup: true))
                onEditToggle?.Invoke(!isEditing);
        UiSharedService.AttachToolTip(isEditing ? "Stop Editing your TriggerPhrase Info." : "Modify Your TriggerPhrase Info");
    }

    public void DrawListenerPairGroup(Action? onSendName = null)
    {
        if(_handler.SelectedPair?.LastAliasData is null)
            return;
        bool pairHasName = _handler.SelectedPair.LastAliasData.HasNameStored;
        using var group = ImRaii.Group();

        // display name, then display the downloads and likes on the other side.
        var ButtonWidth = _uiShared.GetIconButtonSize(FontAwesomeIcon.Save).X * 4 - ImGui.GetStyle().ItemInnerSpacing.X * 3;
        using (ImRaii.PushColor(ImGuiCol.Text, pairHasName ? ImGuiColors.DalamudGrey : ImGuiColors.ParsedGold))
            if (_uiShared.IconTextButton(FontAwesomeIcon.CloudUploadAlt, "Send Name", ImGui.GetContentRegionAvail().X - ButtonWidth, true, pairHasName))
                onSendName?.Invoke();
        UiSharedService.AttachToolTip("Send this Pair your In-Game Character Name, so they can listen to your players for triggers!" +
        "--SEP--This is intentionally done this way so that you are not always transferring your name with general actions.");

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ButtonWidth);
        using (ImRaii.Disabled())
        using (ImRaii.PushColor(ImGuiCol.Text, PairPerms.AllowSitRequests ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            _uiShared.IconButton(FontAwesomeIcon.Chair, inPopup: true);
        UiSharedService.AttachToolTip(_handler.SelectedPair?.GetNickAliasOrUid() + " allows you to make them perform /sit and /groundsit (cycle pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled())
        using (ImRaii.PushColor(ImGuiCol.Text, PairPerms.AllowMotionRequests ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            _uiShared.IconButton(FontAwesomeIcon.Walking, inPopup: true);
        UiSharedService.AttachToolTip(_handler.SelectedPair?.GetNickAliasOrUid() + " allows you to make them perform emotes and expressions (cycle Pose included)");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled())
        using (ImRaii.PushColor(ImGuiCol.Text, PairPerms.AllowAllRequests ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
            _uiShared.IconButton(FontAwesomeIcon.CheckDouble, inPopup: true);
        UiSharedService.AttachToolTip(_handler.SelectedPair?.GetNickAliasOrUid() + " allows you to make them perform any command.");
    }

    public void DrawEditingTriggersWindow(ref string tempTriggers, ref string tempSartChar, ref string tempEndChar)
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.AlignTextToFramePadding();
        UiSharedService.ColorText("Your Trigger Phrases", ImGuiColors.ParsedPink);

        ImGui.Spacing();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputTextWithHint("##TriggerPhrase", "Leave Blank for none...", ref tempTriggers, 64);
        UiSharedService.AttachToolTip("You can create multiple trigger phrases by placing a | between phrases.");

        using (ImRaii.Group())
        {
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            UiSharedService.ColorText("Custom Brackets:", ImGuiColors.ParsedPink);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(20 * ImGuiHelpers.GlobalScale);
            ImGui.InputText("##sStarChar", ref tempSartChar, 1);
            if (ImGui.IsItemDeactivatedAfterEdit())
                if (string.IsNullOrWhiteSpace(tempSartChar)) tempSartChar = "(";
            UiSharedService.AttachToolTip($"Custom Start Character that replaces the left enclosing bracket." +
                Environment.NewLine + "Replaces the [ ( ] in: [ TriggerPhrase (commandToExecute) ]");

            ImUtf8.SameLineInner();
            _uiShared.IconText(FontAwesomeIcon.GripLinesVertical, ImGuiColors.ParsedPink);
            ImUtf8.SameLineInner();

            ImGui.SetNextItemWidth(20 * ImGuiHelpers.GlobalScale);
            ImGui.InputText("##sEndChar", ref tempEndChar, 1);
            if (ImGui.IsItemDeactivatedAfterEdit())
                if(string.IsNullOrWhiteSpace(tempEndChar)) tempEndChar = ")";
            UiSharedService.AttachToolTip($"Custom End Character that replaces the right enclosing bracket." +
                Environment.NewLine + "Replaces the [ ) ] in Ex: [ TriggerPhrase (commandToExecute) ]");
        }
    }

    public void DrawTriggersWindow(string triggerPhrases, string startChar, string endChar)
    {
        var TriggerPhrase = triggerPhrases;
        string[] triggers = TriggerPhrase.Split('|');

        using (ImRaii.Group())
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.AlignTextToFramePadding();
            UiSharedService.ColorText("Your Trigger Phrases", ImGuiColors.ParsedPink);

            if (!triggers.Any() || triggers[0].IsNullOrEmpty())
            {
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted("No Trigger Phrase Set.");
            }

            foreach (var trigger in triggers)
            {
                if (trigger.IsNullOrEmpty())
                    continue;

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
            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(startChar);
            UiSharedService.AttachToolTip($"Custom Start Character that replaces the left enclosing bracket." +
                Environment.NewLine + "Replaces the [ ( ] in: [ TriggerPhrase (commandToExecute) ]");

            ImUtf8.SameLineInner();
            _uiShared.IconText(FontAwesomeIcon.GripLinesVertical, ImGuiColors.ParsedPink);
            ImUtf8.SameLineInner();

            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(endChar);
            UiSharedService.AttachToolTip($"Custom End Character that replaces the right enclosing bracket." +
                Environment.NewLine + "Replaces the [ ) ] in Ex: [ TriggerPhrase (commandToExecute) ]");
        }

        if(triggerPhrases.IsNullOrEmpty())
            return;

        ImGui.Spacing();
        ImGui.Separator();

        string charaName = $"<YourNameWorld> ";
        UiSharedService.ColorText("Example Usage:", ImGuiColors.ParsedPink);
        ImGui.TextWrapped(charaName + triggers[0] + " " + OwnPerms.StartChar + " glamour apply Hogtied | p | [me] " + PairPerms.EndChar);

    }


    public void DrawAliasItemBox(AliasTrigger aliasItem)
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
    }
}
