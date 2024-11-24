using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.UI.Components;
public abstract class TabMenuBase<T> where T : Enum
{
    private readonly UiSharedService _uiShared;
    public TabMenuBase(UiSharedService uiSharedService)
    {
        _uiShared = uiSharedService;
    }

    /// <summary>
    /// The currently selected tab.
    /// </summary>
    public T SelectedTab { get; set; }

    /// <summary>
    /// Abstract method to get the display name of the tab
    /// </summary>
    protected abstract string GetTabDisplayName(T tab);

    /// <summary>
    /// Virtual boolean that determines if a particular tab should be displayed or not
    /// </summary>
    protected virtual bool ShouldDisplayTab(T tab)
    {
        // By default, all tabs are displayed. But can be configured to hide tabs at will.
        return true;
    }

    /// <summary>
    /// Virtual boolean that determines if a particular tab should be disabled or not
    /// </summary>
    protected virtual bool IsTabDisabled(T tab)
    {
        // By default, no tabs are disabled. But can be configured to disable tabs at will.
        return false;
    }

    /// <summary>
    /// Virtual method to get the tooltip text for a particular tab
    /// </summary>
    protected virtual string GetTabTooltip(T tab)
    {
        // By default, no tooltip is provided. But can be configured to provide tooltips for tabs.
        return string.Empty;
    }

    /// <summary>
    /// Draws out selectable list to determine what draws on the right half of the UI
    /// </summary>
    public void DrawSelectableTabMenu()
    {
        foreach (var window in Enum.GetValues(typeof(T)))
        {
            if (window.ToString() == "None" || !ShouldDisplayTab((T)window)) continue;

            var displayName = GetTabDisplayName((T)window);
            var isDisabled = IsTabDisabled((T)window);
            var tooltip = GetTabTooltip((T)window);

            using (ImRaii.Disabled(isDisabled))
            {
                using (_uiShared.UidFont.Push())
                {
                    if (ImGui.Selectable($"{displayName}", window.Equals(SelectedTab)))
                        SelectedTab = (T)window;
                }
            }
            if (!string.IsNullOrEmpty(tooltip))
                UiSharedService.AttachToolTip(tooltip);
        }
        ImGui.Spacing();
    }
}

