using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Drawers;
using ImGuiNET;

namespace GagSpeak.UI.Components;

public class ToyboxTabs : IconTabBar<ToyboxTabs.SelectedTab>
{
    public enum SelectedTab
    {
        ToyOverview, // manage connections, active toys, audio sounds, ext.
        VibeServer, // connect to hub, create groups, invite others, access instanced vibrators for each
        PatternManager, // manage, create, or send patterns.
        TriggerManager, // create a new trigger.
        AlarmManager, // manage, create, or send alarms.
    }

    public ToyboxTabs()
    {
        AddDrawButton(FontAwesomeIcon.PersonBooth, SelectedTab.ToyOverview, "Device Manager" +
            "--SEP--Configure either Simulated Vibrators, or Intiface connected devices.");

        AddDrawButton(FontAwesomeIcon.MobileButton, SelectedTab.VibeServer, "Vibe Rooms" +
            "--SEP--Create, invite, or join other Vibe rooms, control other pairs devices in realtime!");

        AddDrawButton(FontAwesomeIcon.FileAudio, SelectedTab.PatternManager, "Patterns" +
            "--SEP--Manage, create, and playback various patterns!");

        AddDrawButton(FontAwesomeIcon.Clock, SelectedTab.AlarmManager, "Alarms" +
            "--SEP--Manage, create, and toggle your various Alarms!");

        AddDrawButton(FontAwesomeIcon.LocationCrosshairs, SelectedTab.TriggerManager, "Triggers" +
            "--SEP--Manage, create, and toggle your various Triggers!");
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
