namespace GagSpeak.Services.Mediator;

/// <summary>
/// The subscriber base, including a tracelog, and a unsubscribe all method.
/// </summary>
public abstract class MediatorSubscriberBase : IMediatorSubscriber
{
    protected MediatorSubscriberBase(ILogger logger, GagspeakMediator mediator)
    {
        Logger = logger;

        Logger.LogTrace("Creating " + GetType().Name + " (" + this + ")", LoggerType.Mediator);
        Mediator = mediator;
    }

    public GagspeakMediator Mediator { get; }
    protected ILogger Logger { get; }

    protected void UnsubscribeAll()
    {
        Logger.LogTrace("Unsubscribing from all for " + GetType().Name + " (" + this + ")", LoggerType.Mediator);
        Mediator.UnsubscribeAll(this);
    }
}
