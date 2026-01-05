using CkCommons.Gui;
using CkCommons.RichText;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Utils;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;

namespace GagSpeak;
public static class MoodlesEx
{
    public static IPCMoodleAccessTuple ToIpc(this ProviderMoodleAccessTuple t)
        => ((MoodleAccess)t.OtherAccessFlags, t.OtherMaxTime, (MoodleAccess)t.CallerAccessFlags, t.CallerMaxTime);

    public static ProviderMoodleAccessTuple ToCallGate(this IPCMoodleAccessTuple t)
        => ((short)t.OtherAccess, t.OtherMaxTime, (short)t.CallerAccess, t.CallerMaxTime);

    public static void AttachTooltip(this MoodlesStatusInfo item, IEnumerable<MoodlesStatusInfo> otherStatuses)
    {
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            return;

        ImGui.SetNextWindowSizeConstraints(new Vector2(350f, 0f), new Vector2(350f, float.MaxValue));
        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
            .Push(ImGuiStyleVar.WindowRounding, 4f)
            .Push(ImGuiStyleVar.PopupBorderSize, 1f);
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        using var tt = ImRaii.Tooltip();

        // push the title, converting all color tags into the actual label.
        CkRichText.Text(item.Title, cloneId: 100);
        if (!item.Description.IsNullOrWhitespace())
        {
            ImGui.Separator();
            CkRichText.Text(350f, item.Description);
        }

        CkGui.ColorText("Duration:", ImGuiColors.ParsedGold);
        var length = TimeSpan.FromTicks(item.ExpireTicks);
        ImGui.SameLine();
        ImGui.Text($"{length.Days}d {length.Hours}h {length.Minutes}m {length.Seconds}");

        CkGui.ColorText("Category:", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(item.Type.ToString());

        if (item.ChainedStatus != Guid.Empty)
        {
            CkGui.ColorText("Chained Status:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            var status = otherStatuses.FirstOrDefault(x => x.GUID == item.ChainedStatus).Title ?? "Unknown";
            ImGui.Text(status);
        }
    }

    /// <summary>
    ///     Does a fairly basic check before applying a set of status tuples to ensure we are able to apply them.
    /// </summary>
    public static bool CanApplyMoodles(PairPerms perms, IEnumerable<MoodlesStatusInfo> statuses)
    {
        foreach (var status in statuses)
        {
            if (status.Type is StatusType.Positive && !perms.MoodleAccess.HasAny(MoodleAccess.Positive))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Positive Statuses.");
                return false;
            }
            else if (status.Type is StatusType.Negative && !perms.MoodleAccess.HasAny(MoodleAccess.Negative))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Negative Statuses.");
                return false;

            }
            else if (status.Type is StatusType.Special && !perms.MoodleAccess.HasAny(MoodleAccess.Special))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Special Statuses.");
                return false;
            }
            else if (status.Permanent && !perms.MoodleAccess.HasAny(MoodleAccess.Permanent))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Permanent Statuses.");
                return false;
            }
            else if (status.ExpireTicks > 0)
            {
                var totalTime = TimeSpan.FromMilliseconds(status.ExpireTicks - DateTimeOffset.Now.ToUnixTimeMilliseconds());
                if (totalTime > perms.MaxMoodleTime)
                {
                    Svc.Toasts.ShowError("You do not have permission to apply Statuses for that long.");
                    return false;
                }
            }
        }
        // return true if reached here.
        return true;
    }


}
