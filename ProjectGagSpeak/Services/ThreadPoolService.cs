namespace GagSpeak.Services;

/// <summary>
///   ThreadPool from ECommons, used as a placeholder while 
///   Emoji management gets more optimized and we no longer need it.
/// </summary>
public class ThreadPoolService : IDisposable
{
    private ILogger<ThreadPoolService> _logger;

    private ConcurrentQueue<(Action Action, Action<Exception?>? OnCompletion)> TaskQueue = new();

    private readonly int  MaxThreads = Math.Clamp(Environment.ProcessorCount / 3, 1, 8);
    private volatile uint ThreadNum;
    private volatile uint BusyThreads;
    private volatile bool Disposed;
    public ThreadPoolService(ILogger<ThreadPoolService> logger)
    {
        _logger = logger;
    }

    public bool IsWorking => BusyThreads != 0;
    public (uint RunningThreads, uint BusyThreads, int TasksQueued) State => (RunningThreads: ThreadNum, BusyThreads: BusyThreads, TasksQueued: TaskQueue.Count);

    public void Dispose()
    {
        Disposed = true;
    }

    public void Run(Action task, Action<Exception?>? onCompletion = null)
    {
        TaskQueue.Enqueue((task, onCompletion));
        long num = Math.Max(1L, Math.Min(MaxThreads, TaskQueue.Count + BusyThreads));
        if (ThreadNum < num)
        {
            _logger.LogTrace($"{ThreadNum} threads running, {BusyThreads} are busy, requested {num} threads, Creating new thread to deal with tasks...");
            ThreadNum++;
            new Thread(ThreadRun).Start();
        }
    }

    private void ThreadRun()
    {
        string text = $"{Random.Shared.Next():X8}";
        _logger.LogTrace($"Beginning Thread {text}!");
        int num = 0;
        while (!Disposed)
        {
            if (TaskQueue.TryDequeue(out (Action, Action<Exception?>?) result))
            {
                BusyThreads++;
                num = 0;
                Exception obj = null!;
                try
                {
                    result.Item1();
                }
                catch (Exception ex)
                {
                    if (result.Item2 == null)
                        _logger.LogError(ex, $"Exception in thread {text} with no error handler!");
                    else
                        obj = ex;
                }

                if (result.Item2 != null)
                {
                    try
                    {
                        result.Item2(obj);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Exception in thread {text} while running error handler!");
                    }
                }

                BusyThreads--;
            }
            else
            {
                num++;
                Thread.Sleep(100);
                if (num > 100 || Disposed)
                {
                    ThreadNum--;
                    break;
                }
            }
        }

        _logger.LogTrace($"Thread {text} is ending!");
    }
}

