using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OtterGui;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Gui.Debug;

/// <summary> Draw a resolver for restricted gear. </summary>
public class RestrictedGearDrawer(RestrictedGear _restrictedGear, RestrictedItemsMale _male, RestrictedItemsFemale _female) : IGameDataDrawer
{
    /// <inheritdoc/>
    public string Label
        => "Restricted Gear Data";

    /// <inheritdoc/>
    public bool Disabled
        => !_restrictedGear.Finished;

    private CharacterArmor _resolveArmor;

    /// <inheritdoc/>
    public void Draw()
    {
        DrawResolver();
        DrawRacialData();
        DrawDictData("Male-Restricted Sets",   _male.Value);
        DrawDictData("Female-Restricted Sets", _female.Value);
    }

    /// <summary> Draw input and result for resolving restricted models. </summary>
    private void DrawResolver()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Resolve Model");
        ModelInput.DrawArmorInput(ref _resolveArmor);
        foreach (var race in Enum.GetValues<Race>().Skip(1))
        {
            ReadOnlySpan<Gender> genders = [Gender.Male, Gender.Female];
            foreach (var gender in genders)
            {
                foreach (var slot in EquipSlotExtensions.EqdpSlots)
                {
                    var (replaced, model) = _restrictedGear.ResolveRestricted(_resolveArmor, slot, race, gender);
                    if (replaced)
                        ImGui.TextUnformatted($"{race.ToName()} - {gender} - {slot.ToName()} resolves to {model}.");
                }
            }
        }
    }

    private static void DrawRacialData()
    {
        using var tree = ImRaii.TreeNode("Race-Restricted Sets");
        if (!tree)
            return;

        using var table = ImRaii.Table("race", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table)
            return;

        foreach (var (value, idx) in RestrictedItemsRace.RaceGenderGroup.WithIndex())
        {
            var armor  = new CharacterArmor((PrimaryId)(value & 0xFFFF), (Variant)(value >> 16), StainIds.None);
            var gender = idx % 2 == 0 ? Gender.Male : Gender.Female;
            var race   = (Race)(idx / 2 + 1);
            ImGuiUtil.DrawTableColumn(race.ToName());
            ImGuiUtil.DrawTableColumn(gender.ToName());
            ImGuiUtil.DrawTableColumn(armor.Set.Id.ToString());
            ImGuiUtil.DrawTableColumn(armor.Variant.Id.ToString());
        }
    }

    private static void DrawDictData(string name, IReadOnlyDictionary<uint, uint> dict)
    {
        using var tree = ImRaii.TreeNode(name);
        if (!tree)
            return;

        using var table = ImRaii.Table("table", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table)
            return;

        foreach (var (value, replacement) in dict)
        {
            var armor  = new CharacterArmor((PrimaryId)(value & 0xFFFF),       (Variant)(value >> 16),       StainIds.None);
            var armor2 = new CharacterArmor((PrimaryId)(replacement & 0xFFFF), (Variant)(replacement >> 16), StainIds.None);
            var slot   = (EquipSlot)(value >> 24);
            ImGuiUtil.DrawTableColumn(slot.ToName());
            ImGuiUtil.DrawTableColumn(armor.Set.Id.ToString());
            ImGuiUtil.DrawTableColumn(armor.Variant.Id.ToString());
            ImGuiUtil.DrawTableColumn(armor2.Set.Id.ToString());
            ImGuiUtil.DrawTableColumn(armor2.Variant.Id.ToString());
        }
    }
}
