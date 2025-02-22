using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.PlayerState.Visual;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Enums;

namespace GagSpeak.UI;

public class DebugTab
{
    /// <summary> Displays the Debug section within the settings, where we can set our debug level </summary>
    private static readonly Dictionary<string, LoggerType[]> loggerSections = new Dictionary<string, LoggerType[]>
    {
        { "Foundation", new[] { LoggerType.Achievements, LoggerType.AchievementEvents, LoggerType.AchievementInfo } },
        { "Interop", new[] { LoggerType.IpcGagSpeak, LoggerType.IpcCustomize, LoggerType.IpcGlamourer, LoggerType.IpcMare, LoggerType.IpcMoodles, LoggerType.IpcPenumbra } },
        { "State Managers", new[] { LoggerType.AppearanceState, LoggerType.ToyboxState, LoggerType.Mediator, LoggerType.GarblerCore } },
        { "Update Monitors", new[] { LoggerType.ToyboxAlarms, LoggerType.ActionsNotifier, LoggerType.KinkPlateMonitor, LoggerType.EmoteMonitor, LoggerType.ChatDetours, LoggerType.ActionEffects, LoggerType.SpatialAudioLogger } },
        { "Hardcore", new[] { LoggerType.HardcoreActions, LoggerType.HardcoreMovement, LoggerType.HardcorePrompt } },
        { "Data & Modules", new[] { LoggerType.ClientPlayerData, LoggerType.GagHandling, LoggerType.PadlockHandling, LoggerType.Restraints, LoggerType.Puppeteer, LoggerType.CursedLoot, LoggerType.ToyboxDevices, LoggerType.ToyboxPatterns, LoggerType.ToyboxTriggers, LoggerType.VibeControl } },
        { "Pair Data", new[] { LoggerType.PairManagement, LoggerType.PairInfo, LoggerType.PairDataTransfer, LoggerType.PairHandlers, LoggerType.OnlinePairs, LoggerType.VisiblePairs, LoggerType.PrivateRooms, LoggerType.GameObjects } },
        { "Services", new[] { LoggerType.Cosmetics, LoggerType.Textures, LoggerType.GlobalChat, LoggerType.ContextDtr, LoggerType.PatternHub, LoggerType.Safeword } },
        { "UI", new[] { LoggerType.UiCore, LoggerType.UserPairDrawer, LoggerType.Permissions, LoggerType.Simulation } },
        { "WebAPI", new[] { LoggerType.PiShock, LoggerType.ApiCore, LoggerType.Callbacks, LoggerType.Health, LoggerType.HubFactory, LoggerType.JwtTokens } }
    };

    private readonly GagspeakConfigService _mainConfig;
    private readonly GlobalData _playerData;
    private readonly PairManager _pairManager;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CursedLootManager _cursedLoot;
    private readonly PatternManager _patterns;
    private readonly PuppeteerManager _alias;
    private readonly TriggerManager _triggers;
    private readonly UiSharedService _uiShared;
    public DebugTab(GagspeakConfigService config, PairManager pairManager, GlobalData playerData,
        GagRestrictionManager gags, RestrictionManager restrictions, RestraintManager restraints,
        CursedLootManager cursedLoot, PatternManager patterns, PuppeteerManager alias,
        TriggerManager triggers, UiSharedService uiShared)
    {
        _mainConfig = config;
        _playerData = playerData;
        _pairManager = pairManager;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _cursedLoot = cursedLoot;
        _patterns = patterns;
        _alias = alias;
        _triggers = triggers;
        _uiShared = uiShared;
    }

    public void DrawDebugMain()
    {
        _uiShared.GagspeakBigText("Debug Configuration");

        // display the combo box for setting the log level we wish to have for our plugin
        _uiShared.DrawCombo("Log Level", 400, Enum.GetValues<LogLevel>(), (level) => level.ToString(), (level) =>
        {
            _mainConfig.Config.LogLevel = level;
            _mainConfig.Save();
        }, _mainConfig.Config.LogLevel);

        var logFilters = _mainConfig.Config.LoggerFilters;

        // draw a collapsible tree node here to draw the logger settings:
        ImGui.Spacing();
        if (ImGui.TreeNode("Advanced Logger Filters (Only Edit if you know what you're doing!)"))
        {
            AdvancedLogger();
            ImGui.TreePop();
        }
    }

    public void DrawDevDebug()
    {
        if (ImGui.CollapsingHeader("Client Player Data"))
            DrawPlayerCharacterDebug();

        if (ImGui.CollapsingHeader("Pair Data"))
            DrawPairsDebug();
    }

    private void AdvancedLogger()
    {
        var isFirstSection = true;

        // Iterate through each section in loggerSections
        foreach (var section in loggerSections)
        {
            // Begin a new group for the section
            using (ImRaii.Group())
            {
                // Calculate the number of checkboxes in the current section
                var checkboxes = section.Value;

                // Draw a custom line above the table to simulate the upper border
                var drawList = ImGui.GetWindowDrawList();
                var cursorPos = ImGui.GetCursorScreenPos();
                drawList.AddLine(new Vector2(cursorPos.X, cursorPos.Y), new Vector2(cursorPos.X + ImGui.GetContentRegionAvail().X, cursorPos.Y), ImGui.GetColorU32(ImGuiCol.Border));

                // Add some vertical spacing to position the table correctly
                ImGui.Dummy(new Vector2(0, 1));

                // Begin a new table for the checkboxes without any borders
                using (ImRaii.Table(section.Key, 4, ImGuiTableFlags.None))
                {
                    // Iterate through the checkboxes, managing columns and rows
                    for (var i = 0; i < checkboxes.Length; i++)
                    {
                        ImGui.TableNextColumn();

                        var isEnabled = _mainConfig.Config.LoggerFilters.Contains(checkboxes[i]);

                        if (ImGui.Checkbox(checkboxes[i].ToName(), ref isEnabled))
                        {
                            if (isEnabled)
                            {
                                _mainConfig.Config.LoggerFilters.Add(checkboxes[i]);
                                LoggerFilter.AddAllowedCategory(checkboxes[i]);
                            }
                            else
                            {
                                _mainConfig.Config.LoggerFilters.Remove(checkboxes[i]);
                                LoggerFilter.RemoveAllowedCategory(checkboxes[i]);
                            }
                            _mainConfig.Save();
                        }
                    }

                    // Add "All On" and "All Off" buttons for the first section
                    if (isFirstSection)
                    {
                        ImGui.TableNextColumn();
                        if (ImGui.Button("All On"))
                        {
                            _mainConfig.Config.LoggerFilters = LoggerFilter.GetAllRecommendedFilters();
                            _mainConfig.Save();
                            LoggerFilter.AddAllowedCategories(_mainConfig.Config.LoggerFilters);
                        }
                        ImUtf8.SameLineInner();
                        if (ImGui.Button("All Off"))
                        {
                            _mainConfig.Config.LoggerFilters.Clear();
                            _mainConfig.Config.LoggerFilters.Add(LoggerType.None);
                            _mainConfig.Save();
                            LoggerFilter.ClearAllowedCategories();
                        }
                    }
                }

                // Display a tooltip when hovering over any element in the group
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.RectOnly))
                {
                    ImGui.BeginTooltip();
                    UiSharedService.ColorText(section.Key, ImGuiColors.ParsedGold);
                    ImGui.EndTooltip();
                }
            }

            // Mark that the first section has been processed
            isFirstSection = false;
        }

        // Ensure LoggerType.None is always included in the filtered categories
        if (!_mainConfig.Config.LoggerFilters.Contains(LoggerType.None))
        {
            _mainConfig.Config.LoggerFilters.Add(LoggerType.None);
            LoggerFilter.AddAllowedCategory(LoggerType.None);
        }
    }


    private void DrawPlayerCharacterDebug()
    {
        DrawGlobalPermissions("Player", _playerData.GlobalPerms ?? new UserGlobalPermissions());
        DrawAppearance("Player", _gags.ActiveGagsData ?? new CharaActiveGags());
        DrawWardrobe("Player", _restraints.ActiveRestraintData ?? new CharaActiveRestraint());
        // draw an enclosed tree node here for the alias data. Inside of this, we will have a different tree node for each of the keys in our alias storage,.
        using (ImRaii.TreeNode("Alias Data"))
        {
            foreach (var alias in _alias.PairAliasStorage)
            {
                using (ImRaii.TreeNode("Your Alias List for: " + alias.Key))
                {
                    ImGui.Text("Listener Name: " + alias.Value.StoredNameWorld);
                    DrawAlias(alias.Key, alias.Value.Storage.ToAliasData());
                }
            }
        }
/*        DrawToybox("Player", _mainConfig.CompileToyboxToAPI());*/
    }

    private void DrawPairsDebug()
    {
        ImGui.Text($"Total Pairs: {_pairManager.DirectPairs.Count}");
        ImGui.Text($"Visible Users: {_pairManager.GetVisibleUserCount()}");
        // draw an enclosed tree node here for the pair data. Inside of this, we will have a different tree node for each of the keys in our pair storage.
        using var pairData = ImRaii.TreeNode("Pair Data Listings");
        if (!pairData) return;

        var orderedPairs = _pairManager.DirectPairs.OrderBy(u => u.GetNickname() ?? u.UserData.AliasOrUID).ToList();

        foreach (var pair in orderedPairs)
        {
            using var listing = ImRaii.TreeNode(pair.GetNickAliasOrUid() + "'s Pair Info");
            if (!listing) continue;

            DrawPairPerms("Own Pair Perms for " + pair.UserData.UID, pair.OwnPerms);
            DrawPairPermAccess("Own Pair Perm Access for " + pair.UserData.UID, pair.OwnPermAccess);
            DrawGlobalPermissions(pair.UserData.UID + "'s Global Perms", pair.PairGlobals);
            DrawPairPerms(pair.UserData.UID + "'s Pair Perms for you.", pair.PairPerms);
            DrawPairPermAccess(pair.UserData.UID + "'s Pair Perm Access for you", pair.PairPermAccess);
            DrawAppearance(pair.UserData.UID, pair.LastGagData ?? new CharaActiveGags());
            DrawWardrobe(pair.UserData.UID, pair.LastRestraintData ?? new CharaActiveRestraint());
            DrawAlias(pair.UserData.UID, pair.LastAliasData ?? new CharaAliasData());
            DrawToybox(pair.UserData.UID, pair.LastToyboxData ?? new CharaToyboxData());
            DrawLightStorage(pair.UserData.UID, pair.LastLightStorage ?? new CharaLightStorageData());
        }
    }

    private void DrawPermissionRowBool(string name, bool value)
    {
        ImGuiUtil.DrawTableColumn(name);
        ImGui.TableNextColumn();
        _uiShared.IconText(value ? FontAwesomeIcon.Check : FontAwesomeIcon.Times, value ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
        ImGui.TableNextRow();
    }

    private void DrawPermissionRowString(string name, string value)
    {
        ImGuiUtil.DrawTableColumn(name);
        ImGui.TableNextColumn();
        ImGui.Text(value);
        ImGui.TableNextRow();
    }

    private void DrawGlobalPermissions(string uid, UserGlobalPermissions perms)
    {
        using var nodeMain = ImRaii.TreeNode(uid + " Global Perms");
        if (!nodeMain) return;

        using var table = ImRaii.Table("##debug-global" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        ImGui.TableSetupColumn("Permission");
        ImGui.TableSetupColumn("Value");
        ImGui.TableHeadersRow();

        DrawPermissionRowBool("Live Chat Garbler", perms.ChatGarblerActive);
        DrawPermissionRowBool("Live Chat Garbler Locked", perms.ChatGarblerLocked);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Gag Glamours", perms.GagVisuals);
        DrawPermissionRowBool("Wardrobe Active", perms.WardrobeEnabled);
        DrawPermissionRowBool("Restraint Glamours", perms.RestraintSetVisuals);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Puppeteer Active", perms.PuppeteerEnabled);
        DrawPermissionRowString("Global Trigger Phrase", perms.GlobalTriggerPhrase);
        DrawPermissionRowBool("Allow Sit Requests", perms.PuppetPerms.HasFlag(PuppetPerms.Sit));
        DrawPermissionRowBool("Allow Motion Requests", perms.PuppetPerms.HasFlag(PuppetPerms.Emotes));
        DrawPermissionRowBool("Allow Alias Requests", perms.PuppetPerms.HasFlag(PuppetPerms.Alias));
        DrawPermissionRowBool("Allow All Requests", perms.PuppetPerms.HasFlag(PuppetPerms.All));
        ImGui.TableNextRow();
        DrawPermissionRowBool("Toybox Active", perms.ToyboxEnabled);
        DrawPermissionRowBool("Lock Toybox UI", perms.LockToyboxUI);
        DrawPermissionRowBool("Sex Toy Active", perms.ToysAreActive);
        DrawPermissionRowBool("Spatial Vibrator Audio", perms.SpatialAudio);
        ImGui.TableNextRow();
        DrawPermissionRowString("Forced Follow", perms.ForcedFollow);
        DrawPermissionRowString("Forced Emote State", perms.ForcedEmoteState);
        DrawPermissionRowString("Forced Stay", perms.ForcedStay);
        DrawPermissionRowString("Chat Boxes Hidden", perms.ChatBoxesHidden);
        DrawPermissionRowString("Chat Input Hiddeen", perms.ChatInputHidden);
        DrawPermissionRowString("Chat Input Blocked", perms.ChatInputBlocked);
        ImGui.TableNextRow();
        DrawPermissionRowString("Shock Collar Code", perms.GlobalShockShareCode);
        DrawPermissionRowBool("Allow Shocks ", perms.AllowShocks);
        DrawPermissionRowBool("Allow Vibrations", perms.AllowVibrations);
        DrawPermissionRowBool("Allow Beeps", perms.AllowBeeps);
        DrawPermissionRowString("Max Intensity", perms.MaxIntensity.ToString());
        DrawPermissionRowString("Max Duration", perms.MaxDuration.ToString());
    }

    private void DrawPairPerms(string uid, UserPairPermissions perms)
    {
        using var nodeMain = ImRaii.TreeNode(uid + " Pair Perms");
        if (!nodeMain) return;

        using var table = ImRaii.Table("##debug-pair" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        ImGui.TableSetupColumn("Permission");
        ImGui.TableSetupColumn("Value");
        ImGui.TableHeadersRow();

        DrawPermissionRowBool("Is Paused", perms.IsPaused);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Allows Permanent Locks", perms.PermanentLocks);
        DrawPermissionRowBool("Allows Owner Locks", perms.OwnerLocks);
        DrawPermissionRowBool("Allows Devotional Locks", perms.DevotionalLocks);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Apply Gags", perms.ApplyGags);
        DrawPermissionRowBool("Lock Gags", perms.LockGags);
        DrawPermissionRowString("Max Gag Time", perms.MaxGagTime.ToString());
        DrawPermissionRowBool("Unlock Gags", perms.UnlockGags);
        DrawPermissionRowBool("Remove Gags", perms.RemoveGags);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Apply Restraint Sets", perms.ApplyRestraintSets);
        DrawPermissionRowBool("Lock Restraint Sets", perms.LockRestraintSets);
        DrawPermissionRowString("Max Restraint Lock Time", perms.MaxRestraintTime.ToString());
        DrawPermissionRowBool("Unlock Restraint Sets", perms.UnlockRestraintSets);
        DrawPermissionRowBool("Remove Restraint Sets", perms.RemoveRestraintSets);
        ImGui.TableNextRow();
        DrawPermissionRowString("Trigger Phrase", perms.TriggerPhrase);
        DrawPermissionRowString("Start Char", perms.StartChar.ToString());
        DrawPermissionRowString("End Char", perms.EndChar.ToString());
        DrawPermissionRowBool("Allow Sit Requests", perms.PuppetPerms.HasFlag(PuppetPerms.Sit));
        DrawPermissionRowBool("Allow Motion Requests", perms.PuppetPerms.HasFlag(PuppetPerms.Emotes));
        DrawPermissionRowBool("Allow Alias Requests", perms.PuppetPerms.HasFlag(PuppetPerms.Alias));
        DrawPermissionRowBool("Allow All Requests", perms.PuppetPerms.HasFlag(PuppetPerms.All));
        ImGui.TableNextRow();
        DrawPermissionRowBool("Allow Positive Moodles", perms.AllowPositiveStatusTypes);
        DrawPermissionRowBool("Allow Negative Moodles", perms.AllowNegativeStatusTypes);
        DrawPermissionRowBool("Allow Neutral Moodles", perms.AllowSpecialStatusTypes);
        DrawPermissionRowBool("Apply Own Moodles", perms.PairCanApplyOwnMoodlesToYou);
        DrawPermissionRowBool("Apply Your Moodles", perms.PairCanApplyYourMoodlesToYou);
        DrawPermissionRowString("Max Moodle Time", perms.MaxMoodleTime.ToString());
        DrawPermissionRowBool("Allow Permanent Moodles", perms.AllowPermanentMoodles);
        DrawPermissionRowBool("Allow Removing Moodles", perms.AllowRemovingMoodles);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Can Toggle Toy State", perms.CanToggleToyState);
        DrawPermissionRowBool("Can Use Vibe Remote", perms.CanUseRemoteOnToys);
        DrawPermissionRowBool("Can Execute Patterns", perms.ExecutePatterns);
        DrawPermissionRowBool("Can Stop Patterns", perms.CanStopPatterns);
        DrawPermissionRowBool("Can Toggle Alarms", perms.ToggleAlarms);
        DrawPermissionRowBool("Can Send Alarms", perms.ToggleAlarms);
        DrawPermissionRowBool("Can Toggle Triggers", perms.ToggleTriggers);
        ImGui.TableNextRow();
        DrawPermissionRowBool("In Hardcore Mode", perms.InHardcore);
        DrawPermissionRowBool("Devotional States For Pair", perms.PairLockedStates);
        DrawPermissionRowBool("Allow Forced Follow", perms.AllowForcedFollow);
        DrawPermissionRowBool("Allow Forced Sit", perms.AllowForcedSit);
        DrawPermissionRowBool("Allow Forced Emote", perms.AllowForcedEmote);
        DrawPermissionRowBool("Allow Forced To Stay", perms.AllowForcedToStay);
        DrawPermissionRowBool("Allow Hiding Chat Boxes", perms.AllowHidingChatBoxes);
        DrawPermissionRowBool("Allow Hiding Chat Input", perms.AllowHidingChatInput);
        DrawPermissionRowBool("Allow Chat Input Blocking", perms.AllowChatInputBlocking);
        ImGui.TableNextRow();
        DrawPermissionRowString("Shock Collar Share Code", perms.PiShockShareCode);
        DrawPermissionRowBool("Allow Shocks", perms.AllowShocks);
        DrawPermissionRowBool("Allow Vibrations", perms.AllowVibrations);
        DrawPermissionRowBool("Allow Beeps", perms.AllowBeeps);
        DrawPermissionRowString("Max Intensity", perms.MaxIntensity.ToString());
        DrawPermissionRowString("Max Duration", perms.MaxDuration.ToString());
    }

    private void DrawPairPermAccess(string uid, UserEditAccessPermissions perms)
    {
        using var nodeMain = ImRaii.TreeNode(uid + " Perm Edit Access");
        if (!nodeMain) return;

        using var table = ImRaii.Table("##debug-access-pair" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        ImGui.TableSetupColumn("Permission");
        ImGui.TableSetupColumn("Value");
        ImGui.TableHeadersRow();

        // Live Chat Permissions
        DrawPermissionRowBool("Live Chat Garbler Active", perms.ChatGarblerActiveAllowed);
        DrawPermissionRowBool("Live Chat Garbler Locked", perms.ChatGarblerLockedAllowed);
        ImGui.TableNextRow();

        // Padlock permissions
        DrawPermissionRowBool("Allows Permanent Locks", perms.PermanentLocksAllowed);
        DrawPermissionRowBool("Allows Owner Locks", perms.OwnerLocksAllowed);
        DrawPermissionRowBool("Allows Devotional Locks", perms.DevotionalLocksAllowed);
        ImGui.TableNextRow();

        // Gag permissions
        DrawPermissionRowBool("Allows Applying Gags", perms.ApplyGagsAllowed);
        DrawPermissionRowBool("Allows Locking Gags", perms.LockGagsAllowed);
        DrawPermissionRowBool("Max Gag Time", perms.MaxGagTimeAllowed);
        DrawPermissionRowBool("Allows Unlocking Gags", perms.UnlockGagsAllowed);
        DrawPermissionRowBool("Allows Removing Gags", perms.RemoveGagsAllowed);
        ImGui.TableNextRow();

        // Wardrobe Permissions
        DrawPermissionRowBool("Wardrobe Enabled", perms.WardrobeEnabledAllowed);
        DrawPermissionRowBool("Auto Equip Items", perms.GagVisualsAllowed);
        DrawPermissionRowBool("Auto Equip Restraints", perms.RestraintSetVisualsAllowed);
        DrawPermissionRowBool("Apply Restraint Sets", perms.ApplyRestraintSetsAllowed);
        DrawPermissionRowBool("Lock Restraint Sets", perms.LockRestraintSetsAllowed);
        DrawPermissionRowBool("Max Restraint Lock Time", perms.MaxRestraintTimeAllowed);
        DrawPermissionRowBool("Unlock Restraint Sets", perms.UnlockRestraintSetsAllowed);
        DrawPermissionRowBool("Remove Restraint Sets", perms.RemoveRestraintSetsAllowed);
        ImGui.TableNextRow();

        // Puppeteer Permissions
        DrawPermissionRowBool("Puppeteer Enabled", perms.PuppeteerEnabledAllowed);
        DrawPermissionRowBool("Allow Sit Requests", perms.SitRequestsAllowed);
        DrawPermissionRowBool("Allow Motion Requests", perms.MotionRequestsAllowed);
        DrawPermissionRowBool("Allow Alias Requests", perms.AliasRequestsAllowed);
        DrawPermissionRowBool("Allow All Requests", perms.AllRequestsAllowed);
        ImGui.TableNextRow();

        // Moodle Permissions
        DrawPermissionRowBool("Moodles Enabled", perms.MoodlesEnabledAllowed);
        DrawPermissionRowBool("Allow Positive Moodles", perms.PositiveStatusTypesAllowed);
        DrawPermissionRowBool("Allow Negative Moodles", perms.NegativeStatusTypesAllowed);
        DrawPermissionRowBool("Allow Special Moodles", perms.SpecialStatusTypesAllowed);
        DrawPermissionRowBool("Apply Own Moodles", perms.PairCanApplyOwnMoodlesToYouAllowed);
        DrawPermissionRowBool("Apply Your Moodles", perms.PairCanApplyYourMoodlesToYouAllowed);
        DrawPermissionRowBool("Max Moodle Time", perms.MaxMoodleTimeAllowed);
        DrawPermissionRowBool("Allow Permanent Moodles", perms.PermanentMoodlesAllowed);
        DrawPermissionRowBool("Allow Removing Moodles", perms.RemovingMoodlesAllowed);
        ImGui.TableNextRow();

        // Toybox Permissions
        DrawPermissionRowBool("Toybox Enabled", perms.ToyboxEnabledAllowed);
        DrawPermissionRowBool("Lock Toybox UI", perms.LockToyboxUIAllowed);
        DrawPermissionRowBool("Spatial Vibrator Audio", perms.SpatialAudioAllowed);
        DrawPermissionRowBool("Can Toggle Toy State", perms.CanToggleToyStateAllowed);
        DrawPermissionRowBool("Can Use Vibe Remote", perms.CanUseRemoteOnToysAllowed);
        DrawPermissionRowBool("Can Execute Patterns", perms.ExecutePatternsAllowed);
        DrawPermissionRowBool("Can Stop Patterns", perms.CanStopPatternsAllowed);
        DrawPermissionRowBool("Can Toggle Alarms", perms.ToggleAlarmsAllowed);
        DrawPermissionRowBool("Can Send Alarms", perms.ToggleAlarmsAllowed);
        DrawPermissionRowBool("Can Toggle Triggers", perms.ToggleTriggersAllowed);
    }

    private void DrawAppearance(string uid, CharaActiveGags appearance)
    {
        using var nodeMain = ImRaii.TreeNode("Appearance Data");
        if (!nodeMain) return;

        using (ImRaii.Table("##debug-appearance" + uid, 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("##EmptyHeader");
            ImGui.TableSetupColumn("Layer 1");
            ImGui.TableSetupColumn("Layer 2");
            ImGui.TableSetupColumn("Layer 3");
            ImGui.TableHeadersRow();

            ImGuiUtil.DrawTableColumn("GagType:");
            for (var i = 0; i < 3; i++)
                ImGuiUtil.DrawTableColumn(appearance.GagSlots[i].GagItem.GagName());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Padlock:");
            for (var i = 0; i < 3; i++)
                ImGuiUtil.DrawTableColumn(appearance.GagSlots[i].Padlock.ToName());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Password:");
            for (var i = 0; i < 3; i++)
                ImGuiUtil.DrawTableColumn(appearance.GagSlots[i].Password);
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Time Remaining:");
            for (var i = 0; i < 3; i++)
            {
                ImGui.TableNextColumn();
                UiSharedService.ColorText(appearance.GagSlots[i].Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
            }
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Assigner:");
            for (var i = 0; i < 3; i++)
                ImGuiUtil.DrawTableColumn(appearance.GagSlots[i].PadlockAssigner);
        }
    }

    private void DrawWardrobe(string uid, CharaActiveRestraint wardrobe)
    {
        using var nodeMain = ImRaii.TreeNode("Wardrobe Data");
        if (!nodeMain) return;

        using (ImRaii.Table("##debug-wardrobe" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            DrawPermissionRowString("Active Set ID", wardrobe.Identifier.ToString());
            DrawPermissionRowString("Active Set Enabled By", wardrobe.Enabler);
            DrawPermissionRowString("Padlock", wardrobe.Padlock.ToName());
            DrawPermissionRowString("Password", wardrobe.Password);
            ImGuiUtil.DrawTableColumn("Expiration Time");
            ImGui.TableNextColumn();
            UiSharedService.ColorText(wardrobe.Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
            ImGui.TableNextRow();
            DrawPermissionRowString("Assigner", wardrobe.PadlockAssigner);
        }
    }

    // Probably better to make these light cursed item structs or something idk.
    private void DrawCursedLoot(string uid, List<Guid> cursedItems)
    {
        // draw out the list of cursed item GUID's
        using var subnodeCursedItems = ImRaii.TreeNode("Active Cursed Items");
        if (!subnodeCursedItems) return;

        foreach (var item in cursedItems)
            ImGui.TextUnformatted(item.ToString());

    }

    private void DrawAlias(string uid, CharaAliasData alias)
    {
        using var nodeMain = ImRaii.TreeNode("Alias Data");
        if (!nodeMain) return;

        ImGui.Text("Has Name Stored: ");
        _uiShared.BooleanToColoredIcon(alias.HasNameStored, true);

        ImGui.Text("Listener Name: '" + alias.ListenerName + "'");
        ImGui.Text("Extracted Listener Name: '" + alias.ExtractedListenerName + "'");

        using (ImRaii.Table("##debug-aliasdata-" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Alias Input");
            ImGui.TableSetupColumn("Alias Output");
            ImGui.TableHeadersRow();
            foreach (var aliasData in alias.AliasList)
            {
                ImGuiUtil.DrawTableColumn(aliasData.InputCommand);
                ImGuiUtil.DrawTableColumn("(Output sections being worked on atm?)");
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawToybox(string uid, CharaToyboxData toybox)
    {
        using var nodeMain = ImRaii.TreeNode("Toybox Data");
        if (!nodeMain) return;

        ImGui.Text("Active Pattern ID: " + toybox.ActivePattern);
        // alarm sub-node
        using (var subnodeAlarm = ImRaii.TreeNode("Active Alarms"))
        {
            if (subnodeAlarm)
            {
                foreach (var alarm in toybox.ActiveAlarms)
                    ImGui.TextUnformatted(alarm.ToString());
            }
        }

        // active triggers sub-node
        using (var subnodeTriggers = ImRaii.TreeNode("Active Triggers"))
        {
            if (subnodeTriggers)
            {
                foreach (var trigger in toybox.ActiveTriggers)
                    ImGui.TextUnformatted(trigger.ToString());
            }
        }
    }

    private void DrawLightStorage(string uid, CharaLightStorageData lightStorage)
    {
        using var nodeMain = ImRaii.TreeNode("Light Storage Data");
        if (!nodeMain) return;

        // lightStorage subnode gags.
        using (var subnodeGagGlamours = ImRaii.TreeNode("Gags with Glamour's"))
        {
            if (subnodeGagGlamours)
            {
                using (ImRaii.Table("##debug-lightstorage-gag-glamours" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Gag Type");
                    ImGui.TableSetupColumn("Equip Slot");
                    ImGui.TableHeadersRow();

                    foreach (var gag in lightStorage.GagItems)
                    {
                        ImGuiUtil.DrawTableColumn(gag.Key.ToString());
                        ImGuiUtil.DrawTableColumn(gag.Value.Slot.ToString());
                        ImGui.TableNextRow();
                    }
                }
            }
        }

        // lightStorage subnode restraints.
        using (var subnodeRestraints = ImRaii.TreeNode("Restraints"))
        {
            if (subnodeRestraints)
            {
                using (ImRaii.Table("##debug-lightstorage-restraints" + uid, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Set ID");
                    ImGui.TableSetupColumn("Restraint Name");
                    ImGui.TableSetupColumn("Affected Slots");
                    ImGui.TableHeadersRow();

                    foreach (var set in lightStorage.Restraints)
                    {
                        ImGuiUtil.DrawTableColumn(set.Id.ToString());
                        ImGuiUtil.DrawTableColumn(set.Label);
                        ImGuiUtil.DrawTableColumn(string.Join(",", set.AffectedSlots.Select(x => ((EquipSlot)x.Slot).ToString())));
                        ImGui.TableNextRow();
                    }
                }
            }
        }

        // lightStorage subnode cursed items.
        using (var subnodeCursedItems = ImRaii.TreeNode("Cursed Items"))
        {
            if (subnodeCursedItems)
            {
                using (ImRaii.Table("##debug-lightstorage-cursed-items" + uid, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Cursed Item ID");
                    ImGui.TableSetupColumn("Cursed Item Name");
                    ImGui.TableSetupColumn("RestrictionType?");
                    ImGui.TableHeadersRow();

                    foreach (var item in lightStorage.CursedItems)
                    {
                        ImGuiUtil.DrawTableColumn(item.Id.ToString());
                        ImGuiUtil.DrawTableColumn(item.Label);
                        ImGuiUtil.DrawTableColumn(item.Type.ToString());
                        ImGui.TableNextRow();
                    }
                }
            }
        }


        // lightStorage subnode patterns.
        using (var subnodePatterns = ImRaii.TreeNode("Patterns"))
        {
            if (subnodePatterns)
            {
                using (ImRaii.Table("##debug-lightstorage-patterns" + uid, 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Pattern ID");
                    ImGui.TableSetupColumn("Name");
                    ImGui.TableSetupColumn("Duration");
                    ImGui.TableSetupColumn("Loops?");
                    ImGui.TableHeadersRow();

                    foreach (var pattern in lightStorage.Patterns)
                    {
                        ImGuiUtil.DrawTableColumn(pattern.Id.ToString());
                        ImGuiUtil.DrawTableColumn(pattern.Label);
                        ImGuiUtil.DrawTableColumn(pattern.Duration.ToString());
                        ImGuiUtil.DrawTableColumn(pattern.Loops.ToString());
                        ImGui.TableNextRow();
                    }
                }
            }
        }

        // lightStorage subnode alarms.
        using (var subnodeAlarms = ImRaii.TreeNode("Alarms"))
        {
            if (subnodeAlarms)
            {
                using (ImRaii.Table("##debug-lightstorage-alarms" + uid, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Alarm ID");
                    ImGui.TableSetupColumn("Name");
                    ImGui.TableSetupColumn("Alarm Time");
                    ImGui.TableHeadersRow();

                    foreach (var alarm in lightStorage.Alarms)
                    {
                        ImGuiUtil.DrawTableColumn(alarm.Id.ToString());
                        ImGuiUtil.DrawTableColumn(alarm.Label);
                        ImGuiUtil.DrawTableColumn(alarm.SetTimeUTC.ToLocalTime().TimeOfDay.ToString());
                        ImGui.TableNextRow();
                    }
                }
            }
        }

        // lightStorage subnode triggers.
        using (var subnodeTriggers = ImRaii.TreeNode("Triggers"))
        {
            if (!subnodeTriggers) return;

            using (ImRaii.Table("##debug-lightstorage-triggers" + uid, 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Trigger ID");
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Trigger Type");
                ImGui.TableSetupColumn("Action Kind");
                ImGui.TableHeadersRow();

                foreach (var trigger in lightStorage.Triggers)
                {
                    ImGuiUtil.DrawTableColumn(trigger.Id.ToString());
                    ImGuiUtil.DrawTableColumn(trigger.Label);
                    ImGuiUtil.DrawTableColumn(trigger.Type.ToString());
                    ImGuiUtil.DrawTableColumn(trigger.ActionOnTrigger.ToString());
                    ImGui.TableNextRow();
                }
            }
        }
    }
}
