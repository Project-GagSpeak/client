using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Penumbra.GameData.Enums;
using System.Numerics;

namespace GagSpeak.UI.Components.Combos;

public sealed class GagComboPair : GagspeakComboButtonBase<GagType>
{
    private readonly SetPreviewComponent _gagPreview;
    private readonly MainHub _mainHub;

    private Pair _pairRef;

    public GagComboPair(ILogger log, SetPreviewComponent gagPreview, MainHub mainHub, UiSharedService uiShared,
        Pair pairData, string bText, string bTT) : base(log, uiShared, bText, bTT)
    {
        _gagPreview = gagPreview;
        _mainHub = mainHub;
        _pairRef = pairData;

        // update current selection to the last registered gagType from that pair on construction.
        CurrentSelection = _pairRef.LastAppearanceData?.GagSlots[PairCombos.GagLayer].GagType.ToGagType() ?? GagType.None;
    }

    // override the method to extract items by extracting all gagTypes.
    protected override IReadOnlyList<GagType> ExtractItems() => Enum.GetValues<GagType>().Except(new[] { GagType.None }).ToList();

    // we need to override the toItemString here.
    protected override string ToItemString(GagType item) => item.GagName();

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(GagType gagItem, bool selected)
    {
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(gagItem.GagName(), selected);

        // IF the GagType is present in their light gag storage dictionary, then draw the link icon.
        if (_pairRef.LastLightStorage is not null && _pairRef.LastLightStorage.GagItems.ContainsKey(gagItem))
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
            _uiShared.IconText(FontAwesomeIcon.Link, ImGui.GetColorU32(ImGuiColors.HealerGreen));
            DrawItemTooltip(gagItem, _pairRef.GetNickAliasOrUid() + " set a Glamour for this Gag.");
        }
        return ret;
    }

    protected override bool DisableCondition()
    {
        if (_pairRef.LastAppearanceData is null) return true;
        // otherwise return the condition.
        return _pairRef.LastAppearanceData.GagSlots[PairCombos.GagLayer].GagType.ToGagType() == CurrentSelection
            || !_pairRef.PairPerms.ApplyGags;
    }

    protected override void OnButtonPress()
    {
        // we need to go ahead and create a deepclone of our new appearanceData, and ensure it is valid.
        if (_pairRef.LastAppearanceData is null) return;
        var newAppearance = _pairRef.LastAppearanceData.DeepCloneData();
        if (newAppearance is null) return;

        // update the gag slot with the new gag.
        newAppearance.GagSlots[PairCombos.GagLayer].GagType = CurrentSelection.GagName();
        // push to server.
        _ = _mainHub.UserPushPairDataAppearanceUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, newAppearance, (GagLayer)PairCombos.GagLayer,
            GagUpdateType.GagApplied, newAppearance.GagSlots[PairCombos.GagLayer].Padlock.ToPadlock(), UpdateDir.Other));

        UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, true, PairCombos.GagLayer, CurrentSelection, MainHub.UID, _pairRef.UserData.UID); // move this.
        PairCombos.Opened = InteractionType.None;

        // log success.
        _logger.LogDebug("Applying Selected Gag " + CurrentSelection.GagName() + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
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
                for (int i = 0; i < splitText.Length; i++)
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
            if (_pairRef.LastLightStorage is not null && _pairRef.LastLightStorage.GagItems.TryGetValue(item, out var appliedSlot))
            {
                ImGui.Separator();
                using (ImRaii.Group())
                {
                    _gagPreview.DrawAppliedSlot(appliedSlot);
                    ImGui.SameLine();
                    using (ImRaii.Group())
                    {
                        var equipItem = ItemIdVars.Resolve((EquipSlot)appliedSlot.Slot, appliedSlot.CustomItemId);
                        ImGui.Text(equipItem.Name);
                        ImGui.Text(((EquipSlot)appliedSlot.Slot).ToName() + " Slot");
                    }
                }
            }
            ImGui.EndTooltip();
        }
    }
}

