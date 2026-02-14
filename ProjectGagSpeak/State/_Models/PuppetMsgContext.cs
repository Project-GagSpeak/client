using GagSpeak.Kinksters;
using GagspeakAPI.Data;

namespace GagSpeak.State.Models;

public readonly struct PuppetMsgContext
{
    public string DisplayName { get; }
    public string? UID { get; }
    public PuppetPerms PuppetPerms { get; }
    public string Trigger { get; }
    public char? StartChar { get; }
    public char? EndChar { get; }
    public IReadOnlyList<AliasTrigger> Aliases { get; }

    public PuppetMsgContext(string dispName, string? uid, string trigger, IReadOnlyList<AliasTrigger> aliases, PuppetPerms perms, char? sChar, char? eChar)
    {
        DisplayName = dispName;
        UID = uid;
        Trigger = trigger;
        Aliases = aliases;
        PuppetPerms = perms;
        StartChar = sChar;
        EndChar = eChar;
    }

    public static PuppetMsgContext ForGlobal(string nameWorld, string? uid, string trigger, IReadOnlyList<AliasTrigger> aliases, PuppetPerms perms)
        => new PuppetMsgContext(nameWorld, uid, trigger, aliases, perms, null, null);

    public static PuppetMsgContext ForPair(Kinkster k, string trigger, IReadOnlyList<AliasTrigger> aliases)
        => new PuppetMsgContext(k.GetDisplayName(), k.UserData.UID, trigger, aliases, k.OwnPerms.PuppetPerms, k.OwnPerms.StartChar, k.OwnPerms.EndChar);
}
