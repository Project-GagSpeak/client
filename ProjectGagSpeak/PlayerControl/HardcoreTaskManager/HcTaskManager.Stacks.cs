using TerraFX.Interop.Windows;

namespace GagSpeak.PlayerControl;

/// <summary>
///     Hardcore Task Manager task stacking.
/// </summary>
public partial class HcTaskManager
{
    /// <summary> A temporary task stack used to allocate enqueued tasks so they can all be processed in sequence. </summary>
    private List<Func<bool?>> _tempStack { get; init; } = [];
    private string _tempStackName = string.Empty;
    private HcTaskConfiguration _tempStackConfig = new HcTaskConfiguration();

    public void BeginStack(string name, HcTaskConfiguration stackConfig)
    {
        // Reject if a stack is already active.
        if (_tempStack.Count > 0)
        {
            _logger.LogWarning("A task stack already active. Clearing anyways, but this should never happen.");
        }
        _logger.LogDebug("HcTaskOperation Stack Beginning.", LoggerType.HardcoreTasks);
        
        _tempStack.Clear();
        _tempStackName = name;
        _tempStackConfig = stackConfig;
    }

    public void AddToStack(Func<bool?> func)
        => _tempStack.Add(func);

    public void AddToStack(Func<bool> func)
        => _tempStack.Add(() => func());

    public void AddToStack(Action action)
        => _tempStack.Add(() => { action(); return true; });

    /// <summary>
    ///     Enqueues the current stack into the end of the _tasks list.
    /// </summary>
    public void EnqueueStack()
    {
        _logger.LogDebug($"Enqueuing HcTaskOperationStack {_tempStackName} with {_tempStack.Count} tasks.", LoggerType.HardcoreTasks);
        EnqueueTaskOperation(new(_tempStack, _tempStackName, _tempStackConfig));

        _tempStack.Clear();
        _tempStackName = string.Empty;
        _tempStackConfig = new HcTaskConfiguration();
    }

    /// <summary>
    ///     Inserts the current stack into the beginning of the _tasks list.
    /// </summary>
    public void InsertStack()
    {
        _logger.LogDebug($"Inserting HcTaskOperationStack {_tempStackName} with {_tempStack.Count} tasks.", LoggerType.HardcoreTasks);
        InsertTaskOperation(new(_tempStack, _tempStackName, _tempStackConfig));
        _tempStack.Clear();
        _tempStackName = string.Empty;
        _tempStackConfig = new HcTaskConfiguration();
    }

    public void DiscardStack()
    {
        _logger.LogDebug($"Discarding Hardcore Task Stack with {_tempStack.Count}.", LoggerType.HardcoreTasks);
        _tempStack.Clear();
        _tempStackName = string.Empty;
        _tempStackConfig = new HcTaskConfiguration();
    }

    /// <summary>
    ///     Primarily used for preset insertions that build a stack inside of a function and want it to be executed immidiately.
    /// </summary>
    public void EnqueueStack(string name, HcTaskConfiguration config, Action executeAct)
    {
        BeginStack(name, config);
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
    public void InsertStack(string name, HcTaskConfiguration config, Action executeAct)
    {
        BeginStack(name, config);
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
