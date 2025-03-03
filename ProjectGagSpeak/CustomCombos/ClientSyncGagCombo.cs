using Dalamud.Interface;
using Dalamud.Interface.Colors;
using GagSpeak.CustomCombos;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace ProjectGagSpeak.CustomCombos;

// This is the 'special one'....
// Except, we likely wont even need it later probably.
// Since it will be set from the selectors instead... :fatcatKO:
public sealed class ClientSyncGagCombo : CkFilterComboCacheSync<GagType>
{
    private readonly GagspeakMediator _mediator;
    private readonly GagRestrictionManager _gags;
    private GagLayer Layer { get; init; }

    public ClientSyncGagCombo(GagLayer layer, ILogger log, GagspeakMediator mediator, GagRestrictionManager gags)
        : base(GetGagList, log)
    {
        _mediator = mediator;
        _gags = gags;
        Layer = layer;
    }

    private static IReadOnlyList<GagType> GetGagList() => Enum.GetValues<GagType>().Except([GagType.None]).ToList();

    protected override string ToString(GagType obj) => obj.GagName();

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var gagItem = Items[globalIdx];

        var ret = ImGui.Selectable(gagItem.GagName(), selected);
        // IF the GagType is active in the gag storage, draw it's link icon.
        if (_gags.Storage.IsActiveWithData(gagItem))
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
            // This means that the GagType is active, and has an active Glamour, Mod, or Moodle attached.
            CkGui.IconText(FontAwesomeIcon.Link, ImGui.GetColorU32(ImGuiColors.HealerGreen));
        }
        return ret;
    }

    protected override void OnItemSelected(GagType newGagSelection)
    {
        // return if we are not allow to do the application.
        if (!_gags.CanApply(Layer, newGagSelection) || _gags.ActiveGagsData is null)
            return;

        var updateType = _gags.ActiveGagsData.GagSlots[(int)Layer].GagItem is GagType.None
            ? DataUpdateType.Applied
            : DataUpdateType.Swapped;

        // put in place the new data we want to update on the server.
        var newSlotData = new ActiveGagSlot()
        {
            GagItem = newGagSelection,
            Enabler = MainHub.UID,
        };
        // publish the event to push the request to the server.
        _mediator.Publish(new GagDataChangedMessage(updateType, Layer, newSlotData));
        Log.LogDebug($"ClientSyncGagCombo: {Layer} GagType {newGagSelection} selected.");
    }

    public override bool Draw(string label, string preview, string tooltip, float previewWidth, float itemHeight,
        ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        var ret = base.Draw(label, preview, tooltip, previewWidth, itemHeight, flags);
        // Handle right click clearing.
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            if (!_gags.CanRemove(Layer))
                return ret;
            // clear it completely.
            _mediator.Publish(new GagDataChangedMessage(DataUpdateType.Removed, Layer, new ActiveGagSlot()));
        }
        return ret;
    }
}

