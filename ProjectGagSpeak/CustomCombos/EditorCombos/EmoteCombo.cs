using Dalamud.Plugin.Services;
using GagSpeak.Services;
using GagSpeak.UpdateMonitoring;
using ImGuiNET;
using OtterGui.Raii;

namespace GagSpeak.CustomCombos.Editor;

/// <summary> Capable of displaying every valid emote, along with its icon and all command variants. </summary>
public sealed class EmoteCombo : CkFilterComboCache<ParsedEmoteRow>
{
    private readonly ITextureProvider _iconDrawer;
    public EmoteCombo(ItemService items, ITextureProvider iconDrawer, ILogger log)
        : base(() => [
            ..EmoteMonitor.ValidEmotes.OrderBy(e => e.RowId)
        ], log)
    {
        _iconDrawer = iconDrawer;
        SearchByParts = true;
    }

    public bool Draw(string label, float width, float innerWidthScaler, float itemH, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        InnerWidth = width * innerWidthScaler;
        // if we have a new item selected we need to update some conditionals.
        var previewLabel = CurrentSelection.Name ?? "Select an Emote...";
        return Draw(label, previewLabel, string.Empty, width, itemH, flags);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var parsedEmote = Items[globalIdx];

        // Draw a ghost selectable at first.
        var startPos = ImGui.GetCursorPos();
        var ret = ImGui.Selectable("##Entry" + globalIdx, selected, ImGuiSelectableFlags.None, new Vector2(0, 24));
        var endPos = ImGui.GetCursorPos();

        // Use these positions to go back over and draw it properly this time.
        ImGui.SetCursorPos(startPos);
        using (ImRaii.Group())
        {
            var icon = _iconDrawer.GetFromGameIcon(parsedEmote.GetIconId()).GetWrapOrDefault();
            if(icon is { } wrap)
            {
                ImGui.Image(icon.ImGuiHandle, new Vector2(24, 24));
            };
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(parsedEmote.Name);
        }
        // Correct cursor position.
        ImGui.SetCursorPos(endPos);
        return ret;
    }
    protected override string ToString(ParsedEmoteRow emote) => emote.Name;

    protected override void OnClosePopup()
    {
        var split = Filter.Text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2 || !ushort.TryParse(split[0], out var setId) || !byte.TryParse(split[1], out var variant))
            return;
    }
}

