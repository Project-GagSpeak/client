using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Interfaces;

namespace GagSpeak.PlayerState.Models;

public abstract record Trigger
{
    public abstract TriggerKind Type { get; }

    public Guid Identifier { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = false;
    public int Priority { get; set; } = 0;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public InvokableActionType ActionType => InvokableAction.ExecutionType;
    public InvokableGsAction InvokableAction { get; set; } = new TextAction();

    public Trigger() { }

    public Trigger(Trigger other, bool copyID)
    {
        Identifier = copyID ? other.Identifier : Guid.NewGuid();
        Enabled = other.Enabled;
        Priority = other.Priority;
        Label = other.Label;
        Description = other.Description;
        InvokableAction = other.InvokableAction switch
        {
            SexToyAction sta     => new SexToyAction(sta),
            PiShockAction ps     => new PiShockAction(ps),
            MoodleAction ma      => new MoodleAction(ma),
            RestraintAction ra   => new RestraintAction(ra),
            RestrictionAction ra => new RestrictionAction(ra),
            GagAction ga         => new GagAction(ga),
            TextAction ta        => new TextAction(ta),
            _ => throw new NotImplementedException()
        };
    }

    public LightTrigger ToLightData() 
        => new LightTrigger(Identifier, Priority, Label, Description, Type, ActionType);
}


