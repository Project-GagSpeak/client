using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.Handlers;
using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.MufflerCore;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using ImGuiNET;
using OtterGui;
using OtterGui.Extensions;
using OtterGui.Text;
using System.Buffers;
using System.Collections.Immutable;

namespace GagSpeak.Gui;

public class DebugPersonalDataUI : WindowMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly KinksterManager _pairs;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;

    private KinksterSearchList _searchList;
    public DebugPersonalDataUI(
        ILogger<DebugPersonalDataUI> logger,
        GagspeakMediator mediator,
        MainConfig config,
        MoodleDrawer moodleDrawer,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        IdDisplayHandler nameDisplay,
        KinksterManager pairs,
        CosmeticService icon)
        : base(logger, mediator, "Kinkster Data Debugger")
    {
        _pairs = pairs;
        _config = config;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;

        _searchList = new KinksterSearchList(config, nameDisplay, pairs, icon);
        // Ensure the list updates properly.
        Mediator.Subscribe<RefreshUiMessage>(this, _ => UpdateList());

        IsOpen = true;
        this.SetBoundaries(new Vector2(625, 400), ImGui.GetIO().DisplaySize);
    }

    protected override void PreDrawInternal()
    { }

    protected override void PostDrawInternal()
    { }

    protected ImmutableList<Kinkster> _immutablePairs = ImmutableList<Kinkster>.Empty;
    protected string _searchValue = string.Empty;

    public void UpdateList()
    {
        // Get direct pairs, then filter them.
        var filteredPairs = _pairs.DirectPairs
            .Where(p =>
            {
                if (_searchValue.IsNullOrEmpty())
                    return true;
                // Match for Alias, Uid, Nick, or PlayerName.
                return p.UserData.AliasOrUID.Contains(_searchValue, StringComparison.OrdinalIgnoreCase)
                    || (p.GetNickname()?.Contains(_searchValue, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (p.PlayerName?.Contains(_searchValue, StringComparison.OrdinalIgnoreCase) ?? false);
            });

        // Take the remaining filtered list, and sort it.
        _immutablePairs = filteredPairs
            .OrderByDescending(u => u.IsVisible)
            .ThenByDescending(u => u.IsOnline)
            .ThenBy(pair => !pair.PlayerName.IsNullOrEmpty()
                ? (_config.Current.PreferNicknamesOverNames ? pair.GetNickAliasOrUid() : pair.PlayerName)
                : pair.GetNickAliasOrUid(), StringComparer.OrdinalIgnoreCase)
            .ToImmutableList();
    }

    protected override void DrawInternal()
    {
        if (ImGui.CollapsingHeader("Client Player Data"))
            DrawPlayerCharacterDebug();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Pair Data"))
        {
            ImGui.Text($"Total Pairs: {_pairs.DirectPairs.Count}");
            ImGui.Text($"Visible Users: {_pairs.GetVisibleUserCount()}");

            // The search.
            if (FancySearchBar.Draw("##PairDebugSearch", ImGui.GetContentRegionAvail().X, "Search for Pair..", ref _searchValue, 40))
                UpdateList();

            // Seperator, then the results.
            ImGui.Separator();
            var width = ImGui.GetContentRegionAvail().X;
            foreach (var pair in _immutablePairs)
                DrawPairData(pair, width);
        }
    }

    private void DrawPairData(Kinkster pair, float width)
    {
        using var node = ImRaii.TreeNode(pair.GetNickAliasOrUid() + "'s Pair Info");
        if (!node) return;

        DrawPairPerms("Own Pair Perms for " + pair.UserData.UID, pair.OwnPerms);
        DrawPairPermAccess("Own Pair Perm Access for " + pair.UserData.UID, pair.OwnPermAccess);
        DrawGlobalPermissions(pair.UserData.UID + "'s Global Perms", pair.PairGlobals);
        DrawPairPerms(pair.UserData.UID + "'s Pair Perms for you.", pair.PairPerms);
        DrawPairPermAccess(pair.UserData.UID + "'s Pair Perm Access for you", pair.PairPermAccess);
        DrawPairIpcData(pair.UserData.UID, pair.LastIpcData);
        DrawGagData(pair.UserData.UID, pair.ActiveGags);
        DrawPairRestrictions(pair.UserData.UID, pair);
        DrawRestraint(pair.UserData.UID, pair);
        DrawAlias(pair.UserData.UID, "Global", pair.LastGlobalAliasData);
        DrawAlias(pair.UserData.UID, "Unique", pair.LastPairAliasData.Storage);
        DrawToybox(pair.UserData.UID, pair.ActivePattern, pair.ActiveAlarms, pair.ActiveTriggers);
        DrawKinksterCache(pair);
        ImGui.Separator();
    }


    private void DrawPlayerCharacterDebug()
    {
        DrawGlobalPermissions("Player", OwnGlobals.Perms ?? new GlobalPerms());
        DrawGagData("Player", _gags.ServerGagData ?? new CharaActiveGags());
        DrawRestrictions("Player", _restrictions.ServerRestrictionData ?? new CharaActiveRestrictions());
        DrawRestraint("Player", _restraints.ServerData ?? new CharaActiveRestraint());
    }

    private void DrawPermissionRowBool(string name, bool value)
    {
        ImGuiUtil.DrawTableColumn(name);
        ImGui.TableNextColumn();
        CkGui.IconText(value ? FAI.Check : FAI.Times, value ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
        ImGui.TableNextRow();
    }

    private void DrawPermissionRowString(string name, string value)
    {
        ImGuiUtil.DrawTableColumn(name);
        ImGui.TableNextColumn();
        ImGui.Text(value);
        ImGui.TableNextRow();
    }

    private void DrawGlobalPermissions(string uid, IReadOnlyGlobalPerms perms)
    {
        using var nodeMain = ImRaii.TreeNode(uid + " Global Perms");
        if (!nodeMain) return;

        try
        {
            using var table = ImRaii.Table("##debug-global" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
            if (!table) return;
            ImGui.TableSetupColumn("Permission");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            DrawPermissionRowString("Allowed Garble Channels", perms.AllowedGarblerChannels.ToString());
            DrawPermissionRowBool("Live Chat Garbler", perms.ChatGarblerActive);
            DrawPermissionRowBool("Live Chat Garbler Locked", perms.ChatGarblerLocked);
            DrawPermissionRowBool("Gagged Nameplate", perms.GaggedNameplate);

            ImGui.TableNextRow();
            DrawPermissionRowBool("Wardrobe Active", perms.WardrobeEnabled);
            DrawPermissionRowBool("Gag Visuals", perms.GagVisuals);
            DrawPermissionRowBool("Restriction Visuals", perms.RestrictionVisuals);
            DrawPermissionRowBool("Restraint Visuals", perms.RestraintSetVisuals);

            ImGui.TableNextRow();
            DrawPermissionRowBool("Puppeteer Active", perms.PuppeteerEnabled);
            DrawPermissionRowString("Global Trigger Phrase", perms.TriggerPhrase);
            DrawPermissionRowBool("Allow Sit Requests", perms.PuppetPerms.HasAny(PuppetPerms.Sit));
            DrawPermissionRowBool("Allow Motion Requests", perms.PuppetPerms.HasAny(PuppetPerms.Emotes));
            DrawPermissionRowBool("Allow Alias Requests", perms.PuppetPerms.HasAny(PuppetPerms.Alias));
            DrawPermissionRowBool("Allow All Requests", perms.PuppetPerms.HasAny(PuppetPerms.All));

            ImGui.TableNextRow();
            DrawPermissionRowBool("Toybox Active", perms.ToyboxEnabled);
            DrawPermissionRowBool("Toys Are Interactable", perms.ToysAreInteractable);
            DrawPermissionRowBool("In VibeRoom", perms.InVibeRoom);
            DrawPermissionRowBool("Spatial Vibrator Audio", perms.SpatialAudio);

            ImGui.TableNextRow();
            DrawPermissionRowString("ActiveHypnosisEffect", perms.HypnosisCustomEffect);

            ImGui.TableNextRow();
            DrawPermissionRowString("Forced Follow", perms.LockedFollowing);
            DrawPermissionRowString("Forced Emote State", perms.LockedEmoteState);
            DrawPermissionRowString("Forced Stay", perms.IndoorConfinement);
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
        catch (Bagagwa e)
        {
            _logger.LogError($"Error while drawing global permissions for {uid}: {e.Message}");
        }
    }

    private void DrawPairPerms(string uid, PairPerms perms)
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
        DrawPermissionRowBool("Apply Restrictions", perms.ApplyRestrictions);
        DrawPermissionRowBool("Lock Restrictions", perms.LockRestrictions);
        DrawPermissionRowString("Max Restriction Lock Time", perms.MaxRestrictionTime.ToString());
        DrawPermissionRowBool("Unlock Restrictions", perms.UnlockRestrictions);
        DrawPermissionRowBool("Remove Restrictions", perms.RemoveRestrictions);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Apply Restraint Sets", perms.ApplyRestraintSets);
        DrawPermissionRowBool("Apply Restraint Layers", perms.ApplyLayers);
        DrawPermissionRowBool("Apply Layers while locked", perms.ApplyLayersWhileLocked);
        DrawPermissionRowBool("Lock Restraint Sets", perms.LockRestraintSets);
        DrawPermissionRowString("Max Restraint Set Lock Time", perms.MaxRestraintTime.ToString());
        DrawPermissionRowBool("Unlock Restraint Sets", perms.UnlockRestraintSets);
        DrawPermissionRowBool("Remove Restraint Layers", perms.RemoveLayers);
        DrawPermissionRowBool("Remove Layers while locked", perms.RemoveLayersWhileLocked);
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
        DrawPermissionRowBool("Allow Positive Moodles", perms.MoodlePerms.HasFlag(MoodlePerms.PositiveStatusTypes));
        DrawPermissionRowBool("Allow Negative Moodles", perms.MoodlePerms.HasFlag(MoodlePerms.NegativeStatusTypes));
        DrawPermissionRowBool("Allow Special Moodles", perms.MoodlePerms.HasFlag(MoodlePerms.SpecialStatusTypes));
        DrawPermissionRowBool("Apply Own Moodles", perms.MoodlePerms.HasFlag(MoodlePerms.PairCanApplyTheirMoodlesToYou));
        DrawPermissionRowBool("Apply Your Moodles", perms.MoodlePerms.HasFlag(MoodlePerms.PairCanApplyYourMoodlesToYou));
        DrawPermissionRowString("Max Moodle Time", perms.MaxMoodleTime.ToString());
        DrawPermissionRowBool("Allow Permanent Moodles", perms.MoodlePerms.HasFlag(MoodlePerms.PositiveStatusTypes));
        DrawPermissionRowBool("Allow Removing Moodles", perms.MoodlePerms.HasFlag(MoodlePerms.PositiveStatusTypes));
        ImGui.TableNextRow();
        DrawPermissionRowBool("Can Execute Patterns", perms.ExecutePatterns);
        DrawPermissionRowBool("Can Stop Patterns", perms.StopPatterns);
        DrawPermissionRowBool("Can Toggle Alarms", perms.ToggleAlarms);
        DrawPermissionRowBool("Can Send Alarms", perms.ToggleAlarms);
        DrawPermissionRowBool("Can Toggle Triggers", perms.ToggleTriggers);
        ImGui.TableNextRow();
        DrawPermissionRowBool("Allow Hypnosis Effect Sending", perms.HypnoEffectSending);
        ImGui.TableNextRow();
        DrawPermissionRowBool("In Hardcore Mode", perms.InHardcore);
        DrawPermissionRowBool("Devotional States For Pair", perms.PairLockedStates);
        DrawPermissionRowBool("Allow Forced Follow", perms.AllowLockedFollowing);
        DrawPermissionRowBool("Allow Forced Sit", perms.AllowLockedSitting);
        DrawPermissionRowBool("Allow Forced Emote", perms.AllowLockedEmoting);
        DrawPermissionRowBool("Allow Indoor Confinement", perms.AllowIndoorConfinement);
        DrawPermissionRowBool("Allow Imprisonment", perms.AllowImprisonment);
        DrawPermissionRowBool("Allow GarbleChannelEditing", perms.AllowGarbleChannelEditing);
        DrawPermissionRowBool("Allow Hiding Chat Boxes", perms.AllowHidingChatBoxes);
        DrawPermissionRowBool("Allow Hiding Chat Input", perms.AllowHidingChatInput);
        DrawPermissionRowBool("Allow Chat Input Blocking", perms.AllowChatInputBlocking);
        DrawPermissionRowBool("Allow Hypnosis Image Sending", perms.AllowHypnoImageSending);
        ImGui.TableNextRow();
        DrawPermissionRowString("Shock Collar Share Code", perms.PiShockShareCode);
        DrawPermissionRowBool("Allow Shocks", perms.AllowShocks);
        DrawPermissionRowBool("Allow Vibrations", perms.AllowVibrations);
        DrawPermissionRowBool("Allow Beeps", perms.AllowBeeps);
        DrawPermissionRowString("Max Intensity", perms.MaxIntensity.ToString());
        DrawPermissionRowString("Max Duration", perms.MaxDuration.ToString());
    }

    private void DrawPairPermAccess(string uid, PairPermAccess perms)
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
        DrawPermissionRowBool("Gag Visuals Active", perms.GagVisualsAllowed);
        DrawPermissionRowBool("Allows Applying Gags", perms.ApplyGagsAllowed);
        DrawPermissionRowBool("Allows Locking Gags", perms.LockGagsAllowed);
        DrawPermissionRowBool("Max Gag Time", perms.MaxGagTimeAllowed);
        DrawPermissionRowBool("Allows Unlocking Gags", perms.UnlockGagsAllowed);
        DrawPermissionRowBool("Allows Removing Gags", perms.RemoveGagsAllowed);
        ImGui.TableNextRow();

        DrawPermissionRowBool("Wardrobe Enabled", perms.WardrobeEnabledAllowed);
        // Restriction permissions
        DrawPermissionRowBool("Restriction Visuals Active", perms.RestrictionVisualsAllowed);
        DrawPermissionRowBool("Allows Applying Restrictions", perms.ApplyRestrictionsAllowed);
        DrawPermissionRowBool("Allows Locking Restrictions", perms.LockRestrictionsAllowed);
        DrawPermissionRowBool("Max Restriction Lock Time", perms.MaxRestrictionTimeAllowed);
        DrawPermissionRowBool("Allows Unlocking Restrictions", perms.UnlockRestrictionsAllowed);
        DrawPermissionRowBool("Allows Removing Restrictions", perms.RemoveRestrictionsAllowed);
        ImGui.TableNextRow();

        // Restraint permissions
        DrawPermissionRowBool("Restraint Visuals Active", perms.RestraintSetVisualsAllowed);
        DrawPermissionRowBool("Allows Applying Restraints", perms.ApplyRestraintSetsAllowed);
        DrawPermissionRowBool("Allows Locking Restraints", perms.LockRestraintSetsAllowed);
        DrawPermissionRowBool("Max Restraint Lock Time", perms.MaxRestraintTimeAllowed);
        DrawPermissionRowBool("Allows Unlocking Restraints", perms.UnlockRestraintSetsAllowed);
        DrawPermissionRowBool("Allows Removing Restraints", perms.RemoveRestraintSetsAllowed);
        ImGui.TableNextRow();

        // Puppeteer Permissions
        DrawPermissionRowBool("Puppeteer Enabled", perms.PuppeteerEnabledAllowed);
        DrawPermissionRowBool("Allow Sit Requests", perms.PuppetPermsAllowed.HasAny(PuppetPerms.Sit));
        DrawPermissionRowBool("Allow Motion Requests", perms.PuppetPermsAllowed.HasAny(PuppetPerms.Emotes));
        DrawPermissionRowBool("Allow Alias Requests", perms.PuppetPermsAllowed.HasAny(PuppetPerms.Alias));
        DrawPermissionRowBool("Allow All Requests", perms.PuppetPermsAllowed.HasAny(PuppetPerms.All));
        ImGui.TableNextRow();

        // Moodle Permissions
        DrawPermissionRowBool("Moodles Enabled", perms.MoodlesEnabledAllowed);
        DrawPermissionRowBool("Allow Positive Moodles", perms.MoodlePermsAllowed.HasAny(MoodlePerms.PositiveStatusTypes));
        DrawPermissionRowBool("Allow Negative Moodles", perms.MoodlePermsAllowed.HasAny(MoodlePerms.NegativeStatusTypes));
        DrawPermissionRowBool("Allow Special Moodles", perms.MoodlePermsAllowed.HasAny(MoodlePerms.SpecialStatusTypes));
        DrawPermissionRowBool("Apply Own Moodles", perms.MoodlePermsAllowed.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou));
        DrawPermissionRowBool("Apply Your Moodles", perms.MoodlePermsAllowed.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou));
        DrawPermissionRowBool("Max Moodle Time", perms.MaxMoodleTimeAllowed);
        DrawPermissionRowBool("Allow Permanent Moodles", perms.MoodlePermsAllowed.HasAny(MoodlePerms.PermanentMoodles));
        DrawPermissionRowBool("Allow Removing Moodles", perms.MoodlePermsAllowed.HasAny(MoodlePerms.RemovingMoodles));
        ImGui.TableNextRow();

        // Toybox Permissions
        DrawPermissionRowBool("Spatial Vibrator Audio", perms.SpatialAudioAllowed);
        DrawPermissionRowBool("Can Execute Patterns", perms.ExecutePatternsAllowed);
        DrawPermissionRowBool("Can Stop Patterns", perms.StopPatternsAllowed);
        DrawPermissionRowBool("Can Toggle Alarms", perms.ToggleAlarmsAllowed);
        DrawPermissionRowBool("Can Toggle Triggers", perms.ToggleTriggersAllowed);
    }

    private void DrawPairIpcData(string uid, CharaIPCData ipcData)
    {
        using var nodeMain = ImRaii.TreeNode(uid + " IPC Data");
        if (!nodeMain) return;

        ImUtf8.TextFrameAligned($"Active Moodles: {ipcData.DataInfo.Count()}");
        ImGui.SameLine();
        _moodleDrawer.DrawStatusInfos(ipcData.DataInfoList, MoodleDrawer.IconSizeFramed);

        ImGui.Text($"Total Moodles: {ipcData.StatusList.Count()}");
        ImGui.Text($"Total Presets: {ipcData.PresetList.Count()}");
    }

    private void DrawGagData(string uid, CharaActiveGags appearance)
    {
        using var nodeMain = ImRaii.TreeNode("Appearance Data");
        if (!nodeMain)
            return;

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
                CkGui.ColorText(appearance.GagSlots[i].Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
            }
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Assigner:");
            for (var i = 0; i < 3; i++)
                ImGuiUtil.DrawTableColumn(appearance.GagSlots[i].PadlockAssigner);
        }
    }

    private void DrawRestrictions(string uid, CharaActiveRestrictions restrictions)
    {
        using var nodeMain = ImRaii.TreeNode("Restrictions Data");
        if (!nodeMain) return;

        using (ImRaii.Table("##debug-restrictions" + uid, 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Enabler");
            ImGui.TableSetupColumn("Padlock");
            ImGui.TableSetupColumn("Password");
            ImGui.TableSetupColumn("TimeLeft");
            ImGui.TableSetupColumn("Assigner");
            ImGui.TableHeadersRow();

            foreach (var restriction in restrictions.Restrictions)
            {
                ImGuiUtil.DrawTableColumn(restriction.Identifier.ToString());
                ImGuiUtil.DrawTableColumn(restriction.Enabler);
                ImGuiUtil.DrawTableColumn(restriction.Padlock.ToName());
                ImGuiUtil.DrawTableColumn(restriction.Password);
                ImGui.TableNextColumn();
                CkGui.ColorText(restriction.Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
                ImGuiUtil.DrawTableColumn(restriction.PadlockAssigner);
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawPairRestrictions(string uid, Kinkster kinkster)
    {
        using var nodeMain = ImRaii.TreeNode($"Restrictions Data##{uid}");
        if (!nodeMain) return;

        using (ImRaii.Table("##debug-restrictions" + uid, 7, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Layer");
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Enabler");
            ImGui.TableSetupColumn("Padlock");
            ImGui.TableSetupColumn("Password");
            ImGui.TableSetupColumn("TimeLeft");
            ImGui.TableSetupColumn("Assigner");
            ImGui.TableHeadersRow();

            foreach (var (item, idx) in kinkster.ActiveRestrictions.Restrictions.WithIndex())
            {
                var name = kinkster.LightCache.Restrictions.TryGetValue(item.Identifier, out var i)
                    ? i.Label : item.Identifier == Guid.Empty ? "None" : item.Identifier.ToString();
                ImGuiUtil.DrawTableColumn($"{idx + 1}");
                ImGuiUtil.DrawTableColumn(name);
                ImGuiUtil.DrawTableColumn(item.Enabler);
                ImGuiUtil.DrawTableColumn(item.Padlock.ToName());
                ImGuiUtil.DrawTableColumn(item.Password);
                ImGui.TableNextColumn();
                CkGui.ColorText(item.Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
                ImGuiUtil.DrawTableColumn(item.PadlockAssigner);
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawRestraint(string uid, CharaActiveRestraint restraint)
    {
        using var nodeMain = ImRaii.TreeNode("Restraint Data");
        if (!nodeMain) return;

        using (ImRaii.Table("##debug-wardrobe" + uid, 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Enabler");
            ImGui.TableSetupColumn("Padlock");
            ImGui.TableSetupColumn("Password");
            ImGui.TableSetupColumn("TimeLeft");
            ImGui.TableSetupColumn("Assigner");
            ImGui.TableHeadersRow();
           
            ImGuiUtil.DrawFrameColumn(restraint.Identifier.ToString());
            ImGuiUtil.DrawFrameColumn(restraint.Enabler);
            ImGuiUtil.DrawFrameColumn(restraint.Padlock.ToName());
            ImGuiUtil.DrawFrameColumn(restraint.Password);
            ImGui.TableNextColumn();
            CkGui.ColorText(restraint.Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
            ImGuiUtil.DrawFrameColumn(restraint.PadlockAssigner);
            ImGui.TableNextRow();
        }
    }

    private void DrawRestraint(string uid, Kinkster kinkster)
    {
        using var nodeMain = ImRaii.TreeNode($"Restraint Data##{uid}");
        if (!nodeMain) return;

        var item = kinkster.ActiveRestraint;
        using (ImRaii.Table("##debug-restraint" + uid, 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            var name = kinkster.LightCache.Restraints.TryGetValue(item.Identifier, out var i)
                ? i.Label : item.Identifier == Guid.Empty ? "None" : item.Identifier.ToString();

            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Enabler");
            ImGui.TableSetupColumn("Padlock");
            ImGui.TableSetupColumn("Password");
            ImGui.TableSetupColumn("TimeLeft");
            ImGui.TableSetupColumn("Assigner");
            ImGui.TableHeadersRow();

            ImGuiUtil.DrawTableColumn(name);
            ImGuiUtil.DrawTableColumn(item.Enabler);
            ImGuiUtil.DrawTableColumn(item.Padlock.ToName());
            ImGuiUtil.DrawTableColumn(item.Password);
            ImGui.TableNextColumn();
            CkGui.ColorText(item.Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
            ImGuiUtil.DrawTableColumn(item.PadlockAssigner);
            ImGui.TableNextRow();
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

    private void DrawAlias(string uid, string label, AliasStorage storage)
    {
        using var nodeMain = ImRaii.TreeNode($"{label}'s Alias Data");
        if (!nodeMain) return;

        using (ImRaii.Table("##debug-aliasdata-" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Alias Input");
            ImGui.TableSetupColumn("Alias Output");
            ImGui.TableHeadersRow();
            foreach (var aliasData in storage.Items)
            {
                ImGuiUtil.DrawTableColumn(aliasData.InputCommand);
                ImGuiUtil.DrawTableColumn("(Output sections being worked on atm?)");
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawToybox(string uid, Guid activePattern, List<Guid> activeAlarms, List<Guid> activeTriggers)
    {
        ImGui.Text($"Active Pattern ID: {activePattern}");
        // alarm sub-node
        using (var subnodeAlarm = ImRaii.TreeNode("Active Alarms"))
        {
            if (subnodeAlarm)
            {
                foreach (var alarm in activeAlarms)
                    ImGui.TextUnformatted(alarm.ToString());
            }
        }

        // active triggers sub-node
        using (var subnodeTriggers = ImRaii.TreeNode("Active Triggers"))
        {
            if (subnodeTriggers)
            {
                foreach (var trigger in activeTriggers)
                    ImGui.TextUnformatted(trigger.ToString());
            }
        }
    }

    private void DrawKinksterCache(Kinkster kinksterRef)
    {
        using var nodeMain = ImRaii.TreeNode($"{kinksterRef.UserData.UID}'s Cache");
        if (!nodeMain) return;

        var uid = kinksterRef.UserData.UID;
        var cache = kinksterRef.LightCache;
        DrawKinksterGagCache(uid, cache);
        DrawKinksterRestrictionCache(uid, cache);
        DrawKinksterRestraintCache(uid, cache);
        DrawKinksterLootCache(uid, cache);
        DrawKinksterPatternCache(uid, cache);
        DrawKinksterAlarmCache(uid, cache);
        DrawKinksterTriggerCache(uid, cache);
    }

    private void DrawKinksterGagCache(string uid, KinksterCache cache)
    {
        using var n = ImRaii.TreeNode($"{uid}'s GagCache");
        if (!n) return;


        using (ImRaii.Table("##debug-lightstorage-gag-glamours" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Gag Type");
            ImGui.TableSetupColumn("Equip Slot");
            ImGui.TableHeadersRow();

            foreach (var (kind, data) in cache.Gags)
            {
                ImGuiUtil.DrawTableColumn(kind.GagName());
                ImGuiUtil.DrawTableColumn($"Is Enabled: {data.IsEnabled}");
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawKinksterRestrictionCache(string uid, KinksterCache cache)
    {
        using var n = ImRaii.TreeNode($"{uid}'s Restriction Cache");
        if (!n) return;

        using (ImRaii.Table("##debug-lightstorage-restrictions" + uid, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Set ID");
            ImGui.TableSetupColumn("Restriction Name");
            ImGui.TableSetupColumn("Affected Slots");
            ImGui.TableHeadersRow();

            foreach (var (id, data) in cache.Restrictions)
            {
                ImGuiUtil.DrawTableColumn(id.ToString());
                ImGuiUtil.DrawTableColumn(data.Label);
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawKinksterRestraintCache(string uid, KinksterCache cache)
    {
        using var n = ImRaii.TreeNode($"{uid}'s Restraint Cache");
        if (!n) return;

        using (ImRaii.Table("##debug-lightstorage-restraints" + uid, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Set ID");
            ImGui.TableSetupColumn("Restraint Set Name");
            ImGui.TableSetupColumn("Affected Slots");
            ImGui.TableHeadersRow();

            foreach (var (id, data) in cache.Restraints)
            {
                ImGuiUtil.DrawTableColumn(id.ToString());
                ImGuiUtil.DrawTableColumn(data.Label);
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawKinksterLootCache(string uid, KinksterCache cache)
    {
        using var n = ImRaii.TreeNode($"{uid}'s Loot Cache");
        if (!n) return;

        using (ImRaii.Table("##debug-lightstorage-cursed-items" + uid, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Cursed Item ID");
            ImGui.TableSetupColumn("Cursed Item Name");
            ImGui.TableSetupColumn("RestrictionType?");
            ImGui.TableHeadersRow();

            foreach (var (id, data) in cache.CursedItems)
            {
                ImGuiUtil.DrawTableColumn(id.ToString());
                ImGuiUtil.DrawTableColumn(data.Label);
                if (data.ItemReference is KinksterGag gag)
                    ImGuiUtil.DrawTableColumn($"Gag: [{gag.Gag.GagName()}]");
                else if (data.ItemReference is KinksterRestriction r)
                    ImGuiUtil.DrawTableColumn($"Restriction: [{r.Label}]");
                else
                    ImGuiUtil.DrawTableColumn("Unknown Type");
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawKinksterPatternCache(string uid, KinksterCache cache)
    {
        using var n = ImRaii.TreeNode($"{uid}'s Pattern Cache");
        if (!n) return;

        using (ImRaii.Table("##debug-lightstorage-patterns" + uid, 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Pattern ID");
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Duration");
            ImGui.TableSetupColumn("Loops?");
            ImGui.TableHeadersRow();

            foreach (var (id, data) in cache.Patterns)
            {
                ImGuiUtil.DrawTableColumn(id.ToString());
                ImGuiUtil.DrawTableColumn(data.Label);
                ImGuiUtil.DrawTableColumn(data.Duration.ToString());
                ImGuiUtil.DrawTableColumn(data.Loops.ToString());
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawKinksterAlarmCache(string uid, KinksterCache cache)
    {
        using var n = ImRaii.TreeNode($"{uid}'s Alarm Cache");
        if (!n) return;
        using (ImRaii.Table("##debug-lightstorage-alarms" + uid, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Alarm ID");
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Alarm Time");
            ImGui.TableHeadersRow();
            foreach (var (id, data) in cache.Alarms)
            {
                ImGuiUtil.DrawTableColumn(id.ToString());
                ImGuiUtil.DrawTableColumn(data.Label);
                ImGuiUtil.DrawTableColumn(data.SetTimeUTC.ToLocalTime().TimeOfDay.ToString());
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawKinksterTriggerCache(string uid, KinksterCache cache)
    {
        using var n = ImRaii.TreeNode($"{uid}'s Trigger Cache");
        if (!n) return;
        using (ImRaii.Table("##debug-lightstorage-triggers" + uid, 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Trigger ID");
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Trigger Type");
            ImGui.TableSetupColumn("Action Kind");
            ImGui.TableHeadersRow();
            foreach (var (id, data) in cache.Triggers)
            {
                ImGuiUtil.DrawTableColumn(id.ToString());
                ImGuiUtil.DrawTableColumn(data.Label);
                ImGuiUtil.DrawTableColumn(data.Kind.ToName());
                ImGuiUtil.DrawTableColumn(data.ActionType.ToName());
                ImGui.TableNextRow();
            }
        }
    }
}
