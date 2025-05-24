using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using GagSpeak.CkCommons.Gui;
using GagSpeak.PlayerState.Models;
using GagSpeak.UpdateMonitoring;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.EditorCombos;

/// <summary> Capable of displaying every valid emote, along with its icon and all command variants. </summary>
public sealed class EmoteCombo : CkFilterComboCache<ParsedEmoteRow>
{
    private readonly IconDisplayer _iconDrawer;
    private float _iconScale = 1.0f;
    private uint _currentEmoteId;
    
    public EmoteCombo(float scale, IconDisplayer disp, ILogger log)
        : base(() => [ ..EmoteService.ValidLightEmoteCache.OrderBy(e => e.RowId) ], log)
    {
        _iconScale = scale;
        _iconDrawer = disp;
        SearchByParts = true;
        _currentEmoteId = Items.FirstOrDefault().RowId;
    }

    public EmoteCombo(float scale, IconDisplayer disp, ILogger log, Func<IReadOnlyList<ParsedEmoteRow>> gen)
        : base(gen, log)
    {
        _iconDrawer = disp;
        SearchByParts = true;
        _currentEmoteId = Items.FirstOrDefault().RowId;
    }

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => base.IsVisible(globalIndex, filter) && !Items[globalIndex].EmoteCommands.IsDefaultOrEmpty;

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current.RowId == _currentEmoteId)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.RowId == _currentEmoteId);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return CurrentSelectionIdx;
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string id, uint current, float width, uint? searchBg = null)
        => Draw(id, current, width, 1f, CFlags.None, searchBg);
    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string id, uint current, float width, float innerWidthScaler = 1f, CFlags flags = CFlags.None, uint? searchBg = null)
    {
        InnerWidth = width * innerWidthScaler;
        _currentEmoteId = current;
        var preview = Items.FirstOrDefault(i => i.RowId == current).Name ?? "Select Emote...";
        return Draw(id, preview, string.Empty, width, ImGui.GetFrameHeight() * _iconScale, flags, searchBg);
    }

    public void DrawSelectedIcon(float height)
    {
        if (Current.RowId is 0 or uint.MaxValue)
            return;

        var image = _iconDrawer.GetGameIconOrEmpty(Current.IconId);
        ImGui.Image(image.ImGuiHandle, new Vector2(height));
        DrawItemTooltip(Current, image);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var parsedEmote = Items[globalIdx];

        // Draw a ghost selectable at first.
        bool ret = false;
        var pos = ImGui.GetCursorPos();
        var img = _iconDrawer.GetGameIconOrEmpty(parsedEmote.IconId);
        using (ImRaii.Group())
        {
            var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight() * _iconScale);
            ret = ImGui.Selectable("##Entry" + globalIdx, selected, ImGuiSelectableFlags.None, size);
            // Use these positions to go back over and draw it properly this time.
            ImGui.SetCursorPos(pos);

            ImGui.Image(img.ImGuiHandle, new Vector2(size.Y));
            CkGui.TextFrameAlignedInline(parsedEmote.Name);
        }
        DrawItemTooltip(parsedEmote, img);

        return ret;
    }
    protected override string ToString(ParsedEmoteRow emote) => emote.Name ?? string.Empty;

    private void DrawItemTooltip(ParsedEmoteRow item, IDalamudTextureWrap img)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            // begin the tooltip interface
            ImGui.BeginTooltip();
            using (ImRaii.Group())
            {
                ImGui.Image(img.ImGuiHandle, new Vector2(ImGui.GetFrameHeight() * 2));
                ImGui.SameLine();
                using (ImRaii.Group())
                {
                    ImGui.Text(item.Name);
                    CkGui.ColorTextInline($"(Id: {item.RowId})", CkGui.Color(ImGuiColors.DalamudGrey2));
                    CkGui.ColorText($"(Icon: {item.IconId})", CkGui.Color(ImGuiColors.DalamudGrey));
                }
            }
            ImGui.Separator();
            
            CkGui.ColorText("Commands:", ImGuiColors.ParsedPink);
            CkGui.TextInline(string.Join(", ", item.CommandsSafe.Select(cmd => "/" + cmd)));
            ImGui.EndTooltip();
        }
    }

    protected override void OnClosePopup()
    {
        var split = Filter.Text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2 || !ushort.TryParse(split[0], out var setId) || !byte.TryParse(split[1], out var variant))
            return;
    }
}

