using OtterGui.Log;
using OtterGui.Services;

namespace OtterGui.Classes;

public static class EventWrapperBase
{
    internal static Logger? Logger;

    public static void ChangeLogger(Logger? logger)
        => Logger = logger;
}

public delegate        void ActionRef<T1>(ref T1 a);
public delegate        void ActionRef<T1, T2>(ref T1 a, ref T2 b);
public delegate        void ActionRef2<in T1, T2>(T1 a, ref T2 b);
public delegate        void ActionRef3<in T1, in T2, T3>(T1 a, T2 b, ref T3 c);
public delegate        void ActionRef34<in T1, in T2, T3, T4>(T1 a, T2 b, ref T3 c, ref T4 d);
public unsafe delegate void ActionPtr<T1>(T1* a) where T1 : unmanaged;
public unsafe delegate void ActionPtr<T1, T2>(T1* a, T2* b) where T1 : unmanaged where T2 : unmanaged;
public unsafe delegate void ActionPtr<T1, T2, T3>(T1* a, T2* b, T3* c) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged;
public unsafe delegate void ActionPtr1<T1, in T2>(T1* a, T2 b) where T1 : unmanaged;
public unsafe delegate void ActionPtr1<T1, in T2, in T3>(T1* a, T2 b, T3 c) where T1 : unmanaged;

public unsafe delegate void ActionPtr234<in T1, T2, T3, T4>(T1 a, T2* b, T3* c, T4* d)
    where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged;

public unsafe delegate void ActionPtr12Ref34<T1, T2, T3, T4>(T1* a, T2* b, ref T3 c, ref T4 d)
    where T1 : unmanaged where T2 : unmanaged;

public abstract class EventWrapperBase<TPriority>(string name) : IDisposable, IService
    where TPriority : struct, Enum
{
    public readonly    string                                        Name  = name;
    protected readonly List<(object Subscriber, TPriority Priority)> Event = [];
    protected readonly ReaderWriterLockSlim                          Lock  = new(LockRecursionPolicy.SupportsRecursion);

    public bool HasSubscribers
        => Event.Count > 0;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        Lock.EnterWriteLock();
        Event.Clear();
        Lock.ExitWriteLock();
    }

    protected virtual void Dispose(bool disposing)
    { }

    protected void Subscribe(object subscriber, TPriority priority)
    {
        Lock.EnterReadLock();
        var existingIdx = Event.FindIndex(p => ReferenceEquals(p.Subscriber, subscriber));
        var idx         = Event.FindIndex(p => p.Priority.CompareTo(priority) > 0);
        Lock.ExitReadLock();
        Lock.EnterWriteLock();
        if (idx == existingIdx)
        {
            if (idx < 0)
                Event.Add((subscriber, priority));
            else
                Event[idx] = (subscriber, priority);
        }
        else
        {
            if (idx < 0)
                Event.Add((subscriber, priority));
            else
                Event.Insert(idx, (subscriber, priority));

            if (existingIdx >= 0)
                Event.RemoveAt(existingIdx < idx ? existingIdx : existingIdx + 1);
        }

        Lock.ExitWriteLock();
    }

    protected void Unsubscribe(object subscriber)
    {
        Lock.EnterReadLock();
        var idx = Event.FindIndex(p => ReferenceEquals(p.Subscriber, subscriber));
        Lock.ExitReadLock();
        if (idx < 0)
            return;

        Lock.EnterWriteLock();
        Event.RemoveAt(idx);
        Lock.ExitWriteLock();
    }

    protected IEnumerable<T> Enumerate<T>() where T : Delegate
    {
        Lock.EnterReadLock();
        for (var i = Event.Count - 1; i >= 0; --i)
            yield return (T)Event[i].Subscriber;

        Lock.ExitReadLock();
    }
}

public abstract class EventWrapper<TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
{
    public void Subscribe(Action subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(Action subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke()
    {
        foreach (var action in Enumerate<Action>())
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract class EventWrapper<T1, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
{
    public void Subscribe(Action<T1> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(Action<T1> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1 a)
    {
        foreach (var action in Enumerate<Action<T1>>())
        {
            try
            {
                action.Invoke(a);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract class EventWrapper<T1, T2, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
{
    public void Subscribe(Action<T1, T2> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(Action<T1, T2> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1 a, T2 b)
    {
        foreach (var action in Enumerate<Action<T1, T2>>())
        {
            try
            {
                action.Invoke(a, b);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract class EventWrapper<T1, T2, T3, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
{
    public void Subscribe(Action<T1, T2, T3> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(Action<T1, T2, T3> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1 a, T2 b, T3 c)
    {
        foreach (var action in Enumerate<Action<T1, T2, T3>>())
        {
            try
            {
                action.Invoke(a, b, c);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract class EventWrapper<T1, T2, T3, T4, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
{
    public void Subscribe(Action<T1, T2, T3, T4> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(Action<T1, T2, T3, T4> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1 a, T2 b, T3 c, T4 d)
    {
        foreach (var action in Enumerate<Action<T1, T2, T3, T4>>())
        {
            try
            {
                action.Invoke(a, b, c, d);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract class EventWrapper<T1, T2, T3, T4, T5, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
{
    public void Subscribe(Action<T1, T2, T3, T4, T5> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(Action<T1, T2, T3, T4, T5> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1 a, T2 b, T3 c, T4 d, T5 e)
    {
        foreach (var action in Enumerate<Action<T1, T2, T3, T4, T5>>())
        {
            try
            {
                action.Invoke(a, b, c, d, e);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract class EventWrapper<T1, T2, T3, T4, T5, T6, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
{
    public void Subscribe(Action<T1, T2, T3, T4, T5, T6> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(Action<T1, T2, T3, T4, T5, T6> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f)
    {
        foreach (var action in Enumerate<Action<T1, T2, T3, T4, T5, T6>>())
        {
            try
            {
                action.Invoke(a, b, c, d, e, f);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract unsafe class EventWrapperRef2<T1, T2, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
{
    public void Subscribe(ActionRef2<T1, T2> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(ActionRef2<T1, T2> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1 a, ref T2 b)
    {
        foreach (var action in Enumerate<ActionRef2<T1, T2>>())
        {
            try
            {
                action.Invoke(a, ref b);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract unsafe class EventWrapperRef3<T1, T2, T3, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
{
    public void Subscribe(ActionRef3<T1, T2, T3> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(ActionRef3<T1, T2, T3> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1 a, T2 b, ref T3 c)
    {
        foreach (var action in Enumerate<ActionRef3<T1, T2, T3>>())
        {
            try
            {
                action.Invoke(a, b, ref c);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract unsafe class EventWrapperRef34<T1, T2, T3, T4, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
{
    public void Subscribe(ActionRef34<T1, T2, T3, T4> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(ActionRef34<T1, T2, T3, T4> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1 a, T2 b, ref T3 c, ref T4 d)
    {
        foreach (var action in Enumerate<ActionRef34<T1, T2, T3, T4>>())
        {
            try
            {
                action.Invoke(a, b, ref c, ref d);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract unsafe class EventWrapperPtr<T1, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
    where T1 : unmanaged
{
    public void Subscribe(ActionPtr<T1> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(ActionPtr<T1> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1* a)
    {
        foreach (var action in Enumerate<ActionPtr<T1>>())
        {
            try
            {
                action.Invoke(a);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract unsafe class EventWrapperPtr1<T1, T2, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
    where T1 : unmanaged
{
    public void Subscribe(ActionPtr1<T1, T2> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(ActionPtr1<T1, T2> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1* a, T2 b)
    {
        foreach (var action in Enumerate<ActionPtr1<T1, T2>>())
        {
            try
            {
                action.Invoke(a, b);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract unsafe class EventWrapperPtr1<T1, T2, T3, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
    where T1 : unmanaged
{
    public void Subscribe(ActionPtr1<T1, T2, T3> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(ActionPtr1<T1, T2, T3> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1* a, T2 b, T3 c)
    {
        foreach (var action in Enumerate<ActionPtr1<T1, T2, T3>>())
        {
            try
            {
                action.Invoke(a, b, c);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract unsafe class EventWrapperPtr<T1, T2, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
    where T1 : unmanaged
    where T2 : unmanaged
{
    public void Subscribe(ActionPtr<T1, T2> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(ActionPtr<T1, T2> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1* a, T2* b)
    {
        foreach (var action in Enumerate<ActionPtr<T1, T2>>())
        {
            try
            {
                action.Invoke(a, b);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract unsafe class EventWrapperPtr<T1, T2, T3, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
    where T1 : unmanaged
    where T2 : unmanaged
    where T3 : unmanaged
{
    public void Subscribe(ActionPtr<T1, T2, T3> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(ActionPtr<T1, T2, T3> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1* a, T2* b, T3* c)
    {
        foreach (var action in Enumerate<ActionPtr<T1, T2, T3>>())
        {
            try
            {
                action.Invoke(a, b, c);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract unsafe class EventWrapperPtr234<T1, T2, T3, T4, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
    where T2 : unmanaged
    where T3 : unmanaged
    where T4 : unmanaged
{
    public void Subscribe(ActionPtr234<T1, T2, T3, T4> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(ActionPtr234<T1, T2, T3, T4> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1 a, T2* b, T3* c, T4* d)
    {
        foreach (var action in Enumerate<ActionPtr234<T1, T2, T3, T4>>())
        {
            try
            {
                action.Invoke(a, b, c, d);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}

public abstract unsafe class EventWrapperPtr12Ref34<T1, T2, T3, T4, TPriority>(string name) : EventWrapperBase<TPriority>(name)
    where TPriority : struct, Enum
    where T1 : unmanaged
    where T2 : unmanaged
{
    public void Subscribe(ActionPtr12Ref34<T1, T2, T3, T4> subscriber, TPriority priority)
        => base.Subscribe(subscriber, priority);

    public void Unsubscribe(ActionPtr12Ref34<T1, T2, T3, T4> subscriber)
        => base.Unsubscribe(subscriber);

    public void Invoke(T1* a, T2* b, ref T3 c, ref T4 d)
    {
        foreach (var action in Enumerate<ActionPtr12Ref34<T1, T2, T3, T4>>())
        {
            try
            {
                action.Invoke(a, b, ref c, ref d);
            }
            catch (Exception ex)
            {
                EventWrapperBase.Logger?.Error($"[{Name}] Exception thrown during invocation:\n{ex}");
            }
        }
    }
}
