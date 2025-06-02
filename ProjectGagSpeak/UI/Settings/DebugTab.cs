using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.Services.Configs;
using ImGuiNET;
using OtterGui.Text;
using Penumbra.GameData.Enums;

namespace GagSpeak.CkCommons.Gui;

public class DebugTab
{
    /// <summary> Displays the Debug section within the settings, where we can set our debug level </summary>
    private static readonly Dictionary<string, LoggerType[]> loggerSections = new Dictionary<string, LoggerType[]>
    {
        { "Foundation",     [ LoggerType.Achievements, LoggerType.AchievementEvents, LoggerType.AchievementInfo ] },
        { "Interop",        [ LoggerType.IpcGagSpeak, LoggerType.IpcCustomize, LoggerType.IpcGlamourer, LoggerType.IpcMare, LoggerType.IpcMoodles, LoggerType.IpcPenumbra ] },
        { "State Managers", [ LoggerType.AppearanceState, LoggerType.ToyboxState, LoggerType.Mediator, LoggerType.GarblerCore ] },
        { "Update Monitors",[ LoggerType.ToyboxAlarms, LoggerType.ActionsNotifier, LoggerType.KinkPlateMonitor, LoggerType.EmoteMonitor, LoggerType.ChatDetours, LoggerType.ActionEffects, LoggerType.SpatialAudioLogger ] },
        { "Hardcore",       [ LoggerType.HardcoreActions, LoggerType.HardcoreMovement, LoggerType.HardcorePrompt ] },
        { "Data & Modules", [ LoggerType.ClientPlayerData, LoggerType.GagHandling, LoggerType.PadlockHandling, LoggerType.Restraints, LoggerType.Puppeteer, LoggerType.CursedLoot, LoggerType.ToyboxDevices, LoggerType.ToyboxPatterns, LoggerType.ToyboxTriggers, LoggerType.VibeControl ] },
        { "Pair Data",      [ LoggerType.PairManagement, LoggerType.PairInfo, LoggerType.PairDataTransfer, LoggerType.PairHandlers, LoggerType.OnlinePairs, LoggerType.VisiblePairs, LoggerType.VibeRooms, LoggerType.GameObjects ] },
        { "Services",       [ LoggerType.Cosmetics, LoggerType.Textures, LoggerType.GlobalChat, LoggerType.ContextDtr, LoggerType.PatternHub, LoggerType.Safeword ] },
        { "UI",             [ LoggerType.UiCore, LoggerType.UserPairDrawer, LoggerType.Permissions, LoggerType.Simulation ] },
        { "WebAPI",         [ LoggerType.PiShock, LoggerType.ApiCore, LoggerType.Callbacks, LoggerType.Health, LoggerType.HubFactory, LoggerType.JwtTokens ] }
    };

    private readonly GagspeakConfigService _mainConfig;
    public DebugTab(GagspeakConfigService config)
    {
        _mainConfig = config;
    }

    public void DrawDebugMain()
    {
        CkGui.GagspeakBigText("Debug Configuration");

        // display the combo box for setting the log level we wish to have for our plugin
        if (CkGuiUtils.EnumCombo("Log Level", 400, GagspeakConfigService.LogLevel, out var newValue))
        {
            GagspeakConfigService.LogLevel = newValue;
            _mainConfig.Save();
        }

        var logFilters = GagspeakConfigService.LoggerFilters;

        // draw a collapsible tree node here to draw the logger settings:
        ImGui.Spacing();
        if (ImGui.TreeNode("Advanced Logger Filters (Only Edit if you know what you're doing!)"))
        {
            AdvancedLogger();
            ImGui.TreePop();
        }
    }

    private void AdvancedLogger()
    {
        var isFirstSection = true;

        // Iterate through each section in loggerSections
        foreach (var section in loggerSections)
        {
            // Begin a new group for the section
            using (ImRaii.Group())
            {
                // Calculate the number of checkboxes in the current section
                var checkboxes = section.Value;

                // Draw a custom line above the table to simulate the upper border
                var drawList = ImGui.GetWindowDrawList();
                var cursorPos = ImGui.GetCursorScreenPos();
                drawList.AddLine(new Vector2(cursorPos.X, cursorPos.Y), new Vector2(cursorPos.X + ImGui.GetContentRegionAvail().X, cursorPos.Y), ImGui.GetColorU32(ImGuiCol.Border));

                // Add some vertical spacing to position the table correctly
                ImGui.Dummy(new Vector2(0, 1));

                // Begin a new table for the checkboxes without any borders
                using (ImRaii.Table(section.Key, 4, ImGuiTableFlags.None))
                {
                    // Iterate through the checkboxes, managing columns and rows
                    for (var i = 0; i < checkboxes.Length; i++)
                    {
                        ImGui.TableNextColumn();

                        var isEnabled = GagspeakConfigService.LoggerFilters.Contains(checkboxes[i]);

                        if (ImGui.Checkbox(checkboxes[i].ToName(), ref isEnabled))
                        {
                            if (isEnabled)
                            {
                                GagspeakConfigService.LoggerFilters.Add(checkboxes[i]);
                                LoggerFilter.AddAllowedCategory(checkboxes[i]);
                            }
                            else
                            {
                                GagspeakConfigService.LoggerFilters.Remove(checkboxes[i]);
                                LoggerFilter.RemoveAllowedCategory(checkboxes[i]);
                            }
                            _mainConfig.Save();
                        }
                    }

                    // Add "All On" and "All Off" buttons for the first section
                    if (isFirstSection)
                    {
                        ImGui.TableNextColumn();
                        if (ImGui.Button("All On"))
                        {
                            GagspeakConfigService.LoggerFilters = LoggerFilter.GetAllRecommendedFilters();
                            _mainConfig.Save();
                            LoggerFilter.AddAllowedCategories(GagspeakConfigService.LoggerFilters);
                        }
                        ImUtf8.SameLineInner();
                        if (ImGui.Button("All Off"))
                        {
                            GagspeakConfigService.LoggerFilters.Clear();
                            GagspeakConfigService.LoggerFilters.Add(LoggerType.None);
                            _mainConfig.Save();
                            LoggerFilter.ClearAllowedCategories();
                        }
                    }
                }

                // Display a tooltip when hovering over any element in the group
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.RectOnly))
                {
                    ImGui.BeginTooltip();
                    CkGui.ColorText(section.Key, ImGuiColors.ParsedGold);
                    ImGui.EndTooltip();
                }
            }

            // Mark that the first section has been processed
            isFirstSection = false;
        }

        // Ensure LoggerType.None is always included in the filtered categories
        if (!GagspeakConfigService.LoggerFilters.Contains(LoggerType.None))
        {
            GagspeakConfigService.LoggerFilters.Add(LoggerType.None);
            LoggerFilter.AddAllowedCategory(LoggerType.None);
        }
    }
}
