using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerState.Visual;
using ImGuiNET;

namespace GagSpeak.UI.Components;

public class PuppeteerTabs : IconTabBarBase<PuppeteerTabs.SelectedTab>
{
    public enum SelectedTab
    {
        GlobalAliasList,
        TriggerPhrases,
        PairAliasList,
        OtherPairAliasList,
    }

    private readonly PuppeteerManager _manager;
    public PuppeteerTabs(PuppeteerManager manager)
    {
        _manager = manager;
        AddDrawButton(FontAwesomeIcon.Globe, SelectedTab.GlobalAliasList, "Global Alias List");
        AddDrawButton(FontAwesomeIcon.QuoteLeft, SelectedTab.TriggerPhrases, "Trigger Phrases");
        AddDrawButton(FontAwesomeIcon.List, SelectedTab.PairAliasList, "Pair Alias List");
        AddDrawButton(FontAwesomeIcon.ListAlt, SelectedTab.OtherPairAliasList, "Other Pair Alias List");
    }

    protected override bool IsTabDisabled(SelectedTab tab)
    {
        return tab switch
        {
            SelectedTab.GlobalAliasList => false,
            SelectedTab.TriggerPhrases => false,
            SelectedTab.PairAliasList => _manager.ActiveEditorPair is null,
            SelectedTab.OtherPairAliasList => _manager.ActiveEditorPair is null,
            _ => true,
        };

    }

    public override void Draw(float availableWidth)
    {
        if (_tabButtons.Count == 0)
            return;

        using var btncolor = ImRaii.PushColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0)));
        var spacing = ImGui.GetStyle().ItemSpacing;
        var buttonX = (availableWidth - (spacing.X * (_tabButtons.Count - 1))) / _tabButtons.Count;
        var buttonY = CkGui.IconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);
        var drawList = ImGui.GetWindowDrawList();

        ImGuiHelpers.ScaledDummy(spacing.Y / 2f);

        foreach (var tab in _tabButtons)
            DrawTabButton(tab, buttonSize, spacing, drawList);

        // advance to the new line and dispose of the button color.
        ImGui.NewLine();
        btncolor.Dispose();

        ImGuiHelpers.ScaledDummy(3f);
        ImGui.Separator();
    }

}
