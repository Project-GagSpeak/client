using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;

namespace GagSpeak.Gui.Toybox;

public partial class PatternsPanel
{
    private readonly ILogger<PatternsPanel> _logger;
    private readonly PatternFileSelector _selector;
    private readonly PatternManager _manager;
    private readonly DistributorService _dds;
    private readonly RemoteService _remotes;
    private readonly TutorialService _guides;

    public PatternsPanel(
        ILogger<PatternsPanel> logger,
        PatternFileSelector selector,
        PatternManager manager,
        DistributorService dds,
        RemoteService remotes,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _manager = manager;
        _dds = dds;
        _remotes = remotes;
        _guides = guides;
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, ToyboxTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("PatternsTL", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("PatternsBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("PatternsTR", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawPatternInfo(drawRegions.BotRight, curveSize);
    }

    private void DrawPatternInfo(CkHeader.DrawRegion region, float curveSize)
    {
        DrawSelectedDisplay(region);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawSelectedDisplay(CkHeader.DrawRegion region)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        var item = _selector.Selected;
        var editorItem = _manager.ItemInEditor;

        var isEditing = item is not null && item.Identifier.Equals(editorItem?.Identifier);
        var isActive = item is not null && item.Identifier.Equals(_remotes.ClientData.ActivePattern);

        var label = item is null ? "No Item Selected!" : isEditing ? $"{item.Label} - (Editing)" : item.Label;
        var tooltip = item is null ? "No item selected!" : isActive ? "Pattern is Active!"
                : $"Double Click to {(editorItem is null ? "Edit" : "Save Changes to")} this Pattern.--SEP--Right Click to cancel and exit Editor.";

        using (var c = CkRaii.ChildLabelCustomButton("##PatternSel", region.Size, ImGui.GetFrameHeight(), DrawLabel, BeginEdits, tooltip, DFlags.RoundCornersRight, LabelFlags.SizeIncludesHeader))
        {
            if (item is null)
                return;

            if (isEditing && editorItem is not null)
                DrawSelectedInner(editorItem, true);
            else
                DrawSelectedInner(item, false);
        }

        void DrawLabel()
        {
            using var c = CkRaii.Child("##PatternSelLabel", new Vector2(region.SizeX * .6f, ImGui.GetFrameHeight()));
            ImGui.Spacing();
            ImGui.SameLine();
            ImUtf8.TextFrameAligned(label);
            ImGui.TextUnformatted(label);
            ImGui.SameLine(c.InnerRegion.X * .7f - (ImGui.GetFrameHeight() * 1.5f));
            CkGui.FramedIconText(isEditing ? FAI.Save : FAI.Edit);
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is not ImGuiMouseButton.Left || item is null || isActive)
                return;

            if (isEditing)
                _manager.SaveChangesAndStopEditing();
            else
                _manager.StartEditing(_selector.Selected!);
        }
    }

    // This will draw out the respective information for the pattern info.
    // Displayed information can call the preview or editor versions of each field.
    private void DrawSelectedInner(Pattern pattern, bool isEditorItem)
    {
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 1));

        CkGui.Separator();
        DrawDescription(pattern, isEditorItem);

        CkGui.Separator();
        DrawDurationLength(pattern, isEditorItem);

        CkGui.Separator();
        DrawPatternTimeSpans(pattern, isEditorItem);

        if (!isEditorItem)
            DrawPatternToggleButton(pattern);

        DrawFooter(pattern);
    }

    private void DrawPatternToggleButton(Pattern pattern)
    {
        var region = ImGui.GetContentRegionAvail();
        var height = region.Y - CkStyle.ThreeRowHeight();
        var offset = region.X - height;
        ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(offset * .5f, ImGui.GetFrameHeight()));

        ImGui.Dummy(new Vector2(height));
        var hovered = ImGui.IsItemHovered();
        var clicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        var isActive = _remotes.ClientData.ActivePattern.Equals(pattern.Identifier);
        var icon = isActive ? CoreTexture.Stop : CoreTexture.Play;
        var color = hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkCol.CurvedHeaderFade.Uint();
        var size = ImGui.GetItemRectSize();
        ImGui.GetWindowDrawList().AddDalamudImageRounded(CosmeticService.CoreTextures.Cache[icon], ImGui.GetItemRectMin(), size, 45);
        ImGui.GetWindowDrawList().AddCircleFilled(ImGui.GetItemRectMin() + size * .5f, size.X * .55f, color);
        var wouldBeSwitching = _manager.ActivePatternId != Guid.Empty && !isActive;
        if (clicked)
        {
            // at the moment this does not interact with the server but probably should so that pairs dont fall out of sync.
            if (isActive)
            {
                _manager.DisablePattern(_manager.ActivePatternId, MainHub.UID);
            }
            else
            {
                if (wouldBeSwitching)
                {
                    _manager.SwitchPattern(pattern.Identifier, MainHub.UID);
                }
                else
                {
                    _manager.EnablePattern(pattern.Identifier, MainHub.UID);

                }
            }
        }
        CkGui.AttachToolTip(isActive ? "Stop this Pattern." : wouldBeSwitching  
            ? "Switch to play this Pattern on all valid Toys." : "Start this Pattern on all Toys.");
    }
}
