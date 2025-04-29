using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CustomCombos;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.CustomCombos.PairActions;

public sealed class PairRestrictionCombo : CkFilterComboButton<LightRestriction>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;

    public PairRestrictionCombo(ILogger log, Pair pair, MainHub hub, Func<IReadOnlyList<LightRestriction>> generator)
        : base(generator, log)
    {
        _mainHub = hub;
        _pairRef = pair;
        Current = default;
    }

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var restriction = Items[globalIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(restriction.Label, selected);
        
        var iconWidth = CkGui.IconSize(FAI.InfoCircle).X;
        var hasGlamour = restriction.Item.CustomItemId != ulong.MaxValue;
        var hasDesc = !restriction.Description.IsNullOrWhitespace();
        var shiftOffset = iconWidth;

        // shift over to the right to draw out the icons.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - shiftOffset);

        if (hasDesc)
        {
            CkGui.IconText(FAI.InfoCircle, ImGui.GetColorU32(ImGuiColors.ParsedGold));
            DrawItemTooltip(restriction);
            ImGui.SameLine();
        }

        // icon for the glamour preview.
        CkGui.IconText(FAI.Tshirt, ImGui.GetColorU32(hasGlamour ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey));
        // if (hasGlamour) _ttPreview.DrawLightRestraintOnHover(restraintItem);
        return ret;
    }


    protected override bool DisableCondition()
        => Current is null || !_pairRef.PairPerms.ApplyRestraintSets || _pairRef.LastRestraintData.Identifier == Current.Id;

    protected override void OnButtonPress(int layerIdx)
    {
        if (Current is null)
            return;

        var updateType = _pairRef.LastRestrictionsData.Restrictions[layerIdx].Identifier.IsEmptyGuid()
            ? DataUpdateType.Applied : DataUpdateType.Swapped;

        // construct the dto to send.
        var dto = new PushPairRestrictionDataUpdateDto(_pairRef.UserData, updateType)
        {
            Layer = layerIdx,
            RestrictionId = Current.Id,
            Enabler = MainHub.UID,
        };

        _mainHub.UserPushPairDataRestrictions(dto).ConfigureAwait(false);
        PairCombos.Opened = InteractionType.None;
        Log.LogDebug("Applying Restraint Set " + Current.Label + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
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

