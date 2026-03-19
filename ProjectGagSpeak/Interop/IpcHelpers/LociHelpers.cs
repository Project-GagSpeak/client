using CkCommons.Gui;
using CkCommons.RichText;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using LociApi.Enums;

namespace GagSpeak.Interop.Helpers;

// Help bridge the gap between LociAPI and GagspeakAPI
// Also help bridge the gap between MoodlesTuples and LociStructs.
public static class LociHelpers
{
    /// <summary> Convert the depricated status tuple to a reliable LociStatusStruct. </summary>
    public static LociStatusInfo ToLociTuple(this DeprecatedStatusInfo info)
        => new()
        {
            Version = info.Ver,
            GUID = info.GUID,
            IconID = (uint)info.IconID,
            Title = info.Title,
            Description = info.Desc,
            CustomVFXPath = info.Vfx,
            ExpireTicks = info.ExpireTicks,
            Type = (StatusType)info.Type,
            Stacks = info.Stacks,
            StackSteps = info.Steps,
            StackToChain = 0,
            Modifiers = info.Mods,
            ChainedGUID = info.ChainedStatus,
            ChainType = 0,
            ChainTrigger = (ChainTrigger)info.ChainCond,
            Applier = info.Applier,
            Dispeller = info.Dispeller
        };

    /// <summary> Convert the LociStatusTuple to GagSpeakAPI struct format. </summary>
    public static LociStatusStruct ToStruct(this LociStatusInfo info)
        => new()
        {
            Version = info.Version,
            GUID = info.GUID,
            IconID = info.IconID,
            Title = info.Title,
            Description = info.Description,
            CustomVFXPath = info.CustomVFXPath,
            ExpireTicks = info.ExpireTicks,
            Type = (byte)info.Type,
            Stacks = info.Stacks,
            StackSteps = info.StackSteps,
            StackToChain = info.StackToChain,
            Modifiers = info.Modifiers,
            ChainedGUID = info.ChainedGUID,
            ChainType = (byte)info.ChainType,
            ChainTrigger = (int)info.ChainTrigger,
            Applier = info.Applier,
            Dispeller = info.Dispeller
        };

    /// <summary> Convert the GagSpeakAPI struct to the LociStatusTuple. </summary>
    public static LociStatusInfo ToTuple(this LociStatusStruct statStruct)
        => (statStruct.Version,
            statStruct.GUID,
            statStruct.IconID,
            statStruct.Title,
            statStruct.Description,
            statStruct.CustomVFXPath,
            (statStruct.ExpireTicks == 0) ? -1 : statStruct.ExpireTicks,
            (StatusType)statStruct.Type,
            statStruct.Stacks,
            statStruct.StackSteps,
            statStruct.StackToChain,
            statStruct.Modifiers,
            statStruct.ChainedGUID,
            (ChainType)statStruct.ChainType,
            (ChainTrigger)statStruct.ChainTrigger,
            statStruct.Applier,
            statStruct.Dispeller
        );

    /// <summary> Convert the depricated preset tuple to a reliable LociPresetStruct. </summary>
    public static LociPresetInfo ToLociTuple(this DeprecatedPresetInfo info)
        => new()
        {
            Version = 1,
            GUID = info.GUID,
            Statuses = info.Statuses,
            ApplicationType = info.ApplicationType,
            Title = info.Title,
            Description = string.Empty,
        };

    /// <summary> Convert the LociPresetTuple to GagSpeakAPI struct format. </summary>
    public static LociPresetStruct ToStruct(this LociPresetInfo info)
        => new()
        {
            Version = info.Version,
            GUID = info.GUID,
            Statuses = info.Statuses,
            ApplicationType = info.ApplicationType,
            Title = info.Title,
            Description = info.Description
        };

    /// <summary> Convert the GagSpeakAPI struct to the LociPresetTuple. </summary>
    public static LociPresetInfo ToTuple(this LociPresetStruct presetStruct)
        => (presetStruct.Version,
            presetStruct.GUID,
            presetStruct.Statuses,
            presetStruct.ApplicationType,
            presetStruct.Title,
            presetStruct.Description);

    public static void AttachTooltip(this LociStatusInfo item, LociContainer data)
    {
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            return;

        ImGui.SetNextWindowSizeConstraints(new Vector2(350f, 0f), new Vector2(350f, float.MaxValue));
        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
            .Push(ImGuiStyleVar.WindowRounding, 4f)
            .Push(ImGuiStyleVar.PopupBorderSize, 1f);
        using var c = ImRaii.PushColor(ImGuiCol.Border, GsCol.VibrantPink.Uint());
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

        if (item.ChainedGUID != Guid.Empty)
        {
            if (item.ChainType == (byte)ChainType.Status)
            {
                CkGui.ColorText("Chained Status:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                var status = data.Statuses.TryGetValue(item.ChainedGUID, out var match) ? match.Title : "Unknown";
                CkRichText.Text(status, 100);
            }
            else
            {
                CkGui.ColorText("Chained Preset:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                var preset = data.Presets.TryGetValue(item.ChainedGUID, out var match) ? match.Title : "Unknown";
                CkRichText.Text(preset, 100);
            }
        }
    }

    public static void AttachTooltip(this LociStatusStruct item, LociContainer data)
    {
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            return;

        ImGui.SetNextWindowSizeConstraints(new Vector2(350f, 0f), new Vector2(350f, float.MaxValue));
        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
            .Push(ImGuiStyleVar.WindowRounding, 4f)
            .Push(ImGuiStyleVar.PopupBorderSize, 1f);
        using var c = ImRaii.PushColor(ImGuiCol.Border, GsCol.VibrantPink.Uint());
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

        if (item.ChainedGUID != Guid.Empty)
        {
            if (item.ChainType == (byte)ChainType.Status)
            {
                CkGui.ColorText("Chained Status:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                var status = data.Statuses.TryGetValue(item.ChainedGUID, out var match) ? match.Title : "Unknown";
                CkRichText.Text(status, 100);
            }
            else
            {
                CkGui.ColorText("Chained Preset:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                var preset = data.Presets.TryGetValue(item.ChainedGUID, out var match) ? match.Title : "Unknown";
                CkRichText.Text(preset, 100);
            }
        }
    }

    public static bool CanApply(PairPerms perms, IEnumerable<LociStatusInfo> statuses)
    {
        foreach (var status in statuses)
        {
            if (status.Type == StatusType.Positive && !perms.LociAccess.HasAny(LociAccess.Positive))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Positive Statuses.");
                return false;
            }
            else if (status.Type == StatusType.Negative && !perms.LociAccess.HasAny(LociAccess.Negative))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Negative Statuses.");
                return false;

            }
            else if (status.Type == StatusType.Special && !perms.LociAccess.HasAny(LociAccess.Special))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Special Statuses.");
                return false;
            }
            else if (status.ExpireTicks == -1 && !perms.LociAccess.HasAny(LociAccess.Permanent))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Permanent Statuses.");
                return false;
            }
            else if (status.ExpireTicks > 0)
            {
                var totalTime = TimeSpan.FromMilliseconds(status.ExpireTicks - DateTimeOffset.Now.ToUnixTimeMilliseconds());
                if (totalTime > perms.MaxLociTime)
                {
                    Svc.Toasts.ShowError("You do not have permission to apply Statuses for that long.");
                    return false;
                }
            }
        }
        // return true if reached here.
        return true;
    }

    /// <summary>
    ///     API Tuple format of if we can apply statuses or not.
    /// </summary>
    public static bool CanApply(PairPerms perms, IEnumerable<LociStatusStruct> statuses)
    {
        foreach (var status in statuses)
        {
            if (status.Type == 0 && !perms.LociAccess.HasAny(LociAccess.Positive))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Positive Statuses.");
                return false;
            }
            else if (status.Type == 1 && !perms.LociAccess.HasAny(LociAccess.Negative))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Negative Statuses.");
                return false;

            }
            else if (status.Type == 2 && !perms.LociAccess.HasAny(LociAccess.Special))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Special Statuses.");
                return false;
            }
            else if (status.ExpireTicks == -1 && !perms.LociAccess.HasAny(LociAccess.Permanent))
            {
                Svc.Toasts.ShowError("You do not have permission to apply Permanent Statuses.");
                return false;
            }
            else if (status.ExpireTicks > 0)
            {
                var totalTime = TimeSpan.FromMilliseconds(status.ExpireTicks - DateTimeOffset.Now.ToUnixTimeMilliseconds());
                if (totalTime > perms.MaxLociTime)
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
