using CkCommons;
using Dalamud.Bindings.ImGui;
using GagSpeak.Services.Mediator;
using OtterGui.Raii;
using Penumbra.Api.Enums;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Interop.Helpers;

public sealed class PenumbraChangedItemTooltip : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerPenumbra _penumbra;
    private readonly ItemData _itemData;
    public DateTime LastTooltip { get; private set; } = DateTime.MinValue;
    public DateTime LastClick { get; private set; } = DateTime.MinValue;

    public PenumbraChangedItemTooltip(ILogger<PenumbraChangedItemTooltip> logger,
        GagspeakMediator mediator, IpcCallerPenumbra penumbra, ItemData itemData)
        : base(logger, mediator)
    {
        _penumbra = penumbra;
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
        if (!Svc.ClientState.IsLoggedIn || Svc.PlayerState.ContentId == 0)
        {
            return;
        }
        var slot = item.Type.ToSlot();
        switch (slot)
        {
            case EquipSlot.RFinger:
                using (_ = !openTooltip ? null : ImRaii.Tooltip())
                {
                    ImGui.TextUnformatted($"{prefix} Middle-Click to apply to assign in GagSpeak Editor  (Right Finger).");
                    ImGui.TextUnformatted($"{prefix} CTRL + Middle-Click to assign in GagSpeak Editor (Left Finger).");
                }
                break;
            default:
                using (_ = !openTooltip ? null : ImRaii.Tooltip())
                {
                    ImGui.TextUnformatted($"{prefix} Middle-Click to apply to selected Restraint Set.");
                }
                break;
        }
    }

    public void ApplyItem(EquipItem item)
    {
        var slot = item.Type.ToSlot();
        if (slot is EquipSlot.RFinger)
        {
            if (ImGui.GetIO().KeyCtrl)
            {
                Logger.LogDebug($"Applying {item.Name} to Right Finger.", LoggerType.IpcPenumbra);
                Mediator.Publish(new TooltipSetItemToEditorMessage(EquipSlot.RFinger, item));
                return;
            }
            else
            {
                Logger.LogDebug($"Applying {item.Name} to Left Finger.", LoggerType.IpcPenumbra);
                Mediator.Publish(new TooltipSetItemToEditorMessage(EquipSlot.LFinger, item));
                return;
            }
        }
        else
        {
            Logger.LogDebug($"Applying {item.Name} to {slot.ToName()}.", LoggerType.IpcPenumbra);
            Mediator.Publish(new TooltipSetItemToEditorMessage(slot, item));
            return;
        }
    }

    private void OnPenumbraTooltip(ChangedItemType type, uint id)
    {
        LastTooltip = DateTime.UtcNow;
        if (!PlayerData.IsLoggedIn || PlayerData.CID is 0)
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

        if (!PlayerData.IsLoggedIn || PlayerData.CID is 0)
            return;

        if (type is ChangedItemType.Item)
        {
            if (!_itemData.TryGetValue(id, type is ChangedItemType.Item ? EquipSlot.MainHand : EquipSlot.OffHand, out var item))
            {
                return;
            }
            ApplyItem(item);
        }
    }
}
