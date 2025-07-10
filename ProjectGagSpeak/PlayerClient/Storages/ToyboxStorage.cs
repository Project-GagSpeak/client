using GagspeakAPI.Data;
using System.Diagnostics.CodeAnalysis;
using GagSpeak.State.Models;

namespace GagSpeak.PlayerClient;

public class BuzzToyStorage : ConcurrentDictionary<Guid, BuzzToy>, IEditableStorage<BuzzToy>
{
    public bool TryApplyChanges(BuzzToy oldItem, BuzzToy changedItem)
    {
        if (changedItem is null || changedItem is not BuzzToy)
            return false;

        oldItem.ApplyChanges(changedItem);
        return true;
    }
}

public class PatternStorage : List<Pattern>, IEditableStorage<Pattern>
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

    public bool TryApplyChanges(Pattern oldItem, Pattern changedItem)
    {
        if (changedItem is null)
            return false;
        oldItem.ApplyChanges(changedItem);
        return true;
    }
}

public class AlarmStorage : List<Alarm>, IEditableStorage<Alarm>
{
    /// <summary> C# Quirk Dev Note here: Modifying any properties from the fetched object WILL update them directly.
    /// <para> Modifying the object itself will not update the actual item in the list, and must be accessed by index. </para>
    /// </summary>
    public bool TryGetAlarm(Guid id, [NotNullWhen(true)] out Alarm? item)
    {
        item = this.FirstOrDefault(x => x.Identifier == id);
        return item != null;
    }

    /// <summary> A mix of FindIndex() and TryGetValue() through the item GUID </summary>
    /// <param name="id"> the RestraintSet GUID to find the index of in storage. </param>
    /// <param name="index"> the index of the item in the list (if found). </param>
    /// <returns> True if the index was found, false if it was not. </returns>
    /// <remarks> This should be used when updating the full object, and not just its properties. </remarks>
    public bool TryFindIndexById(Guid id, out int index)
        => (index = this.FindIndex(x => x.Identifier == id)) != -1;

    /// <summary> Informs us if the item is in the storage. </summary>
    public bool Contains(Guid id)
        => this.Any(x => x.Identifier == id);

    public bool TryApplyChanges(Alarm oldItem, Alarm changedItem)
    {
        if (changedItem is null)
            return false;

        oldItem.ApplyChanges(changedItem);
        return true;
    }
}

public class TriggerStorage : List<Trigger>, IEditableStorage<Trigger>
{
    /// <summary> C# Quirk Dev Note here: Modifying any properties from the fetched object WILL update them directly.
    /// <para> Modifying the object itself will not update the actual item in the list, and must be accessed by index. </para>
    /// </summary>
    public bool TryGetTrigger(Guid id, [NotNullWhen(true)] out Trigger? item)
        => (item = this.FirstOrDefault(x => x.Identifier == id)) != null;

    /// <summary> A mix of FindIndex() and TryGetValue() through the item GUID </summary>
    /// <param name="id"> the RestraintSet GUID to find the index of in storage. </param>
    /// <param name="index"> the index of the item in the list (if found). </param>
    /// <returns> True if the index was found, false if it was not. </returns>
    /// <remarks> This should be used when updating the full object, and not just its properties. </remarks>
    public bool TryFindIndexById(Guid id, out int index)
        => (index = this.FindIndex(x => x.Identifier == id)) != -1;

    /// <summary> Informs us if the item is in the storage. </summary>
    public bool Contains(Guid id)
        => this.Any(x => x.Identifier == id);

    public IEnumerable<SpellActionTrigger> SpellAction => this.OfType<SpellActionTrigger>().Where(x => x.Enabled);
    public IEnumerable<HealthPercentTrigger> HealthPercent => this.OfType<HealthPercentTrigger>().Where(x => x.Enabled);
    public IEnumerable<RestraintTrigger> RestraintState => this.OfType<RestraintTrigger>().Where(x => x.Enabled);
    public IEnumerable<RestrictionTrigger> RestrictionState => this.OfType<RestrictionTrigger>().Where(x => x.Enabled);
    public IEnumerable<GagTrigger> GagState => this.OfType<GagTrigger>().Where(x => x.Enabled);
    public IEnumerable<SocialTrigger> Social => this.OfType<SocialTrigger>().Where(x => x.Enabled);
    public IEnumerable<EmoteTrigger> Emote => this.OfType<EmoteTrigger>().Where(x => x.Enabled);

    public Trigger? ReplaceSource(Trigger oldItem, Trigger newItem)
    {
        if (oldItem is null || newItem is null)
            return null;

        int index = IndexOf(oldItem);
        if (index is -1)
            return null;

        // Update the object directly. (This will void any references to the old item)
        this[index] = newItem;

        // return the new item that is set.
        return this[index];
    }

    public bool TryApplyChanges(Trigger oldItem, Trigger changedItem)
    {
        if (changedItem is null)
            return false;

        oldItem.ApplyChanges(changedItem);
        return true;
    }
}
