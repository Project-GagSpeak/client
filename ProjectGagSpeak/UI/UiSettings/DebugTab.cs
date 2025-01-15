using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Common.Lua;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using System.Numerics;

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

    private readonly ClientData _playerData;
    private readonly ClientDataChanges _playerDataChanges;
    private readonly PairManager _pairManager;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly UiSharedService _uiShared;
    public DebugTab(PairManager pairManager, ClientData playerData, ClientDataChanges playerDataChanges,
        ClientConfigurationManager clientConfigs, UiSharedService uiShared)
    {
        _playerData = playerData;
        _playerDataChanges = playerDataChanges;
        _pairManager = pairManager;
        _clientConfigs = clientConfigs;
        _uiShared = uiShared;
    }

    public void DrawDebugMain()
    {
        _uiShared.GagspeakBigText("Debug Configuration");

        // display the combo box for setting the log level we wish to have for our plugin
        _uiShared.DrawCombo("Log Level", 400, Enum.GetValues<LogLevel>(), (level) => level.ToString(), (level) =>
        {
            _clientConfigs.GagspeakConfig.LogLevel = level;
            _clientConfigs.Save();
        }, _clientConfigs.GagspeakConfig.LogLevel);

        var logFilters = _clientConfigs.GagspeakConfig.LoggerFilters;

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
        bool isFirstSection = true;

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
                    for (int i = 0; i < checkboxes.Length; i++)
                    {
                        ImGui.TableNextColumn();

                        bool isEnabled = _clientConfigs.GagspeakConfig.LoggerFilters.Contains(checkboxes[i]);

                        if (ImGui.Checkbox(checkboxes[i].ToName(), ref isEnabled))
                        {
                            if (isEnabled)
                            {
                                _clientConfigs.GagspeakConfig.LoggerFilters.Add(checkboxes[i]);
                                LoggerFilter.AddAllowedCategory(checkboxes[i]);
                            }
                            else
                            {
                                _clientConfigs.GagspeakConfig.LoggerFilters.Remove(checkboxes[i]);
                                LoggerFilter.RemoveAllowedCategory(checkboxes[i]);
                            }
                            _clientConfigs.Save();
                        }
                    }

                    // Add "All On" and "All Off" buttons for the first section
                    if (isFirstSection)
                    {
                        ImGui.TableNextColumn();
                        if (ImGui.Button("All On"))
                        {
                            _clientConfigs.GagspeakConfig.LoggerFilters = LoggerFilter.GetAllRecommendedFilters();
                            _clientConfigs.Save();
                            LoggerFilter.AddAllowedCategories(_clientConfigs.GagspeakConfig.LoggerFilters);
                        }
                        ImUtf8.SameLineInner();
                        if (ImGui.Button("All Off"))
                        {
                            _clientConfigs.GagspeakConfig.LoggerFilters.Clear();
                            _clientConfigs.GagspeakConfig.LoggerFilters.Add(LoggerType.None);
                            _clientConfigs.Save();
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
        if (!_clientConfigs.GagspeakConfig.LoggerFilters.Contains(LoggerType.None))
        {
            _clientConfigs.GagspeakConfig.LoggerFilters.Add(LoggerType.None);
            LoggerFilter.AddAllowedCategory(LoggerType.None);
        }
    }


    private void DrawPlayerCharacterDebug()
    {
        DrawGlobalPermissions("Player", _playerData.GlobalPerms ?? new UserGlobalPermissions());
        DrawAppearance("Player", _playerData.AppearanceData ?? new CharaAppearanceData());
        DrawWardrobe("Player", _clientConfigs.CompileWardrobeToAPI());
        // draw an enclosed tree node here for the alias data. Inside of this, we will have a different tree node for each of the keys in our alias storage,.
        using (ImRaii.TreeNode("Alias Data"))
        {
            foreach (var alias in _clientConfigs.AliasConfig.AliasStorage)
            {
                using (ImRaii.TreeNode("Your Alias List for: " + alias.Key))
                {
                    DrawAlias(alias.Key, alias.Value.ToAliasData());
                }
            }
        }
        DrawToybox("Player", _clientConfigs.CompileToyboxToAPI());
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
            DrawAppearance(pair.UserData.UID, pair.LastAppearanceData ?? new CharaAppearanceData());
            DrawWardrobe(pair.UserData.UID, pair.LastWardrobeData ?? new CharaWardrobeData());
            DrawAlias(pair.UserData.UID, pair.LastAliasData ?? new CharaAliasData());
            DrawToybox(pair.UserData.UID, pair.LastToyboxData ?? new CharaToyboxData());
            DrawLightStorage(pair.UserData.UID, pair.LastLightStorage ?? new CharaStorageData());
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

        DrawPermissionRowBool("Live Chat Garbler", perms.LiveChatGarblerActive);
        DrawPermissionRowBool("Live Chat Garbler Locked", perms.LiveChatGarblerLocked);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Gag Glamours", perms.ItemAutoEquip);
        DrawPermissionRowBool("Wardrobe Active", perms.WardrobeEnabled);
        DrawPermissionRowBool("Restraint Glamours", perms.RestraintSetAutoEquip);
        DrawPermissionRowBool("Moodles Active", perms.MoodlesEnabled);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Puppeteer Active", perms.PuppeteerEnabled);
        DrawPermissionRowString("Global Trigger Phrase", perms.GlobalTriggerPhrase);
        DrawPermissionRowBool("Allow Sit", perms.GlobalAllowSitRequests);
        DrawPermissionRowBool("Allow Motion", perms.GlobalAllowMotionRequests);
        DrawPermissionRowBool("Allow Alias", perms.GlobalAllowAliasRequests);
        DrawPermissionRowBool("Allow All", perms.GlobalAllowAllRequests);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Toybox Active", perms.ToyboxEnabled);
        DrawPermissionRowBool("Lock Toybox UI", perms.LockToyboxUI);
        DrawPermissionRowBool("Sex Toy Active", perms.ToyIsActive);
        DrawPermissionRowBool("Spatial Vibrator Audio", perms.SpatialVibratorAudio);
        ImGui.TableNextRow();
        DrawPermissionRowString("Forced Follow", perms.ForcedFollow);
        DrawPermissionRowString("Forced Emote State", perms.ForcedEmoteState);
        DrawPermissionRowString("Forced Stay", perms.ForcedStay);
        DrawPermissionRowString("Forced Blindfold", perms.ForcedBlindfold);
        DrawPermissionRowString("Chat Boxes Hidden", perms.ChatBoxesHidden);
        DrawPermissionRowString("Chat Input Hiddeen", perms.ChatInputHidden);
        DrawPermissionRowString("Chat Input Blocked", perms.ChatInputBlocked);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Shock Collar Active", perms.ShockCollarIsActive);
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
        DrawPermissionRowString("Max Restraint Lock Time", perms.MaxAllowedRestraintTime.ToString());
        DrawPermissionRowBool("Unlock Restraint Sets", perms.UnlockRestraintSets);
        DrawPermissionRowBool("Remove Restraint Sets", perms.RemoveRestraintSets);
        ImGui.TableNextRow();
        DrawPermissionRowString("Trigger Phrase", perms.TriggerPhrase);
        DrawPermissionRowString("Start Char", perms.StartChar.ToString());
        DrawPermissionRowString("End Char", perms.EndChar.ToString());
        DrawPermissionRowBool("Allow Sit Requests", perms.SitRequests);
        DrawPermissionRowBool("Allow Motion Requests", perms.MotionRequests);
        DrawPermissionRowBool("Allow Alias Requests", perms.AliasRequests);
        DrawPermissionRowBool("Allow All Requests", perms.AllRequests);
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
        DrawPermissionRowBool("Can Use Vibe Remote", perms.CanUseVibeRemote);
        DrawPermissionRowBool("Can Execute Patterns", perms.CanExecutePatterns);
        DrawPermissionRowBool("Can Stop Patterns", perms.CanStopPatterns);
        DrawPermissionRowBool("Can Toggle Alarms", perms.CanToggleAlarms);
        DrawPermissionRowBool("Can Send Alarms", perms.CanSendAlarms);
        DrawPermissionRowBool("Can Toggle Triggers", perms.CanToggleTriggers);
        ImGui.TableNextRow();
        DrawPermissionRowBool("In Hardcore Mode", perms.InHardcore);
        DrawPermissionRowBool("Devotional States For Pair", perms.DevotionalStatesForPair);
        DrawPermissionRowBool("Allow Forced Follow", perms.AllowForcedFollow);
        DrawPermissionRowBool("Allow Forced Sit", perms.AllowForcedSit);
        DrawPermissionRowBool("Allow Forced Emote", perms.AllowForcedEmote);
        DrawPermissionRowBool("Allow Forced To Stay", perms.AllowForcedToStay);
        DrawPermissionRowBool("Allow Blindfold", perms.AllowBlindfold);
        DrawPermissionRowBool("Allow Hiding Chat Boxes", perms.AllowHidingChatBoxes);
        DrawPermissionRowBool("Allow Hiding Chat Input", perms.AllowHidingChatInput);
        DrawPermissionRowBool("Allow Chat Input Blocking", perms.AllowChatInputBlocking);
        ImGui.TableNextRow();
        DrawPermissionRowString("Shock Collar Share Code", perms.ShockCollarShareCode);
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
        DrawPermissionRowBool("Live Chat Garbler Active", perms.LiveChatGarblerActiveAllowed);
        DrawPermissionRowBool("Live Chat Garbler Locked", perms.LiveChatGarblerLockedAllowed);
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
        DrawPermissionRowBool("Auto Equip Items", perms.ItemAutoEquipAllowed);
        DrawPermissionRowBool("Auto Equip Restraints", perms.RestraintSetAutoEquipAllowed);
        DrawPermissionRowBool("Apply Restraint Sets", perms.ApplyRestraintSetsAllowed);
        DrawPermissionRowBool("Lock Restraint Sets", perms.LockRestraintSetsAllowed);
        DrawPermissionRowBool("Max Restraint Lock Time", perms.MaxAllowedRestraintTimeAllowed);
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
        DrawPermissionRowBool("Allow Positive Moodles", perms.AllowPositiveStatusTypesAllowed);
        DrawPermissionRowBool("Allow Negative Moodles", perms.AllowNegativeStatusTypesAllowed);
        DrawPermissionRowBool("Allow Special Moodles", perms.AllowSpecialStatusTypesAllowed);
        DrawPermissionRowBool("Apply Own Moodles", perms.PairCanApplyOwnMoodlesToYouAllowed);
        DrawPermissionRowBool("Apply Your Moodles", perms.PairCanApplyYourMoodlesToYouAllowed);
        DrawPermissionRowBool("Max Moodle Time", perms.MaxMoodleTimeAllowed);
        DrawPermissionRowBool("Allow Permanent Moodles", perms.AllowPermanentMoodlesAllowed);
        DrawPermissionRowBool("Allow Removing Moodles", perms.AllowRemovingMoodlesAllowed);
        ImGui.TableNextRow();

        // Toybox Permissions
        DrawPermissionRowBool("Toybox Enabled", perms.ToyboxEnabledAllowed);
        DrawPermissionRowBool("Lock Toybox UI", perms.LockToyboxUIAllowed);
        DrawPermissionRowBool("Spatial Vibrator Audio", perms.SpatialVibratorAudioAllowed);
        DrawPermissionRowBool("Can Toggle Toy State", perms.CanToggleToyStateAllowed);
        DrawPermissionRowBool("Can Use Vibe Remote", perms.CanUseVibeRemoteAllowed);
        DrawPermissionRowBool("Can Execute Patterns", perms.CanExecutePatternsAllowed);
        DrawPermissionRowBool("Can Stop Patterns", perms.CanStopPatternsAllowed);
        DrawPermissionRowBool("Can Toggle Alarms", perms.CanToggleAlarmsAllowed);
        DrawPermissionRowBool("Can Send Alarms", perms.CanSendAlarmsAllowed);
        DrawPermissionRowBool("Can Toggle Triggers", perms.CanToggleTriggersAllowed);
    }

    private void DrawAppearance(string uid, CharaAppearanceData appearance)
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
            for (int i = 0; i < 3; i++)
                ImGuiUtil.DrawTableColumn(appearance.GagSlots[i].GagType);
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Padlock:");
            for (int i = 0; i < 3; i++)
                ImGuiUtil.DrawTableColumn(appearance.GagSlots[i].Padlock);
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Password:");
            for (int i = 0; i < 3; i++)
                ImGuiUtil.DrawTableColumn(appearance.GagSlots[i].Password);
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Time Remaining:");
            for (int i = 0; i < 3; i++)
            {
                ImGui.TableNextColumn();
                UiSharedService.ColorText(appearance.GagSlots[i].Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
            }
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Assigner:");
            for (int i = 0; i < 3; i++)
                ImGuiUtil.DrawTableColumn(appearance.GagSlots[i].Assigner);
        }
    }

    private void DrawWardrobe(string uid, CharaWardrobeData wardrobe)
    {
        using var nodeMain = ImRaii.TreeNode("Wardrobe Data");
        if (!nodeMain) return;

        using (ImRaii.Table("##debug-wardrobe" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            DrawPermissionRowString("Active Set ID", wardrobe.ActiveSetId.ToString());
            DrawPermissionRowString("Active Set Enabled By", wardrobe.ActiveSetEnabledBy);
            DrawPermissionRowString("Padlock", wardrobe.Padlock);
            DrawPermissionRowString("Password", wardrobe.Password);
            ImGuiUtil.DrawTableColumn("Expiration Time");
            ImGui.TableNextColumn();
            UiSharedService.ColorText(wardrobe.Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
            ImGui.TableNextRow();
            DrawPermissionRowString("Assigner", wardrobe.Assigner);
        }

        // draw out the list of cursed item GUID's
        using var subnodeCursedItems = ImRaii.TreeNode("Active Cursed Items");
        if(!subnodeCursedItems) return;
        
        foreach (var item in wardrobe.ActiveCursedItems)
            ImGui.TextUnformatted(item.ToString());
    }

    private void DrawAlias(string uid, CharaAliasData alias)
    {
        using var nodeMain = ImRaii.TreeNode("Alias Data");
        if (!nodeMain) return;

        ImGui.Text("Has Name Stored: ");
        _uiShared.BooleanToColoredIcon(alias.HasNameStored, true);

        ImGui.Text("Listener Name: '" + alias.ListenerName + "'");
        ImGui.Text("Extracted Listener Name: '" + alias.ExtractedListernerName + "'");

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

        ImGui.Text("Active Pattern ID: " + toybox.ActivePatternId);
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

    private void DrawLightStorage(string uid, CharaStorageData lightStorage)
    {
        using var nodeMain = ImRaii.TreeNode("Light Storage Data");
        if (!nodeMain) return;

        ImGui.Text("Blindfold Item Slot: " + lightStorage.BlindfoldItem.Slot);

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
                        ImGuiUtil.DrawTableColumn(set.Identifier.ToString());
                        ImGuiUtil.DrawTableColumn(set.Name);
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
                    ImGui.TableSetupColumn("Is Gag?");
                    ImGui.TableHeadersRow();

                    foreach (var item in lightStorage.CursedItems)
                    {
                        ImGuiUtil.DrawTableColumn(item.Identifier.ToString());
                        ImGuiUtil.DrawTableColumn(item.Name);
                        ImGuiUtil.DrawTableColumn(item.IsGag.ToString());
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
                        ImGuiUtil.DrawTableColumn(pattern.Identifier.ToString());
                        ImGuiUtil.DrawTableColumn(pattern.Name);
                        ImGuiUtil.DrawTableColumn(pattern.Duration.ToString());
                        ImGuiUtil.DrawTableColumn(pattern.ShouldLoop.ToString());
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
                        ImGuiUtil.DrawTableColumn(alarm.Identifier.ToString());
                        ImGuiUtil.DrawTableColumn(alarm.Name);
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
                    ImGuiUtil.DrawTableColumn(trigger.Identifier.ToString());
                    ImGuiUtil.DrawTableColumn(trigger.Name);
                    ImGuiUtil.DrawTableColumn(trigger.Type.ToString());
                    ImGuiUtil.DrawTableColumn(trigger.ActionOnTrigger.ToString());
                    ImGui.TableNextRow();
                }
            }
        }
    }
}
