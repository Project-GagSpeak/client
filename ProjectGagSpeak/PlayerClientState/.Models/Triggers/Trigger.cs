using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Interfaces;

namespace GagSpeak.PlayerState.Models;

[Serializable]
public abstract record Trigger : IExecutableAction
{
    public abstract TriggerKind Type { get; }

    public Guid Identifier { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = false;
    public int Priority { get; set; } = 0;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ActionExecutionType ExecutionType => ExecutableAction.ExecutionType;

    /// <summary> Fancy hecking interface action that can be sent across to others through special message pack handling. </summary>
    public IActionGS ExecutableAction { get; set; } = new MoodleAction();

    public LightTrigger ToLightData() => new LightTrigger(Identifier, Priority, Label, Description, Type, GetTypeName());

    public ActionExecutionType GetTypeName() => ExecutableAction.ExecutionType;

    public abstract Trigger DeepClone();
}


