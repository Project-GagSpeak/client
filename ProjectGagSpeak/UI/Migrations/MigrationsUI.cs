using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Components;
using ImGuiNET;

namespace GagSpeak.UI;

internal class MigrationsUI : WindowMediatorSubscriberBase
{
    private readonly MigrationTabs _tabMenu;
    private readonly AccountInfoExchanger _infoExchanger;
    private readonly GagspeakConfigService _mainConfig;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;
    private bool ThemePushed = false;

    // Come back to this when we actually have everything working properly.
    public MigrationsUI(ILogger<InteractionEventsUI> logger, GagspeakMediator mediator,
        AccountInfoExchanger infoExchanger, GagspeakConfigService config, CosmeticService cosmetics,
        UiSharedService uiShared) : base(logger, mediator, "GagSpeak Migrations")
    {
        _infoExchanger = infoExchanger;
        _mainConfig = config;
        _cosmetics = cosmetics;
        _uiShared = uiShared;

        _tabMenu = new MigrationTabs(_uiShared);

        AllowPinning = false;
        AllowClickthrough = false;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(740, 480),
            MaximumSize = new Vector2(740, 480)
        };
    }

    private HashSet<string> CurrentAccountUids = new HashSet<string>();
    private string SelectedAccountUid = string.Empty;

    // The temporary data storage containers that we will use to store the data we are migrating.
    private Dictionary<GagType, GarblerRestriction> LoadedGagData = new Dictionary<GagType, GarblerRestriction>();
    private List<RestraintSet> LoadedRestraints = new List<RestraintSet>();
    private List<CursedItem> LoadedCursedItems = new List<CursedItem>();
    private List<Trigger> LoadedTriggers = new List<Trigger>();
    private List<Alarm> LoadedAlarms = new List<Alarm>();

    private GagType SelectedGag = GagType.None;
    private RestraintSet? SelectedRestraintSet = null;
    private CursedItem? SelectedCursedItem = null;
    private Trigger? SelectedTrigger = null;
    private Alarm? SelectedAlarm = null;

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
        // get information about the window region, its item spacing, and the topleftside height.
        var region = ImGui.GetContentRegionAvail();
        
        _tabMenu.Draw(region.X);

        // display right half viewport based on the tab selection
        using (ImRaii.Child($"###GagSetupRight", Vector2.Zero, false))
        {
            switch (_tabMenu.TabSelection)
            {
                case MigrationTabs.SelectedTab.Restraints:
                    DrawTransferRestraints();
                    break;
                case MigrationTabs.SelectedTab.Restrictions:
                    break;
                case MigrationTabs.SelectedTab.Gags:
                    DrawTransferGags();
                    break;
                case MigrationTabs.SelectedTab.CursedLoot:
                    DrawTransferCursedLoot();
                    break;
                case MigrationTabs.SelectedTab.Alarms:
                    DrawTransferAlarms();
                    break;
                case MigrationTabs.SelectedTab.Triggers:
                    DrawTransferTriggers();
                    break;
                default:
                    break;
            };
        }
    }

    private void DrawTransferGags()
    {
        /*DrawUidSelector();
        ImGui.Separator();
        _uiShared.GagspeakBigText(" Transfer GagData:");

        IEnumerable<GagType> GagsToMigrate = LoadedGagData.Keys;
        if (SelectedGag is GagType.None)
            SelectedGag = GagsToMigrate.FirstOrDefault();

        ImGui.SameLine();
        var size = _uiShared.CalcFontTextSize("A", _uiShared.GagspeakLabelFont);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        _uiShared.DrawComboSearchable("GagStorage Gag Type", 175f, GagsToMigrate, (gag) => gag.GagName(), false, (i) => SelectedGag = i, SelectedGag);

        ImUtf8.SameLineInner();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        if (_uiShared.IconTextButton(FontAwesomeIcon.FileImport, "Transfer All", disabled: LoadedGagData.Count == 0))
        {
            foreach (var (gag, gagData) in LoadedGagData)
                _clientConfigs.GagStorageConfig.GagStorage.GagEquipData[gag] = gagData;
        }

        ImGui.Separator();
        if (SelectedGag != GagType.None)
        {
            if (ImGui.Button("Transfer " + SelectedGag.GagName() + " Data", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight())))
            {
                if (LoadedGagData.TryGetValue(SelectedGag, out var dataToMigrate))
                    _clientConfigs.GagStorageConfig.GagStorage.GagEquipData[SelectedGag] = dataToMigrate;
            }
            ImGui.Separator();
        }

        if (LoadedGagData.TryGetValue(SelectedGag, out var data))
        {
            _uiShared.GagspeakBigText("Gag Data Glamour:");
            using (ImRaii.Group())
            {
                _previewer.DrawEquipSlotPreview(data, 100f);

                ImGui.SameLine();
                using (ImRaii.Group())
                {
                    ImGui.Text($"Slot: {data.Slot}");
                    ImGui.Text($"GameItem: {data.GameItem.Name}");
                }
            }
            _uiShared.GagspeakBigText("Adjustments:");
            ImGui.Text("IsEnabled:");
            ImGui.SameLine();
            _uiShared.BooleanToColoredIcon(data.IsEnabled);

            ImGui.Text("ForceHeadgear:");
            ImGui.SameLine();
            _uiShared.BooleanToColoredIcon(data.ForceHeadgear);

            ImGui.Text("ForceVisor:");
            ImGui.SameLine();
            _uiShared.BooleanToColoredIcon(data.ForceVisor);

            ImGui.Text("Contains Gag Moodles:");
            ImGui.SameLine();
            _uiShared.BooleanToColoredIcon(data.AssociatedMoodles.Count > 0);
        }*/
    }

    private void DrawTransferRestraints()
    {/*
        // only display the restraint sets that we do not already have in our client config.
        IEnumerable<RestraintSet> RestraintsToMigrate = LoadedRestraints;
        if (SelectedRestraintSet is null)
        {
            SelectedRestraintSet = RestraintsToMigrate.FirstOrDefault();
        }

        DrawUidSelector();
        ImGui.Separator();

        _uiShared.GagspeakBigText(" Transfer Restraints:");
        var size = _uiShared.CalcFontTextSize(" Transfer Restraints:", _uiShared.GagspeakLabelFont);
        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.FileImport, "Transfer All");
        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - 175f - buttonSize - ImGui.GetStyle().ItemSpacing.X * 2);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        _uiShared.DrawComboSearchable("Restraint Set List Items.", 175f, RestraintsToMigrate, (item) => item.Name, false, (i) =>
        {
            if (i is not null)
            {
                SelectedRestraintSet = i;
            }
        }, SelectedRestraintSet ?? default);

        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - buttonSize - ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        if (_uiShared.IconTextButton(FontAwesomeIcon.FileImport, "Transfer All", disabled: LoadedRestraints.Count == 0))
        {
            _clientConfigs.AddNewRestraintSets(LoadedRestraints);
        }

        ImGui.Separator();
        if (SelectedRestraintSet is not null)
        {
            using (ImRaii.Table("Restraint Information", 2, ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("##Info", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Preview", ImGuiTableColumnFlags.WidthFixed, 200f * ImGuiHelpers.GlobalScale);
                ImGui.TableNextColumn();

                if (ImGui.Button("Transfer Restraint: " + SelectedRestraintSet.Name, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight())))
                {
                    _clientConfigs.AddNewRestraintSet(SelectedRestraintSet);
                }
                ImGui.Separator();
                // info here....
                _uiShared.GagspeakBigText("Restraint Info:");
                ImGui.Text("Name: " + SelectedRestraintSet.Name);
                ImGui.Text("Description: " + SelectedRestraintSet.Description);

                ImGui.Text("ForceHeadgear:");
                ImGui.SameLine();
                _uiShared.BooleanToColoredIcon(SelectedRestraintSet.ForceHeadgear);

                ImGui.Text("ForceVisor:");
                ImGui.SameLine();
                _uiShared.BooleanToColoredIcon(SelectedRestraintSet.ForceVisor);

                ImGui.Text("Has Customize Data:");
                ImGui.SameLine();
                _uiShared.BooleanToColoredIcon(SelectedRestraintSet.CustomizeObject != null);

                ImGui.Text("Has Attached Mods:");
                ImGui.SameLine();
                _uiShared.BooleanToColoredIcon(SelectedRestraintSet.AssociatedMods.Count > 0);

                ImGui.Text("Contains Gag Moodles:");
                ImGui.SameLine();
                _uiShared.BooleanToColoredIcon(SelectedRestraintSet.AssociatedMoodles.Count > 0);

                ImGui.TableNextColumn();
                // for some wierd ass reason when we calculate the centered preview text you need to take away the window padding or else it will never fit???
                var previewRegion = ImGui.GetContentRegionAvail() - ImGui.GetStyle().WindowPadding;
                _previewer.DrawRestraintSetPreviewCentered(SelectedRestraintSet, previewRegion);
            }
        }*/
    }

    private void DrawTransferCursedLoot()
    {
        DrawUidSelector();
        ImGui.Separator();
/*
        // only display the restraint sets that we do not already have in our client config.
        IEnumerable<CursedItem> CursedItemsToMigrate = LoadedCursedItems;
        if (SelectedCursedItem is null)
        {
            SelectedCursedItem = CursedItemsToMigrate.FirstOrDefault();
        }

        _uiShared.GagspeakBigText(" Transfer Cursed Loot:");
        var size = _uiShared.CalcFontTextSize(" Transfer Cursed Loot:", _uiShared.GagspeakLabelFont);
        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.FileImport, "Transfer All");
        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - 175f - buttonSize - ImGui.GetStyle().ItemSpacing.X * 2);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        _uiShared.DrawComboSearchable("Cursed Loot Migration Items", 175f, CursedItemsToMigrate, (item) => item.Name, false,
            (i) => { if (i is not null) { SelectedCursedItem = i; } }, SelectedCursedItem ?? default);

        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - buttonSize - ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        if (_uiShared.IconTextButton(FontAwesomeIcon.FileImport, "Transfer All", disabled: LoadedCursedItems.Count == 0))
        {
            foreach (var item in LoadedCursedItems)
                _clientConfigs.AddCursedItem(item);
        }
        ImGui.Separator();

        if (SelectedCursedItem is not null)
        {
            if (ImGui.Button("Transfer Cursed-Item: " + SelectedCursedItem.Name, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight())))
            {
                _clientConfigs.AddCursedItem(SelectedCursedItem);
            }
            ImGui.Separator();
        }


        if (SelectedCursedItem is not null)
        {
            _uiShared.GagspeakBigText("Cursed Item Info:");
            UiSharedService.ColorText("Name:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedCursedItem.Name);

            UiSharedService.ColorText("In Pool: ", ImGuiColors.ParsedGold);
            _uiShared.BooleanToColoredIcon(SelectedCursedItem.InPool, true);

            UiSharedService.ColorText("Cursed Item Type:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedCursedItem.IsGag ? "Gag" : "Equip");

            if (SelectedCursedItem.IsGag)
            {
                UiSharedService.ColorText("Gag Type:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(SelectedCursedItem.GagType.GagName());
            }
            else
            {
                // display equip stuff.
                UiSharedService.ColorText("Can Override: ", ImGuiColors.ParsedGold);
                _uiShared.BooleanToColoredIcon(SelectedCursedItem.CanOverride, true);

                UiSharedService.ColorText("Override Precedence: ", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(SelectedCursedItem.OverridePrecedence.ToString());

                UiSharedService.ColorText("Applied Item:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(SelectedCursedItem.AppliedItem.GameItem.Name);

                UiSharedService.ColorText("Associated Mod:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(SelectedCursedItem.AssociatedMod.Mod.Name);

                UiSharedService.ColorText("Moodle Type:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(SelectedCursedItem.MoodleType.ToString());
            }
        }*/
    }

    private void DrawTransferTriggers()
    {
        DrawUidSelector();
        ImGui.Separator();
/*
        // only display the restraint sets that we do not already have in our client config.
        IEnumerable<Trigger> TriggersToMigrate = LoadedTriggers;
        if (SelectedTrigger is null)
        {
            SelectedTrigger = TriggersToMigrate.FirstOrDefault();
        }

        _uiShared.GagspeakBigText(" Transfer Triggers:");
        var size = _uiShared.CalcFontTextSize(" Transfer Triggers:", _uiShared.GagspeakLabelFont);
        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.FileImport, "Transfer All");
        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - 175f - buttonSize - ImGui.GetStyle().ItemSpacing.X * 2);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        _uiShared.DrawComboSearchable("Trigger Migration Items", 175f, TriggersToMigrate, (item) => item.Name, false,
            (i) => { if (i is not null) { SelectedTrigger = i; } }, SelectedTrigger ?? default);

        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - buttonSize - ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        if (_uiShared.IconTextButton(FontAwesomeIcon.FileImport, "Transfer All", disabled: LoadedTriggers.Count == 0))
        {
            foreach (var item in LoadedTriggers)
                _clientConfigs.AddNewTrigger(item);
        }
        ImGui.Separator();

        if (SelectedTrigger is not null)
        {
            if (ImGui.Button("Transfer [" + SelectedTrigger.Type.ToString() + "] Trigger: " + SelectedTrigger.Name, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight())))
                _clientConfigs.AddNewTrigger(SelectedTrigger);
            ImGui.Separator();

            _uiShared.GagspeakBigText("Trigger Info:");
            UiSharedService.ColorText("Name:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedTrigger.Name);

            UiSharedService.ColorText("Type:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedTrigger.Type.ToString());

            UiSharedService.ColorText("Priority:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedTrigger.Priority.ToString());

            UiSharedService.ColorText("Description:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            UiSharedService.TextWrapped(SelectedTrigger.Description);

            UiSharedService.ColorText("Trigger Action Kind:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedTrigger.GetTypeName().ToName());
        }*/
    }

    private void DrawTransferAlarms()
    {
        DrawUidSelector();
        ImGui.Separator();
/*
        // only display the restraint sets that we do not already have in our client config.
        IEnumerable<Alarm> AlarmsToMigrate = LoadedAlarms;
        if (SelectedAlarm is null)
        {
            SelectedAlarm = AlarmsToMigrate.FirstOrDefault();
        }

        _uiShared.GagspeakBigText(" Transfer Alarms:");
        var size = _uiShared.CalcFontTextSize(" Transfer Alarms:", _uiShared.GagspeakLabelFont);
        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.FileImport, "Transfer All");
        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - 175f - buttonSize - ImGui.GetStyle().ItemSpacing.X * 2);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        _uiShared.DrawComboSearchable("Alarm Migration Items", 175f, AlarmsToMigrate, (item) => item.Name, false,
            (i) => { if (i is not null) { SelectedAlarm = i; } }, SelectedAlarm ?? default);

        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - buttonSize - ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        if (_uiShared.IconTextButton(FontAwesomeIcon.FileImport, "Transfer All", disabled: LoadedAlarms.Count == 0))
        {
            foreach (var item in LoadedAlarms)
                _clientConfigs.AddNewAlarm(item);
        }
        ImGui.Separator();

        if (SelectedAlarm is not null)
        {
            if (ImGui.Button("Transfer Alarm: " + SelectedAlarm.Name, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight())))
                _clientConfigs.AddNewAlarm(SelectedAlarm);
            ImGui.Separator();

            _uiShared.GagspeakBigText("Alarm Info:");
            UiSharedService.ColorText("Name:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedAlarm.Name);

            UiSharedService.ColorText("Set Time:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedAlarm.SetTimeUTC.ToLocalTime().ToString("HH:mm"));

            UiSharedService.ColorText("Pattern to Play:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedAlarm.PatternToPlay.ToString());

            UiSharedService.ColorText("StartPoint:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedAlarm.PatternStartPoint.ToString(@"mm\:ss"));

            UiSharedService.ColorText("Duration:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedAlarm.PatternDuration.ToString(@"mm\:ss"));

            UiSharedService.ColorText("Set Alarm on Days:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(string.Join(", ", SelectedAlarm.RepeatFrequency.Select(day => day.ToString())));
        }*/
    }

    private void DrawUidSelector()
    {
        if (CurrentAccountUids.Count == 0)
        {
            CurrentAccountUids = _infoExchanger.GetUIDs()/*.Except(new[] { MainHub.UID }).ToHashSet()*/;
            // set the selected item UID of us.
            SelectedAccountUid = CurrentAccountUids.First();
            LoadDataForUid(SelectedAccountUid); // Might cause a massive crash? idk lol we will find out later.
        }
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        Vector2 textSize;
        using (_uiShared.UidFont.Push()) { textSize = ImGui.CalcTextSize("Select Account UID"); }
        var centerYpos = (textSize.Y - ImGui.GetFrameHeight());

        using (ImRaii.Child("MigrationsHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), ImGui.GetFrameHeight() + (centerYpos - startYpos) * 2)))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemSpacing.X);
            using (_uiShared.UidFont.Push())
            {
                ImGui.AlignTextToFramePadding();
                UiSharedService.ColorText("Select Account UID", ImGuiColors.ParsedPink);
            }
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - 175f - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            // let them pick what account they want to have migrations for.
            _uiShared.DrawCombo("##Account UID", 175f, CurrentAccountUids, (name) => name, (selected) =>
            {
                if (selected is not null && selected != SelectedAccountUid)
                {
                    // Set the new selected item, and load in all data from that profile (temporarily)
                    SelectedAccountUid = selected;
                    LoadDataForUid(SelectedAccountUid);
                }
            }, SelectedAccountUid);
        }
    }

    private void LoadDataForUid(string uid)
    {
        _logger.LogDebug("Loading data for UID: " + uid);
/*
        var gagStorage = _infoExchanger.GetGagStorageFromUID(uid);
        LoadedGagData = gagStorage.GagEquipData.Where(kvp => kvp.Value.GameItem.ItemId != ItemService.NothingItem(kvp.Value.Slot).ItemId).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var importedSets = _infoExchanger.GetRestraintSetsFromUID(uid);
        LoadedRestraints = importedSets.Where(x => _clientConfigs.GetSetIdxByGuid(x.RestraintId) == -1).ToList();
        // Ensure that the restraint sets are not enabled and don't have an active lock.
        foreach (var restraint in LoadedRestraints)
        {
            restraint.Enabled = false;
            restraint.EnabledBy = string.Empty;
            restraint.Padlock = Padlocks.None.ToName();
            restraint.Password = string.Empty;
            restraint.Timer = DateTimeOffset.MinValue;
            restraint.Assigner = string.Empty;
        }

        var importedCursedItems = _infoExchanger.GetCursedItemsFromUID(uid);
        LoadedCursedItems = importedCursedItems.Where(x => _clientConfigs.IsGuidInItems(x.Identifier) is false).ToList();

        var importedTriggers = _infoExchanger.GetTriggersFromUID(uid);
        LoadedTriggers = importedTriggers.Where(x => _clientConfigs.IsGuidInTriggers(x.Identifier) is false).ToList();

        var importedAlarms = _infoExchanger.GetAlarmsFromUID(uid);
        LoadedAlarms = importedAlarms.Where(x => _clientConfigs.IsGuidInAlarms(x.Identifier) is false).ToList();
*/
        // reset any selected variables
        SelectedGag = GagType.None;
        SelectedRestraintSet = null;
        SelectedCursedItem = null;
        SelectedTrigger = null;
        SelectedAlarm = null;
    }

    public override void OnClose()
    {
        base.OnClose();

        // clear out the temporary data storage containers.
        LoadedGagData = new Dictionary<GagType, GarblerRestriction>();
        LoadedRestraints = new List<RestraintSet>();
        LoadedCursedItems = new List<CursedItem>();
        LoadedTriggers = new List<Trigger>();
        LoadedAlarms = new List<Alarm>();
    }

}
