using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using ImGuiNET;
using System.Numerics;

namespace GagSpeak.UI.Components;

public abstract class IconTabBarBase<ITab> where ITab : Enum
{
    protected record TabButtonDefinition(FontAwesomeIcon Icon, ITab TargetTab, string Tooltip, Action? CustomAction = null);

    protected readonly List<TabButtonDefinition> _tabButtons = new(); // Store tab data
    private ITab _selectedTab;
    protected readonly UiSharedService UiSharedService;

    public ITab TabSelection
    {
        get => _selectedTab;
        set
        {
            _selectedTab = value;
            OnTabSelectionChanged(value);
        }
    }

    protected IconTabBarBase(UiSharedService uiSharedService) => UiSharedService = uiSharedService;

    protected virtual void OnTabSelectionChanged(ITab newTab) { }
    protected virtual bool IsTabDisabled(ITab tab) => false;

    public void AddDrawButton(FontAwesomeIcon icon, ITab targetTab, string tooltip, Action? customAction = null)
    {
        _tabButtons.Add(new TabButtonDefinition(icon, targetTab, tooltip, customAction));
    }

    protected void DrawTabButton(TabButtonDefinition tab, Vector2 buttonSize, Vector2 spacing, ImDrawListPtr drawList)
    {
        var x = ImGui.GetCursorScreenPos();

        var isDisabled = IsTabDisabled(tab.TargetTab);
        using (ImRaii.Disabled(isDisabled))
        {

            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button(tab.Icon.ToIconString(), buttonSize))
                    TabSelection = tab.TargetTab;
            }

            ImGui.SameLine();
            var xPost = ImGui.GetCursorScreenPos();

            if (EqualityComparer<ITab>.Default.Equals(TabSelection, tab.TargetTab))
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
                    UiSharedService.DrawOutlinedFont(ImGui.GetWindowDrawList(), messageText, messageCountPosition, ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGold), 0xFF000000, 1);
                }
            }
        }
        UiSharedService.AttachToolTip(tab.Tooltip);

        // Execute custom action if provided
        tab.CustomAction?.Invoke();
    }

    public abstract void Draw(float widthAvailable);
}

