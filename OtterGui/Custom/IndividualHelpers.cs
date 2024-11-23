using Dalamud.Game.ClientState.Objects.Enums;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Custom;

public static class IndividualHelpers
{
    public static bool DrawObjectKindCombo(float width, ObjectKind current, out ObjectKind result, IEnumerable<ObjectKind> kinds)
    {
        ImGui.SetNextItemWidth(width);
        using var combo = ImRaii.Combo("##newKind", current.ToName());
        result = current;
        if (!combo)
            return false;

        var ret = false;
        foreach (var kind in kinds)
        {
            if (!ImGui.Selectable(kind.ToName(), current == kind))
                continue;

            result = kind;
            ret    = true;
        }

        return ret;
    }

    public static string ToName(this ObjectKind kind)
        => kind switch
        {
            ObjectKind.None      => "Unknown",
            ObjectKind.BattleNpc => "Battle NPC",
            ObjectKind.EventNpc  => "Event NPC",
            ObjectKind.MountType => "Mount",
            ObjectKind.Companion => "Companion",
            ObjectKind.Ornament  => "Accessory",
            _                    => kind.ToString(),
        };
}
