using GagSpeak.PlayerClient;
using GagspeakAPI.Data;

namespace GagSpeak.State.Models;

public class AliasTrigger : IEditableStorageItem<AliasTrigger>
{
    /// <summary>
    ///     Unique identifier for the trigger.
    /// </summary>
    public Guid Identifier { get; set; } = Guid.NewGuid();

    /// <summary>
    ///     Whether the trigger is enabled or not.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    ///     The label for the trigger.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     If the trigger should ignore case sensativity or respect it.
    /// </summary>
    public bool IgnoreCase { get; set; } = false;

    /// <summary>
    ///     The input command that triggers the output command
    /// </summary>
    public string InputCommand { get; set; } = string.Empty;

    /// <summary>
    ///     Stores Actions with unique types.
    /// </summary>
    public HashSet<InvokableGsAction> Actions { get; set; } = new HashSet<InvokableGsAction>();
   
    /// <summary>
    ///     The Kinksters allowed to view and use this Alias.
    /// </summary>
    public HashSet<string> WhitelistedUIDs { get; set; } = new HashSet<string>();
    
    public AliasTrigger() 
    { }

    public AliasTrigger(AliasTrigger other, bool keepId)
    {
        Identifier = keepId ? other.Identifier : Guid.NewGuid();
        ApplyChanges(other);
    }

    public AliasTrigger(GagspeakAlias dto)
    {
        Identifier = dto.Identifier;
        Enabled = dto.Enabled;
        Label = dto.Label;
        IgnoreCase = dto.IgnoreCase;
        InputCommand = dto.InputCommand;
        Actions = dto.Actions.ToHashSet();
        WhitelistedUIDs = dto.WhitelistedUIDs.ToHashSet();
    }

    public AliasTrigger Clone(bool keepId = false)
        => new AliasTrigger(this, keepId);

    public void ApplyChanges(AliasTrigger changedItem)
    {
        Enabled = changedItem.Enabled;
        Label = changedItem.Label;
        IgnoreCase = changedItem.IgnoreCase;
        InputCommand = changedItem.InputCommand;
        Actions = changedItem.Actions.ToHashSet();
        WhitelistedUIDs = changedItem.WhitelistedUIDs.ToHashSet();
    }

    public void ApplyChanges(GagspeakAlias dto)
    {
        Enabled = dto.Enabled;
        Label = dto.Label;
        IgnoreCase = dto.IgnoreCase;
        InputCommand = dto.InputCommand;
        Actions = dto.Actions.ToHashSet();
        WhitelistedUIDs = dto.WhitelistedUIDs.ToHashSet();
    }

    public bool CanView(string uid)
        => WhitelistedUIDs.Count is 0 || WhitelistedUIDs.Contains(uid);

    public bool ValidAlias()
        => !string.IsNullOrWhiteSpace(Label)
        && !string.IsNullOrWhiteSpace(InputCommand)
        && Actions.Count > 0
        && Actions.All(a => a.IsValid());

    public GagspeakAlias ToDto()
        => new GagspeakAlias
        {
            Identifier = Identifier,
            Enabled = Enabled,
            Label = Label,
            IgnoreCase = IgnoreCase,
            InputCommand = InputCommand,
            Actions = Actions,
            WhitelistedUIDs = WhitelistedUIDs
        };
}
