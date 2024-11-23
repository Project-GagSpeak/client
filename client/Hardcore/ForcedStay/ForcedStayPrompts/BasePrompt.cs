namespace GagSpeak.Hardcore.ForcedStay;
public class BasePrompt
{
    public virtual bool Enabled { get; protected set; }
    public virtual string Key => GetType().Name;

    public virtual void Enable()
    {
        Enabled = true;
    }

    public virtual void Disable()
    {
        Enabled = false;
    }
}
