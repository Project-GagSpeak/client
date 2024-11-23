namespace GagSpeak.Services.Mediator;

/// <summary>
/// The disposable base for a mediator subscriber, including a dispose method.
/// </summary>
public abstract class DisposableMediatorSubscriberBase : MediatorSubscriberBase, IDisposable
{
    protected DisposableMediatorSubscriberBase(ILogger logger, GagspeakMediator mediator) : base(logger, mediator)
    { }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Logger.LogTrace("Disposing "+GetType().Name+" ("+this+")", LoggerType.Mediator);
        UnsubscribeAll();
    }
}
