using Dalamud.Interface.Utility;
using GagSpeak.Interop.Ipc;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Widgets;
using System.Numerics;
using GagSpeak.UI.Components.Combos;

// taken off Otter's ModCombo.cs from the mod association tab for convince purposes
namespace GagSpeak.Interop.IpcHelpers.Penumbra;
public sealed class CustomModCombo : CustomFilterComboCache<(Mod Mod, ModSettings Settings)>
{
    public CustomModCombo(IpcCallerPenumbra penumbra, ILogger log)
        : base(penumbra.GetMods, MouseWheelType.None, log)
    {
        SearchByParts = false;
    }

    protected override string ToString((Mod Mod, ModSettings Settings) obj)
        => obj.Mod.Name;

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => filter.IsContained(Items[globalIndex].Mod.Name) || filter.IsContained(Items[globalIndex].Mod.DirectoryName);

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        using var id = ImRaii.PushId(globalIdx);
        var (mod, settings) = Items[globalIdx];
        bool ret;
        using (var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), !settings.Enabled))
        {
            ret = ImGui.Selectable(mod.Name, selected);
        }
        // draws a fancy box when the mod is hovered giving you the details about the mod.
        if (ImGui.IsItemHovered())
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
            using var tt = ImRaii.Tooltip();
            var namesDifferent = mod.Name != mod.DirectoryName;
            ImGui.Dummy(new Vector2(300 * ImGuiHelpers.GlobalScale, 0));
            using (var group = ImRaii.Group())
            {
                if (namesDifferent)
                    ImGui.TextUnformatted("Directory Name");
                ImGui.TextUnformatted("Enabled");
                ImGui.TextUnformatted("Priority");
                DrawSettingsLeft(settings);
            }

            ImGui.SameLine(Math.Max(ImGui.GetItemRectSize().X + 3 * ImGui.GetStyle().ItemSpacing.X, 150 * ImGuiHelpers.GlobalScale));
            using (var group = ImRaii.Group())
            {
                if (namesDifferent)
                    ImGui.TextUnformatted(mod.DirectoryName);
                ImGui.TextUnformatted(settings.Enabled.ToString());
                ImGui.TextUnformatted(settings.Priority.ToString());
                DrawSettingsRight(settings);
            }
        }

        return ret;
    }

    public static void DrawSettingsLeft(ModSettings settings)
    {
        foreach (var setting in settings.Settings)
        {
            ImGui.TextUnformatted(setting.Key);
            for (var i = 1; i < setting.Value.Count; ++i)
                ImGui.NewLine();
        }
    }

    public static void DrawSettingsRight(ModSettings settings)
    {
        foreach (var setting in settings.Settings)
        {
            if (setting.Value.Count == 0)
                ImGui.TextUnformatted("<None Enabled>");
            else
                foreach (var option in setting.Value)
                    ImGui.TextUnformatted(option);
        }
    }
}
