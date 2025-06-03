using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CustomCombos.PairActions;

public sealed class PairRestraintCombo : CkFilterComboButton<LightRestraintSet>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;

    public PairRestraintCombo(Pair pair, MainHub hub, ILogger log)
        : base([.. pair.LastLightStorage.Restraints.OrderBy(x => x.Label)], log)
    {
        _mainHub = hub;
        _pairRef = pair;
        Current = _pairRef.LastLightStorage.Restraints.FirstOrDefault(r => r.Id == _pairRef.LastRestraintData.Identifier);
    }

    protected override bool DisableCondition()
        => Current is null || !_pairRef.PairPerms.ApplyRestraintSets || _pairRef.LastRestraintData.Identifier == Current.Id;

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalAlarmIdx, bool selected)
    {
        var restraintSet = Items[globalAlarmIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(restraintSet.Label, selected);
        
        var iconWidth = CkGui.IconSize(FAI.InfoCircle).X;
        var hasGlamour = restraintSet.AffectedSlots.Any();
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

        var updateType = _pairRef.LastRestraintData.Identifier.IsEmptyGuid()
            ? DataUpdateType.Applied : DataUpdateType.Swapped;
        // construct the dto to send.
        var dto = new PushPairRestraintDataUpdateDto(_pairRef.UserData, updateType)
        {
            ActiveSetId = Current.Id,
            Enabler = MainHub.UID,
        };

        var result = await _mainHub.UserPushPairDataRestraint(dto);
        if (result is not GagSpeakApiEc.Success)
        {
            Log.LogError($"Failed to Perform PairRestraint action to {_pairRef.GetNickAliasOrUid()} : {result}");
            return false;
        }
        else
        {
            Log.LogDebug("Applying Restraint Set " + Current.Label + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            return true;
        }
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
            var hasDescription = !setItem.Description.IsNullOrWhitespace() && !setItem.Description.Contains("Enter Description Here...");

            if(hasDescription)
            {
                // push the text wrap position to the font size times 35
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                // we will then check to see if the text contains a tooltip
                if (setItem.Description.Contains(CkGui.TooltipSeparator, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = setItem.Description.Split(CkGui.TooltipSeparator, StringSplitOptions.None);
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

