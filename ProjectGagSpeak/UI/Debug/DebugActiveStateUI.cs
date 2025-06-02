using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.Interop.Ipc;
using GagSpeak.Localization;
using GagSpeak.PlayerState.Listener;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.CkCommons.Gui;

public class DebugActiveStateUI : WindowMediatorSubscriberBase
{
    private static OptionalBoolCheckbox HelmetCheckbox = new();
    private static OptionalBoolCheckbox VisorCheckbox = new();
    private static OptionalBoolCheckbox WeaponCheckbox = new();

    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly TraitsDrawer _traitsDrawer;
    private readonly IpcCallerMoodles _moodlesIpc;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CursedLootManager _cursedLoot;
    private readonly VisualStateListener _listenerCache;

    public DebugActiveStateUI(ILogger<DebugActiveStateUI> logger, 
        GagspeakMediator mediator,
        EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        TraitsDrawer traitsDrawer,
        IpcCallerMoodles moodlesIpc,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        CursedLootManager cursedLoot,
        VisualStateListener listenerCache)
        : base(logger, mediator, "Active State Debugger")
    {
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _traitsDrawer = traitsDrawer;
        _moodlesIpc = moodlesIpc;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _cursedLoot = cursedLoot;
        _listenerCache = listenerCache;


        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(625, 400),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
    }

    private VisualAdvancedRestrictionsCache GagsCache => _gags.VisualCache;
    private VisualRestrictionsCache RestrictionsCache => _restrictions.VisualCache;
    private VisualAdvancedRestrictionsCache RestraintsCache => _restraints.VisualCache;
    private VisualRestrictionsCache CursedLootCache => _cursedLoot.VisualCache;
    private VisualStateListener ListenerCache => _listenerCache;

    private IEnumerable<MoodlesStatusInfo> ActiveStatuses => VisualApplierMoodles.LatestIpcData.MoodlesDataStatuses;
    private IEnumerable<MoodlesStatusInfo> Statuses => VisualApplierMoodles.LatestIpcData.MoodlesStatuses;
    private IEnumerable<MoodlePresetInfo> Presets => VisualApplierMoodles.LatestIpcData.MoodlesPresets;

    protected override void PreDrawInternal() { }

    protected override void PostDrawInternal() { }

    protected override void DrawInternal()
    {
        if (ImGui.CollapsingHeader("Moodles IPC Status"))
            DrawMoodlesIpc();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Gags Visual Cache"))
            DrawGagVisualState();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Restrictions Visual Cache"))
            DrawRestrictionVisualState();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Restraints Visual Cache"))
            DrawRestraintsVisualState();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Cursed Loot Visual Cache"))
            DrawCursedLootVisualState();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Listener Cache State"))
            DrawListenerCacheState();
    }

    // Draws the current state of the moodles IPC data.
    private void DrawMoodlesIpc()
    {
        using var _ = ImRaii.Group();
        ImGui.Text("Moodles IPC Status:");
        CkGui.ColorTextInline(IpcCallerMoodles.APIAvailable ? "Available" : "Unavailable", ImGuiColors.ParsedOrange);

        ImUtf8.TextFrameAligned($"Active Moodles: {ActiveStatuses.Count()}");
        ImGui.SameLine();
        _moodleDrawer.DrawMoodleStatuses(ActiveStatuses, MoodleDrawer.IconSizeFramed);

        ImGui.Text($"Total Moodles: {Statuses.Count()}");
        ImGui.Text($"Total Presets: {Presets.Count()}");
    }

    // Draws what the current active gag items should be applying collectively.
    private void DrawGagVisualState()
        => DrawCache("Gags", GagsCache);

    // Draws what the current active restrictions should be applying collectively.
    private void DrawRestrictionVisualState()
        => DrawCache("Restrictions", RestrictionsCache);

    // Draws what the current active restraints should be applying collectively.
    private void DrawRestraintsVisualState()
        => DrawCache("Restraints", RestraintsCache);

    // Draws what the current active cursed loot should be applying collectively.
    private void DrawCursedLootVisualState()
        => DrawCache("Cursed Item Cache", CursedLootCache);

    // Draws the current state of the listener cache, which tracks changes in visual states.
    private void DrawListenerCacheState()
    {
        ImGui.Text("Listener Cache State (Should be the collective of all these, no current way to know)");
        // Here you would draw the listener cache state, e.g., using ImGui.Text or other ImGui functions
        // Example: ImGui.Text($"Current Listeners: {listenerCache.CurrentListenersCount}");
    }

    private void DrawCache(string label, IVisualCache cache)
    {
        using var _ = ImRaii.Group();

        var frameH = ImGui.GetFrameHeight();
        var iconSize = new Vector2(frameH * 2);
        var spacing = ImGui.GetStyle().ItemSpacing;

        // Glamour Items.
        var size = new Vector2(iconSize.X * 2 + spacing.X, iconSize.Y * 5 + spacing.Y * 4);
        using (CkRaii.HeaderChild($"{label} Glam", size.WithWinPadding()))
        {
            using (ImRaii.Group())
            {
                foreach (var slot in EquipSlotExtensions.EquipmentSlots)
                {
                    var item = cache.Glamour.GetValueOrDefault(slot)?.GameItem ?? ItemService.NothingItem(slot);
                    _equipDrawer.DrawEquipItem(slot, item, iconSize);
                }
            }

            ImGui.SameLine();
            using (ImRaii.Group())
            {
                foreach (var slot in EquipSlotExtensions.AccessorySlots)
                {
                    var item = cache.Glamour.GetValueOrDefault(slot)?.GameItem ?? ItemService.NothingItem(slot);
                    _equipDrawer.DrawEquipItem(slot, item, iconSize);
                }
            }
        }
        var glamourChildSize = ImGui.GetItemRectSize();


        // Mods.
        ImGui.SameLine();
        using (ImRaii.Group())
        {
            var widthLeft = ImGui.GetContentRegionAvail().X;
            // Mods, then Moodles.
            var modsHeight = glamourChildSize.Y / 2;
            using (CkRaii.HeaderChild($"{label} Mods ({cache.Mods.Count()} stored)", new Vector2(widthLeft, modsHeight), HeaderFlags.CR_HeaderCentered))
            {
                using (var table = ImRaii.Table($"##mod-Info", 3, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg))
                {
                    if (!table)
                        return;

                    ImGui.TableSetupColumn("Mod Name");
                    ImGui.TableSetupColumn("Container Mod");
                    ImGui.TableSetupColumn("##Settings", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
                    ImGui.TableHeadersRow();

                    foreach (var mod in cache.Mods)
                    {
                        var labelText = mod.Label.IsNullOrEmpty() ? "<No Label Set>" : mod.Label;
                        var modText = mod.Container.ModName.IsNullOrEmpty() ? "<No Mod Source>" : mod.Container.ModName;

                        ImGuiUtil.DrawTableColumn(labelText);

                        ImGui.TableNextColumn();
                        CkGui.ColorText(modText, ImGuiColors.DalamudGrey);

                        ImGui.TableNextColumn();
                        CkGui.IconText(FAI.QuestionCircle, ImGuiColors.TankBlue);
                        if (ImGui.IsItemHovered())
                            _modDrawer.DrawPresetTooltip(mod);
                    }
                }
            }

            // Moodles.
            var moodlesHeight = MoodleDrawer.IconSize.Y.AddWinPadY();
            using (var c = CkRaii.ChildPaddedW($"{label} Moodles ({cache.Moodles.Count()} stored)", widthLeft, moodlesHeight, CkColor.ElementBG.Uint()))
            {
                _moodleDrawer.FramedMoodleIconDisplay(label, cache.Moodles, c.InnerRegion.X, CkStyle.ChildRounding());
            }

            var traitsStimHeight = glamourChildSize.Y - moodlesHeight.AddWinPadY() - modsHeight - CkStyle.HeaderHeight();
            using (var c = CkRaii.ChildPaddedW($"{label} Traits", widthLeft, traitsStimHeight, CkColor.ElementBG.Uint()))
            {
                // State Traits & Stimulation.
                _traitsDrawer.OneRowTraitsInner(cache, c.InnerRegion.X, Traits.All, false);

                ImGui.Spacing();

                // Meta
                if (cache is VisualAdvancedRestrictionsCache advCache)
                {
                    using (ImRaii.Group())
                    {
                        HelmetCheckbox.Draw("##MetaHelmet", advCache.Headgear, out var _, true);
                        ImUtf8.SameLineInner();
                        CkGui.FramedIconText(FAI.HatCowboySide);
                    }

                    ImGui.SameLine();
                    using (ImRaii.Group())
                    {
                        HelmetCheckbox.Draw("##MetaVisor", advCache.Visor, out var _, true);
                        ImUtf8.SameLineInner();
                        CkGui.FramedIconText(FAI.Glasses);
                    }

                    ImGui.SameLine();
                    using (ImRaii.Group())
                    {
                        HelmetCheckbox.Draw("##MetaWeapon", advCache.Weapon, out var _, true);
                        ImUtf8.SameLineInner();
                        CkGui.FramedIconText(FAI.Explosion);
                    }
                }
            }
        }

        // Customize Profile
        if (cache is VisualAdvancedRestrictionsCache customizeCache)
        {
            CkGui.FramedIconText(FAI.PersonRays);
            CkGui.TextFrameAlignedInline($"C+ Profile: {customizeCache.CustomizeProfile.Profile}");
            CkGui.ColorTextFrameAlignedInline($"Priority: {customizeCache.CustomizeProfile.Priority}", ImGuiColors.DalamudGrey);
        }
    }
}

