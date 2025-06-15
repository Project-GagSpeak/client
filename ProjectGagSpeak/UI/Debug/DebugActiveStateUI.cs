using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.Interop;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.State.Handlers;
using GagSpeak.State.Managers;
using GagspeakAPI.Attributes;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui;

public class DebugActiveStateUI : WindowMediatorSubscriberBase
{
    private static OptionalBoolCheckbox HelmetCheckbox = new();
    private static OptionalBoolCheckbox VisorCheckbox = new();
    private static OptionalBoolCheckbox WeaponCheckbox = new();

    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly AttributeDrawer _attributeDrawer;
    private readonly IpcCallerMoodles _moodlesIpc;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CursedLootManager _cursedLoot;
    private readonly CacheStateManager _cacheManager;
    private readonly GlamourCache _glamourCache;
    private readonly ModCache _modCache;
    private readonly MoodleCache _moodleCache;
    private readonly TraitsCache _traitsCache;
    private readonly ArousalService _arousal;
    private readonly TextureService _iconTextures;

    public DebugActiveStateUI(ILogger<DebugActiveStateUI> logger, 
        GagspeakMediator mediator,
        EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        AttributeDrawer traitsDrawer,
        IpcCallerMoodles moodlesIpc,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        CursedLootManager cursedLoot,
        CacheStateManager cacheManager,
        GlamourCache glamourCache,
        ModCache modCache,
        MoodleCache moodleCache,
        TraitsCache traitsCache,
        ArousalService arousal,
        TextureService iconTextures)
        : base(logger, mediator, "Active State Debugger")
    {
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _attributeDrawer = traitsDrawer;
        _moodlesIpc = moodlesIpc;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _cursedLoot = cursedLoot;
        _cacheManager = cacheManager;
        _glamourCache = glamourCache;
        _modCache = modCache;
        _moodleCache = moodleCache;
        _traitsCache = traitsCache;
        _arousal = arousal;
        _iconTextures = iconTextures;

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(625, 400),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
    }

    protected override void PreDrawInternal() { }

    protected override void PostDrawInternal() { }

    protected override void DrawInternal()
    {
        if (ImGui.CollapsingHeader("Moodles IPC Status"))
            DrawMoodlesIpc();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Glamour Cache"))
            _glamourCache.DrawCacheTable(_iconTextures);

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
        if (ImGui.CollapsingHeader("Stimulation Cache"))
            _arousal.DrawCacheTable();
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
