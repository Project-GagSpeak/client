using CkCommons;
using CkCommons.Gui;
using CkCommons.Textures;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services;
using GagSpeak.Services.Controller;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.State.Listeners;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
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
    private readonly ImprisonmentController _imprisonment;
    private readonly HousingNavmesh _navmesh;
    private readonly MovementController _movement;
    private readonly KinksterManager _kinksters;
    private readonly ClientDataListener _clientListener;
    private Vector3? _navPoint;
    private float _leashRange = 5f;
    private bool _leashForceWalk;

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
        OnTickService onTick,
        ImprisonmentController imprisonment,
        HousingNavmesh navmesh,
        MovementController movement,
        KinksterManager kinksters,
        ClientDataListener clientListener)
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
        _imprisonment = imprisonment;
        _navmesh = navmesh;
        _movement = movement;
        _kinksters = kinksters;
        _clientListener = clientListener;

        this.SetBoundaries(new Vector2(625, 400), ImGui.GetIO().DisplaySize);
    }

    protected override void DrawInternal()
    {
        if (ImGui.CollapsingHeader("Loci IPC"))
            DrawLociIpc();

        if (ImGui.CollapsingHeader("Hardcore State"))
            _clientData.DrawHardcoreStatus();

        if (ImGui.CollapsingHeader("HcTaskManager State"))
            _hcTasks.DrawCacheState();

#if DEBUG
        if (ImGui.CollapsingHeader("Pathfinder Test"))
            DrawPathfinderTest();
#endif

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

    private void DrawPathfinderTest()
    {
        ImGui.Text($"Navmesh: {(_navmesh.Ready ? "Ready" : "Not Built")}");

        // Walk to a saved point.
        ImGui.Text(_navPoint is { } point ? $"Nav Point: {point:F2}" : "Nav Point: None");
        if (CkGui.IconTextButton(FAI.MapMarkerAlt, "Set Nav Point Here", disabled: !PlayerData.Available))
            _navPoint = PlayerData.Position;
        ImGui.SameLine();
        if (CkGui.IconTextButton(FAI.Walking, "Walk To Nav Point", disabled: _navPoint is null || !_navmesh.Ready))
            _imprisonment.DebugWalkTo(_navPoint!.Value);

        ImGui.Separator();
        var target = Svc.Targets.Target;
        ImGui.Text($"Target: {target?.Name.TextValue ?? "None"}");

        // Solo: follow or leash to any target, with no hardcore state behind it.
        if (_movement.DebugFollow)
        {
            if (CkGui.IconTextButton(FAI.Stop, "Stop Following"))
                _movement.DebugFollow = false;
        }
        else if (CkGui.IconTextButton(FAI.Walking, "Follow Target", disabled: target is null || !_navmesh.Ready))
            _movement.DebugFollow = true;

        ImGui.SameLine();
        if (_movement.Leashed && Leash.Debug is not null)
        {
            if (CkGui.IconTextButton(FAI.Stop, "Stop Leash"))
                _movement.StopLeash();
        }
        else if (CkGui.IconTextButton(FAI.Link, "Leash To Target", disabled: target is null || _movement.Leashed))
        {
            var leader = target!;
            Leash.Debug = new(_leashRange, _leashForceWalk);
            _movement.StartLeash(Leash.Debug.Value, () => leader.IsValid() ? leader : null);
        }

        ImGui.SetNextItemWidth(150f);
        ImGui.SliderFloat("Leash Range##DebugLeash", ref _leashRange, 2f, 20f, "%.1fy");
        ImGui.SameLine();
        ImGui.Checkbox("Force Walk##DebugLeash", ref _leashForceWalk);

        // With a partner: apply their real Locked Follow or Leash to ourselves locally, with every restriction,
        // without them pressing anything. Nothing goes to the server.
        ImGui.Separator();
        var partner = target is null ? null : _kinksters.DirectPairs.FirstOrDefault(k => k.IsRendered && k.PlayerAddress == target.Address);
        ImGui.Text($"Partner: {partner?.GetNickAliasOrUid() ?? "Target a visible Kinkster"}");
        if (ClientData.Hardcore.IsEnabled(HcAttribute.Follow))
        {
            if (CkGui.IconTextButton(FAI.Stop, "Stop Locked Follow / Leash"))
                SetOwnFollow(_kinksters.GetValueOrDefault(new(ClientData.Hardcore!.LockedFollowing.Split('|')[0])), string.Empty);
        }
        else
        {
            if (CkGui.IconTextButton(FAI.PersonWalkingArrowRight, "Locked Follow Partner", disabled: partner is null))
                SetOwnFollow(partner, partner!.User.UID);
            ImGui.SameLine();
            if (CkGui.IconTextButton(FAI.Link, "Leash To Partner", disabled: partner is null))
                SetOwnFollow(partner, $"{partner!.User.UID}|{new Leash(_leashRange, _leashForceWalk).ToTag()}");
        }
    }

    /// <summary> Debug: changes our own Locked Follow as if <paramref name="partner"/> had, locally only. </summary>
    private void SetOwnFollow(Kinkster? partner, string followString)
    {
        if (partner is null || ClientData.HardcoreClone() is not { } hc)
            return;
        _clientListener.ChangeHardcoreStatus(partner.User, HcAttribute.Follow, hc with { LockedFollowing = followString });
    }
}
