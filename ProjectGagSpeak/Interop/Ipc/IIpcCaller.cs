namespace GagSpeak.Interop; 
public interface IIpcCaller : IDisposable
{
    static bool APIAvailable { get; }
    void CheckAPI();
}
