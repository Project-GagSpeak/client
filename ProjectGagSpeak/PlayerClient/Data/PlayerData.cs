using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using PlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace GagSpeak.PlayerClient;

/// <summary> 
///     Static Accessor for everything Player Related one might need to access.
/// </summary>
public static unsafe class PlayerData
{
    public static readonly int MaxLevel = 100;
    public static ClientLanguage Language => Svc.ClientState.ClientLanguage;
    public static IPlayerCharacter? Object => Svc.ClientState.LocalPlayer;
    public static IntPtr ObjectAddress => Svc.ClientState.LocalPlayer?.Address ?? IntPtr.Zero;
    public static bool Available => Svc.ClientState.LocalPlayer != null;
    public unsafe static bool AvailableThreadSafe => GameObjectManager.Instance()->Objects.IndexSorted[0].Value != null;
    public static ulong ContentId => Svc.ClientState.LocalContentId;
    public static ulong ContendIdInstanced => Control.Instance()->LocalPlayer->ContentId;
    public static StatusList? Status => Object?.StatusList;
    public static string Name => Object?.Name.ToString() ?? string.Empty;
    public static string NameInstanced => Control.Instance()->LocalPlayer->Name.ToString() ?? string.Empty;
    public static string HomeWorld => Object?.HomeWorld.Value.Name.ToString() ?? string.Empty;
    public static string HomeWorldInstanced => Svc.Data.GetExcelSheet<World>().GetRowOrDefault(HomeWorldIdInstanced) is { } w ? w.Name.ToString() : string.Empty;
    public static uint HomeWorldId => Object?.HomeWorld.RowId ?? 0;
    public static uint HomeWorldIdInstanced => Control.Instance()->LocalPlayer->HomeWorld;
    public static string NameWithWorld => GetNameWithWorld(Object);
    public static string NameWithWorldInstanced => NameInstanced + "@" + HomeWorldInstanced;
    public static string GetNameWithWorld(this IPlayerCharacter? pc) => pc is null ? string.Empty : (pc.Name.ToString() + "@" + pc.HomeWorld.Value.Name.ToString());
    public static uint CurrentWorldId => Object?.CurrentWorld.RowId ?? 0;
    public static string CurrentWorld => Object?.CurrentWorld.Value.Name.ToString() ?? string.Empty;
    public static string HomeDataCenter => Svc.Data.GetExcelSheet<World>().GetRowOrDefault(HomeWorldId)?.DataCenter.ValueNullable?.Name.ToString();
    public static string CurrentDataCenter => Svc.Data.GetExcelSheet<World>().GetRowOrDefault(CurrentWorldId)?.DataCenter.ValueNullable?.Name.ToString();

    public static uint OnlineStatus => Object?.OnlineStatus.RowId ?? 0;
    public static unsafe short Commendations => PlayerState.Instance()->PlayerCommendations;
    public static bool IsInHomeWorld => Available ? Object!.HomeWorld.RowId == Object!.CurrentWorld.RowId : false;
    public static bool IsInHomeDC => Available ? Object!.CurrentWorld.Value.DataCenter.RowId == Object!.HomeWorld.Value.DataCenter.RowId : false;
    public static unsafe bool IsInDuty => GameMain.Instance()->CurrentContentFinderConditionId is not 0; // alternative method from IDutyState
    public static unsafe bool IsOnIsland => MJIManager.Instance()->IsPlayerInSanctuary is 1;
    public static bool IsInPvP => GameMain.IsInPvPInstance();
    public static bool IsInGPose => GameMain.IsInGPose();

    public static uint Health => Available ? Object!.CurrentHp : 0;
    public static int Level => Object?.Level ?? 0;
    public static bool IsLevelSynced => PlayerState.Instance()->IsLevelSynced == 1;
    public static int SyncedLevel => PlayerState.Instance()->SyncedLevel;
    public static int UnsyncedLevel => GetUnsyncedLevel(JobId);
    public static int GetUnsyncedLevel(uint job) => PlayerState.Instance()->ClassJobLevels[Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault(job).Value.ExpArrayIndex];

    public static uint JobId => Object?.ClassJob.RowId ?? 0;
    public static unsafe ushort JobIdThreadSafe => PlayerState.Instance()->CurrentClassJobId;
    public static ActionRoles JobRole => (ActionRoles)(Object?.ClassJob.Value.Role ?? 0);
    public static byte GrandCompany => PlayerState.Instance()->GrandCompany;

    public static bool IsLoggedIn => Svc.ClientState.IsLoggedIn;
    public static bool InQuestEvent => Svc.Condition[ConditionFlag.OccupiedInQuestEvent];
    public static bool IsChocoboRacing => Svc.Condition[ConditionFlag.ChocoboRacing];
    public static bool IsZoning => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51];
    public static bool InDungeonDuty => Svc.Condition[ConditionFlag.BoundByDuty] || Svc.Condition[ConditionFlag.BoundByDuty56] || Svc.Condition[ConditionFlag.BoundByDuty95] || Svc.Condition[ConditionFlag.InDeepDungeon];
    public static bool InCutscene => !InDungeonDuty && Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Svc.Condition[ConditionFlag.WatchingCutscene78];
    public static bool CanFly => Control.CanFly;
    public static bool Mounted => Svc.Condition[ConditionFlag.Mounted];
    public static bool Mounting => Svc.Condition[ConditionFlag.MountOrOrnamentTransition];
    public static bool CanMount => Svc.Data.GetExcelSheet<TerritoryType>().GetRow(PlayerContent.TerritoryID).Mount && PlayerState.Instance()->NumOwnedMounts > 0;

    public static int PartySize => Svc.Party.Length;
    public static bool InSoloParty => Svc.Party.Length <= 1 && IsInDuty;

    public static Character* Character => (Character*)Svc.ClientState.LocalPlayer.Address;
    public static BattleChara* BattleChara => (BattleChara*)Svc.ClientState.LocalPlayer.Address;
    public static GameObject* GameObject => (GameObject*)Svc.ClientState.LocalPlayer.Address;

    public static Vector3 Position => Available ? Object!.Position : Vector3.Zero;
    public static float Rotation => Available ? Object!.Rotation : 0;
    public static bool IsMoving => Available && (AgentMap.Instance()->IsPlayerMoving || IsJumping);
    public static bool IsJumping => Available && (Svc.Condition[ConditionFlag.Jumping] || Svc.Condition[ConditionFlag.Jumping61] || Character->IsJumping());
    public static bool IsDead => Svc.Condition[ConditionFlag.Unconscious];
    public static bool Revivable => IsDead && AgentRevive.Instance()->ReviveState != 0;

    public static float DistanceTo(Vector3 other) => Vector3.Distance(Position, other);
    public static float DistanceTo(Vector2 other) => Vector2.Distance(new Vector2(Position.X, Position.Z), other);
    public static float DistanceTo(IGameObject other) => Vector3.Distance(Position, other.Position);

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
