namespace GagSpeak.Services.Mediator;

#pragma warning disable MA0048
// the record structure for the base of a message, determining if it should keep thread context or not.
public abstract record MessageBase
{
    public virtual bool KeepThreadContext => false;
}

public record SameThreadMessage : MessageBase
{
    public override bool KeepThreadContext => true;
}
#pragma warning restore MA0048
