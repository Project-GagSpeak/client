using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui.Text;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace GagSpeak.UI.Components.Combos;
public sealed class PairRestraintCombo : GagspeakComboButtonBase<LightRestraintData>
{
    private readonly SetPreviewComponent _ttPreview;
    private readonly MainHub _mainHub;
    private Pair _pairRef;

    public PairRestraintCombo(Pair pairData, MainHub mainHub, ILogger log, SetPreviewComponent ttPreview, 
        UiSharedService uiShared, string bText, string bTT) : base(log, uiShared, bText, bTT)
    {
        _ttPreview = ttPreview;
        _mainHub = mainHub;
        _pairRef = pairData;

        // update the current selection to the pairs active set if the last wardrobe & light data are not null.
        if (_pairRef.LastWardrobeData is not null && _pairRef.LastLightStorage is not null)
        {
            CurrentSelection = _pairRef.LastLightStorage.Restraints
                .FirstOrDefault(r => r.Identifier == _pairRef.LastWardrobeData.ActiveSetId);
        }
    }

    // override the method to extract items by extracting all gagTypes.
    protected override IReadOnlyList<LightRestraintData> ExtractItems() => _pairRef.LastLightStorage?.Restraints ?? new List<LightRestraintData>();

    // we need to override the toItemString here.
    protected override string ToItemString(LightRestraintData item) => item.Name;

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(LightRestraintData restraintItem, bool selected)
    {
        // we want to start by drawing the selectable first.
        var name = ToItemString(restraintItem);
        var ret = ImGui.Selectable(name, selected);
        if (_pairRef.LastLightStorage is not null)
        {
            var iconWidth = _uiShared.GetIconData(FontAwesomeIcon.InfoCircle).X;
            var hasGlamour = restraintItem.AffectedSlots.Any();
            var hasInfo = !restraintItem.Description.IsNullOrWhitespace() 
                ||  restraintItem.HardcoreTraits.TryGetValue(MainHub.UID, out var traits) && traits.AnyEnabled();

            var shift = hasInfo ? iconWidth * 2 + ImGui.GetStyle().ItemSpacing.X : iconWidth;

            // shift over to the right to draw out the icons.
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - shift);

            if (hasInfo)
            {
                _uiShared.IconText(FontAwesomeIcon.InfoCircle, ImGui.GetColorU32(ImGuiColors.ParsedGold));
                DrawItemTooltip(restraintItem);
                ImGui.SameLine();
            }

            // icon for the glamour preview.
            _uiShared.IconText(FontAwesomeIcon.Tshirt, ImGui.GetColorU32(hasGlamour ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey));
            if (hasGlamour) _ttPreview.DrawLightRestraintOnHover(restraintItem);

        }
        return ret;
    }

    protected override bool DisableCondition()
    {
        if (_pairRef.LastWardrobeData is null || CurrentSelection is null) return true;
        // otherwise return the condition.
        return _pairRef.LastWardrobeData.ActiveSetId == CurrentSelection.Identifier || !_pairRef.PairPerms.ApplyRestraintSets;
    }

    protected override void OnButtonPress()
    {
        // we need to go ahead and create a deep clone of our new appearanceData, and ensure it is valid.
        if (_pairRef.LastWardrobeData is null || CurrentSelection is null) return;
        var newWardrobe = _pairRef.LastWardrobeData.DeepCloneData();
        if (newWardrobe is null) return;

        // update the wardrobe with the new information.
        newWardrobe.ActiveSetId = CurrentSelection.Identifier;
        newWardrobe.ActiveSetEnabledBy = MainHub.UID;
        // push to server.
        _ = _mainHub.UserPushPairDataWardrobeUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, newWardrobe, WardrobeUpdateType.RestraintApplied, newWardrobe.Padlock, UpdateDir.Other));
        PairCombos.Opened = InteractionType.None;
        _logger.LogDebug("Applying Restraint Set " + CurrentSelection.Name + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
    }

    private void DrawItemTooltip(LightRestraintData setItem)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            // begin the tooltip interface
            ImGui.BeginTooltip();
            bool hasDescription = !setItem.Description.IsNullOrWhitespace() && !setItem.Description.Contains("Enter Description Here...");

            if(hasDescription)
            {
                // push the text wrap position to the font size times 35
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                // we will then check to see if the text contains a tooltip
                if (setItem.Description.Contains(UiSharedService.TooltipSeparator, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = setItem.Description.Split(UiSharedService.TooltipSeparator, StringSplitOptions.None);
                    // for each of the split text, we will display the text unformatted
                    for (int i = 0; i < splitText.Length; i++)
                    {
                        ImGui.TextUnformatted(splitText[i]);
                        if (i != splitText.Length - 1) ImGui.Separator();
                    }
                }
                else
                {
                    ImGui.TextUnformatted(setItem.Description);
                }
                // finally, pop the text wrap position
                ImGui.PopTextWrapPos();
            }

            bool traitsExist = setItem.HardcoreTraits.TryGetValue(MainHub.UID, out var traits) && traits.AnyEnabled();

            if (traitsExist && !setItem.Description.IsNullOrWhitespace()) ImGui.Separator();

            if (traitsExist && traits is not null)
            {
                ImGui.TextUnformatted("When you apply this Set," + Environment.NewLine
                    + "the following traits are enabled:");
                ImGui.Separator();

                if (traits.LegsRestrained)
                {
                    UiSharedService.ColorText("Legs Restrained:", ImGuiColors.ParsedGold);
                    ImUtf8.SameLineInner();
                    ImGui.TextUnformatted("Actions requiring the use of legs are revoked.");
                }
                else if (traits.ArmsRestrained)
                {
                    UiSharedService.ColorText("Arms Restrained:", ImGuiColors.ParsedGold);
                    ImUtf8.SameLineInner();
                    ImGui.TextUnformatted("Actions requiring the use of arms are revoked.");
                }
                else if (traits.Gagged)
                {
                    UiSharedService.ColorText("Gagged:", ImGuiColors.ParsedGold);
                    ImUtf8.SameLineInner();
                    ImGui.TextUnformatted("Actions that use the mouth are revoked.");
                }
                else if (traits.Blindfolded)
                {
                    UiSharedService.ColorText("Blindfolded:", ImGuiColors.ParsedGold);
                    ImUtf8.SameLineInner();
                    ImGui.TextUnformatted("Sight is obscured.");
                }
                else if (traits.Immobile)
                {
                    UiSharedService.ColorText("Immobilized", ImGuiColors.ParsedGold);
                    ImUtf8.SameLineInner();
                    ImGui.TextUnformatted("Actions requiring any movement are revoked.");
                }
                else if (traits.Weighty)
                {
                    UiSharedService.ColorText("Weighty", ImGuiColors.ParsedGold);
                    ImUtf8.SameLineInner();
                    ImGui.TextUnformatted("Movement is restricted to walking.");
                }
                else if (traits.StimulationLevel != StimulationLevel.None)
                {
                    UiSharedService.ColorText("Stimulation:", ImGuiColors.ParsedGold);
                    ImUtf8.SameLineInner();
                    var stimulationStr = traits.StimulationLevel.ToString() + " (GCD Delay +";
                    // display the GCD delay percentage.
                    ImGui.TextUnformatted(stimulationStr + traits.StimulationLevel switch
                    {
                        StimulationLevel.Light => "12.5%",
                        StimulationLevel.Mild => "25%",
                        StimulationLevel.Heavy => "50%",
                        _ => "0%"
                    } + ")");
                }
            }
            ImGui.EndTooltip();
        }
    }
}

