using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak.Interop.IpcHelpers.Penumbra;
public class ModUpdateResult
{
    public AssociatedMod UpdatedMod { get; set; }
    public bool IsChanged { get; set; }

    public ModUpdateResult(AssociatedMod updatedMod, bool isChanged)
    {
        UpdatedMod = updatedMod;
        IsChanged = isChanged;
    }
}
