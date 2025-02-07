using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.Components.Combos;

/// <summary>
/// Unique GagCombo type.
/// </summary>
public sealed class PairPatternCombo : GagspeakComboButtonBase<LightPattern>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    public PairPatternCombo(Pair pairData, MainHub mainHub, ILogger log, UiSharedService uiShared, string bText, string bTT)
        : base(log, uiShared, FontAwesomeIcon.Female, bText, bTT)
    {
        _mainHub = mainHub;
        _pairRef = pairData;

        // update current selection to the last registered LightPattern from that pair on construction.
        if (_pairRef.LastToyboxData is not null && _pairRef.LastLightStorage is not null)
        {
            CurrentSelection = _pairRef.LastLightStorage.Patterns
                .FirstOrDefault(r => r.Identifier == _pairRef.LastToyboxData.ActivePatternId);
        }
    }

    // override the method to extract items by extracting all LightPatterns.
    protected override IReadOnlyList<LightPattern> ExtractItems() => _pairRef.LastLightStorage?.Patterns ?? new List<LightPattern>();

    protected override string ToItemString(LightPattern item) => item.Name;

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(LightPattern patternItem, bool selected)
    {
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(patternItem.Name, selected);

        // IF the LightPattern is present in their light gag storage dictionary, then draw the link icon.
        if (_pairRef.LastLightStorage is not null)
        {
            // shift over and draw an info circle, and a loop circle if any.
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - 2 * ImGui.GetTextLineHeight() - ImGui.GetStyle().ItemSpacing.X);

            // draw the shouldLoop icon.
            _uiShared.IconText(FontAwesomeIcon.Sync, ImGui.GetColorU32(patternItem.ShouldLoop ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey));
            if (patternItem.ShouldLoop) UiSharedService.AttachToolTip("This is a Looping Pattern.");

            // draw the info icon.
            ImGui.SameLine();
            _uiShared.IconText(FontAwesomeIcon.InfoCircle, ImGuiColors.TankBlue);
            DrawItemTooltip(patternItem);
        }
        return ret;
    }

    protected override bool DisableCondition()
    {
        if (_pairRef.LastToyboxData is null || CurrentSelection is null) return true;
        // otherwise return the condition.
        return _pairRef.LastToyboxData.ActivePatternId == CurrentSelection.Identifier || !_pairRef.PairPerms.CanExecutePatterns;
    }

    protected override void OnButtonPress()
    {
        // we need to go ahead and create a deep clone of our new appearanceData, and ensure it is valid.
        if (_pairRef.LastToyboxData is null) return;
        var newToyboxData = _pairRef.LastToyboxData.DeepClone();
        if (newToyboxData is null || CurrentSelection is null) return;

        // set all other stored patterns active state to false, and the pattern with the onButtonPress matching GUID to true.
        newToyboxData.InteractionId = CurrentSelection.Identifier;
        newToyboxData.ActivePatternId = CurrentSelection.Identifier;

        // Run the call to execute the pattern to the server.
        _ = _mainHub.UserPushPairDataToyboxUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, newToyboxData, ToyboxUpdateType.PatternExecuted, UpdateDir.Other));
        PairCombos.Opened = InteractionType.None;
        // log success.
        _logger.LogDebug("Executing Pattern " + CurrentSelection.Name + " on " + _pairRef.GetNickAliasOrUid() + "'s Toy", LoggerType.Permissions);
    }

    private void DrawItemTooltip(LightPattern item)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            // begin the tooltip interface
            ImGui.BeginTooltip();

            // draw trigger description
            if (!item.Description.IsNullOrWhitespace())
            {
                // push the text wrap position to the font size times 35
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                // we will then check to see if the text contains a tooltip
                if (item.Description.Contains(UiSharedService.TooltipSeparator, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = item.Description.Split(UiSharedService.TooltipSeparator, StringSplitOptions.None);
                    // for each of the split text, we will display the text unformatted
                    for (int i = 0; i < splitText.Length; i++)
                    {
                        ImGui.TextUnformatted(splitText[i]);
                        if (i != splitText.Length - 1) ImGui.Separator();
                    }
                }
                else
                {
                    ImGui.TextUnformatted(item.Description);
                }
                // finally, pop the text wrap position
                ImGui.PopTextWrapPos();
                ImGui.Separator();
            }

            UiSharedService.ColorText("Duration:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            var durationStr = item.Duration.Hours > 0 ? item.Duration.ToString("hh\\:mm\\:ss") : item.Duration.ToString("mm\\:ss");
            ImGui.Text(durationStr);

            UiSharedService.ColorText("Loops?:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.Text(item.ShouldLoop ? "Yes" : "No");

            ImGui.EndTooltip();
        }
    }
}

