using CkCommons.HybridSaver;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services.Configs;

public interface IHybridSavable : IHybridSavable<GsFiles>;

/// <summary> Handles the Saving of enqueued services. Handles this in a threadsafe manner. </summary>
/// <remarks> All saves are performed via secure write. Failed writes will not process. </remarks>
public sealed class HybridSaveService : HybridSaveServiceBase<GsFiles>, IHostedService
{
    private readonly ILogger<HybridSaveService> _logger;

    private HashSet<IHybridSavable> _toSaveOnDispose = [];

    public HybridSaveService(ILogger<HybridSaveService> logger, GsFiles provider)
        : base(provider)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting HybridSaveService...");
        Init();
        _logger.LogInformation("HybridSaveService started.");
        return Task.CompletedTask;
    }

    public bool MarkForSaveOnDispose(IHybridSavable savable)
        => _toSaveOnDispose.Add(savable);

    public bool ReleaseFromSaveOnDispose(IHybridSavable savable)
        => _toSaveOnDispose.Remove(savable);

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping HybridSaveService...");
        _logger.LogDebug($"Savables tracked for DisposalSave:" +
            $"\n──────────────────────\n - " +
            $"{string.Join("\n - ", _toSaveOnDispose.Select(s => s.GetType().Name))}" +
            $"\n──────────────────────");
        foreach (var savable in _toSaveOnDispose)
        {
            try
            {
                Save(savable);
                _logger.LogDebug($"Enqueued [{savable.GetType().Name}] for save on disposal.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save [{savable.GetType().Name}] on disposal:\n{ex}");
            }
        }
        await Dispose();
        _logger.LogInformation("HybridSaveService stopped.");
    }
}
