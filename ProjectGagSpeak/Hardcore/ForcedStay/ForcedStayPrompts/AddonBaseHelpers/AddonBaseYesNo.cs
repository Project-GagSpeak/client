using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Utils;

namespace GagSpeak.Hardcore.ForcedStay;
public static unsafe class AddonBaseYesNo
{
    public static SeString SeStringNullTerminated(AddonSelectYesno* addon)
        => MemoryHelper.ReadSeStringNullTerminated(new nint(addon->AtkValues[0].String));

    public static string GetTextLegacy(AddonSelectYesno* addon)
        => string.Join(string.Empty, SeStringNullTerminated(addon).Payloads
            .OfType<TextPayload>()
            .Select(t => t.Text)).Replace('\n', ' ').Trim();

    public static void Yes(AddonSelectYesno* addon)
    {
        if (addon->YesButton != null && !addon->YesButton->IsEnabled)
        {
            StaticLogger.Logger.LogTrace($"{nameof(AddonSelectYesno)}: Force enabling yes button");
            var flagsPtr = (ushort*)&addon->YesButton->AtkComponentBase.OwnerNode->AtkResNode.NodeFlags;
            *flagsPtr ^= 1 << 5;
        }
        ClickButtonIfEnabled(addon->YesButton);
    }

    public static void No(AddonSelectYesno* addon)
    {
        ClickButtonIfEnabled(addon->NoButton);
        StaticLogger.Logger.LogTrace($"{nameof(AddonSelectYesno)}: Force enabling no button");
    }

    public static bool ClickButtonIfEnabled(AtkComponentButton* button)
    {
        if (button->IsEnabled)
        {
            button->ClickAddonButton((AtkUnitBase*)button);
            return true;
        }
        return false;
    }
}
