using JetBrains.Annotations;

namespace OtterGui.Services;

[UsedImplicitly(ImplicitUseTargetFlags.Itself | ImplicitUseTargetFlags.WithInheritors)]
public interface IService
{ }

public interface IDataContainer : IService
{
    public long   Time       { get; }
    public long   Memory     { get; }
    public string Name       { get; }
    public int    TotalCount { get; }
}

public interface IAsyncService : IService
{
    public Task Awaiter { get; }

    public bool Finished { get; }
}

public interface IRequiredService : IService;

public interface IAwaitedService : IRequiredService, IAsyncService;

public interface IHookService : IAwaitedService
{
    public nint Address { get; }
    public void Enable();
    public void Disable();
}

public interface IAsyncDataContainer : IDataContainer, IAsyncService
{ }

public interface IInvokeService : IService
{
    public void Invoke();
}

public interface ILoadService : IService
{
    public void Load();
}

public interface IUiService : IService
{ }

public interface IApiService : IService
{ }
