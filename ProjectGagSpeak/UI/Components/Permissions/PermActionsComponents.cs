using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Enums;
using GagspeakAPI.Data.Interfaces;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Numerics;
using GagspeakAPI.Data.Permissions;
using Dalamud.Utility;

namespace GagSpeak.UI.Components;

/// <summary>
/// A helper class to make the permission actions less repetitive and obnoxious.
/// </summary>
public class PermActionsComponents
{
    private readonly ILogger<PermActionsComponents> _logger;
    private readonly MainHub _apiHubMain;
    private readonly UiSharedService _uiShared;
    private readonly MoodlesService _moodlesService;

    private int _selectedGagLayer = 0;
    private string _password = "";
    private string _timer = "";
    private Dictionary<string, string> SearchStrings = new();
    private Dictionary<string, object> CachedPermActionSelections = new();
    
    public PermActionsComponents(ILogger<PermActionsComponents> logger,
        MainHub apiHubMain, UiSharedService uiShared, 
        MoodlesService moodlesService)
    {
        _logger = logger;
        _apiHubMain = apiHubMain;
        _uiShared = uiShared;
        _moodlesService = moodlesService;
    }

    public int GagLayer { get => _selectedGagLayer; set => _selectedGagLayer = value; }
    public string Password { get => _password; set => _password = value;}
    public string Timer { get => _timer; set => _timer = value; }

    // helper function to set the selected action of a combo item
    public T? GetSelectedItem<T>(string identifier, string pairAliasUID)
    {
        string comboName = ComboName(identifier, pairAliasUID);
        if (CachedPermActionSelections.TryGetValue(comboName, out var selectedItem))
        {
            return selectedItem is T item ? item : default;
        }
        return default;
    }

    public void DrawGagLayerSelection(float comboWidth, string pairAliasUID)
    {
        using (var gagLayerGroup = ImRaii.Group())
        {
            ImGui.SetNextItemWidth(comboWidth);
            ImGui.Combo("##GagLayerSelection", ref _selectedGagLayer, new string[] { "Layer 1", "Layer 2", "Layer 3" }, 3);
            UiSharedService.AttachToolTip("Select the layer to apply a Gag to.");
        }
    }

    public string ComboName(string identifier, string pairAliasUID) => "##" + identifier + "-" + pairAliasUID;

    public void DrawGenericComboButton<T>(
        string pairUID, 
        string identifier, 
        string buttonLabel, 
        float totalWidth,
        IEnumerable<T> comboItems, 
        Func<T, string> itemToName, 
        bool isSearchable = false, 
        bool buttonDisabled = false, 
        bool isIconButton = false, 
        T? initialSelectedItem = default, 
        FontAwesomeIcon icon = FontAwesomeIcon.None, 
        ImGuiComboFlags flags = ImGuiComboFlags.None, 
        Action<T?>? onSelected = null, 
        Action<T?>? onButton = null,
        string comboTT = "",
        string buttonTT = "")
    {
        // formulate our conjoined combo name identifier.
        string comboName = ComboName(identifier,pairUID);
        // return blank combo if empty.
        if (!comboItems.Any())
        {
            ImGui.SetNextItemWidth(totalWidth);
            ImGui.BeginCombo(comboName, "No Items...", flags);
            ImGui.EndCombo();
            return;
        }

        // if the selected item for this list is not in the dictionary,
        // set it to the first item in the list or the initial selected item.
        if (!CachedPermActionSelections.TryGetValue(comboName, out var selectedItem) && selectedItem == null)
        {
            if (!EqualityComparer<T>.Default.Equals(initialSelectedItem, default))
            {
                selectedItem = initialSelectedItem;
                CachedPermActionSelections[comboName] = selectedItem!;
                if (!EqualityComparer<T>.Default.Equals(initialSelectedItem, default))
                    onSelected?.Invoke(initialSelectedItem);
            }
            else
            {
                selectedItem = comboItems.First();
                CachedPermActionSelections[comboName] = selectedItem!;
            }
        }

        float comboWidth = isIconButton
            ? totalWidth - _uiShared.GetIconTextButtonSize(icon, buttonLabel) - ImGui.GetStyle().ItemInnerSpacing.X
            : totalWidth - ImGuiHelpers.GetButtonSize(buttonLabel).X - ImGui.GetStyle().ItemInnerSpacing.X;
        ImGui.SetNextItemWidth(comboWidth);
        if (ImGui.BeginCombo(comboName, selectedItem != null ? itemToName((T)selectedItem) : "Nothing Selected..", flags))
        {
            IEnumerable<T> itemsToDisplay = comboItems;

            if (isSearchable)
            {
                if (!SearchStrings.ContainsKey(comboName))
                    SearchStrings[comboName] = string.Empty;

                string searchText = SearchStrings[comboName].ToLowerInvariant();

                ImGui.SetNextItemWidth(comboWidth);
                if (ImGui.InputTextWithHint("##filter", "Filter...", ref searchText, 255))
                    SearchStrings[comboName] = searchText;

                if (!string.IsNullOrEmpty(searchText))
                    itemsToDisplay = comboItems.Where(item => itemToName(item).ToLowerInvariant().Contains(searchText));
            }

            // Display filtered content.
            foreach (var item in itemsToDisplay)
            {
                bool isSelected = EqualityComparer<T>.Default.Equals(item, (T?)selectedItem);
                if (ImGui.Selectable(itemToName(item), isSelected))
                {
                    CachedPermActionSelections[comboName] = item!;
                    onSelected?.Invoke(item!); // invoke the selected action.
                }
            }
            ImGui.EndCombo();
        }
        if(comboTT != string.Empty) UiSharedService.AttachToolTip(comboTT);

        // Check if the item was right-clicked. If so, reset to default value.
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            selectedItem = comboItems.First();
            CachedPermActionSelections[comboName] = selectedItem!;
            onSelected?.Invoke((T)selectedItem!); // invoke the selected action on clear.
        }

        ImUtf8.SameLineInner();
        if (isIconButton)
        {
            if (_uiShared.IconTextButton(icon, buttonLabel, null, false, buttonDisabled, (comboName + identifier + buttonLabel)))
            {
                onButton?.Invoke((T)selectedItem!); // invoke the button action.
            }
            if(buttonTT != string.Empty) UiSharedService.AttachToolTip(buttonTT);
        }
        else
        {
            if (ImGuiUtil.DrawDisabledButton(buttonLabel, new Vector2(), string.Empty, buttonDisabled))
            {
                onButton?.Invoke((T)selectedItem!); // invoke the button action.
            }
            if (buttonTT != string.Empty) UiSharedService.AttachToolTip(buttonTT);
        }
    }

    public (bool, string) PadlockVerifyLock<T>(T item, Padlocks selectedLock, bool extended, bool owner, bool devotional, TimeSpan maxTime) where T : IPadlockable
    {

        var result = false;
        switch (selectedLock)
        {
            case Padlocks.None:
                return (false, "No padlock selected.");

            case Padlocks.MetalPadlock:
            case Padlocks.FiveMinutesPadlock:
                _timer = "5m";
                return (true, string.Empty);

            case Padlocks.CombinationPadlock:
                result = _uiShared.ValidateCombination(Password);
                return (result, (result ? string.Empty : "Invalid combination entered: "+Password));

            case Padlocks.PasswordPadlock:
                result = _uiShared.ValidatePassword(Password);
                return (result, (result ? string.Empty : "Invalid password entered: "+Password));

            case Padlocks.TimerPasswordPadlock:
                if (_uiShared.TryParseTimeSpan(Timer, out var pwdTimer) || Timer.IsNullOrEmpty())
                {
                    if ((pwdTimer > TimeSpan.FromHours(1) && !extended) || pwdTimer > maxTime)
                        return (false, "Attempted to lock for more than 1 hour without permission.");

                    result = _uiShared.ValidatePassword(Password) && pwdTimer > TimeSpan.Zero;
                }
                if (!result) _logger.LogWarning("Invalid password or time entered: {Password} {Timer}", Password, Timer);
                return (result, (result ? string.Empty : "Invalid field. PWD:"+Password+", TIME: "+Timer));
            case Padlocks.OwnerPadlock:
                return (owner, owner ? string.Empty : "You don't have Owner Lock permissions!");
            case Padlocks.OwnerTimerPadlock:
                if (!_uiShared.TryParseTimeSpan(Timer, out var ownerTime) || Timer.IsNullOrEmpty())
                    return (false, "Invalid Timer format: " + Timer);

                if ((ownerTime > TimeSpan.FromHours(1) && !extended) || ownerTime > maxTime)
                    return (false, "Attempted to lock for a timer longer than allowed.");

                return (owner, owner ? string.Empty : "You don't have Owner Lock permissions!");
            case Padlocks.DevotionalPadlock:
                return (devotional, devotional ? string.Empty : "You don't have Devotional Lock permissions!");
            case Padlocks.DevotionalTimerPadlock:
                if(!_uiShared.TryParseTimeSpan(Timer, out var devotionalTime) || Timer.IsNullOrEmpty())
                    return (false, "Invalid Timer format: "+Timer);
                // Check if the TimeSpan is longer than one hour and extended locks are not allowed
                if ((devotionalTime > TimeSpan.FromHours(1) && !extended) || devotionalTime > maxTime)
                    return (false, "Attempted to lock for a timer longer than allowed.");
                // return base case.
                return (devotional, devotional ? string.Empty : "You don't have Devotional Lock permissions!");
        }
        return (false, "Invalid padlock selected.");
    }

    public void ResetInputs()
    {
        _password = string.Empty;
        _timer = string.Empty;
    }

    public (bool, string) PadlockVerifyUnlock<T>(T data, Padlocks selectedPadlock, bool allowOwner, bool allowDevotional) where T : IPadlockable
    {
        switch (selectedPadlock)
        {
            case Padlocks.None:
                return (false, "No padlock selected.");
            case Padlocks.MetalPadlock:
            case Padlocks.FiveMinutesPadlock:
                return (true, string.Empty);
            case Padlocks.CombinationPadlock:
                var resCombo = _uiShared.ValidateCombination(Password) && Password == data.Password;
                return (resCombo, resCombo ? string.Empty : "Invalid combination entered: "+Password);
            case Padlocks.PasswordPadlock:
            case Padlocks.TimerPasswordPadlock:
                var resPass = _uiShared.ValidatePassword(Password) && Password == data.Password;
                return (resPass, resPass ? string.Empty : "Invalid password entered: "+Password);
            case Padlocks.OwnerPadlock:
            case Padlocks.OwnerTimerPadlock:
                return (allowOwner, allowOwner ? string.Empty : "You don't have Owner Lock Access");
            case Padlocks.DevotionalPadlock:
            case Padlocks.DevotionalTimerPadlock:
                var canUnlock = allowDevotional && MainHub.UID == data.Assigner;
                return (canUnlock, canUnlock ? string.Empty : "You aren't the Devotional Assigner");
        }
        return (false, "Invalid padlock selected.");
    }

    public bool ExpandLockHeightCheck(Padlocks type)
        => type is Padlocks.CombinationPadlock or Padlocks.PasswordPadlock or Padlocks.TimerPasswordPadlock or Padlocks.OwnerTimerPadlock or Padlocks.DevotionalTimerPadlock;


    public void DisplayPadlockFields(Padlocks selectedPadlock, bool unlocking = false)
    {
        float width = ImGui.GetContentRegionAvail().X;
        switch (selectedPadlock)
        {
            case Padlocks.CombinationPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Combination_Input", "Enter 4 digit combination...", ref _password, 4);
                break;
            case Padlocks.PasswordPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref _password, 20);
                break;
            case Padlocks.TimerPasswordPadlock:
                if(unlocking)
                {
                    ImGui.SetNextItemWidth(width);
                    ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref _password, 20);
                    break;
                }
                else
                {
                    ImGui.SetNextItemWidth(width * (2 / 3f));
                    ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref _password, 20);
                    ImUtf8.SameLineInner();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    ImGui.InputTextWithHint("##Timer_Input", "Ex: 0h2m7s", ref _timer, 12); ;
                }
                break;
            case Padlocks.OwnerTimerPadlock:
            case Padlocks.DevotionalTimerPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Timer_Input", "Ex: 0h2m7s", ref _timer, 12); 
                break;
        }
    }
}

