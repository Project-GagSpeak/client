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
    NativeColors,
    NativeStyle,
}

// No, im not yoinking this from penumbra/glamourer and making modifications to it at all. Totally not.
public class StyleEditorUI : WindowMediatorSubscriberBase
{
    private const string COLOR_PICKER_TIP = "--COL--[Left-Click Square]--COL-- Opens color picker." +
        "--NL----COL--[Right-Click Square]--COL-- Open edit options menu.";

    private readonly MainConfig _config;
    // Maybe a Theme config but thats a 2.X feature

    private StyleTab _lastTab = StyleTab.GagSpeakColors;
    private bool _themeChanged = false;
    private bool _copyModifiedOnly = false;
    private string _filterString = string.Empty;

    private ImGuiColorEditFlags _colorFlags = ImGuiColorEditFlags.AlphaPreviewHalf;

    // Atm only works for GsCols, but should try making it work for other types too, or just give them their own dictionaries.
    private Dictionary<GsCol, Vector4>    _gsColChanges     = [];
    private Dictionary<CkCol, Vector4>    _ckColChanges     = [];
    private Dictionary<ImGuiCol, Vector4> _imguiColChanges  = [];

    public StyleEditorUI(ILogger<StyleEditorUI> logger, GagspeakMediator mediator, MainConfig config)
        : base(logger, mediator, "GagSpeak Style Editor")
    {
        Flags = WFlags.NoScrollbar;
        this.SetBoundaries(new Vector2(625, 400), ImGui.GetIO().DisplaySize);
    }

    protected override void PreDrawInternal()
    { }

    protected override void PostDrawInternal()
    { }

    private string? _vec4ConvertStr = null;
    private Vector4 _parsedStr = Vector4.Zero;
    private string? _uintConvertStr = null;
    private uint _parsedUint = uint.MinValue;

    protected override void DrawInternal()
    {
        var width = ImGui.GetContentRegionAvail().X;
        var halfW = width / 2;
        CkGui.FontText("Selected Theme:", UiFontService.GagspeakLabelFont);
        if (DrawThemeCombo(width))
        {
            // Do some theme Setting here
        }

        if (CkGui.IconTextButton(FAI.Save, "Save Changes", disabled: true))
        {
            // Some uniform save, eventually, hopefully.
        }
        CkGui.AttachToolTip("Currently Non-Functional" +
            "--NL--Should Save all changes to a temporary theme template storage in the editor, " +
            "which is used as a placeholder theme until the window is exited." +
            "--SEP--Saving this as a Theme will export it and add it to your Config.");

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Redo, "Revert All Changes", disabled: true))
        {
            // Some uniform save, eventually, hopefully
        }
        CkGui.AttachToolTip("Currently Non-Functional." +
            "--NL--Should revert all applied / saved changes and revert to the selected theme.");

        ImGui.Separator();
        DrawValueConverters();

        ImGui.Separator();
        DrawStyleEditor();
    }

    private void DrawStyleEditor()
    {
        using var bar = ImRaii.TabBar("##style-editor-tabs", ImGuiTabBarFlags.None);
        GsColorTab();
        //GsStyleTab();
        CkColorTab();
        //CkStyleTab();
        //NativeColorTab();
        //NativeStyleTab();
    }

    private ImRaii.IEndObject DrawTab(StyleTab newTab)
    {
        var tab = ImRaii.TabItem(GetName(newTab));
        if (tab)
            _lastTab = newTab;
        // Can call this theoretical function if we need to make some updates on each tab swap.
        // UpdateMeta();
        return tab;

        string GetName (StyleTab tab) => tab switch
        {
            StyleTab.GagSpeakColors => "GS Colors",
            StyleTab.GagSpeakStyle => "GS Style",
            StyleTab.CkColors => "Ck Colors",
            StyleTab.CkStyle => "Ck Style",
            StyleTab.NativeColors => "ImGui Colors",
            StyleTab.NativeStyle => "ImGui Style",
            _ => string.Empty
        };
    }

    #region GagSpeak
    private void GsColorTab()
    {
        using var tab = DrawTab(StyleTab.GagSpeakColors);
        if (!tab) return;

        if (CkGui.IconTextButton(FAI.Clipboard, "Copy Vec4s"))
            GsColors.Vec4ToClipboard(_copyModifiedOnly ? _gsColChanges : GsColors.AsVec4Dictionary());

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Clipboard, "Copy Uints"))
            GsColors.UintToClipboard(_copyModifiedOnly ? _gsColChanges.ToDictionary(c => c.Key, c => c.Value.ToUint()) : GsColors.AsUintDictionary());

        ImUtf8.SameLineInner();
        ImGui.Checkbox("Only copy modified", ref _copyModifiedOnly);

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
            {
                // placeholder until we get something more stable, since the vec4 ref messes up the uint
                GsColors.Set(colIdx, vec4);
                // Would do some comparison and temporary applicatoin here idealy.
                _gsColChanges[colIdx] = GsColors.Vec4(colIdx);
            }
            // Some disabled save and reverts for the individual row.
            ImGui.SameLine();
            if (CkGui.IconButton(FAI.Save, disabled: true))
            {
                // An individual save to the temporary placeholder theme.
            }
            CkGui.AttachToolTip("Does nothing atm, but would save the individual color change.");

            ImUtf8.SameLineInner();
            if (CkGui.IconButton(FAI.Redo, disabled: !_gsColChanges.ContainsKey(colIdx)))
            {
                GsColors.RevertCol(colIdx);
                _gsColChanges.Remove(colIdx);
            }
            CkGui.AttachToolTip("Reverts any changes made to this color.");

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

        if (CkGui.IconTextButton(FAI.Clipboard, "Copy Vec4s"))
            CkColors.Vec4ToClipboard(_copyModifiedOnly ? _ckColChanges : CkColors.AsVec4Dictionary());

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Clipboard, "Copy Uints"))
            CkColors.UintToClipboard(_copyModifiedOnly ? _ckColChanges.ToDictionary(c => c.Key, c => c.Value.ToUint()) : CkColors.AsUintDictionary());

        ImUtf8.SameLineInner();
        ImGui.Checkbox("Only copy modified", ref _copyModifiedOnly);

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
            {
                // placeholder until we get something more stable, since the vec4 ref messes up the uint
                CkColors.SetDefault(colIdx, vec4);
                CkColors.ApplyTheme();
                // Would do some comparison and temporary applicatoin here idealy.
                _ckColChanges[colIdx] = CkColors.Vec4(colIdx);
            }
            // Some disabled save and reverts for the individual row.
            ImGui.SameLine();
            if (CkGui.IconButton(FAI.Save, disabled: true))
            {
                // An individual save to the temporary placeholder theme.
            }
            CkGui.AttachToolTip("Does nothing atm, but would save the individual color change.");

            ImUtf8.SameLineInner();
            if (CkGui.IconButton(FAI.Redo, disabled: !_ckColChanges.ContainsKey(colIdx)))
            {
                CkColors.RevertCol(colIdx);
                _ckColChanges.Remove(colIdx);
            }
            CkGui.AttachToolTip("Reverts any changes made to this color.");

            CkGui.TextInline(name);
            ImGui.PopID();
        }
    }
    #endregion CkCommons

    private bool DrawThemeCombo(float width)
    {
        return false;
    }

    // Helps cordys sanity
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
