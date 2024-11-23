using Microsoft.Extensions.Hosting;
using System.Reflection;

// After inspection of gagspeaks code, ive come to the conclusion that I like the idea of events handled by a mediator much more than i do manual event creation.
// it also helps clean up file clutter with event handling, and looks a lot cleaner in code.
// Therefore the following model is heavily taken from gagspeak with little to no changes.
namespace GagSpeak.Services.Mediator;

// the gagspeak mediator had a performance collector for tracking interactions. I have removed it, but if needed in the future, reference back to it.
public sealed class GagspeakMediator : IHostedService
{
    private readonly object _addRemoveLock = new();                         // the add remove lock for the subscriber dictionary
    private readonly ConcurrentDictionary<object, DateTime> _lastErrorTime = [];      // the last error time for a subscriber
    private readonly ILogger<GagspeakMediator> _logger;                     // the logger for the mediator
    private readonly CancellationTokenSource _loopCts = new();              // the cancellation token source for the loop
    private readonly ConcurrentQueue<MessageBase> _messageQueue = new();    // the queue of mediator published messages
    private readonly ConcurrentDictionary<Type, HashSet<SubscriberAction>> _subscriberDict = []; // the subscriber dictionary
    private bool _processQueue = false;                                     // if we should be processing the queue
    private readonly ConcurrentDictionary<Type, MethodInfo?> _genericExecuteMethods = new(); // the generic execution methods
    public GagspeakMediator(ILogger<GagspeakMediator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs information about all subscribers, including the types of messages they are subscribed to.
    /// (No real reason to really add this, possibly remove.)
    /// </summary>
    public void PrintSubscriberInfo()
    {
        // for each subscriber in the subscriber dictionary, log the subscriber and the messages they are subscribed to
        foreach (var subscriber in _subscriberDict.SelectMany(c => c.Value.Select(v => v.Subscriber))
            .DistinctBy(p => p).OrderBy(p => p.GetType().FullName, StringComparer.Ordinal).ToList())
        {
            // log the subscriber
            _logger.LogInformation("Subscriber {type}: {sub}", subscriber.GetType().Name, subscriber.ToString());
            // create a string builder
            StringBuilder sb = new();
            sb.Append("=> ");
            // for each item in the subscriber dictionary, if the subscriber is the same as the subscriber in the loop, append the name of the message to the string builder
            foreach (var item in _subscriberDict.Where(item => item.Value.Any(v => v.Subscriber == subscriber)).ToList())
            {
                sb.Append(item.Key.Name).Append(", ");
            }

            // if the string builder is not equal to "=> ", log the string builder
            if (!string.Equals(sb.ToString(), "=> ", StringComparison.Ordinal))
                _logger.LogInformation("{sb}", sb.ToString());
            _logger.LogInformation("---");
        }
    }

    /// <summary>
    /// Allows publishing a message of type T. If the message indicates it should keep the thread context, it's executed immediately; otherwise, it's enqueued for later processing.
    /// </summary>
    public void Publish<T>(T message) where T : MessageBase
    {
        // if the message should keep the thread context, execute the message so it executes immediately
        if (message.KeepThreadContext)
        {
            ExecuteMessage(message);
        }
        // otherwise, enqueue the message for later processing (potentially in a different thread)
        else
        {
            _messageQueue.Enqueue(message);
        }
    }

    /// <summary>
    /// The required startAsync method by the mediator-base
    /// <para>Begins processing the message queue in a loop</para>
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // start the loop in async
        _ = Task.Run(async () =>
        {
            // while the cancellation token is not cancelled
            while (!_loopCts.Token.IsCancellationRequested)
            {
                // while we should not be processing the queue, delay for 100ms then try again, and keep doing this until we should process the queue
                while (!_processQueue)
                {
                    await Task.Delay(100, _loopCts.Token).ConfigureAwait(false);
                }

                // await 100 ms before processing the queue
                await Task.Delay(100, _loopCts.Token).ConfigureAwait(false);

                // create a hashset of processed messages
                HashSet<MessageBase> processedMessages = [];
                // while the message queue tries to dequeue a message, execute the message
                while (_messageQueue.TryDequeue(out var message))
                {
                    // if the message is already processed, continue to the next message
                    if (processedMessages.Contains(message)) { continue; }
                    // otherwise, add the message to the processed messages hashset
                    processedMessages.Add(message);

                    // then execute the message
                    ExecuteMessage(message);
                }
            }
        });

        _logger.LogInformation("Started Gagspeak Mediator");

        return Task.CompletedTask;
    }

    /// <summary>
    /// An overhead from the stopasync in the base, this is called instead,
    /// ensuring that the messagequeue is cleared and the cancelation token is called, returning a completed task.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _messageQueue.Clear();
        _loopCts.Cancel();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Method to subscribe to a message of type T. 
    /// Subscribed messages listen for the message and execute the action.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="subscriber"></param>
    /// <param name="action"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Subscribe<T>(IMediatorSubscriber subscriber, Action<T> action) where T : MessageBase
    {
        // lock the add remove lock so it becomes thread safe
        lock (_addRemoveLock)
        {
            // if the subscriber dictionary does not contain the type of T, add it to the subscriber dictionary
            _subscriberDict.TryAdd(typeof(T), []);

            // if we are already subscribed to this message, throw an exception
            if (!_subscriberDict[typeof(T)].Add(new(subscriber, action)))
            {
                throw new InvalidOperationException("Already subscribed");
            }

            // otherwise, we would have sucessfully added it to the dictionary, logging its sucess afterward
            _logger.LogDebug("Subscriber added for message "+typeof(T).Name+": "+subscriber.GetType().Name, LoggerType.Mediator);
        }
    }

    /// <summary>
    /// Method for unsubsribing from a message of type T. 
    /// This stops listening for the mediator publish actions and removes it from the subscriber dictionary.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="subscriber"></param>
    public void Unsubscribe<T>(IMediatorSubscriber subscriber) where T : MessageBase
    {
        // lock the add remove lock so it becomes thread safe
        lock (_addRemoveLock)
        {
            // if the subscriber dictionary contains the type of T, remove the subscriber from the dictionary
            if (_subscriberDict.ContainsKey(typeof(T)))
            {
                // remove the subscriber from the dictionary
                _subscriberDict[typeof(T)].RemoveWhere(p => p.Subscriber == subscriber);
            }
        }
    }

    /// <summary>
    /// Method to unsubscribe from all messages at once (for a specific subscriber).
    /// </summary>
    /// <param name="subscriber"></param>
    internal void UnsubscribeAll(IMediatorSubscriber subscriber)
    {
        // lock the add remove lock so it becomes thread safe
        lock (_addRemoveLock)
        {
            // for each key value pair in the subscriber dictionary, remove the subscriber from the dictionary
            foreach (Type kvp in _subscriberDict.Select(k => k.Key))
            {
                // remove the subscriber from the dictionary
                int unSubbed = _subscriberDict[kvp]?.RemoveWhere(p => p.Subscriber == subscriber) ?? 0;
                // if the subscriber was removed, log the sucess
                if (unSubbed > 0)
                {
                    _logger.LogDebug(subscriber.GetType().Name+" unsubscribed from "+kvp.Name, LoggerType.Mediator);
                }
            }
        }
    }

    /// <summary>
    /// Method that executes a message, calling the action for the respective message
    /// </summary>
    /// <param name="message"></param>
    private void ExecuteMessage(MessageBase message)
    {
        // if the subscriber dictionary does not contain the type of the message, return
        if (!_subscriberDict.TryGetValue(message.GetType(), out HashSet<SubscriberAction>? subscribers) || subscribers == null || !subscribers.Any()) return;

        // otherwise, get the subscribers and create a copy of the subscribers
        List<SubscriberAction> subscribersCopy = [];

        // lock the add remove lock so it becomes thread safe
        lock (_addRemoveLock)
        {
            // create a copy of the subscribers
            subscribersCopy = subscribers?.Where(s => s.Subscriber != null).ToList() ?? [];
        }

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        // get the type of the message
        var msgType = message.GetType();
        // if the generic execute methods does not contain the message type, get the method info for the message type
        if (!_genericExecuteMethods.TryGetValue(msgType, out var methodInfo))
        {
            // get the method info for the message type
            _genericExecuteMethods[msgType] = methodInfo = GetType()
                 .GetMethod(nameof(ExecuteReflected), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                 .MakeGenericMethod(msgType);
        }

        // now that we have it, we can invoke the subscribers actions
        methodInfo!.Invoke(this, [subscribersCopy, message]);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
    }

    /// <summary>
    /// Uses reflection to invoke subscriber actions with the correct message type. This method is called by ExecuteMessage().
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="subscribers"></param>
    /// <param name="message"></param>
    private void ExecuteReflected<T>(List<SubscriberAction> subscribers, T message) where T : MessageBase
    {
        foreach (SubscriberAction subscriber in subscribers)
        {
            try
            {
                ((Action<T>)subscriber.Action).Invoke(message);
            }
            catch (Exception ex)
            {
                if (_lastErrorTime.TryGetValue(subscriber, out var lastErrorTime) && lastErrorTime.Add(TimeSpan.FromSeconds(10)) > DateTime.UtcNow)
                    continue;

                _logger.LogError(ex.InnerException ?? ex, "Error executing {type} for subscriber {subscriber}",
                    message.GetType().Name, subscriber.Subscriber.GetType().Name);
                _lastErrorTime[subscriber] = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Starts the message queue processing.
    /// </summary>
    public void StartQueueProcessing()
    {
        _logger.LogInformation("Starting Message Queue Processing");
        _processQueue = true;
    }

    /// <summary>
    /// A sealed class that stores the Mediator subscriber, caching the action to be executed when the message is published.
    /// </summary>
    private sealed class SubscriberAction
    {
        // takes in a subscriber and an action
        public SubscriberAction(IMediatorSubscriber subscriber, object action)
        {
            // and stores it to the variables
            Subscriber = subscriber;
            Action = action;
        }

        // the action that should be executed, and the subscriber that should execute it
        public object Action { get; }
        public IMediatorSubscriber Subscriber { get; }
    }
}
