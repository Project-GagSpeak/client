using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UI.Components.Combos;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CustomCombos.PairActions;

public sealed class PairRestraintCombo : CkFilterComboButton<LightRestraintSet>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;

    public PairRestraintCombo(Pair pairData, MainHub mainHub, ILogger log, UiSharedService uiShared, string bText, string bTT)
        : base(() => [
            .. pairData.LastLightStorage.Restraints.OrderBy(x => x.Label),
        ], log, uiShared, bText, bTT)
    {
        _mainHub = mainHub;
        _pairRef = pairData;

        // update the current selection to the pairs active set if the last wardrobe & light data are not null.
        CurrentSelection = _pairRef.LastLightStorage.Restraints
            .FirstOrDefault(r => r.Id == _pairRef.LastRestraintData.Identifier);
    }

    protected override bool DisableCondition()
        => _pairRef.PairPerms.ApplyRestraintSets is false
        || _pairRef.LastRestraintData.Identifier == CurrentSelection?.Id
        || CurrentSelection is null;

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalAlarmIdx, bool selected)
    {
        var restraintSet = Items[globalAlarmIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(restraintSet.Label, selected);
        
        var iconWidth = _uiShared.GetIconData(FontAwesomeIcon.InfoCircle).X;
        var hasGlamour = restraintSet.AffectedSlots.Any();
        var hasInfo = !restraintSet.Desc.IsNullOrWhitespace() || restraintSet.TraitAllowances.Contains(MainHub.UID);
        var shiftOffset = hasInfo ? iconWidth * 2 + ImGui.GetStyle().ItemSpacing.X : iconWidth;

        // shift over to the right to draw out the icons.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - shiftOffset);

        if (hasInfo)
        {
            _uiShared.IconText(FontAwesomeIcon.InfoCircle, ImGui.GetColorU32(ImGuiColors.ParsedGold));
            DrawItemTooltip(restraintSet);
            ImGui.SameLine();
        }

        // icon for the glamour preview.
        _uiShared.IconText(FontAwesomeIcon.Tshirt, ImGui.GetColorU32(hasGlamour ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey));
        // if (hasGlamour) _ttPreview.DrawLightRestraintOnHover(restraintItem);
        return ret;
    }

    protected override void OnButtonPress()
    {
        // we need to go ahead and create a deep clone of our new appearanceData, and ensure it is valid.
        if (CurrentSelection is null)
            return;

        var updateType = _pairRef.LastRestraintData.Identifier.IsEmptyGuid()
            ? DataUpdateType.Applied : DataUpdateType.Swapped;
        // construct the dto to send.
        var dto = new PushPairRestraintDataUpdateDto(_pairRef.UserData, updateType)
        {
            ActiveSetId = CurrentSelection.Id,
            Enabler = MainHub.UID,
        };

        _ = _mainHub.UserPushPairDataRestraint(dto);
        PairCombos.Opened = InteractionType.None;
        Log.LogDebug("Applying Restraint Set " + CurrentSelection.Label + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
    }

    private void DrawItemTooltip(LightRestraintSet setItem)
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

