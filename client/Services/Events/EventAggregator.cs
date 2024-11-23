using GagSpeak.Services.Mediator;
using Microsoft.Extensions.Hosting;
using GagSpeak.Utils;

namespace GagSpeak.Services.Events;

/// <summary>
/// Handles the management of logging, storing, and saving collected Events through the plugins lifespan.
/// </summary>
public class EventAggregator : MediatorSubscriberBase, IHostedService
{
    private readonly ILogger<EventAggregator> _logger;
    private readonly string _configDirectory;

    private readonly RollingList<InteractionEvent> _events = new(500);
    private readonly SemaphoreSlim _lock = new(1);
    private string CurrentLogName => $"{DateTime.Now:yyyy-MM-dd}-events.log";
    private DateTime _currentTime;

    public EventAggregator(string configDirectory, ILogger<EventAggregator> logger, 
        GagspeakMediator gagspeakMediator) : base(logger, gagspeakMediator)
    {
        _logger = logger;
        _configDirectory = configDirectory;
        // Collect any events sent out.
        Mediator.Subscribe<EventMessage>(this, (msg) =>
        {
            _lock.Wait();
            try
            {
                Logger.LogTrace("Received Event: "+msg.Event.ToString(), LoggerType.ActionsNotifier);
                _events.Add(msg.Event);
                WriteToFile(msg.Event);
                UnreadInteractionsCount++;
            }
            finally
            {
                _lock.Release();
            }

            RecreateLazy();
        });

        // Create a new event list
        EventList = CreateEventLazy();
        // Get the current time on the current day.
        _currentTime = DateTime.Now - TimeSpan.FromDays(1);
    }

    /// <summary>
    /// Public list of events that have been collected and stored in the EventAggregators rolling list.
    /// </summary>
    public Lazy<List<InteractionEvent>> EventList { get; private set; }
    public bool NewEventsAvailable => !EventList.IsValueCreated;
    public string EventLogFolder => Path.Combine(_configDirectory, "eventlog");
    public static int UnreadInteractionsCount = 0;

    /// <summary>
    /// Recreate the publically accessible list of events.
    /// </summary>
    private void RecreateLazy()
    {
        if (!EventList.IsValueCreated) 
            return;

        EventList = CreateEventLazy();
    }

    /// <summary>
    /// Create a new lazy event list with all the private event data inside.
    /// </summary>
    /// <returns></returns>
    private Lazy<List<InteractionEvent>> CreateEventLazy()
    {
        return new Lazy<List<InteractionEvent>>(() =>
        {
            _lock.Wait();
            try
            {
                return [.. _events];
            }
            finally
            {
                _lock.Release();
            }
        });
    }

    /// <summary>
    /// Write the event data into the event log file.
    /// </summary>
    private void WriteToFile(InteractionEvent receivedEvent)
    {
        if (DateTime.Now.Day != _currentTime.Day)
        {
            try
            {
                _currentTime = DateTime.Now;
                var filesInDirectory = Directory.EnumerateFiles(EventLogFolder, "*.log");
                if (filesInDirectory.Skip(10).Any())
                {
                    File.Delete(filesInDirectory.OrderBy(f => new FileInfo(f).LastWriteTimeUtc).First());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete last events");
            }
        }

        var eventLogFile = Path.Combine(EventLogFolder, CurrentLogName);
        try
        {
            if (!Directory.Exists(EventLogFolder)) Directory.CreateDirectory(EventLogFolder);
            File.AppendAllLines(eventLogFile, [receivedEvent.ToString()]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Could not write to event file {eventLogFile}");
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Started Interaction EventAggregator");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

