using Dalamud.IoC;
using Dalamud.Plugin;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Classes;
using OtterGui.Log;

namespace OtterGui.Services;

public class ServiceManager : IDisposable
{
    private readonly Logger            _logger;
    private readonly ServiceCollection _collection = [];
    public readonly  StartTimeTracker  Timers      = new();
    public           ServiceProvider?  Provider { get; private set; }

    public ServiceManager(Logger logger)
    {
        _logger = logger;
        _collection.AddSingleton(_logger);
        _collection.AddSingleton(this);
    }

    public IEnumerable<T> GetServicesImplementing<T>()
    {
        if (Provider == null)
            yield break;

        var type = typeof(T);
        foreach (var typeDescriptor in _collection)
        {
            if (typeDescriptor.Lifetime is ServiceLifetime.Singleton
             && typeDescriptor.ServiceType.IsAssignableTo(type))
                yield return (T)Provider.GetRequiredService(typeDescriptor.ServiceType);
        }
    }

    public T GetService<T>() where T : class
        => Provider!.GetRequiredService<T>();

    public ServiceProvider CreateProvider()
    {
        if (Provider != null)
            return Provider;

        Provider = _collection.BuildServiceProvider(new ServiceProviderOptions()
        {
            ValidateOnBuild = true,
            ValidateScopes  = false,
        });

        return Provider;
    }

    public void EnsureRequiredServices()
    {
        CreateProvider();

        foreach (var service in _collection)
        {
            if (service.ServiceType.IsAssignableTo(typeof(IRequiredService)))
                Provider!.GetRequiredService(service.ServiceType);
        }
    }

    public ServiceManager AddSingleton<T>()
        => AddSingleton(typeof(T));

    public ServiceManager AddSingleton<T>(Func<IServiceProvider, T> factory) where T : class
    {
        _collection.AddSingleton<T>(Func);
        return this;

        T Func(IServiceProvider p)
        {
            _logger.Verbose($"Constructing Service {typeof(T).Name} with custom factory function.");
            using var timer = Timers.Measure(typeof(T).Name!);
            return factory(p);
        }
    }

    public void AddIServices(Assembly assembly)
    {
        var iType = typeof(IService);
        foreach (var type in assembly.ExportedTypes.Where(t => t is { IsInterface: false, IsAbstract: false } && iType.IsAssignableFrom(t)))
        {
            if (_collection.All(t => t.ServiceType != type))
                AddSingleton(type);
        }
    }

    public ServiceManager AddDalamudService<T>(IDalamudPluginInterface pi) where T : class
    {
        var wrapper = new DalamudServiceWrapper<T>(pi);
        _collection.AddSingleton(wrapper.Service);
        _collection.AddSingleton(pi);
        return this;
    }

    public ServiceManager AddExistingService<T>(T service) where T : class
    {
        _collection.AddSingleton(service);
        return this;
    }

    public void Dispose()
    {
        _logger.Debug("Disposing all services.");
        Provider?.Dispose();
        _logger.Debug("Disposed all services.");
        GC.SuppressFinalize(this);
    }

    private ServiceManager AddSingleton(Type type)
    {
        _collection.AddSingleton(type, Func);
        return this;

        object Func(IServiceProvider p)
        {
            var constructor = type.GetConstructors().MaxBy(c => c.GetParameters().Length);
            if (constructor == null)
                return Activator.CreateInstance(type) ?? throw new Exception($"No constructor available for {type.Name}.");

            var parameterTypes = constructor.GetParameters();
            var parameters     = parameterTypes.Select(t => p.GetRequiredService(t.ParameterType)).ToArray();
            _logger.Verbose(
                $"Constructing Service {type.Name} with {string.Join(", ", parameterTypes.Select(name => $"{name.ParameterType}"))}.");
            using var timer = Timers.Measure(type.Name!);
            return constructor.Invoke(parameters);
        }
    }

    private class DalamudServiceWrapper<T>
    {
        [PluginService]
        public T Service { get; private set; } = default!;

        public DalamudServiceWrapper(IDalamudPluginInterface pi)
        {
            pi.Inject(this);
        }
    }
}
