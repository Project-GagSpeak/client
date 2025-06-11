namespace GagSpeak.GameInternals.Structs;
public struct ActionEffectEntry
{
    public uint SourceID { get; }
    public ulong TargetID { get; }

    // make this use a byte over ActionEffectType, so we fetch the type even if its
    // not one we care about, and only accept it if it is one we care about.
    public LimitedActionEffectType Type { get; }
    public uint ActionID { get; }
    public uint Damage { get; }

    public ActionEffectEntry(uint sourceID, ulong targetID, LimitedActionEffectType type, uint actionID, uint damage)
    {
        SourceID = sourceID;
        TargetID = targetID;
        Type = type;
        ActionID = actionID;
        Damage = damage;
    }
}
