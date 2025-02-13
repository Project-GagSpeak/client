using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Restrictions;

namespace GagSpeak.Interop.IpcHelpers.Penumbra;
public class ModUpdateResult
{
    public ModAssociation UpdatedMod { get; set; }
    public bool IsChanged { get; set; }

    public ModUpdateResult(ModAssociation updatedMod, bool isChanged)
    {
        UpdatedMod = updatedMod;
        IsChanged = isChanged;
    }
}
