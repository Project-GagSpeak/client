using CkCommons;
using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using ImGuiNET;
using Penumbra.GameData.Enums;

namespace GagSpeak.CustomCombos.Pairs;
public sealed class PairGagCombo : CkFilterComboButton<KinksterGag>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _kinksterRef;

    public PairGagCombo(ILogger log, MainHub hub, Kinkster kinkster, Action postButtonPress)
        : base(() => [ ..kinkster.LightCache.Gags.Values.OrderBy(x => x.Gag)], log)
    {
        _mainHub = hub;
        _kinksterRef = kinkster;
        Current = kinkster.LightCache.Gags.GetValueOrDefault(GagType.None);
        PostButtonPress = postButtonPress;
    }

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalGagIdx, bool selected)
    {
        var gagItem = Items[globalGagIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(gagItem.Gag.GagName(), selected);

        // IF the GagType is present in their light gag storage dictionary, then draw the link icon.
        if (gagItem.IsEnabled)
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconSize(FAI.Link).X);
            CkGui.IconText(FAI.Link, ImGuiColors.HealerGreen.ToUint());
            DrawItemTooltip(gagItem);
        }
        return ret;
    }

    protected override bool DisableCondition()
        => Current is null || Current.Gag is GagType.None || !_kinksterRef.PairPerms.ApplyGags;

    protected override void OnButtonPress(int layerIdx)
    {
        if (Current is null)
            return;

        // we need to go ahead and create a deep clone of our new appearanceData, and ensure it is valid.
        if (_kinksterRef.ActiveGags.GagSlots[layerIdx].GagItem == Current.Gag)
            return;

        var updateType = _kinksterRef.ActiveGags.GagSlots[layerIdx].GagItem is GagType.None
            ? DataUpdateType.Applied : DataUpdateType.Swapped;
        
        // construct the dto to send.
        var dto = new PushKinksterActiveGagSlot(_kinksterRef.UserData, updateType)
        {
            Layer = layerIdx,
            Gag = Current.Gag,
            Enabler = MainHub.UID,
        };

        UiService.SetUITask(async () =>
        {
            var result = await _mainHub.UserChangeKinksterActiveGag(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform ApplyGag with {Current.Gag.GagName()} on {_kinksterRef.GetNickAliasOrUid()}, Reason:{result}", LoggerType.StickyUI);
            }
            else
            {
                Log.LogDebug($"Applying Gag with {Current.Gag.GagName()} on {_kinksterRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
                PostButtonPress?.Invoke();
            }
        });

    }

    private void DrawItemTooltip(KinksterGag item)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
                .Push(ImGuiStyleVar.WindowRounding, 4f)
                .Push(ImGuiStyleVar.PopupBorderSize, 1f);
            using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            // begin the tooltip interface
            ImGui.BeginTooltip();
            ImGui.Text($"{_kinksterRef.GetNickAliasOrUid()} has this Gag's Visuals Enabled!");
            // before we pop the text wrap position, we will draw the item's icon.
            ImGui.Separator();
            ImGui.Text(item.GlamItem.Name);
            ImGui.Text(item.Slot.ToName() + " Slot");

            ImGui.EndTooltip();
        }
    }
}

