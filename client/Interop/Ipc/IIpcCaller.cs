namespace GagSpeak.Interop.Ipc;
public interface IIpcCaller : IDisposable
{
    static bool APIAvailable { get; }
    void CheckAPI();
}
