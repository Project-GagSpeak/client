using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.FileSystems;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Util;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui;
using Penumbra.GameData.Enums;

namespace GagSpeak.CkCommons.Gui;

public class DebugStorageUI : WindowMediatorSubscriberBase
{
    private readonly KinksterRequests _kinksterRequests;
    private readonly GlobalPermissions _globals;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CursedLootManager _cursedLoot;
    private readonly PuppeteerManager _puppeteer;
    private readonly TriggerManager _triggers;
    private readonly AlarmManager _alarms;
    private readonly PatternManager _patterns;
    private readonly GagFileSystem _gagFS;
    private readonly RestrictionFileSystem _restrictionsFS;
    private readonly RestraintSetFileSystem _restraintsFS;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly ModPresetDrawer _modPresetDrawer;
    public DebugStorageUI(
        ILogger<DebugStorageUI> logger,
        GagspeakMediator mediator,
        KinksterRequests kinksterRequests,
        GlobalPermissions globals,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        CursedLootManager cursedLoot,
        PuppeteerManager puppeteer,
        TriggerManager triggers,
        AlarmManager alarms,
        PatternManager patterns,
        GagFileSystem gagFS,
        RestrictionFileSystem restrictionsFS,
        RestraintSetFileSystem restraintsFS,
        MoodleDrawer moodleDrawer,
        ModPresetDrawer modPresetDrawer)
        : base(logger, mediator, "Debugger for Storages")
    {
        _kinksterRequests = kinksterRequests;
        _globals = globals;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _cursedLoot = cursedLoot;
        _puppeteer = puppeteer;
        _triggers = triggers;
        _alarms = alarms;
        _patterns = patterns;
        _gagFS = gagFS;
        _restrictionsFS = restrictionsFS;
        _restraintsFS = restraintsFS;
        _moodleDrawer = moodleDrawer;
        _modPresetDrawer = modPresetDrawer;

        IsOpen = false;

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(380, 400),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
    }

    protected override void PreDrawInternal() { }

    protected override void PostDrawInternal() { }

    protected override void DrawInternal()
    {
        DrawGlobalData();

        ImGui.Separator();
        DrawGagStorage();

        ImGui.Separator();
        DrawRestrictionStorage();

        ImGui.Separator();
        DrawRestraintStorage();
        
        ImGui.Separator();
        DrawCursedLootStorage();
        
        ImGui.Separator();
        DrawPuppeteerGlobalStorage();
        
        ImGui.Separator();
        DrawPuppeteerPairStorage();

        ImGui.Separator();
        DrawTriggerStorage();
        
        ImGui.Separator();
        DrawAlarmStorage();
        
        ImGui.Separator();
        DrawPatternStorage();
    }

    public void DrawGlobalData()
    {
        if (!ImGui.CollapsingHeader("Player Global Data"))
            return;
        using (var node = ImRaii.TreeNode("Incoming Pair Requests##0"))
            if (node)
            {
                ImGui.TextUnformatted("Incoming Requests:");
                using (ImRaii.Table("##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGuiUtil.DrawTableColumn("User");
                    ImGuiUtil.DrawTableColumn("RecipientUser");
                    ImGuiUtil.DrawTableColumn("AttachedMessage");
                    ImGuiUtil.DrawTableColumn("CreationTime");
                    foreach (var req in _kinksterRequests.IncomingRequests)
                    {
                        ImGui.TableNextRow();
                        ImGuiUtil.DrawTableColumn(req.User.UID.ToString());
                        ImGuiUtil.DrawTableColumn(req.Target.UID.ToString());
                        ImGuiUtil.DrawTableColumn(req.Message.ToString());
                        ImGuiUtil.DrawTableColumn(req.CreationTime.ToString());
                    }
                }
                ImGui.Spacing();
            }
        using (var node = ImRaii.TreeNode("Outgoing Pair Requests##1"))
            if (node)
            {
                ImGui.TextUnformatted("Outgoing Requests:");
                using (ImRaii.Table("##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGuiUtil.DrawTableColumn("User");
                    ImGuiUtil.DrawTableColumn("RecipientUser");
                    ImGuiUtil.DrawTableColumn("AttachedMessage");
                    ImGuiUtil.DrawTableColumn("CreationTime");
                    foreach (var req in _kinksterRequests.OutgoingRequests)
                    {
                        ImGui.TableNextRow();
                        ImGuiUtil.DrawTableColumn(req.User.UID.ToString());
                        ImGuiUtil.DrawTableColumn(req.Target.UID.ToString());
                        ImGuiUtil.DrawTableColumn(req.Message.ToString());
                        ImGuiUtil.DrawTableColumn(req.CreationTime.ToString());
                    }
                }
            }
        using (var node = ImRaii.TreeNode("Global Permissions##2"))
            if (node)
            {
                ImGui.TextUnformatted("Global Permissions:");
                using (ImRaii.Table("##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGuiUtil.DrawTableColumn("ChatGarblerChannelsBitfield:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ChatGarblerChannelsBitfield.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ChatGarblerActive:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ChatGarblerActive.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ChatGarblerLocked:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ChatGarblerLocked.ToString());
                    ImGui.TableNextRow();

                    // wardrobe global modifiable permissions
                    ImGuiUtil.DrawTableColumn("WardrobeEnabled:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.WardrobeEnabled.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("GagVisuals:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.GagVisuals.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("RestrictionVisuals:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.RestrictionVisuals.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("RestraintSetVisuals:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.RestraintSetVisuals.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("PuppeteerEnabled:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.PuppeteerEnabled.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("TriggerPhrase:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.TriggerPhrase.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("PuppetPerms:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.PuppetPerms.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ToyboxEnabled:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ToyboxEnabled.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("LockToyboxUI:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.LockToyboxUI.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ToysAreConnected:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ToysAreConnected.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ToysAreInUse:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ToysAreInUse.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("SpatialAudio:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.SpatialAudio.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ForcedFollow:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ForcedFollow.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ForcedEmoteState:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ForcedEmoteState.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ForcedStay:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ForcedStay.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ChatBoxesHidden:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ChatBoxesHidden.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ChatInputHidden:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ChatInputHidden.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ChatInputBlocked:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ChatInputBlocked.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("GlobalShockShareCode:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.GlobalShockShareCode.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("AllowShocks:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.AllowShocks.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("AllowVibrations:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.AllowVibrations.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("AllowBeeps:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.AllowBeeps.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("MaxIntensity:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.MaxIntensity.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("MaxDuration:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.MaxDuration.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ShockVibrateDuration:");
                    ImGuiUtil.DrawTableColumn(_globals.Current.ShockVibrateDuration.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("User");
                    ImGuiUtil.DrawTableColumn("RecipientUser");
                    ImGuiUtil.DrawTableColumn("AttachedMessage");
                    ImGuiUtil.DrawTableColumn("CreationTime");
                }
            }
    }

    public void DrawGagStorage()
    {
        if (!ImGui.CollapsingHeader("Gag Storage"))
            return;
        if (_gags.Storage.IsNullOrEmpty())
        {
            ImGui.TextUnformatted("Gag Storage is null or empty");
            return;
        }
        foreach (var (type, gagRestriction) in _gags.Storage)
        {
            using var node = ImRaii.TreeNode($"{type.GagName()}");
            if (!node)
                continue;

            DrawGag(gagRestriction);
        }
    }

    public void DrawRestrictionStorage()
    {
        if (!ImGui.CollapsingHeader("Restriction Storage"))
            return;
        if (_restrictions.Storage.IsNullOrEmpty())
        {
            ImGui.TextUnformatted("Restriction Storage is null or empty");
            return;
        }
        foreach (var (restriction, idx) in _restrictions.Storage.WithIndex())
        {
            using var node = ImRaii.TreeNode($"{restriction.Label}##{idx}");
            if (!node)
                continue;

            DrawRestriction(restriction);
        }
    }

    public void DrawRestraintStorage()
    {
        if (!ImGui.CollapsingHeader("RestraintSet Storage"))
            return;
        if (_restraints.Storage.IsNullOrEmpty())
        {
            ImGui.TextUnformatted("RestraintSet Storage is null or empty");
            return;
        }
        foreach (var (restraintSet, idx) in _restraints.Storage.WithIndex())
        {
            using var node = ImRaii.TreeNode($"{restraintSet.Label}##{idx}");
            if (!node)
                continue;

            DrawRestraintSet(restraintSet);
        }
    }

    public void DrawCursedLootStorage()
    {
        if (!ImGui.CollapsingHeader("CursedLoot Storage"))
            return;
        if (_cursedLoot.Storage.IsNullOrEmpty())
        {
            ImGui.TextUnformatted("Cursed Loot Storage is null or empty");
            return;
        }
        foreach (var (cursedloot, idx) in _cursedLoot.Storage.WithIndex())
        {
            using var node = ImRaii.TreeNode($"{cursedloot.Label}##{idx}");
            if (!node)
                continue;
            DrawCursedLoot(cursedloot);
        }
    }

    public void DrawTriggerStorage()
    {
        if (!ImGui.CollapsingHeader("Trigger Storage"))
            return;
        if (_triggers.Storage.IsNullOrEmpty())
        {
            ImGui.TextUnformatted("Trigger Storage is null or empty");
            return;
        }
        foreach (var (trigger, idx) in _triggers.Storage.WithIndex())
        {
            using var node = ImRaii.TreeNode($"{trigger.Label}##{idx}");
            if (!node)
                continue;
            DrawTrigger(trigger);
        }
    }

    public void DrawPuppeteerGlobalStorage()
    {
        if (!ImGui.CollapsingHeader("Puppeteer Global Alias Storage"))
            return;
        if (_puppeteer.GlobalAliasStorage.Items.IsNullOrEmpty())
        {
            ImGui.TextUnformatted("Puppeteer Storage is null or empty");
            return;
        }
        foreach (var (alias, idx) in _puppeteer.GlobalAliasStorage.Items.WithIndex())
        {
            using var node = ImRaii.TreeNode($"{alias.Label}##{idx}");
            if (!node)
                continue;
            DrawAliasTrigger(alias);
        }
    }

    public void DrawPuppeteerPairStorage()
    {
        if (!ImGui.CollapsingHeader("Puppeteer Pair Alias Storage"))
            return;
        if (_puppeteer.PairAliasStorage.IsNullOrEmpty())
        {
            ImGui.TextUnformatted("Puppeteer Storage is null or empty");
            return;
        }

        foreach (var (alias, idx) in _puppeteer.PairAliasStorage.WithIndex())
        {
            using var pairNode = ImRaii.TreeNode($"{alias.Key}##{idx}");
            if (!pairNode)
                continue;
            var world = alias.Value.StoredNameWorld;
            var listener = alias.Value.ExtractedListenerName;
            foreach (var aliasTrigger in alias.Value.Storage.Items)
            {
                using var aliasNode = ImRaii.TreeNode($"{aliasTrigger.Label}##{idx}");
                if (!aliasNode)
                    continue;
                DrawAliasTrigger(aliasTrigger);
            }
        }

    }
    public void DrawPatternStorage()
    {
        if (!ImGui.CollapsingHeader("Pattern Storage"))
            return;
        if (_patterns.Storage.IsNullOrEmpty())
        {
            ImGui.TextUnformatted("Pattern Storage is null or empty");
            return;
        }
        foreach (var (pattern, idx) in _patterns.Storage.WithIndex())
        {
            using var node = ImRaii.TreeNode($"{pattern.Label}##{idx}");
            if (!node)
                continue;
            DrawPattern(pattern);
        }
    }
    public void DrawAlarmStorage()
    {
        if (!ImGui.CollapsingHeader("Alarm Storage"))
            return;
        if (_alarms.Storage.IsNullOrEmpty())
        {
            ImGui.TextUnformatted("Alarm Storage is null or empty");
            return;
        }
        foreach (var (alarm, idx) in _alarms.Storage.WithIndex())
        {
            using var node = ImRaii.TreeNode($"{alarm.Label}##{idx}");
            if (!node)
                continue;
            DrawAlarm(alarm);
        }
    }

    public void DrawRestraintSet(RestraintSet rs)
    {
        ImGui.TextUnformatted("Overview:");
        using (ImRaii.Table("##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGuiUtil.DrawTableColumn("Name");
            ImGuiUtil.DrawTableColumn(rs.Label);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Description (Hover)");
            ImGuiUtil.HoverTooltip(rs.Description);
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Identifier");
            ImGuiUtil.DrawTableColumn(rs.Identifier.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("RestraintSet File System Path");
            ImGuiUtil.DrawTableColumn(_restraintsFS.FindLeaf(rs, out var leaf) ? leaf.FullName() : "No Path Known");
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Assigned Thumbnail Path");
            ImGuiUtil.DrawTableColumn(rs.ThumbnailPath);
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Hardcore Traits");
            ImGuiUtil.DrawTableColumn(rs.Traits.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Arousal");
            ImGuiUtil.DrawTableColumn(rs.Arousal.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Do Redraw?");
            ImGuiUtil.DrawTableColumn(rs.DoRedraw ? "Yes" : "No");
            ImGui.TableNextRow();
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Equipment:");
        DrawRestraintEquipment(rs);

        ImGui.Spacing();
        ImGui.TextUnformatted("Layers:");
        DrawRestraintLayers(rs);

        ImGui.Spacing();
        ImGui.TextUnformatted("MetaData:");
        using (ImRaii.Table("##metadata", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGuiUtil.DrawTableColumn("Headgear");
            ImGuiUtil.DrawTableColumn(rs.HeadgearState.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Visor");
            ImGuiUtil.DrawTableColumn(rs.VisorState.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Weapon");
            ImGuiUtil.DrawTableColumn(rs.WeaponState.ToString());
            ImGui.TableNextRow();
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Mods:");
        using (ImRaii.Table("##mods", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            foreach (var mod in rs.GetMods())
                DrawModAssociationRow(mod);

        ImGui.Spacing();
        ImGui.TextUnformatted("Moodles:");
        using (ImRaii.Table("##moodles", 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            _moodleDrawer.ShowStatusIcons(rs.GetMoodles(), ImGui.GetContentRegionAvail().X, MoodleDrawer.IconSizeFramed, 2);
        }
    }

    public void DrawGag(GarblerRestriction gag)
    {
        ImGui.TextUnformatted("Overview:");
        using (ImRaii.Table("##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGuiUtil.DrawTableColumn("Gag Type");
            ImGuiUtil.DrawTableColumn(gag.GagType.GagName());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Gag FS Path");
            ImGuiUtil.DrawTableColumn(_gagFS.FindLeaf(gag, out var leaf) ? leaf.FullName() : "No Path Known");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Headgear State");
            ImGuiUtil.DrawTableColumn(gag.HeadgearState.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Show Visor?");
            ImGuiUtil.DrawTableColumn(gag.VisorState.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Do Redraw?");
            ImGuiUtil.DrawTableColumn(gag.DoRedraw ? "Yes" : "No");
            ImGui.TableNextRow();

            // Glamour.
            ImGuiUtil.DrawTableColumn("Glamour");
            ImGuiUtil.DrawTableColumn(gag.Glamour.GameItem.Name);
            ImGuiUtil.DrawTableColumn(gag.Glamour.GameItem.ItemId.ToString());
            ImGuiUtil.DrawTableColumn(gag.Glamour.Slot.ToName());
            ImGuiUtil.DrawTableColumn(gag.Glamour.GameStain.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Mod Association");
            if(gag.Mod.HasData)
            {
                ImGuiUtil.DrawTableColumn(gag.Mod.Container.ModName);
                ImGuiUtil.DrawTableColumn(gag.Mod.Label);
            }
            else
            {
                ImGuiUtil.DrawTableColumn("<No Assigned Data>");
            }
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Moodle");
            if (gag.Moodle is MoodlePreset preset)
            {
                ImGuiUtil.DrawTableColumn("[Preset Type]");
                ImGuiUtil.DrawTableColumn(MoodleCache.IpcData.Presets
                    .GetValueOrDefault(preset.Id).Title.StripColorTags() ?? "Unknown Preset");
                ImGui.TableNextRow();
            }
            else
            {
                ImGuiUtil.DrawTableColumn("[Status Type]");
                ImGuiUtil.DrawTableColumn(MoodleCache.IpcData.Statuses
                    .GetValueOrDefault(gag.Moodle.Id).Title.StripColorTags() ?? "Unknown Status");
                ImGui.TableNextRow();
            }

            ImGuiUtil.DrawTableColumn("Hardcore Traits");
            ImGuiUtil.DrawTableColumn(gag.Traits.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Arousal Strength");
            ImGuiUtil.DrawTableColumn(gag.Arousal.ToString());
        }
    }

    public void DrawRestriction(RestrictionItem restriction)
    {
        ImGui.TextUnformatted("Overview:");
        using (ImRaii.Table("##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGuiUtil.DrawTableColumn("Restriction Type");
            ImGuiUtil.DrawTableColumn(restriction.Type.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Name");
            ImGuiUtil.DrawTableColumn(restriction.Label);
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Identifier");
            ImGuiUtil.DrawTableColumn(restriction.Identifier.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Restrictions FS Path");
            ImGuiUtil.DrawTableColumn(_restrictionsFS.FindLeaf(restriction, out var leaf) ? leaf.FullName() : "No Path Known");
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Thumbnail Path");
            ImGuiUtil.DrawTableColumn(restriction.ThumbnailPath);
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Do Redraw?");
            ImGuiUtil.DrawTableColumn(restriction.DoRedraw ? "Yes" : "No");
            ImGui.TableNextRow();

            // Glamour.
            ImGuiUtil.DrawTableColumn("Glamour");
            ImGuiUtil.DrawTableColumn(restriction.Glamour.GameItem.Name);
            ImGuiUtil.DrawTableColumn(restriction.Glamour.GameItem.ItemId.ToString());
            ImGuiUtil.DrawTableColumn(restriction.Glamour.Slot.ToName());
            ImGuiUtil.DrawTableColumn(restriction.Glamour.GameStain.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Mod Association");
            ImGuiUtil.DrawTableColumn(restriction.Mod.Container.ModName);
            ImGuiUtil.DrawTableColumn(restriction.Mod.Label);
            ImGui.TableNextRow();

            if (restriction.Moodle is MoodlePreset preset)
            {
                ImGuiUtil.DrawTableColumn("Moodle Type");
                ImGuiUtil.DrawTableColumn("Moodle Preset");
                ImGuiUtil.DrawTableColumn(MoodleCache.IpcData.Presets
                    .GetValueOrDefault(preset.Id).Title.StripColorTags() ?? "Unknown Preset");
                ImGui.TableNextRow();
            }
            else
            {
                ImGuiUtil.DrawTableColumn("Moodle Type");
                ImGuiUtil.DrawTableColumn("Moodle Status");
                ImGuiUtil.DrawTableColumn(MoodleCache.IpcData.Statuses
                    .GetValueOrDefault(restriction.Moodle.Id).Title.StripColorTags() ?? "Unknown Status");
                ImGui.TableNextRow();
            }

            ImGuiUtil.DrawTableColumn("Hardcore Traits");
            ImGuiUtil.DrawTableColumn(restriction.Traits.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Arousal Strength");
            ImGuiUtil.DrawTableColumn(restriction.Arousal.ToString());
        }
    }

    private void DrawRestraintEquipment(RestraintSet rs)
    {
        using var node = ImRaii.TreeNode($"{rs.Label}-Equipment##{rs.Identifier}-Equipment");
        if (!node)
            return;

        using (ImRaii.Table("##equipment", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            foreach (var (slot, item) in rs.RestraintSlots)
            {
                if (item is RestraintSlotBasic basicSlot)
                {
                    ImGuiUtil.DrawTableColumn(slot.ToName());
                    ImGuiUtil.DrawTableColumn("Normal");
                    ImGuiUtil.DrawTableColumn($"{basicSlot.EquipItem.Name} ({basicSlot.EquipItem.ItemId}) ({basicSlot.Stains})");
                    ImGuiUtil.DrawTableColumn(item.ApplyFlags.ToString());
                    ImGui.TableNextRow();
                }
                else if (item is RestraintSlotAdvanced advancedSlot)
                {
                    ImGuiUtil.DrawTableColumn(slot.ToName());
                    ImGuiUtil.DrawTableColumn("Advanced");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("Reference (" + advancedSlot.Ref?.Identifier ?? "NULL REF" + ")");
                    if (ImGui.IsItemHovered() && advancedSlot.Ref != null)
                    {
                        ImGui.BeginTooltip();
                        DrawRestriction(advancedSlot.Ref);
                        ImGui.EndTooltip();
                    }
                    ImGuiUtil.DrawTableColumn(item.ApplyFlags.ToString());
                    ImGuiUtil.DrawTableColumn($"Custom Stains ({advancedSlot.CustomStains})");
                    ImGui.TableNextRow();
                }
                else
                {
                    ImGuiUtil.DrawTableColumn(slot.ToName());
                    ImGuiUtil.DrawTableColumn("Unknown Item");
                    ImGui.TableNextRow();
                }
                ImGui.Spacing();
            }
        }
    }

    private void DrawRestraintLayers(RestraintSet rs)
    {
        using var node = ImRaii.TreeNode($"{rs.Label}-Layers##{rs.Identifier}-Layers");
        if (!node)
            return;

        using (ImRaii.Table("##layers", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            var layerIdx = 0;
            foreach (var layer in rs.Layers)
            {
                if (layer is RestrictionLayer bindLayer)
                {
                    ImGuiUtil.DrawTableColumn("Layer" + layerIdx);
                    ImGuiUtil.DrawTableColumn(bindLayer.IsActive ? "Active" : "Inactive");
                    ImGuiUtil.DrawTableColumn(bindLayer.ID.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("Restriction");
                    ImGuiUtil.DrawTableColumn(bindLayer.Ref?.Identifier.ToString() ?? "NULL REFERENCE");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("Further Info (Hover)");
                    if (ImGui.IsItemHovered() && bindLayer.Ref != null)
                    {
                        ImGui.BeginTooltip();
                        DrawRestriction(bindLayer.Ref);
                        ImGui.EndTooltip();
                    }
                    ImGuiUtil.DrawTableColumn(bindLayer.ApplyFlags.ToString());
                    ImGui.TableNextRow();

                    ImGuiUtil.DrawTableColumn("Custom Stains");
                    ImGuiUtil.DrawTableColumn(bindLayer.CustomStains.ToString());
                    ImGui.TableNextRow();
                }
                else if (layer is ModPresetLayer modLayer)
                {
                    ImGuiUtil.DrawTableColumn("Layer" + layerIdx);
                    ImGuiUtil.DrawTableColumn(modLayer.IsActive ? "Active" : "Inactive");
                    ImGuiUtil.DrawTableColumn(modLayer.ID.ToString());
                    ImGui.TableNextRow();

                    ImGuiUtil.DrawTableColumn("ModLayer");
                    DrawModAssociationRow(modLayer.Mod);
                }
                else
                {
                    ImGuiUtil.DrawTableColumn("UNK LAYER DATA");
                    ImGui.TableNextRow();
                }
                ImGui.Spacing();
                layerIdx++;
            }
        }
    }

    private void DrawModAssociationRow(ModSettingsPreset mod)
    {
        ImGuiUtil.DrawTableColumn(mod.Container.ModName);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(mod.Label + "(Hover)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            _modPresetDrawer.DrawPresetPreview(mod);
            ImGui.EndTooltip();
        }
        ImGuiUtil.DrawTableColumn($"[{mod.Container.DirectoryPath}]");
        ImGui.TableNextRow();
    }
    private void DrawCursedLoot(CursedItem cursedLoot)
    {
        using (ImRaii.Table("##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGuiUtil.DrawTableColumn("Identifier");
            ImGuiUtil.DrawTableColumn(cursedLoot.Identifier.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Label");
            ImGuiUtil.DrawTableColumn(cursedLoot.Label.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("InPool");
            ImGuiUtil.DrawTableColumn(cursedLoot.InPool.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("AppliedTime");
            ImGuiUtil.DrawTableColumn(cursedLoot.AppliedTime.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("ReleaseTime");
            ImGuiUtil.DrawTableColumn(cursedLoot.ReleaseTime.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("CanOverride");
            ImGuiUtil.DrawTableColumn(cursedLoot.CanOverride.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Precedence");
            ImGuiUtil.DrawTableColumn(cursedLoot.Precedence.ToString());
            ImGui.TableNextRow();
            var restrictionRef = cursedLoot.RestrictionRef as RestrictionItem;
            if (restrictionRef == null)
            {
                ImGuiUtil.DrawTableColumn("Restriction");
                ImGuiUtil.DrawTableColumn("null");
                ImGui.TableNextRow();
            }
            else
            {
                DrawRestriction(restrictionRef!);
            }
        }
    }
    private void DrawTrigger(Trigger trigger)
    {
        using (ImRaii.Table("##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGuiUtil.DrawTableColumn("Identifier");
            ImGuiUtil.DrawTableColumn(trigger.Identifier.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Enabled");
            ImGuiUtil.DrawTableColumn(trigger.Enabled.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Priority");
            ImGuiUtil.DrawTableColumn(trigger.Priority.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Label");
            ImGuiUtil.DrawTableColumn(trigger.Label);
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Description");
            ImGuiUtil.DrawTableColumn(trigger.Description);
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Action Type");
            ImGuiUtil.DrawTableColumn(trigger.ActionType.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Invokable Action");
            ImGuiUtil.DrawTableColumn(trigger.InvokableAction.ToString());
        }
    }

    private void DrawPattern(Pattern pattern)
    {
        using (ImRaii.Table("##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGuiUtil.DrawTableColumn("Identifier");
            ImGuiUtil.DrawTableColumn(pattern.Identifier.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Label");
            ImGuiUtil.DrawTableColumn(pattern.Label);
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Description");
            ImGuiUtil.DrawTableColumn(pattern.Description);
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Duration");
            ImGuiUtil.DrawTableColumn(pattern.Duration.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("StartPoint");
            ImGuiUtil.DrawTableColumn(pattern.StartPoint.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("PlaybackDuration");
            ImGuiUtil.DrawTableColumn(pattern.PlaybackDuration.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("ShouldLoop");
            ImGuiUtil.DrawTableColumn(pattern.ShouldLoop.ToString());
        }
    }
    private void DrawAlarm(Alarm alarm)
    {
        using (ImRaii.Table("##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGuiUtil.DrawTableColumn("Identifier");
            ImGuiUtil.DrawTableColumn(alarm.Identifier.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Enabled");
            ImGuiUtil.DrawTableColumn(alarm.Enabled.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Label");
            ImGuiUtil.DrawTableColumn(alarm.Label);

            ImGuiUtil.DrawTableColumn("SetTimeUTC");
            ImGuiUtil.DrawTableColumn(alarm.SetTimeUTC.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("PatternToPlay");
            ImGuiUtil.DrawTableColumn(alarm.PatternRef.Identifier.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("PatternStartPoint");
            ImGuiUtil.DrawTableColumn(alarm.PatternStartPoint.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("PatternDuration");
            ImGuiUtil.DrawTableColumn(alarm.PatternDuration.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("RepeatFrequency");
            ImGuiUtil.DrawTableColumn(alarm.DaysToFire.ToString());
        }
    }

    private void DrawAliasTrigger(AliasTrigger alias)
    {
        using (ImRaii.Table("##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGuiUtil.DrawTableColumn("Identifier");
            ImGuiUtil.DrawTableColumn(alias.Identifier.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Enabled");
            ImGuiUtil.DrawTableColumn(alias.Enabled.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Label");
            ImGuiUtil.DrawTableColumn(alias.Label.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("InputCommand");
            ImGuiUtil.DrawTableColumn(alias.InputCommand.ToString());
            ImGui.TableNextRow();

        }

        using var node = ImRaii.TreeNode($"Actions##actions-{alias.Identifier}");
        if (!node)
            return;

        foreach (var (action, idx) in alias.Actions.WithIndex())
        {
            using var actionNode = ImRaii.TreeNode($"{action.ActionType}##{idx}");
            if (!actionNode)
                continue;
            using (ImRaii.Table($"##overview", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                DrawActionTable(action);
                ImGui.TableNextRow();
            }
        }
    }

    private void DrawActionTable(InvokableGsAction action)
    {
        if (action is TextAction text)
        {
            ImGuiUtil.DrawTableColumn("Type:");
            ImGuiUtil.DrawTableColumn("TextAction");
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("OutputCommand");
            ImGuiUtil.DrawTableColumn(text.OutputCommand.ToString());
        }
        else if (action is GagAction gag)
        {

            ImGuiUtil.DrawTableColumn("Type:");
            ImGuiUtil.DrawTableColumn("GagAction");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("LayerIdx");
            ImGuiUtil.DrawTableColumn(gag.LayerIdx.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("NewState");
            ImGuiUtil.DrawTableColumn(gag.NewState.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("GagType");
            ImGuiUtil.DrawTableColumn(gag.GagType.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Padlock");
            ImGuiUtil.DrawTableColumn(gag.Padlock.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("LowerBound");
            ImGuiUtil.DrawTableColumn(gag.LowerBound.ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("UpperBound");
            ImGuiUtil.DrawTableColumn(gag.UpperBound.ToString());
        }
        else if (action is RestrictionAction restriction)
        {
            ImGuiUtil.DrawTableColumn("Type:");
            ImGuiUtil.DrawTableColumn("RestrictionAction");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("LayerIdx");
            ImGuiUtil.DrawTableColumn(restriction.LayerIdx.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("NewState");
            ImGuiUtil.DrawTableColumn(restriction.NewState.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("RestrictionId");
            ImGuiUtil.DrawTableColumn(restriction.RestrictionId.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Padlock");
            ImGuiUtil.DrawTableColumn(restriction.Padlock.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("LowerBound");
            ImGuiUtil.DrawTableColumn(restriction.LowerBound.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("UpperBound");
            ImGuiUtil.DrawTableColumn(restriction.UpperBound.ToString());
        }
        else if (action is RestraintAction restrain)
        {
            ImGuiUtil.DrawTableColumn("Type:");
            ImGuiUtil.DrawTableColumn("RestraintAction");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("NewState");
            ImGuiUtil.DrawTableColumn(restrain.NewState.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("RestrictionId");
            ImGuiUtil.DrawTableColumn(restrain.RestrictionId.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Padlock");
            ImGuiUtil.DrawTableColumn(restrain.Padlock.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("LowerBound");
            ImGuiUtil.DrawTableColumn(restrain.LowerBound.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("UpperBound");
            ImGuiUtil.DrawTableColumn(restrain.UpperBound.ToString());
        }
        else if (action is MoodleAction moodle)
        {
            ImGuiUtil.DrawTableColumn("Type:");
            ImGuiUtil.DrawTableColumn("MoodleAction");
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("MoodleItem");
            ImGuiUtil.DrawTableColumn(moodle.MoodleItem.Id.ToString());
            // MoodleItem
        }
        else if (action is PiShockAction shock)
        {
            ImGuiUtil.DrawTableColumn("Type:");
            ImGuiUtil.DrawTableColumn("PiShockAction");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("ShockInstruction.OpCode");
            ImGuiUtil.DrawTableColumn(shock.ShockInstruction.OpCode.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("ShockInstruction.Duration");
            ImGuiUtil.DrawTableColumn(shock.ShockInstruction.Duration.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("ShockInstruction.Intensity");
            ImGuiUtil.DrawTableColumn(shock.ShockInstruction.Intensity.ToString());
        }
        else if (action is SexToyAction toy)
        {
            ImGuiUtil.DrawTableColumn("Type:");
            ImGuiUtil.DrawTableColumn("SexToyAction");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("StartAfter");
            ImGuiUtil.DrawTableColumn(toy.StartAfter.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("EndAfter");
            ImGuiUtil.DrawTableColumn(toy.EndAfter.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("ShockInstruction.Intensity");
        }
        else
        {

            // If it's not a type of action that we know of, we'll just brute for it so that we at least _have_ the data even if it's not pretty.
            ImGuiUtil.DrawTableColumn("Unknown Action");
            ImGuiUtil.DrawTableColumn(action.ToString());
        }
    }
}
