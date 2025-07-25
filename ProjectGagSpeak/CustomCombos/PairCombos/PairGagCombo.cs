using CkCommons.Gui;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.WebAPI;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using ImGuiNET;
using Penumbra.GameData.Enums;

namespace GagSpeak.CustomCombos.Pairs;
public sealed class PairGagCombo : CkFilterComboButton<GagType>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _kinksterRef;

    public PairGagCombo(ILogger log, MainHub hub, Kinkster pair, Action postButtonPress)
        : base(() => [.. Enum.GetValues<GagType>().Skip(1)], log)
    {
        _mainHub = hub;
        _kinksterRef = pair;

        // update current selection to the last registered gagType from that pair on construction.
        Current = GagType.None;
        PostButtonPress = postButtonPress;
    }

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalGagIdx, bool selected)
    {
        var gagItem = Items[globalGagIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(gagItem.GagName(), selected);

        // IF the GagType is present in their light gag storage dictionary, then draw the link icon.
        if (_kinksterRef.LightCache.Gags.TryGetValue(gagItem, out var gagData))
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
            CkGui.IconText(FAI.Link, ImGui.GetColorU32(ImGuiColors.HealerGreen));
            DrawItemTooltip(gagData, $"View {_kinksterRef.GetNickAliasOrUid()}'s Data for this Gag.");
        }
        return ret;
    }

    protected override bool DisableCondition()
        => Current == GagType.None || !_kinksterRef.PairPerms.ApplyGags;

    protected override async Task<bool> OnButtonPress(int layerIdx)
    {
        // we need to go ahead and create a deep clone of our new appearanceData, and ensure it is valid.
        if (_kinksterRef.ActiveGags.GagSlots[layerIdx].GagItem == Current)
            return false;

        var updateType = _kinksterRef.ActiveGags.GagSlots[layerIdx].GagItem is GagType.None
            ? DataUpdateType.Applied : DataUpdateType.Swapped;
        
        // construct the dto to send.
        var dto = new PushKinksterActiveGagSlot(_kinksterRef.UserData, updateType)
        {
            Layer = layerIdx,
            Gag = Current,
            Enabler = MainHub.UID,
        };

        // push to server.
        var result = await _mainHub.UserChangeKinksterActiveGag(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform ApplyGag with {Current.GagName()} on {_kinksterRef.GetNickAliasOrUid()}, Reason:{result}", LoggerType.StickyUI);
            return false;
        }
        else
        {
            Log.LogDebug($"Applying Gag with {Current.GagName()} on {_kinksterRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
            PostButtonPress?.Invoke();
            return true;
        }
    }

    private void DrawItemTooltip(KinksterGag item, string headerText)
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
            if (headerText.Contains(CkGui.TipSep, StringComparison.Ordinal))
            {
                // if it does, we will split the text by the tooltip
                var splitText = headerText.Split(CkGui.TipSep, StringSplitOptions.None);
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
            ImGui.Separator();
            ImGui.Text(item.GlamItem.Name);
            ImGui.Text(item.Slot.ToName() + " Slot");

            ImGui.EndTooltip();
        }
    }
}

