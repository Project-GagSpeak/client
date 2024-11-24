using Dalamud.Plugin.Services;
using GagSpeak.Interop.Ipc;
using GagSpeak.Services.Mediator;
using ImGuiNET;
using OtterGui.Raii;
using Penumbra.Api.Enums;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Interop.IpcHelpers.Penumbra;

public sealed class PenumbraChangedItemTooltip : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerPenumbra _penumbra;
    private readonly ItemData _itemData;
    private readonly IClientState _clientState;
    public DateTime LastTooltip { get; private set; } = DateTime.MinValue;
    public DateTime LastClick { get; private set; } = DateTime.MinValue;

    public PenumbraChangedItemTooltip(ILogger<PenumbraChangedItemTooltip> logger,
        GagspeakMediator mediator, IpcCallerPenumbra penumbra, IClientState clientState,
        ItemData itemData) : base(logger, mediator)
    {
        _penumbra = penumbra;
        _clientState = clientState;
        _itemData = itemData;
        _penumbra.Tooltip += OnPenumbraTooltip;
        _penumbra.Click += OnPenumbraClick;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _penumbra.Tooltip -= OnPenumbraTooltip;
        _penumbra.Click -= OnPenumbraClick;
    }

    public void CreateTooltip(EquipItem item, string prefix, bool openTooltip)
    {
        if (!_clientState.IsLoggedIn || _clientState.LocalContentId == 0)
        {
            return;
        }
        var slot = item.Type.ToSlot();
        switch (slot)
        {
            case EquipSlot.RFinger:
                using (_ = !openTooltip ? null : ImRaii.Tooltip())
                {
                    ImGui.TextUnformatted($"{prefix} Middle-Click to apply to assign in Restraint Editor  (Right Finger).");
                    ImGui.TextUnformatted($"{prefix} SHIFT + Middle-Click to assign to opened Cursed Item (Right Finger).");
                    ImGui.TextUnformatted($"{prefix} CTRL + Middle-Click to assign in Restraint Editor (Left Finger).");
                    ImGui.TextUnformatted($"{prefix} CTRL + SHIFT + Middle-Click to opened Cursed Item (Left Finger).");
                }
                break;
            default:
                using (_ = !openTooltip ? null : ImRaii.Tooltip())
                {
                    ImGui.TextUnformatted($"{prefix} Middle-Click to apply to selected Restraint Set.");
                    ImGui.TextUnformatted($"{prefix} SHIFT + Middle-Click to assign to opened Cursed Item.");
                }
                break;
        }
    }

    public void ApplyItem(EquipItem item)
    {
        var slot = item.Type.ToSlot();
        switch (slot)
        {
            case EquipSlot.RFinger:
                switch (ImGui.GetIO().KeyShift, ImGui.GetIO().KeyCtrl)
                {
                    // Apply Restraint Right Finger
                    case (false, false):
                        Logger.LogDebug($"Applying {item.Name} to Right Finger.", LoggerType.IpcPenumbra);
                        Mediator.Publish(new TooltipSetItemToRestraintSetMessage(EquipSlot.RFinger, item));
                        return;
                    // Apply Restraint Left Finger
                    case (false, true):
                        Logger.LogDebug($"Applying {item.Name} to Left Finger.", LoggerType.IpcPenumbra);
                        Mediator.Publish(new TooltipSetItemToRestraintSetMessage(EquipSlot.LFinger, item));
                        return;
                    // Apply Cursed Item Right Finger
                    case (true, false):
                        Logger.LogDebug($"Applying {item.Name} to Right Finger in cursed items.", LoggerType.IpcPenumbra);
                        Mediator.Publish(new TooltipSetItemToCursedItemMessage(EquipSlot.RFinger, item));
                        return;
                    // Apply Cursed Item Left Finger
                    case (true, true):
                        Logger.LogDebug($"Applying {item.Name} to Left Finger in cursed items.", LoggerType.IpcPenumbra);
                        Mediator.Publish(new TooltipSetItemToCursedItemMessage(EquipSlot.LFinger, item));
                        return;
                }
            default:
                if (ImGui.GetIO().KeyShift)
                {
                    Logger.LogDebug($"Applying {item.Name} to {slot.ToName()} in cursedItems.", LoggerType.IpcPenumbra);
                    Mediator.Publish(new TooltipSetItemToCursedItemMessage(slot, item));
                    return;
                }
                Logger.LogDebug($"Applying {item.Name} to {slot.ToName()}.", LoggerType.IpcPenumbra);
                Mediator.Publish(new TooltipSetItemToRestraintSetMessage(slot, item));
                return;
        }
    }

    private void OnPenumbraTooltip(ChangedItemType type, uint id)
    {
        LastTooltip = DateTime.UtcNow;
        if (!_clientState.IsLoggedIn || _clientState.LocalContentId == 0)
        {
            return;
        }

        if (type == ChangedItemType.Item)
        {
            if (!_itemData.TryGetValue(id, type is ChangedItemType.Item ? EquipSlot.MainHand : EquipSlot.OffHand, out var item))
            {
                return;
            }
            CreateTooltip(item, "[GagSpeak] ", false);
            return;
        }
    }

    private void OnPenumbraClick(MouseButton button, ChangedItemType type, uint id)
    {
        LastClick = DateTime.UtcNow;
        if (button is not MouseButton.Middle)
            return;

        if (!_clientState.IsLoggedIn || _clientState.LocalContentId == 0)
        {
            return;
        }

        if (type == ChangedItemType.Item)
        {
            if (!_itemData.TryGetValue(id, type is ChangedItemType.Item ? EquipSlot.MainHand : EquipSlot.OffHand, out var item))
            {
                return;
            }
            ApplyItem(item);
        }
    }
}
