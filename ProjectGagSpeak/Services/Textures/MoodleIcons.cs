using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.Gui;
using GagSpeak.CkCommons.Helpers;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System.Buffers.Binary;
using GagSpeak.CkCommons;

namespace GagSpeak.Services.Textures;

// Migrate this to a static class soon.
public class MoodleIcons
{
    public static IDalamudTextureWrap? GetGameIconOrDefault(uint iconId)
        => Svc.Texture.GetFromGameIcon(iconId).GetWrapOrDefault();

    public static IDalamudTextureWrap GetGameIconOrEmpty(uint iconId)
        => Svc.Texture.GetFromGameIcon(iconId).GetWrapOrEmpty();

    public static IDalamudTextureWrap? GetGameIconOrDefault(int iconId, int stacks)
        => Svc.Texture.GetFromGameIcon(new GameIconLookup((uint)(iconId + stacks - 1))).GetWrapOrDefault();


    /// <summary>
    ///     Draws the Moodle icon. This only draw a single image so you can use IsItemHovered() outside.
    /// </summary>
    public static void DrawMoodleIcon(int iconId, int stacks, Vector2 size)
    {
        if (Svc.Texture.GetFromGameIcon(new GameIconLookup((uint)(iconId + stacks - 1))).GetWrapOrDefault() is { } wrap)
            ImGui.Image(wrap.ImGuiHandle, size);
        else
            ImGui.Dummy(size);
    }

    public static void DrawMoodleStatusTooltip(MoodlesStatusInfo item, IEnumerable<MoodlesStatusInfo> otherStatuses)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetNextWindowSizeConstraints(new Vector2(350f, 0f), new Vector2(350f, float.MaxValue));

            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            ImGui.BeginTooltip();

            // push the title, converting all color tags into the actual label.
            CkRichText.Text(item.Title);

            if (!item.Description.IsNullOrWhitespace())
            {
                ImGui.Separator();
                CkRichText.Text(350f, item.Description);
            }

            ImGui.Separator();
            CkGui.ColorText("Stacks:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Stacks.ToString());
            if (item.StackOnReapply)
            {
                ImGui.SameLine();
                CkGui.ColorText(" (inc by " + item.StacksIncOnReapply + ")", ImGuiColors.ParsedGold);
            }

            CkGui.ColorText("Duration:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text($"{item.Days}d {item.Hours}h {item.Minutes}m {item.Seconds}");

            CkGui.ColorText("Category:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Type.ToString());

            CkGui.ColorText("Dispellable:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Dispelable ? "Yes" : "No");

            if (item.StatusOnDispell != Guid.Empty)
            {
                CkGui.ColorText("StatusOnDispell:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                var status = otherStatuses.FirstOrDefault(x => x.GUID == item.StatusOnDispell).Title ?? "Unknown";
                ImGui.Text(status);
            }

            ImGui.EndTooltip();
        }
    }
}
