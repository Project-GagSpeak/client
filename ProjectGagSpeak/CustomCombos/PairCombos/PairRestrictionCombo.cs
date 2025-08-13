using CkCommons;
using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using Dalamud.Bindings.ImGui;

namespace GagSpeak.CustomCombos.Pairs;

public sealed class PairRestrictionCombo : CkFilterComboButton<KinksterRestriction>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _ref;

    public PairRestrictionCombo(ILogger log, MainHub hub, Kinkster kinkster, Action postButtonPress)
        : base(() => [.. kinkster.LightCache.Restrictions.Values.OrderBy(x => x.Label)], log)
    {
        PostButtonPress = postButtonPress;
        _mainHub = hub;
        _ref = kinkster;
        Current = default;
    }

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var restriction = Items[globalIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(restriction.Label, selected);
        
        var iconWidth = CkGui.IconSize(FAI.InfoCircle).X;
        var isEnabled = restriction.IsEnabled;
        if (restriction.IsEnabled)
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconSize(FAI.InfoCircle).X);
            CkGui.IconText(FAI.InfoCircle, ImGuiColors.ParsedGold.ToUint());
            DrawItemTooltip(restriction);
        }
        return ret;
    }


    protected override bool DisableCondition()
        => Current is null || !_ref.PairPerms.ApplyRestraintSets || _ref.ActiveRestraint.Identifier == Current.Id;

    protected override void OnButtonPress(int layerIdx)
    {
        if (Current is null)
            return;

        var updateType = _ref.ActiveRestrictions.Restrictions[layerIdx].Identifier == Guid.Empty
            ? DataUpdateType.Applied : DataUpdateType.Swapped;

        // construct the dto to send.
        var dto = new PushKinksterActiveRestriction(_ref.UserData, updateType)
        {
            Layer = layerIdx,
            RestrictionId = Current.Id,
            Enabler = MainHub.UID,
        };

        UiService.SetUITask(async () =>
        {
            var result = await _mainHub.UserChangeKinksterActiveRestriction(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform ApplyRestraint with {Current.Label} on {_ref.GetNickAliasOrUid()}, Reason:{result.ErrorCode}", LoggerType.StickyUI);
            }
            else
            {
                Log.LogDebug($"Applying Restraint with {Current.Label} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
                PostButtonPress.Invoke();
            }
        });
    }

    // make this generic within an extention utility.
    private void DrawItemTooltip(KinksterRestriction setItem)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
                .Push(ImGuiStyleVar.WindowRounding, 4f)
                .Push(ImGuiStyleVar.PopupBorderSize, 1f);
            using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
            ImGui.BeginTooltip();
            ImGui.Text("Im a fancy tooltip!");
            ImGui.EndTooltip();
        }
    }
}

