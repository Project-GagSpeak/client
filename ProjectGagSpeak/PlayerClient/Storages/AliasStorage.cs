using GagSpeak.State.Models;
using MessagePack;

namespace GagSpeak.PlayerClient;

[MessagePackObject(keyAsPropertyName: true)]
public class AliasStorage : IEditableStorage<AliasTrigger>
{
    public List<AliasTrigger> Items { get; set; } = new();

    public AliasStorage()
    { }

    public AliasStorage(List<AliasTrigger> init)
        => Items = init;

    public bool TryApplyChanges(AliasTrigger oldItem, AliasTrigger changedItem)
    {
        if (changedItem is null)
            return false;
        
        oldItem.ApplyChanges(changedItem);
        return true;
    }
}
