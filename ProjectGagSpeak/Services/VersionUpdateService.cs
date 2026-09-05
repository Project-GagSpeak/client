using CkCommons;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Services.Mediator;
using System.Reflection;
using System.Text.Json;

namespace GagSpeak.Services;

/// <summary>
///   Dalamud Updates don't always natively inform on version updates. <br/>
///   UpdateService code was asopted from PlayerSync's implementation.
/// </summary>
public class VersionUpdateService : DisposableMediatorSubscriberBase
{
    private static readonly TimeSpan UPDATE_PERIOD = TimeSpan.FromMinutes(5);
    private const string RepositoryUrl = "https://raw.githubusercontent.com/Project-GagSpeak/repo/main/projectgagspeak.json";

    private readonly ILogger<VersionUpdateService> _logger;
    private readonly HttpClient _httpClient;

    // Disconnect is SameThreadMessage, so needs a lock.
    private readonly object _lock = new();
    private Version _latestVersion;

    private CancellationTokenSource? _checkCTS;
    private Task? _checkTask;

    public VersionUpdateService(ILogger<VersionUpdateService> logger, GagspeakMediator mediator, HttpClient httpClient)
        : base(logger, mediator)
    {
        _logger = logger;
        _httpClient = httpClient;
        _latestVersion = Assembly.GetExecutingAssembly().GetName().Version!;

        Mediator.Subscribe<ConnectedMessage>(this, _ => Start());
        Mediator.Subscribe<DisconnectedMessage>(this, _ => Stop());
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Stop();
        base.Dispose(disposing);
    }

    private void Start()
    {
        lock (_lock)
        {
            if (_checkTask is { IsCompleted: false })
                return;
            _checkCTS = _checkCTS.SafeCancelRecreate();
            _checkTask = PeriodicCheckVersionTask(_checkCTS.Token);
        }
    }

    private void Stop()
    {
        CancellationTokenSource? cts;
        lock (_lock)
        {
            cts = _checkCTS;
            _checkCTS = null;
        }
        cts.SafeCancelDispose();
    }

    private async Task PeriodicCheckVersionTask(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, RepositoryUrl);
                    req.Headers.Accept.ParseAdd("application/json");

                    using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"Unable to check for update from {RepositoryUrl} (HTTP {(int)resp.StatusCode})");
                        await Task.Delay(UPDATE_PERIOD, ct).ConfigureAwait(false);
                        continue;
                    }

                    var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                    Version version;
                    try
                    {
                        version = ParseAssemblyVersion(json);
                    }
                    catch
                    {
                        _logger.LogWarning($"There was an issue parsing the repo.json for {RepositoryUrl}");
                        await Task.Delay(UPDATE_PERIOD, ct).ConfigureAwait(false);
                        continue;
                    }

                    if (_latestVersion < version)
                    {
                        _logger.LogInformation($"Detected repository version (v{version}) is higher than the client version (v{_latestVersion})");
                        _latestVersion = version;
                        AlertService.PrintVersionUpdateMessage($"Update v{version} is now downloadable. Please update at your leisure.", version.ToString());
                    }
                    await Task.Delay(UPDATE_PERIOD, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Version update check failed");
                    await Task.Delay(UPDATE_PERIOD, ct).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            lock (_lock)
                _checkTask = null;
        }
    }

    private static Version ParseAssemblyVersion(string json)
    {
        using var repoJson = JsonDocument.Parse(json);
        var firstElement = repoJson.RootElement[0];
        var rawVersion = firstElement.TryGetProperty("AssemblyVersion", out var v) ? v.ToString() : null;
        return rawVersion is not null && Version.TryParse(rawVersion, out var ver) ? ver : new Version(0, 0, 0, 0);
    }
}
