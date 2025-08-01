namespace GagSpeak;

#nullable disable
public class GagspeakEventManager
{
    private readonly ILoggerFactory _loggerFactory;
    public static ILogger UnlocksLogger { get; set; }

    public GagspeakEventManager(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        // assign the logger.
        UnlocksLogger = _loggerFactory.CreateLogger("Achievements");
    }

    private static Dictionary<UnlocksEvent, Delegate> EventDictionary = new Dictionary<UnlocksEvent, Delegate>();

    // Subscribe with no parameters
    public void Subscribe(UnlocksEvent eventName, Action listener)
    {
        if (!EventDictionary.ContainsKey(eventName))
        {
            EventDictionary[eventName] = null;
        }
        EventDictionary[eventName] = (Action)EventDictionary[eventName] + listener;
    }

    // Subscribe with one parameter
    public void Subscribe<T>(UnlocksEvent eventName, Action<T> listener)
    {
        if (!EventDictionary.ContainsKey(eventName))
        {
            EventDictionary[eventName] = null;
        }
        EventDictionary[eventName] = (Action<T>)EventDictionary[eventName] + listener;
    }

    // Subscribe with two parameters
    public void Subscribe<T1, T2>(UnlocksEvent eventName, Action<T1, T2> listener)
    {
        if (!EventDictionary.ContainsKey(eventName))
        {
            EventDictionary[eventName] = null;
        }
        EventDictionary[eventName] = (Action<T1, T2>)EventDictionary[eventName] + listener;
    }

    // Subscribe with three parameters
    public void Subscribe<T1, T2, T3>(UnlocksEvent eventName, Action<T1, T2, T3> listener)
    {
        if (!EventDictionary.ContainsKey(eventName))
        {
            EventDictionary[eventName] = null;
        }
        EventDictionary[eventName] = (Action<T1, T2, T3>)EventDictionary[eventName] + listener;
    }

    // Subscribe with four parameters
    public void Subscribe<T1, T2, T3, T4>(UnlocksEvent eventName, Action<T1, T2, T3, T4> listener)
    {
        if (!EventDictionary.ContainsKey(eventName))
        {
            EventDictionary[eventName] = null;
        }
        EventDictionary[eventName] = (Action<T1, T2, T3, T4>)EventDictionary[eventName] + listener;
    }

    // subscribe with 5 parameters
    public void Subscribe<T1, T2, T3, T4, T5>(UnlocksEvent eventName, Action<T1, T2, T3, T4, T5> listener)
    {
        if (!EventDictionary.ContainsKey(eventName))
        {
            EventDictionary[eventName] = null;
        }
        EventDictionary[eventName] = (Action<T1, T2, T3, T4, T5>)EventDictionary[eventName] + listener;
    }

    // Unsubscribe with no parameters
    public void Unsubscribe(UnlocksEvent eventName, Action listener)
    {
        if (EventDictionary.TryGetValue(eventName, out var existingDelegate))
        {
            var currentListeners = (Action)existingDelegate;
            currentListeners -= listener;
            if (currentListeners == null)
            {
                EventDictionary.Remove(eventName);
            }
            else
            {
                EventDictionary[eventName] = currentListeners;
            }
        }
    }

    // Unsubscribe with one parameter
    public void Unsubscribe<T>(UnlocksEvent eventName, Action<T> listener)
    {
        if (EventDictionary.TryGetValue(eventName, out var existingDelegate))
        {
            var currentListeners = (Action<T>)existingDelegate;
            currentListeners -= listener;
            if (currentListeners == null)
            {
                EventDictionary.Remove(eventName);
            }
            else
            {
                EventDictionary[eventName] = currentListeners;
            }
        }
    }

    // Unsubscribe with two parameters
    public void Unsubscribe<T1, T2>(UnlocksEvent eventName, Action<T1, T2> listener)
    {
        if (EventDictionary.TryGetValue(eventName, out var existingDelegate))
        {
            var currentListeners = (Action<T1, T2>)existingDelegate;
            currentListeners -= listener;
            if (currentListeners == null)
            {
                EventDictionary.Remove(eventName);
            }
            else
            {
                EventDictionary[eventName] = currentListeners;
            }
        }
    }

    // Unsubscribe with three parameters
    public void Unsubscribe<T1, T2, T3>(UnlocksEvent eventName, Action<T1, T2, T3> listener)
    {
        if (EventDictionary.TryGetValue(eventName, out var existingDelegate))
        {
            var currentListeners = (Action<T1, T2, T3>)existingDelegate;
            currentListeners -= listener;
            if (currentListeners == null)
            {
                EventDictionary.Remove(eventName);
            }
            else
            {
                EventDictionary[eventName] = currentListeners;
            }
        }
    }

    // Unsubscribe with four parameters
    public void Unsubscribe<T1, T2, T3, T4>(UnlocksEvent eventName, Action<T1, T2, T3, T4> listener)
    {
        if (EventDictionary.TryGetValue(eventName, out var existingDelegate))
        {
            var currentListeners = (Action<T1, T2, T3, T4>)existingDelegate;
            currentListeners -= listener;
            if (currentListeners == null)
            {
                EventDictionary.Remove(eventName);
            }
            else
            {
                EventDictionary[eventName] = currentListeners;
            }
        }
    }

    // Unsubscribe with five parameters
    public void Unsubscribe<T1, T2, T3, T4, T5>(UnlocksEvent eventName, Action<T1, T2, T3, T4, T5> listener)
    {
        if (EventDictionary.TryGetValue(eventName, out var existingDelegate))
        {
            var currentListeners = (Action<T1, T2, T3, T4, T5>)existingDelegate;
            currentListeners -= listener;
            if (currentListeners == null)
            {
                EventDictionary.Remove(eventName);
            }
            else
            {
                EventDictionary[eventName] = currentListeners;
            }
        }
    }

    // Trigger event with no parameter
    public static void AchievementEvent(UnlocksEvent eventName)
    {
        if (EventDictionary.TryGetValue(eventName, out var action))
        {
            try
            {
                UnlocksLogger.LogDebug($"EventFired: [({eventName})", LoggerType.AchievementEvents);
                if (action is Action eventHandler)
                {
                    eventHandler.Invoke();
                }
                else
                {
                    UnlocksLogger.LogError($"Invalid action type for event: {eventName}", LoggerType.AchievementEvents);
                }
            }
            catch (Bagagwa ex)
            {
                UnlocksLogger.LogError("Error in AchievementEvent: " + eventName, ex, LoggerType.AchievementEvents);
            }
        }
    }

    // Trigger event with one parameter
    public static void AchievementEvent<T>(UnlocksEvent eventName, T param)
    {
        if (EventDictionary.TryGetValue(eventName, out var action))
        {
            try
            {
                UnlocksLogger.LogDebug($"EventFired: [({eventName}) >> ({param})", LoggerType.AchievementEvents);
                if (action is Action<T> eventHandler)
                {
                    eventHandler.Invoke(param);
                }
                else
                {
                    UnlocksLogger.LogError($"Invalid action type for event: {eventName}", LoggerType.AchievementEvents);
                }
            }
            catch (Bagagwa ex)
            {
                UnlocksLogger.LogError("Error in AchievementEvent: " + eventName, ex, LoggerType.AchievementEvents);
            }
        }
    }

    // Trigger event with two parameters
    public static void AchievementEvent<T1, T2>(UnlocksEvent eventName, T1 param1, T2 param2)
    {
        if (EventDictionary.TryGetValue(eventName, out var action))
        {
            try
            {
                UnlocksLogger.LogDebug($"EventFired: [({eventName}) >> ({param1}) ({param2})", LoggerType.AchievementEvents);
                if (action is Action<T1, T2> eventHandler)
                {
                    eventHandler.Invoke(param1, param2);
                }
                else
                {
                    UnlocksLogger.LogError($"Invalid action type for event: {eventName}", LoggerType.AchievementEvents);
                }
            }
            catch (Bagagwa ex)
            {
                UnlocksLogger.LogError("Error in AchievementEvent: " + eventName, ex, LoggerType.AchievementEvents);
            }
        }
    }

    // Trigger event with three parameters
    public static void AchievementEvent<T1, T2, T3>(UnlocksEvent eventName, T1 param1, T2 param2, T3 param3)
    {
        if (EventDictionary.TryGetValue(eventName, out var action))
        {
            try
            {
                UnlocksLogger.LogDebug($"EventFired: [({eventName}) >> ({param1}) ({param2}) ({param3})", LoggerType.AchievementEvents);
                if (action is Action<T1, T2, T3> eventHandler)
                {
                    eventHandler.Invoke(param1, param2, param3);
                }
                else
                {
                    UnlocksLogger.LogError($"Invalid action type for event: {eventName}", LoggerType.AchievementEvents);
                }
            }
            catch (Bagagwa ex)
            {
                UnlocksLogger.LogError("Error in AchievementEvent: " + eventName, ex, LoggerType.AchievementEvents);
            }
        }
    }

    // Trigger event with four parameters
    public static void AchievementEvent<T1, T2, T3, T4>(UnlocksEvent eventName, T1 param1, T2 param2, T3 param3, T4 param4)
    {
        if (EventDictionary.TryGetValue(eventName, out var action))
        {
            try
            {
                UnlocksLogger.LogDebug($"EventFired: [({eventName}) >> ({param1}) ({param2}) ({param3}) ({param4})", LoggerType.AchievementEvents);
                if (action is Action<T1, T2, T3, T4> eventHandler)
                {
                    eventHandler.Invoke(param1, param2, param3, param4);
                }
                else
                {
                    UnlocksLogger.LogError($"Invalid action type for event: {eventName}", LoggerType.AchievementEvents);
                }
            }
            catch (Bagagwa ex)
            {
                UnlocksLogger.LogError("Error in AchievementEvent: " + eventName, ex, LoggerType.AchievementEvents);
            }
        }
    }

    // Trigger event with five parameters
    public static void AchievementEvent<T1, T2, T3, T4, T5>(UnlocksEvent eventName, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
    {
        if (EventDictionary.TryGetValue(eventName, out var action))
        {
            try
            {
                UnlocksLogger.LogDebug($"EventFired: [({eventName}) >> ({param1}) ({param2}) ({param3}) ({param4}) ({param5})", LoggerType.AchievementEvents);
                if (action is Action<T1, T2, T3, T4, T5> eventHandler)
                {
                    eventHandler.Invoke(param1, param2, param3, param4, param5);
                }
                else
                {
                    UnlocksLogger.LogError($"Invalid action type for event: {eventName}", LoggerType.AchievementEvents);
                }
            }
            catch (Bagagwa ex)
            {
                UnlocksLogger.LogError("Error in AchievementEvent: " + eventName, ex, LoggerType.AchievementEvents);
            }
        }
    }
}
