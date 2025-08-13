using CkCommons;
using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using Dalamud.Bindings.ImGui;

namespace GagSpeak.CustomCombos.Pairs;

public sealed class PairRestraintCombo : CkFilterComboButton<KinksterRestraint>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _pairRef;
    private LayerFlagsWidget _layersHelper = new(FAI.LayerGroup, "RestraintLayers", "Select Layers..");

    public PairRestraintCombo(ILogger log, MainHub hub, Kinkster pair, Action postButtonPress)
        : base(() => [ ..pair.LightCache.Restraints.Values ], log)
    {
        _mainHub = hub;
        _pairRef = pair;
        PostButtonPress = postButtonPress;
        _layersHelper = new(FAI.LayerGroup, "RestraintLayers", "Select Layers..");
        Current = _pairRef.LightCache.Restraints.GetValueOrDefault(_pairRef.ActiveRestraint.Identifier);
    }

    protected override bool DisableCondition()
        => Current is null || !_pairRef.PairPerms.ApplyRestraintSets || _pairRef.ActiveRestraint.Identifier.Equals(Current?.Id);

    protected override string ToString(KinksterRestraint obj)
        => obj.Label.IsNullOrWhitespace() ? $"UNK SET NAME" : obj.Label;

    public bool DrawComboButton(string label, float width, string buttonTT)
        => DrawComboButton(label, width, -1, "Apply", buttonTT);

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalAlarmIdx, bool selected)
    {
        var restraintSet = Items[globalAlarmIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(restraintSet.Label, selected);

        if (restraintSet.IsEnabled)
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconSize(FAI.InfoCircle).X);
            CkGui.IconText(FAI.InfoCircle, ImGuiColors.ParsedGold.ToUint());
            DrawItemTooltip(restraintSet);
            ImGui.SameLine();
        }

        return ret;
    }

    protected override void OnButtonPress(int _)
    {
        // we need to go ahead and create a deep clone of our new appearanceData, and ensure it is valid.
        if (Current is null)
            return;

        var updateType = _pairRef.ActiveRestraint.Identifier== Guid.Empty
            ? DataUpdateType.Applied : DataUpdateType.Swapped;
        // construct the dto to send.
        var dto = new PushKinksterActiveRestraint(_pairRef.UserData, updateType)
        {
            ActiveSetId = Current.Id,
            Enabler = MainHub.UID,
        };

        UiService.SetUITask(async () =>
        {
            var result = await _mainHub.UserChangeKinksterActiveRestraint(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogError($"Failed to Perform PairRestraint action to {_pairRef.GetNickAliasOrUid()} : {result.ErrorCode}", LoggerType.StickyUI);
            }
            else
            {
                Log.LogDebug("Applying Restraint Set " + Current.Label + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.StickyUI);
                PostButtonPress?.Invoke();
            }
        });
    }

    public void DrawApplyLayersComboButton(float width)
    {
        if (_pairRef.ActiveRestraint.Identifier == Guid.Empty)
        {
            PostButtonPress?.Invoke();
            return;
        }

        if (!_pairRef.LightCache.Restraints.TryGetValue(_pairRef.ActiveRestraint.Identifier, out var cacheInfo))
        {
            ImGui.Text("No Valid CacheInfo for active Restraint Set!");
            return;
        }

        // Valid for draw:
        var layers = cacheInfo.Layers.Count;
        var options = Enum.GetValues<RestraintLayer>().Skip(1).SkipLast(5 - layers + 1);
        if (_layersHelper.DrawApply(width, _pairRef.ActiveRestraint.ActiveLayers, out var changes, options, GetLabel))
        {
            // we need to ensure that we have a valid pairRef and that the changes are not empty.
            if (_pairRef is null || changes == RestraintLayer.None)
                return;

            UiService.SetUITask(async () =>
            {
                // if we have changes, we will apply them.
                var newLayers = _pairRef.ActiveRestraint.ActiveLayers | changes;
                var dto = new PushKinksterActiveRestraint(_pairRef.UserData, DataUpdateType.LayersApplied) { ActiveLayers = newLayers };
                var result = await _mainHub.UserChangeKinksterActiveRestraint(dto);
                if (result.ErrorCode is not GagSpeakApiEc.Success)
                {
                    Log.LogError($"Failed to Perform ApplyLayer action to {_pairRef.GetNickAliasOrUid()} : {result}");
                    Svc.Toasts.ShowError(result.ErrorCode switch
                    {
                        GagSpeakApiEc.BadUpdateKind => "Invalid Update Kind. Please try again.",
                        GagSpeakApiEc.InvalidLayer => "Attempted to apply to a layer that was invalid.",
                        GagSpeakApiEc.LackingPermissions => "You do not have permission to perform this action.",
                        GagSpeakApiEc.NoActiveItem => "No active item is present.",
                        _ => $"UNK ApplyLayer Error: {result.ErrorCode}."
                    });
                }
                else
                {
                    Log.LogDebug($"Applied Layers to {_pairRef.GetNickAliasOrUid()}'s Restraint Set.", LoggerType.StickyUI);
                    PostButtonPress?.Invoke();
                }
            });
        }

        string GetLabel(RestraintLayer layer)
        {
            var idx = BitOperations.TrailingZeroCount((int)layer);
            return idx < layers ? cacheInfo.Layers[idx].Label : $"Layer {idx + 1} (Unknown Contents)";
        }
    }

    public void DrawRemoveLayersComboButton(float width)
    {
        if (_pairRef.ActiveRestraint.Identifier == Guid.Empty)
        {
            PostButtonPress?.Invoke();
            return;
        }

        if (!_pairRef.LightCache.Restraints.TryGetValue(_pairRef.ActiveRestraint.Identifier, out var cacheInfo))
        {
            ImGui.Text("No Valid CacheInfo for active Restraint Set!");
            return;
        }

        // Valid for draw:
        var layers = cacheInfo.Layers.Count;
        var options = Enum.GetValues<RestraintLayer>().Skip(1).SkipLast(5 - layers + 1);
        if (_layersHelper.DrawRemove(width, _pairRef.ActiveRestraint.ActiveLayers, out var changes, options, GetLabel))
        {
            UiService.SetUITask(async () =>
            {
                // we need to ensure that we have a valid pairRef and that the changes are not empty.
                if (_pairRef is null || changes == RestraintLayer.None)
                    return;
                // if we have changes, we will apply them.
                var newLayers = _pairRef.ActiveRestraint.ActiveLayers & ~changes;
                var dto = new PushKinksterActiveRestraint(_pairRef.UserData, DataUpdateType.LayersRemoved) { ActiveLayers = newLayers };
                var result = await _mainHub.UserChangeKinksterActiveRestraint(dto);
                if (result.ErrorCode is not GagSpeakApiEc.Success)
                {
                    Log.LogError($"Failed to Perform RemoveLayer action to {_pairRef.GetNickAliasOrUid()} : {result}");
                    Svc.Toasts.ShowError(result.ErrorCode switch
                    {
                        GagSpeakApiEc.BadUpdateKind => "Invalid Update Kind. Please try again.",
                        GagSpeakApiEc.InvalidLayer => "Attempted to remove a layer that was invalid.",
                        GagSpeakApiEc.LackingPermissions => "You do not have permission to perform this action.",
                        GagSpeakApiEc.NoActiveItem => "No active item is present.",
                        _ => $"UNK RemoveLayer Error: {result.ErrorCode}."
                    });
                }
                else
                {
                    Log.LogDebug($"Removed Layers from {_pairRef.GetNickAliasOrUid()}'s Restraint Set", LoggerType.StickyUI);
                    PostButtonPress?.Invoke();
                }
            });
        }

        string GetLabel(RestraintLayer layer)
        {
            var idx = BitOperations.TrailingZeroCount((int)layer);
            return idx < layers ? cacheInfo.Layers[idx].Label : $"Layer {idx + 1} (Unknown Contents)";
        }
    }

    private void DrawItemTooltip(KinksterRestraint setItem)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
                .Push(ImGuiStyleVar.WindowRounding, 4f)
                .Push(ImGuiStyleVar.PopupBorderSize, 1f);
            using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
            
            ImGui.BeginTooltip();

            if (!setItem.Description.IsNullOrWhitespace() && !setItem.Description.Contains("Enter Description Here..."))
                CkGui.TextWrappedTooltipFormat(setItem.Description, 35f, ImGuiColors.ParsedPink);

            ImGui.EndTooltip();
        }
    }
}

