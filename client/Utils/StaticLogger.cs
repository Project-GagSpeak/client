using Microsoft.Extensions.Hosting;

namespace GagSpeak;
public class StaticLoggerInit : IHostedService
{
    private readonly ILoggerFactory _loggerFactory;
    public StaticLoggerInit(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        StaticLogger.Logger = _loggerFactory.CreateLogger("Static Logger");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public static class StaticLogger
{
    public static ILogger Logger { get; set; }
}
