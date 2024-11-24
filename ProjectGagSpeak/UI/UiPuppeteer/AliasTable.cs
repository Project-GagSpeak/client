using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.UiPuppeteer;

public class AliasTable : DisposableMediatorSubscriberBase
{
    private readonly UiSharedService _uiShared;
    private readonly PuppeteerHandler _handler;

    public AliasTable(ILogger<AliasTable> logger, GagspeakMediator mediator,
        UiSharedService uiSharedService, PuppeteerHandler handler) : base(logger, mediator)
    {
        _uiShared = uiSharedService;
        _handler = handler;
    }

    private int EditableAliasIndex = -1; // Field to track the editable AliasTrigger
    private AliasTrigger NewTrigger = new AliasTrigger(); // stores data of a trigger yet to be added, modifiable in the add new alias row.

    public void DrawAliasListTable(string KeyToDrawFor, float paddingHeight)
    {
        // draw table.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X * 0.3f, paddingHeight));
        using var table = ImRaii.Table("UniqueAliasListCreator", 2, ImGuiTableFlags.RowBg);
        if (!table) { return; }
        // draw the header row
        ImGui.TableSetupColumn("##Edit", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight() + (ImGuiHelpers.GlobalScale) * 10);
        ImGui.TableSetupColumn("Alias Input / Output", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        // create a new alias trigger list temporarily
        try
        { 
            var aliasTriggerListShallowCopy = _handler.ClonedAliasStorageForEdit?.AliasList.ToList();
            if(aliasTriggerListShallowCopy is null)
                return;

            foreach (var (aliasTrigger, idx) in aliasTriggerListShallowCopy.Select((value, index) => (value, index)))
            {
                using var id = ImRaii.PushId(idx);
                DrawAssociatedModRow(aliasTrigger, idx, KeyToDrawFor);
            }
            DrawNewModRow(KeyToDrawFor);
        }
        catch (Exception ex) { Logger.LogError(ex.ToString()); }
    }

    private void DrawAssociatedModRow(AliasTrigger aliasTrigger, int idx, string userID)
    {
        bool canEdit = EditableAliasIndex == idx;
        ImGui.TableNextColumn();
        if (_uiShared.IconButton(FontAwesomeIcon.Trash, disabled: !KeyMonitor.ShiftPressed()))
            _handler.RemoveAlias(aliasTrigger);
        if(ImGui.IsItemDeactivatedAfterEdit()) _handler.MarkAsModified();
        UiSharedService.AttachToolTip("Delete alias from list.--SEP--Hold SHIFT in order to delete.");

        var enabledRef = aliasTrigger.Enabled;
        if (ImGui.Checkbox($"##Enable{idx}{aliasTrigger.InputCommand}", ref enabledRef))
            _handler.ClonedAliasStorageForEdit!.AliasList[idx].Enabled = enabledRef;
        if (ImGui.IsItemDeactivatedAfterEdit()) _handler.MarkAsModified();
        UiSharedService.AttachToolTip("Enable / Disable the Alias.");
        // try to draw the rest, but if it was removed, it cant, so we should skip over and consume the error
        try
        {
            ImGui.TableNextColumn();
            using (ImRaii.Group())
            {
                string aliasInput = aliasTrigger.InputCommand;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputTextWithHint($"##aliasText{idx}", "Input phrase goes here...", ref aliasInput, 64))
                    _handler.ClonedAliasStorageForEdit!.AliasList[idx].InputCommand = aliasInput;
                if (ImGui.IsItemDeactivatedAfterEdit()) _handler.MarkAsModified();
                UiSharedService.AttachToolTip($"The string of words that {userID} would have to say to make you execute the output command");

                // next line draw output
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2*ImGuiHelpers.GlobalScale);
                _uiShared.IconButton(FontAwesomeIcon.LongArrowAltRight, disabled: true);
                UiSharedService.AttachToolTip($"The command that will be executed when the input phrase is said by {userID}");
                ImUtf8.SameLineInner();
                ImUtf8.SameLineInner();
                string aliasOutput = aliasTrigger.OutputCommand;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputTextWithHint($"##command{idx}", "Output command goes here...", ref aliasOutput, 200))
                    _handler.ClonedAliasStorageForEdit!.AliasList[idx].OutputCommand = aliasOutput;
                if (ImGui.IsItemDeactivatedAfterEdit()) _handler.MarkAsModified();
                UiSharedService.AttachToolTip($"Replaces the statement above in the puppeteers message with this.--SEP--DO NOT INCLUDE A '/' HERE.");
            }
        }
        catch(Exception ex) { Logger.LogError(ex.StackTrace); }
    }

    private void DrawNewModRow(string userID)
    {
        ImGui.TableNextColumn();
        if (_uiShared.IconButton(FontAwesomeIcon.Plus))
        {
            _handler.AddAlias(NewTrigger);
            NewTrigger = new AliasTrigger();
        }
        UiSharedService.AttachToolTip("Add the alias configuration to the list.");

        ImGui.TableNextColumn();
        string newAliasText = NewTrigger.InputCommand;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputTextWithHint("##newAliasText", "Alias Input...", ref newAliasText, 50))
            NewTrigger.InputCommand = newAliasText; // Update the new alias entry input
        UiSharedService.AttachToolTip("The string of words that the player would have to say to make you execute the output field.");

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        string newAliasCommand = NewTrigger.OutputCommand;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGuiHelpers.GlobalScale);
        if (ImGui.InputTextWithHint("##newAliasCommand", "Output Command...", ref newAliasCommand, 200))
            NewTrigger.OutputCommand = newAliasCommand; // Update the new alias entry output
        UiSharedService.AttachToolTip("The command that will be executed when the input phrase is said by the player.");
    }
}
