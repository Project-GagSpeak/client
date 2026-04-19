using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Interop.Helpers;
using OtterGui.Text;

namespace GagSpeak.Gui;

/// <summary>
///   Portfolio of Cordys projects.
/// </summary>
public sealed class ProjectTabBar
{
    private readonly PluginGuideProvider _imageResolver;
    private sealed record TabButtonDefinition(OptionalPlugin Tab, string IconUrl, string Name, string Tooltip);

    private readonly List<TabButtonDefinition> _tabButtons = new();
    private OptionalPlugin _selectedTab;

    public ProjectTabBar(PluginGuideProvider resolver)
    {
        _imageResolver = resolver;
        // Pull from your existing plugin info source
        var loci = PluginGuideProvider.PluginInfo[OptionalPlugin.Loci];
        var sundouleia = PluginGuideProvider.PluginInfo[OptionalPlugin.Sundouleia];
        AddProject(OptionalPlugin.Sundouleia, sundouleia.IconUrl, sundouleia.Name, sundouleia.Punchline);
        AddProject(OptionalPlugin.Loci, loci.IconUrl, loci.Name, loci.Punchline);

        _selectedTab = OptionalPlugin.Sundouleia;
    }

    public OptionalPlugin TabSelection
    {
        get => _selectedTab;
        set
        {
            if (_selectedTab != value)
                _selectedTab = value;
        }
    }

    public void AddProject(OptionalPlugin tab, string imgUrl, string name, string tooltip)
        => _tabButtons.Add(new TabButtonDefinition(tab, imgUrl, name, tooltip));

    public void Draw(float width, IFontHandle fontHandle, bool isFramed = false)
    {
        if (_tabButtons.Count == 0)
            return;

        using var color = ImRaii.PushColor(ImGuiCol.Button, 0);
        var textH = CkGui.CalcFontTextSize("A", fontHandle).Y;

        var spacing = ImUtf8.ItemSpacing;
        var height = isFramed ? textH + ImGui.GetStyle().FramePadding.Y * 2 : textH;
        
        var imgSize = new Vector2(height);
        var buttonW = (width - (spacing.X * (_tabButtons.Count - 1))) / _tabButtons.Count;
        var buttonSize = new Vector2(buttonW, height);
        var wdl = ImGui.GetWindowDrawList();

        // Draw out the buttons, with a newline after.
        for (var i = 0; i < _tabButtons.Count; i++)
        {
            DrawTabButton(_tabButtons[i], fontHandle, buttonSize, imgSize, spacing, wdl);
            if (i < _tabButtons.Count - 1)
                ImGui.SameLine();
        }

        ImGuiHelpers.ScaledDummy(3f);
        ImGui.Separator();
    }

    private void DrawTabButton(TabButtonDefinition tab, IFontHandle font, Vector2 buttonSize, Vector2 imgSize, Vector2 spacing, ImDrawListPtr wdl)
    {
        var x = ImGui.GetCursorScreenPos();
        using var group = ImRaii.Group();
        if (ImGui.Button($"##portfolio_tab_{tab.Tab}", buttonSize))
            TabSelection = tab.Tab;
        CkGui.AttachTooltip(tab.Tooltip);

        if (TabSelection == tab.Tab)
            wdl.AddLine(x with { Y = x.Y + buttonSize.Y + spacing.Y }, ImGui.GetItemRectMax() + new Vector2(0, spacing.Y), GsCol.VibrantPink.Uint(), 2f);

        using var _ = font.Push();
        
        ImGui.SetCursorScreenPos(x);

        // Resolve image (may or may not exist, layout doesn't care)
        _imageResolver.TryGetOnlineImage(tab.IconUrl, out var texture);
        var textSize = ImGui.CalcTextSize(tab.Name);

        // Image + spacing + text — always the same
        var contentWidth = imgSize.X + spacing.X + textSize.X;
        var contentStart = x + new Vector2((buttonSize.X - contentWidth) * 0.5f, (buttonSize.Y - imgSize.Y) * 0.5f);
        // Draw image slot (always)
        var cursor = contentStart;

        if (texture is { } wrap)
            wdl.AddImage(texture.Handle, cursor, cursor + imgSize);
        else
            wdl.AddRectFilled(cursor, cursor + imgSize, ImGui.GetColorU32(ImGuiCol.FrameBg));

        ImGui.SetCursorScreenPos(cursor with { X = cursor.X + imgSize.X + spacing.X });
        ImGui.Text(tab.Name);
    }
}
