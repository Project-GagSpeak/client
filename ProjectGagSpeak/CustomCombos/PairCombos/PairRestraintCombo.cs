using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Pairs;

public sealed class PairRestraintCombo : CkFilterComboButton<KinksterRestraint>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _pairRef;
    private LayerFlagsComboButton _layersHelper = new(FAI.LayerGroup, "RestraintLayers", "Select Layers..");

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
        
        var iconWidth = CkGui.IconSize(FAI.InfoCircle).X;
        var hasGlamour = restraintSet.SlotData.Any();
        var hasInfo = !restraintSet.Description.IsNullOrWhitespace();
        var shiftOffset = hasInfo ? iconWidth * 2 + ImGui.GetStyle().ItemSpacing.X : iconWidth;

        // shift over to the right to draw out the icons.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - shiftOffset);

        if (hasInfo)
        {
            CkGui.IconText(FAI.InfoCircle, ImGui.GetColorU32(ImGuiColors.ParsedGold));
            DrawItemTooltip(restraintSet);
            ImGui.SameLine();
        }

        // icon for the glamour preview.
        CkGui.IconText(FAI.Tshirt, ImGui.GetColorU32(hasGlamour ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey));
        // if (hasGlamour) _ttPreview.DrawLightRestraintOnHover(restraintItem);
        return ret;
    }

    protected override async Task<bool> OnButtonPress(int _)
    {
        // we need to go ahead and create a deep clone of our new appearanceData, and ensure it is valid.
        if (Current is null)
            return false;

        var updateType = _pairRef.ActiveRestraint.Identifier== Guid.Empty
            ? DataUpdateType.Applied : DataUpdateType.Swapped;
        // construct the dto to send.
        var dto = new PushKinksterActiveRestraint(_pairRef.UserData, updateType)
        {
            ActiveSetId = Current.Id,
            Enabler = MainHub.UID,
        };

        var result = await _mainHub.UserChangeKinksterActiveRestraint(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogError($"Failed to Perform PairRestraint action to {_pairRef.GetNickAliasOrUid()} : {result}");
            return false;
        }
        else
        {
            Log.LogDebug("Applying Restraint Set " + Current.Label + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.StickyUI);
            PostButtonPress?.Invoke();
            return true;
        }
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
            UiService.SetUITask(async () =>
            {
                // we need to ensure that we have a valid pairRef and that the changes are not empty.
                if (_pairRef is null || changes == RestraintLayer.None)
                    return;
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
            int idx = BitOperations.TrailingZeroCount((int)layer);
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
            int idx = BitOperations.TrailingZeroCount((int)layer);
            return idx < layers ? cacheInfo.Layers[idx].Label : $"Layer {idx + 1} (Unknown Contents)";
        }
    }

    private void DrawItemTooltip(KinksterRestraint setItem)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            // begin the tooltip interface
            ImGui.BeginTooltip();
            var hasDescription = !setItem.Description.IsNullOrWhitespace() && !setItem.Description.Contains("Enter Description Here...");

            if(hasDescription)
            {
                // push the text wrap position to the font size times 35
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                // we will then check to see if the text contains a tooltip
                if (setItem.Description.Contains(CkGui.TipSep, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = setItem.Description.Split(CkGui.TipSep, StringSplitOptions.None);
                    // for each of the split text, we will display the text unformatted
                    for (var i = 0; i < splitText.Length; i++)
                    {
                        ImGui.TextUnformatted(splitText[i]);
                        if (i != splitText.Length - 1) ImGui.Separator();
                    }
                }
                else
                {
                    ImGui.TextUnformatted(setItem.Description);
                }
                ImGui.PopTextWrapPos();
            }

            ImGui.EndTooltip();
        }
    }
}

