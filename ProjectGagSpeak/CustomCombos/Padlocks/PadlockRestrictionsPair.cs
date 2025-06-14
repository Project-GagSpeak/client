using GagSpeak.Kinksters.Pairs;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Network;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;

namespace GagSpeak.CustomCombos.Padlock;

public class PairRestrictionPadlockCombo : CkPadlockComboBase<ActiveRestriction>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    public PairRestrictionPadlockCombo(Pair pair, MainHub hub, ILogger log)
        : base([ ..pair.LastRestrictionsData.Restrictions ], log)
    {
        _mainHub = hub;
        _pairRef = pair;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.GetLocksForPair(_pairRef.PairPerms);
    protected override string ItemName(ActiveRestriction item)
        => _pairRef.LastLightStorage.Restrictions.FirstOrDefault(r => r.Id == item.Identifier) is { } restriction
            ? restriction.Label : "None";
    protected override bool DisableCondition(int layerIdx)
        => !_pairRef.PairPerms.ApplyRestrictions || SelectedLock == Items[layerIdx].Padlock || !Items[layerIdx].CanLock();

    protected override async Task<bool> OnLockButtonPress(int layerIdx)
    {
        if (Items[layerIdx].CanLock() && _pairRef.PairPerms.LockRestrictions)
        {
            var dto = new PushKinksterRestrictionUpdate(_pairRef.UserData, DataUpdateType.Locked)
            {
                Layer = layerIdx,
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID,
            };

            var result = await _mainHub.UserChangeKinksterRestrictionState(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform LockRestriction with {SelectedLock.ToName()} on {_pairRef.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
                ResetSelection();
                ResetInputs();
                return false;
            }
            else
            {
                Log.LogDebug($"Locking Restriction with {SelectedLock.ToName()} on {_pairRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
                ResetSelection();
                ResetInputs();
                return true;
            }
        }
        return false;
    }

    protected override async Task<bool> OnUnlockButtonPress(int layerIdx)
    {
        if (Items[layerIdx].CanUnlock() && _pairRef.PairPerms.UnlockRestrictions)
        {
            var dto = new PushKinksterRestrictionUpdate(_pairRef.UserData, DataUpdateType.Unlocked)
            {
                Layer = layerIdx,
                Padlock = Items[layerIdx].Padlock,
                Password = Password,
                PadlockAssigner = MainHub.UID,
            };

            var result = await _mainHub.UserChangeKinksterRestrictionState(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform UnlockRestriction with {Items[layerIdx].Padlock.ToName()} on {_pairRef.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
                ResetSelection();
                ResetInputs();
                return false;
            }
            else
            {
                Log.LogDebug($"Unlocking Restriction with {Items[layerIdx].Padlock.ToName()} on {_pairRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
                ResetSelection();
                ResetInputs();
                return true;
            }
        }
        return false;
    }
}
