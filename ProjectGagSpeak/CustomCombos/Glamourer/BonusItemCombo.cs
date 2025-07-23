using GagSpeak.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Extensions;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.CustomCombos.Glamourer;

public sealed class BonusItemCombo : CkFilterComboCache<EquipItem>
{
    public readonly string Label;
    public BonusItemId _currentItem;
    private float _innerWidth;
    public PrimaryId CustomSetId { get; private set; }
    public Variant CustomVariant { get; private set; }
    public BonusItemCombo(BonusItemFlag slot, ILogger log)
        : base(() => GetItems(slot), log)
    {
        Label = GetLabel(slot);
        _currentItem = 0;
        SearchByParts = true;
    }

    protected override void DrawList(float width, float itemHeight)
    {
        base.DrawList(width, itemHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            Current = Items[NewSelection.Value];
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current.Id == _currentItem)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.Id == _currentItem);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    public bool Draw(string previewName, BonusItemId previewIdx, float width, float innerWidth, string labelDisp = "")
    {
        _innerWidth = innerWidth;
        _currentItem = previewIdx;
        CustomVariant = 0;
        return Draw($"{labelDisp}##Test{Label}", previewName, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
    }

    protected override float GetFilterWidth()
        => _innerWidth - 2 * ImGui.GetStyle().FramePadding.X;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var obj = Items[globalIdx];
        var name = ToString(obj);
        var ret = ImGui.Selectable(name, selected);
        ImGui.SameLine();
        using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF808080);
        ImGuiUtil.RightAlign($"({obj.PrimaryId.Id}-{obj.Variant.Id})");
        return ret;
    }

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => base.IsVisible(globalIndex, filter) || Items[globalIndex].ModelString.StartsWith(filter.Lower);

    protected override string ToString(EquipItem obj)
        => obj.Name;

    private static string GetLabel(BonusItemFlag slot)
    {
        var sheet = Svc.Data.GetExcelSheet<Addon>()!;
        return slot switch
        {
            BonusItemFlag.Glasses => sheet.GetRow(16050).Text.ToString() ?? "Facewear",
            BonusItemFlag.UnkSlot => sheet.GetRow(16051).Text.ToString() ?? "Facewear",
            _ => string.Empty,
        };
    }

    private static IReadOnlyList<EquipItem> GetItems(BonusItemFlag slot) 
        => ItemSvc.GetBonusItems(slot);

    protected override void OnClosePopup()
    {
        var split = Filter.Text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2 || !ushort.TryParse(split[0], out var setId) || !byte.TryParse(split[1], out var variant))
            return;

        CustomSetId = setId;
        CustomVariant = variant;
    }
}

