namespace GagSpeak.State.Models;
public class VfxLoopItem
{
    public VfxSpawnItem Item;
    public DateTime RemovedTime;

    public VfxLoopItem(VfxSpawnItem item, DateTime removedTime)
    {
        Item = item;
        RemovedTime = removedTime;
    }
}
