using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CustomCombos;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UI.Components.Combos;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.CustomCombos.PairActions;

public sealed class PairRestrictionCombo : CkFilterComboButton<LightRestriction>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    private int CurrentLayer;

    public PairRestrictionCombo(int layer, Pair pair, MainHub hub, ILogger log, UiSharedService ui, string bText, string bTT)
        : base(() => [
            .. pair.LastLightStorage.Restrictions.OrderBy(x => x.Label),
        ], log, ui, bText, bTT)
    {
        CurrentLayer = layer;
        _mainHub = hub;
        _pairRef = pair;

        // update the current selection to the pairs active set if the last wardrobe & light data are not null.
        CurrentSelection = _pairRef.LastLightStorage.Restrictions
            .FirstOrDefault(r => r.Id == _pairRef.LastRestrictionsData.Restrictions[CurrentLayer].Identifier);
    }

    public void SetLayer(int newLayer)
    {
        var priorState = IsInitialized;
        if (priorState)
            Cleanup();
        // update the layer.
        CurrentLayer = newLayer;

        // Update the item.
        CurrentSelectionIdx = Items.IndexOf(item => item.Id == _pairRef.LastRestrictionsData.Restrictions[CurrentLayer].Identifier);
        if (CurrentSelectionIdx >= 0)
            CurrentSelection = Items[CurrentSelectionIdx];
        else if (Items.Count > 0)
            CurrentSelection = Items[0];

        if (!priorState)
            Cleanup();
    }

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var restriction = Items[globalIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(restriction.Label, selected);
        
        var iconWidth = _uiShared.GetIconData(FontAwesomeIcon.InfoCircle).X;
        var hasGlamour = restriction.AffectedSlot.CustomItemId != ulong.MaxValue;
        var hasDesc = !restriction.Desc.IsNullOrWhitespace();
        var hasTraits = restriction.TraitAllowances.Contains(MainHub.UID);
        var shiftOffset = hasTraits ? iconWidth * 2 + ImGui.GetStyle().ItemSpacing.X : iconWidth;

        // shift over to the right to draw out the icons.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - shiftOffset);

        if (hasTraits || hasDesc)
        {
            _uiShared.IconText(FontAwesomeIcon.InfoCircle, ImGui.GetColorU32(ImGuiColors.ParsedGold));
            DrawItemTooltip(restriction);
            ImGui.SameLine();
        }

        // icon for the glamour preview.
        _uiShared.IconText(FontAwesomeIcon.Tshirt, ImGui.GetColorU32(hasGlamour ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey));
        // if (hasGlamour) _ttPreview.DrawLightRestraintOnHover(restraintItem);
        return ret;
    }


    protected override bool DisableCondition()
        => _pairRef.PairPerms.ApplyRestraintSets is false
        || _pairRef.LastRestraintData.Identifier == CurrentSelection?.Id
        || CurrentSelection is null;

    protected override void OnButtonPress()
    {
        if (CurrentSelection is null)
            return;

        var updateType = _pairRef.LastRestrictionsData.Restrictions[CurrentLayer].Identifier.IsEmptyGuid()
            ? DataUpdateType.Applied : DataUpdateType.Swapped;
        // construct the dto to send.
        var dto = new PushPairRestrictionDataUpdateDto(_pairRef.UserData, updateType)
        {
            RestrictionId = CurrentSelection.Id,
            Enabler = MainHub.UID,
        };

        _ = _mainHub.UserPushPairDataRestrictions(dto);
        PairCombos.Opened = InteractionType.None;
        Log.LogDebug("Applying Restraint Set " + CurrentSelection.Label + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
    }

    private void DrawItemTooltip(LightRestriction setItem)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            // begin the tooltip interface
            ImGui.BeginTooltip();
            var hasDescription = !setItem.Desc.IsNullOrWhitespace() && !setItem.Desc.Contains("Enter Description Here...");

            if(hasDescription)
            {
                // push the text wrap position to the font size times 35
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                // we will then check to see if the text contains a tooltip
                if (setItem.Desc.Contains(UiSharedService.TooltipSeparator, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = setItem.Desc.Split(UiSharedService.TooltipSeparator, StringSplitOptions.None);
                    // for each of the split text, we will display the text unformatted
                    for (var i = 0; i < splitText.Length; i++)
                    {
                        ImGui.TextUnformatted(splitText[i]);
                        if (i != splitText.Length - 1) ImGui.Separator();
                    }
                }
                else
                {
                    ImGui.TextUnformatted(setItem.Desc);
                }
                ImGui.PopTextWrapPos();
            }

            if (setItem.TraitAllowances.Contains(MainHub.UID))
            {
                ImGui.Separator();
                ImGui.TextUnformatted("Applies Hardcore Traits when enabled by you");
            }

            ImGui.EndTooltip();
        }
    }
}

