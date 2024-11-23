namespace OtterGui.Tasks;

public enum ActionState
{
    NotQueued,
    NotStarted,
    Cancelled,
    Running,
    Failed,
    Succeeded,
}

public interface IAction : IEquatable<IAction>
{
    public void Execute(CancellationToken token);
}
