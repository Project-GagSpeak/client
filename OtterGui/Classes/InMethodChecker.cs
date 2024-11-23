namespace OtterGui.Classes;

public readonly struct InMethodChecker
{
    private readonly ThreadLocal<uint> _inUpdate = new(() => 0, false);

    public uint RecursionDepth
        => _inUpdate.IsValueCreated ? _inUpdate.Value : 0;

    public bool InMethod
        => _inUpdate is { IsValueCreated: true, Value: > 0 };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public RaiiSetter EnterMethod()
        => new(_inUpdate);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public InMethodChecker()
    { }

    public readonly ref struct RaiiSetter
    {
        private readonly ThreadLocal<uint> _threadLocal;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public RaiiSetter(ThreadLocal<uint> threadLocal)
        {
            _threadLocal = threadLocal;
            ++_threadLocal.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Dispose()
            => --_threadLocal.Value;
    }
}
