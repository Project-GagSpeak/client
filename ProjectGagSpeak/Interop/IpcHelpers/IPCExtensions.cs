using Dalamud.Plugin.Ipc.Exceptions;
using Dalamud.Plugin.Services;

namespace GagSpeak.Interop.Helpers;

public static class IPCExtensions
{
    extension(IFramework fw)
    {
        /// <summary>
        ///  Safely invoke IPC calls on the framework tick, ensuring that asynchronous calls do not have a chance to crash the client because the state changed since we last checked.
        /// </summary>
        public async Task<T> InvokeIPC<T>(Func<T> func, T returnOnFail)
        {
            try
            {
                return await fw.RunOnFrameworkThread(func).ConfigureAwait(false);
            }
            catch (IpcNotReadyError ex)
            {
                Svc.Logger.Warning("Called IPC when it was shut down/not available.\n" + ex);
                return returnOnFail;
            }
        }
    }
}
