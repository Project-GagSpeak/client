using Dalamud.Interface.ImGuiNotification;

namespace OtterGui.Classes;

public abstract class DelayedReferenceLoader<TObj, TData>(MessageService messager)
    where TObj : notnull
{
    protected readonly MessageService Messager = messager;

    private readonly ConcurrentQueue<(TObj Parent, TData Data)> _data = [];

    protected abstract bool TryGetObject(TData identity, [NotNullWhen(true)] out TObj? obj);

    protected abstract bool SetObject(TObj parent, TObj child, TData data, out string error);

    public virtual void SetAllObjects()
    {
        while (_data.TryDequeue(out var tuple))
        {
            if (TryGetObject(tuple.Data, out var child))
            {
                if (!SetObject(tuple.Parent, child, tuple.Data, out var error))
                    HandleChildNotSet(tuple.Parent, child, error);
            }
            else
            {
                HandleChildNotFound(tuple.Parent, tuple.Data);
            }
        }
    }

    public void AddObject(TObj parent, in TData data)
    {
        if (!TryGetObject(data, out var childObject) || !SetObject(parent, childObject, data, out _))
            _data.Enqueue((parent, data));
    }

    protected virtual void HandleChildNotFound(TObj parent, TData data)
        => Messager.AddMessage(new Notification($"Could not find the object corresponding to the identifier {data} for {parent}.",
            NotificationType.Warning));

    protected virtual void HandleChildNotSet(TObj parent, TObj child, string error)
        => Messager.AddMessage(new Notification($"Could not add the child {child} to {parent}: {error}", NotificationType.Warning));
}