namespace GagSpeak.CkCommons.HybridSaver;

public interface IConfigFileProvider
{
    /// <summary> Nessisary to inform the HybridSaveService that any AccountUnique configs are ready to be saved. </summary>
    public bool PerPlayerConfigsInitialized { get; }
}
