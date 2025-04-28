using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui;
using ImGuiNET;
using System.Runtime.CompilerServices;

namespace GagSpeak.CkCommons.Widgets;

public abstract class ImageTabBar<ITab> where ITab : Enum
{
    protected sealed record TabButton(IDalamudTextureWrap Image, ITab TargetTab, string Tooltip);

    protected readonly List<TabButton> _tabButtons = new();
    private ITab _selectedTab;
    public ITab TabSelection
    {
        get => _selectedTab;
        set
        {
            TabSelectionChanged?.Invoke(_selectedTab, value);
            _selectedTab = value;
        }
    }

    protected ImageTabBar() { }

    protected virtual bool IsTabDisabled(ITab tab) => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddDrawButton(IDalamudTextureWrap image, ITab targetTab, string tooltip)
    {
        _tabButtons.Add(new TabButton(image, targetTab, tooltip));
    }

    protected void DrawTabButton(TabButton tab, Vector2 buttonSize, ImDrawListPtr drawList, Vector2? customPadding = null)
    {
        var x = ImGui.GetCursorScreenPos();
        var padding = customPadding ?? buttonSize/6;
        var isDisabled = IsTabDisabled(tab.TargetTab);
        using (ImRaii.Disabled(isDisabled))
        {
            var isHovered = ImGui.IsMouseHoveringRect(x, x + buttonSize);
            var bgColor = isHovered ? ImGui.GetColorU32(ImGuiCol.ButtonHovered) : CkGui.Color(new Vector4(.1f, .022f, .022f, .299f));

            // draw the BG.
            drawList.AddCircleFilled(x + buttonSize / 2, buttonSize.X / 2, bgColor, 32);

            if (tab.Image is { } wrap)
            {
                var topLeft = x + padding;
                var bottomRight = x + buttonSize - padding;
                drawList.AddImage(wrap.ImGuiHandle, topLeft, bottomRight, Vector2.Zero, Vector2.One, CkGui.Color(Vector4.One));
            }

            if (EqualityComparer<ITab>.Default.Equals(TabSelection, tab.TargetTab))
            {
                drawList.AddCircle(x + buttonSize / 2, buttonSize.X / 2, CkGui.Color(ImGuiColors.ParsedGold), 32, 2);
            }

            // draw a scaled dummy over the region.
            ImGuiHelpers.ScaledDummy(buttonSize);
            if(isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                TabSelection = tab.TargetTab;
        }
        CkGui.AttachToolTip(tab.Tooltip);
    }

    public virtual void Draw(Vector2 region, Vector2? customPadding = null)
    {
        if (_tabButtons.Count == 0)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var _ = ImRaii.Child("ImageTabBar", region, false, ImGuiWindowFlags.NoDecoration);

        var buttonSize = new Vector2(region.Y);
        var spacingBetweenButtons = (region.X - buttonSize.X * _tabButtons.Count) / (_tabButtons.Count + 1);

        var pos = ImGui.GetCursorScreenPos();
        ImGui.SetCursorScreenPos(new Vector2(pos.X + spacingBetweenButtons, pos.Y));

        foreach (var tab in _tabButtons)
        {
            DrawTabButton(tab, buttonSize, ImGui.GetWindowDrawList(), customPadding);
            ImGui.SameLine(0, spacingBetweenButtons);
        }

        ImGui.SetCursorScreenPos(pos);
    }

    /// <summary> Invokes actions informing people of the previous and new tab selected. </summary>
    public event Action<ITab, ITab>? TabSelectionChanged;
}

