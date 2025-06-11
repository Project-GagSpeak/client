namespace GagSpeak.GameInternals.Structs;
public struct EffectEntry
{
    public byte type;
    public byte param0;
    public byte param1;
    public byte param2;
    public byte mult;
    public byte flags;
    public ushort value;

    public byte AttackType => (byte)(param1 & 0xF);

    public uint Damage => mult == 0 ? value : value + ((uint)ushort.MaxValue + 1) * mult;

    public bool TryGetActionEffectType(out LimitedActionEffectType effectType)
    {
        if (Enum.IsDefined(typeof(LimitedActionEffectType), type))
        {
            effectType = (LimitedActionEffectType)type;
            return true;
        }
        effectType = default;
        return false;
    }

    public override string ToString()
    {
        return
            $"Type: {type}, p0: {param0:D3}, p1: {param1:D3}, p2: {param2:D3} 0x{param2:X2} '{Convert.ToString(param2, 2).PadLeft(8, '0')}', " +
            $"mult: {mult:D3}, flags: {flags:D3} | {Convert.ToString(flags, 2).PadLeft(8, '0')}, value: {value:D6} ATTACK TYPE: {AttackType}";
    }
}
