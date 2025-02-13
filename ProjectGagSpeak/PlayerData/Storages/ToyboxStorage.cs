using GagSpeak.PlayerState.Models;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerData.Storage;

public class PatternStorage : List<Pattern>
{
    public bool TryGetPattern(Guid id, [NotNullWhen(true)] out Pattern? item)
    {
        item = this.FirstOrDefault(x => x.Identifier == id);
        return item != null;
    }

    public Pattern? ByIdentifier(Guid id)
        => this.FirstOrDefault(x => x.Identifier == id);

    public bool Contains(Guid id)
        => ByIdentifier(id) != null;
}

public class AlarmStorage : List<Alarm>
{
    public bool TryGetAlarm(Guid id, [NotNullWhen(true)] out Alarm? item)
    {
        item = this.FirstOrDefault(x => x.Identifier == id);
        return item != null;
    }

    public Alarm? ByIdentifier(Guid id)
        => this.FirstOrDefault(x => x.Identifier == id);

    public bool Contains(Guid id)
        => ByIdentifier(id) != null;
}

public class TriggerStorage : List<Trigger>
{
    public bool TryGetTrigger(Guid id, [NotNullWhen(true)] out Trigger? item)
    {
        item = this.FirstOrDefault(x => x.Identifier == id);
        return item != null;
    }

    public Trigger? ByIdentifier(Guid id)
        => this.FirstOrDefault(x => x.Identifier == id);

    public bool Contains(Guid id)
        => ByIdentifier(id) != null;
}
