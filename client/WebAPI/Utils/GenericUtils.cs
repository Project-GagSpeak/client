using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Common.Lua;
using GagSpeak.Interop.IpcHelpers.GameData;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Enums;

namespace GagSpeak.WebAPI.Utils;

public static class GenericUtils
{

    public static void CancelDispose(this CancellationTokenSource? cts)
    {
        try
        {
            cts?.Cancel();
            cts?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // swallow it
        }
    }

    public static CancellationTokenSource CancelRecreate(this CancellationTokenSource? cts)
    {
        cts?.CancelDispose();
        return new CancellationTokenSource();
    }

    /// <summary>
    /// One big nasty function for checking for updated data. (obviously i shorted it a lot lol)
    /// </summary>
    /// <returns></returns>
    public static HashSet<PlayerChanges> CheckUpdatedData(this CharaIPCData newData, Guid applicationBase,
            CharaIPCData? oldData, ILogger logger, PairHandler cachedPlayer)
    {
        oldData ??= new();
        var charaDataToUpdate = new HashSet<PlayerChanges>();

        bool moodlesDataDifferent = !string.Equals(oldData.MoodlesData, newData.MoodlesData, StringComparison.Ordinal);
        if (moodlesDataDifferent)
        {
            logger.LogDebug("[BASE-"+ applicationBase+"] Updating "+cachedPlayer+" (Diff moodles data) => "+PlayerChanges.Moodles, LoggerType.GameObjects);
            charaDataToUpdate.Add(PlayerChanges.Moodles);
        }

        return charaDataToUpdate;
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
