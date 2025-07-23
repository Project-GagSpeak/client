using GagspeakAPI.Data;

namespace GagSpeak.State.Models;

public abstract class Trigger : IEditableStorageItem<Trigger>
{
    public abstract TriggerKind Type { get; }

    public Guid Identifier { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = false;
    public int Priority { get; set; } = 0;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public InvokableActionType ActionType => InvokableAction.ActionType;
    public InvokableGsAction InvokableAction { get; set; } = new TextAction();

    public Trigger()
    { }

    public Trigger(Trigger other, bool keepId)
    {
        Identifier = keepId ? other.Identifier : Guid.NewGuid();
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

    public abstract Trigger Clone(bool keepId);

    public virtual void ApplyChanges(Trigger other)
    {
        Enabled = other.Enabled;
        Priority = other.Priority;
        Label = other.Label;
        Description = other.Description;
        InvokableAction = other.InvokableAction switch
        {
            SexToyAction sta => new SexToyAction(sta),
            PiShockAction ps => new PiShockAction(ps),
            MoodleAction ma => new MoodleAction(ma),
            RestraintAction ra => new RestraintAction(ra),
            RestrictionAction ra => new RestrictionAction(ra),
            GagAction ga => new GagAction(ga),
            TextAction ta => new TextAction(ta),
            _ => throw new NotImplementedException()
        };
    }

    public LightTrigger ToLightItem()
        => new LightTrigger(Identifier, Priority, Label, Description, Type, ActionType);
}


