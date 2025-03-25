using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Components;
using GagSpeak.UI.Handlers;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.UI.Puppeteer;

// It is out of place for this to have this many namespaces. Something is up, but im too bothered to fix it.
public class PuppeteerUI : WindowMediatorSubscriberBase
{
    private readonly PuppeteerTabs _tabMenu;
    private readonly MainHub _hub;
    private readonly GlobalData _clientData;
    private readonly PuppeteerComponents _components;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly PuppeteerManager _puppetManager;
    private readonly VisualApplierMoodles _moodles;
    private readonly UserPairListHandler _pairList;
    private readonly ClientMonitor _clientMonitor;
    private readonly CkGui _ckGui;
    public PuppeteerUI(ILogger<PuppeteerUI> logger, GagspeakMediator mediator,
        MainHub hub, GlobalData clientData, PuppeteerComponents components,
        GagRestrictionManager gags, RestrictionManager restrictions,
        RestraintManager restraints, PuppeteerManager manager,
        VisualApplierMoodles moodles, UserPairListHandler pairList,
        ClientMonitor clientMonitor, CkGui uiShared) : base(logger, mediator, "Puppeteer UI")
    {
        _hub = hub;
        _clientData = clientData;
        _components = components;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _puppetManager = manager;
        _moodles = moodles;
        _pairList = pairList;
        _clientMonitor = clientMonitor;
        _ckGui = uiShared;

        _tabMenu = new PuppeteerTabs(manager);

        AllowPinning = false;
        AllowClickthrough = false;
        // define initial size of window and to not respect the close hotkey.
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(690, 370),
            MaximumSize = new Vector2(1000, float.MaxValue)
        };
        RespectCloseHotkey = false;

        _tabMenu.TabSelectionChanged += (oldTab, newTab) => HandleTabSelection(oldTab, newTab);
    }

    private string AliasSearchString = string.Empty;
    private bool isEditingTriggerOptions = false;
    private string UnsavedTriggerPhrase = string.Empty;
    private string UnsavedNewStartChar = string.Empty;
    private string UnsavedNewEndChar = string.Empty;
    private DateTime LastSaveTime = DateTime.MinValue;

    private bool ThemePushed = false;

    private void HandleTabSelection(PuppeteerTabs.SelectedTab oldTab, PuppeteerTabs.SelectedTab newTab)
    {
        _puppetManager.StopEditing();
        if (newTab is PuppeteerTabs.SelectedTab.GlobalAliasList)
        {
            _puppetManager.StartEditingGlobal();
        }
        else
        {
            if (oldTab is PuppeteerTabs.SelectedTab.GlobalAliasList)
            {
                _puppetManager.StopEditing();
            }
        }
    }


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

        // Dunno why we arent using a pair selector here instead but whatever.
        using (ImRaii.Child("##PuppetPairs", new Vector2(region.X/4, region.Y), false, WFlags.NoScrollbar))
        {
            _pairList.DrawSearchFilter(false);
            _pairList.DrawPairListSelectable(true, 2);
        }

        ImGui.SameLine();
        using var group = ImRaii.Group();
        // Draw tab bar thing here or whatever.
        switch (_tabMenu.TabSelection)
        {
            case PuppeteerTabs.SelectedTab.GlobalAliasList:
                DrawGlobalPuppeteer();
                break;
            case PuppeteerTabs.SelectedTab.TriggerPhrases:
                DrawTriggerPhrases();
                break;
            case PuppeteerTabs.SelectedTab.PairAliasList:
                if(_pairList.SelectedPair is not null)
                    DrawAliasList(_puppetManager.PairAliasStorage[_pairList.SelectedPair.UserData.UID].Storage);
                break;
            case PuppeteerTabs.SelectedTab.OtherPairAliasList:
                DrawPairAliasList();
                break;
        }
    }

    private void DrawGlobalPuppeteer()
    {
        using (UiFontService.GagspeakTitleFont.Push()) ImGuiUtil.Center("Global Alias Triggers");
        ImGui.Separator();
        DrawAliasList(_puppetManager.GlobalAliasStorage);
    }

    private void DrawTriggerPhrases()
    {
        using (UiFontService.GagspeakTitleFont.Push()) ImGuiUtil.Center("Trigger Phrases");
        ImGui.Separator();

        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(5f * _ckGui.GetFontScalerFloat(), 0));
        using (ImRaii.Table("##TriggersDisplayForPair", 2, ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetContentRegionAvail().X / 2);
            ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextColumn();
            DrawClientTriggersBox();
            ImGui.TableNextColumn();
            DrawPairTriggersBox();
        }
    }

    private void DrawClientTriggersBox()
    {
        if (_pairList.SelectedPair is not { } pair)
            return;

        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        // create the child window.
        using (ImRaii.Child("##TriggerDataForClient", new Vector2(ImGui.GetContentRegionAvail().X, 0), true, WFlags.ChildWindow | WFlags.NoScrollbar))
        {
            // Draw the client change actions.
            _components.DrawListenerClientGroup(isEditingTriggerOptions,
                (newSits) =>
                {
                    _logger.LogTrace($"Updated AllowSits permission: " + newSits);
                    _ = _hub.UserUpdateOwnPairPerm(new(pair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("SitRequests", newSits), UpdateDir.Own));
                },
                (newMotions) =>
                {
                    _logger.LogTrace($"Updated AllowMotions permission: " + newMotions);
                    _ = _hub.UserUpdateOwnPairPerm(new(pair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("MotionRequests", newMotions), UpdateDir.Own));
                },
                (newAlias) =>
                {
                    _logger.LogTrace($"Updated AllowAlias permission: " + newAlias);
                    _ = _hub.UserUpdateOwnPairPerm(new(pair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("AliasRequests", newAlias), UpdateDir.Own));
                },
                (newAll) =>
                {
                    _logger.LogTrace($"Updated AllowAll permission: " + newAll);
                    _ = _hub.UserUpdateOwnPairPerm(new(pair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("AllRequests", newAll), UpdateDir.Own));
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
                            _ = _hub.UserUpdateOwnPairPerm(new(pair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("TriggerPhrase", UnsavedTriggerPhrase), UpdateDir.Own));
                            UnsavedTriggerPhrase = string.Empty;
                        }
                        if (!UnsavedNewStartChar.IsNullOrEmpty())
                        {
                            _logger.LogTrace($"Updated own pair permission: StartChar to {UnsavedNewStartChar}");
                            _ = _hub.UserUpdateOwnPairPerm(new(pair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("StartChar", UnsavedNewStartChar[0]), UpdateDir.Own));
                            UnsavedNewStartChar = string.Empty;
                        }
                        if (!UnsavedNewEndChar.IsNullOrEmpty())
                        {
                            _logger.LogTrace($"Updated own pair permission: EndChar to {UnsavedNewEndChar}");
                            _ = _hub.UserUpdateOwnPairPerm(new(pair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("EndChar", UnsavedNewEndChar[0]), UpdateDir.Own));
                            UnsavedNewEndChar = string.Empty;
                        }
                        LastSaveTime = DateTime.Now;
                    }
                    else
                    {
                        UnsavedTriggerPhrase = pair.OwnPerms.TriggerPhrase;
                        UnsavedNewStartChar = pair.OwnPerms.StartChar.ToString();
                        UnsavedNewEndChar = pair.OwnPerms.EndChar.ToString();
                    }
                });

            // setup the listener name if any.
            using (ImRaii.PushFont(UiBuilder.MonoFont))
                ImGui.TextUnformatted(_puppetManager.PairAliasStorage[pair.UserData.UID].StoredNameWorld);
            CkGui.AttachToolTip("The In Game Character that can use your trigger phrases below on you");

            // draw the trigger phrase box based on if we are editing or not.
            if (isEditingTriggerOptions)
                _components.DrawEditingTriggersWindow(ref UnsavedTriggerPhrase, ref UnsavedNewStartChar, ref UnsavedNewEndChar);
            else
                _components.DrawTriggersWindow(
                    pair.OwnPerms.TriggerPhrase,
                    pair.OwnPerms.StartChar.ToString(),
                    pair.OwnPerms.EndChar.ToString());
        }
    }

    private void DrawPairTriggersBox()
    {
        if (_pairList.SelectedPair is not { } pair)
            return;

        var name = pair.UserData.UID;
        // push rounding window corners
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        // push a pink border color for the window border.
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        // push a less transparent very dark grey background color.
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        // create the child window.
        using (ImRaii.Child("##TriggerDataForPair" + name, new Vector2(ImGui.GetContentRegionAvail().X, 0), true, WFlags.ChildWindow))
        {
            // draw the listener top row.
            _components.DrawListenerPairGroup(onSendName: () =>
            {
                var dataToPush = new CharaAliasData()
                {
                    HasNameStored = true,
                    ListenerName = _clientMonitor.ClientPlayer.NameWithWorld(),
                };
                _ = _hub.UserPushPairDataAliasStorage(new(pair.UserData, dataToPush, DataUpdateType.NameRegistered));
                _logger.LogDebug("Sent Puppeteer Name to " + pair.GetNickAliasOrUid(), LoggerType.Permissions);
            });

            // Draw out the listener name.
            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(pair.LastAliasData.HasNameStored
                ? _clientMonitor.ClientPlayer.NameWithWorld() : "Not Yet Listening!");

            // Draw the display for trigger phrases.
            _components.DrawTriggersWindow(
                pair.PairPerms.TriggerPhrase,
                pair.PairPerms.StartChar.ToString(),
                pair.PairPerms.EndChar.ToString());
        }
    }

    private void DrawAliasList(List<AliasTrigger> aliasList)
    {
        using (ImRaii.Child("##AliasListChild", ImGui.GetContentRegionAvail(), false, WFlags.NoScrollbar))
        {
            // Formatting.
            using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
            using var windowPadding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(8, 5));
            using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(4, 2));
            using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
            using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
            using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

            // Draw out the search filter, then the list of alias's below.
            DrawSearchFilter(ref AliasSearchString, true, true, _puppetManager.ActiveEditorItem is not null,
            () => _puppetManager.CreateNew(), // On [+Add] Button
            (newEditState) => // On [Edit] Button
            {
                if (newEditState)
                {
                    if (_tabMenu.TabSelection is PuppeteerTabs.SelectedTab.GlobalAliasList)
                        _puppetManager.StartEditingGlobal();
                    else _puppetManager.StartEditingPair(_pairList.SelectedPair);
                }
                else _puppetManager.SaveChangesAndStopEditing();
            });

            ImGui.Separator();
            using var seperatorColor = ImRaii.PushColor(ImGuiCol.Separator, ImGuiColors.ParsedPink);

            List<AliasTrigger> filteredList = aliasList
                .Where(trigger => trigger.Label.Contains(AliasSearchString, StringComparison.OrdinalIgnoreCase))
                .ToList();
            // Since this will be reworked, just make a dummy fix for now.
            var lightRestrictions = new List<LightRestriction>();
            LightRestraintSet[] lightSets = [];
            var ipcData = new CharaIPCData();

            var aliasToRemove = Guid.Empty; // used for removing items after we finish drawing the list.
            foreach (var aliasItem in filteredList)
            {
                if (_puppetManager.ActiveEditorItem is not null)
                {
                    _components.DrawAliasItemEditBox(aliasItem, out var wasRemoved);
                    if (wasRemoved)
                    {
                        // delete and early return to prevent null draws.
                        _puppetManager.Delete(aliasItem);
                        return;
                    }
                }
                else _components.DrawAliasItemBox(aliasItem.Identifier.ToString(), aliasItem);
            }
        }
    }

    private void DrawPairAliasList()
    {
        if (_pairList.SelectedPair is not { } pair)
            return;

        // if any of our data is invalid, do not show.
        if (MainHub.ServerStatus is not ServerState.Connected || pair.LastAliasData is null)
        {
            CkGui.BigText("Not Connected, or required Data is null!");
            return;
        }

        using (ImRaii.Child("##PairAliasListChild", ImGui.GetContentRegionAvail(), false, WFlags.NoScrollbar))
        {
            // Formatting.
            using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
            using var windowPadding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(8, 5));
            using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(4, 2));
            using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
            using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
            using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

            DrawSearchFilter(ref AliasSearchString, false, false);
            ImGui.Separator();

            if (pair.LastAliasData.AliasList.Count <= 0)
            {
                CkGui.GagspeakBigText("No Alias's found from this Kinkster!");
                return;
            }

            using var seperatorColor = ImRaii.PushColor(ImGuiCol.Separator, ImGuiColors.ParsedPink);

            var items = pair.LastAliasData.AliasList.Where(a => a.Label.Contains(AliasSearchString, StringComparison.OrdinalIgnoreCase));
            var lightRestraints = pair.LastLightStorage.Restraints;
            var moodlesInfo = pair.LastIpcData ?? new CharaIPCData();

            // Draw out the pairs list
            foreach (var alias in items)
                _components.DrawAliasItemBox(alias.Identifier.ToString(), alias);
        }
    }

    public void DrawSearchFilter(ref string searchStr, bool showAdd, bool showEdit, bool isEditing = false,
        Action? onAdd = null, Action<bool>? onEditToggle = null)
    {
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var editSize = showEdit ? CkGui.IconButtonSize(FAI.Edit).X + spacing : 0;
        var addNewSize = showAdd ? CkGui.IconTextButtonSize(FAI.Plus, "New Alias") + spacing : 0;
        var clearSize = CkGui.IconTextButtonSize(FAI.Ban, "Clear") + spacing;

        var spaceLeft = editSize + addNewSize + clearSize;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - spaceLeft);
        ImGui.InputTextWithHint("##AliasSearchStringFilter", "Search for an Alias", ref AliasSearchString, 255);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Ban, "Clear", disabled: string.IsNullOrEmpty(AliasSearchString)))
            AliasSearchString = string.Empty;
        CkGui.AttachToolTip("Clear the search filter.");

        // Dont show the rest if we are not editing.
        if (showAdd)
        {
            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Plus, "New Alias", disabled: !isEditing))
            {
                _logger.LogDebug("Adding new Alias");
                onAdd?.Invoke();
            }
            CkGui.AttachToolTip("Add a new Alias to the list.");
        }

        if (showEdit)
        {
            ImUtf8.SameLineInner();
            using (ImRaii.PushColor(ImGuiCol.Text, isEditing ? ImGuiColors.ParsedPink : ImGuiColors.DalamudWhite))
                if (CkGui.IconButton(isEditing ? FAI.Save : FAI.Edit))
                    onEditToggle?.Invoke(!isEditing);
            CkGui.AttachToolTip(isEditing ? "Save Changes." : "Start Editing Alias List");
        }
    }
}
