using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Interop;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using OtterGui.Text;

namespace GagSpeak.Gui;

public class DebugActiveStateUI : WindowMediatorSubscriberBase
{
    private readonly ClientData _clientData;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly HcTaskManager _hcTasks;
    private readonly GlamourCache _glamourCache;
    private readonly CustomizePlusCache _profileCache;
    private readonly ModCache _modCache;
    private readonly MoodleCache _moodleCache;
    private readonly TraitsCache _traitsCache;
    private readonly OverlayCache _overlayCache;
    private readonly ArousalService _arousal;
    private readonly RemoteService _remotes;
    private readonly TextureService _iconTextures;

    public DebugActiveStateUI(ILogger<DebugActiveStateUI> logger,
        GagspeakMediator mediator,
        ClientData clientData,
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        HcTaskManager hcTasks,
        GlamourCache glamourCache,
        CustomizePlusCache profileCache,
        ModCache modCache,
        MoodleCache moodleCache,
        TraitsCache traitsCache,
        OverlayCache overlayCache,
        ArousalService arousal,
        RemoteService remotes,
        TextureService iconTextures)
        : base(logger, mediator, "Active State Debugger")
    {
        _clientData = clientData;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _hcTasks = hcTasks;
        _glamourCache = glamourCache;
        _profileCache = profileCache;
        _modCache = modCache;
        _moodleCache = moodleCache;
        _traitsCache = traitsCache;
        _overlayCache = overlayCache;
        _arousal = arousal;
        _remotes = remotes;
        _iconTextures = iconTextures;

        // IsOpen = true;
        this.SetBoundaries(new Vector2(625, 400), ImGui.GetIO().DisplaySize);
    }

    protected override void PreDrawInternal() { }

    protected override void PostDrawInternal() { }

    protected override void DrawInternal()
    {
        if (ImGui.CollapsingHeader("Moodles IPC Status"))
            DrawMoodlesIpc();

        if (ImGui.CollapsingHeader("Hardcore State"))
            _clientData.DrawHardcoreStatus();

        if (ImGui.CollapsingHeader("HcTaskManager State"))
            _hcTasks.DrawCacheState();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Glamour Cache"))
            _glamourCache.DrawCacheTable(_iconTextures);

        if (ImGui.CollapsingHeader("Unbound Cache Queue"))
            _glamourCache.DrawUnboundCacheStates(_iconTextures);

        if (ImGui.CollapsingHeader("CPlus State"))
            _profileCache.DrawCacheTable();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Mods Cache"))
            _modCache.DrawCacheTable(_iconTextures, _modDrawer);

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Moodles Cache"))
            _moodleCache.DrawCacheTable(_iconTextures, _moodleDrawer);

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Traits/Attributes Cache"))
            _traitsCache.DrawCacheTable();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Overlay Cache"))
            _overlayCache.DrawCacheTable();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Stimulation Cache"))
            _arousal.DrawCacheTable();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("RemoteService Cache"))
            _remotes.DrawCacheTable();
    }

    // Draws the current state of the moodles IPC data.
    private void DrawMoodlesIpc()
    {
        using var _ = ImRaii.Group();
        ImGui.Text("Moodles IPC Status:");
        CkGui.ColorTextInline(IpcCallerMoodles.APIAvailable ? "Available" : "Unavailable", ImGuiColors.ParsedOrange);

        ImUtf8.TextFrameAligned($"Active Moodles: {MoodleCache.IpcData.DataInfo.Count()}");
        ImGui.SameLine();
        _moodleDrawer.DrawStatusInfos(MoodleCache.IpcData.DataInfoList, MoodleDrawer.IconSizeFramed);

        ImGui.Text($"Total Moodles: {MoodleCache.IpcData.StatusList.Count()}");
        ImGui.Text($"Total Presets: {MoodleCache.IpcData.PresetList.Count()}");
    }
}
