using Dalamud.Game.ClientState.Objects.Types;
using GagSpeak.Interop.IpcHelpers.GameData;

namespace GagSpeak.CkCommons;

public static class Generic
{
    /// <summary> A helper method to ensure the action is executed safely. Exceptions are logged. </summary>
    /// <param name="act">the action to execute</param>
    public static void ExecuteSafely(Action act)
    {
        try
        {
            act();
        }
        catch (Exception ex)
        {
            StaticLogger.Logger.LogCritical("Error on executing safely:" + ex);
        }
    }

    public static void CancelDispose(this CancellationTokenSource? cts)
    {
        try
        {
            cts?.Cancel();
            cts?.Dispose();
        }
        catch (ObjectDisposedException) { }
    }

    public static CancellationTokenSource CancelRecreate(this CancellationTokenSource? cts)
    {
        cts?.CancelDispose();
        return new CancellationTokenSource();
    }

    public static T DeepClone<T>(this T obj)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            Converters = new List<JsonConverter> { new GameStainConverter() },
        };

        var jsonString = JsonConvert.SerializeObject(obj, settings);
        return JsonConvert.DeserializeObject<T>(jsonString, settings)!;
    }
    public static unsafe int? ObjectTableIndex(this IGameObject? gameObject)
    {
        if (gameObject == null || gameObject.Address == IntPtr.Zero)
        {
            return null;
        }

        return ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)gameObject.Address)->ObjectIndex;
    }
}

