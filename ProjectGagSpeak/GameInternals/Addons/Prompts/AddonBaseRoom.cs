using CkCommons;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace GagSpeak.GameInternals.Addons;
public unsafe static class AddonBaseRoom
{
    public static SeString SeString(AtkUnitBase* addon, uint idx)
        => addon->GetTextNodeById(idx)->NodeText.AsDalamudSeString();
    public static string ToText(AtkUnitBase* addon, uint idx) => SeString(addon, idx).ExtractText();

    public static string GetAllTextNodesText(AtkUnitBase* baseUnit)
    {
        var textList = new List<string>();
        var sizeCounter = 0;
        Svc.Logger.Debug("Rooms Menu has " + baseUnit->UldManager.NodeListCount + " text nodes");
        for (var i = 0; i < baseUnit->UldManager.NodeListCount; i++)
        {
            var node = baseUnit->UldManager.NodeList[i];
            if (node->Type == NodeType.Text)
            {
                var textNode = (AtkTextNode*)node;
                var seString = textNode->NodeText.AsDalamudSeString();
                textList.Add("["+sizeCounter+"] "+seString.ExtractText());
                sizeCounter++;
            }
        }

        return string.Join("\n", textList);
    }
}
