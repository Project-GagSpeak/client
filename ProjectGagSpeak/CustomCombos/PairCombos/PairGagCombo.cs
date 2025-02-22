using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UI.Components.Combos;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using Penumbra.GameData.Enums;

namespace GagSpeak.CustomCombos.PairActions;

public sealed class PairGagCombo : CkFilterComboButton<GagType>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    private int CurrentLayer;
    
    public PairGagCombo(int layer, Pair pair, MainHub hub, ILogger log, UiSharedService ui, string bText, string bTT)
        : base(() => [
            .. Enum.GetValues<GagType>().Except(new[] { GagType.None }),
        ], log, ui, bText, bTT)
    {
        CurrentLayer = layer;
        _mainHub = hub;
        _pairRef = pair;

        // update current selection to the last registered gagType from that pair on construction.
        CurrentSelection = _pairRef.LastGagData.GagSlots[layer].GagItem;
    }

    public void SetLayer(int newLayer)
    {
        var priorState = IsInitialized;
        if (priorState)
            Cleanup();
        // update the layer.
        CurrentLayer = newLayer;

        // Update the item.
        CurrentSelectionIdx = Items.IndexOf(gag => gag == _pairRef.LastGagData.GagSlots[CurrentLayer].GagItem);
        if(CurrentSelectionIdx >= 0)
            CurrentSelection = Items[CurrentSelectionIdx];
        else if (Items.Count > 0)
            CurrentSelection = Items[0];

        if (!priorState)
            Cleanup();
    }


    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalGagIdx, bool selected)
    {
        var gagItem = Items[globalGagIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(gagItem.GagName(), selected);

        // IF the GagType is present in their light gag storage dictionary, then draw the link icon.
        if (_pairRef.LastLightStorage.GagItems.ContainsKey(gagItem))
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
            _uiShared.IconText(FontAwesomeIcon.Link, ImGui.GetColorU32(ImGuiColors.HealerGreen));
            DrawItemTooltip(gagItem, _pairRef.GetNickAliasOrUid() + " set a Glamour for this Gag.");
        }
        return ret;
    }

    protected override bool DisableCondition()
        => _pairRef.LastGagData.GagSlots[CurrentLayer].GagItem == GagType.None
        || _pairRef.PairPerms.ApplyGags is false;

    protected override void OnButtonPress()
    {
        // we need to go ahead and create a deep clone of our new appearanceData, and ensure it is valid.
        if (_pairRef.LastGagData.GagSlots[CurrentLayer].GagItem == CurrentSelection)
            return;

        var updateType = _pairRef.LastGagData.GagSlots[CurrentLayer].GagItem is GagType.None
            ? DataUpdateType.Applied : DataUpdateType.Swapped;
        // construct the dto to send.
        var dto = new PushPairGagDataUpdateDto(_pairRef.UserData, updateType)
        {
            Gag = CurrentSelection,
            Enabler = MainHub.UID,
        };

        // push to server.
        _ = _mainHub.UserPushPairDataGags(dto);
        PairCombos.Opened = InteractionType.None;
        Log.LogDebug("Applying Selected Gag " + CurrentSelection.GagName() + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
    }

    private void DrawItemTooltip(GagType item, string headerText)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            // begin the tooltip interface
            ImGui.BeginTooltip();
            // push the text wrap position to the font size times 35
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
            // we will then check to see if the text contains a tooltip
            if (headerText.Contains(UiSharedService.TooltipSeparator, StringComparison.Ordinal))
            {
                // if it does, we will split the text by the tooltip
                var splitText = headerText.Split(UiSharedService.TooltipSeparator, StringSplitOptions.None);
                // for each of the split text, we will display the text unformatted
                for (var i = 0; i < splitText.Length; i++)
                {
                    ImGui.TextUnformatted(splitText[i]);
                    if (i != splitText.Length - 1) ImGui.Separator();
                }
            }
            else
            {
                ImGui.TextUnformatted(headerText);
            }
            // finally, pop the text wrap position
            ImGui.PopTextWrapPos();

            // before we pop the text wrap position, we will draw the item's icon.
            if (_pairRef.LastLightStorage.GagItems.TryGetValue(item, out var appliedSlot))
            {
                ImGui.Separator();
                using (ImRaii.Group())
                {
                    //_gagPreview.DrawAppliedSlot(appliedSlot);
                    ImGui.SameLine();
                    using (ImRaii.Group())
                    {
                        //var equipItem = ItemService.Resolve((EquipSlot)appliedSlot.Slot, appliedSlot.CustomItemId);
                        //ImGui.Text(equipItem.Name);
                        ImGui.Text(((EquipSlot)appliedSlot.Slot).ToName() + " Slot");
                    }
                }
            }
            ImGui.EndTooltip();
        }
    }
}

