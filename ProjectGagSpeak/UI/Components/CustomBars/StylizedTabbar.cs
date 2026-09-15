using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using OtterGui.Text;
using GagSpeak.Services;
using GagSpeak.Utils;

namespace GagSpeak.Gui.Components;

[Flags]
public enum TabBarFlags
{
    None        = 0 << 0,
    LineGlow    = 1 << 0,
    HoverBorder = 1 << 1,
    Overlay     = 1 << 2,
    PulseColor  = 1 << 3,
    Dividers    = 1 << 4,

    // Masks
    Minimal = Dividers,
    MinimalGlow = LineGlow | Dividers,
    Animated = LineGlow | HoverBorder | Overlay,
    AnimatedPulse = LineGlow | HoverBorder | Overlay | PulseColor
}

/// <summary>
///   A customizable modern navbar for navigation.
///   Methods can be overriden for additional functionality.
/// </summary>
public class StylizedTabbar<ITab> where ITab : struct, Enum
{
    public sealed record StylizedTab(ITab Tab, string Label, FAI? Icon = null, uint? Col = null, string? Tooltip = null);

    // Tab Selection
    protected readonly List<StylizedTab> _tabButtons = [];
    private ITab _selectedTab;

    // Animation Helpers.
    protected Dictionary<ITab, ImRect> _overlayCellRect = [];
    protected ITab? _prevTab;
    protected float _changeTime = 0f;

    public StylizedTabbar()
    {
        _selectedTab = default;
    }

    // Stylization
    public float SlideTime { get; set; } = 0.35f;
    public uint TextCol { get; set; } = GsCol.Text.Uint();
    public uint TextMutedCol { get; set; } = GsCol.TextMuted.Uint();
    public uint TextFaintCol { get; set; } = GsCol.TextFaint.Uint();
    public uint SeparatorCol { get; set; } = CkCol.Divider.Uint();
    public uint LineCol { get; set; } = GsCol.VibrantPink.Uint();

    public virtual ITab TabSelection
    {
        get => _selectedTab;
        set
        {
            var prev = _selectedTab;
            _selectedTab = value;
            OnTabChanged(prev, value);
        }
    }

    protected virtual bool IsTabDisabled(ITab tab)
        => false;

    protected virtual void OnTabChanged(ITab oldTab, ITab newTab)
    {
        _prevTab = oldTab;
        _changeTime = (float)ImGui.GetTime();
    }

    public void AddTab(StylizedTab tab)
        => _tabButtons.Add(tab);

    public void AddTab(ITab tab, string label, FAI? icon = null, uint? col = null, string? tooltip = null)
        => _tabButtons.Add(new StylizedTab(tab, label, icon, col, tooltip));

    // Left-Aligned
    public void DrawTabs(TabBarFlags flags)
        => DrawTabs(null, ImUtf8.FrameHeight, ImGui.GetStyle().FrameRounding, flags);

    public void DrawTabs(float? tabWidth = null, float? height = null, TabBarFlags flags = TabBarFlags.Minimal)
        => DrawTabs(tabWidth, height ?? ImUtf8.FrameHeight, ImGui.GetStyle().FrameRounding, flags);

    public void DrawTabs(float? tabWidth = null, float? height = null, float? rounding = null, TabBarFlags flags = TabBarFlags.Minimal)
    {
        if (_tabButtons.Count is 0)
            return;

        var wdl = ImGui.GetWindowDrawList();
        height ??= ImUtf8.FrameHeight;
        rounding ??= ImGui.GetStyle().FrameRounding;

        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0)
            .Push(ImGuiCol.ButtonHovered, GsCol.HoverOverlay.Uint())
            .Push(ImGuiCol.ButtonActive, GsCol.ActiveOverlay.Uint());
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, rounding.Value);
        DrawTabsInternal(wdl, tabWidth, height.Value, rounding.Value, flags);
    }

    // Centered.
    public void DrawCenteredTabs(float? availableWidth = null, float? height = null, float? rounding = null, TabBarFlags flags = TabBarFlags.Minimal)
    {
        if (_tabButtons.Count is 0)
            return;

        var wdl = ImGui.GetWindowDrawList();
        availableWidth ??= ImGui.GetContentRegionAvail().X;
        height ??= ImUtf8.FrameHeight;
        rounding ??= ImGui.GetStyle().FrameRounding;
        var spacing = ImUtf8.ItemSpacing.X * (((flags & TabBarFlags.Dividers) != 0) ? 2 : 1);

        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0)
            .Push(ImGuiCol.ButtonHovered, GsCol.HoverOverlay.Uint())
            .Push(ImGuiCol.ButtonActive, GsCol.ActiveOverlay.Uint());
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, rounding.Value);

        // This can be unreliable when some tabs have extremely long labels, as it
        // would both cut off and not span the entire width.
        // Could precompute before we draw, but it would be more costly.
        var tabWidth = (availableWidth - (spacing * (_tabButtons.Count - 1))) / _tabButtons.Count;
        DrawTabsInternal(wdl, tabWidth, height.Value, rounding.Value, flags);
    }

    private void DrawTabsInternal(ImDrawListPtr wdl, float? tabWidth, float height, float rounding, TabBarFlags flags)
    {
        // Draw out the tabs.
        var selectedIdx = -1;
        for (var i = 0; i < _tabButtons.Count; i++)
        {
            var tab = _tabButtons[i];
            var selected = EqualityComparer<ITab>.Default.Equals(tab.Tab, TabSelection);
            DrawTab(tab, wdl, selected, height, tabWidth);
            if (selected)
                selectedIdx = i;
            // Then attach tooltip.
            CkGui.AttachTooltip(tab.Tooltip, ImGuiColors.DalamudOrange);
            // Shift for same-line draw.
            if (i < _tabButtons.Count - 1)
            {
                if ((flags & TabBarFlags.Dividers) != 0)
                {
                    ImGui.SameLine(0, ImUtf8.ItemSpacing.X * 2);

                    var pos = ImGui.GetCursorScreenPos();
                    pos -= new Vector2(ImUtf8.ItemSpacing.X, 0);
                    // Separator hairline
                    wdl.AddLine(new(pos.X, pos.Y + 5f * ImGuiHelpers.GlobalScale),
                        new(pos.X, pos.Y + height - 5f * ImGuiHelpers.GlobalScale), ImGui.GetColorU32(ImGuiCol.Separator), 1.2f * ImGuiHelpers.GlobalScale);
                }
                else
                {
                    ImGui.SameLine();
                }
            }
        }

        if (selectedIdx < 0)
            return;

        // Draw based on style.
        if (((flags & TabBarFlags.Overlay) != 0))
            DrawOverlayStyle(wdl, selectedIdx, flags);
        else
            DrawSimpleStyle(wdl, selectedIdx, flags);
    }

    // Returns true if the selected tab.
    protected virtual void DrawTab(StylizedTab tab, ImDrawListPtr wdl, bool selected, float height, float? width = null)
    {
        var pos = ImGui.GetCursorScreenPos();
        var disabled = IsTabDisabled(tab.Tab);

        using var id = ImRaii.PushId(tab.Label);
        using var dis = ImRaii.Disabled(disabled);

        var contentW = 0f;
        var min = Vector2.Zero;
        var max = Vector2.Zero;
        var ctr = Vector2.Zero;

        if (tab.Icon.HasValue)
        {
            var iconTxt = tab.Icon.Value.ToIconString();
            var iconSize = CkGui.CalcFontTextSize(iconTxt, Fonts.IconFramedFont);
            var labelSize = ImGui.CalcTextSize(tab.Label);
            // Gap.
            var iconLabelGap = 8f * ImGuiHelpers.GlobalScale;
            // Fallback to generic width if nessisary.
            contentW = iconSize.X + iconLabelGap + labelSize.X;
            var buttonW = width ?? (contentW + ImUtf8.FramePadding.X * 4);
            // Draw out the button, fallback to content width if no width spesified.
            if (ImGui.Button("##tab", new Vector2(buttonW, height)))
            {
                TabSelection = tab.Tab;
                selected = true;
            }

            var hovered = ImGui.IsItemHovered();
            min = ImGui.GetItemRectMin();
            max = ImGui.GetItemRectMax();
            ctr = (min + max) * 0.5f;

            var txtCol = selected ? TextCol : hovered ? TextMutedCol : TextFaintCol;

            // Content Draw
            var padX = width.HasValue ? (width.Value - contentW) * 0.5f : 2 * ImUtf8.FramePadding.X;
            var contentY = ctr.Y - (iconSize.Y * 0.5f);
            var drawX = min.X + padX;
            using (Fonts.IconFramedFont.Push())
                wdl.AddText(new(drawX, contentY), txtCol, iconTxt);

            drawX += iconSize.X + iconLabelGap;
            // Count baseline aligned to icon (centred vertically)
            var countY = ctr.Y - (labelSize.Y * 0.5f);
            wdl.AddText(new Vector2(drawX, countY), txtCol, tab.Label);
        }
        else
        {
            var label = tab.Label;
            var labelSize = ImGui.CalcTextSize(tab.Label);
            contentW = labelSize.X;

            // Draw out the button, fallback to content width if no width spesified.
            var buttonW = width ?? (contentW + ImUtf8.FramePadding.X * 2);
            if (ImGui.Button("##tab", new Vector2(buttonW, height)))
            {
                TabSelection = tab.Tab;
                selected = true;
            }

            var hovered = ImGui.IsItemHovered();
            min = ImGui.GetItemRectMin();
            max = ImGui.GetItemRectMax();
            ctr = (min + max) * 0.5f;

            var txtCol = selected ? TextCol : hovered ? TextMutedCol : TextFaintCol;
            var drawPos = ctr - (labelSize * 0.5f);
            wdl.AddText(drawPos, txtCol, tab.Label);
        }

        _overlayCellRect[tab.Tab] = new(min, max);
    }

    private void DrawSimpleStyle(ImDrawListPtr wdl, int selectedIdx, TabBarFlags flags)
    {
        var curTab = _tabButtons[selectedIdx];
        var curRect = _overlayCellRect[curTab.Tab];
        var col = curTab.Col ?? LineCol;
        var underY = curRect.Max.Y + 2f * ImGuiHelpers.GlobalScale;
        var ulMin = new Vector2(curRect.Min.X + 10f * ImGuiHelpers.GlobalScale, curRect.Max.Y);
        var ulMax = new Vector2(curRect.Max.X - 10f * ImGuiHelpers.GlobalScale, underY);
        if ((flags & TabBarFlags.LineGlow) != 0)
        {
            for (int i = 3; i > 0; i--)
            {
                float r = i * 2 * ImGuiHelpers.GlobalScale;
                var gMin = ulMin - new Vector2(r, r);
                var gMax = ulMax + new Vector2(r, r);
                wdl.AddRectFilled(gMin, gMax, ColorHelpers.ApplyOpacity(col, 0.12f / i));
            }
        }
        // Main Underline.
        wdl.AddRectFilled(ulMin, ulMax, col);
    }

    private void DrawOverlayStyle(ImDrawListPtr wdl, int selectedIdx, TabBarFlags flags)
    {
        var curTab = _tabButtons[selectedIdx];
        var curRect = _overlayCellRect[curTab.Tab];
        var inTransition = _prevTab is not null;
        var timeFloat = ((flags & TabBarFlags.Overlay) != 0) ? (float)ImGui.GetTime() : 0f;

        // Draw Geometry
        var curSize = curRect.Max - curRect.Min;
        var minX = 0f;
        var minY = 0f;
        var maxY = 0f;
        var boxW = 0f;
        var tEase = 0f;

        if (inTransition)
        {
            var fromRect = _overlayCellRect[_prevTab!.Value];
            var t = timeFloat - _changeTime;
            var tNorm = Math.Clamp(t / SlideTime, 0f, 1f);
            // easeOutCubic
            tEase = 1f - MathF.Pow(1f - tNorm, 3f);

            minX = GagspeakEx.Lerp(fromRect.Min.X, curRect.Min.X, tEase);
            minY = GagspeakEx.Lerp(fromRect.Min.Y, curRect.Min.Y, tEase);
            maxY = GagspeakEx.Lerp(fromRect.Max.Y, curRect.Max.Y, tEase);
            boxW = GagspeakEx.Lerp(fromRect.Max.X - fromRect.Min.X, curRect.Max.X - curRect.Min.X, tEase);

            // Stop transition when we reach the end.
            if (tNorm >= 1f)
                _prevTab = null;
        }
        else
        {
            minX = curRect.Min.X;
            minY = curRect.Min.Y;
            maxY = curRect.Max.Y;
            boxW = curRect.Max.X - curRect.Min.X;
        }

        var col = curTab.Col ?? LineCol;
        var bodyMin = new Vector2(minX, minY + ImGuiHelpers.GlobalScale);
        var bodyMax = new Vector2(minX + boxW, maxY - ImGuiHelpers.GlobalScale * 2);

        var fadeOuter = ColorHelpers.ApplyOpacity(col, 0.025f);
        var fadeInner = ColorHelpers.ApplyOpacity(col, 0.12f);
        var accentCol = ColorHelpers.ApplyOpacity(col, 0.3f);
        var midX = minX + boxW * 0.5f;
        // Body
        wdl.AddRectFilledMultiColor(bodyMin, new Vector2(midX, bodyMax.Y), fadeOuter, fadeInner, fadeInner, fadeOuter);
        wdl.AddRectFilledMultiColor(new Vector2(midX, bodyMin.Y), bodyMax, fadeInner, fadeOuter, fadeOuter, fadeInner);
        // Accents
        wdl.AddRectFilled(new Vector2(minX, minY), new Vector2(minX + boxW, minY + ImGuiHelpers.GlobalScale), accentCol);

        // Glow radiating from the line.
        if ((flags & TabBarFlags.LineGlow) != 0)
        {
            for (var i = 1; i <= 3; i++)
            {
                var pad = i * 1.5f * ImGuiHelpers.GlobalScale;
                var alpha = 0.18f / i;
                wdl.AddRectFilled(new(minX, maxY - (2 * ImGuiHelpers.GlobalScale) - pad), new(minX + boxW, maxY - 2 * ImGuiHelpers.GlobalScale), ColorHelpers.ApplyOpacity(col, alpha));
            }
        }
        // Bottom Underline.
        wdl.AddRectFilled(new(minX, maxY - (2 * ImGuiHelpers.GlobalScale)), new(minX + boxW, maxY), col);

        // Process click effects and breathing.
        if (inTransition)
        {
            var ctr = curRect.Min + (curSize * 0.5f);
            float r0 = curSize.X * 0.30f;
            float r1 = curSize.X * 1.20f;
            float rippleR = r0 + (r1 - r0) * tEase;
            float rippleA = (1f - tEase) * 0.55f;
            if (rippleA > 0.01f)
            {
                // Two concentric rings for depth - outer fades faster
                wdl.AddCircle(ctr, rippleR, ColorHelpers.ApplyOpacity(col, rippleA), 30, 1.5f);
                wdl.AddCircle(ctr, rippleR * 0.72f, ColorHelpers.ApplyOpacity(col, rippleA * 0.55f), 30, 1.0f);
            }
        }
        else if ((flags & TabBarFlags.PulseColor) != 0)
        {
            var breath = 0.5f + 0.5f * MathF.Sin(timeFloat * MathF.PI * 1.2f);
            var alphaBlend = 0.04f + 0.04f * breath;
            var maskCol = ColorHelpers.ApplyOpacity(col, alphaBlend);
            wdl.AddRectFilled(bodyMin, bodyMax, maskCol);
        }
    }
}
