namespace GagSpeak.CkCommons;

public static class Generic
{
    /// <summary> A helper method to ensure the action is executed safely. Exceptions are logged. </summary>
    /// <param name="act">the action to execute</param>
    public static void ExecuteSafely(Action act)
    {
        try
        {
            act();
        }
        catch (Exception ex)
        {
            Svc.Logger.Error("Error on executing safely:" + ex);
        }
    }

    public static void CancelDispose(this CancellationTokenSource? cts)
    {
        try
        {
            cts?.Cancel();
            cts?.Dispose();
        }
        catch (ObjectDisposedException) { }
    }

    public static CancellationTokenSource CancelRecreate(this CancellationTokenSource? cts)
    {
        cts?.CancelDispose();
        return new CancellationTokenSource();
    }
}

