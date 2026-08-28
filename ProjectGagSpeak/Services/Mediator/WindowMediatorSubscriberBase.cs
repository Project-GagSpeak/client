using Dalamud.Interface.Windowing;

namespace GagSpeak.Services.Mediator;

public abstract class WindowMediatorSubscriberBase : Window, IMediatorSubscriber, IDisposable
{
    protected readonly ILogger _logger;

    protected WindowMediatorSubscriberBase(ILogger logger, GagspeakMediator mediator, string name) : base(name)
    {
        _logger = logger;
        Mediator = mediator;
        _logger.LogTrace("Creating "+GetType(), LoggerType.Mediator);

        Mediator.Subscribe<UiToggleMessage>(this, (msg) =>
        {
            if (msg.UiType == GetType())
            {
                // Handle the toggle type (Toggle, Show, Hide)
                switch (msg.ToggleType)
                {
                    case ToggleType.Toggle:
                        Toggle();  // Toggles visibility (e.g., if visible, hide; if hidden, show)
                        _logger.LogTrace("Toggling UI", LoggerType.Mediator);
                        break;

                    case ToggleType.Show:
                        IsOpen = true;
                        BringToFront();
                        _logger.LogTrace("Showing UI", LoggerType.Mediator);
                        break;

                    case ToggleType.Hide:
                        IsOpen = false;
                        _logger.LogTrace("Hiding UI", LoggerType.Mediator);
                        break;
                }
            }
        });

    }

    // the gagspeak mediator
    public GagspeakMediator Mediator { get; }

    /// <summary>
    /// Properly dispose of the mediator object
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary> 
    /// Overrides the default WindowSystem Draw so we can call out own internal draws 
    /// </summary>
    public override void Draw()
        => DrawInternal();

    /// <summary> 
    /// Abstract method for DrawingInternally, defined by classes using the subscriber base 
    /// </summary>
    protected abstract void DrawInternal();

    /// <summary>
    /// All mediators require a startasync and stopasync method. This calls the stopasync method at the base.
    /// The startasync will be in the main GagspeakMediator
    /// </summary>
    public virtual Task StopAsync(CancellationToken ct)
        => Task.CompletedTask;

    protected virtual void Dispose(bool disposing)
    {
        _logger.LogTrace("Disposing "+GetType(), LoggerType.Mediator);
        Mediator.UnsubscribeAll(this);
    }
}
