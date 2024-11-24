using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Utils;

namespace GagSpeak.Hardcore.ForcedStay;
public unsafe static class AddonBaseString
{
    public static SeString SeString(AddonSelectString* addon)
        => MemoryHelper.ReadSeString(&((AtkUnitBase*)addon)->GetTextNodeById(2)->NodeText);
    public static string ToText(AddonSelectString* addon) => SeString(addon).ExtractText();

    public static Entry[] GetEntries(AddonSelectString* addon)
    {
        var ret = new Entry[addon->PopupMenu.PopupMenu.EntryCount];
        for (var i = 0; i < ret.Length; i++)
        {
            ret[i] = new Entry(addon, i);
        }
        return ret;
    }

    public struct Entry
    {
        private AddonSelectString* Addon;
        public int Index { get; init; }

        public Entry(AddonSelectString* addon, int index)
        {
            Addon = addon;
            Index = index;
        }

        public SeString SeString => MemoryHelper.ReadSeStringNullTerminated((nint)Addon->PopupMenu.PopupMenu.EntryNames[Index]);
        public string Text => SeString.ExtractText();
    }
}
