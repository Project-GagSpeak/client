using CkCommons.Custom;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Interop;
using GagSpeak.Interop.Helpers;
using GagSpeak.Localization;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using OtterGui.Text;
using OtterGuiInternal;

namespace GagSpeak.Gui.Settings;

// Settings but drawn in the newer style.
public class NewSettingsUI : WindowMediatorSubscriberBase
{
    private readonly PluginGuideProvider _guideProvider;
    private readonly TutorialService _guides;

    private readonly SettingsMainPluginUI _mainPluginUI;
    private readonly SettingsMainNativeUI _mainNativeUI;
    private readonly SettingsMainChat _mainChat;
    private readonly SettingsMainNotifications _mainNotifications;

    private readonly SettingsModulesHardcore _modulesHardcore;
    private readonly SettingsModulesWardrobe _modulesWardrobe;
    private readonly SettingsModulesPuppeteer _modulesPuppeteer;
    private readonly SettingsModulesToybox _modulesToybox;

    private readonly SettingsHubService _hubService;
    private readonly SettingsHubProfile _hubProfile;
    private readonly SettingsDataDebug _dataDebug;

    private readonly SettingsSideNav _navbar;
    private OptionalPlugin _expandedInfo = OptionalPlugin.None;

    // Revise this later for a better theme manager.
    public static readonly Vector4 BgCol = new Vector4(0.055f, 0.063f, 0.078f, 0.75f);
    public static readonly Vector4 ActionBar = new Vector4(0.039f, 0.043f, 0.063f, 0.75f);
    public static readonly Vector4 RibbonTop = new Vector4(0.047f, 0.055f, 0.071f, 0.85f);
    public static readonly Vector4 RibbonBot = new Vector4(0.031f, 0.039f, 0.051f, 0.85f);
    public static readonly Vector4 BorderSoft = new Vector4(0.245f, 0.257f, 0.304f, 1.0f);
    public static readonly Vector4 SurfaceCol  = new Vector4(0.055f, 0.063f, 0.078f, 0.75f);

    public NewSettingsUI(ILogger<NewSettingsUI> logger, GagspeakMediator mediator,
        PluginGuideProvider guideProvider,
        TutorialService guides,
        SettingsMainPluginUI mainPluginUI,
        SettingsMainNativeUI mainNativeUI,
        SettingsMainChat mainChat,
        SettingsMainNotifications mainNotifications,
        SettingsModulesHardcore modulesHardcore,
        SettingsModulesWardrobe modulesWardrobe,
        SettingsModulesPuppeteer modulesPuppeteer,
        SettingsModulesToybox modulesToybox,
        SettingsHubService hubService,
        SettingsHubProfile hubProfile,
        SettingsDataDebug dataDebug)
        : base(logger, mediator, "GagSpeak Settings")
    {
        _guides = guides;
        _guideProvider = guideProvider;
        _mainPluginUI = mainPluginUI;
        _mainNativeUI = mainNativeUI;
        _mainChat = mainChat;
        _mainNotifications = mainNotifications;
        _modulesHardcore = modulesHardcore;
        _modulesWardrobe = modulesWardrobe;
        _modulesPuppeteer = modulesPuppeteer;
        _modulesToybox = modulesToybox;
        _hubService = hubService;
        _hubProfile = hubProfile;
        _dataDebug = dataDebug;

        Flags = WFlags.NoScrollbar;
        this.SetBoundaries(new(630, 475), ImGui.GetIO().DisplaySize);

        TitleBarButtons = new TitleBarButtonBuilder()
            .Add(FAI.Book, "Changelog", () => Mediator.Publish(new UiToggleMessage(typeof(ChangelogUI))))
            .Add(FAI.Bell, "Actions Notifier", () => Mediator.Publish(new UiToggleMessage(typeof(InteractionEventsUI))))
            .Build();

        _navbar = new SettingsSideNav(new StylizedNavBarBuilder<SettingsNavOption>()
            .CreateGroup("General")
                .Add(SettingsNavOption.PluginUI, "Plugin UI", FAI.Computer)
                .Add(SettingsNavOption.NativeUI, "Native UI", FAI.Gamepad)
                .Add(SettingsNavOption.Chat, "Chat", FAI.Comments)
                .Add(SettingsNavOption.Notifications, "Notifications", FAI.Bell)
                .EndGroup()
            .CreateGroup("Modules")
                .Add(SettingsNavOption.Hardcore, "Hardcore", FAI.Handcuffs)
                .Add(SettingsNavOption.Wardrobe, "Wardrobe", FAI.ToiletPortable)
                .Add(SettingsNavOption.Puppeteer, "Puppeteer", FAI.PersonHarassing)
                .Add(SettingsNavOption.Toybox, "Toybox", FAI.BoxOpen)
                .EndGroup()
            .CreateGroup("Service")
                .Add(SettingsNavOption.ServiceSettings, "Service Settings", FAI.Server)
                .Add(SettingsNavOption.Profile, "Profile", FAI.UserCircle)
                .Add(SettingsNavOption.Debug, "Debug", FAI.Bug)
                .EndGroup()
            .ToList());

        Mediator.Subscribe<OpenSettingsUI>(this, _ => OnOpenSettingsEvent(_.NavbarIdx, _.SubnavBarIdx));
    }

    private void OnOpenSettingsEvent(int navbarIdx, int subnavBarIdx)
    {
        if (navbarIdx >= Enum.GetValues<SettingsNavOption>().Length)
            return;

        var targetTab = (SettingsNavOption)navbarIdx;
        var targetSub = subnavBarIdx;

        // Check if we are already open to the requested tab
        if (IsOpen && _navbar.TabSelection == targetTab)
        {
            // Fetch the current sub-navbar index for the active tab.
            var currentSub = targetTab switch
            {
                SettingsNavOption.PluginUI => _mainPluginUI.NavbarIdx,
                SettingsNavOption.NativeUI => _mainNativeUI.NavbarIdx,
                SettingsNavOption.Chat => _mainChat.NavbarIdx,
                SettingsNavOption.Notifications => _mainNotifications.NavbarIdx,
                SettingsNavOption.Hardcore => _modulesHardcore.NavbarIdx,
                SettingsNavOption.Wardrobe => _modulesWardrobe.NavbarIdx,
                SettingsNavOption.Puppeteer => _modulesPuppeteer.NavbarIdx,
                SettingsNavOption.Toybox => _modulesToybox.NavbarIdx,
                SettingsNavOption.ServiceSettings => _hubService.NavbarIdx,
                SettingsNavOption.Profile => _hubProfile.NavbarIdx,
                SettingsNavOption.Debug => _dataDebug.NavbarIdx,
                _ => -1
            };
            // Close UI if the same.
            if (targetSub < 0 || currentSub == targetSub)
            {
                IsOpen = false;
                return;
            }
        }

        // Otherwise, open it and set the state.
        IsOpen = true;
        _navbar.TabSelection = targetTab;

        if (targetSub < 0)
            return;

        // Then the subnavbar
        switch (_navbar.TabSelection)
        {
            case SettingsNavOption.PluginUI: _mainPluginUI.SetNavbarIdx(targetSub); break;
            case SettingsNavOption.NativeUI: _mainNativeUI.SetNavbarIdx(targetSub); break;
            case SettingsNavOption.Chat: _mainChat.SetNavbarIdx(targetSub); break;
            case SettingsNavOption.Notifications: _mainNotifications.SetNavbarIdx(targetSub); break;
            case SettingsNavOption.Hardcore: _modulesHardcore.SetNavbarIdx(targetSub); break;
            case SettingsNavOption.Wardrobe: _modulesWardrobe.SetNavbarIdx(targetSub); break;
            case SettingsNavOption.Puppeteer: _modulesPuppeteer.SetNavbarIdx(targetSub); break;
            case SettingsNavOption.Toybox: _modulesToybox.SetNavbarIdx(targetSub); break;
            case SettingsNavOption.ServiceSettings: _hubService.SetNavbarIdx(targetSub); break;
            case SettingsNavOption.Profile: _hubProfile.SetNavbarIdx(targetSub); break;
            case SettingsNavOption.Debug: _dataDebug.SetNavbarIdx(targetSub); break;
        }
    }

    private static float SideNavWidth => 150f * ImGuiHelpers.GlobalScale;

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 4f);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(2);
    }

    protected override void DrawInternal()
    {
        // Draw using Internal methods.
        var winPtr = ImGuiInternal.GetCurrentWindow();
        var style = ImGui.GetStyle();
        var scale = ImGuiHelpers.GlobalScale;
        var frameH = ImUtf8.FrameHeight;
        // Account for border. (If no border, is just InnerRect.Min/Max)
        var min = winPtr.InnerRect.Min + new Vector2(style.WindowBorderSize, 0);
        var max = winPtr.InnerRect.Max - new Vector2(style.WindowBorderSize);
        var size = max - min;

        // Cover the full window without removing the padding in pre-draw, do not intersect, assert priority.
        winPtr.DrawList.PushClipRect(min, max, false);

        // The top ribbon height has a special calculation we need to predetermine.
        var topH = ((ImUtf8.TextHeight * 3) + (scale * 6)) + ((style.ItemSpacing.Y * 1.5f) * 2) + style.WindowPadding.Y * 2;
        var stroke = 1.5f * scale;

        var topRibbonMax = new Vector2(max.X, min.Y + topH);
        var sideNavMin = new Vector2(min.X, min.Y + topH);
        var sideNavMax = new Vector2(min.X + SideNavWidth + style.WindowPadding.X * 2, max.Y);
        var contentMin = new Vector2(sideNavMax.X, sideNavMin.Y);

        // Main BG.
        //winPtr.DrawList.AddRectFilled(min, max, BgCol.ToUint(), style.WindowRounding, ImDrawFlags.RoundCornersBottom);
        // Top Ribbon.
        var ribbonTop = RibbonTop.ToUint();
        var ribbonBot = RibbonBot.ToUint();
        winPtr.DrawList.AddRectFilledMultiColor(min, topRibbonMax, ribbonTop, ribbonTop, ribbonBot, ribbonBot);
        // Side Nav
        winPtr.DrawList.AddRectFilled(sideNavMin, sideNavMax, SurfaceCol.ToUint(), style.WindowRounding, ImDrawFlags.RoundCornersBottomLeft);

        // Render the status pill displays
        DrawHeaderRibbon(winPtr, min, topRibbonMax);
        ImGui.SetCursorScreenPos(sideNavMin + ImGui.GetStyle().WindowPadding);
        // Navbar element.
        using var s = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(5 * scale, 4 * scale))
            .Push(ImGuiStyleVar.ScrollbarSize, 10f);
        using (CkRaii.Child("sidenav", new(SideNavWidth, -1), wFlags: WFlags.NoScrollbar))
            _navbar.Draw(SideNavWidth);

        // Then draw out the contents.
        ImGui.SameLine(0, style.WindowPadding.X * 2);
        using var _ = CkRaii.Child("setting-panel", ImGui.GetContentRegionAvail(), wFlags: WFlags.NoScrollbar);
        switch (_navbar.TabSelection)
        {
            case SettingsNavOption.PluginUI:
                _mainPluginUI.Draw(winPtr);
                break;
            case SettingsNavOption.NativeUI:
                _mainNativeUI.Draw(winPtr);
                break;
            case SettingsNavOption.Chat:
                _mainChat.Draw(winPtr);
                break;
            case SettingsNavOption.Notifications:
                _mainNotifications.Draw(winPtr);
                break;
            case SettingsNavOption.Hardcore:
                _modulesHardcore.Draw(winPtr);
                break;
            case SettingsNavOption.Wardrobe:
                _modulesWardrobe.Draw(winPtr);
                break;
            case SettingsNavOption.Puppeteer:
                _modulesPuppeteer.Draw(winPtr);
                break;
            case SettingsNavOption.Toybox:
                _modulesToybox.Draw(winPtr);
                break;
            case SettingsNavOption.ServiceSettings:
                _hubService.Draw(winPtr);
                break;
            case SettingsNavOption.Profile:
                _hubProfile.Draw(winPtr);
                break;
            case SettingsNavOption.Debug:
                _dataDebug.Draw(winPtr);
                break;
        }

        // Draw lines last so they go over everything
        // - SideNav Line
        winPtr.DrawList.AddLine(new(sideNavMax.X - stroke, sideNavMin.Y), new(sideNavMax.X - stroke, max.Y), BorderSoft.ToUint(), stroke);
        // - Ribbon Line
        winPtr.DrawList.AddLine(new(min.X, topRibbonMax.Y), new Vector2(topRibbonMax.X, topRibbonMax.Y), BorderSoft.ToUint(), stroke);
        // Since we are doing a full render over the window, mimic ImGui ResizeGrip draw behavior.
        winPtr.RenderCustomResizeGrips();
        winPtr.DrawList.PopClipRect();
    }

    private void DrawHeaderRibbon(ImGuiWindowPtr winPtr, Vector2 min, Vector2 max)
    {
        // Unique Frame Padding for this entry.
        using var s = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, ImGuiHelpers.GlobalScale))
            .Push(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing * new Vector2(1f, 1.5f));
        using (ImRaii.Group())
        {
            CkGui.TextFrameAligned("Plugins:");
            CkGui.TextFrameAligned("Optional:");
            CkGui.TextFrameAligned("Resources:");
        }
        ImGui.SameLine();
        using (ImRaii.Group())
        {
            DrawIpcStatusPill("Penumbra", IpcCallerPenumbra.APIAvailable);
            ImGui.SameLine();
            DrawIpcStatusPill("Glamourer", IpcCallerGlamourer.APIAvailable);

            DrawIpcStatusPill("Sundouleia", IpcCallerGlamourer.APIAvailable);
            ImGui.SameLine();
            DrawIpcStatusPill("CPlus", IpcCallerCustomize.APIAvailable);
            ImGui.SameLine();
            // Moodles -OR- Loci
            var hasMoodles = IpcCallerMoodles.APIAvailable;
            var hasLoci = IpcCallerLoci.APIAvailable;
            var hasEither = hasMoodles || hasLoci;

            var moodleCol = hasMoodles ? ImGuiColors.HealerGreen : hasEither ? ImGuiColors.ParsedGrey : ImGuiColors.DalamudRed;
            var moodleTT = hasMoodles ? GSLoc.Settings.PluginValid : hasEither ? "Loci satisfies this dependancy." : GSLoc.Settings.PluginInvalid;
            CkCustom.StatusPillTag("Moodles", moodleCol);
            CkGui.AttachTooltip(moodleTT);

            ImGui.SameLine();
            var lociCol = hasLoci ? ImGuiColors.HealerGreen : hasEither ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudRed;
            var noLociTT = "--COL--Moodles--COL-- is used for your custom status icon display plugin." +
                "--SEP--This will work fine, however you will not be able to see other's Loci statuses.";
            var lociTT = hasLoci ? GSLoc.Settings.PluginValid : hasEither ? noLociTT : GSLoc.Settings.PluginInvalid;
            CkCustom.StatusPillTag("Loci", lociCol);
            CkGui.AttachTooltip(lociTT);

            ImGui.SameLine();
            DrawIpcStatusPill("Lifestream", IpcCallerLifestream.APIAvailable);

            ImGui.SameLine();
            DrawIpcStatusPill("Intiface", IpcCallerIntiface.APIAvailable);

            // Resources.
            if (CkCustom.ButtonPillTag("Discord", 0xFFDA8972.ToVec4()))
                Util.OpenLink("https://discord.gg/kinkporium");
            CkGui.AttachTooltip("Verify your account for full social access on our discord!");

            ImGui.SameLine();
            if (CkCustom.ButtonPillTag("GitHub", 0xFFD5449D.ToVec4()))
                Util.OpenLink("https://github.com/Project-GagSpeak/client");
            CkGui.AttachTooltip("Opens GagSpeak's GitHub page.");

            ImGui.SameLine();
            if (CkCustom.ButtonPillTag("ChangeLog", ImGuiColors.DalamudViolet))
                Mediator.Publish(new UiToggleMessage(typeof(ChangelogUI)));
            CkGui.AttachTooltip("Opens the Changelog UI.");

            ImGui.SameLine();
            if (CkCustom.ButtonPillTag("Plugin Configs", ImGuiColors.DalamudGrey))
            {
                try { Process.Start(new ProcessStartInfo { FileName = GsFiles.ConfigDirectory, UseShellExecute = true }); }
                catch (Bagagwa e) { Svc.Logger.Error($"Failed to open the config directory. {e.Message}"); }
            }
            CkGui.AttachTooltip("Opens plugin config folder.");

            _guideProvider.DrawOptionalPluginDetails(_expandedInfo, ImGui.GetContentRegionAvail().X);
        }

        void DrawIpcStatusPill(string label, bool isAvailable)
        {
            var col = isAvailable ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed;
            var tt = isAvailable ? GSLoc.Settings.PluginValid : GSLoc.Settings.PluginInvalid;
            CkCustom.StatusPillTag(label, col);
            CkGui.AttachTooltip(tt);
        }
    }
}
