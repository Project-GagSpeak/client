using Dalamud.Game.ClientState.Objects.SubKinds;
using GagSpeak.Utils;

namespace GagSpeak.UpdateMonitoring.Triggers;
public sealed class MonitoredPlayerState
{
    private readonly IPlayerCharacter TrackedPlayerObj;
    public string PlayerNameWithWorld => TrackedPlayerObj.GetNameWithWorld();
    public MonitoredPlayerState(IPlayerCharacter player)
    {
        TrackedPlayerObj = player;
        PreviousHp = player.CurrentHp;
        PreviousMaxHp = player.MaxHp;
    }

    public bool IsValid => TrackedPlayerObj != null && TrackedPlayerObj.IsValid();
    public bool IsDead => TrackedPlayerObj.IsDead;
    public uint PreviousHp { get; private set; }
    public uint PreviousMaxHp { get; private set; }
    public uint CurrentHp => TrackedPlayerObj.CurrentHp;
    public uint MaxHp => TrackedPlayerObj.MaxHp;

    public bool HasHpChanged()
    {
        if (!IsValid) return false;

        return PreviousHp != CurrentHp || PreviousMaxHp != MaxHp;
    }

    public void UpdateHpChange()
    {
        if (!IsValid) return;

        PreviousHp = TrackedPlayerObj.CurrentHp;
        PreviousMaxHp = TrackedPlayerObj.MaxHp;
    }
}
