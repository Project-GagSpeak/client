using CkCommons;
using CkCommons.DrawSystem;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using OtterGui.Text;
using TerraFX.Interop.Windows;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace GagSpeak.Gui.Wardrobe;

public class AliasesTab : IFancyTab
{
    private readonly ILogger<AliasesTab> _logger;
    private readonly MainHub _hub;
    private readonly AliasesFileSelector _selector;
    private readonly AliasTriggerDrawer _aliasDrawer;
    private readonly PuppeteerManager _manager;
    private readonly KinksterManager _kinksters;
    private readonly TutorialService _guides;

    private InvokableActionType _selected;
    private PairCombo _pairCombo;

    private Kinkster? _selectedKinkster = null;

    public AliasesTab(ILogger<AliasesTab> logger, GagspeakMediator mediator,
        MainHub hub, FavoritesConfig favorites, AliasesFileSelector selector, 
        AliasTriggerDrawer drawer, PuppeteerManager manager,
        KinksterManager kinksters, TutorialService guides)
    {
        _logger = logger;
        _hub = hub;
        _selector = selector;
        _aliasDrawer = drawer;
        _manager = manager;
        _kinksters = kinksters;
        _guides = guides;

        _pairCombo = new(logger, mediator, favorites, () => [
            ..( _manager.ItemInEditor is { } editor 
                ? kinksters.DirectPairs.Where(k => !editor.WhitelistedUIDs.Contains(k.UserData.UID)) : kinksters.DirectPairs)
                .OrderByDescending(p => FavoritesConfig.Kinksters.Contains(p.UserData.UID))
                .ThenByDescending(u => u.IsRendered)
                .ThenByDescending(u => u.IsOnline)
                .ThenBy(pair => pair.GetDisplayName())
        ]);
    }

    public string   Label       => "Aliases";
    public string   Tooltip     => "Create aliases to enhance puppeteer!";
    public bool     Disabled    => false;

    // should be very similar to drawing out the list of items, except this will have a unique flavor to it.
    public void DrawContents(float width)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, ImUtf8.FramePadding - new Vector2(0, 1));

        var leftW = width * 0.45f;
        var rounding = FancyTabBar.BarHeight * .4f;

        using (var _ = CkRaii.FramedChildPaddedWH("list", new(leftW, ImGui.GetContentRegionAvail().Y), 0, GsCol.VibrantPink.Uint(), rounding))
        {
            _selector.DrawFilterRow(_.InnerRegion.X);
            _selector.DrawList(_.InnerRegion.X);
        }
        ImUtf8.SameLineInner();
        using (var _ = CkRaii.FramedChildPaddedWH("alias", ImGui.GetContentRegionAvail(), 0, GsCol.VibrantPink.Uint(), rounding))
        {
            if (_manager.ItemInEditor is { } item)
                DrawAliasEditor(_.InnerRegion, item, rounding);
            else
                DrawAlias(_.InnerRegion, rounding);
        }
    }

    private void DrawAlias(Vector2 region, float rounding)
    {
        if (_selector.Selected is not { } alias)
            return;

        // Top right should have a selectable checkmark to flick its state
        CkGui.BooleanToColoredIcon(alias.Enabled, false);
        CkGui.AttachToolTip("If this alias is enabled.--SEP----COL--Click to toggle!--COL--", ImGuiColors.ParsedPink);
        if (ImGui.IsItemClicked())
        {
            UiService.SetUITask(async () =>
            {
                _manager.ToggleState(alias);
                // Toggle the state, and then afterward, update the people from it
                var toSend = _kinksters.GetOnlineUserDatas().Where(u => alias.WhitelistedUIDs.Contains(u.UID)).ToList();
                _logger.LogDebug($"Pushing AliasStateChange to {string.Join(", ", toSend.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);

                var dto = new PushClientAliasState(toSend, alias.Identifier, alias.Enabled);
                if (await _hub.UserPushAliasState(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
                    _logger.LogWarning($"Failed to push AliasStateChange update to server. Reason: [{res}]");
            });
        }

        CkGui.TextFrameAlignedInline(alias.Label);
        if (string.IsNullOrWhiteSpace(alias.Label))
        {
            ImGui.SameLine();
            CkGui.FramedIconText(FAI.ExclamationTriangle, ImGuiColors.DalamudRed);
            CkGui.AttachToolTip("Must have a valid alias to scan!");
        }

        var endX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        ImGui.SameLine(endX -= CkGui.IconButtonSize(FAI.Edit).X);
        if (CkGui.IconButton(FAI.Edit, inPopup: true))
            _manager.StartEditing(alias);
        CkGui.AttachToolTip("Edit this alias.");

        // Draw out what the alias detects, and if it ignores case or not
        CkGui.FramedIconText(FAI.AssistiveListeningSystems);
        if (string.IsNullOrWhiteSpace(alias.InputCommand))
        {
            CkGui.ColorTextFrameAlignedInline("<NO-DETECTION>", ImGuiColors.DalamudRed);
        }
        else
        {
            CkGui.TextFrameAlignedInline("Detects \"");
            ImGui.SameLine(0, 0);
            CkGui.ColorTextFrameAligned(alias.InputCommand, ImGuiColors.TankBlue);
            ImGui.SameLine(0, 0);
            CkGui.TextFrameAligned("\"");
        }
        ImGui.SameLine();
        CkGui.RightAlignedColor(alias.IgnoreCase ? "Case Sensative" : "Ignores Case", ImGuiColors.DalamudGrey2);

        ImGui.Separator();
        var posY = ImGui.GetCursorPosY();
        DrawReactions(alias, region.X);

        ImGui.SetCursorPosY(posY + CkStyle.GetFrameRowsHeight(7));
        DrawWhitelist(alias);

        // Iterate through the hashset whitelist and display all of the Kinksters that you have allowed.
        // If a Kinkster is not located, mark them as an invalid kinkster.

    }

    private void DrawReactions(AliasTrigger alias, float width)
    {
        if (alias.Actions.Count is 0)
        {
            CkGui.ColorTextCentered("No Reactions assigned!", ImGuiColors.DalamudRed);
            return;
        }

        // Determine height and draw
        var rows = alias.Actions.Count;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImUtf8.ItemSpacing.X, 2));
        using var _ = CkRaii.Child("reacitons", new Vector2(width, CkStyle.GetFrameRowsHeight(rows)));

        foreach (var reaction in alias.Actions.ToList())
        {
            switch (reaction)
            {
                case TextAction ta: _aliasDrawer.DrawTextRow(ta); break;
                case GagAction ga: _aliasDrawer.DrawGagRow(ga); break;
                case RestrictionAction rsa: _aliasDrawer.DrawRestrictionRow(rsa); break;
                case RestraintAction rta: _aliasDrawer.DrawRestraintRow(rta); break;
                case MoodleAction ma: _aliasDrawer.DrawMoodleRow(ma); break;
                case PiShockAction ps: _aliasDrawer.DrawShockRow(ps); break;
                case SexToyAction sta: _aliasDrawer.DrawToyRow(sta); break;
            }
        }
    }

    private void DrawWhitelist(AliasTrigger alias)
    {
        CkGui.FontText("Allowed Kinksters:", UiFontService.Default150Percent);
        CkGui.Separator(GsCol.VibrantPink.Uint());

        if (alias.WhitelistedUIDs.Count is 0)
        {
            CkGui.FontTextAligned("Everyone", UiFontService.Default150Percent);
            ImUtf8.SameLineInner();
            CkGui.FontTextAligned("(Global)", UiFontService.Default150Percent, ImGuiColors.DalamudGrey2);
            return;
        }

        // Then draw out the allowed kinksters in the remaining region
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 8f);
        using var c = CkRaii.Child("child", ImGui.GetContentRegionAvail());
        using var t = ImRaii.Table("whitelist", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.RowBg, ImGui.GetContentRegionAvail());
        if (!t) return;

        ImGui.TableSetupColumn("Whitelist");
        ImGui.TableNextColumn();
        var widthInner = ImGui.GetContentRegionAvail().X;

        foreach (var kinksterUid in alias.WhitelistedUIDs.ToList())
        {
            ImGui.TableNextColumn();
            if (_kinksters.TryGetKinkster(new(kinksterUid), out var Kinkster))
            {
                CkGui.IconTextAligned(FAI.UserCircle);
                DrawDisplayName(Kinkster);
                CkGui.AttachToolTip($"UID: --COL--{kinksterUid}--COL--", GsCol.VibrantPink.Vec4Ref());
            }
            else
            {
                CkGui.IconTextAligned(FAI.UserCircle, ImGuiColors.DalamudGrey3);
                using (ImRaii.PushFont(UiBuilder.MonoFont))
                    CkGui.TextFrameAlignedInline(kinksterUid);
            }
            ImGui.TableNextRow();
        }

        void DrawDisplayName(Kinkster s)
        {
            // Set it to the display name.
            var dispName = s.GetDisplayName();
            // Update mono to be disabled if the display name is not the alias/uid.
            var useMono = s.UserData.AliasOrUID.Equals(dispName, StringComparison.Ordinal);
            // Display the name.
            using (ImRaii.PushFont(UiBuilder.MonoFont, useMono))
                CkGui.TextFrameAlignedInline(dispName);
        }
    }

    private void DrawAliasEditor(Vector2 region, AliasTrigger alias, float rounding)
    {
        var txtWidth = ImGui.CalcTextSize("Ignore Case");
        var checkboxW = ImUtf8.FrameHeight + txtWidth.X + ImUtf8.ItemInnerSpacing.X * 2;

        // Checkbox with label, then shift right and draw revert/save
        var enabled = alias.Enabled;
        if (ImGui.Checkbox("##state", ref enabled))
            alias.Enabled = !alias.Enabled;
        CkGui.AttachToolTip("If this can be detected as an alias");

        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - checkboxW);
        var label = alias.Label;
        if (ImGui.InputTextWithHint("##name", "Display Name..", ref label, 64))
            alias.Label = label;
        CkGui.AttachToolTip("The UI display name for the AliasTrigger");

        var endX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        ImGui.SameLine(endX - CkGui.IconButtonSize(FAI.Redo).X - CkGui.IconButtonSize(FAI.Save).X);
        if (CkGui.IconButton(FAI.Undo, inPopup: true))
            _manager.StopEditing();
        CkGui.AttachToolTip("Reverts any changes and exits the editor");
        ImGui.SameLine(0, 0);
        if (CkGui.IconButton(FAI.Save, inPopup: true))
            _manager.SaveChangesAndStopEditing();
        CkGui.AttachToolTip("Saves all changes and exits the editor");

        // Draw the input area
        CkGui.FramedIconText(FAI.AssistiveListeningSystems);
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - checkboxW);
        var listenTxt = alias.InputCommand;
        if (ImGui.InputTextWithHint("##listener-text", "Text to detect..", ref listenTxt, 64))
            alias.InputCommand = listenTxt;
        CkGui.AttachToolTip("The UI display name for the AliasTrigger");

        ImUtf8.SameLineInner();
        var ignoreCase = alias.IgnoreCase;
        if (ImGui.Checkbox("Ignore Case", ref ignoreCase))
            alias.IgnoreCase = !alias.IgnoreCase;
        CkGui.AttachToolTip("If detection text works RegArdleSs oF CaSE");

        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(2))
            .Push(ImGuiStyleVar.ItemSpacing, new Vector2(ImUtf8.ItemSpacing.X, 2));

        DrawReactionsEditor(alias, region.X);

        // Draw out the whitelist area here (do later and stuff)
        DrawWhitelistEditor(alias);
    }

    private void DrawReactionsEditor(AliasTrigger alias, float width)
    {
        var comboW = 100f * ImGuiHelpers.GlobalScale;
        var rightW = CkGui.IconButtonSize(FAI.Plus).X + comboW;
        var activeTypes = alias.Actions.Select(x => x.ActionType);
        var options = Enum.GetValues<InvokableActionType>().Except(activeTypes).ToList();

        ImGui.Separator();

        CkGui.TextFrameAligned("Alias Reactions:");
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - rightW);
        if (CkGuiUtils.EnumCombo("##types", comboW, _selected, out var newVal, options, i => i.ToName(), "All In Use"))
            _selected = newVal;
        CkGui.AttachToolTip("The reaction type to add");

        ImUtf8.SameLineInner();
        if (CkGui.IconButton(FAI.Plus, disabled: options.Count is 0))
        {
            alias.Actions.Add(_selected switch
            {
                InvokableActionType.TextOutput => new TextAction(),
                InvokableActionType.Gag => new GagAction(),
                InvokableActionType.Restriction => new RestrictionAction(),
                InvokableActionType.Restraint => new RestraintAction(),
                InvokableActionType.Moodle => new MoodleAction(),
                InvokableActionType.ShockCollar => new PiShockAction(),
                InvokableActionType.SexToy => new SexToyAction(),
                _ => throw new ArgumentOutOfRangeException(nameof(_selected), _selected, null)
            });
            // sort the order of the actions and reset selection
            alias.Actions = alias.Actions.OrderBy(x => x.ActionType).ToHashSet();
            _selected = options.Except(alias.Actions.Select(x => x.ActionType)).FirstOrDefault();
        }
        CkGui.AttachToolTip("Add the selected reaction type to this alias.");

        ImGui.Separator();
        // Determine height and draw
        var rows = alias.Actions.Count;
        using var _ = CkRaii.Child("reacitons", new Vector2(width, CkStyle.GetFrameRowsHeight(rows)));

        foreach (var reaction in alias.Actions)
        {
            using var id = ImRaii.PushId($"{reaction.ActionType}");
            switch (reaction)
            {
                case TextAction ta: _aliasDrawer.DrawTextRowEditor(ta); break;
                case GagAction ga: _aliasDrawer.DrawGagRowEditor(ga); break;
                case RestrictionAction rsa: _aliasDrawer.DrawRestrictionRowEditor(rsa); break;
                case RestraintAction rta: _aliasDrawer.DrawRestraintRowEditor(rta); break;
                case MoodleAction ma: _aliasDrawer.DrawMoodleRowEditor(ma, MoodleCache.IpcData); break;
                case PiShockAction ps: _aliasDrawer.DrawShockRowEditor(ps); break;
                case SexToyAction sta: _aliasDrawer.DrawToyRowEditor(sta); break;
            }
            
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.Minus).X);
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
                if (CkGui.IconButton(FAI.Minus, inPopup: true))
                {
                    alias.Actions.Remove(reaction);
                    _selected = options.Except(alias.Actions.Select(x => x.ActionType)).FirstOrDefault();
                }
            CkGui.AttachToolTip("Removes this reaction from the AliasTrigger.");
        }
    }

    private void DrawWhitelistEditor(AliasTrigger alias)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 8f);
        CkGui.FontText("Whitelist", UiFontService.Default150Percent);

        if (CkGui.IconTextButton(FAI.Ban, "Clear", disabled: ImGui.GetIO().KeyShift))
        {
            alias.WhitelistedUIDs.Clear();
        }
        CkGui.AttachToolTip(" Add all visible sundesmos nearby.");

        ImUtf8.SameLineInner();
        if (_pairCombo.Draw(_selectedKinkster, ImGui.GetContentRegionAvail().X))
        {
            if (_pairCombo.Current is not { } selected)
                return;

            if (alias.WhitelistedUIDs.Add(selected.UserData.UID))
                _logger.LogInformation($"Adding {selected.GetDisplayName()} to Alias {alias.Label}");
            // Reset selection.
            _selectedKinkster = null;
        }

        using var _ = CkRaii.Child("whitelist-child", ImGui.GetContentRegionAvail());
        using var t = ImRaii.Table("whitelist", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.RowBg, ImGui.GetContentRegionAvail());
        if (!t) return;

        ImGui.TableSetupColumn("Whitelist");
        ImGui.TableNextColumn();
        var widthInner = ImGui.GetContentRegionAvail().X;

        foreach (var kinksterUid in alias.WhitelistedUIDs.ToList())
        {
            ImGui.TableNextColumn();
            if (_kinksters.TryGetKinkster(new(kinksterUid), out var Kinkster))
            {
                CkGui.IconTextAligned(FAI.UserCircle);
                DrawDisplayName(Kinkster);
                CkGui.AttachToolTip($"UID: --COL--{kinksterUid}--COL--", GsCol.VibrantPink.Vec4Ref());
            }
            else
            {
                CkGui.IconTextAligned(FAI.UserCircle, ImGuiColors.DalamudGrey3);
                using (ImRaii.PushFont(UiBuilder.MonoFont))
                    CkGui.TextFrameAlignedInline(kinksterUid);
            }

            ImGui.SameLine(widthInner - CkGui.IconButtonSize(FAI.Minus).X);
            if (CkGui.IconButton(FAI.Minus, id: kinksterUid, inPopup: true))
                alias.WhitelistedUIDs.Remove(kinksterUid);
            CkGui.AttachToolTip($"Remove from Selection");
            ImGui.TableNextRow();
        }

        void DrawDisplayName(Kinkster s)
        {
            // Set it to the display name.
            var dispName = s.GetDisplayName();
            // Update mono to be disabled if the display name is not the alias/uid.
            var useMono = s.UserData.AliasOrUID.Equals(dispName, StringComparison.Ordinal);
            // Display the name.
            using (ImRaii.PushFont(UiBuilder.MonoFont, useMono))
                CkGui.TextFrameAlignedInline(dispName);
        }
    }
}
