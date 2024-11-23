namespace OtterGui.Tasks;

/// <summary>
/// For tasks that are kept as members.
/// Just a wrapper around Task.Run to better keep track about task usage.
/// </summary>
public static class TrackedTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Task Run(Action action)
        => Task.Run(action);
}

/// <summary>
/// For tasks that are immediately awaited on the same thread.
/// Just a wrapper around Task.Run to better keep track about task usage.
/// </summary>
public static class AwaitedTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Task Run(Action action)
        => Task.Run(action);
}

/// <summary>
/// For tasks that are used asynchronously.
/// Just a wrapper around Task.Run to better keep track about task usage.
/// </summary>
public static class AsyncTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Task Run(Action action)
        => Task.Run(action);
}
