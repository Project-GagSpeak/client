using Dalamud.Interface;
using Dalamud.Interface.Colors;
using GagSpeak.CustomCombos;
using GagSpeak.Restrictions;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Widgets;

namespace GagSpeak.UI.Components.Combos;

// do this last or you will lose your god damn mind.
public sealed class GagComboClient : CkFilterComboCache<GagType>
{
    private readonly GagspeakMediator _mediator;
    private readonly GagRestrictionManager _gags;
    private GagLayer Layer { get; init; }

    public GagComboClient(GagLayer layer, ILogger log, GagspeakMediator mediator, GagRestrictionManager gags, UiSharedService uiShared) 
        : base(Enum.GetValues<GagType>().Where(x => x != GagType.None), log, uiShared, "Apply", "Apply a Gag to the "+layer.ToString())
    {
        _mediator = mediator;
        _gags = gags;
        Layer = layer;
    }

    // override the method to extract items by extracting all gagTypes.
    protected override IReadOnlyList<GagType> ExtractItems() => Enum.GetValues<GagType>().Where(x => x != GagType.None).ToList();
    protected override GagType CurrentActiveItem() => _gags.ActiveGags?.GagSlots[Layer.ToIndex()].GagType.ToGagType() ?? GagType.None;
    protected override string ToItemString(GagType item) => item.GagName();

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(GagType gagItem, bool selected)
    {
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(gagItem.GagName(), selected);

        // IF the GagType is active in the gag storage, draw it's link icon.
        if (_gags.Storage.IsActiveWithData(gagItem))
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
            // This means that the GagType is active, and has an active Glamour, Mod, or Moodle attached.
            _uiShared.IconText(FontAwesomeIcon.Link, ImGui.GetColorU32(ImGuiColors.HealerGreen));
        }
        return ret;
    }

    protected override bool DisableCondition() => _gags.ActiveGags?.GagSlots[Layer.ToIndex()].IsLocked() ?? true;
    protected override void OnItemSelected(GagType newGagSelection)
    {
        // return if we are not allow to do the application.
        if (!_gags.CanApply(Layer, newGagSelection))
            return;

        // construct the new information we will send off to the server. (make sure to not modify our actual data)
        var newSlotData = new GagSlot { GagType = newGagSelection.GagName(), Assigner = MainHub.UID };
        // publish the event to push the request to the server.
        _mediator.Publish(new AppearanceDataCreatedMessage(GagUpdateType.Applied, Layer, newSlotData));
    }

    protected override void OnClearActiveItem()
    {
        if(!_gags.CanRemove(Layer))
            return;
        // publish the event to push the request to the server.
        _mediator.Publish(new AppearanceDataCreatedMessage(GagUpdateType.Applied, Layer, new GagSlot()));
    }
}

