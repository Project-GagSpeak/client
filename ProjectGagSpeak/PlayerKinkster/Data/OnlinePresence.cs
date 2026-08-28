namespace Sundouleia.PlayerOther;

[Flags]
public enum OnlinePresence
{
    None = 0 << 0,
    Direct = 1 << 0,
    Sanction = 1 << 1,
    Radar = 1 << 2
}