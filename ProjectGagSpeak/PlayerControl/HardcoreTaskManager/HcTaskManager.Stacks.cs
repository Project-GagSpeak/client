namespace GagSpeak.PlayerControl;

/// <summary>
///     Hardcore Task Manager task stacking.
/// </summary>
public partial class HcTaskManager
{
    /// <summary> A temporary task stack used to allocate enqueued tasks so they can all be processed in sequence. </summary>
    private List<HardcoreTask> _tempStack { get; init; } = [];

    /// <summary> If we are currently processing a task stack. </summary>
    public bool IsStackActive { get; private set; } = false;

    // Begin Stack method.
    // currently only logs a warning when trying to begin a stack when one already exists and clears the previous.
    // in the future maybe make this return a bool based on if it was successful or not.
    public void BeginStack()
    {
        // Reject if a stack is already active.
        if (IsStackActive)
        {
            _logger.LogWarning("A task stack already active.");
        }
        _logger.LogDebug("Hardcore Task Stack Beginning.", LoggerType.HardcoreTasks);
        _tempStack.Clear();
        IsStackActive = false;
    }

    /// <summary>
    ///     Enqueues the current stack into the end of the _tasks list.
    /// </summary>
    public void EnqueueStack()
    {
        _logger.LogDebug($"Enqueuing Hardcore Task Stack with {_tempStack.Count}.", LoggerType.HardcoreTasks);
        IsStackActive = false;
        EnqueueMultipleTasks([.. _tempStack]);
        _tempStack.Clear();
    }

    /// <summary>
    ///     Inserts the current stack into the beginning of the _tasks list.
    /// </summary>
    public void InsertStack()
    {
        _logger.LogDebug($"Inserting Hardcore Task Stack with {_tempStack.Count}.", LoggerType.HardcoreTasks);
        IsStackActive = false;
        InsertMultipleTasks([.. _tempStack]);
        _tempStack.Clear();
    }

    public void DiscardStack()
    {
        _logger.LogDebug($"Discarding Hardcore Task Stack with {_tempStack.Count}.", LoggerType.HardcoreTasks);
        IsStackActive = false;
        _tempStack.Clear();
    }

    /// <summary>
    ///     Primarily used for preset insertions that build a stack inside of a function and want it to be executed immidiately.
    /// </summary>
    public void EnqueueStack(Action executeAct)
    {
        BeginStack();
        try
        {
            executeAct();
        }
        catch (Bagagwa ex)
        {
            _logger.LogError($"Error while executing stack action: {ex.Message}", LoggerType.HardcoreTasks);
            DiscardStack();
            return;
        }
        EnqueueStack();
    }

    public void InsertStack(Action executeAct)
    {
        BeginStack();
        try
        {
            executeAct();
        }
        catch (Bagagwa ex)
        {
            _logger.LogError($"Error while executing stack action: {ex.Message}", LoggerType.HardcoreTasks);
            DiscardStack();
            return;
        }
        InsertStack();
    }
}
