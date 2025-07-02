using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.Gui.Components;

// designed for a single purpose right now, but can be abstracted later?
public sealed class LayerEditorClient
{
    private readonly GagspeakMediator _mediator;
    public RestraintLayer _selected { get; private set; } = RestraintLayer.None;
    public RestraintLayer _current;

    public LayerEditorClient(GagspeakMediator mediator)
    {
        _mediator = mediator;
    }

    public RestraintLayer Added => _selected & ~_current;
    public RestraintLayer Removed => _current & ~_selected;
    public bool IsDirty => _selected != _current;
    public void Reset() => _selected = _current;

    public void Draw(string label, RestraintLayer current, int maxLayers, Func<RestraintLayer, string>? toString = null)
    {
        using var _ = ImRaii.Group();
        // Update the current, if the current is different from previous current, update the selected to match.
        if (_current != current)
        {
            _current = current;
            _selected = current;
        }

        // Draw the checkboxes.
        var idx = 0;
        foreach (var flag in Enum.GetValues<RestraintLayer>().Skip(1).SkipLast(1))
        {
            var isSet = _selected.HasAny(flag);
            var wasSet = _current.HasAny(flag);
            var changed = isSet != wasSet;

            using var dis = ImRaii.Disabled(idx >= maxLayers);
            using var col = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen, changed);
         
            var dispName = toString?.Invoke(flag);
            var dispNameFinal = string.IsNullOrEmpty(dispName) ? flag.ToString() : dispName;
            var temp = isSet;
            if (ImGui.Checkbox($"{dispNameFinal}##{label}{flag}", ref temp))
                _selected ^= flag;

            idx++;
        }

        // Draw the apply button.
        if (CkGui.IconTextButton(FAI.Sync, "Update Layers", disabled: !IsDirty))
            OnButtonPressed();
    }

    private void OnButtonPressed()
    {
        var added = Added;
        var removed = Removed;

        // we applied and removed layers.
        if (added is not RestraintLayer.None && removed is not RestraintLayer.None)
            SendUpdate(DataUpdateType.LayersChanged);
        // we only applied layers.
        else if (added is not RestraintLayer.None)
            SendUpdate(DataUpdateType.LayersApplied);
        // we only removed layers.
        else if (removed is not RestraintLayer.None)
            SendUpdate(DataUpdateType.LayersRemoved);
        // Nothing occured.
        Reset();
    }

    private void SendUpdate(DataUpdateType type)
    {
        var newData = new CharaActiveRestraint { ActiveLayers = _selected };
        _mediator.Publish(new RestraintDataChangedMessage(type, newData));
        Reset();
    }
}
