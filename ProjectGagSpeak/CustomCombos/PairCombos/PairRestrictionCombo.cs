using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using OtterGui;
using OtterGui.Text;

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

    protected override string ToString(KinksterRestriction obj)
        => obj.Label.IsNullOrWhitespace() ? $"UNK ITEM NAME" : obj.Label;

    public bool DrawComboButton(string label, float width, int layer, string buttonTT)
        => DrawComboButton(label, width, layer, "Apply", buttonTT);

    public override bool DrawComboButton(string label, float width, int layerIdx, string bText, string tt, Action? onButtonSuccess = null)
    {
        // we need to first extract the width of the button.
        var comboWidth = width - ImGuiHelpers.GetButtonSize(bText).X - ImGui.GetStyle().ItemInnerSpacing.X;
        InnerWidth = width;

        // if we have a new item selected we need to update some conditionals.

        var previewLabel = Current is not null ? ToString(Current) : "Select an Item...";
        var ret = Draw(label, previewLabel, string.Empty, comboWidth, ImGui.GetTextLineHeightWithSpacing(), CFlags.None);
        // move just beside it to draw the button.
        ImUtf8.SameLineInner();

        // disable the button if we should.
        var disable = Current is null
            || !_ref.PairGlobals.RestrictionVisuals
            || !_ref.PairPerms.ApplyRestrictions
            || _ref.ActiveRestrictions.Restrictions[layerIdx].Identifier == Current.Id;
        if (ImGuiUtil.DrawDisabledButton(bText, new Vector2(), string.Empty, disable))
            OnButtonPress(layerIdx);
        CkGui.AttachTooltip(tt);

        return ret;
    }


    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var restriction = Items[globalIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(restriction.Label, selected);

        if (restriction.IsEnabled)
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconSize(FAI.InfoCircle).X);
            CkGui.IconText(FAI.InfoCircle, ImGuiColors.ParsedGold.ToUint());
            DrawItemTooltip(restriction);
        }
        return ret;
    }


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

    private void DrawItemTooltip(KinksterRestriction setItem)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
                .Push(ImGuiStyleVar.WindowRounding, 4f)
                .Push(ImGuiStyleVar.PopupBorderSize, 1f);
            using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
            ImGui.BeginTooltip();
            CkGui.TextWrappedTooltipFormat($"Visuals: --COL--{setItem.IsEnabled}--COL--, Slot: --COL--{setItem.Slot}--COL--" +
                $"--NL--Using --COL--{setItem.GlamItem}--COL--" +
                $"--NL--Applies --COL--{setItem.ModName}--COL--" +
                $"--NL--Hardcore Traits: --COL--{setItem.Traits}--COL--" +
                $"--NL--Arousal: --COL--{setItem.Arousal}--COL--", 320, ImGuiColors.ParsedGold);
            ImGui.EndTooltip();
        }
    }
}

