using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.PlayerClient;
using ImGuiNET;

namespace GagSpeak.CkCommons.Gui;

public class DebugTab
{
    /// <summary> Displays the Debug section within the settings, where we can set our debug level </summary>
    private static readonly (string Label, LoggerType[] Flags)[] FlagGroups =
    {
        ("Achievements", [ LoggerType.Achievements, LoggerType.AchievementEvents, LoggerType.AchievementInfo ]),
        ("Hardcore", [ LoggerType.HardcoreActions, LoggerType.HardcoreMovement, LoggerType.HardcorePrompt ]),
        ("Interop / IPC", [ 
            LoggerType.IpcGagSpeak, LoggerType.IpcMare, LoggerType.IpcPenumbra, 
            LoggerType.IpcGlamourer, LoggerType.IpcCustomize, LoggerType.IpcMoodles
            ]),
        ("MufflerCore", [ LoggerType.GarblerCore, LoggerType.ChatDetours ]),
        ("PlayerClientState", [ 
            LoggerType.Listeners, LoggerType.VisualCache, LoggerType.Gags, LoggerType.Restrictions,
            LoggerType.Restraints, LoggerType.CursedItems, LoggerType.Puppeteer, LoggerType.Toys,
            LoggerType.VibeLobbies, LoggerType.Patterns, LoggerType.Alarms, LoggerType.Triggers
            ]),
        ("Kinkster Data", [
            LoggerType.PairManagement, LoggerType.PairInfo, LoggerType.PairDataTransfer, LoggerType.PairHandlers,
            LoggerType.OnlinePairs, LoggerType.VisiblePairs, LoggerType.GameObjects
            ]),
        ("Services", [
            LoggerType.ActionsNotifier, LoggerType.Textures, LoggerType.ContextDtr, LoggerType.GlobalChat,
            LoggerType.Kinkplates, LoggerType.Mediator, LoggerType.ShareHub
            ]),
        ("UI", [ LoggerType.UI, LoggerType.StickyUI, LoggerType.Combos, LoggerType.FileSystems ]),
        ("Update Monitoring", [ LoggerType.ActionEffects, LoggerType.EmoteMonitor, LoggerType.SpatialAudio, LoggerType.Arousal ]),
        ("GagspeakHub", [ 
            LoggerType.PiShock, LoggerType.ApiCore, LoggerType.Callbacks, LoggerType.HubFactory,
            LoggerType.Health, LoggerType.JwtTokens
            ])
    };

    private readonly MainConfig _mainConfig;
    public DebugTab(MainConfig config)
    {
        _mainConfig = config;
    }

    public void DrawDebugMain()
    {
        CkGui.GagspeakBigText("Debug Configuration");

        // display the combo box for setting the log level we wish to have for our plugin
        if (CkGuiUtils.EnumCombo("Log Level", 400, MainConfig.LogLevel, out var newValue))
        {
            MainConfig.LogLevel = newValue;
            _mainConfig.Save();
        }

        var logFilters = MainConfig.LoggerFilters;

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
        var flags = (ulong)MainConfig.LoggerFilters;
        bool isFirstSection = true;

        var drawList = ImGui.GetWindowDrawList();
        foreach (var (label, flagGroup) in FlagGroups)
        {
            using (ImRaii.Group())
            {
                // Draw separator line on top of the group
                var cursorPos = ImGui.GetCursorScreenPos();
                drawList.AddLine(
                    new Vector2(cursorPos.X, cursorPos.Y),
                    new Vector2(cursorPos.X + ImGui.GetContentRegionAvail().X, cursorPos.Y),
                    ImGui.GetColorU32(ImGuiCol.Border)
                );
                ImGui.Dummy(new Vector2(0, 4));

                // Begin table for 4 columns
                using (var table = ImRaii.Table(label, 4, ImGuiTableFlags.None))
                {
                    for (int i = 0; i < flagGroup.Length; i++)
                    {
                        ImGui.TableNextColumn();

                        var flag = flagGroup[i];
                        bool flagState = (flags & (ulong)flag) != 0;
                        if (ImGui.Checkbox(flag.ToString(), ref flagState))
                        {
                            if (flagState) 
                                flags |= (ulong)flag;
                            else
                                flags &= ~(ulong)flag;

                            // update the loggerFilters.
                            MainConfig.LoggerFilters = (LoggerType)flags;
                            _mainConfig.Save();
                        }
                    }

                    // In the first section, add "All On" / "All Off" buttons in the last column
                    if (isFirstSection)
                    {
                        ImGui.TableNextColumn();
                        if (ImGui.Button("All On"))
                        {
                            MainConfig.LoggerFilters = LoggerType.Recommended;
                            _mainConfig.Save();
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("All Off"))
                        {
                            MainConfig.LoggerFilters = LoggerType.None;
                            _mainConfig.Save();
                        }
                    }
                }
                CkGui.AttachToolTip(label, color: new Vector4(1f, 0.85f, 0f, 1f));
            }

            isFirstSection = false;
        }
    }
}
