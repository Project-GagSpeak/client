using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI;
using GagSpeak.UI.UiWardrobe;
using GagSpeak.UpdateMonitoring;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;
using Penumbra.Api.Enums;
using System.Numerics;

namespace GagSpeak.Interop.IpcHelpers.Penumbra;
public class ModAssociations : DisposableMediatorSubscriberBase
{
    private readonly WardrobeHandler _handler;
    private readonly IpcCallerPenumbra _penumbra;
    private readonly CustomModCombo _modCombo;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly TutorialService _guides;

    public ModAssociations(ILogger<ModAssociations> logger, GagspeakMediator mediator, 
        WardrobeHandler handler, IpcCallerPenumbra penumbra, OnFrameworkService frameworkUtils,
        TutorialService guides) : base(logger, mediator)
    {
        _handler = handler;
        _penumbra = penumbra;
        _frameworkUtils = frameworkUtils;
        _guides = guides;

        _modCombo = new CustomModCombo(penumbra, logger);
    }

    public (Mod Mod, ModSettings Settings) CurrentSelection => _modCombo.CurrentSelection;

    public void DrawUnstoredSetTable(RestraintSet unstoredSet, float paddingHeight)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X * 0.3f, paddingHeight));
        using var table = ImRaii.Table("UnstoredSetMods", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY);
        if (!table) { return; }

        ImGui.TableSetupColumn("##Delete", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("Mods to enable with this Set", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Toggle", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Toggle").X);
        ImGui.TableSetupColumn("##Redraw", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("##Update", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableHeadersRow();

        Mod? removedMod = null;
        ModUpdateResult? updatedMod = null;

        foreach (var (associatedMod, idx) in unstoredSet.AssociatedMods.WithIndex())
        {
            using var id = ImRaii.PushId(idx);

            DrawAssociatedModRow(associatedMod, idx, out var removedModTmp, out var updatedModTmp);

            if (removedModTmp.HasValue)
            {
                removedMod = removedModTmp;
            }

            if (updatedModTmp != null && updatedModTmp.IsChanged)
            {
                updatedMod = updatedModTmp;
            }
        }
        DrawUnstoredSetNewModRow(ref unstoredSet);

        if (removedMod.HasValue)
        {
            var ModToRemove = unstoredSet.AssociatedMods.FirstOrDefault(x => x.Mod == removedMod.Value);
            if (ModToRemove == null) return;

            unstoredSet.AssociatedMods.Remove(ModToRemove);
        }

        if (updatedMod != null && updatedMod.IsChanged)
        {
            // make sure the associated mods list is not already in the list, and if not, add & save.
            int associatedModIdx = unstoredSet.AssociatedMods.FindIndex(x => x == updatedMod.UpdatedMod);
            if (associatedModIdx == -1) return;

            unstoredSet.AssociatedMods[associatedModIdx] = updatedMod.UpdatedMod;
        }
    }

    private void DrawAssociatedModRow(AssociatedMod currentMod, int idx, out Mod? removedMod, out ModUpdateResult? updatedMod)
    {
        removedMod = null;
        // set updated mod to be set as not yet changed.
        updatedMod = new(currentMod, false);

        // get the index of this mod
        ImGui.TableNextColumn();
        // delete icon
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
        "Delete this mod from associations (Hold Shift)", !ImGui.GetIO().KeyShift, true))
        {
            removedMod = currentMod.Mod;
        }

        // the name of the appended mod
        ImGui.TableNextColumn();
        ImGui.Selectable($"{currentMod.Mod.Name}##name");
        if (ImGui.IsItemHovered()) { ImGui.SetTooltip($"Mod to be enabled when restraint set it turned on.\n{currentMod.Mod.Name}"); }
        // if we should enable or disable this mod list (all buttons should sync)

        ImGui.TableNextColumn();
        // store updated mod's mod and mod setting info from current

        // set icon and help text
        var iconText = currentMod.DisableWhenInactive ? FontAwesomeIcon.Check : FontAwesomeIcon.Times;
        var helpText = currentMod.DisableWhenInactive ? "Mods are disabled when set is disabled" : "Mods will stay enabled after set is turned off";
        if (ImGuiUtil.DrawDisabledButton(iconText.ToIconString(), new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()),
        helpText, false, true))
        {
            updatedMod.UpdatedMod.DisableWhenInactive = !currentMod.DisableWhenInactive;
            updatedMod.IsChanged = true;
        }

        ImGui.TableNextColumn();
        // redraw button
        var iconText2 = currentMod.RedrawAfterToggle ? FontAwesomeIcon.Redo : FontAwesomeIcon.None;
        var helpText2 = currentMod.RedrawAfterToggle ? "Redraws self after set toggle (nessisary for VFX/Animation Mods)" : "Do not redraw when set is toggled (uses fast redraw)";
        if (ImGuiUtil.DrawDisabledButton(iconText2.ToIconString(), new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()),
        helpText2, false, true))
        {
            updatedMod.UpdatedMod.RedrawAfterToggle = !currentMod.RedrawAfterToggle;
            updatedMod.IsChanged = true;
        }

        // button to update the status the mod from penumbra
        ImGui.TableNextColumn();
        ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Search.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
        "Inspect current mod status", false, true);
        if (ImGui.IsItemHovered())
        {
            var (_, newSettings) = _penumbra.GetMods().FirstOrDefault(m => m.Mod == currentMod.Mod);
            if (ImGui.IsItemClicked())
            {
                updatedMod.UpdatedMod.ModSettings = newSettings;
                updatedMod.IsChanged = true;
            }

            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
            using var tt = ImRaii.Tooltip();
            ImGui.Separator();
            var namesDifferent = currentMod.Mod.Name != currentMod.Mod.DirectoryName;
            ImGui.Dummy(new Vector2(300 * ImGuiHelpers.GlobalScale, 0));
            using (ImRaii.Group())
            {
                if (namesDifferent)
                    ImGui.TextUnformatted("Directory Name");

                ImGui.TextUnformatted("Enabled");
                ImGui.TextUnformatted("Priority");
                CustomModCombo.DrawSettingsLeft(newSettings);
            }

            ImGui.SameLine(Math.Max(ImGui.GetItemRectSize().X + 3 * ImGui.GetStyle().ItemSpacing.X, 150 * ImGuiHelpers.GlobalScale));
            using (ImRaii.Group())
            {
                if (namesDifferent)
                    ImGui.TextUnformatted(currentMod.Mod.DirectoryName);

                ImGui.TextUnformatted(newSettings.Enabled.ToString());
                ImGui.TextUnformatted(newSettings.Priority.ToString());
                CustomModCombo.DrawSettingsRight(newSettings);
            }
        }
    }

    private void DrawNewModRow(ref RestraintSet refSet)
    {
        var currentName = _modCombo.CurrentSelection.Mod.Name;
        ImGui.TableNextColumn();
        var tt = currentName.IsNullOrEmpty()
            ? "Please select a mod first."
            : refSet.AssociatedMods.Any(x => x.Mod == _modCombo.CurrentSelection.Mod)
                ? "The design already contains an association with the selected mod."
                : string.Empty;

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(),
            new Vector2(ImGui.GetFrameHeight()), tt, tt.Length > 0, true))
        {
            // locate the associated mod we want to add based on what is in the mod combo's current selection
            var associatedMod = new AssociatedMod
            {
                Mod = _modCombo.CurrentSelection.Mod,
                ModSettings = _modCombo.CurrentSelection.Settings,
                DisableWhenInactive = false,
                RedrawAfterToggle = false
            };
            refSet.AssociatedMods.Add(associatedMod);
        }

        ImGui.TableNextColumn();
        _modCombo.Draw("##new", currentName.IsNullOrEmpty() ? "Select new Mod..." : currentName, string.Empty,
            ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight());
    }

    private void DrawUnstoredSetNewModRow(ref RestraintSet refSet)
    {
        var currentName = _modCombo.CurrentSelection.Mod.Name;
        ImGui.TableNextColumn();
        var tt = currentName.IsNullOrEmpty()
            ? "Please select a mod first."
            : refSet.AssociatedMods.Any(x => x.Mod == _modCombo.CurrentSelection.Mod)
                ? "The design already contains an association with the selected mod."
                : string.Empty;

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(),
            new Vector2(ImGui.GetFrameHeight()), tt, tt.Length > 0, true))
        {
            // locate the associated mod we want to add based on what is in the mod combo's current selection
            var associatedMod = new AssociatedMod
            {
                Mod = _modCombo.CurrentSelection.Mod,
                ModSettings = _modCombo.CurrentSelection.Settings,
                DisableWhenInactive = false,
                RedrawAfterToggle = false
            };
            refSet.AssociatedMods.Add(associatedMod);
        }
        // tutorial things.
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.AddingMod, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

        ImGui.TableNextColumn();
        _modCombo.Draw("##new", currentName.IsNullOrEmpty() ? "Select new Mod..." : currentName, string.Empty,
            ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight());
        // tutorial things.
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.SelectingMod, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

        // go to the final column.
        ImGui.TableNextColumn();
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()));
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ModOptions, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
    }

    public void DrawCursedItemSelection(CursedItem cursedItem, float width)
    {
        // Get the current mod selection from the mod combo
        var currentName = _modCombo.CurrentSelection.Mod.Name;

        _modCombo.Draw("##modSelect"+cursedItem.LootId, currentName.IsNullOrEmpty() ? "Select Mod..." : currentName, string.Empty, width, ImGui.GetTextLineHeight());

        if (ImGui.IsItemHovered())
            UiSharedService.AttachToolTip("Select a Mod to bind to this Cursed Item");
    }
}
