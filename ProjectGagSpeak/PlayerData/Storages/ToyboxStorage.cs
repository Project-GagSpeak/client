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

    public IEnumerable<SpellActionTrigger> SpellAction => this.OfType<SpellActionTrigger>().Where(x => x.Enabled);
    public IEnumerable<HealthPercentTrigger> HealthPercent => this.OfType<HealthPercentTrigger>().Where(x => x.Enabled);
    public IEnumerable<RestraintTrigger> RestraintState => this.OfType<RestraintTrigger>().Where(x => x.Enabled);
    public IEnumerable<RestrictionTrigger> RestrictionState => this.OfType<RestrictionTrigger>().Where(x => x.Enabled);
    public IEnumerable<GagTrigger> GagState => this.OfType<GagTrigger>().Where(x => x.Enabled);
    public IEnumerable<SocialTrigger> Social => this.OfType<SocialTrigger>().Where(x => x.Enabled);
    public IEnumerable<EmoteTrigger> Emote => this.OfType<EmoteTrigger>().Where(x => x.Enabled);
}
