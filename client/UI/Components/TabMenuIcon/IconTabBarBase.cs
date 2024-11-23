using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using ImGuiNET;
using System.Numerics;

namespace GagSpeak.UI.Components;

public abstract class IconTabBarBase<TTab> where TTab : Enum
{
    protected record TabButtonDefinition(FontAwesomeIcon Icon, TTab TargetTab, string Tooltip, Action? CustomAction = null);

    protected readonly List<TabButtonDefinition> _tabButtons = new(); // Store tab data
    private TTab _selectedTab;
    protected readonly UiSharedService UiSharedService;

    public TTab TabSelection
    {
        get => _selectedTab;
        set
        {
            _selectedTab = value;
            OnTabSelectionChanged(value);
        }
    }

    protected IconTabBarBase(UiSharedService uiSharedService) => UiSharedService = uiSharedService;

    protected abstract void OnTabSelectionChanged(TTab newTab);

    public void AddDrawButton(FontAwesomeIcon icon, TTab targetTab, string tooltip, Action? customAction = null)
    {
        _tabButtons.Add(new TabButtonDefinition(icon, targetTab, tooltip, customAction));
    }

    protected void DrawTabButton(TabButtonDefinition tab, Vector2 buttonSize, Vector2 spacing, ImDrawListPtr drawList)
    {
        var x = ImGui.GetCursorScreenPos();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.Button(tab.Icon.ToIconString(), buttonSize))
                TabSelection = tab.TargetTab;
        }
        
        ImGui.SameLine();
        var xPost = ImGui.GetCursorScreenPos();

        if (EqualityComparer<TTab>.Default.Equals(TabSelection, tab.TargetTab))
        {
            drawList.AddLine(
                x with { Y = x.Y + buttonSize.Y + spacing.Y },
                xPost with { Y = xPost.Y + buttonSize.Y + spacing.Y, X = xPost.X - spacing.X },
                ImGui.GetColorU32(ImGuiCol.Separator), 2f);
        }

        if (tab.TargetTab is MainMenuTabs.SelectedTab.GlobalChat)
        {
            if (DiscoverService.NewMessages > 0)
            {
                var messageCountPosition = new Vector2(x.X + buttonSize.X / 2, x.Y - spacing.Y);
                var messageText = DiscoverService.NewMessages > 99 ? "99+" : DiscoverService.NewMessages.ToString();
                UiSharedService.DrawOutlinedFont(ImGui.GetWindowDrawList(), messageText, messageCountPosition, UiSharedService.Color(ImGuiColors.ParsedGold), 0xFF000000, 1);
            }
        }
        UiSharedService.AttachToolTip(tab.Tooltip);

        // Execute custom action if provided
        tab.CustomAction?.Invoke();
    }

    public abstract void Draw();
}

