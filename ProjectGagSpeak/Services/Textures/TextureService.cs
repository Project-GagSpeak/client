using Dalamud.Interface.Textures.TextureWraps;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

// pulled from glamourer pretty much 1:1... optimize later.
// This is actually a fairly smart way to handle texture caching for internal gamedata.
// If it is possible to reconstruct this into an internal game storage cache for job actions and other things, do so.
namespace GagSpeak.Services.Textures;

public sealed class TextureService() : TextureCache(Svc.Data, Svc.Texture), IDisposable
{
    private readonly IDalamudTextureWrap?[] _slotIcons = CreateSlotIcons();

    public (Dalamud.Bindings.ImGui.ImTextureID?, Vector2, bool) GetIcon(EquipItem item, EquipSlot slot)
    {
        if (item.IconId.Id != 0 && TryLoadIcon(item.IconId.Id, out var ret))
            return (ret.Handle, new Vector2(ret.Width, ret.Height), false);

        var idx = slot.ToIndex();
        return idx < 12 && _slotIcons[idx] != null
            ? (_slotIcons[idx]!.Handle, new Vector2(_slotIcons[idx]!.Width, _slotIcons[idx]!.Height), true)
            : (null, Vector2.Zero, true);
    }

    public (Dalamud.Bindings.ImGui.ImTextureID?, Vector2, bool) GetIcon(EquipItem item, BonusItemFlag slot)
    {
        if (item.IconId.Id != 0 && TryLoadIcon(item.IconId.Id, out var ret))
            return (ret.Handle, new Vector2(ret.Width, ret.Height), false);

        var idx = slot.ToIndex();
        if (idx == uint.MaxValue)
            return (null, Vector2.Zero, true);

        idx += 12;
        return idx < 13 && _slotIcons[idx] != null
            ? (_slotIcons[idx]!.Handle, new Vector2(_slotIcons[idx]!.Width, _slotIcons[idx]!.Height), true)
            : (null, Vector2.Zero, true);
    }

    public void Dispose()
    {
        for (var i = 0; i < _slotIcons.Length; ++i)
        {
            _slotIcons[i]?.Dispose();
            _slotIcons[i] = null;
        }
    }

    private static IDalamudTextureWrap?[] CreateSlotIcons()
    {
        var ret = new IDalamudTextureWrap?[13];

        using var uldWrapper = Svc.PluginInterface.UiBuilder.LoadUld("ui/uld/Character.uld");

        if (!uldWrapper.Valid)
        {
            return ret;
        }

        SetIcon(EquipSlot.Head, 19);
        SetIcon(EquipSlot.Body, 20);
        SetIcon(EquipSlot.Hands, 21);
        SetIcon(EquipSlot.Legs, 23);
        SetIcon(EquipSlot.Feet, 24);
        SetIcon(EquipSlot.Ears, 25);
        SetIcon(EquipSlot.Neck, 26);
        SetIcon(EquipSlot.Wrists, 27);
        SetIcon(EquipSlot.RFinger, 28);
        SetIcon(EquipSlot.MainHand, 17);
        SetIcon(EquipSlot.OffHand, 18);
        Set(BonusItemFlag.Glasses.ToName(), (int)BonusItemFlag.Glasses.ToIndex() + 12, 55);
        ret[EquipSlot.LFinger.ToIndex()] = ret[EquipSlot.RFinger.ToIndex()];

        return ret;

        void Set(string name, int slot, int index)
        {
            try
            {
                ret[slot] = uldWrapper.LoadTexturePart("ui/uld/Character_hr1.tex", index)!;
            }
            catch (Bagagwa)
            {
/*                logger.LogError($"Could not get empty slot texture for {name}, icon will be left empty. "
                  + $"This may be because of incompatible mods affecting your character screen interface:\n{ex}");*/
                ret[slot] = null;
            }
        }

        void SetIcon(EquipSlot slot, int index)
            => Set(slot.ToName(), (int)slot.ToIndex(), index);
    }
}
