using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.PlayerState.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Dto;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Interop;
using System.Windows.Forms;

namespace GagSpeak.CkCommons.Gui.Modules.Puppeteer;
public partial class PuppetVictimGlobalPanel
{
    private HashSet<Guid> ExpandedTriggers = new HashSet<Guid>();

    private void DrawAliasTrigger(AliasTrigger aliasItem)
    {
        TriggerHeaderDisplay(aliasItem);

        ImGui.Separator();
        foreach (var triggerAction in aliasItem.Actions)
        {
            switch (triggerAction)
            {
                case TextAction ta:         _aliasDrawer.DrawOutputTextAction(ta);  break;
                case GagAction ga:          _aliasDrawer.DrawGagAction(ga);         break;
                case RestrictionAction rsa: _aliasDrawer.DrawRestrictionAction(rsa);break;
                case RestraintAction rta:   _aliasDrawer.DrawRestraintAction(rta);  break;
                case MoodleAction ma:       _aliasDrawer.DrawMoodleAction(ma);      break;
                case PiShockAction ps:      _aliasDrawer.DrawShockAction(ps);       break;
                case SexToyAction sta:      _aliasDrawer.DrawSexToyAction(sta);     break;

                default: throw new InvalidOperationException($"Bad Type: {triggerAction.ActionType}");
            }
        }
    }

    private void DrawAliasTriggerEditor()
    {
        if (_manager.ItemInEditor is not { } aliasItem)
            return;

        TriggerHeaderEditor(aliasItem);

        ImGui.Separator();
        foreach (var triggerAction in aliasItem.Actions)
        {
            switch (triggerAction)
            {
                case TextAction ta:         _aliasDrawer.DrawOutputTextAction(ta);  break;
                case GagAction ga:          _aliasDrawer.DrawGagAction(ga);         break;
                case RestrictionAction rsa: _aliasDrawer.DrawRestrictionAction(rsa);break;
                case RestraintAction rta:   _aliasDrawer.DrawRestraintAction(rta);  break;
                case MoodleAction ma:       _aliasDrawer.DrawMoodleAction(ma);      break;
                case PiShockAction ps:      _aliasDrawer.DrawShockAction(ps);       break;
                case SexToyAction sta:      _aliasDrawer.DrawSexToyAction(sta);     break;

                default: throw new InvalidOperationException($"Bad Type: {triggerAction.ActionType}");
            }
        }
    }

    private void TriggerHeaderDisplay(AliasTrigger aliasItem)
    {
        using var _ = ImRaii.Group();

        CkGui.BooleanToColoredIcon(aliasItem.Enabled);
        if (ImGui.IsItemClicked())
            _manager.ToggleState(aliasItem);
        CkGui.AttachToolTip("Click to toggle the AliasTriggers state!--SEP--Current State is: " + (aliasItem.Enabled ? "Enabled" : "Disabled"));

        // Draw out the name.
        CkGui.SameLineText(aliasItem.Label.IsNullOrEmpty() ? "<No Alias Name Set!>" : aliasItem.Label);

        // Draw out the quote marks with the phrase.
        CkGui.FramedIconText(FAI.QuoteLeft, ImGuiColors.DalamudGrey2);

        using (ImRaii.PushFont(UiBuilder.MonoFont))
            CkGui.SameLineText(aliasItem.InputCommand);
        CkGui.AttachToolTip("The text to scan for (Input String)");

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.QuoteRight, ImGuiColors.DalamudGrey2);

        // Draw out the dropdown button.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
        var isExpanded = ExpandedTriggers.Contains(aliasItem.Identifier);
        if (CkGui.IconButton(isExpanded ? FAI.ChevronUp : FAI.ChevronDown, inPopup: true))
        {
            if (isExpanded)
                ExpandedTriggers.Remove(aliasItem.Identifier);
            else
                ExpandedTriggers.Add(aliasItem.Identifier);
        }
        CkGui.AttachToolTip(isExpanded ? "Collapse" : "Expand");
    }

    private void TriggerHeaderEditor(AliasTrigger aliasItem)
    {
        using var _ = ImRaii.Group();

        // The Enabled/Disabled Icon
        CkGui.BooleanToColoredIcon(aliasItem.Enabled);
        if (ImGui.IsItemClicked())
            _manager.ToggleState(aliasItem);
        CkGui.AttachToolTip("Click to toggle the AliasTriggers state!--SEP--Current State is: " + (aliasItem.Enabled ? "Enabled" : "Disabled"));

        // Label editor.
        ImGui.SameLine();
        var tempName = aliasItem.Label;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2);
        if (ImGui.InputTextWithHint($"##label_{aliasItem.Identifier}", "Give Alias a Label...", ref tempName, 70))
            aliasItem.Label = tempName;
        CkGui.AttachToolTip("The Alias Label given to help with searching and organization.");

        // Draw out the quote marks with the phrase.
        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.QuoteLeft, ImGuiColors.DalamudGrey2);

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() * 2);
        var inputTmp = aliasItem.InputCommand;
        if (ImGui.InputTextWithHint("##InputCommand_" + aliasItem.Identifier, "Enter Text To Scan For...", ref inputTmp, 256))
            aliasItem.InputCommand = inputTmp;
        CkGui.AttachToolTip("The text to scan for (Input String)");

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.QuoteRight, ImGuiColors.DalamudGrey2);

        // the Save button.
        if (CkGui.IconButton(FAI.Save, inPopup: true))
            _manager.SaveChangesAndStopEditing();
    }
}
