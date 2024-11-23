using Dalamud.Hooking;

namespace OtterGui.Services;

public abstract class FastHook<T> : IHookService where T : Delegate
{
    protected Task<Hook<T>> Task { get; init; } = null!;

    public Task Awaiter
        => Task;

    public bool Finished
        => Task.IsCompletedSuccessfully;

    public nint Address
        => Task.Result.Address;

    public void Enable()
        => Task.Result.Enable();

    public void Disable()
        => Task.Result.Disable();

    public void Toggle(bool value)
    {
        if (value)
            Enable();
        else
            Disable();
    }
}
