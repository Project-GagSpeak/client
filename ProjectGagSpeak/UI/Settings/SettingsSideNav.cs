using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using OtterGui.Text;

namespace GagSpeak.Gui.Settings;

public enum SettingsNavOption
{
    // General
    PluginUI,
    NativeUI,
    Chat,
    Notifications,
    // Modules
    Hardcore,
    Wardrobe,
    //CursedLoot,
    Puppeteer,
    Toybox,
    // Service
    ServiceSettings,
    Profile,
    Debug,
}

public class SettingsSideNav : StylizedNavBar<SettingsNavOption>
{
    public SettingsSideNav()
        : base()
    { }

    public SettingsSideNav(IReadOnlyList<NavBarGroup<SettingsNavOption>> groups)
        : base(groups)
    {
    }

    protected override void DrawInternal(float width, float rounding)
    {
        _pinkActive = GsCol.VibrantPink.Vec4().WithAlpha(0.25f).ToUint();
        _pinkPressed = GsCol.VibrantPink.Vec4().WithAlpha(0.35f).ToUint();
        _pinkHovered = GsCol.VibrantPink.Vec4().WithAlpha(0.15f).ToUint();
        _pinkFeint = GsCol.VibrantPink.Vec4().WithAlpha(0.05f).ToUint();
        base.DrawInternal(width, rounding);
    }

    private uint _pinkFeint;
    private uint _pinkHovered;
    private uint _pinkPressed;
    private uint _pinkActive;

    protected override void DrawGroupHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return;

        ImGui.TextDisabled(header);
    }

    protected override bool DrawNavElement(NavBarItem<SettingsNavOption> item, float width, bool selected)
    {
        var wdl = ImGui.GetWindowDrawList();
        var min = ImGui.GetCursorScreenPos();
        var disabled = IsDisabled(item.Id);

        using var id = ImRaii.PushId(item.Id.ToString());

        var clicked = ImGui.InvisibleButton("nav-item", new Vector2(width, ImUtf8.FrameHeight)) && !disabled;
        var hovered = !disabled && ImGui.IsItemHovered();
        var active = !disabled && selected;
        var max = ImGui.GetItemRectMax();
        // Tooltip help.
        CkGui.AttachTooltip(ToTooltip(item.Id));

        if (active)
        {
            var seam = min.X + width * 0.80f;
            wdl.AddRectFilledMultiColor(min, new Vector2(seam, max.Y), _pinkActive, _pinkFeint, _pinkFeint, _pinkActive);
            wdl.AddRectFilledMultiColor(new Vector2(seam, min.Y), max, _pinkFeint, 0, 0, _pinkFeint);
        }
        else if (hovered)
        {
            wdl.AddRectFilled(min, max, _pinkHovered);
        }

        // Glowing Vertical left bar.
        if (active)
        {
            var gap = (max.Y - min.Y) * .15f;
            var barMin = new Vector2(min.X, min.Y + gap);
            var barMax = new Vector2(min.X + 3f * ImGuiHelpers.GlobalScale, max.Y - gap);
            var step = gap / 3f;
            for (int g = 3; g >= 1; g--)
            {
                var pad = g * step;
                var gMin = new Vector2(barMin.X - pad, barMin.Y - pad);
                var gMax = new Vector2(barMax.X + pad, barMax.Y + pad);
                var gCol = GsCol.VibrantPink.Vec4().WithAlpha(0.10f / g).ToUint();
                wdl.AddRectFilled(gMin, gMax, gCol);
            }
            wdl.AddRectFilled(barMin, barMax, GsCol.LushPinkLine.Uint());
        }

        var cursorX = min.X + ImUtf8.ItemSpacing.X;
        var itemCenterY = (min.Y + max.Y) * 0.5f;

        using var dis = ImRaii.Disabled(disabled);
        ImGui.SetCursorScreenPos(min);
        CkGui.InlineSpacing();
        if (item.Icon.HasValue)
        {
            CkGui.FramedIconText(item.Icon.Value);
            ImUtf8.SameLineInner();
        }

        CkGui.TextFrameAligned(item.Label);
        return clicked && !disabled;
    }

    private string ToTooltip(SettingsNavOption option) => option switch
    {
        _ => string.Empty
    };

    protected override void DrawGroupSeparator(NavBarGroup<SettingsNavOption> group, float width)
    {
        ImGui.Spacing();
        ImGui.Separator();
    }

    protected override void OnTabChanged(SettingsNavOption oldTab, SettingsNavOption newTab)
    {
        // Nothing at the moment.
    }
}
