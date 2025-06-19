using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using ImGuiNET;

namespace GagSpeak.State.Caches;

/// <summary>
///     Stores the expected mods and their respective modSettings to keep as temporary mods while restricted.
/// </summary>
public class ModCache
{
    private readonly ILogger<ModCache> Logger;
    public ModCache(ILogger<ModCache> logger)
    {
        Logger = logger;
    }

    // This is really difficult for me, because the key is for the most part out of my control.
    // If at any point the mod name is changed in penumbra, it would damage the storage in gagspeak...
    // So unless I could resync the cache on every mod name change, this would be difficult to pull off.
    private SortedList<(CombinedCacheKey, Guid), ModSettingsPreset> _mods = new();
    private HashSet<ModSettingsPreset> _finalMods = new();
    public IReadOnlySet<ModSettingsPreset> FinalMods => _finalMods;

    /// <summary> Applies a <paramref name="mod"/> to the Mods Cache under <paramref name="key"/>. </summary>
    public bool AddMod(CombinedCacheKey key, ModSettingsPreset mod)
        => AddMod(key, [mod]);

    /// <summary> Applies all <paramref name="mods"/> to the Mods Cache under <paramref name="key"/>. </summary>
    public bool AddMod(CombinedCacheKey key, IEnumerable<ModSettingsPreset> mods)
    {
        if (_mods.Keys.Any(keys => keys.Item1.Equals(key)))
        {
            Logger.LogWarning($"Cannot add [{key}] to Cache, the Key already exists!");
            return false;
        }

        bool added = false;
        foreach (var mod in mods)
        {
            if (!mod.HasData) continue;
            added |= _mods.TryAdd((key, mod.Id), mod);
            if(added) Logger.LogDebug($"Added ModCache ([{key}]-[{mod.Id}]) -> [{mod.Label}] from [{mod.Container.ModName}]");
        }
        return added;
    }

    /// <summary> Removes the <paramref name="key"/> from the Mods Cache. </summary>
    public bool RemoveMod(CombinedCacheKey key)
        => RemoveMod([key]);

    /// <summary> Removes all <paramref name="keys"/> from the Mods Cache. </summary>
    public bool RemoveMod(IEnumerable<CombinedCacheKey> keys)
    {
        var allKeys = _mods.Keys.Where(k => keys.Contains(k.Item1)).ToList();
        if (!allKeys.Any())
        {
            Logger.LogWarning($"None of the CombinedKeys were found in the ModCache!");
            return false;
        }

        // Remove all glamours for the combined key.
        bool anyRemoved = false;
        foreach (var key in allKeys)
        {
            Logger.LogDebug($"Removing GlamourCache key ([{key.Item1}]-[{key.Item2}])");
            anyRemoved |= _mods.Remove(key);
        }
        return anyRemoved;
    }

    /// <summary>
    ///     Careful where and how you call this, use responsibly.
    ///     If done poorly, things will go out of sync.
    /// </summary>
    public void ClearCache()
        => _mods.Clear();

    public bool UpdateFinalCache(out List<ModSettingsPreset> removed)
    {
        var prevMods = _finalMods.ToList();
        var anyChange = false;
        _finalMods.Clear();
        // Cycle through the glamours in the order they are sorted in.
        // Once a mod is added, any further presets with the same source mod won't be added.
        foreach (var modItem in _mods.Values)
            anyChange |= _finalMods.Add(modItem);

        // output the mods that were removed as well.
        removed = prevMods.Except(_finalMods).ToList();
        return anyChange || removed.Any();
    }

    #region DebugHelper
    public void DrawCacheTable(TextureService textures, ModPresetDrawer drawer)
    {
        using var display = ImRaii.Group();

        var iconSize = new Vector2(ImGui.GetFrameHeight());
        using (var node = ImRaii.TreeNode("Individual Mod Listings"))
        {
            if (node)
            {
                using (var table = ImRaii.Table("ModsCache", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (!table)
                        return;

                    ImGui.TableSetupColumn("Combined Key");
                    ImGui.TableSetupColumn("Mod Name");
                    ImGui.TableSetupColumn("Container Mod");
                    ImGui.TableSetupColumn("##Settings", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
                    ImGui.TableHeadersRow();

                    var grouped = _mods.GroupBy(kvp => kvp.Key.Item1); // Group by CombinedCacheKey

                    foreach (var group in grouped)
                    {
                        var firstRow = true;

                        foreach (var ((_, presetKey), mod) in group)
                        {
                            ImGui.TableNextRow();

                            if (firstRow)
                            {
                                // Column 1: Combined Key info (only once)
                                ImGui.TableSetColumnIndex(0);
                                ImGui.Text($"{group.Key.Manager} / {group.Key.LayerIndex}");
                                firstRow = false;
                            }
                            else
                            {
                                // Leave Column 1 empty for subsequent rows
                                ImGui.TableSetColumnIndex(0);
                                ImGui.Text("");
                            }

                            // Column 2: Preset Label
                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text(string.IsNullOrEmpty(mod.Label) ? "<No Label Set>" : mod.Label);

                            // Column 3: Container Mod Name
                            ImGui.TableSetColumnIndex(2);
                            var modName = mod.Container?.ModName ?? "<No Mod Source>";
                            CkGui.ColorText(modName, ImGuiColors.DalamudGrey);

                            // Column 4: Settings Preview / Tooltip Icon
                            ImGui.TableSetColumnIndex(3);
                            CkGui.IconText(FAI.QuestionCircle, ImGuiColors.TankBlue);
                            if (ImGui.IsItemHovered())
                                drawer.DrawPresetTooltip(mod);
                        }
                    }
                }
            }
        }

        CkGui.ColorText("Final Mods Cache", ImGuiColors.DalamudRed);
        ImGui.Separator();
        using (var table = ImRaii.Table("FinalModsCache", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
        {
            if (!table)
                return;

            ImGui.TableSetupColumn("Combined Key");
            ImGui.TableSetupColumn("Mod Name");
            ImGui.TableSetupColumn("Container Mod");
            ImGui.TableSetupColumn("##Settings", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
            ImGui.TableHeadersRow();

            if (_finalMods.Count > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Final State");

                foreach (var final in _finalMods)
                {
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(string.IsNullOrEmpty(final.Label) ? "<No Label Set>" : final.Label);

                    ImGui.TableSetColumnIndex(2);
                    var modName = final.Container?.ModName ?? "<No Mod Source>";
                    CkGui.ColorText(modName, ImGuiColors.DalamudGrey);

                    ImGui.TableSetColumnIndex(3);
                    CkGui.IconText(FAI.Check, ImGuiColors.HealerGreen);
                    if (ImGui.IsItemHovered())
                        drawer.DrawPresetTooltip(final);
                }
            }
        }
    }
    #endregion Debug Helper
}
