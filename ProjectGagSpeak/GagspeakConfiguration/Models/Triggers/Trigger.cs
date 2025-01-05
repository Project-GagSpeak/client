using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;

namespace GagSpeak.GagspeakConfiguration.Models;

[Serializable]
public abstract record Trigger : IExecutableAction
{
    // Define which kind of trigger it is
    public abstract TriggerKind Type { get; }

    // required attributes
    public Guid Identifier { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = false;
    public int Priority { get; set; } = 0;

    // generic attributes
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ActionExecutionType ExecutionType => ExecutableAction.ExecutionType;

    public IActionGS ExecutableAction { get; set; } = new MoodleAction();

    public LightTrigger ToLightData()
    {
        return new LightTrigger
        {
            Identifier = Identifier,
            Priority = Priority,
            Name = Name,
            Description = Description,
            Type = Type,
            ActionOnTrigger = GetTypeName()
        };
    }

    public ActionExecutionType GetTypeName() => ExecutableAction.ExecutionType;

    public abstract Trigger DeepClone();
}


