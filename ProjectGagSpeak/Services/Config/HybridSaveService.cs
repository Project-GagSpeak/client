using CkCommons.HybridSaver;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services.Configs;

/// <summary> Handles the Saving of enqueued services. Handles this in a threadsafe manner. </summary>
/// <remarks> All saves are performed via secure write. Failed writes will not process. </remarks>
public sealed class HybridSaveService : HybridSaveServiceBase<ConfigFileProvider>, IHostedService
{
    public HybridSaveService(ConfigFileProvider fileNameStructure) : base(fileNameStructure)
    { }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Init();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Dispose();
    }
}
