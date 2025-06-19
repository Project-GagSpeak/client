using Dalamud.Game.ClientState.Aetherytes;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using GagSpeak.Utils;
using Lumina.Excel.Sheets;
using Aetheryte = Lumina.Excel.Sheets.Aetheryte;
using PlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace GagSpeak.PlayerClient;

/// <summary> 
///     Static Accessor for everything Player Related one might need to access.
/// </summary>
public static class PlayerContent
{
    public static uint TerritoryID => Svc.ClientState.TerritoryType;

    private static string TerritoryName
    {
        get
        {
            var t = Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(TerritoryID);
            return $"{TerritoryID} | {t?.ContentFinderCondition.ValueNullable?.Name.ToString() ?? (t?.PlaceName.ValueNullable?.Name.ToString())}";
        }
    }

    public static uint Territory => Svc.ClientState.TerritoryType;
    public static TerritoryIntendedUseEnum TerritoryIntendedUse => (TerritoryIntendedUseEnum)(Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(Territory)?.TerritoryIntendedUse.ValueNullable?.RowId ?? default);
    public unsafe static IAetheryteEntry? HomeAetheryte => Svc.AetheryteList[PlayerState.Instance()->HomeAetheryteId];
    public static bool InMainCity => Svc.Data.GetExcelSheet<Aetheryte>()?.Any(x => x.IsAetheryte && x.Territory.RowId == Territory && x.Territory.Value.TerritoryIntendedUse.Value.RowId is 0) ?? false;
    public static string MainCityName => Svc.Data.GetExcelSheet<Aetheryte>()?.FirstOrDefault(x => x.IsAetheryte && x.Territory.RowId == Territory && x.Territory.Value.TerritoryIntendedUse.Value.RowId is 0).PlaceName.ToString() ?? "Unknown";
    public static TerritoryType TerritoryType => Svc.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(Territory) ?? default;

    public static void OpenMapWithMapLink(MapLinkPayload mapLink) => Svc.GameGui.OpenMapWithMapLink(mapLink);
    public static DeepDungeonType? GetDeepDungeonType()
    {
        if (Svc.Data.GetExcelSheet<TerritoryType>()?.GetRow(Svc.ClientState.TerritoryType) is { } territoryInfo)
        {
            return territoryInfo switch
            {
                { TerritoryIntendedUse.Value.RowId: 31, ExVersion.RowId: 0 or 1 } => DeepDungeonType.PalaceOfTheDead,
                { TerritoryIntendedUse.Value.RowId: 31, ExVersion.RowId: 2 } => DeepDungeonType.HeavenOnHigh,
                { TerritoryIntendedUse.Value.RowId: 31, ExVersion.RowId: 4 } => DeepDungeonType.EurekaOrthos,
                _ => null
            };
        }
        return null;
    }

}
