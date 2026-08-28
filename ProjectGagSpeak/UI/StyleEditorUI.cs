using CkCommons;
using CkCommons.Gui;
using CkCommons.Helpers;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using OtterGui.Text;
using System.Globalization;

namespace GagSpeak.Gui;

public enum StyleTab
{
    GagSpeakColors,
    GagSpeakStyle,
    CkColors,
    CkStyle,
}

public class StyleEditorUI : WindowMediatorSubscriberBase
{
    private const string COLOR_PICKER_TIP = "--COL--[Left-Click Square]--COL-- Opens color picker." +
        "--NL----COL--[Right-Click Square]--COL-- Open edit options menu.";

    private readonly MainConfig _config;

    private StyleTab _lastTab = StyleTab.CkColors;
    private string _filterString = string.Empty;

    private ImGuiColorEditFlags _colorFlags = ImGuiColorEditFlags.AlphaPreviewHalf;

    // Atm only works for GsCols, but should try making it work for other types too, or just give them their own dictionaries.
    private Dictionary<GsCol, Vector4> _gsColChanges = [];
    private Dictionary<CkCol, Vector4> _ckColChanges = [];

    private string? _vec4ConvertStr = null;
    private Vector4 _parsedStr = Vector4.Zero;
    private string? _uintConvertStr = null;
    private uint _parsedUint = uint.MinValue;

    public StyleEditorUI(ILogger<StyleEditorUI> logger, GagspeakMediator mediator, MainConfig config)
        : base(logger, mediator, "GagSpeak Style Editor")
    {
        _config = config;
        Flags = WFlags.NoScrollbar;
        this.SetBoundaries(new Vector2(625, 400), ImGui.GetIO().DisplaySize);
    }

    protected override void DrawInternal()
    {
        var width = ImGui.GetContentRegionAvail().X;
        var halfW = width / 2;
        CkGui.FontText("Selected Theme:", Fonts.DefaultScaled);
        ImGui.Separator();
        DrawValueConverters();

        ImGui.Separator();
        DrawStyleEditor();
    }

    private void DrawStyleEditor()
    {
        using var bar = ImRaii.TabBar("##style-editor-tabs", ImGuiTabBarFlags.None);
        try
        {
            GsColorTab();
            CkColorTab();
            //CkStyleTab();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error rendering style editor: {ex}");
        }
    }

    private ImRaii.TabItemDisposable DrawTab(StyleTab newTab)
    {
        var tab = ImRaii.TabItem(GetName(newTab));
        if (tab)
            _lastTab = newTab;
        // Can call this theoretical function if we need to make some updates on each tab swap.
        // UpdateMeta();
        return tab;

        string GetName(StyleTab tab) => tab switch
        {
            StyleTab.GagSpeakColors => "Sund Colors",
            StyleTab.GagSpeakStyle => "Sund Style",
            StyleTab.CkColors => "Ck Colors",
            StyleTab.CkStyle => "Ck Style",
            _ => string.Empty
        };
    }

    #region GagSpeak
    private void GsColorTab()
    {
        using var tab = DrawTab(StyleTab.GagSpeakColors);
        if (!tab) return;

        // Search Filter for the selected colors
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        ImGui.InputTextWithHint("Display Filter", "Filter Style...", ref _filterString, 64);

        var flags = _colorFlags;
        if (ImGui.RadioButton("Opaque", flags == ImGuiColorEditFlags.NoAlpha)) { _colorFlags = ImGuiColorEditFlags.NoAlpha; } ImGui.SameLine();
        if (ImGui.RadioButton("Alpha", flags == ImGuiColorEditFlags.AlphaPreview)) { _colorFlags = ImGuiColorEditFlags.AlphaPreview; } ImGui.SameLine();
        if (ImGui.RadioButton("Alpha Half", flags == ImGuiColorEditFlags.AlphaPreviewHalf)) { _colorFlags = ImGuiColorEditFlags.AlphaPreviewHalf; }
        CkGui.HelpText(COLOR_PICKER_TIP, ImGuiColors.DalamudOrange);

        // Now frame within a child spanning the remaing region, the editable children
        using var scrollbar = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 8f);
        using var child = ImRaii.Child("##style-editor-region", ImGui.GetContentRegionAvail(), true);

        for (int i = 0; i < GsColors.Count; i++)
        {
            var colIdx = (GsCol)i;
            // Grab the current name
            var name = colIdx.ToName();
            if (_filterString.Length > 0 && !name.Contains(_filterString, StringComparison.OrdinalIgnoreCase))
                continue;

            ImGui.PushID(i);
            // This would be editing the base color directly which could prove dangerous so will
            // need to experiment with this later as we integrate push/pop
            var vec4 = GsColors.Vec4(colIdx);
            ImGui.ColorEdit4("##color", ref vec4, ImGuiColorEditFlags.AlphaBar | flags);
            if (!GsColors.Vec4(colIdx).Equals(vec4))
                UiService.GsColChanges[colIdx] = vec4;

            // Some disabled save and reverts for the individual row.
            ImGui.SameLine();
            var isDefaultCol = !GsColors.Defaults.GetValueOrDefault(colIdx).Equals(GsColors.Vec4(colIdx));
            if (CkGui.IconButton(FAI.Redo, disabled: !isDefaultCol))
                UiService.GsColChanges[colIdx] = GsColors.Defaults.GetValueOrDefault(colIdx, Vector4.One);
            CkGui.AttachTooltip("Revert this color to default value.");

            CkGui.TextInline(name);
            ImGui.PopID();
        }
    }
    #endregion GagSpeak

    #region CkCommons
    private void CkColorTab()
    {
        using var tab = DrawTab(StyleTab.CkColors);
        if (!tab) return;

        // Search Filter for the selected colors
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        ImGui.InputTextWithHint("Display Filter", "Filter Style...", ref _filterString, 64);

        var flags = _colorFlags;
        if (ImGui.RadioButton("Opaque", flags == ImGuiColorEditFlags.NoAlpha)) { _colorFlags = ImGuiColorEditFlags.NoAlpha; }
        ImGui.SameLine();
        if (ImGui.RadioButton("Alpha", flags == ImGuiColorEditFlags.AlphaPreview)) { _colorFlags = ImGuiColorEditFlags.AlphaPreview; }
        ImGui.SameLine();
        if (ImGui.RadioButton("Alpha Half", flags == ImGuiColorEditFlags.AlphaPreviewHalf)) { _colorFlags = ImGuiColorEditFlags.AlphaPreviewHalf; }
        CkGui.HelpText(COLOR_PICKER_TIP, ImGuiColors.DalamudOrange);

        // Now frame within a child spanning the remaing region, the editable children
        using var scrollbar = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 8f);
        using var child = ImRaii.Child("##style-editor-region", ImGui.GetContentRegionAvail(), true);

        for (int i = 0; i < CkColors.Count; i++)
        {
            var colIdx = (CkCol)i;
            // Grab the current name
            var name = colIdx.ToName();
            if (_filterString.Length > 0 && !name.Contains(_filterString, StringComparison.OrdinalIgnoreCase))
                continue;

            ImGui.PushID(i);
            // This would be editing the base color directly which could prove dangerous so will
            // need to experiment with this later as we integrate push/pop
            var vec4 = CkColors.Vec4(colIdx);
            ImGui.ColorEdit4("##color", ref vec4, ImGuiColorEditFlags.AlphaBar | flags);
            if (!CkColors.Vec4(colIdx).Equals(vec4))
                UiService.CkColChanges[colIdx] = vec4;

            ImGui.SameLine();
            var isDefaultCol = !CkColors.Defaults.GetValueOrDefault(colIdx).Equals(CkColors.Vec4(colIdx));
            if (CkGui.IconButton(FAI.Redo, disabled: !isDefaultCol))
                UiService.CkColChanges[colIdx] = CkColors.Defaults.GetValueOrDefault(colIdx, Vector4.One);
            CkGui.AttachTooltip("Revert this color to default value.");

            CkGui.TextInline(name);
            ImGui.PopID();
        }
    }
    #endregion CkCommons
    private void DrawValueConverters()
    {
        var halfW = ImGui.GetContentRegionAvail().X / 2;
        ImGui.SetNextItemWidth(halfW);
        _vec4ConvertStr ??= string.Empty;
        if (ImGui.InputTextWithHint("##vec4-text-here", "new Vector4(1.000f, 0.181f, 0.715f, 0.825f)...", ref _vec4ConvertStr, 300))
        {
            if (RegexEx.TryParseVec4Code(_vec4ConvertStr, out Vector4 parsed))
                _parsedStr = parsed;
            else
            {
                _vec4ConvertStr = null;
                _parsedStr = Vector4.Zero;
            }
        }
        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.ArrowRight);
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        uint c = _parsedStr.ToUint();
        var s = $"0x{c:X8}";
        ImGui.InputText("##uint-translated", ref s, 64, ITFlags.ReadOnly);

        ImGui.Separator();
        ImGui.SetNextItemWidth(halfW);
        _uintConvertStr ??= string.Empty;

        if (ImGui.InputTextWithHint("##uint-text", "0xAABBGGRR..", ref _uintConvertStr, 32))
        {
            if (TryParseHexUint(_uintConvertStr, out uint u))
                _parsedUint = u;
            else
            {
                _uintConvertStr = null;
                _parsedUint = uint.MinValue;
            }
        }
        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.ArrowRight);
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        var vec = _parsedUint.ToVec4();
        var vecStr = string.Create(CultureInfo.InvariantCulture, $"new Vector4({vec.X:0.000}f, {vec.Y:0.000}f, {vec.Z:0.000}f, {vec.W:0.000}f)");
        ImGui.InputText("##vec4-translated", ref vecStr, 200, ITFlags.ReadOnly);

        bool TryParseHexUint(string text, out uint value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text[2..];

            return uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }
    }
}
