using CkCommons.Gui;
using CkCommons.Textures;
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
using GagSpeak.State.Listeners;
using GagSpeak.Utils;
using OtterGui.Text;

namespace GagSpeak.Gui;

public class DebugActiveStateUI : WindowMediatorSubscriberBase
{
    private readonly ClientData _clientData;
    private readonly ModPresetDrawer _modDrawer;
    private readonly HcTaskManager _hcTasks;
    private readonly GlamourCache _glamourCache;
    private readonly CustomizePlusCache _profileCache;
    private readonly ModCache _modCache;
    private readonly LociCache _lociCache;
    private readonly TraitsCache _traitsCache;
    private readonly OverlayCache _overlayCache;
    private readonly ArousalService _arousal;
    private readonly HealthMonitor _hpMonitor;
    private readonly RemoteService _remotes;
    private readonly TextureService _iconTextures;
    private readonly OnTickService _onTick;

    public DebugActiveStateUI(ILogger<DebugActiveStateUI> logger,
        GagspeakMediator mediator,
        ClientData clientData,
        ModPresetDrawer modDrawer,
        HcTaskManager hcTasks,
        GlamourCache glamourCache,
        CustomizePlusCache profileCache,
        ModCache modCache,
        LociCache lociCache,
        TraitsCache traitsCache,
        OverlayCache overlayCache,
        ArousalService arousal,
        HealthMonitor hpMonitor,
        RemoteService remotes,
        TextureService iconTextures,
        OnTickService onTick)
        : base(logger, mediator, "Active State Debugger")
    {
        _clientData = clientData;
        _modDrawer = modDrawer;
        _hcTasks = hcTasks;
        _glamourCache = glamourCache;
        _profileCache = profileCache;
        _modCache = modCache;
        _lociCache = lociCache;
        _traitsCache = traitsCache;
        _overlayCache = overlayCache;
        _arousal = arousal;
        _hpMonitor = hpMonitor;
        _remotes = remotes;
        _iconTextures = iconTextures;
        _onTick = onTick;

        // IsOpen = true;
        this.SetBoundaries(new Vector2(625, 400), ImGui.GetIO().DisplaySize);
    }

    protected override void PreDrawInternal()
    { }

    protected override void PostDrawInternal()
    { }

    protected override void DrawInternal()
    {
        if (ImGui.CollapsingHeader("Loci IPC"))
            DrawLociIpc();

        if (ImGui.CollapsingHeader("Hardcore State"))
            _clientData.DrawHardcoreStatus();

        if (ImGui.CollapsingHeader("HcTaskManager State"))
            _hcTasks.DrawCacheState();

        if (ImGui.CollapsingHeader("Location Service"))
        {
            using (var t = ImRaii.Table("Location Data", 2, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchSame))
            {
                if (!t) return;

                ImGui.TableSetupColumn("Previous");
                ImGui.TableSetupColumn("Current");
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                DebugArea(OnTickService.Previous);

                ImGui.TableNextColumn();
                DebugArea(OnTickService.Current);
                ImGui.TableNextRow();
            }
            if (CkGui.IconTextButton(FAI.Sync, "Force Update"))
                _onTick.TriggerUpdate();
        }

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
        if (ImGui.CollapsingHeader("Loci Cache"))
            _lociCache.DrawCacheTable(_iconTextures);

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

        _hpMonitor.DrawDebug();
    }

    public static unsafe void DebugArea(LocationEntry entry)
    {
        ImGui.Text("DataCenter:");
        CkGui.ColorTextInline($"{entry.DataCenterName} ({entry.DataCenterId})", ImGuiColors.DalamudGrey);
        ImGui.Text("World:");
        CkGui.ColorTextInline($"{entry.WorldName} ({entry.WorldId})", ImGuiColors.DalamudGrey);

        ImGui.Text("Territory Intended Use:");
        CkGui.ColorTextInline($"{entry.IntendedUse} ({(byte)entry.IntendedUse})", ImGuiColors.DalamudGrey);

        ImGui.Text("Territory:");
        CkGui.ColorTextInline($"{entry.TerritoryName} ({entry.TerritoryId})", ImGuiColors.DalamudGrey);

        ImGui.Text("In Housing District:");
        ImUtf8.SameLineInner();
        CkGui.ColorTextBool(entry.IsInHousing.ToString(), entry.IsInHousing);
        if (entry.IsInHousing)
        {
            ImGui.Text("Housing Area:");
            CkGui.ColorTextInline($"{OnTickService.ResidentialNames[entry.HousingArea]}", ImGuiColors.DalamudGrey);
            ImGui.Text("Housing Type:");
            CkGui.ColorTextInline($"{entry.HousingType} ({(byte)entry.HousingType})", ImGuiColors.DalamudGrey);
            ImGui.Text("Ward:");
            CkGui.ColorTextInline($"{entry.Ward + 1}", ImGuiColors.DalamudGrey);
            ImGui.Text("Plot:");
            CkGui.ColorTextInline($"{entry.Plot + 1}", ImGuiColors.DalamudGrey);
            ImGui.Text("Indoors:");
            ImUtf8.SameLineInner();
            CkGui.ColorTextBool(entry.IsIndoors.ToString(), entry.IsIndoors);
            if (entry.IsIndoors)
            {
                ImGui.Text("Apartment Division:");
                CkGui.ColorTextInline($"{entry.ApartmentDivision}", ImGuiColors.TankBlue);
            }
        }
    }

    private unsafe void DrawLociIpc()
    {
        ImGui.Text("Loci IPC Status:");
        CkGui.ColorTextInline(IpcCallerLoci.APIAvailable ? "Available" : "Unavailable", ImGuiColors.ParsedOrange);

        ImUtf8.TextFrameAligned($"Active Loci: {LociCache.Data.DataInfo.Count()}");
        ImGui.SameLine();
        LociDrawer.DrawTuples(LociCache.Data.DataInfoList.ToList(), LociIcon.SizeFramed);

        ImGui.Text($"Total Statuses: {LociCache.Data.StatusList.Count()}");
        ImGui.Text($"Total Presets: {LociCache.Data.PresetList.Count()}");
    }
}
