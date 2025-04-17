using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Localization;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.PlayerState.Visual;
using GagSpeak.RestraintSets;
using GagSpeak.Services.Configs;
using GagSpeak.UI.Components;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Enums;

namespace GagSpeak.UI;

public class DebuggerBinds
{
    private readonly GlobalData _playerData;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CursedLootManager _cursedLoot;
    private readonly TriggerManager _triggers;
    private readonly RestrictionFileSystem _restrictionsFS;
    private readonly RestraintSetFileSystem _restraintsFS;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly ModPresetDrawer _modPresetDrawer;
    public DebuggerBinds(
        GlobalData playerData,
        RestrictionManager restrictions,
        RestraintManager restraints,
        CursedLootManager cursedLoot,
        TriggerManager triggers,
        RestrictionFileSystem restrictionsFS,
        RestraintSetFileSystem restraintsFS,
        MoodleDrawer moodleDrawer,
        ModPresetDrawer modPresetDrawer)
    {
        _playerData = playerData;
        _restrictions = restrictions;
        _restraints = restraints;
        _cursedLoot = cursedLoot;
        _triggers = triggers;
        _restrictionsFS = restrictionsFS;
        _restraintsFS = restraintsFS;
        _moodleDrawer = moodleDrawer;
        _modPresetDrawer = modPresetDrawer;
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
                    foreach (var req in _playerData.IncomingRequests)
                    {
                        ImGui.TableNextRow();
                        ImGuiUtil.DrawTableColumn(req.User.UID.ToString());
                        ImGuiUtil.DrawTableColumn(req.RecipientUser.UID.ToString());
                        ImGuiUtil.DrawTableColumn(req.AttachedMessage.ToString());
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
                    foreach (var req in _playerData.OutgoingRequests)
                    {
                        ImGui.TableNextRow();
                        ImGuiUtil.DrawTableColumn(req.User.UID.ToString());
                        ImGuiUtil.DrawTableColumn(req.RecipientUser.UID.ToString());
                        ImGuiUtil.DrawTableColumn(req.AttachedMessage.ToString());
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
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ChatGarblerChannelsBitfield.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ChatGarblerActive:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ChatGarblerActive.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ChatGarblerLocked:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ChatGarblerLocked.ToString());
                    ImGui.TableNextRow();

                    // wardrobe global modifiable permissions
                    ImGuiUtil.DrawTableColumn("WardrobeEnabled:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.WardrobeEnabled.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("GagVisuals:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.GagVisuals.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("RestrictionVisuals:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.RestrictionVisuals.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("RestraintSetVisuals:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.RestraintSetVisuals.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("PuppeteerEnabled:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.PuppeteerEnabled.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("TriggerPhrase:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.TriggerPhrase.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("PuppetPerms:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.PuppetPerms.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ToyboxEnabled:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ToyboxEnabled.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("LockToyboxUI:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.LockToyboxUI.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ToysAreConnected:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ToysAreConnected.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ToysAreInUse:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ToysAreInUse.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("SpatialAudio:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.SpatialAudio.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ForcedFollow:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ForcedFollow.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ForcedEmoteState:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ForcedEmoteState.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ForcedStay:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ForcedStay.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ChatBoxesHidden:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ChatBoxesHidden.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ChatInputHidden:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ChatInputHidden.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ChatInputBlocked:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ChatInputBlocked.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("GlobalShockShareCode:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.GlobalShockShareCode.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("AllowShocks:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.AllowShocks.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("AllowVibrations:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.AllowVibrations.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("AllowBeeps:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.AllowBeeps.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("MaxIntensity:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.MaxIntensity.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("MaxDuration:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.MaxDuration.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("ShockVibrateDuration:");
                    ImGuiUtil.DrawTableColumn(_playerData.GlobalPerms?.ShockVibrateDuration.ToString());
                    ImGui.TableNextRow();
                    ImGuiUtil.DrawTableColumn("User");
                    ImGuiUtil.DrawTableColumn("RecipientUser");
                    ImGuiUtil.DrawTableColumn("AttachedMessage");
                    ImGuiUtil.DrawTableColumn("CreationTime");
                }
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
            ImGuiUtil.DrawTableColumn("Stimulation");
            ImGuiUtil.DrawTableColumn(rs.Stimulation.ToString());
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
            foreach (var mod in rs.RestraintMods)
                DrawModAssociationRow(mod);

        ImGui.Spacing();
        ImGui.TextUnformatted("Moodles:");
        using (ImRaii.Table("##moodles", 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            foreach (var moodleItem in rs.RestraintMoodles)
            {
                _moodleDrawer.DrawMoodles(moodleItem, MoodleDrawer.IconSize);
                ImGui.TableNextRow();
            }
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

            ImGuiUtil.DrawTableColumn("Moodle Type");
            ImGuiUtil.DrawTableColumn(restriction.Moodle is MoodlePreset ? "Moodle Preset" : "Moodle Status");
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Moodle Id");
            ImGuiUtil.DrawTableColumn(restriction.Moodle.Id.ToString());
            ImGui.TableNextRow();
            if (restriction.Moodle is MoodlePreset preset)
            {
                ImGuiUtil.DrawTableColumn("Moodle Status Ids");
                ImGuiUtil.DrawTableColumn(string.Join(", ", preset.StatusIds));
                ImGui.TableNextRow();
            }

            ImGuiUtil.DrawTableColumn("Hardcore Traits");
            ImGuiUtil.DrawTableColumn(restriction.Traits.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Hardcore Stimulation");
            ImGuiUtil.DrawTableColumn(restriction.Stimulation.ToString());
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
                    ImGuiUtil.DrawTableColumn(bindLayer.ApplyFlags.ToSplitFlagString());
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
            } else {
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
}
