using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.State.Models;
using ImGuiNET;
using CkCommons.Gui;

namespace GagSpeak.Gui;

internal class MigrationsUI : WindowMediatorSubscriberBase
{
    private readonly MigrationTabs _tabMenu = new MigrationTabs();
    private readonly AccountInfoExchanger _infoExchanger;
    private readonly MainConfig _mainConfig;
    private readonly CosmeticService _cosmetics;
    private bool ThemePushed = false;

    // Come back to this when we actually have everything working properly.
    public MigrationsUI(ILogger<InteractionEventsUI> logger, GagspeakMediator mediator,
        AccountInfoExchanger infoExchanger, MainConfig config,
        CosmeticService cosmetics) : base(logger, mediator, "GagSpeak Migrations")
    {
        _infoExchanger = infoExchanger;
        _mainConfig = config;
        _cosmetics = cosmetics;

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
    //private Dictionary<GagType, GarblerRestriction> LoadedGagData = new Dictionary<GagType, GarblerRestriction>();
    //private List<RestraintSet> LoadedRestrictions = new List<RestraintSet>();
    //private List<CursedItem> LoadedCursedItems = new List<CursedItem>();
    //private List<Trigger> LoadedTriggers = new List<Trigger>();
    //private List<Alarm> LoadedAlarms = new List<Alarm>();

    //private GagType SelectedGag = GagType.None;
    //private RestraintSet? SelectedRestraintSet = null;
    //private CursedItem? SelectedCursedItem = null;
    //private Trigger? SelectedTrigger = null;
    //private Alarm? SelectedAlarm = null;

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
                case MigrationTabs.SelectedTab.Restrictions:
                    DrawTransferRestrictions();
                    break;
                case MigrationTabs.SelectedTab.Restraints:
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
        CkGui.GagspeakBigText(" Transfer GagData:");

        IEnumerable<GagType> GagsToMigrate = LoadedGagData.Keys;
        if (SelectedGag is GagType.None)
            SelectedGag = GagsToMigrate.FirstOrDefault();

        ImGui.SameLine();
        var size = CkGui.CalcFontTextSize("A", CkGui.GagspeakLabelFont);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        CkGui.DrawComboSearchable("GagStorage Gag Type", 175f, GagsToMigrate, (gag) => gag.GagName(), false, (i) => SelectedGag = i, SelectedGag);

        ImUtf8.SameLineInner();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        if (CkGui.IconTextButton(FAI.FileImport, "Transfer All", disabled: LoadedGagData.Count == 0))
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
            CkGui.GagspeakBigText("Gag Data Glamour:");
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
            CkGui.GagspeakBigText("Adjustments:");
            ImGui.Text("IsEnabled:");
            ImGui.SameLine();
            CkGui.BooleanToColoredIcon(data.IsEnabled);

            ImGui.Text("ForceHeadgear:");
            ImGui.SameLine();
            CkGui.BooleanToColoredIcon(data.ForceHeadgear);

            ImGui.Text("ForceVisor:");
            ImGui.SameLine();
            CkGui.BooleanToColoredIcon(data.ForceVisor);

            ImGui.Text("Contains Gag Moodles:");
            ImGui.SameLine();
            CkGui.BooleanToColoredIcon(data.AssociatedMoodles.Count > 0);
        }*/
    }

    private void DrawTransferRestrictions()
    {/*
        // only display the restraint sets that we do not already have in our client config.
        IEnumerable<RestraintSet> RestrictionsToMigrate = LoadedRestrictions;
        if (SelectedRestraintSet is null)
        {
            SelectedRestraintSet = RestrictionsToMigrate.FirstOrDefault();
        }

        DrawUidSelector();
        ImGui.Separator();

        CkGui.GagspeakBigText(" Transfer Restrictions:");
        var size = CkGui.CalcFontTextSize(" Transfer Restrictions:", CkGui.GagspeakLabelFont);
        var buttonSize = CkGui.IconTextButtonSize(FAI.FileImport, "Transfer All");
        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - 175f - buttonSize - ImGui.GetStyle().ItemSpacing.X * 2);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        CkGui.DrawComboSearchable("Restriction Set List Items.", 175f, RestrictionsToMigrate, (item) => item.Name, false, (i) =>
        {
            if (i is not null)
            {
                SelectedRestraintSet = i;
            }
        }, SelectedRestraintSet ?? default);

        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - buttonSize - ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        if (CkGui.IconTextButton(FAI.FileImport, "Transfer All", disabled: LoadedRestrictions.Count == 0))
        {
            _clientConfigs.AddNewRestraintSets(LoadedRestrictions);
        }

        ImGui.Separator();
        if (SelectedRestraintSet is not null)
        {
            using (ImRaii.Table("Restriction Information", 2, ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("##Info", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Preview", ImGuiTableColumnFlags.WidthFixed, 200f * ImGuiHelpers.GlobalScale);
                ImGui.TableNextColumn();

                if (ImGui.Button("Transfer Restriction: " + SelectedRestraintSet.Name, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight())))
                {
                    _clientConfigs.AddNewRestraintSet(SelectedRestraintSet);
                }
                ImGui.Separator();
                // info here....
                CkGui.GagspeakBigText("Restriction Info:");
                ImGui.Text("Name: " + SelectedRestraintSet.Name);
                ImGui.Text("Description: " + SelectedRestraintSet.Description);

                ImGui.Text("ForceHeadgear:");
                ImGui.SameLine();
                CkGui.BooleanToColoredIcon(SelectedRestraintSet.ForceHeadgear);

                ImGui.Text("ForceVisor:");
                ImGui.SameLine();
                CkGui.BooleanToColoredIcon(SelectedRestraintSet.ForceVisor);

                ImGui.Text("Has Customize Data:");
                ImGui.SameLine();
                CkGui.BooleanToColoredIcon(SelectedRestraintSet.CustomizeObject != null);

                ImGui.Text("Has Attached Mods:");
                ImGui.SameLine();
                CkGui.BooleanToColoredIcon(SelectedRestraintSet.AssociatedMods.Count > 0);

                ImGui.Text("Contains Gag Moodles:");
                ImGui.SameLine();
                CkGui.BooleanToColoredIcon(SelectedRestraintSet.AssociatedMoodles.Count > 0);

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

        CkGui.GagspeakBigText(" Transfer Cursed Loot:");
        var size = CkGui.CalcFontTextSize(" Transfer Cursed Loot:", CkGui.GagspeakLabelFont);
        var buttonSize = CkGui.IconTextButtonSize(FAI.FileImport, "Transfer All");
        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - 175f - buttonSize - ImGui.GetStyle().ItemSpacing.X * 2);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        CkGui.DrawComboSearchable("Cursed Loot Migration Items", 175f, CursedItemsToMigrate, (item) => item.Name, false,
            (i) => { if (i is not null) { SelectedCursedItem = i; } }, SelectedCursedItem ?? default);

        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - buttonSize - ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        if (CkGui.IconTextButton(FAI.FileImport, "Transfer All", disabled: LoadedCursedItems.Count == 0))
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
            CkGui.GagspeakBigText("Cursed Item Info:");
            CkGui.ColorText("Name:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedCursedItem.Name);

            CkGui.ColorText("In Pool: ", ImGuiColors.ParsedGold);
            CkGui.BooleanToColoredIcon(SelectedCursedItem.InPool, true);

            CkGui.ColorText("Cursed Item Type:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedCursedItem.IsGag ? "Gag" : "Equip");

            if (SelectedCursedItem.IsGag)
            {
                CkGui.ColorText("Gag Type:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(SelectedCursedItem.GagType.GagName());
            }
            else
            {
                // display equip stuff.
                CkGui.ColorText("Can Override: ", ImGuiColors.ParsedGold);
                CkGui.BooleanToColoredIcon(SelectedCursedItem.CanOverride, true);

                CkGui.ColorText("Override Precedence: ", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(SelectedCursedItem.OverridePrecedence.ToString());

                CkGui.ColorText("Applied Item:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(SelectedCursedItem.AppliedItem.GameItem.Name);

                CkGui.ColorText("Associated Mod:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(SelectedCursedItem.AssociatedMod.Mod.Name);

                CkGui.ColorText("Moodle Type:", ImGuiColors.ParsedGold);
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

        CkGui.GagspeakBigText(" Transfer Triggers:");
        var size = CkGui.CalcFontTextSize(" Transfer Triggers:", CkGui.GagspeakLabelFont);
        var buttonSize = CkGui.IconTextButtonSize(FAI.FileImport, "Transfer All");
        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - 175f - buttonSize - ImGui.GetStyle().ItemSpacing.X * 2);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        CkGui.DrawComboSearchable("Trigger Migration Items", 175f, TriggersToMigrate, (item) => item.Name, false,
            (i) => { if (i is not null) { SelectedTrigger = i; } }, SelectedTrigger ?? default);

        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - buttonSize - ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        if (CkGui.IconTextButton(FAI.FileImport, "Transfer All", disabled: LoadedTriggers.Count == 0))
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

            CkGui.GagspeakBigText("Trigger Info:");
            CkGui.ColorText("Name:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedTrigger.Name);

            CkGui.ColorText("Type:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedTrigger.Type.ToString());

            CkGui.ColorText("Priority:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedTrigger.Priority.ToString());

            CkGui.ColorText("Description:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            CkGui.TextWrapped(SelectedTrigger.Description);

            CkGui.ColorText("Trigger Action Kind:", ImGuiColors.ParsedGold);
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

        CkGui.GagspeakBigText(" Transfer Alarms:");
        var size = CkGui.CalcFontTextSize(" Transfer Alarms:", CkGui.GagspeakLabelFont);
        var buttonSize = CkGui.IconTextButtonSize(FAI.FileImport, "Transfer All");
        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - 175f - buttonSize - ImGui.GetStyle().ItemSpacing.X * 2);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        CkGui.DrawComboSearchable("Alarm Migration Items", 175f, AlarmsToMigrate, (item) => item.Name, false,
            (i) => { if (i is not null) { SelectedAlarm = i; } }, SelectedAlarm ?? default);

        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - buttonSize - ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        if (CkGui.IconTextButton(FAI.FileImport, "Transfer All", disabled: LoadedAlarms.Count == 0))
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

            CkGui.GagspeakBigText("Alarm Info:");
            CkGui.ColorText("Name:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedAlarm.Name);

            CkGui.ColorText("Set Time:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedAlarm.SetTimeUTC.ToLocalTime().ToString("HH:mm"));

            CkGui.ColorText("Pattern to Play:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedAlarm.PatternToPlay.ToString());

            CkGui.ColorText("StartPoint:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedAlarm.PatternStartPoint.ToString(@"mm\:ss"));

            CkGui.ColorText("Duration:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(SelectedAlarm.PatternDuration.ToString(@"mm\:ss"));

            CkGui.ColorText("Set Alarm on Days:", ImGuiColors.ParsedGold);
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
        using (UiFontService.UidFont.Push()) { textSize = ImGui.CalcTextSize("Select Account UID"); }
        var centerYpos = (textSize.Y - ImGui.GetFrameHeight());

        using (ImRaii.Child("MigrationsHeader", new Vector2(CkGui.GetWindowContentRegionWidth(), ImGui.GetFrameHeight() + (centerYpos - startYpos) * 2)))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemSpacing.X);
            using (UiFontService.UidFont.Push())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.ColorText("Select Account UID", ImGuiColors.ParsedPink);
            }
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - 175f - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            // let them pick what account they want to have migrations for.
            var current = SelectedAccountUid;
            using (var combo = ImRaii.Combo("##Account UID", SelectedAccountUid))
            {
                if (combo)
                    foreach(var accountUid in CurrentAccountUids)
                        if (ImGui.Selectable(accountUid, accountUid == SelectedAccountUid))
                        {
                            SelectedAccountUid = accountUid;
                            LoadDataForUid(SelectedAccountUid);
                        }
            }
        }
    }

    private void LoadDataForUid(string uid)
    {
        _logger.LogDebug("Loading data for UID: " + uid);
/*
        var gagStorage = _infoExchanger.GetGagStorageFromUID(uid);
        LoadedGagData = gagStorage.GagEquipData.Where(kvp => kvp.Value.GameItem.ItemId != ItemSvc.NothingItem(kvp.Value.Slot).ItemId).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var importedSets = _infoExchanger.GetRestraintSetsFromUID(uid);
        LoadedRestrictions = importedSets.Where(x => _clientConfigs.GetSetIdxByGuid(x.RestrictionId) == -1).ToList();
        // Ensure that the restraint sets are not enabled and don't have an active lock.
        foreach (var restraint in LoadedRestrictions)
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
        //SelectedGag = GagType.None;
        //SelectedRestraintSet = null;
        //SelectedCursedItem = null;
        //SelectedTrigger = null;
        //SelectedAlarm = null;
    }

    public override void OnClose()
    {
        base.OnClose();

        // clear out the temporary data storage containers.
        //LoadedGagData = new Dictionary<GagType, GarblerRestriction>();
        //LoadedRestrictions = new List<RestraintSet>();
        //LoadedCursedItems = new List<CursedItem>();
        //LoadedTriggers = new List<Trigger>();
        //LoadedAlarms = new List<Alarm>();
    }

}
