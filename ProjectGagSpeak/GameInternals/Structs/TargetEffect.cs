namespace GagSpeak.GameInternals.Structs;

public unsafe struct TargetEffect
{
    private readonly EffectEntry* _effects;

    public ulong TargetID { get; }

    public TargetEffect(ulong targetId, EffectEntry* effects)
    {
        TargetID = targetId;
        _effects = effects;
    }

    public EffectEntry this[int index]
    {
        get
        {
            if (index < 0 || index > 7) return default;
            return _effects[index];
        }
    }

    public void ForEach(Action<EffectEntry> act)
    {
        if (act == null) return;
        for (var i = 0; i < 8; i++)
        {
            var e = this[i];
            act(e);
        }
    }
}
