using ImGuiNET;

namespace OtterGui.Text.HelperObjects;

internal struct DataCache<T> where T : unmanaged, INumber<T>
{
    public static T    Value;
    public static uint LastId;
    public static bool IsActive;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void Update(T value, uint id)
    {
        Value    = value;
        LastId   = id;
        IsActive = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool Return(ref T oldValue)
    {
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            IsActive = false;
            if (Value == oldValue)
                return false;

            oldValue = Value;
            return true;
        }

        if (ImGui.IsItemDeactivated())
            IsActive = false;
        return false;
    }
}
