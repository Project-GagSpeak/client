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
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.IPC;
using ImGuiNET;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.UiPuppeteer;

public class PuppeteerUI : WindowMediatorSubscriberBase
{
    private readonly MainHub _apiHubMain;
    private readonly AliasTable _aliasTable;
    private readonly PuppeteerComponents _components;
    private readonly PuppeteerHandler _puppeteerHandler;
    private readonly UserPairListHandler _userPairListHandler;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ClientMonitorService _clientService;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;

    private PuppeteerTab _currentTab = PuppeteerTab.TriggerPhrases;
    private enum PuppeteerTab { TriggerPhrases, ClientAliasList, PairAliasList }

    public PuppeteerUI(ILogger<PuppeteerUI> logger, GagspeakMediator mediator,
        MainHub apiHubMain, AliasTable aliasTable, PuppeteerComponents components,
        PuppeteerHandler handler, UserPairListHandler userPairListHandler, 
        ClientConfigurationManager clientConfigs, ClientMonitorService clientService,
        CosmeticService cosmetics, UiSharedService uiShared) : base(logger, mediator, "Puppeteer UI")
    {
        _apiHubMain = apiHubMain;
        _aliasTable = aliasTable;
        _components = components;
        _puppeteerHandler = handler;
        _userPairListHandler = userPairListHandler;
        _clientConfigs = clientConfigs;
        _clientService = clientService;
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
    private string UnsavedTriggerPhrase = string.Empty;
    private string UnsavedNewStartChar = string.Empty;
    private string UnsavedNewEndChar = string.Empty;

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
        using var table = ImRaii.Table($"PuppeteerUiWindowTable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV);
        // setup the columns for the table
        ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, 200f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();
        var regionSize = ImGui.GetContentRegionAvail();
        using (ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f)))
        {
            using (ImRaii.Child($"###PuppeteerLeft", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
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
        }

        ImGui.TableNextColumn();
        using (ImRaii.Child($"###PuppeteerRightSide", Vector2.Zero, false)) DrawPuppeteer(cellPadding);
    }


    // Main Right-half Draw function for puppeteer.
    private void DrawPuppeteer(Vector2 DefaultCellPadding)
    {
        // update the display if we switched selected Pairs.
        if (_puppeteerHandler.SelectedPair is null)
        {
            _uiShared.BigText("Select a Pair to view information!");
            return;
        }

        DrawPuppeteerHeader(DefaultCellPadding);

        ImGui.Separator();

        switch (_currentTab)
        {
            case PuppeteerTab.TriggerPhrases:
                DrawTriggerPhrases();
                break;
            case PuppeteerTab.ClientAliasList:
                ImGui.Text("Alias List Under Construction");
                //_aliasTable.DrawAliasListTable(_puppeteerHandler.SelectedPair.UserData.UID, DefaultCellPadding.Y);
                break;
            case PuppeteerTab.PairAliasList:
                ImGui.Text("Alias List Under Construction");
                break;
        }
    }

    private bool AliasDataListExists => _puppeteerHandler.SelectedPair?.LastAliasData?.AliasList.Any() ?? false;
    private DateTime LastSaveTime = DateTime.MinValue;

    private void DrawPuppeteerHeader(Vector2 DefaultCellPadding)
    {
        if (_puppeteerHandler.SelectedPair is null)
            return;

        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        Vector2 textSize;
        using (_uiShared.UidFont.Push()) { textSize = ImGui.CalcTextSize("View Info"); }
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


            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - triggerButtonSize - clientAliasListSize - pairAliasListSize - ImGui.GetStyle().ItemSpacing.X * 3);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            var currentYpos = ImGui.GetCursorPosY();
            if (_uiShared.IconTextButton(FontAwesomeIcon.Microphone, "Triggers", null, false, _currentTab == PuppeteerTab.TriggerPhrases))
                _currentTab = PuppeteerTab.TriggerPhrases;
            UiSharedService.AttachToolTip("View your set trigger phrase, your pairs, and use case examples!");

            // draw revert button at the same location but right below that button
            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            if (_uiShared.IconTextButton(FontAwesomeIcon.EllipsisV, "Your List", disabled: _currentTab is PuppeteerTab.ClientAliasList))
                _currentTab = PuppeteerTab.ClientAliasList;
            UiSharedService.AttachToolTip("Configure your Alias List.");

            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            if (_uiShared.IconTextButton(FontAwesomeIcon.EllipsisV, "Pair's List", disabled: _currentTab == PuppeteerTab.PairAliasList))
                _currentTab = PuppeteerTab.PairAliasList;
            UiSharedService.AttachToolTip("View this Pair's Alias List.");
        }
    }

    private void DrawTriggerPhrases()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(5f * _uiShared.GetFontScalerFloat(), 0));
        using var table = ImRaii.Table($"TriggersDisplayForPair", 2, ImGuiTableFlags.BordersInnerV);

        if (!table) return;
        ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetContentRegionAvail().X / 2);
        ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();

        DrawClientTriggersBox();
        ImGui.TableNextColumn();
        DrawPairTriggersBox();
    }

    private void DrawClientTriggersBox()
    {
        if (_puppeteerHandler.SelectedPair is null)
            return;

        // push rounding window corners
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        // push a pink border color for the window border.
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        // push a less transparent very dark grey background color.
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        // create the child window.
        using (ImRaii.Child("##TriggerDataForClient", new Vector2(ImGui.GetContentRegionAvail().X, 0), true, ImGuiWindowFlags.ChildWindow))
        {
            // Draw the client change actions.
            _components.DrawListenerClientGroup(isEditingTriggerOptions,
                (newSits) =>
                {
                    _logger.LogTrace($"Updated AlowSits permission: " + newSits);
                    _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("AllowSitRequests", newSits), UpdateDir.Own));
                },
                (newMotions) =>
                {
                    _logger.LogTrace($"Updated AlowMotions permission: " + newMotions);
                    _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("AllowMotionRequests", newMotions), UpdateDir.Own));
                },
                (newAll) =>
                {
                    _logger.LogTrace($"Updated AlowAll permission: " + newAll);
                    _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("AllowAllRequests", newAll), UpdateDir.Own));
                },
                (newEditState) =>
                {
                    // set the new state, then based on its new state, do things.
                    isEditingTriggerOptions = newEditState;
                    if (newEditState is false)
                    {
                        // save and update our changes.
                        if (!UnsavedTriggerPhrase.IsNullOrEmpty())
                        {
                            _logger.LogTrace($"Updated own pair permission: TriggerPhrase to {UnsavedTriggerPhrase}");
                            _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("TriggerPhrase", UnsavedTriggerPhrase), UpdateDir.Own));
                            UnsavedTriggerPhrase = string.Empty;
                        }
                        if (!UnsavedNewStartChar.IsNullOrEmpty())
                        {
                            _logger.LogTrace($"Updated own pair permission: StartChar to {UnsavedNewStartChar}");
                            _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("StartChar", UnsavedNewStartChar[0]), UpdateDir.Own));
                            UnsavedNewStartChar = string.Empty;
                        }
                        if (!UnsavedNewEndChar.IsNullOrEmpty())
                        {
                            _logger.LogTrace($"Updated own pair permission: EndChar to {UnsavedNewEndChar}");
                            _ = _apiHubMain.UserUpdateOwnPairPerm(new(_puppeteerHandler.SelectedPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("EndChar", UnsavedNewStartChar[0]), UpdateDir.Own));
                            UnsavedNewEndChar = string.Empty;
                        }
                        LastSaveTime = DateTime.Now;
                    }
                    else
                    {
                        UnsavedTriggerPhrase = _puppeteerHandler.SelectedPair.OwnPerms.TriggerPhrase;
                        UnsavedNewStartChar = _puppeteerHandler.SelectedPair.OwnPerms.StartChar.ToString();
                        UnsavedNewEndChar = _puppeteerHandler.SelectedPair.OwnPerms.EndChar.ToString();
                    }
                });

            // setup the listener name if any.
            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(_puppeteerHandler.ListenerNameForPair());
            UiSharedService.AttachToolTip("The In Game Character that can use your trigger phrases below on you");

            // draw the trigger phrase box based on if we are editing or not.
            if (isEditingTriggerOptions)
                _components.DrawEditingTriggersWindow(ref UnsavedTriggerPhrase, ref UnsavedNewStartChar, ref UnsavedNewEndChar);
            else
                _components.DrawTriggersWindow(
                    _puppeteerHandler.SelectedPair.OwnPerms.TriggerPhrase,
                    _puppeteerHandler.SelectedPair.OwnPerms.StartChar.ToString(),
                    _puppeteerHandler.SelectedPair.OwnPerms.EndChar.ToString());
        }
    }

    private void DrawPairTriggersBox()
    {
        if(_puppeteerHandler.SelectedPair?.LastAliasData is null || _clientService.ClientPlayer is null)
            return;

        var name = _puppeteerHandler.SelectedPair.UserData.UID;
        // push rounding window corners
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        // push a pink border color for the window border.
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        // push a less transparent very dark grey background color.
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        // create the child window.
        using (ImRaii.Child("##TriggerDataForPair" + name, new Vector2(ImGui.GetContentRegionAvail().X, 0), true, ImGuiWindowFlags.ChildWindow))
        {
            // draw the listener top row.
            _components.DrawListenerPairGroup(onSendName: () =>
            {
                var name = _clientService.Name;
                var world = _clientService.HomeWorldId;
                var worldName = OnFrameworkService.WorldData.Value[(ushort)world];
                // compile the alias data to send including our own name and world information, along with an empty alias list.
                var dataToPush = new CharaAliasData()
                {
                    HasNameStored = true,
                    ListenerName = name + "@" + worldName,
                };
                _ = _apiHubMain.UserPushPairDataAliasStorageUpdate(new(_puppeteerHandler.SelectedPair.UserData, MainHub.PlayerUserData, dataToPush, PuppeteerUpdateType.PlayerNameRegistered, UpdateDir.Own));
                _logger.LogDebug("Sent Puppeteer Name to " + _puppeteerHandler.SelectedPair.GetNickAliasOrUid(), LoggerType.Permissions);
            });

            // Draw out the listener name.
            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(_puppeteerHandler.SelectedPair.LastAliasData.HasNameStored 
                ? _clientService.ClientPlayer.GetNameWithWorld() : "Not Yet Listening!");

            // Draw the display for trigger phrases.
            _components.DrawTriggersWindow(
                _puppeteerHandler.SelectedPair.PairPerms.TriggerPhrase,
                _puppeteerHandler.SelectedPair.PairPerms.StartChar.ToString(),
                _puppeteerHandler.SelectedPair.PairPerms.EndChar.ToString());
        }
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
}
