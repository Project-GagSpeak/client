namespace GagSpeak.UI.Simulation;

// just throwing ideas around
public class StruggleStamina
{
    public int MaxStamina { get; set; } = 100;
    public int CurrentStamina { get; set; } = 100;
    public int RegenerationRate { get; set; } = 1; // 1 point every 2 minutes
    private DateTime lastRegenTime;

    public StruggleStamina()
    {
        lastRegenTime = DateTime.Now;
    }

    public void RegenerateStamina()
    {
        if ((DateTime.Now - lastRegenTime).TotalSeconds >= 2 && CurrentStamina < MaxStamina)
        {
            CurrentStamina = Math.Min(MaxStamina, CurrentStamina + RegenerationRate);
            lastRegenTime = DateTime.Now;
        }
    }

    public void UseStamina(int amount)
    {
        CurrentStamina -= amount;
        if (CurrentStamina < 0) CurrentStamina = 0;
    }

    public bool HasStamina(int amount)
    {
        return CurrentStamina >= amount;
    }
}

public class StruggleItem
{
    public float Tightness { get; set; } = .3f;  // 1.0 is normal, higher values mean tighter
    public float Wear { get; set; } = 0.0f;       // 0 is brand new, increases with effort
    public float WearThreshold { get; set; } = 1.0f; // Max wear before item breaks down

    public bool IsEscaped => Wear >= WearThreshold;

    public void IncreaseWear(float effort)
    {
        Wear += effort;
    }
}

public class ProgressBar
{
    public float Progress { get; set; } = 0.0f;  // 0 is empty, 1.0 is full
    private readonly float drainRate; // How fast the progress drains
    private readonly StruggleItem item; // The item that the progress bar is associated with

    public ProgressBar(StruggleItem item)
    {
        this.item = item;
        this.drainRate = 0.01f; // Base drain rate
    }

    public void Drain()
    {
        Progress -= drainRate * item.Tightness;  // Drains faster if the item is tighter
        if (Progress < 0) Progress = 0;
    }

    public void IncreaseProgress(float amount)
    {
        Progress += amount;
        if (Progress > 1.0f) Progress = 1.0f;
    }

    public bool IsFilled => Progress >= 1.0f;
}
