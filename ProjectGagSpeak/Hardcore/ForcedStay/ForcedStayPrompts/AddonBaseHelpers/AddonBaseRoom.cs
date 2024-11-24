using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Utils;

namespace GagSpeak.Hardcore.ForcedStay;
public unsafe static class AddonBaseRoom
{
    public static SeString SeString(AtkUnitBase* addon, uint idx)
        => MemoryHelper.ReadSeString(&(addon)->GetTextNodeById(idx)->NodeText);
    public static string ToText(AtkUnitBase* addon, uint idx) => SeString(addon, idx).ExtractText();

    public static string GetAllTextNodesText(AtkUnitBase* baseUnit)
    {
        var textList = new List<string>();
        var sizeCounter = 0;
        StaticLogger.Logger.LogDebug("Rooms Menu has " + baseUnit->UldManager.NodeListCount + " text nodes");
        for (var i = 0; i < baseUnit->UldManager.NodeListCount; i++)
        {
            var node = baseUnit->UldManager.NodeList[i];
            if (node->Type == NodeType.Text)
            {
                var textNode = (AtkTextNode*)node;
                var seString = MemoryHelper.ReadSeString(&textNode->NodeText);
                textList.Add("["+sizeCounter+"] "+seString.ExtractText());
                sizeCounter++;
            }
        }

        return string.Join("\n", textList);
    }
}
