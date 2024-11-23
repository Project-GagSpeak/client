using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerData.Handlers;
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

public class PuppeteerUI : WindowMediatorSubscriberBase
{
    private readonly MainHub _apiHubMain;
    private readonly AliasTable _aliasTable;
    private readonly PuppeteerHandler _puppeteerHandler;
    private readonly UserPairListHandler _userPairListHandler;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;

    private PuppeteerTab _currentTab = PuppeteerTab.TriggerPhrases;
    private enum PuppeteerTab { TriggerPhrases, ClientAliasList, PairAliasList }

    public PuppeteerUI(ILogger<PuppeteerUI> logger, GagspeakMediator mediator,
        MainHub apiHubMain, AliasTable aliasTable, PuppeteerHandler handler,
        UserPairListHandler userPairListHandler, ClientConfigurationManager clientConfigs,
        CosmeticService cosmetics, UiSharedService uiShared) : base(logger, mediator, "Puppeteer UI")
    {
        _apiHubMain = apiHubMain;
        _clientConfigs = clientConfigs;
        _userPairListHandler = userPairListHandler;
        _puppeteerHandler = handler;
        _aliasTable = aliasTable;
        _cosmetics = cosmetics;
        _uiShared = uiShared;

        AllowPinning = false;
        AllowClickthrough = false;
        // define initial size of window and to not respect the close hotkey.
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(650, 370),
            MaximumSize = new Vector2(1000, float.MaxValue)
        };
        RespectCloseHotkey = false;
    }

    private bool isEditingTriggerOptions = false;
    private string? UnsavedTriggerPhrase = null;
    private string? UnsavedNewStartChar = null;
    private string? UnsavedNewEndChar = null;

    private bool ThemePushed = false;
    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));

            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        // _logger.LogInformation(ImGui.GetWindowSize().ToString()); <-- USE FOR DEBUGGING ONLY.
        // get information about the window region, its item spacing, and the top left side height.
        var region = ImGui.GetContentRegionAvail();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var topLeftSideHeight = region.Y;
        var cellPadding = ImGui.GetStyle().CellPadding;

        // create the draw-table for the selectable and viewport displays
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(5f * _uiShared.GetFontScalerFloat(), 0));

        using (ImRaii.Table($"PuppeteerUiWindowTable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV))
        {
            // setup the columns for the table
            ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, 200f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextColumn();

            var regionSize = ImGui.GetContentRegionAvail();
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));

            using (var leftChild = ImRaii.Child($"###PuppeteerLeft", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
            {
                var iconTexture = _cosmetics.CorePluginTextures[CorePluginTexture.Logo256];
                if (iconTexture is { } wrap)
                {
                    UtilsExtensions.ImGuiLineCentered("###PuppeteerLogo", () =>
                    {
                        ImGui.Image(wrap.ImGuiHandle, new(125f * _uiShared.GetFontScalerFloat(), 125f * _uiShared.GetFontScalerFloat()));
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"What's this? A tooltip hidden in plain sight?");
                            ImGui.EndTooltip();
                        }
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                            UnlocksEventManager.AchievementEvent(UnlocksEvent.EasterEggFound, "Puppeteer");
                    });
                }
                // add separator
                ImGui.Spacing();
                ImGui.Separator();
                float width = ImGui.GetContentRegionAvail().X;

                // show the search filter just above the contacts list to form a nice separation.
                _userPairListHandler.DrawSearchFilter(width, ImGui.GetStyle().ItemInnerSpacing.X, false);
                ImGui.Separator();
                using (ImRaii.Child($"###PuppeteerList", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoScrollbar))
                {
                    _userPairListHandler.DrawPairListSelectable(width, true, 2);
                }
            }
            // pop pushed style variables and draw next column.
            ImGui.PopStyleVar();
            ImGui.TableNextColumn();
            // display right half viewport based on the tab selection
            using (var rightChild = ImRaii.Child($"###PuppeteerRightSide", Vector2.Zero, false))
            {
                DrawPuppeteer(cellPadding);
            }
        }
    }
    

    // Main Right-half Draw function for puppeteer.
    private void DrawPuppeteer(Vector2 DefaultCellPadding)
    {
        // update the display if we switched selected Pairs.
        if (_userPairListHandler.SelectedPair is null)
        {
            _uiShared.BigText("Select a Pair to view information!");
            return;
        }

        if (_userPairListHandler.SelectedPair is not null && _puppeteerHandler.SelectedPair is null)
        {
            _puppeteerHandler.UpdateDisplayForNewPair(_userPairListHandler.SelectedPair);
            isEditingTriggerOptions = false;
            UnsavedTriggerPhrase = null;
            UnsavedNewStartChar = null;
            UnsavedNewEndChar = null;
            return;
        }

        if (_puppeteerHandler.SelectedPair is null || _userPairListHandler.SelectedPair is null)
        {
            _uiShared.BigText("No Pair Selected, select one first!");
            return;
        }

        // update display if we switched selected pairs.
        if (_puppeteerHandler.SelectedPair.UserData.UID != _userPairListHandler.SelectedPair.UserData.UID)
        {
            _puppeteerHandler.UpdateDisplayForNewPair(_userPairListHandler.SelectedPair);
            isEditingTriggerOptions = false;
            UnsavedTriggerPhrase = null;
            UnsavedNewStartChar = null;
            UnsavedNewEndChar = null;
        }

        var region = ImGui.GetContentRegionAvail();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;

        // draw title
        DrawPuppeteerHeader(DefaultCellPadding);

        ImGui.Separator();

        switch (_currentTab)
        {
            case PuppeteerTab.TriggerPhrases:
                DrawTriggerPhrases(region.X);
                break;
            case PuppeteerTab.ClientAliasList:
                _aliasTable.DrawAliasListTable(_puppeteerHandler.SelectedPair.UserData.UID, DefaultCellPadding.Y);
                break;
            case PuppeteerTab.PairAliasList:
                DrawPairAliasList(_puppeteerHandler.SelectedPair.LastAliasData);
                break;
        }
    }

    private bool AliasDataListExists => _puppeteerHandler.SelectedPair?.LastAliasData?.AliasList.Any() ?? false;
    private DateTime LastSaveTime = DateTime.MinValue;

    private void DrawPuppeteerHeader(Vector2 DefaultCellPadding)
    {
        if (_puppeteerHandler.SelectedPair is null || _puppeteerHandler.ClonedAliasStorageForEdit is null) 
            return;

        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        Vector2 textSize;
        using (_uiShared.UidFont.Push()) { textSize = ImGui.CalcTextSize("View Info"); }
        var saveSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Save).X;
        var triggerButtonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Microphone, "Triggers");
        var clientAliasListSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.EllipsisV, "Your List");
        var pairAliasListSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.EllipsisV, "Pair's List");
        var centerYpos = (textSize.Y - ImGui.GetFrameHeight());

        using (ImRaii.Child("ViewPairInformationHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(),
            _uiShared.GetIconButtonSize(FontAwesomeIcon.Voicemail).Y + (centerYpos - startYpos) * 2 - DefaultCellPadding.Y)))
        {
            // now next to it we need to draw the header text
            ImGui.SameLine(ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            using (_uiShared.UidFont.Push())
            {
                UiSharedService.ColorText($"View Info", ImGuiColors.ParsedPink);
            }


            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() 
                - saveSize - triggerButtonSize - clientAliasListSize - pairAliasListSize - ImGui.GetStyle().ItemSpacing.X * 3);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            var currentYpos = ImGui.GetCursorPosY();
            // draw revert button at the same location but right below that button
            if (_uiShared.IconButton(FontAwesomeIcon.Save, disabled: DateTime.Now - LastSaveTime < TimeSpan.FromSeconds(3)))
            {
                if(!_puppeteerHandler.IsModified)
                    return;

                _puppeteerHandler.UpdatedEditedStorage();
                // save and update our changes.
                if (UnsavedTriggerPhrase is not null)
                {
                    _logger.LogTrace($"Updated own pair permission: TriggerPhrase to {UnsavedTriggerPhrase}");
                    _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData, new KeyValuePair<string, object>("TriggerPhrase", UnsavedTriggerPhrase)));
                    UnsavedTriggerPhrase = null;
                }
                if (UnsavedNewStartChar is not null)
                {
                    _logger.LogTrace($"Updated own pair permission: StartChar to {UnsavedNewStartChar}");
                    _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData, new KeyValuePair<string, object>("StartChar", UnsavedNewStartChar[0])));
                    UnsavedNewStartChar = null;
                }
                if (UnsavedNewEndChar is not null)
                {
                    _logger.LogTrace($"Updated own pair permission: EndChar to {UnsavedNewEndChar}");
                    _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData, new KeyValuePair<string, object>("EndChar", UnsavedNewEndChar[0])));
                    UnsavedNewEndChar = null;
                }
                LastSaveTime = DateTime.Now;
            }
            UiSharedService.AttachToolTip("Press this to push your new changes to the server and save your current ones here!");

            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            if (_uiShared.IconTextButton(FontAwesomeIcon.Microphone, "Triggers", null, false, _currentTab == PuppeteerTab.TriggerPhrases))
                _currentTab = PuppeteerTab.TriggerPhrases;
            UiSharedService.AttachToolTip("View your set trigger phrase, your pairs, and use case examples!");

            // draw revert button at the same location but right below that button
            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            if (_uiShared.IconTextButton(FontAwesomeIcon.EllipsisV, "Your List", disabled: _currentTab is PuppeteerTab.ClientAliasList || !_puppeteerHandler.ClonedAliasStorageForEdit.IsValid))
                _currentTab = PuppeteerTab.ClientAliasList;
            UiSharedService.AttachToolTip("Configure your Alias List.");

            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            if (_uiShared.IconTextButton(FontAwesomeIcon.EllipsisV, "Pair's List", disabled: _currentTab == PuppeteerTab.PairAliasList))
                _currentTab = PuppeteerTab.PairAliasList;
            UiSharedService.AttachToolTip("View this Pair's Alias List.");
        }
    }

    private void DrawTriggerPhrases(float width)
    {
        if (_puppeteerHandler.SelectedPair is null) return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(5f * _uiShared.GetFontScalerFloat(), 0));
        using var table = ImRaii.Table($"TriggersDisplayForPair", 2, ImGuiTableFlags.BordersInnerV);

        if (!table) return;
        ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetContentRegionAvail().X / 2);
        ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();

        // compile a struct for displaying the example data.
        var clientTriggerData = new TriggerData(string.Empty, "Client",
            _puppeteerHandler.SelectedPair.OwnPerms.TriggerPhrase,
            _puppeteerHandler.SelectedPair.OwnPerms.StartChar,
            _puppeteerHandler.SelectedPair.OwnPerms.EndChar,
            _puppeteerHandler.SelectedPair.OwnPerms.AllowSitRequests,
            _puppeteerHandler.SelectedPair.OwnPerms.AllowMotionRequests,
            _puppeteerHandler.SelectedPair.OwnPerms.AllowAllRequests);

        DrawTriggerPhraseDetailBox(clientTriggerData);

        ImGui.TableNextColumn();

        var pairTriggerData = new TriggerData(_puppeteerHandler.SelectedPair.GetNickname() ?? _puppeteerHandler.SelectedPair.UserData.Alias ?? string.Empty,
            _puppeteerHandler.SelectedPair.UserData.UID,
            _puppeteerHandler.SelectedPair.PairPerms.TriggerPhrase,
            _puppeteerHandler.SelectedPair.PairPerms.StartChar,
            _puppeteerHandler.SelectedPair.PairPerms.EndChar,
            _puppeteerHandler.SelectedPair.PairPerms.AllowSitRequests,
            _puppeteerHandler.SelectedPair.PairPerms.AllowMotionRequests,
            _puppeteerHandler.SelectedPair.PairPerms.AllowAllRequests);

        DrawTriggerPhraseDetailBox(pairTriggerData);
    }

    private void DrawPairAliasList(CharaAliasData? pairAliasData)
    {
        if (!AliasDataListExists || MainHub.ServerStatus is not ServerState.Connected || pairAliasData == null)
        {
            _uiShared.BigText("Pair has no List for you!");
            return;
        }

        using var pairAliasListChild = ImRaii.Child("##PairAliasListChild", ImGui.GetContentRegionAvail(), false);
        if (!pairAliasListChild) return;
        // display a custom box icon for each search result obtained.
        foreach (var aliasItem in pairAliasData.AliasList)
            DrawAliasItemBox(aliasItem);
    }

    private void DrawTriggerPhraseDetailBox(TriggerData triggerInfo)
    {
        if (_puppeteerHandler.SelectedPair is null) 
            return;

        bool isClient = triggerInfo.UID == "Client";
        bool displayInRed = isClient && !_puppeteerHandler.ClonedAliasStorageForEdit!.IsValid;
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
                            _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData,
                                new KeyValuePair<string, object>("AllowSitRequests", !triggerInfo.AllowsSits)));
                        }
                    }
                }
                UiSharedService.AttachToolTip(isClient
                    ? "Allows " + _puppeteerHandler.SelectedPair.GetNickAliasOrUid() + " to make you perform /sit and /groundsit (cycle pose included)"
                    : _puppeteerHandler.SelectedPair.GetNickAliasOrUid() + " allows you to make them perform /sit and /groundsit (cycle pose included)");
                using (ImRaii.Disabled(!isClient))
                {
                    ImUtf8.SameLineInner();
                    using (ImRaii.PushColor(ImGuiCol.Text, triggerInfo.AllowsMotions ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
                    {
                        if (_uiShared.IconButton(FontAwesomeIcon.Walking, null, null, false, true))
                        {
                            _logger.LogTrace($"Updated own pair permission: AllowEmotesExpressions to {!triggerInfo.AllowsMotions}");
                            _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData,
                                new KeyValuePair<string, object>("AllowMotionRequests", !triggerInfo.AllowsMotions)));
                        }
                    }
                }
                UiSharedService.AttachToolTip(isClient
                    ? "Allows " + _puppeteerHandler.SelectedPair.GetNickAliasOrUid() + " to make you perform emotes and expressions (cycle Pose included)"
                    : _puppeteerHandler.SelectedPair.GetNickAliasOrUid() + " allows you to make them perform emotes and expressions (cycle Pose included)");
                using (ImRaii.Disabled(!isClient))
                {
                    ImUtf8.SameLineInner();
                    using (ImRaii.PushColor(ImGuiCol.Text, triggerInfo.AllowsAll ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey))
                    {
                        if (_uiShared.IconButton(FontAwesomeIcon.CheckDouble, null, null, false, true))
                        {
                            _logger.LogTrace($"Updated own pair permission: AllowAllCommands to {!triggerInfo.AllowsAll}");
                            _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData,
                                new KeyValuePair<string, object>("AllowAllRequests", !triggerInfo.AllowsAll)));
                        }
                    }
                }
                UiSharedService.AttachToolTip(isClient
                    ? "Allows " + _puppeteerHandler.SelectedPair.GetNickAliasOrUid() + " to make you perform any command."
                    : _puppeteerHandler.SelectedPair.GetNickAliasOrUid() + " allows you to make them perform any command.");

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
                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(_puppeteerHandler.ClonedAliasStorageForEdit?.NameWithWorld ?? "");
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
                    _puppeteerHandler.MarkAsModified();
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
                        _puppeteerHandler.MarkAsModified();
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
                        _puppeteerHandler.MarkAsModified();
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
                    : $"<{_puppeteerHandler.ClonedAliasStorageForEdit?.CharacterName.Split(' ').First()}" +
                      $"{_puppeteerHandler.ClonedAliasStorageForEdit?.CharacterWorld}> ";
                UiSharedService.ColorText("Example Usage:", ImGuiColors.ParsedPink);
                ImGui.TextWrapped(charaName + triggers[0] + " " +
                    _puppeteerHandler.SelectedPair?.OwnPerms.StartChar +
                   " glamour apply Hogtied | p | [me] " +
                   _puppeteerHandler.SelectedPair?.OwnPerms.EndChar);
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
    }

    private struct TriggerData
    {
        public string NickOrAlias;
        public string UID;
        public string TriggerPhrase;
        public char StartChar;
        public char EndChar;
        public bool AllowsSits;
        public bool AllowsMotions;
        public bool AllowsAll;
        public TriggerData(string nickOrAlias, string uid, string triggerPhrase, char startChar, char endChar, bool sits, bool motions, bool all)
        {
            NickOrAlias = nickOrAlias;
            UID = uid;
            TriggerPhrase = triggerPhrase;
            StartChar = startChar;
            EndChar = endChar;
            AllowsSits = sits;
            AllowsMotions = motions;
            AllowsAll = all;
        }
    }
}
