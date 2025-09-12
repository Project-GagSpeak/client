using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Textures;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Common.Lua;
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
using OtterGui;
using OtterGui.Extensions;
using OtterGui.Text;
using System.Buffers;
using System.Collections.Immutable;

namespace GagSpeak.Gui;

public class DebugPersonalDataUI : WindowMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly ClientData _clientData;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly KinksterManager _pairs;
    private readonly RestraintManager _restraints;
    private readonly RestrictionManager _restrictions;
    private readonly GagRestrictionManager _gags;
    private readonly CollarManager _collar;
    private readonly BuzzToyManager _toys;
    private readonly PatternManager _patterns;
    private readonly AlarmManager _alarms;
    private readonly TriggerManager _triggers;
    public DebugPersonalDataUI(
        ILogger<DebugPersonalDataUI> logger,
        GagspeakMediator mediator,
        MainConfig config,
        ClientData clientData,
        MoodleDrawer moodleDrawer,
        KinksterManager pairs,
        RestraintManager restraints,
        RestrictionManager restrictions,
        GagRestrictionManager gags,
        CollarManager collar,
        BuzzToyManager toys,
        PatternManager patterns,
        AlarmManager alarms,
        TriggerManager triggers)
        : base(logger, mediator, "Kinkster Data Debugger")
    {
        _config = config;
        _clientData = clientData;
        _moodleDrawer = moodleDrawer;
        _pairs = pairs;
        _restraints = restraints;
        _restrictions = restrictions;
        _gags = gags;
        _collar = collar;
        _toys = toys;
        _patterns = patterns;
        _alarms = alarms;
        _triggers = triggers;
        // Ensure the list updates properly.
        Mediator.Subscribe<RefreshUiKinkstersMessage>(this, _ => UpdateList());

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
        if (ImGui.CollapsingHeader("Client Player Data (Serverside Active State)"))
            DrawPlayerCharacterDebug();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Pair Data"))
        {
            ImGui.Text($"Total Pairs: {_pairs.DirectPairs.Count}");
            ImGui.Text($"Visible Users: {_pairs.GetVisibleUserCount()}");

            // The search.
            if (FancySearchBar.Draw("##PairDebugSearch", ImGui.GetContentRegionAvail().X, "Search for Pair..", ref _searchValue, 40))
                UpdateList();

            // Separator, then the results.
            ImGui.Separator();
            var width = ImGui.GetContentRegionAvail().X;
            foreach (var pair in _immutablePairs)
                DrawPairData(pair, width);
        }
    }

    private void DrawPairData(Kinkster pair, float width)
    {
        var nick = pair.GetNickAliasOrUid();
        using var node = ImRaii.TreeNode($"{nick}'s Pair Info");
        if (!node) return;

        DrawPairPerms(nick, pair);
        DrawPairAccess(nick, pair);
        DrawGlobalPermissions(pair.UserData.UID + "'s Global Perms", pair.PairGlobals);
        DrawHardcoreState(pair.UserData.UID + "'s Hardcore State", pair.PairHardcore);
        DrawKinksterIpcData(pair);
        DrawGagData(pair.UserData.UID, pair.ActiveGags);
        DrawPairRestrictions(pair.UserData.UID, pair);
        DrawRestraint(pair.UserData.UID, pair);
        DrawAlias(pair.UserData.UID, "Global", pair.LastGlobalAliasData);
        DrawNamedAlias(pair.UserData.UID, "Unique", pair.LastPairAliasData);
        DrawToybox(pair.UserData.UID, pair);
        DrawKinksterCache(pair);
        ImGui.Separator();
    }


    private void DrawPlayerCharacterDebug()
    {
        DrawGlobalPermissions("Player", ClientData.Globals ?? new GlobalPerms());
        DrawPlayerHardcore();
        DrawGagData("Player", _gags.ServerGagData ?? new CharaActiveGags());
        DrawRestrictions("Player", _restrictions.ServerRestrictionData ?? new CharaActiveRestrictions());
        DrawRestraint("Player", _restraints.ServerData ?? new CharaActiveRestraint());
        DrawCollar("Player", _collar.SyncedData ?? new CharaActiveCollar());

        CkGui.ColorText("Active Valid Toys:", ImGuiColors.ParsedGold);
        CkGui.TextInline(string.Join(", ", _toys.ValidToysForRemotes));

        // Draw out the active pattern.
        CkGui.ColorText("Active Pattern:", ImGuiColors.ParsedGold);
        CkGui.TextInline(_patterns.ActivePatternId.ToString());

        // Alarms.
        CkGui.ColorText("Active Alarms:", ImGuiColors.ParsedGold);
        CkGui.TextInline(string.Join(", ", _alarms.ActiveAlarms.Select(a => a.Label)));

        // Triggers.
        CkGui.ColorText("Active Triggers:", ImGuiColors.ParsedGold);
        CkGui.TextInline(string.Join(", ", _triggers.ActiveTriggers.Select(t => t.Label)));
    }

    private void DrawPlayerHardcore()
    {
        using var node = ImRaii.TreeNode($"Player Hardcore State");
        if (!node) return;

        _clientData.DrawHardcoreState();
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

    private void DrawKinksterPermRowBool(string name, bool valueOwn, bool valueOther)
    {
        ImGuiUtil.DrawTableColumn(name);
        ImGui.TableNextColumn();
        CkGui.IconText(valueOwn ? FAI.Check : FAI.Times, valueOwn ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
        ImGui.TableNextColumn();
        CkGui.IconText(valueOther ? FAI.Check : FAI.Times, valueOther ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
        ImGui.TableNextRow();
    }

    private void DrawKinksterPermRowString(string name, string valueOwn, string valueOther)
    {
        ImGuiUtil.DrawTableColumn(name);
        ImGuiUtil.DrawTableColumn(valueOwn);
        ImGuiUtil.DrawTableColumn(valueOther);
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

    private void DrawHardcoreState(string uid, HardcoreState perms)
    {
        using var nodeMain = ImRaii.TreeNode(uid + " Hardcore State");
        if (!nodeMain) return;

        PermissionHelper.DrawHardcoreState(perms);
    }

    private void DrawPairPerms(string label, Kinkster k)
    {
        using var nodeMain = ImRaii.TreeNode($"{label}'s Pair Permissions");
        if (!nodeMain) return;

        using var table = ImRaii.Table("##debug-pair" + k.UserData.UID, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        ImGui.TableSetupColumn("Permission");
        ImGui.TableSetupColumn("Own Setting");
        ImGui.TableSetupColumn($"{label}'s Setting");
        ImGui.TableHeadersRow();

        DrawKinksterPermRowBool("Is Paused", k.OwnPerms.IsPaused, k.PairPerms.IsPaused);
        ImGui.TableNextRow();

        DrawKinksterPermRowBool("Allows Permanent Locks", k.OwnPerms.PermanentLocks, k.PairPerms.PermanentLocks);
        DrawKinksterPermRowBool("Allows Owner Locks", k.OwnPerms.OwnerLocks, k.PairPerms.OwnerLocks);
        DrawKinksterPermRowBool("Allows Devotional Locks", k.OwnPerms.DevotionalLocks, k.PairPerms.DevotionalLocks);
        ImGui.TableNextRow();

        DrawKinksterPermRowBool("Apply Gags", k.OwnPerms.ApplyGags, k.PairPerms.ApplyGags);
        DrawKinksterPermRowBool("Lock Gags", k.OwnPerms.LockGags, k.PairPerms.LockGags);
        DrawKinksterPermRowString("Max Gag Time", k.OwnPerms.MaxGagTime.ToString(), k.PairPerms.MaxGagTime.ToString());
        DrawKinksterPermRowBool("Unlock Gags", k.OwnPerms.UnlockGags, k.PairPerms.UnlockGags);
        DrawKinksterPermRowBool("Remove Gags", k.OwnPerms.RemoveGags, k.PairPerms.RemoveGags);
        ImGui.TableNextRow();

        DrawKinksterPermRowBool("Apply Restrictions", k.OwnPerms.ApplyRestrictions, k.PairPerms.ApplyRestrictions);
        DrawKinksterPermRowBool("Lock Restrictions", k.OwnPerms.LockRestrictions, k.PairPerms.LockRestrictions);
        DrawKinksterPermRowString("Max Restriction Lock Time", k.OwnPerms.MaxRestrictionTime.ToString(), k.PairPerms.MaxRestrictionTime.ToString());
        DrawKinksterPermRowBool("Unlock Restrictions", k.OwnPerms.UnlockRestrictions, k.PairPerms.UnlockRestrictions);
        DrawKinksterPermRowBool("Remove Restrictions", k.OwnPerms.RemoveRestrictions, k.PairPerms.RemoveRestrictions);
        ImGui.TableNextRow();

        DrawKinksterPermRowBool("Apply Restraint Sets", k.OwnPerms.ApplyRestraintSets, k.PairPerms.ApplyRestraintSets);
        DrawKinksterPermRowBool("Apply Restraint Layers", k.OwnPerms.ApplyLayers, k.PairPerms.ApplyLayers);
        DrawKinksterPermRowBool("Add Locked Layers", k.OwnPerms.ApplyLayersWhileLocked, k.PairPerms.ApplyLayersWhileLocked);
        DrawKinksterPermRowBool("Lock Restraint Sets", k.OwnPerms.LockRestraintSets, k.PairPerms.LockRestraintSets);
        DrawKinksterPermRowString("Max Restraint Lock Time", k.OwnPerms.MaxRestraintTime.ToString(), k.PairPerms.MaxRestraintTime.ToString());
        DrawKinksterPermRowBool("Unlock Restraint Sets", k.OwnPerms.UnlockRestraintSets, k.PairPerms.UnlockRestraintSets);
        DrawKinksterPermRowBool("Remove Restraint Layers", k.OwnPerms.RemoveLayers, k.PairPerms.RemoveLayers);
        DrawKinksterPermRowBool("Remove Locked Layers", k.OwnPerms.RemoveLayersWhileLocked, k.PairPerms.RemoveLayersWhileLocked);
        DrawKinksterPermRowBool("Remove Restraint Sets", k.OwnPerms.RemoveRestraintSets, k.PairPerms.RemoveRestraintSets);
        ImGui.TableNextRow();

        DrawKinksterPermRowString("Trigger Phrase", k.OwnPerms.TriggerPhrase, k.PairPerms.TriggerPhrase);
        DrawKinksterPermRowString("Start Char", k.OwnPerms.StartChar.ToString(), k.PairPerms.StartChar.ToString());
        DrawKinksterPermRowString("End Char", k.OwnPerms.EndChar.ToString(), k.PairPerms.EndChar.ToString());
        DrawKinksterPermRowBool("Sit Requests", k.OwnPerms.PuppetPerms.HasAny(PuppetPerms.Sit), k.PairPerms.PuppetPerms.HasAny(PuppetPerms.Sit));
        DrawKinksterPermRowBool("Motion Requests", k.OwnPerms.PuppetPerms.HasAny(PuppetPerms.Emotes), k.PairPerms.PuppetPerms.HasAny(PuppetPerms.Emotes));
        DrawKinksterPermRowBool("Alias Requests", k.OwnPerms.PuppetPerms.HasAny(PuppetPerms.Alias), k.PairPerms.PuppetPerms.HasAny(PuppetPerms.Alias));
        DrawKinksterPermRowBool("All Requests", k.OwnPerms.PuppetPerms.HasAny(PuppetPerms.All), k.PairPerms.PuppetPerms.HasAny(PuppetPerms.All));
        ImGui.TableNextRow();

        DrawKinksterPermRowBool("Positive Moodles", k.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PositiveStatusTypes), k.PairPerms.MoodlePerms.HasAny(MoodlePerms.PositiveStatusTypes));
        DrawKinksterPermRowBool("Negative Moodles", k.OwnPerms.MoodlePerms.HasAny(MoodlePerms.NegativeStatusTypes), k.PairPerms.MoodlePerms.HasAny(MoodlePerms.NegativeStatusTypes));
        DrawKinksterPermRowBool("Special Moodles", k.OwnPerms.MoodlePerms.HasAny(MoodlePerms.SpecialStatusTypes), k.PairPerms.MoodlePerms.HasAny(MoodlePerms.SpecialStatusTypes));
        DrawKinksterPermRowBool("Apply Own Moodles", k.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou), k.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou));
        DrawKinksterPermRowBool("Apply Your Moodles", k.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou), k.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou));
        DrawKinksterPermRowString("Max Moodle Time", k.OwnPerms.MaxMoodleTime.ToString(), k.PairPerms.MaxMoodleTime.ToString());
        DrawKinksterPermRowBool("Permanent Moodles", k.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PermanentMoodles), k.PairPerms.MoodlePerms.HasAny(MoodlePerms.PermanentMoodles));
        DrawKinksterPermRowBool("Removing Moodles", k.OwnPerms.MoodlePerms.HasAny(MoodlePerms.RemovingMoodles), k.PairPerms.MoodlePerms.HasAny(MoodlePerms.RemovingMoodles));
        ImGui.TableNextRow();

        DrawKinksterPermRowBool("Can Execute Patterns", k.OwnPerms.ExecutePatterns, k.PairPerms.ExecutePatterns);
        DrawKinksterPermRowBool("Can Stop Patterns", k.OwnPerms.StopPatterns, k.PairPerms.StopPatterns);
        DrawKinksterPermRowBool("Can Toggle Alarms", k.OwnPerms.ToggleAlarms, k.PairPerms.ToggleAlarms);
        DrawKinksterPermRowBool("Can Toggle Triggers", k.OwnPerms.ToggleTriggers, k.PairPerms.ToggleTriggers);
        ImGui.TableNextRow();

        DrawKinksterPermRowBool("Hypno Effects", k.OwnPerms.HypnoEffectSending, k.PairPerms.HypnoEffectSending);
        ImGui.TableNextRow();

        DrawKinksterPermRowBool("In Hardcore Mode", k.OwnPerms.InHardcore, k.PairPerms.InHardcore);
        DrawKinksterPermRowBool("Devotional States For Pair", k.OwnPerms.PairLockedStates, k.PairPerms.PairLockedStates);
        DrawKinksterPermRowBool("Allow Forced Follow", k.OwnPerms.AllowLockedFollowing, k.PairPerms.AllowLockedFollowing);
        DrawKinksterPermRowBool("Allow Forced Sit", k.OwnPerms.AllowLockedSitting, k.PairPerms.AllowLockedSitting);
        DrawKinksterPermRowBool("Allow Forced Emote", k.OwnPerms.AllowLockedEmoting, k.PairPerms.AllowLockedEmoting);
        DrawKinksterPermRowBool("Allow Indoor Confinement", k.OwnPerms.AllowIndoorConfinement, k.PairPerms.AllowIndoorConfinement);
        DrawKinksterPermRowBool("Allow Imprisonment", k.OwnPerms.AllowImprisonment, k.PairPerms.AllowImprisonment);
        DrawKinksterPermRowBool("Allow GarbleChannelEditing", k.OwnPerms.AllowGarbleChannelEditing, k.PairPerms.AllowGarbleChannelEditing);
        DrawKinksterPermRowBool("Allow Hiding Chat Boxes", k.OwnPerms.AllowHidingChatBoxes, k.PairPerms.AllowHidingChatBoxes);
        DrawKinksterPermRowBool("Allow Hiding Chat Input", k.OwnPerms.AllowHidingChatInput, k.PairPerms.AllowHidingChatInput);
        DrawKinksterPermRowBool("Allow Chat Input Blocking", k.OwnPerms.AllowChatInputBlocking, k.PairPerms.AllowChatInputBlocking);
        DrawKinksterPermRowBool("Allow Hypnosis Image Sending", k.OwnPerms.AllowHypnoImageSending, k.PairPerms.AllowHypnoImageSending);
        ImGui.TableNextRow();

        DrawKinksterPermRowString("Shock Collar Share Code", k.OwnPerms.PiShockShareCode, k.PairPerms.PiShockShareCode);
        DrawKinksterPermRowBool("Allow Shocks", k.OwnPerms.AllowShocks, k.PairPerms.AllowShocks);
        DrawKinksterPermRowBool("Allow Vibrations", k.OwnPerms.AllowVibrations, k.PairPerms.AllowVibrations);
        DrawKinksterPermRowBool("Allow Beeps", k.OwnPerms.AllowBeeps, k.PairPerms.AllowBeeps);
        DrawKinksterPermRowString("Max Intensity", k.OwnPerms.MaxIntensity.ToString(), k.PairPerms.MaxIntensity.ToString());
        DrawKinksterPermRowString("Max Duration", k.OwnPerms.MaxDuration.ToString(), k.PairPerms.MaxDuration.ToString());
    }

    private void DrawPairAccess(string label, Kinkster k)
    {
        using var nodeMain = ImRaii.TreeNode($"{label}'s Edit Access");
        if (!nodeMain) return;

        using var table = ImRaii.Table("##debug-access-" + k.UserData.UID, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        ImGui.TableSetupColumn("Permission");
        ImGui.TableSetupColumn("Own Setting");
        ImGui.TableSetupColumn($"{label}'s Setting");
        ImGui.TableHeadersRow();

        // Live Chat Permissions
        DrawKinksterPermRowBool("Chat Garbler State", k.OwnPermAccess.ChatGarblerActiveAllowed, k.PairPermAccess.ChatGarblerActiveAllowed);
        DrawKinksterPermRowBool("Chat Garbler Lock", k.OwnPermAccess.ChatGarblerLockedAllowed, k.OwnPermAccess.ChatGarblerLockedAllowed);
        DrawKinksterPermRowBool("Gagged Nameplates", k.OwnPermAccess.GaggedNameplateAllowed, k.PairPermAccess.GaggedNameplateAllowed);
        ImGui.TableNextRow();

        // Visuals
        DrawKinksterPermRowBool("Wardrobe", k.OwnPermAccess.WardrobeEnabledAllowed, k.PairPermAccess.WardrobeEnabledAllowed);
        DrawKinksterPermRowBool("Gag Visuals", k.OwnPermAccess.GagVisualsAllowed, k.PairPermAccess.GagVisualsAllowed);
        DrawKinksterPermRowBool("Restriction Visuals", k.OwnPermAccess.RestrictionVisualsAllowed, k.PairPermAccess.RestrictionVisualsAllowed);
        DrawKinksterPermRowBool("Restraint Visuals", k.OwnPermAccess.RestraintSetVisualsAllowed, k.PairPermAccess.RestraintSetVisualsAllowed);
        ImGui.TableNextRow();

        // Padlocks
        DrawKinksterPermRowBool("Permanent Locks", k.OwnPermAccess.PermanentLocksAllowed, k.PairPermAccess.PermanentLocksAllowed);
        DrawKinksterPermRowBool("Owner Locks", k.OwnPermAccess.OwnerLocksAllowed, k.PairPermAccess.OwnerLocksAllowed);
        DrawKinksterPermRowBool("Devotional Locks", k.OwnPermAccess.DevotionalLocksAllowed, k.PairPermAccess.DevotionalLocksAllowed);
        ImGui.TableNextRow();

        // Gags
        DrawKinksterPermRowBool("Apply Gags", k.OwnPermAccess.ApplyGagsAllowed, k.PairPermAccess.ApplyGagsAllowed);
        DrawKinksterPermRowBool("Lock Gags", k.OwnPermAccess.LockGagsAllowed, k.PairPermAccess.LockGagsAllowed);
        DrawKinksterPermRowString("Max Gag Time", k.OwnPermAccess.MaxGagTimeAllowed.ToString(), k.PairPermAccess.MaxGagTimeAllowed.ToString());
        DrawKinksterPermRowBool("Unlock Gags", k.OwnPermAccess.UnlockGagsAllowed, k.PairPermAccess.UnlockGagsAllowed);
        DrawKinksterPermRowBool("Remove Gags", k.OwnPermAccess.RemoveGagsAllowed, k.PairPermAccess.RemoveGagsAllowed);
        ImGui.TableNextRow();

        // Restrictions
        DrawKinksterPermRowBool("Apply Restrictions", k.OwnPermAccess.ApplyRestrictionsAllowed, k.PairPermAccess.ApplyRestrictionsAllowed);
        DrawKinksterPermRowBool("Lock Restrictions", k.OwnPermAccess.LockRestrictionsAllowed, k.PairPermAccess.LockRestrictionsAllowed);
        DrawKinksterPermRowString("Max Restriction Lock Time", k.OwnPermAccess.MaxRestrictionTimeAllowed.ToString(), k.PairPermAccess.MaxRestrictionTimeAllowed.ToString());
        DrawKinksterPermRowBool("Unlock Restrictions", k.OwnPermAccess.UnlockRestrictionsAllowed, k.PairPermAccess.UnlockRestrictionsAllowed);
        DrawKinksterPermRowBool("Remove Restrictions", k.OwnPermAccess.RemoveRestrictionsAllowed, k.PairPermAccess.RemoveRestrictionsAllowed);
        ImGui.TableNextRow();

        // Restraints
        DrawKinksterPermRowBool("Apply Restraint Sets", k.OwnPermAccess.ApplyRestraintSetsAllowed, k.PairPermAccess.ApplyRestraintSetsAllowed);
        DrawKinksterPermRowBool("Add Restraint Layers", k.OwnPermAccess.ApplyLayersAllowed, k.PairPermAccess.ApplyLayersAllowed);
        DrawKinksterPermRowBool("Apply Locked Layers", k.OwnPermAccess.ApplyLayersWhileLockedAllowed, k.PairPermAccess.ApplyLayersWhileLockedAllowed);
        DrawKinksterPermRowBool("Lock Restraint Sets", k.OwnPermAccess.LockRestraintSetsAllowed, k.PairPermAccess.LockRestraintSetsAllowed);
        DrawKinksterPermRowString("Max Restraint Set Lock Time", k.OwnPermAccess.MaxRestraintTimeAllowed.ToString(), k.PairPermAccess.MaxRestraintTimeAllowed.ToString());
        DrawKinksterPermRowBool("Unlock Restraint Sets", k.OwnPermAccess.UnlockRestraintSetsAllowed, k.PairPermAccess.UnlockRestraintSetsAllowed);
        DrawKinksterPermRowBool("Remove Restraint Layers", k.OwnPermAccess.RemoveLayersAllowed, k.PairPermAccess.RemoveLayersAllowed);
        DrawKinksterPermRowBool("Remove Locked Layers", k.OwnPermAccess.RemoveLayersWhileLockedAllowed, k.PairPermAccess.RemoveLayersWhileLockedAllowed);
        DrawKinksterPermRowBool("Remove Restraint Sets", k.OwnPermAccess.RemoveRestraintSetsAllowed, k.PairPermAccess.RemoveRestraintSetsAllowed);
        ImGui.TableNextRow();

        // Puppeteer
        DrawKinksterPermRowBool("Puppeteer", k.OwnPermAccess.PuppeteerEnabledAllowed, k.PairPermAccess.PuppeteerEnabledAllowed);
        DrawKinksterPermRowBool("Allow Sit Requests", k.OwnPermAccess.PuppetPermsAllowed.HasAny(PuppetPerms.Sit), k.PairPermAccess.PuppetPermsAllowed.HasAny(PuppetPerms.Sit));
        DrawKinksterPermRowBool("Allow Motion Requests", k.OwnPermAccess.PuppetPermsAllowed.HasAny(PuppetPerms.Emotes), k.PairPermAccess.PuppetPermsAllowed.HasAny(PuppetPerms.Emotes));
        DrawKinksterPermRowBool("Allow Alias Requests", k.OwnPermAccess.PuppetPermsAllowed.HasAny(PuppetPerms.Alias), k.PairPermAccess.PuppetPermsAllowed.HasAny(PuppetPerms.Alias));
        DrawKinksterPermRowBool("Allow All Requests", k.OwnPermAccess.PuppetPermsAllowed.HasAny(PuppetPerms.All), k.PairPermAccess.PuppetPermsAllowed.HasAny(PuppetPerms.All));
        ImGui.TableNextRow();

        // Moodles
        DrawKinksterPermRowBool("Moodles", k.OwnPermAccess.MoodlesEnabledAllowed, k.PairPermAccess.MoodlesEnabledAllowed);
        DrawKinksterPermRowBool("Allow Positive Moodles", k.OwnPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.PositiveStatusTypes), k.PairPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.PositiveStatusTypes));
        DrawKinksterPermRowBool("Allow Negative Moodles", k.OwnPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.NegativeStatusTypes), k.PairPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.NegativeStatusTypes));
        DrawKinksterPermRowBool("Allow Special Moodles", k.OwnPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.SpecialStatusTypes), k.PairPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.SpecialStatusTypes));
        DrawKinksterPermRowBool("Apply Own Moodles", k.OwnPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou), k.PairPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou));
        DrawKinksterPermRowBool("Apply Your Moodles", k.OwnPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou), k.PairPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou));
        DrawKinksterPermRowString("Max Moodle Time", k.OwnPermAccess.MaxMoodleTimeAllowed.ToString(), k.PairPermAccess.MaxMoodleTimeAllowed.ToString());
        DrawKinksterPermRowBool("Allow Permanent Moodles", k.OwnPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.PermanentMoodles), k.PairPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.PermanentMoodles));
        DrawKinksterPermRowBool("Allow Removing Moodles", k.OwnPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.RemovingMoodles), k.PairPermAccess.MoodlePermsAllowed.HasAny(MoodlePerms.RemovingMoodles));
        ImGui.TableNextRow();

        // Toybox
        DrawKinksterPermRowBool("Spatial Vibrator Audio", k.OwnPermAccess.SpatialAudioAllowed, k.PairPermAccess.SpatialAudioAllowed);
        DrawKinksterPermRowBool("Can Execute Patterns", k.OwnPermAccess.ExecutePatternsAllowed, k.PairPermAccess.ExecutePatternsAllowed);
        DrawKinksterPermRowBool("Can Stop Patterns", k.OwnPermAccess.StopPatternsAllowed, k.PairPermAccess.StopPatternsAllowed);
        DrawKinksterPermRowBool("Can Toggle Alarms", k.OwnPermAccess.ToggleAlarmsAllowed, k.PairPermAccess.ToggleAlarmsAllowed);
        DrawKinksterPermRowBool("Can Toggle Triggers", k.OwnPermAccess.ToggleTriggersAllowed, k.PairPermAccess.ToggleTriggersAllowed);
    }
    private void DrawKinksterIpcData(Kinkster kinkster)
    {
        var dispName = kinkster.GetNickAliasOrUid();
        using var nodeMain = ImRaii.TreeNode($"{dispName}'s IPC Data");
        if (!nodeMain) return;

        CkGui.ColorTextCentered($"Active Statuses: {kinkster.LastMoodlesData.DataInfo.Count()}", ImGuiColors.ParsedGold);
        _moodleDrawer.ShowStatusInfosFramed($"DataInfo-{dispName}", kinkster.LastMoodlesData.DataInfoList, ImGui.GetContentRegionAvail().X, CkStyle.ChildRoundingLarge(), MoodleDrawer.IconSizeFramed);


        CkGui.ColorTextCentered($"Stored Statuses: {kinkster.LastMoodlesData.StatusList.Count()}", ImGuiColors.ParsedGold);
        _moodleDrawer.ShowStatusInfosFramed($"StatusList-{dispName}", kinkster.LastMoodlesData.StatusList, ImGui.GetContentRegionAvail().X, CkStyle.ChildRoundingLarge(), MoodleDrawer.IconSizeFramed, 2);
        
        DrawMoodlePresetTable(dispName, kinkster.LastMoodlesData);

        // draw out the Appearance Cache.
        DrawAppearanceCache(kinkster, dispName);
    }

    private void DrawMoodlePresetTable(string uid, CharaMoodleData data)
    {
        using var nodeMain = ImRaii.TreeNode($"{uid}'s Stored Preset Data");
        if (!nodeMain) return;

        using (var t = ImRaii.Table($"PresetTable-{uid}", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t) return;
            ImGui.TableSetupColumn("Preset Title");
            ImGui.TableSetupColumn("Statuses");
            ImGui.TableHeadersRow();
            foreach (var preset in data.PresetList)
            {
                ImGui.TableNextColumn();
                ImGui.Text(preset.Title);
                ImGui.TableNextColumn();
                var statuses = preset.Statuses.Select(s => data.Statuses.GetValueOrDefault(s)).Where(x => x.GUID != Guid.Empty);
                _moodleDrawer.DrawStatusInfos(statuses, MoodleDrawer.IconSizeFramed);
            }

        }
    }

    private void DrawAppearanceCache(Kinkster kinkster, string dispName)
    {
        using var node = ImRaii.TreeNode($"{dispName}'s Latest Appearance");
        if (!node) return;

        // Draw out the cache Values.
        using (var t = ImRaii.Table($"AppearanceCache-{dispName}", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t) return;
            ImGui.TableSetupColumn("Source");
            ImGui.TableSetupColumn("Content");
            ImGui.TableHeadersRow();

            ImGui.TableNextColumn();
            CkGui.ColorTextBool("Actor Glamour", kinkster.LastAppearanceData.GlamourerBase64 is not null);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(kinkster.LastAppearanceData.GlamourerBase64 != null, false);
            if (ImGui.IsItemHovered())
                CkGui.AttachToolTip(kinkster.LastAppearanceData.GlamourerBase64 ?? "No Actor Glamour Data");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("C+ Profile");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(kinkster.LastAppearanceData.CustomizeProfile != null, false);
            if (ImGui.IsItemHovered())
                CkGui.AttachToolTip(kinkster.LastAppearanceData.CustomizeProfile ?? "No C+ Profile Data");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Heels Offset");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(kinkster.LastAppearanceData.HeelsOffset != null, false);
            if (ImGui.IsItemHovered())
                CkGui.AttachToolTip(kinkster.LastAppearanceData.HeelsOffset?.ToString() ?? "No Heels Offset Data");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Title Info");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(kinkster.LastAppearanceData.HonorificTitle != null, false);
            if (ImGui.IsItemHovered())
                CkGui.AttachToolTip(kinkster.LastAppearanceData.HonorificTitle?.ToString() ?? "No Honorific Title!");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Pet Nicknames");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(kinkster.LastAppearanceData.PetNicknames != null, false);
            if (ImGui.IsItemHovered())
                CkGui.AttachToolTip(kinkster.LastAppearanceData.PetNicknames?.ToString() ?? "No Pet Nicknames!");
        }
    }

    private void DrawGagData(string uid, CharaActiveGags appearance)
    {
        using var nodeMain = ImRaii.TreeNode("Gag Data");
        if (!nodeMain)
            return;

        using (ImRaii.Table("##debug-gag" + uid, 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Layer");
            ImGui.TableSetupColumn("Type");
            ImGui.TableSetupColumn("Enabler");
            ImGui.TableSetupColumn("Padlock");
            ImGui.TableSetupColumn("Password");
            ImGui.TableSetupColumn("TimeLeft");
            ImGui.TableSetupColumn("Assigner");
            ImGui.TableHeadersRow();

            foreach (var (gag, idx) in appearance.GagSlots.WithIndex())
            {
                ImGuiUtil.DrawTableColumn($"{idx + 1}");
                ImGuiUtil.DrawTableColumn(gag.GagItem.GagName());
                ImGuiUtil.DrawTableColumn(gag.Enabler);
                ImGuiUtil.DrawTableColumn(gag.Padlock.ToName());
                ImGuiUtil.DrawTableColumn(gag.Password);
                ImGui.TableNextColumn();
                CkGui.ColorText(gag.Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
                ImGuiUtil.DrawTableColumn(gag.PadlockAssigner);
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawRestrictions(string uid, CharaActiveRestrictions restrictions)
    {
        using var nodeMain = ImRaii.TreeNode("Restrictions Data");
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

            foreach (var (restriction, idx) in restrictions.Restrictions.WithIndex())
            {
                ImGuiUtil.DrawTableColumn($"{idx + 1}");
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

    private void DrawCollar(string uid, CharaActiveCollar collar)
    {
        using var node = ImRaii.TreeNode("Collar Data");
        if (!node) return;

        using (ImRaii.Table("##debug-collar" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Property");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            DrawPermissionRowBool("Applied", collar.Applied);

            ImGuiUtil.DrawTableColumn("Owners");
            ImGuiUtil.DrawTableColumn(string.Join(", ", collar.OwnerUIDs));
            ImGui.TableNextRow();

            DrawPermissionRowBool("Visuals", collar.Visuals);

            ImGuiUtil.DrawTableColumn("Dye1");
            ImGuiUtil.DrawTableColumn(collar.Dye1.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Dye2");
            ImGuiUtil.DrawTableColumn(collar.Dye2.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Moodle");
            ImGui.TableNextColumn();
            if (collar.Moodle.GUID == Guid.Empty)
                ImGui.TextUnformatted("None");
            else
            {
                MoodleDisplay.DrawMoodleIcon(collar.Moodle.IconID, collar.Moodle.Stacks, MoodleDrawer.IconSizeFramed);
                GsExtensions.DrawMoodleStatusTooltip(collar.Moodle, Enumerable.Empty<MoodlesStatusInfo>());
            }
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Writing");
            ImGuiUtil.DrawTableColumn(collar.Writing ?? "None");
            ImGui.TableNextRow();
        }

        using (ImRaii.Table("##debug-collar-perms" + uid, 7, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Permission-Relation");
            ImGui.TableSetupColumn("Visuals");
            ImGui.TableSetupColumn("Dye Control");
            ImGui.TableSetupColumn("Moodle Control");
            ImGui.TableSetupColumn("Collar Writing");
            ImGui.TableSetupColumn("Glamour/Mod Control");
            ImGui.TableHeadersRow();

            ImGuiUtil.DrawTableColumn(uid);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(collar.CollaredAccess.HasAny(CollarAccess.Visuals), false);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(collar.CollaredAccess.HasAny(CollarAccess.Dyes), false);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(collar.CollaredAccess.HasAny(CollarAccess.Moodle), false);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(collar.CollaredAccess.HasAny(CollarAccess.Writing), false);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(collar.CollaredAccess.HasAny(CollarAccess.GlamMod), false);
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Owners");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(collar.OwnerAccess.HasAny(CollarAccess.Visuals), false);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(collar.OwnerAccess.HasAny(CollarAccess.Dyes), false);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(collar.OwnerAccess.HasAny(CollarAccess.Moodle), false);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(collar.OwnerAccess.HasAny(CollarAccess.Writing), false);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(collar.OwnerAccess.HasAny(CollarAccess.GlamMod), false);
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

    private void DrawNamedAlias(string uid, string label, NamedAliasStorage storage)
    {
        using var nodeMain = ImRaii.TreeNode($"{label}'s Alias Data");
        if (!nodeMain) return;

        CkGui.ColorText($"Listener Name:", ImGuiColors.ParsedGold);
        CkGui.TextInline(storage.ExtractedListenerName);

        using (ImRaii.Table("##debug-aliasdata-" + uid, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {

            ImGui.TableSetupColumn("Alias Input");
            ImGui.TableSetupColumn("Alias Output");
            ImGui.TableHeadersRow();
            foreach (var aliasData in storage.Storage.Items)
            {
                ImGuiUtil.DrawTableColumn(aliasData.InputCommand);
                ImGuiUtil.DrawTableColumn("(Output sections being worked on atm?)");
                ImGui.TableNextRow();
            }
        }
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

    private void DrawToybox(string uid, Kinkster kinkster)
    {

        CkGui.ColorText("Active Valid Toys:", ImGuiColors.ParsedGold);
        CkGui.TextInline(string.Join(", ", kinkster.ValidToys));

        CkGui.ColorText("Active Pattern:", ImGuiColors.ParsedGold);
        CkGui.TextInline($"{kinkster.LightCache.Patterns.GetValueOrDefault(kinkster.ActivePattern)?.Label ?? kinkster.ActivePattern.ToString()}");
        // alarm sub-node
        using (var subnodeAlarm = ImRaii.TreeNode("Active Alarms"))
        {
            if (subnodeAlarm)
            {
                foreach (var alarm in kinkster.ActiveAlarms)
                    ImGui.TextUnformatted($"{kinkster.LightCache.Alarms.GetValueOrDefault(alarm)?.Label ?? alarm.ToString()}");
            }
        }

        // active triggers sub-node
        using (var subnodeTriggers = ImRaii.TreeNode("Active Triggers"))
        {
            if (subnodeTriggers)
            {
                foreach (var trigger in kinkster.ActiveTriggers)
                    ImGui.TextUnformatted($"{kinkster.LightCache.Triggers.GetValueOrDefault(trigger)?.Label ?? trigger.ToString()}");
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
