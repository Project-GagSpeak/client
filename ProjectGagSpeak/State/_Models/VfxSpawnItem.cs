namespace GagSpeak.State.Models;
public class VfxSpawnItem
{
    public readonly string Path;
    public readonly SpawnType Type;
    public readonly bool CanLoop;

    public VfxSpawnItem(string path, SpawnType type, bool canLoop)
    {
        Path = path;
        Type = type;
        CanLoop = canLoop;
    }
}
