using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Gui.Settings;

// Displays all joined sanctions for you to select and interact with its various tabs.
public class SettingsDataDebug
{
    private enum DebugTabs
    {
        Logging,
        Filters,
        Debuggers,
    }

    private readonly ILogger<SettingsDataDebug> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;

    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<DebugTabs> _tabs;

    public SettingsDataDebug(ILogger<SettingsDataDebug> logger, GagspeakMediator mediator, MainConfig config)
    {
        _logger = logger;
        _mediator = mediator;
        _config = config;

        _tabs = new StylizedTabbarBuilder<DebugTabs>()
            .AddTab(DebugTabs.Logging, "Logging")
            .AddTab(DebugTabs.Filters, "Filters")
            .AddTab(DebugTabs.Debuggers, "Debuggers")
            .Build();
    }
    public int NavbarIdx => (int)_tabs.TabSelection;
    public void SetNavbarIdx(int idx)
    {
        if (idx < 0 || idx >= Enum.GetValues<DebugTabs>().Length)
            return;
        _tabs.TabSelection = (DebugTabs)idx;
    }

    // All Content
    public void Draw(ImGuiWindowPtr winPtr)
    {
        // Draw out the tabs using the custom backgrounds and whatever else.
        _tabs.DrawTabs(TabBarFlags.MinimalGlow);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2f * ImGuiHelpers.GlobalScale);
        ImGui.Separator();
        ImGui.Spacing();
        using var _ = ImRaii.Child("data-debug-inner");

        // Draw out the content based on the selected tab.
        switch (_tabs.TabSelection)
        {
            case DebugTabs.Logging:
                DrawLoggingOptions();
                break;
            case DebugTabs.Filters:
                DrawFilters();
                break;
            case DebugTabs.Debuggers:
                DrawDebuggerPopouts();
                break;
        }
    }

    private void DrawLoggingOptions()
    {
        CkGui.FontText("Debug", Fonts.DefaultScaled);

        if (CkGuiUtils.EnumCombo("Log Level", 250f * ImGuiHelpers.GlobalScale, MainConfig.LogLevel, out var newValue, flags: CFlags.None))
        {
            MainConfig.LogLevel = newValue;
            _config.Save();
        }
    }

    // Maybe swap out for framed groups but also maybe not since it
    // would take up more vertical real estate
    private void DrawFilters()
    {
        var isFirstSection = true;
        using var s = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(2));

        foreach (var (label, filterGroup) in FilterGroups)
        {
            using (ImRaii.Group())
            {
                CkGui.TextUnderlined(label);
                if (isFirstSection)
                {
                    var endX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
                    ImGui.SameLine(endX - CkGui.GetButtonSize("Recommended") - CkGui.GetButtonSize("All Off") - CkGui.ItemSpacing.X * 2);
                    ImGui.AlignTextToFramePadding();
                    if (CkGui.SmallButtonEx("Recommended", ImGuiColors.TankBlue))
                    {
                        MainConfig.LogFilters = [.. GsLogFilters.Recommended];
                        GsLogFilters.UpdateFilters(MainConfig.LogFilters);
                        _config.Save();
                    }
                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding(); 
                    if (CkGui.SmallButtonEx("All Off", CkCol.TriStateCross.Vec4()))
                    {
                        MainConfig.LogFilters.Clear();
                        GsLogFilters.UpdateFilters(MainConfig.LogFilters);
                        _config.Save();
                    }
                }

                DrawFilterGroup(label, filterGroup);
            }
            isFirstSection = false;
        }

        void DrawFilterGroup(string label, LogFilter[] filterGroup)
        {
            using var t = ImRaii.Table($"##{label}_table", 4, ImGuiTableFlags.None);
            if (!t) return;

            for (int i = 0; i < filterGroup.Length; i++)
            {
                ImGui.TableNextColumn();

                var flag = filterGroup[i];
                bool flagState = MainConfig.LogFilters.Contains(flag);

                if (ImGui.Checkbox(ToName(flag), ref flagState))
                {
                    if (flagState)
                        MainConfig.LogFilters.Add(flag);
                    else
                        MainConfig.LogFilters.Remove(flag);

                    // Sync the logger's fast array and save
                    GsLogFilters.UpdateFilters(MainConfig.LogFilters);
                    _config.Save();
                }
            }
        }

        string ToName(LogFilter filter) => filter switch
        {
            // Interop / IPC
            LogFilter.IpcGagSpeak => "GagSpeak",
            LogFilter.IpcSundouleia => "Sundouleia",
            LogFilter.IpcPenumbra => "Penumbra",
            LogFilter.IpcGlamourer => "Glamourer",
            LogFilter.IpcCustomize => "Customize+",
            LogFilter.IpcLoci => "Loci",
            LogFilter.IpcHeels => "Heels",
            LogFilter.IpcLifeStream => "LifeStream",
            LogFilter.IpcHonorific => "Honorific",
            LogFilter.IpcIntiface => "Intiface",

            // Achievements
            LogFilter.AchievementEvents => "Events",
            LogFilter.AchievementInfo => "Information",

            // Game Data
            LogFilter.ContextMenus => "Context Menus",
            LogFilter.DtrBar => "DTR Bar",

            // Chat Data
            LogFilter.ChatHooks => "Hooks",
            LogFilter.ChatDetours => "Detours",
            LogFilter.GlobalChat => "GlobalChat",
            LogFilter.DMChatlogs => "DMs",

            // Client Player State
            LogFilter.VisualCache => "Visual Cache",
            LogFilter.CursedItems => "Cursed Items",
            LogFilter.VibeLobbies => "Vibe Lobbies",

            // Hardcore 
            LogFilter.HardcoreActions => "Actions",
            LogFilter.HardcoreMovement => "Movement",
            LogFilter.HardcorePrompt => "Prompt",
            LogFilter.HardcoreTasks => "Tasks",

            // Actor Management
            LogFilter.ActorWatcher => "Watcher",
            LogFilter.ActorVisibility => "Visibility",

            // Kinkster Data
            LogFilter.PairManagement => "Management",
            LogFilter.DataTransfers => "Transfers",
            LogFilter.PairHandlers => "Handlers",
            LogFilter.KinksterCache => "Caches",
            LogFilter.PairExcessive => "Excessive",

            // Update Monitoring
            LogFilter.ActionEffects => "Action Effects",
            LogFilter.EmoteMonitor => "Emotes",
            LogFilter.SpatialAudio => "Spatial Audio",

            // Services
            LogFilter.AutoUnlocks => "Auto-Unlocks",
            LogFilter.EventNotifier => "Event Notifier",
            LogFilter.KinkPlateEditor => "KinkPlate Editor",
            LogFilter.DrawSystems => "Draw Systems",
            LogFilter.ShareHub => "Share Hub",

            // WebAPI
            LogFilter.HubFactory => "Factory",
            LogFilter.MainHub => "Main Hub",
            LogFilter.JwtTokens => "JWT Tokens",

            _ => filter.ToString()
        };
    }

    private void DrawDebuggerPopouts()
    {
        if (CkGui.IconTextButton(FAI.Database, "Debug Storages"))
            _mediator.Publish(new UiToggleMessage(typeof(DebugStorageUI)));
        CkGui.AttachTooltip("Debug various storages in a seprate window.");

        if (CkGui.IconTextButton(FAI.PersonRays, "Debug Personal Data"))
            _mediator.Publish(new UiToggleMessage(typeof(DebugPersonalDataUI)));
        CkGui.AttachTooltip("Debug personal data in a seprate window.");

        if (CkGui.IconTextButton(FAI.Tshirt, "Debug Active State"))
            _mediator.Publish(new UiToggleMessage(typeof(DebugActiveStateUI)));
    }

    /// <summary> Displays the Debug section within the settings, where we can set our debug level </summary>
    private static readonly (string Label, LogFilter[] Filters)[] FilterGroups =
    {
        ("Achievements", [
            LogFilter.Achievements, LogFilter.AchievementEvents, LogFilter.AchievementInfo ]),

        ("Interop / IPC", [
            LogFilter.IpcGagSpeak, LogFilter.IpcSundouleia, LogFilter.IpcPenumbra, LogFilter.IpcGlamourer,
            LogFilter.IpcCustomize, LogFilter.IpcLoci, LogFilter.IpcHeels, LogFilter.IpcLifeStream,
            LogFilter.IpcHonorific, LogFilter.IpcIntiface ]),

        ("Game Data", [
            LogFilter.ContextMenus, LogFilter.Nameplates, LogFilter.DtrBar ]),

        ("Chat Data", [
            LogFilter.ChatHooks, LogFilter.ChatDetours, LogFilter.GlobalChat, LogFilter.DMChatlogs ]),

        ("Client Data", [
            LogFilter.Requests, LogFilter.GarblerCore ]),

        ("Client Player State", [
            LogFilter.Listeners, LogFilter.VisualCache, LogFilter.Gags, LogFilter.Restrictions,
            LogFilter.Restraints, LogFilter.Collars, LogFilter.CursedItems, LogFilter.Puppeteer,
            LogFilter.Toys, LogFilter.VibeLobbies, LogFilter.Patterns, LogFilter.Alarms, LogFilter.Triggers ]),

        ("Hardcore", [
            LogFilter.HardcoreActions, LogFilter.HardcoreMovement, LogFilter.HardcorePrompt, LogFilter.HardcoreTasks ]),

        ("Actor Management", [
            LogFilter.ActorWatcher, LogFilter.ActorVisibility, LogFilter.OnlineUsers ]),

        ("Kinkster Data", [
            LogFilter.PairManagement, LogFilter.DataTransfers, LogFilter.PairHandlers,
            LogFilter.KinksterCache, LogFilter.PairExcessive ]),

        ("Update Monitoring", [
            LogFilter.ActionEffects, LogFilter.EmoteMonitor, LogFilter.Arousal, LogFilter.SpatialAudio ]),

        ("Services", [
            LogFilter.AutoUnlocks, LogFilter.Mediator, LogFilter.UITasks, LogFilter.EventNotifier,
            LogFilter.Textures, LogFilter.KinkPlates, LogFilter.KinkPlateEditor,
            LogFilter.DrawSystems, LogFilter.ShareHub ]),

        ("WebAPI / Hub", [
            LogFilter.HubFactory, LogFilter.MainHub, LogFilter.JwtTokens, LogFilter.Health,
            LogFilter.Callbacks, LogFilter.PiShock ])
    };
}
