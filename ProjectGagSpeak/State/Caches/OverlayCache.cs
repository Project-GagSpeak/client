using Dalamud.Interface.Colors;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Gui;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using ImGuiNET;
using ImGuizmoNET;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface.Utility.Raii;
using OtterGui;
using GagSpeak.CkCommons;

namespace GagSpeak.State.Caches;

/// <summary>
///     Caches the current sources that have overlay items applied.
///     Helpful for the overlay controller and overlay services.
/// </summary>
public sealed class OverlayCache
{
    private readonly ILogger<OverlayCache> _logger;
    public OverlayCache(ILogger<OverlayCache> logger)
    {
        _logger = logger;
    }

    private SortedList<CombinedCacheKey, BlindfoldOverlay> _blindfolds = new();
    private SortedList<CombinedCacheKey, HypnoticOverlay> _hypnoEffects = new();
    // Might be better to turn these into structs or something but dont know.
    private KeyValuePair<CombinedCacheKey, BlindfoldOverlay>? _priorityBlindfold = null;
    private KeyValuePair<CombinedCacheKey, HypnoticOverlay>? _priorityEffect = null;

    // public accessors.
    public BlindfoldOverlay? ActiveBlindfold => _priorityBlindfold?.Value;
    public string? ActiveBlindfoldEnactor => _priorityBlindfold?.Key.EnactorUID;
    public HypnoticOverlay? ActiveEffect => _priorityEffect?.Value;
    public string? ActiveEffectEnactor => _priorityEffect?.Key.EnactorUID;

    public bool ShouldBeFirstPerson => (ActiveBlindfold?.ForceFirstPerson ?? false) || (ActiveEffect?.ForceFirstPerson ?? false);

    public void AddEffectPreview(HypnoticEffect effect, TimeSpan duration)
    {
        // idk maybe something here to temporarily active a preview effect i dunno lol.
    }


    /// <summary>
    ///     Adds a Blindfold <paramref name="overlay"/> to the cache with <paramref name="key"/>
    /// </summary>
    public bool TryAddBlindfold(CombinedCacheKey key, BlindfoldOverlay overlay)
    {
        if (!overlay.IsValid())
            return false;

        if (!_blindfolds.TryAdd(key, overlay))
        {
            _logger.LogWarning($"KeyValuePair ([{key}]) already exists in the Cache!");
            return false;
        }
        else
        {
            _logger.LogDebug($"Blindfold Overlay with key [{key}] added to Cache.");
            return true;
        }
    }

    /// <summary>
    ///     Adds a HypnoEffect <paramref name="overlay"/> to the cache with <paramref name="key"/>
    /// </summary>
    public bool TryAddHypnoEffect(CombinedCacheKey key, HypnoticOverlay overlay)
    {
        if (!overlay.IsValid())
            return false;

        if (!_hypnoEffects.TryAdd(key, overlay))
        {
            _logger.LogWarning($"KeyValuePair ([{key}]) already exists in the Cache!");
            return false;
        }
        else
        {
            _logger.LogDebug($"Hypnotic Effect with key [{key}] added to Cache.");
            return true;
        }
    }

    /// <summary>
    ///     Removes the <paramref name="combinedKey"/> from the cache.
    /// </summary>
    public bool TryRemoveBlindfold(CombinedCacheKey combinedKey)
    {
        if (_blindfolds.Remove(combinedKey, out var effect))
        {
            _logger.LogDebug($"Removed Blindfold Overlay from cache at key [{combinedKey}].");
            return true;
        }
        else
        {
            _logger.LogWarning($"Blindfold Cache key ([{combinedKey}]) not found!!");
            return false;
        }
    }

    /// <summary>
    ///     Removes the <paramref name="combinedKey"/> from the cache.
    /// </summary>
    public bool TryRemoveHypnoEffect(CombinedCacheKey combinedKey)
    {
        if (_hypnoEffects.Remove(combinedKey, out var effect))
        {
            _logger.LogDebug($"Removed Hypnotic Overlay from cache at key [{combinedKey}].");
            return true;
        }
        else
        {
            _logger.LogWarning($"Hypnotic Cache key ([{combinedKey}]) not found!!");
            return false;
        }
    }

    /// <summary>
    ///     Careful where and how you call this, use responsibly.
    ///     If done poorly, things will go out of sync.
    /// </summary>
    public void ClearCaches()
    {
        _blindfolds.Clear();
        _hypnoEffects.Clear();
    }

    /// <summary>
    ///     Updates the priority blindfold by finding the lowest priority blindfold. 
    /// </summary>
    /// <remarks> Remember, while others see the outermost blindfold, you see the innermost. </remarks>
    /// <returns> If the profile Changed. </returns>
    public bool UpdateFinalBlindfoldCache()
    {
        var newFinalItem = _blindfolds.LastOrDefault();
        var anyChange = !_priorityBlindfold?.Key.Equals(newFinalItem.Key) ?? true;
        _priorityBlindfold = newFinalItem;
        return anyChange;
    }

    /// <summary>
    ///     Updates the priority blindfold by finding the lowest priority blindfold. 
    /// </summary>
    /// <remarks> Outputs the previous effects enactor. If the effect was null, the string will be empty. </remarks>
    /// <returns> If the profile Changed. </returns>
    public bool UpdateFinalHypnoEffectCache([NotNullWhen(true)] out string prevEnactor)
    {
        var newFinalItem = _hypnoEffects.FirstOrDefault();
        var anyChange = !_priorityEffect?.Key.Equals(newFinalItem.Key) ?? true;
        prevEnactor = _priorityEffect?.Key.EnactorUID ?? string.Empty;
        _priorityEffect = newFinalItem;
        return anyChange;
    }


    #region DebugHelper
    public void DrawCacheTable()
    {
        using var display = ImRaii.Group();

        var iconSize = new Vector2(ImGui.GetFrameHeight());
        using (var node = ImRaii.TreeNode("Individual Blindfolds"))
        {
            if (node)
            {
                using (var t = ImRaii.Table("BlindfoldsCache", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (!t)
                        return;

                    ImGui.TableSetupColumn("Combined Key");
                    ImGui.TableSetupColumn("Enactor");
                    ImGui.TableSetupColumn("ImagePath");
                    ImGui.TableSetupColumn("1st PoV Forced?");
                    ImGui.TableHeadersRow();
                    foreach (var (key, overlay) in _blindfolds)
                    {
                        ImGuiUtil.DrawFrameColumn($"{key.Manager} / {key.LayerIndex}");
                        ImGuiUtil.DrawFrameColumn(key.EnactorUID);
                        ImGuiUtil.DrawFrameColumn(string.IsNullOrWhiteSpace(overlay.OverlayPath) ? "<No Image Path Set>" : overlay.OverlayPath);
                        ImGui.TableNextColumn();
                        CkGui.BooleanToColoredIcon(overlay.ForceFirstPerson);
                    }
                }
            }
        }

        using (var node = ImRaii.TreeNode("Individual Hypnotic Effects"))
        {
            if (node)
            {
                using (var t = ImRaii.Table("HypnoEffectsCache", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (!t)
                        return;
                    ImGui.TableSetupColumn("Combined Key");
                    ImGui.TableSetupColumn("Enactor");
                    ImGui.TableSetupColumn("ImagePath");
                    ImGui.TableSetupColumn("1st PoV Forced?");
                    ImGui.TableHeadersRow();
                    foreach (var (key, overlay) in _hypnoEffects)
                    {
                        ImGuiUtil.DrawFrameColumn($"{key.Manager} / {key.LayerIndex}");
                        ImGuiUtil.DrawFrameColumn(key.EnactorUID);
                        ImGuiUtil.DrawFrameColumn(string.IsNullOrWhiteSpace(overlay.OverlayPath) ? "<No Image Path Set>" : overlay.OverlayPath);
                        ImGui.TableNextColumn();
                        CkGui.BooleanToColoredIcon(overlay.ForceFirstPerson);
                    }
                }
            }
        }

        ImGui.Separator();
        ImGui.Text("Final Blindfold: ");
        if (_priorityBlindfold is { } validBf)
        {
            CkGui.ColorTextInline($"[{validBf.Key.ToString()}]", CkGui.Color(ImGuiColors.HealerGreen));
            CkGui.TextInline($" Enactor: {validBf.Key.EnactorUID}");
            CkGui.TextInline($" Overlay: {validBf.Value.OverlayPath}");
        }
        else
        {
            CkGui.ColorTextInline("<No Blindfold Applied>", CkGui.Color(ImGuiColors.DalamudRed));
        }

        ImGui.Text("Final Hypnotic Effect: ");
        if (_priorityEffect is { } validHypno)
        {
            CkGui.ColorTextInline($"[{validHypno.Key.ToString()}]", CkGui.Color(ImGuiColors.HealerGreen));
            CkGui.TextInline($" Enactor: {validHypno.Key.EnactorUID}");
            CkGui.TextInline($" Overlay: {validHypno.Value.OverlayPath}");
        }
        else
        {
            CkGui.ColorTextInline("<No Hypnotic Effect Applied>", CkGui.Color(ImGuiColors.DalamudRed));
        }
    }
    #endregion Debug Helper
}
