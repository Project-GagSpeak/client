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
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Enums;

namespace GagSpeak.UI;

public class DebuggerBinds
{
    private readonly GlobalData _playerData;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly RestrictionFileSystem _restrictionsFS;
    private readonly RestraintSetFileSystem _restraintsFS;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly ModPresetDrawer _modPresetDrawer;
    public DebuggerBinds(
        GlobalData playerData,
        RestrictionManager restrictions,
        RestraintManager restraints,
        RestrictionFileSystem restrictionsFS,
        RestraintSetFileSystem restraintsFS,
        MoodleDrawer moodleDrawer,
        ModPresetDrawer modPresetDrawer)
    {
        _playerData = playerData;
        _restrictions = restrictions;
        _restraints = restraints;
        _restrictionsFS = restrictionsFS;
        _restraintsFS = restraintsFS;
        _moodleDrawer = moodleDrawer;
        _modPresetDrawer = modPresetDrawer;
    }

    public void DrawRestrictionStorage()
    {
        if (!ImGui.CollapsingHeader("Restriction Storage"))
            return;

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

        foreach (var (restraintSet, idx) in _restraints.Storage.WithIndex())
        {
            using var node = ImRaii.TreeNode($"{restraintSet.Label}##{idx}");
            if (!node)
                continue;

            DrawRestraintSet(restraintSet);
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
}
