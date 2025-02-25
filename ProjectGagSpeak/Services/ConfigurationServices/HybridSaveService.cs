using GagSpeak.CkCommons.HybridSaver;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services.Configs;

/// <summary> Handles the Saving of enqueued services. Handles this in a threadsafe manner. </summary>
/// <remarks> All saves are performed via secure write. Failed writes will not process. </remarks>
public sealed class HybridSaveService : HybridSaveServiceBase<ConfigFileProvider>, IHostedService
{
    public HybridSaveService(ILogger<HybridSaveService> logger, ConfigFileProvider fileNameStructure)
        : base(logger, fileNameStructure)
    {
        StaticLogger.Logger.LogCritical("IM BEING INITIALIZED!");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        //StartChecking();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopCheckingAsync();
    }
}
