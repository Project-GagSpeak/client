using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Penumbra.GameData.Gui.Debug;

/// <summary> Draw all data associated to identification and resolvers. </summary>
public class IdentificationDrawer(
    ObjectIdentification _identifier,
    GamePathParser _gamePathParser,
    DictAction _actions,
    DictEmote _emotes,
    DictModelChara _modelCharas,
    IdentificationListEquipment _equipment,
    IdentificationListWeapons _weapons,
    IdentificationListModels _models) : IGameDataDrawer
{
    /// <inheritdoc/>
    public string Label
        => "Object Identification";

    /// <inheritdoc/>
    public bool Disabled
        => !_identifier.Finished;


    /// <inheritdoc/>
    public void Draw()
    {
        DrawParser();
        DrawIdentifier();
        DrawDictModelChara();
        DrawIdentificationList("Equipment List", ref _equipmentFrom, ref _equipmentTo, ref _equipmentFilter, _equipment, t => t.Item1);
        DrawIdentificationList("Weapon List",    ref _weaponsFrom,   ref _weaponsTo,   ref _weaponsFilter,   _weapons,   t => t.Item1);
        DrawIdentificationList("Model List", ref _modelsFrom, ref _modelsTo, ref _modelFilter, _models,
            t => _modelCharas.TryGetValue(t.RowId, out var names)
                ? string.Join(", ", names.Select(n => n.Name).Distinct())
                : t.RowId.ToString());
        DrawLuminaDict("Emotes",  ref _emoteFilter,  _emotes,  e => e.Name.ExtractText());
        DrawLuminaDict("Actions", ref _actionFilter, _actions, a => a.Name.ExtractText());
    }

    // Input
    private string          _gamePath = string.Empty;
    private EquipSlot       _slot     = EquipSlot.Head;
    private CharacterWeapon _weapon;

    // Filters
    private ulong  _equipmentFrom;
    private ulong  _weaponsFrom;
    private ulong  _modelsFrom;
    private ulong  _equipmentTo      = ulong.MaxValue;
    private ulong  _weaponsTo        = ulong.MaxValue;
    private ulong  _modelsTo         = ulong.MaxValue;
    private string _equipmentFilter  = string.Empty;
    private string _weaponsFilter    = string.Empty;
    private string _modelFilter      = string.Empty;
    private string _modelCharaFilter = string.Empty;
    private string _actionFilter     = string.Empty;
    private string _emoteFilter      = string.Empty;

    /// <summary> Draw a game path parser. </summary>
    private void DrawParser()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Parse Game Path");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##gamePath", "Enter game path...", ref _gamePath, 256);
        var fileInfo = _gamePathParser.GetFileInfo(_gamePath);
        ImGui.TextUnformatted(
            $"{fileInfo.ObjectType} {fileInfo.EquipSlot} {fileInfo.PrimaryId} {fileInfo.SecondaryId} {fileInfo.Variant} {fileInfo.BodySlot} {fileInfo.CustomizationType}");
        Text(string.Join("\n", _identifier.Identify(_gamePath).Keys));
    }

    /// <summary> Draw an equip item identifier. </summary>
    private void DrawIdentifier()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Identify Model");
        ImGui.SameLine();
        EquipSlotCombo.Draw("##Slot", string.Empty, ref _slot);
        ImGui.SameLine();
        ModelInput.DrawWeaponInput(ref _weapon);

        var items = _identifier.Identify(_weapon.Skeleton, _weapon.Weapon, _weapon.Variant, _slot);
        foreach (var item in items)
            Text(item.Name);
    }

    /// <summary> Draw a list of keys to items optimized for identification. </summary>
    private static void DrawIdentificationList<T>(string name, ref ulong from, ref ulong to, ref string filter, KeyList<T> keyList,
        Func<T, string> drawValue)
    {
        using var tree = ImRaii.TreeNode(name);
        if (!tree)
            return;

        var resetScroll = ImGui.InputTextWithHint("##filter", "Filter...", ref filter, 256);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("From");
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        float width;
        using (_ = ImRaii.PushFont(UiBuilder.MonoFont))
        {
            width = ImGui.CalcTextSize("0000000000000000").X + 2 * ImGui.GetStyle().FramePadding.X;
            ImGui.SetNextItemWidth(width);
            resetScroll |= ImGuiUtil.InputUlong("##From", ref from, "%016llx", ImGuiInputTextFlags.CharsHexadecimal);
        }

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("to");
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        using (_ = ImRaii.PushFont(UiBuilder.MonoFont))
        {
            ImGui.SetNextItemWidth(width);
            resetScroll |= ImGuiUtil.InputUlong("##To", ref to, "%016llx", ImGuiInputTextFlags.CharsHexadecimal);
        }

        var height = ImGui.GetTextLineHeightWithSpacing() + 2 * ImGui.GetStyle().CellPadding.Y;
        using var table = ImRaii.Table("##table", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(-1, 10 * height));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);
        ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthFixed, width);
        ImGui.TableSetupColumn("2", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(height);
        ImGui.TableNextColumn();
        var f       = filter;
        var tmpFrom = from;
        var tmpTo   = to;
        var remainder = ImGuiClip.FilteredClippedDraw(keyList.Value.Select(p => (p.Key, p.Key.ToString("X16"), drawValue(p.Data))), skips,
            p => p.Key >= tmpFrom
             && p.Key <= tmpTo
             && (p.Item2.Contains(f, StringComparison.OrdinalIgnoreCase) || p.Item3.Contains(f, StringComparison.OrdinalIgnoreCase)),
            p =>
            {
                using (_ = ImRaii.PushFont(UiBuilder.MonoFont))
                {
                    ImGuiUtil.DrawTableColumn(p.Item2);
                }

                ImGuiUtil.DrawTableColumn(p.Item3);
            });
        ImGuiClip.DrawEndDummy(remainder, height);
    }

    /// <summary> Draw a dict associating name parts with objects. </summary>
    private static void DrawLuminaDict<T>(string name, ref string filter, DictLuminaName<T> dict,
        Func<T, string> drawValue)
    {
        using var tree = ImRaii.TreeNode(name);
        if (!tree)
            return;

        var resetScroll = ImGui.InputTextWithHint("##filter", "Filter...", ref filter, 256);
        var height      = ImGui.GetTextLineHeightWithSpacing() + 2 * ImGui.GetStyle().CellPadding.Y;
        using var table = ImRaii.Table("##table", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(-1, 10 * height));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);
        ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthStretch, 0.3f);
        ImGui.TableSetupColumn("2", ImGuiTableColumnFlags.WidthStretch, 0.7f);
        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(height);
        ImGui.TableNextColumn();
        var f = filter;
        var remainder = ImGuiClip.FilteredClippedDraw(dict.Value.Select(p => (p.Key, string.Join(", ", p.Value.Select(drawValue)))), skips,
            p => p.Key.Contains(f, StringComparison.OrdinalIgnoreCase) || p.Item2.Contains(f, StringComparison.OrdinalIgnoreCase),
            p =>
            {
                ImGuiUtil.DrawTableColumn(p.Key);
                ImGuiUtil.DrawTableColumn(p.Item2);
            });
        ImGuiClip.DrawEndDummy(remainder, height);
    }

    /// <summary> Draw all collected model character to names. </summary>
    private void DrawDictModelChara()
    {
        DebugUtility.DrawNameTable("Model Chara List", ref _modelCharaFilter, true,
            _modelCharas.Select(kvp => ((ulong)kvp.Key.Id, string.Join(", ", kvp.Value.Select(n => n.Name).Distinct()))));
    }

    /// <summary> Draw text only if it is not empty. </summary>
    private static void Text(string text)
    {
        if (text.Length > 0)
            ImGui.TextUnformatted(text);
    }
}
