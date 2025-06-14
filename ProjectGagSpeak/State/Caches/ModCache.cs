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


    /// <summary>Applies a <paramref name="mod"/> with <paramref name="combinedKey"/> to <see cref="_mods"/> Cache.</summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalMods"/></b></remarks>
    public void AddMod(CombinedCacheKey combinedKey, ModSettingsPreset mod)
        => AddMod(combinedKey, [mod]);

    /// <summary>Applies all <paramref name="mods"/> with <paramref name="combinedKey"/> to <see cref="_mods"/> Cache.</summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalMods"/></b></remarks>
    public void AddMod(CombinedCacheKey combinedKey, IEnumerable<ModSettingsPreset> mods)
    {
        if (_mods.Keys.Any(keys => keys.Item1.Equals(combinedKey)))
        {
            Logger.LogWarning($"Cannot add [{combinedKey}] to Cache, the Key already exists!");
            return;
        }

        foreach (var item in mods)
        {
            if (!item.HasData) continue;

            if (_mods.TryAdd((combinedKey, item.Id), item))
                Logger.LogWarning($"KeyValuePair ([{combinedKey}]-[{item.Id}]) already exists in the Cache!");
            else
                Logger.LogDebug($"Added KeyValuePair ([{combinedKey}]-[{item.Id}]) -> ([{item.Label}]-[{item.Container.ModName}]) to Cache.");
        }
    }

    /// <summary>
    ///     Applies a <paramref name="mod"/> with <paramref name="combinedKey"/> to the <see cref="_mods"/> Cache,
    ///     then updates <see cref="_finalMods"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    public bool AddAndUpdateMod(CombinedCacheKey combinedKey, ModSettingsPreset mod)
        => AddAndUpdateMod(combinedKey, [mod]);

    /// <summary>
    ///     Applies <paramref name="mods"/> with <paramref name="combinedKey"/> to the <see cref="_mods"/> Cache,
    ///     then updates <see cref="_finalMods"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    public bool AddAndUpdateMod(CombinedCacheKey combinedKey, IEnumerable<ModSettingsPreset> mods)
    {
        if (_mods.Keys.Any(keys => keys.Item1.Equals(combinedKey)))
        {
            Logger.LogWarning($"Cannot add [{combinedKey}] to Cache, the Key already exists!");
            return false;
        }

        AddMod(combinedKey, mods);
        return UpdateFinalCache();
    }

    /// <summary>
    ///     Removes all <see cref="CombinedCacheKey"/>'s using any of the <paramref name="combinedKey"/>
    /// </summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalMods"/></b></remarks>
    public void RemoveMod(CombinedCacheKey combinedKey)
        => RemoveMod([combinedKey]);

    /// <summary>
    ///     Removes all <see cref="CombinedCacheKey"/>'s using <paramref name="combinedKeys"/>
    /// </summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalMods"/></b></remarks>
    public void RemoveMod(List<CombinedCacheKey> combinedKeys)
    {
        var keys = _mods.Keys.Where(k => combinedKeys.Contains(k.Item1)).ToList();
        if (!keys.Any())
        {
            Logger.LogWarning($"None of the CombinedKeys were found in the ModCache!");
            return;
        }

        // Remove all glamours for the combined key.
        foreach (var key in keys)
        {
            Logger.LogDebug($"Removing GlamourCache key ([{key.Item1}]-[{key.Item2}])");
            _mods.Remove(key);
        }
    }

    /// <summary> 
    ///     Removes all <see cref="CombinedCacheKey"/>'s using <paramref name="combinedKey"/>, 
    ///     then updates the <see cref="_finalMods"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    /// <remarks> The removed mods Id's are collected in <paramref name="removed"/></remarks>
    public bool RemoveAndUpdateMod(CombinedCacheKey combinedKey, out List<ModSettingsPreset> removed)
        => RemoveAndUpdateMod([combinedKey], out removed);


    /// <summary> 
    ///     Removes all <see cref="CombinedCacheKey"/>'s using <paramref name="combinedKeys"/>, 
    ///     then updates the <see cref="_finalMods"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    /// <remarks> The removed mods Id's are collected in <paramref name="removed"/></remarks>
    public bool RemoveAndUpdateMod(List<CombinedCacheKey> combinedKeys, out List<ModSettingsPreset> removed)
    {
        var prevMods = _finalMods.ToList();

        // Remove all mods for the combined keys.
        RemoveMod(combinedKeys);

        var changes = UpdateFinalCache();
        removed = prevMods.Except(_finalMods).ToList();
        return changes;
    }

    private bool UpdateFinalCache()
    {
        var anyChange = false;
        _finalMods.Clear();

        var seenMods = new HashSet<ModSettingsPreset>();
        // Cycle through the glamours in the order they are sorted in.
        foreach (var modItem in _mods.Values)
        {
            // Try and add the mod, if we can't add it, we have already seen it.
            if (!_finalMods.Add(modItem))
                continue;

            anyChange = true;
        }

        return anyChange;
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
