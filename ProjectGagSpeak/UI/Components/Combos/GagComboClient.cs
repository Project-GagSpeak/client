using Dalamud.Interface;
using Dalamud.Interface.Colors;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.UI.Components.Combos;

// For self application custom gags.
public sealed class GagComboClient : GagspeakComboBase<GagType>
{
    private readonly GagspeakMediator _mediator;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly AppearanceManager _appearance;
    private readonly ClientData _gagData;

    private int GagSlotLayer { get; init; }

    public GagComboClient(int gagLayer, GagspeakMediator mediator, ClientData gagData, ClientConfigurationManager clientConfigs,
        AppearanceManager appearance, UiSharedService uiShared, ILogger log, string label) : base(log, uiShared, label)
    {
        _mediator = mediator;
        _clientConfigs = clientConfigs;
        _gagData = gagData;
        _appearance = appearance;

        GagSlotLayer = gagLayer;
    }

    // override the method to extract items by extracting all gagTypes.
    protected override IReadOnlyList<GagType> ExtractItems() => Enum.GetValues<GagType>().Where(x => x != GagType.None).ToList();
    protected override GagType CurrentActiveItem() => _gagData.AppearanceData?.GagSlots[GagSlotLayer].GagType.ToGagType() ?? GagType.None;
    protected override string ToItemString(GagType item) => item.GagName();

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(GagType gagItem, bool selected)
    {
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(gagItem.GagName(), selected);

        // IF the GagType is active in the gag storage, draw it's link icon.
        if (_clientConfigs.IsGagEnabled(gagItem))
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
            _uiShared.IconText(FontAwesomeIcon.Link, ImGui.GetColorU32(ImGuiColors.HealerGreen));
        }
        return ret;
    }

    protected override bool DisableCondition() => _gagData.AppearanceData?.GagSlots[GagSlotLayer].IsLocked() ?? true;
    protected override async void OnItemSelected(GagType newGagSelection)
    {
        // return if we are not allow to do the application.
        if (!_appearance.CanGagApply((GagLayer)GagSlotLayer))
            return;

        // apply update
        await _appearance.SwapOrApplyGag((GagLayer)GagSlotLayer, newGagSelection, MainHub.UID, false);
    }

    protected override async void OnClearActiveItem()
    {
        // return if we are not allow to do the application.
        if (_gagData.AppearanceData is null)
        {
            _logger.LogTrace("Appearance Data is Null, Skipping.");
            return;
        }

        // reject if locked.
        if (_gagData.AppearanceData.GagSlots[GagSlotLayer].IsLocked())
        {
            _logger.LogTrace("Gag Slot is Locked, Skipping.");
            return;
        }

        // reject if the current gagtype is the same as the new gagtype.
        if (_gagData.AppearanceData.GagSlots[GagSlotLayer].GagType.ToGagType() is GagType.None)
            return;

        // otherwise, remove it.
        await _appearance.GagRemoved((GagLayer)GagSlotLayer, MainHub.UID, true, false);
    }
}

