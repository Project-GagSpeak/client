using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.Permissions;

/// <summary>
/// Contains functions relative to the paired users permissions for the client user.
/// 
/// Yes its messy, yet it's long, but i functionalized it best i could for the insane 
/// amount of logic being performed without adding too much overhead.
/// </summary>
public partial class PairStickyUI
{
    private void DrawWardrobeActions()
    {
        // verify that the unique pair perms and last recieved wardrobe data are not null.
        var lastWardrobeData = StickyPair.LastWardrobeData;
        var lastLightStorage = StickyPair.LastLightStorage;
        if (lastWardrobeData is null || PairPerms is null || lastLightStorage is null)
            return;

        bool applyButtonDisabled = !PairPerms.ApplyRestraintSets || lastLightStorage.Restraints.Count <= 0 || (!lastWardrobeData.ActiveSetId.IsEmptyGuid() && lastWardrobeData.Padlock != Padlocks.None.ToName());
        bool lockButtonDisabled = !PairPerms.LockRestraintSets || lastWardrobeData.ActiveSetId.IsEmptyGuid() || lastWardrobeData.Padlock != Padlocks.None.ToName();
        bool unlockButtonDisabled = !PairPerms.UnlockRestraintSets || lastWardrobeData.Padlock.ToPadlock() is Padlocks.None;
        bool removeButtonDisabled = !PairPerms.RemoveRestraintSets || lastWardrobeData.ActiveSetId.IsEmptyGuid() || lastWardrobeData.Padlock != Padlocks.None.ToName();

        ////////// APPLY RESTRAINT SET //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.Handcuffs, "Apply Restraint Set", WindowMenuWidth, true, applyButtonDisabled))
        {
            Opened = Opened == InteractionType.ApplyRestraint ? InteractionType.None : InteractionType.ApplyRestraint;
        }
        UiSharedService.AttachToolTip("Applies a Restraint Set to " + StickyPair.UserData.AliasOrUID + ". Click to select set.");
        if (Opened is InteractionType.ApplyRestraint)
        {
            using (var actionChild = ImRaii.Child("SetApplyChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!actionChild) return;

                LightRestraintData storedSelectedSet = _permActions.GetSelectedItem<LightRestraintData>("ApplyRestraintSetForPairPermCombo", StickyPair.UserData.UID) ?? new LightRestraintData();

                _permActions.DrawGenericComboButton(StickyPair.UserData.UID, "ApplyRestraintSetForPairPermCombo", "Apply Set", WindowMenuWidth,
                    comboItems: lastLightStorage.Restraints,
                    itemToName: (RSet) => RSet.Name,
                    isSearchable: true,
                    buttonDisabled: storedSelectedSet.Identifier.IsEmptyGuid(),
                    isIconButton: true,
                    initialSelectedItem: default,
                FontAwesomeIcon.Female, ImGuiComboFlags.None, (selected) => { _logger.LogDebug("Selected Restraint Set: " + selected, LoggerType.Permissions); },
                (onButtonPress) =>
                {
                    try
                    {
                        var newWardrobe = StickyPair.LastWardrobeData.DeepClone();
                        if (newWardrobe == null || onButtonPress == null) throw new Exception("Wardrobe data is null, not sending");

                        newWardrobe.ActiveSetId = onButtonPress.Identifier;
                        newWardrobe.ActiveSetEnabledBy = MainHub.UID;
                        _ = _apiHubMain.UserPushPairDataWardrobeUpdate(new(StickyPair.UserData, newWardrobe, MainHub.PlayerUserData, DataUpdateKind.WardrobeRestraintApplied));
                        _logger.LogDebug("Applying Restraint Set with GagPadlock " + onButtonPress.ToString() + " to " + PairNickOrAliasOrUID, LoggerType.Permissions);
                        Opened = InteractionType.ApplyRestraint;
                    }
                    catch (Exception e) { _logger.LogError("Failed to push updated Wardrobe data: " + e.Message); }
                });
            }
            ImGui.Separator();
        }

        ////////// LOCK RESTRAINT SET //////////
        string DisplayText = unlockButtonDisabled ? "Lock Restraint Set" : "Locked with a " + lastWardrobeData.Padlock;
        string tooltipText = unlockButtonDisabled
            ? "Locks the Restraint Set applied to " + PairNickOrAliasOrUID + ". Click to view options."
            : "Set is currently locked with a " + lastWardrobeData.Padlock;
        // push text style
        using (var color = ImRaii.PushColor(ImGuiCol.Text, (lastWardrobeData.Padlock == Padlocks.None.ToName()) ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow))
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.Lock, DisplayText, WindowMenuWidth, true, lockButtonDisabled))
            {
                Opened = Opened == InteractionType.LockRestraint ? InteractionType.None : InteractionType.LockRestraint;
            }
        }
        UiSharedService.AttachToolTip(tooltipText + ((!unlockButtonDisabled && GenericHelpers.TimerPadlocks.Contains(lastWardrobeData.Padlock))
            ? "--SEP----COL--" + UiSharedService.TimeLeftFancy(lastWardrobeData.Timer) : ""), color: ImGuiColors.ParsedPink);

        if (Opened is InteractionType.LockRestraint)
        {
            Padlocks selected = _permActions.GetSelectedItem<Padlocks>("LockRestraintSetForPairPermCombo", StickyPair.UserData.UID);
            float height = _permActions.ExpandLockHeightCheck(selected)
                ? 3 * ImGui.GetFrameHeight() + 2 * ImGui.GetStyle().ItemSpacing.Y
                : 2 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y;

            using (var actionChild = ImRaii.Child("SetLockChild", new Vector2(WindowMenuWidth, height), false))
            {
                if (!actionChild) return;

                bool disabled = selected == Padlocks.None || !PairPerms.LockRestraintSets;

                using (ImRaii.Disabled(true))
                {
                    ImGui.SetNextItemWidth(WindowMenuWidth);
                    if (ImGui.BeginCombo("##DummyComboDisplayLockedSet", "Locking: " + StickyPair.ActiveSetName())) { ImGui.EndCombo(); }
                }

                // Draw combo
                _permActions.DrawGenericComboButton(StickyPair.UserData.UID, "LockRestraintSetForPairPermCombo", "Lock Set",
                WindowMenuWidth, GenericHelpers.NoMimicPadlockList, (padlock) => padlock.ToName(), false, disabled, true, Padlocks.None,
                FontAwesomeIcon.Lock, ImGuiComboFlags.None, (selected) => { _logger.LogDebug("Selected Padlock: " + selected, LoggerType.Permissions); },
                (onButtonPress) =>
                {
                    try
                    {
                        var newWardrobeData = lastWardrobeData.DeepClone();
                        if (newWardrobeData == null) throw new Exception("Wardrobe data is null, not sending");

                        var res = _permActions.PadlockVerifyLock<IPadlockable>(newWardrobeData, onButtonPress, PairPerms.ExtendedLockTimes,
                            PairPerms.OwnerLocks, PairPerms.DevotionalLocks, PairPerms.MaxAllowedRestraintTime);

                        if (res.Item1)
                        {
                            newWardrobeData.Padlock = onButtonPress.ToName();
                            newWardrobeData.Password = _permActions.Password;
                            newWardrobeData.Timer = UiSharedService.GetEndTimeUTC(_permActions.Timer);
                            newWardrobeData.Assigner = MainHub.UID;
                            _ = _apiHubMain.UserPushPairDataWardrobeUpdate(new(StickyPair.UserData, newWardrobeData, MainHub.PlayerUserData, DataUpdateKind.WardrobeRestraintLocked));
                            _logger.LogDebug("Locking Restraint Set with GagPadlock " + onButtonPress.ToString() + " to " + PairNickOrAliasOrUID, LoggerType.Permissions);
                            Opened = InteractionType.None;
                            // reset the password and timer
                        }
                        else
                        {
                            // Fire and forget, additional triggers replace previous.
                            _ = DisplayError(res.Item2);
                        }
                        _permActions.ResetInputs();
                    }
                    catch (Exception e) { _logger.LogError("Failed to push updated Wardrobe data: " + e.Message); }
                });
                // draw password field combos.
                _permActions.DisplayPadlockFields(selected);
            }
            ImGui.Separator();
        }

        // draw the unlock restraint set button.
        if (_uiShared.IconTextButton(FontAwesomeIcon.Unlock, "Unlock Restraint Set", WindowMenuWidth, true, unlockButtonDisabled))
        {
            Opened = Opened == InteractionType.UnlockRestraint ? InteractionType.None : InteractionType.UnlockRestraint;
        }
        UiSharedService.AttachToolTip("Unlocks the Restraint Set applied to " + PairNickOrAliasOrUID + ". Click to view options.");
        if (Opened is InteractionType.UnlockRestraint)
        {
            Padlocks selected = StickyPair.LastWardrobeData?.Padlock.ToPadlock() ?? Padlocks.None;
            float height = _permActions.ExpandLockHeightCheck(selected)
                ? 2 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y
                : ImGui.GetFrameHeight();
            using (var actionChild = ImRaii.Child("SetUnlockChild", new Vector2(WindowMenuWidth, height), false))
            {
                if (!actionChild) return;

                bool disabled = selected == Padlocks.None || !PairPerms.UnlockRestraintSets;
                // Draw combo
                float width = WindowMenuWidth - ImGui.GetStyle().ItemInnerSpacing.X - _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Unlock, "Unlock");
                using (ImRaii.Disabled(true))
                {
                    ImGui.SetNextItemWidth(width);
                    if (ImGui.BeginCombo("##DummyComboDisplayLockedRestraintSet", StickyPair.LastWardrobeData?.Padlock ?? "No Set Lock Active")) { ImGui.EndCombo(); }
                }
                ImUtf8.SameLineInner();
                if (_uiShared.IconTextButton(FontAwesomeIcon.Unlock, "Unlock", ImGui.GetContentRegionAvail().X, false, disabled))
                {
                    try
                    {
                        var newWardrobeData = lastWardrobeData.DeepClone();
                        if (newWardrobeData == null) throw new Exception("Wardrobe data is null, not sending");

                        var res = _permActions.PadlockVerifyUnlock<IPadlockable>(newWardrobeData, selected, PairPerms.OwnerLocks, PairPerms.DevotionalLocks);
                        if (res.Item1)
                        {
                            newWardrobeData.Padlock = selected.ToName();
                            newWardrobeData.Password = _permActions.Password;
                            newWardrobeData.Timer = DateTimeOffset.UtcNow;
                            newWardrobeData.Assigner = MainHub.UID;
                            _ = _apiHubMain.UserPushPairDataWardrobeUpdate(new(StickyPair.UserData, newWardrobeData, MainHub.PlayerUserData, DataUpdateKind.WardrobeRestraintUnlocked));
                            _logger.LogDebug("Unlocking Restraint Set with GagPadlock " + selected.ToString() + " to " + PairNickOrAliasOrUID, LoggerType.Permissions);
                            Opened = InteractionType.None;
                        }
                        else
                        {
                            // Fire and forget, additional triggers replace previous.
                            _ = DisplayError(res.Item2);
                        }
                        _permActions.ResetInputs();
                    }
                    catch (Exception e) { _logger.LogError("Failed to push updated Wardrobe data: " + e.Message); }
                };
                // draw password field combos.
                _permActions.DisplayPadlockFields(selected);
            }
            ImGui.Separator();
        }

        // draw the remove restraint set button.
        if (_uiShared.IconTextButton(FontAwesomeIcon.TimesCircle, "Remove Restraint Set", WindowMenuWidth, true, removeButtonDisabled))
        {
            Opened = Opened == InteractionType.RemoveRestraint ? InteractionType.None : InteractionType.RemoveRestraint;
        }
        UiSharedService.AttachToolTip("Removes the Restraint Set applied to " + PairNickOrAliasOrUID + ". Click to view options.");
        if (Opened is InteractionType.RemoveRestraint)
        {
            using (var actionChild = ImRaii.Child("SetRemoveChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeightWithSpacing()), false))
            {
                if (!actionChild) return;

                if (ImGui.Button("Remove Restraint", ImGui.GetContentRegionAvail()))
                {
                    try
                    {
                        var newWardrobeData = lastWardrobeData.DeepClone();
                        if (newWardrobeData == null) throw new Exception("Wardrobe data is null, not sending");
                        newWardrobeData.ActiveSetId = Guid.Empty;
                        newWardrobeData.ActiveSetEnabledBy = string.Empty;
                        _ = _apiHubMain.UserPushPairDataWardrobeUpdate(new(StickyPair.UserData, newWardrobeData, MainHub.PlayerUserData, DataUpdateKind.WardrobeRestraintDisabled));
                        _logger.LogDebug("Removing Restraint Set from " + PairNickOrAliasOrUID, LoggerType.Permissions);
                        Opened = InteractionType.None;
                    }
                    catch (Exception e) { _logger.LogError("Failed to push updated Wardrobe data: " + e.Message); }
                }
            }
        }
        ImGui.Separator();
    }
}
