namespace OtterGui.Tasks;

public class SingleTaskQueue
{
    private readonly WeakReference<Task> _lastTask = new(null!);

    public Task Enqueue(IAction action, CancellationToken token = default)
    {
        lock (this)
        {
            var resultTask = _lastTask.TryGetTarget(out var lastTask) && !lastTask.IsCompleted
                ? lastTask.ContinueWith(_ => action.Execute(token), token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current)
                : Task.Run(() => action.Execute(token), token);

            _lastTask.SetTarget(resultTask);

            return resultTask;
        }
    }
}
