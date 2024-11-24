using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.MufflerCore.Handler;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;
using System.Security.Cryptography.Pkcs;
using System.Text.RegularExpressions;
using static Lumina.Data.Parsing.Layer.LayerCommon;

namespace GagSpeak.PlayerData.Data;

public partial class GagManager : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PlayerCharacterData _characterManager;
    private readonly GagDataHandler _gagDataHandler;
    private readonly Ipa_EN_FR_JP_SP_Handler _IPAParser;

    public List<GagData> _activeGags;

    // The items currently present in the active Gags UI Panel.
    private static string[] GagSearchFilters = new string[3] { "", "", "" };
    public static GagType[] ComboGags { get; private set; } = new GagType[3] { GagType.None, GagType.None, GagType.None };
    public static Padlocks[] ComboPadlocks { get; set; } = new Padlocks[4] { Padlocks.None, Padlocks.None, Padlocks.None, Padlocks.None };

    public GagManager(ILogger<GagManager> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, PlayerCharacterData characterManager, 
        GagDataHandler gagDataHandler, Ipa_EN_FR_JP_SP_Handler IPAParser) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _characterManager = characterManager;
        _gagDataHandler = gagDataHandler;
        _IPAParser = IPAParser;

        // check for any locked gags on delayed framework to see if their timers expired.
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => CheckForExpiredTimers());
    }

    public bool AnyGagActive => _activeGags.Any(gag => gag.Name != "None");
    public bool AnyGagLocked => _characterManager.AppearanceData?.GagSlots.Any(x => x.Padlock != "None") ?? false;
    // This is a preview of the padlock type that will be applied to the restraint set.
    public static Padlocks[] ActiveSlotPadlocks { get; set; } = new Padlocks[4] { Padlocks.None, Padlocks.None, Padlocks.None, Padlocks.None };
    public static string[] ActiveSlotPasswords { get; set; } = new string[4] { "", "", "", "" };
    public static string[] ActiveSlotTimers { get; set; } = new string[4] { "", "", "", "" };

    /// <summary> ONLY UPDATES THE LOGIC CONTROLLING GARBLE SPEECH, NOT APPEARNACE DATA </summary>
    public void UpdateGagGarblerLogic()
    {
        // compile the strings into a list of strings, then locate the names in the handler storage that match it.
        _activeGags = new List<string>
        {
            _characterManager.AppearanceData?.GagSlots[0].GagType ?? GagType.None.GagName(),
            _characterManager.AppearanceData?.GagSlots[1].GagType ?? GagType.None.GagName(),
            _characterManager.AppearanceData?.GagSlots[2].GagType ?? GagType.None.GagName(),
        }
        .Where(gagType => _gagDataHandler._gagTypes.Any(gag => gag.Name == gagType))
        .Select(gagType => _gagDataHandler._gagTypes.First(gag => gag.Name == gagType))
        .ToList();
    }

    public void PublishGagApplied(GagLayer layer)
    {
        Logger.LogTrace("Sending off Gag Applied Event to server!", LoggerType.GagHandling);
        PushGagChange((int)layer, true);
    }

    public void PublishLockApplied(GagLayer layer, Padlocks padlockType, string password, DateTimeOffset endTime, string assigner)
    {
        Logger.LogTrace("Sending off Lock Applied Event to server!", LoggerType.PadlockHandling);
        var newData = _characterManager.CompileAppearanceToAPI();
        newData.GagSlots[(int)layer].Padlock = padlockType.ToName();
        newData.GagSlots[(int)layer].Password = password;
        newData.GagSlots[(int)layer].Timer = endTime;
        newData.GagSlots[(int)layer].Assigner = assigner;
        PushLockChange(newData, (int)layer, true);
    }

    public void PublishLockRemoved(GagLayer layer)
    {
        Logger.LogTrace("Sending off Lock Removed Event to server!", LoggerType.PadlockHandling);
        var newData = _characterManager.CompileAppearanceToAPI();
        newData.GagSlots[(int)layer].Padlock = Padlocks.None.ToName();
        newData.GagSlots[(int)layer].Password = "";
        newData.GagSlots[(int)layer].Timer = DateTimeOffset.UtcNow;
        newData.GagSlots[(int)layer].Assigner = "";
        PushLockChange(newData, (int)layer, false);
    }

    public void PublishGagRemoved(GagLayer layer)
    {
        Logger.LogTrace("Sending off Gag Removed Event to server!", LoggerType.GagHandling);
        PushGagChange((int)layer, false);
    }

    public void ApplyGag(GagLayer layer, GagType newGagType)
    {
        if (_characterManager.CoreDataNull) return;

        Logger.LogTrace("Applying "+newGagType.GagName()+" on layer "+layer.ToString(), LoggerType.GagHandling);
        // update the appearance data.
        _characterManager.AppearanceData!.GagSlots[(int)layer].GagType = newGagType.GagName();
        // update our combos and logic.
        UpdateGagLockComboSelections();
        UpdateGagGarblerLogic();
    }

    public void LockGag(GagLayer layer, Padlocks padlockType, string password, DateTimeOffset endTime, string assigner)
    {
        if (_characterManager.CoreDataNull) return;

        Logger.LogTrace("Locking Gag at layer " + layer.ToString() + " with Padlock " + padlockType.ToName(), LoggerType.PadlockHandling);
        // update the appearance data.
        _characterManager.AppearanceData!.GagSlots[(int)layer].Padlock = padlockType.ToName();
        _characterManager.AppearanceData!.GagSlots[(int)layer].Password = password;
        _characterManager.AppearanceData!.GagSlots[(int)layer].Timer = endTime;
        _characterManager.AppearanceData!.GagSlots[(int)layer].Assigner = assigner;
        // update our combos and logic.
        UpdateGagLockComboSelections();
        UpdateGagGarblerLogic();
    }

    public void UnlockGag(GagLayer layer)
    {
        if (_characterManager.CoreDataNull) return;

        Logger.LogTrace("Unlocking " + _characterManager.AppearanceData!.GagSlots[(int)layer].Padlock + " at layer " + layer.ToString(), LoggerType.PadlockHandling);
        // update the appearance data.
        _characterManager.AppearanceData!.GagSlots[(int)layer].Padlock = Padlocks.None.ToName();
        _characterManager.AppearanceData!.GagSlots[(int)layer].Password = string.Empty;
        _characterManager.AppearanceData!.GagSlots[(int)layer].Timer = DateTimeOffset.MinValue;
        _characterManager.AppearanceData!.GagSlots[(int)layer].Assigner = string.Empty;
        // update our combos and logic.
        UpdateGagLockComboSelections();
        UpdateGagGarblerLogic();
        // reset the dropdown inputs.
        ActiveSlotPadlocks[(int)layer] = Padlocks.None;
    }

    /// <summary>
    /// Handles the logic for removing the gag.
    /// </summary>
    /// <returns> true if the operation was successful, false if stopped due to no change </returns>
    public bool RemoveGag(GagLayer Layer)
    {
        if (_characterManager.CoreDataNull) 
            return false;

        if (_characterManager.AppearanceData!.GagSlots[(int)Layer].GagType.ToGagType() is GagType.None)
            return false;

        Logger.LogTrace("Removing Gag at layer "+Layer.ToString(), LoggerType.GagHandling);
        // update the appearance data.
        _characterManager.AppearanceData!.GagSlots[(int)Layer].GagType = GagType.None.GagName();
        // update our combos and logic.
        UpdateGagLockComboSelections();
        UpdateGagGarblerLogic();
        return true;
    }

    public void UpdateGagLockComboSelections()
    {
        if (_characterManager.AppearanceData == null)
        {
            Logger.LogWarning("AppearanceData is null, cannot update combo selections.");
            return;
        }
        ComboGags[0] = _characterManager.AppearanceData.GagSlots[0].GagType.ToGagType();
        ComboGags[1] = _characterManager.AppearanceData.GagSlots[1].GagType.ToGagType();
        ComboGags[2] = _characterManager.AppearanceData.GagSlots[2].GagType.ToGagType();
        ComboPadlocks[0] = _characterManager.AppearanceData.GagSlots[0].Padlock.ToPadlock();
        ComboPadlocks[1] = _characterManager.AppearanceData.GagSlots[1].Padlock.ToPadlock();
        ComboPadlocks[2] = _characterManager.AppearanceData.GagSlots[2].Padlock.ToPadlock();
        Logger.LogTrace("Dropdown Gags Now: " + string.Join(" || ", _characterManager.AppearanceData!.GagSlots.Select((g, i) => $"Gag {i}: {g.GagType}")), LoggerType.GagHandling);
        Logger.LogTrace("Dropdown ActiveSlotPadlocks Now: " + string.Join(" || ", ActiveSlotPadlocks.Select((p, i) => $"Lock {i}: {p}")), LoggerType.PadlockHandling);
        Logger.LogTrace("Dropdown Appearance Padlocks Now: " + string.Join(" || ", _characterManager.AppearanceData.GagSlots.Select((p, i) => $"Lock {i}: {p.Padlock}")), LoggerType.PadlockHandling);
    }

    public void UpdateRestraintLockSelections(bool clearActiveSlotPadlock)
    {
        ComboPadlocks[3] = _clientConfigs.GetActiveSet()?.LockType.ToPadlock() ?? Padlocks.None;
        ActiveSlotPadlocks[3] = clearActiveSlotPadlock ? Padlocks.None : _clientConfigs.GetActiveSet()?.LockType.ToPadlock() ?? Padlocks.None;
    }

    private void PushGagChange(int layerIndex, bool isApplying)
    {
        var newData = _characterManager.CompileAppearanceToAPI();
        DataUpdateKind updateKind = layerIndex switch
        {
            0 => isApplying ? DataUpdateKind.AppearanceGagAppliedLayerOne : DataUpdateKind.AppearanceGagRemovedLayerOne,
            1 => isApplying ? DataUpdateKind.AppearanceGagAppliedLayerTwo : DataUpdateKind.AppearanceGagRemovedLayerTwo,
            2 => isApplying ? DataUpdateKind.AppearanceGagAppliedLayerThree : DataUpdateKind.AppearanceGagRemovedLayerThree,
            _ => throw new ArgumentOutOfRangeException(nameof(layerIndex), "Invalid layer index")
        };

        Mediator.Publish(new PlayerCharAppearanceChanged(newData, updateKind));
    }

    private void PushLockChange(CharaAppearanceData newData, int layerIndex, bool isLocking)
    {
        DataUpdateKind updateKind = layerIndex switch
        {
            0 => isLocking ? DataUpdateKind.AppearanceGagLockedLayerOne : DataUpdateKind.AppearanceGagUnlockedLayerOne,
            1 => isLocking ? DataUpdateKind.AppearanceGagLockedLayerTwo : DataUpdateKind.AppearanceGagUnlockedLayerTwo,
            2 => isLocking ? DataUpdateKind.AppearanceGagLockedLayerThree : DataUpdateKind.AppearanceGagUnlockedLayerThree,
            _ => throw new ArgumentOutOfRangeException(nameof(layerIndex), "Invalid layer index")
        };

        Mediator.Publish(new PlayerCharAppearanceChanged(newData, updateKind));
    }

    private void CheckForExpiredTimers()
    {
        if (_characterManager.CoreDataNull) 
            return;

        if (!AnyGagLocked) 
            return;

        // If a gag does have a padlock, ensure it is a timer padlock
        for (int i = 0; i < _characterManager.AppearanceData!.GagSlots.Length; i++)
        {
            var gagSlot = _characterManager.AppearanceData.GagSlots[i];
            if (GenericHelpers.TimerPadlocks.Contains(gagSlot.Padlock) && gagSlot.Timer - DateTimeOffset.UtcNow <= TimeSpan.Zero)
                PublishLockRemoved((GagLayer)i);
        }
    }

    public bool PadlockVerifyLock<T>(T item, GagLayer layer, bool extended, bool owner, bool devotional, TimeSpan maxTime) where T : IPadlockable
    {

        var result = false;
        switch (ActiveSlotPadlocks[(int)layer])
        {
            case Padlocks.None:
                return false;
            case Padlocks.MetalPadlock:
                return true;
            case Padlocks.FiveMinutesPadlock:
                ActiveSlotTimers[(int)layer] = "5m";
                return true;
            case Padlocks.CombinationPadlock:
                result = ValidateCombination(ActiveSlotPasswords[(int)layer]);
                if (!result) Logger.LogWarning("Invalid combination entered: {Password}", ActiveSlotPasswords[(int)layer]);
                return result;
            case Padlocks.PasswordPadlock:
                result = ValidatePassword(ActiveSlotPasswords[(int)layer]);
                if (!result) Logger.LogWarning("Invalid password entered: {Password}", ActiveSlotPasswords[(int)layer]);
                return result;
            case Padlocks.MimicPadlock:
                if (!TryParseTimeSpan(ActiveSlotTimers[(int)layer], out var mimicTime))
                {
                    Logger.LogWarning("Invalid time entered: {Timer}", ActiveSlotTimers[(int)layer]);
                    return false;
                }
                return true;
            case Padlocks.TimerPasswordPadlock:
                if (TryParseTimeSpan(ActiveSlotTimers[(int)layer], out var pwdTimer))
                {
                    if ((pwdTimer > TimeSpan.FromHours(1) && !extended) || pwdTimer > maxTime)
                    {
                        Logger.LogWarning("Attempted to lock for more than 1 hour without permission.");
                        return false;
                    }
                    result = ValidatePassword(ActiveSlotPasswords[(int)layer]) && pwdTimer > TimeSpan.Zero;
                }
                if (!result) Logger.LogWarning("Invalid password or time entered: {Password} {Timer}", ActiveSlotPasswords[(int)layer], ActiveSlotTimers[(int)layer]);
                return result;
            case Padlocks.OwnerPadlock:
                return owner;
            case Padlocks.OwnerTimerPadlock:
                if (!TryParseTimeSpan(ActiveSlotTimers[(int)layer], out var ownerTime))
                {
                    Logger.LogWarning("Invalid time entered: {Timer}", ActiveSlotTimers[(int)layer]);
                    return false;
                }
                if ((ownerTime > TimeSpan.FromHours(1) && !extended) || ownerTime > maxTime)
                {
                    Logger.LogWarning("Attempted to lock for more than 1 hour without permission.");
                    return false;
                }
                return owner;
            case Padlocks.DevotionalPadlock:
                return devotional;
            case Padlocks.DevotionalTimerPadlock:
                if (!TryParseTimeSpan(ActiveSlotTimers[(int)layer], out var devotionalTime))
                {
                    Logger.LogWarning("Invalid time entered: {Timer}", ActiveSlotTimers[(int)layer]);
                    return false;
                }
                // Check if the TimeSpan is longer than one hour and extended locks are not allowed
                if ((devotionalTime > TimeSpan.FromHours(1) && !extended) || devotionalTime > maxTime)
                {
                    Logger.LogWarning("Attempted to lock for longer than you were allowed access for!");
                    return false;
                }
                // return base case.
                return devotional;
        }
        return false;
    }

    public void ResetInputs()
    {
        ActiveSlotTimers = new string[4] { "", "", "", "" };
        ActiveSlotPasswords = new string[4] { "", "", "", "" };
    }

    public void DisplayPadlockFields(Padlocks padlock, int layer, bool unlocking = false, float totalWidth = 250)
    {
        float width = totalWidth;
        switch (padlock)
        {
            case Padlocks.CombinationPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Combination_Input", "Enter 4 digit combination...", ref ActiveSlotPasswords[layer], 4);
                break;
            case Padlocks.PasswordPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref ActiveSlotPasswords[layer], 20);
                break;
            case Padlocks.TimerPasswordPadlock:
                if (unlocking)
                {
                    ImGui.SetNextItemWidth(width);
                    ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref ActiveSlotPasswords[layer], 20);
                    break;
                }
                else
                {
                    ImGui.SetNextItemWidth(width * .65f);
                    ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref ActiveSlotPasswords[layer], 20);
                    ImUtf8.SameLineInner();
                    ImGui.SetNextItemWidth(width * .35f - ImGui.GetStyle().ItemInnerSpacing.X);
                    ImGui.InputTextWithHint("##Timer_Input", "Ex: 0h2m7s", ref ActiveSlotTimers[layer], 12); ;
                }
                break;
            case Padlocks.OwnerTimerPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Timer_Input", "Ex: 0h2m7s", ref ActiveSlotTimers[layer], 12);
                break;
        }
    }

    public bool PasswordValidated(Padlocks padlock, int slot, bool currentlyLocked)
    {
        Logger.LogDebug($"Validating Password for GagSlot {slot} which has padlock type "+padlock.ToName(), LoggerType.PadlockHandling);
        switch (padlock)
        {
            case Padlocks.None:
                return false;
            case Padlocks.MetalPadlock:
            case Padlocks.FiveMinutesPadlock:
                ActiveSlotTimers[slot] = "0h5m0s";
                return true;
            case Padlocks.CombinationPadlock:
                if (currentlyLocked)
                    return ActiveSlotPasswords[slot] == _characterManager.AppearanceData?.GagSlots[slot].Password;
                else
                    return ValidateCombination(ActiveSlotPasswords[slot]);
            case Padlocks.PasswordPadlock:
                if (currentlyLocked)
                    return ActiveSlotPasswords[slot] == _characterManager.AppearanceData?.GagSlots[slot].Password;
                else
                    return ValidatePassword(ActiveSlotPasswords[slot]);
            case Padlocks.TimerPasswordPadlock:
                if (currentlyLocked)
                    return ActiveSlotPasswords[slot] == _characterManager.AppearanceData?.GagSlots[slot].Password;
                else
                    return ValidatePassword(ActiveSlotPasswords[slot]) && TryParseTimeSpan(ActiveSlotTimers[slot], out TimeSpan test);
        }
        return false;
    }

    public bool RestraintPasswordValidate(Padlocks padlock, RestraintSet set, bool currentlyLocked)
    {
        Logger.LogDebug($"Validating Password restraintSet {set.Name} which has padlock type preview {ActiveSlotPadlocks[3]}", LoggerType.PadlockHandling);
        switch (padlock)
        {
            case Padlocks.None:
                return false;
            case Padlocks.MetalPadlock:
            case Padlocks.FiveMinutesPadlock:
                ActiveSlotTimers[3] = "5m";
                return true;
            case Padlocks.CombinationPadlock:
                if (currentlyLocked)
                {
                    Logger.LogTrace($"Checking if {ActiveSlotPasswords[3]} is equal to {set.LockPassword}", LoggerType.PadlockHandling);
                    return string.Equals(ActiveSlotPasswords[3], set.LockPassword, StringComparison.Ordinal);
                }
                else
                    return ValidateCombination(ActiveSlotPasswords[3]);
            case Padlocks.PasswordPadlock:
                if (currentlyLocked)
                {
                    Logger.LogTrace($"Checking if {ActiveSlotPasswords[3]} is equal to {set.LockPassword}", LoggerType.PadlockHandling);
                    return string.Equals(ActiveSlotPasswords[3], set.LockPassword, StringComparison.Ordinal);
                }
                else
                    return ValidatePassword(ActiveSlotPasswords[3]);
            case Padlocks.TimerPasswordPadlock:
                if (currentlyLocked)
                {
                    Logger.LogTrace($"Checking if {ActiveSlotPasswords[3]} is equal to {set.LockPassword}", LoggerType.PadlockHandling);
                    return string.Equals(ActiveSlotPasswords[3], set.LockPassword, StringComparison.Ordinal);
                }
                else
                    return ValidatePassword(ActiveSlotPasswords[3]) && TryParseTimeSpan(ActiveSlotTimers[3], out TimeSpan test);
        }
        return false;
    }

    /// <summary> Validates a password </summary>
    private bool ValidatePassword(string password)
    {
        Logger.LogDebug($"Validating Password {password}", LoggerType.PadlockHandling);
        return !string.IsNullOrWhiteSpace(password) && password.Length <= 20 && !password.Contains(" ");
    }

    /// <summary> Validates a 4 digit combination </summary>
    private bool ValidateCombination(string combination)
    {
        Logger.LogDebug($"Validating Combination {combination}", LoggerType.PadlockHandling);
        return int.TryParse(combination, out _) && combination.Length == 4;
    }

    private bool TryParseTimeSpan(string input, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        var regex = new Regex(@"(?:(\d+)d)?(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s)?");
        var match = regex.Match(input);

        if (!match.Success)
        {
            return false;
        }

        int days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
        int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
        int seconds = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;

        result = new TimeSpan(days, hours, minutes, seconds);
        return true;
    }

    // Below are special combos for the gags and padlocks:
    public void DrawGagCombo(GagLayer layer, float width, Action<GagType> onSelected)
    {
        try
        {
            // Return default if there are no items to display in the combo box.
            string comboLabel = "##ActiveGagsSelection" + layer.ToString();
            string displayText = ComboGags[(int)layer].GagName();
            
            var comboItems = Enum.GetValues<GagType>();
            ImGui.SetNextItemWidth(width);
            if (ImGui.BeginCombo(comboLabel, displayText))
            {
                // Search filter
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##filter", "Filter...", ref GagSearchFilters[(int)layer], 255);
                var searchText = GagSearchFilters[(int)layer].ToLowerInvariant();

                var filteredItems = string.IsNullOrEmpty(searchText)
                    ? comboItems
                    : comboItems.Where(item => item.GagName().ToLowerInvariant().Contains(searchText));

                // display filtered content.
                foreach (var item in filteredItems)
                {
                    bool isSelected = item.GagName() == ComboGags[(int)layer].GagName();
                    if (ImGui.Selectable(item.GagName(), isSelected))
                    {
                        Logger.LogTrace("Selected " + item.GagName() + " from " + comboLabel, LoggerType.GagHandling);
                        ComboGags[(int)layer] = item;
                        onSelected?.Invoke(item);
                    }
                }
                ImGui.EndCombo();
            }
            // Check if the item was right-clicked. If so, reset to default value.
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Logger.LogTrace("Right-clicked on "+comboLabel+". Resetting to default value.", LoggerType.GagHandling);
                ComboGags[(int)layer] = comboItems.First();
                onSelected?.Invoke(comboItems.First());
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in DrawComboSearchable");
        }
    }

    public void DrawPadlockCombo(int idValue, float width, IEnumerable<Padlocks> comboItems, Action<Padlocks> onSelected)
    {
        try
        {
            // Return default if there are no items to display in the combo box.
            string comboLabel = "##ActivePadlocksSelection" + idValue.ToString();
            string displayText = ComboPadlocks[idValue].ToName();

            ImGui.SetNextItemWidth(width);
            if (ImGui.BeginCombo(comboLabel, displayText))
            {
                // display filtered content.
                foreach (var item in comboItems)
                {
                    bool isSelected = item.ToName() == ComboPadlocks[idValue].ToName();
                    if (ImGui.Selectable(item.ToName(), isSelected))
                    {
                        Logger.LogTrace("Selected "+item.ToName()+" from "+comboLabel, LoggerType.PadlockHandling);
                        ComboPadlocks[idValue] = item;
                        onSelected?.Invoke(item);
                    }
                }
                ImGui.EndCombo();
            }
            // Check if the item was right-clicked. If so, reset to default value.
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Logger.LogTrace("Right-clicked on "+comboLabel+". Resetting to default value.", LoggerType.PadlockHandling);
                ComboPadlocks[idValue] = comboItems.First();
                onSelected?.Invoke(comboItems.First());
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in DrawPadlocks");
        }
    }

}
