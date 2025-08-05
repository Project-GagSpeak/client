using Buttplug.Client;using CkCommons;using CkCommons.Classes;using CkCommons.Gui;using CkCommons.Raii;using CkCommons.RichText;using Dalamud.Interface.Colors;using Dalamud.Interface.Utility;using Dalamud.Interface.Utility.Raii;using GagSpeak.Gui.MainWindow;using GagSpeak.Kinksters;using GagSpeak.PlayerClient;using GagSpeak.Services;using GagSpeak.Utils;using GagSpeak.WebAPI;using GagspeakAPI.Data.Permissions;using GagspeakAPI.Extensions;using ImGuiNET;using OtterGui;using OtterGui.Text;using OtterGui.Text.EndObjects;using System.Collections.Immutable;using static FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.DynamicEvent.Delegates;namespace GagSpeak.Gui.Components;
public class ClientPermsForKinkster{
    private readonly MainHub _hub;
    private readonly KinksterShockCollar _shockies;
    private readonly PresetLogicDrawer _presets;

    private static IconCheckboxEx EditAccessCheckbox = new(FAI.Pen, 0xFF00FF00, 0);
    private static IconCheckboxEx HardcoreCheckbox = new(FAI.UserLock, 0xFF00FF00, 0xFF0000FF);

    public ClientPermsForKinkster(MainHub hub, PresetLogicDrawer presets, KinksterShockCollar shockies)
    {
        _hub = hub;
        _shockies = shockies;
        _presets = presets;
    }

    // internal storage.
    private Dictionary<SPPID, string> _timespanCache = new();
    public void DrawPermissions(Kinkster kinkster, string dispName, float width)    {
        ImGuiUtil.Center($"Your Permissions for {dispName}");
        _presets.DrawPresetList(kinkster, width);
        ImGui.Separator();

        // Child area for scrolling.
        using var _ = CkRaii.Child("ClientPermsForKinkster", ImGui.GetContentRegionAvail(), wFlags: WFlags.NoScrollbar);
        if (OwnGlobals.Perms is not { } globals)            return;
        ImGui.TextUnformatted("Global Settings");
        using (ImRaii.Group())
        {
            DrawPermRow(kinkster, dispName, width, SPPID.ChatGarblerActive, globals.ChatGarblerActive, kinkster.OwnPermAccess.ChatGarblerActiveAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.ChatGarblerLocked, globals.ChatGarblerLocked, kinkster.OwnPermAccess.ChatGarblerLockedAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.GaggedNameplate, globals.GaggedNameplate, kinkster.OwnPermAccess.GaggedNameplateAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();        ImGui.TextUnformatted("Padlock Permissions");
        using (ImRaii.Group())
        {
            DrawPermRow(kinkster, dispName, width, SPPID.PermanentLocks, kinkster.OwnPerms.PermanentLocks, kinkster.OwnPermAccess.PermanentLocksAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.OwnerLocks, kinkster.OwnPerms.OwnerLocks, kinkster.OwnPermAccess.OwnerLocksAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.DevotionalLocks, kinkster.OwnPerms.DevotionalLocks, kinkster.OwnPermAccess.DevotionalLocksAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);        ImGui.Separator();        ImGui.TextUnformatted("Gag Permissions");
        using (ImRaii.Group())
        {
            DrawPermRow(kinkster, dispName, width, SPPID.GagVisuals, globals.GagVisuals, kinkster.OwnPermAccess.GagVisualsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.ApplyGags, kinkster.OwnPerms.ApplyGags, kinkster.OwnPermAccess.ApplyGagsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.LockGags, kinkster.OwnPerms.LockGags, kinkster.OwnPermAccess.LockGagsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.MaxGagTime, kinkster.OwnPerms.MaxGagTime, kinkster.OwnPermAccess.MaxGagTimeAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.UnlockGags, kinkster.OwnPerms.UnlockGags, kinkster.OwnPermAccess.UnlockGagsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.RemoveGags, kinkster.OwnPerms.RemoveGags, kinkster.OwnPermAccess.RemoveGagsAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);        ImGui.Separator();        ImGui.TextUnformatted("Restriction Permissions");
        using (ImRaii.Group())
        {
            DrawPermRow(kinkster, dispName, width, SPPID.RestrictionVisuals, globals.RestrictionVisuals, kinkster.OwnPermAccess.RestrictionVisualsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.ApplyRestrictions, kinkster.OwnPerms.ApplyRestrictions, kinkster.OwnPermAccess.ApplyRestrictionsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.LockRestrictions, kinkster.OwnPerms.LockRestrictions, kinkster.OwnPermAccess.LockRestrictionsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.MaxRestrictionTime, kinkster.OwnPerms.MaxRestrictionTime, kinkster.OwnPermAccess.MaxRestrictionTimeAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.UnlockRestrictions, kinkster.OwnPerms.UnlockRestrictions, kinkster.OwnPermAccess.UnlockRestrictionsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.RemoveRestrictions, kinkster.OwnPerms.RemoveRestrictions, kinkster.OwnPermAccess.RemoveRestrictionsAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);        ImGui.Separator();        ImGui.TextUnformatted("Restraint Set Permissions");
        using (ImRaii.Group())
        {
            DrawPermRow(kinkster, dispName, width, SPPID.RestraintSetVisuals, globals.RestraintSetVisuals, kinkster.OwnPermAccess.RestraintSetVisualsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.ApplyRestraintSets, kinkster.OwnPerms.ApplyRestraintSets, kinkster.OwnPermAccess.ApplyRestraintSetsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.ApplyLayers, kinkster.OwnPerms.ApplyLayers, kinkster.OwnPermAccess.ApplyLayersAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.ApplyLayersWhileLocked, kinkster.OwnPerms.ApplyLayersWhileLocked, kinkster.OwnPermAccess.ApplyLayersWhileLockedAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.LockRestraintSets, kinkster.OwnPerms.LockRestraintSets, kinkster.OwnPermAccess.LockRestraintSetsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.MaxRestraintTime, kinkster.OwnPerms.MaxRestraintTime, kinkster.OwnPermAccess.MaxRestraintTimeAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.UnlockRestraintSets, kinkster.OwnPerms.UnlockRestraintSets, kinkster.OwnPermAccess.UnlockRestraintSetsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.RemoveLayers, kinkster.OwnPerms.RemoveLayers, kinkster.OwnPermAccess.RemoveLayersAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.RemoveLayersWhileLocked, kinkster.OwnPerms.RemoveLayersWhileLocked, kinkster.OwnPermAccess.RemoveLayersWhileLockedAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.RemoveRestraintSets, kinkster.OwnPerms.RemoveRestraintSets, kinkster.OwnPermAccess.RemoveRestraintSetsAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);        ImGui.Separator();        ImGui.TextUnformatted("Puppeteer Permissions");
        using (ImRaii.Group())
        {
            DrawPermRow(kinkster, dispName, width, SPPID.PuppetPermSit, kinkster.OwnPerms.PuppetPerms, kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Sit);
            DrawPermRow(kinkster, dispName, width, SPPID.PuppetPermEmote, kinkster.OwnPerms.PuppetPerms, kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Emotes);
            DrawPermRow(kinkster, dispName, width, SPPID.PuppetPermAlias, kinkster.OwnPerms.PuppetPerms, kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Alias);
            DrawPermRow(kinkster, dispName, width, SPPID.PuppetPermAll, kinkster.OwnPerms.PuppetPerms, kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.All);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);        ImGui.Separator();        ImGui.TextUnformatted("Moodles Permissions");
        using (ImRaii.Group())
        {
            DrawPermRow(kinkster, dispName, width, SPPID.ApplyPositive, kinkster.OwnPerms.MoodlePerms, kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PositiveStatusTypes);
            DrawPermRow(kinkster, dispName, width, SPPID.ApplyNegative, kinkster.OwnPerms.MoodlePerms, kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.NegativeStatusTypes);
            DrawPermRow(kinkster, dispName, width, SPPID.ApplySpecial, kinkster.OwnPerms.MoodlePerms, kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.SpecialStatusTypes);
            DrawPermRow(kinkster, dispName, width, SPPID.ApplyOwnMoodles, kinkster.OwnPerms.MoodlePerms, kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PairCanApplyTheirMoodlesToYou);
            DrawPermRow(kinkster, dispName, width, SPPID.ApplyPairsMoodles, kinkster.OwnPerms.MoodlePerms, kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PairCanApplyYourMoodlesToYou);
            DrawPermRow(kinkster, dispName, width, SPPID.MaxMoodleTime, kinkster.OwnPerms.MaxMoodleTime, kinkster.OwnPermAccess.MaxMoodleTimeAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.PermanentMoodles, kinkster.OwnPerms.MoodlePerms, kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PermanentMoodles);
            DrawPermRow(kinkster, dispName, width, SPPID.RemoveMoodles, kinkster.OwnPerms.MoodlePerms, kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.RemovingMoodles);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);        ImGui.Separator();

        ImGui.TextUnformatted("Miscellaneous Permissions");
        using (ImRaii.Group())
        {
            DrawPermRow(kinkster, dispName, width, SPPID.HypnosisMaxTime, kinkster.OwnPerms.MaxHypnosisTime, kinkster.OwnPermAccess.HypnosisMaxTimeAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.HypnosisEffect, kinkster.OwnPerms.HypnoEffectSending, kinkster.OwnPermAccess.HypnosisSendingAllowed);        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();
        ImGui.TextUnformatted("Toybox Permissions");
        using (ImRaii.Group())
        {
            DrawPermRow(kinkster, dispName, width, SPPID.PatternStarting, kinkster.OwnPerms.ExecutePatterns, kinkster.OwnPermAccess.ExecutePatternsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.PatternStopping, kinkster.OwnPerms.StopPatterns, kinkster.OwnPermAccess.StopPatternsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.AlarmToggling, kinkster.OwnPerms.ToggleAlarms, kinkster.OwnPermAccess.ToggleAlarmsAllowed);
            DrawPermRow(kinkster, dispName, width, SPPID.TriggerToggling, kinkster.OwnPerms.ToggleTriggers, kinkster.OwnPermAccess.ToggleTriggersAllowed);        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();

        // Probably a good idea to add a warning here on a popup or something idk.
        ImGui.TextUnformatted("Hardcore Permissions");
        ImUtf8.SameLineInner();
        CkGui.HoverIconText(FAI.QuestionCircle, ImGuiColors.TankBlue.ToUint(), ImGui.GetColorU32(ImGuiCol.TextDisabled));
        if (ImGui.IsItemHovered())
        {
            using (UiFontService.UidFont.Push())
            {
                CkGui.AttachToolTip($"--COL--IMPORTANT:--COL-- Once in hardcore mode, you can only change EditAccess for {dispName}." +
                    $"--NL--{dispName} will have control over any permissions they have edit access to instead." +
                    "--NL--Be sure you are ok with this before enabling!", color: ImGuiColors.DalamudRed);
            }
        }

        // True when they wish to enter hardcore mode.
        if (DrawHardcoreModeRow(kinkster, dispName, width))
            ImGui.OpenPopup("Confirm Hardcore");

        DrawHcBasicPerm(kinkster, dispName, width, SPPID.GarbleChannelEditing, kinkster.OwnPerms.AllowGarbleChannelEditing);
        DrawHcBasicPerm(kinkster, dispName, width, SPPID.HypnoticImage, kinkster.OwnPerms.AllowHypnoImageSending);
        DrawHcStatePerm(kinkster, dispName, width, SPPID.LockedFollowing, nameof(GlobalPerms.LockedFollowing), globals.LockedFollowing, kinkster.OwnPerms.AllowLockedFollowing);
        DrawHcEmotePerm(kinkster, dispName, width, SPPID.LockedEmoteState, nameof(GlobalPerms.LockedEmoteState), globals.LockedEmoteState, kinkster.OwnPerms.AllowLockedSitting, kinkster.OwnPerms.AllowLockedEmoting);
        DrawHcStatePerm(kinkster, dispName, width, SPPID.IndoorConfinement, nameof(GlobalPerms.IndoorConfinement), globals.IndoorConfinement, kinkster.OwnPerms.AllowIndoorConfinement);
        DrawHcStatePerm(kinkster, dispName, width, SPPID.Imprisonment, nameof(GlobalPerms.Imprisonment), globals.Imprisonment, kinkster.OwnPerms.AllowImprisonment);
        DrawHcStatePerm(kinkster, dispName, width, SPPID.ChatBoxesHidden, nameof(GlobalPerms.ChatBoxesHidden), globals.ChatBoxesHidden, kinkster.OwnPerms.AllowHidingChatBoxes);
        DrawHcStatePerm(kinkster, dispName, width, SPPID.ChatInputHidden, nameof(GlobalPerms.ChatInputHidden), globals.ChatInputHidden, kinkster.OwnPerms.AllowHidingChatInput);
        DrawHcStatePerm(kinkster, dispName, width, SPPID.ChatInputBlocked, nameof(GlobalPerms.ChatInputBlocked), globals.ChatInputBlocked, kinkster.OwnPerms.AllowChatInputBlocking);
        ImGui.Separator();

        // Hardcore confirm modal.
        ShowConfirmHardcoreIfValid(kinkster, dispName);

        ImGui.TextUnformatted("Shock Collar Permissions");
        if (OwnGlobals.Perms is not { } p || !p.HasValidShareCode())
            CkGui.ColorTextCentered("Must have a valid Global ShareCode first!", ImGuiColors.DalamudRed);
        else
            _shockies.DrawClientPermsForKinkster(width, kinkster, dispName);    }

    private void DrawPermRow(Kinkster kinkster, string dispName, float width, SPPID perm, bool current, bool canEdit)
        => DrawPermRowCommon(kinkster, dispName, width, perm, current, canEdit, () => !current);

    private void DrawPermRow(Kinkster kinkster, string dispName, float width, SPPID perm, PuppetPerms current, PuppetPerms canEdit, PuppetPerms editFlag)
    {
        var isFlagSet = (current & editFlag) == editFlag;
        DrawPermRowCommonEnum(kinkster, dispName, width, perm, isFlagSet, canEdit.HasAny(editFlag), () => current ^ editFlag, () => canEdit ^ editFlag);
    }

    private void DrawPermRow(Kinkster kinkster, string dispName, float width, SPPID perm, MoodlePerms current, MoodlePerms canEdit, MoodlePerms editFlag)
    {
        var isFlagSet = (current & editFlag) == editFlag;
        DrawPermRowCommonEnum(kinkster, dispName, width, perm, isFlagSet, canEdit.HasAny(editFlag), () => current ^ editFlag, () => canEdit ^ editFlag);
    }

    private void DrawPermRow(Kinkster kinkster, string dispName, float width, SPPID perm, TimeSpan current, bool canEdit)
    {
        using var disabled = ImRaii.Disabled(kinkster.OwnPerms.InHardcore);

        var inputTxtWidth = width * .4f;
        var str = _timespanCache.TryGetValue(perm, out var value) ? value : current.ToGsRemainingTime();
        var data = ClientPermData[perm];
        var editCol = canEdit ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();

        if (CkGui.IconInputText(data.IconYes, data.PermLabel, "0d0h0m0s", ref str, 32, inputTxtWidth, true, kinkster.OwnPerms.InHardcore))
        {
            if (str != current.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(str, out var newTime))
            {
                var ticks = (ulong)newTime.Ticks;
                var res = perm.ToPermValue();

                // Assign the blocking task if allowed.
                if (!res.name.IsNullOrEmpty() && res.type is PermissionType.PairPerm)
                {
                    Svc.Logger.Information($"Setting {dispName} {data.PermLabel} to {ticks} ticks for {kinkster.UserData.AliasOrUID}.");
                    UiService.SetUITask(async () => await PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, res.name, ticks));
                }
            }
            _timespanCache.Remove(perm);
        }
        CkGui.AttachToolTip($"How long of a timer {dispName} can put on your padlocks.", kinkster.OwnPerms.InHardcore);

        ImGui.SameLine(width - ImGui.GetFrameHeight());
        if (EditAccessCheckbox.Draw($"##{perm}", canEdit, out var newVal) && canEdit != newVal)
            UiService.SetUITask(async () => await PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), newVal));
        CkGui.AttachToolTip($"{dispName} {(canEdit ? "can" : "can not")} change your {data.PermLabel} setting.", kinkster.OwnPerms.InHardcore);
    }

    // optimize later and stuff.
    private void DrawPermRowCommon<T>(Kinkster kinkster, string dispName, float width, SPPID perm, bool current, bool canEdit, Func<T> newStateFunc)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0);
        using var dis = ImRaii.Disabled(kinkster.OwnPerms.InHardcore);
        var data = ClientPermData[perm];
        var buttonW = width - (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X);
        var editCol = canEdit ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();
        var pos = ImGui.GetCursorScreenPos();

        // change to ckgui for disabled?
        if (ImGui.Button("##pair" + perm, new Vector2(buttonW, ImGui.GetFrameHeight())))
        {
            Svc.Logger.Information($"Setting {dispName} {data.PermLabel} to {(current ? "false" : "true")} for {kinkster.UserData.AliasOrUID}.");
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;

            Svc.Logger.Information($"Setting {dispName} {res.name} to {newState} for {kinkster.UserData.AliasOrUID}.");

            UiService.SetUITask(async () =>
            {
                switch (res.type)
                {
                    case PermissionType.Global: await _hub.UserChangeOwnGlobalPerm(res.name, newState); break;
                    case PermissionType.PairPerm: await PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.PairPerms, res.name, newState); break;
                    default: break;
                }
            });
        }

        ImGui.SameLine();
        if (EditAccessCheckbox.Draw($"##{perm}", canEdit, out var newVal) && canEdit != newVal)
            UiService.SetUITask(async () => await PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), newVal));
        CkGui.AttachToolTip($"{dispName} {(canEdit ? "can" : "can not")} change your {data.PermLabel} setting.", kinkster.OwnPerms.InHardcore);

        // draw inside of the button.
        ImGui.SetCursorScreenPos(pos);
        PrintButtonRichText(data, current);
        CkGui.AttachToolTip(data.IsGlobal
            ? $"Your {data.PermLabel} {data.JoinWord} {(current ? data.AllowedStr : data.BlockedStr)}. (Globally)"
            : $"You {(current ? data.AllowedStr : data.BlockedStr)} {dispName} {(current ? data.PairAllowedTT : data.PairBlockedTT)}", kinkster.OwnPerms.InHardcore);
    }

    private void DrawPermRowCommonEnum<T>(Kinkster kinkster, string dispName, float width, SPPID perm, bool current, bool canEdit, Func<T> newStateFunc, Func<T> newEditStateFunc)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0);
        using var dis = ImRaii.Disabled(kinkster.OwnPerms.InHardcore);
        var data = ClientPermData[perm];
        var buttonW = width - (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X);
        var editCol = canEdit ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();
        var pos = ImGui.GetCursorScreenPos();

        // change to ckgui for disabled?
        if (ImGui.Button("##pair" + perm, new Vector2(buttonW, ImGui.GetFrameHeight())))
        {
            Svc.Logger.Information($"Setting {dispName} {data.PermLabel} to {(current ? "false" : "true")} for {kinkster.UserData.AliasOrUID}.");
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;

            Svc.Logger.Information($"Setting {dispName} {res.name} to {newState} for {kinkster.UserData.AliasOrUID}.");

            UiService.SetUITask(async () =>
            {
                switch (res.type)
                {
                    case PermissionType.Global: await _hub.UserChangeOwnGlobalPerm(res.name, newState); break;
                    case PermissionType.PairPerm: await PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.PairPerms, res.name, newState); break;
                    case PermissionType.PairAccess: await PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, res.name, newState); break;
                    default: break;
                }
            });
        }

        ImGui.SameLine();
        if (EditAccessCheckbox.Draw($"##{perm}", canEdit, out var newVal) && canEdit != newVal)
        {
            var newEditVal = newEditStateFunc();
            UiService.SetUITask(async () => await PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), newEditVal!));
        }
        CkGui.AttachToolTip($"{dispName} {(canEdit ? "can" : "can not")} change your {data.PermLabel} setting.", kinkster.OwnPerms.InHardcore);

        // draw inside of the button.
        ImGui.SetCursorScreenPos(pos);
        PrintButtonRichText(data, current);
        CkGui.AttachToolTip(data.IsGlobal
            ? $"Your {data.PermLabel} {data.JoinWord} {(current ? data.AllowedStr : data.BlockedStr)}. (Globally)"
            : $"You {(current ? data.AllowedStr : data.BlockedStr)} {dispName} {(current ? data.PairAllowedTT : data.PairBlockedTT)}", kinkster.OwnPerms.InHardcore);
    }


    private void PrintButtonRichText(PermDataClient pdp, bool current)
    {
        using var _ = ImRaii.Group();
        if (pdp.IsGlobal)
        {
            CkGui.FramedIconText(current ? pdp.IconYes : pdp.IconNo);
            CkGui.TextFrameAlignedInline($"Your {pdp.PermLabel} {pdp.JoinWord} ");
            ImGui.SameLine(0, 0);
            CkGui.ColorTextFrameAligned(current ? pdp.AllowedStr : pdp.BlockedStr, current ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
            ImGui.SameLine(0, 0);
            ImUtf8.TextFrameAligned(".");
        }
        else
        {
            CkGui.FramedIconText(current ? pdp.IconYes : pdp.IconNo);
            CkGui.TextFrameAlignedInline("You ");
            ImGui.SameLine(0, 0);
            CkGui.ColorTextFrameAligned(current ? pdp.AllowedStr : pdp.BlockedStr, current ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
            ImGui.SameLine(0, 0);
            ImUtf8.TextFrameAligned($" {pdp.PermLabel}.");
        }
    }

    // True if the user wished to enter hardcore mode.
    private bool DrawHardcoreModeRow(Kinkster k, string name, float width)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0);

        var curState = k.OwnPerms.InHardcore;
        var devotionalState = k.OwnPerms.DevotionalLocks;
        var buttonW = width - (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X);
        var iconCol = devotionalState ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();

        var pos = ImGui.GetCursorScreenPos();
        using (ImRaii.Disabled(k.OwnPerms.InHardcore))
        {
            if (ImGui.Button($"##HardcoreToggle", new Vector2(buttonW, ImGui.GetFrameHeight())))
            {
                // return true to open the popup modal if we want to turn it on.
                if (!k.OwnPerms.InHardcore)
                    return true;
                // otherwise, just turn it off. (temporary until safeword is embedded)
                Svc.Logger.Information($"Setting Hardcore mode to false for {name} ({k.UserData.AliasOrUID}).");
                UiService.SetUITask(async () => await PermissionHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, nameof(PairPerms.InHardcore), false));
            }

            // draw inside of the button.
            ImGui.SetCursorScreenPos(pos);
            using (ImRaii.Group())
            {
                CkGui.FramedIconText(curState ? FAI.Lock : FAI.Unlock);
                CkGui.TextFrameAlignedInline("Hardcore is ");
                ImGui.SameLine(0, 0);
                CkGui.ColorTextFrameAligned(curState ? "enabled" : "disabled", curState ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint());
                ImGui.SameLine(0, 0);
                CkGui.TextFrameAlignedInline($"for {name}.");
            }
        }

        ImGui.SameLine(width - ImGui.GetFrameHeight());
        if (HardcoreCheckbox.Draw($"##DevoLocks{name}", devotionalState, out var newVal) && devotionalState != newVal)
            UiService.SetUITask(async () => await PermissionHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, nameof(PairPerms.DevotionalLocks), !devotionalState));
        CkGui.AttachToolTip(devotionalState
            ? $"Any Hardcore action by {name} will be --COL--pairlocked--COL----NL--This means that only {name} can disable it."
            : $"Anyone you are in Hardcore for can undo Hardcore interactions from --COL--{name}--COL--", color: CkColor.VibrantPink.Vec4());
        return false;
    }

    private void ShowConfirmHardcoreIfValid(Kinkster k, string name)
    {
        // prevent rendering unessisary styles or calculations if it is not open.
        if (!ImGui.IsPopupOpen("Confirm Hardcore"))
            return;
        
        // center the hardcore window.
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Always, new Vector2(0.5f));
        // set the size of the popup.
        var size = new Vector2(600f, 345f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextWindowSize(size);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f)
            .Push(ImGuiStyleVar.WindowRounding, 12f);
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.DalamudGrey2);

        using var pop = ImRaii.Popup("Confirm Hardcore", WFlags.Modal | WFlags.NoResize | WFlags.NoScrollbar | WFlags.NoMove);
        if (!pop)
            return;

        using (ImRaii.Group())
        {
            CkGui.FontTextCentered("CAUTIONARY WARNING", UiFontService.GagspeakTitleFont, ImGuiColors.DalamudRed);

            CkGui.SeparatorColored(size.X, col: ImGuiColors.DalamudRed.ToUint());

            CkGui.OutlinedFont("In Hardcore Mode:", ImGuiColors.DalamudOrange, CkColor.ElementSplit.Vec4(), 2);
            
            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.TextInline("You can no longer change permissions or edit access for");
            CkGui.ColorTextInline(name, CkColor.VibrantPink.Uint());

            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.ColorTextInline(name, CkColor.VibrantPink.Uint());
            CkGui.TextInline("can change non-hardcore permissions with edit access.");
            
            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.TextInline("You can set which Hardcore Interactions");
            CkGui.ColorTextInline(name, CkColor.VibrantPink.Uint());
            CkGui.TextInline("can use.");
            CkGui.ColorTextInline("(Only you can change this)", ImGuiColors.ParsedGrey);

            CkGui.SeparatorColored(size.X - ImGui.GetStyle().WindowPadding.X, col: ImGuiColors.DalamudRed.ToUint());
            CkGui.OutlinedFont("Recommendations:", ImGuiColors.DalamudOrange, CkColor.ElementSplit.Vec4(), 2);
            
            CkGui.IconText(FAI.AngleDoubleRight);
            ImGui.SameLine();
            CkGui.TextWrapped($"Give {name} EditAccess to perms you are OK with them controlling, " +
                "and enable permissions without access as fit for your dynamics limits.");

            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.ColorTextInline("Power Control Adjustment", CkColor.VibrantPink.Uint());
            ImGui.SameLine(0, 1);
            CkGui.HoverIconText(FAI.QuestionCircle, ImGuiColors.TankBlue.ToUint(), ImGui.GetColorU32(ImGuiCol.TextDisabled));
            CkGui.AttachToolTip($"Provides a 5 second window for you to change permissions and edit access for {name}.");
            CkGui.TextInline($"can modify your dynamic limits while in Hardcore.");

            CkGui.SeparatorColored(size.X - ImGui.GetStyle().WindowPadding.X, col: ImGuiColors.DalamudRed.ToUint());

            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.TextInline("Hardcore Safeword:");
            CkGui.ColorTextInline("/safewordhardcore KINKSTERUID", ImGuiColors.ParsedGold);
            CkGui.TextInline("(this has a 10minute CD).");

            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.TextInline($"If ChatInput is blocked, use:");
            CkGui.ColorTextInline("CTRL + ALT + BACKSPACE", ImGuiColors.ParsedGold);
            CkGui.TextInline("('Fuck, go back')");
        }
        string yesButton = $"Enter Hardcore for {name}";
        string noButton = "Oh my, take me back!";
        var yesSize = ImGuiHelpers.GetButtonSize(yesButton);
        var noSize = ImGuiHelpers.GetButtonSize(noButton);
        var offsetX = (size.X - (yesSize.X + noSize.X + ImGui.GetStyle().ItemSpacing.X) - ImGui.GetStyle().WindowPadding.X * 2) / 2;
        CkGui.SeparatorSpaced();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);
        if (ImGui.Button(yesButton))
        {
            Svc.Logger.Information($"Entering Hardcore Mode for {name} ({k.UserData.AliasOrUID})");
            UiService.SetUITask(async () => await PermissionHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, nameof(PairPerms.InHardcore), !k.OwnPerms.InHardcore));
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button(noButton))
        {
            Svc.Logger.Information($"Cancelled Hardcore Mode for {name} ({k.UserData.AliasOrUID})");
            ImGui.CloseCurrentPopup();
        }
    }

    private void DrawHcBasicPerm(Kinkster k, string name, float width, SPPID perm, bool current)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0);
        var data = ClientPermData[perm];
        var editCol = current ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();
        var pos = ImGui.GetCursorScreenPos();

        // change to ckgui for disabled?
        if (ImGui.Button($"##pair-{perm}", new Vector2(width, ImGui.GetFrameHeight())))
        {
            var res = perm.ToPermValue();
            UiService.SetUITask(async () => await PermissionHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, res.name, !current));
        }
        CkGui.AttachToolTip($"You {(current ? data.AllowedStr : data.BlockedStr)} {name} {(current ? data.PairAllowedTT : data.PairBlockedTT)}");

        // go back and draw inside the dummy.
        ImGui.SetCursorScreenPos(pos);
        CkGui.FramedIconText(current ? data.IconYes : data.IconNo);
        CkGui.TextFrameAlignedInline("You ");
        ImGui.SameLine(0, 0);
        CkGui.ColorTextFrameAligned(current ? data.AllowedStr : data.BlockedStr, current ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
        ImGui.SameLine(0, 0);
        ImUtf8.TextFrameAligned($" {data.PermLabel}.");
    }

    private void DrawHcStatePerm(Kinkster k, string name, float width, SPPID perm, string permName, string current, bool allowanceState)
    {
        using var butt = ImRaii.PushColor(ImGuiCol.Button, 0);
        var data = ClientHcPermData[perm];
        var isActive = !string.IsNullOrEmpty(current);
        var isPairlocked = GlobalPermsEx.IsDevotional(current);
        var stateLocker = isActive ? GlobalPermsEx.PermEnactor(current) : string.Empty;
        var editCol = isActive ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();
        var pos = ImGui.GetCursorScreenPos();

        ImGui.Dummy(new Vector2(width - (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X), ImGui.GetFrameHeight()));

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled(isActive))
            if (EditAccessCheckbox.Draw($"##{permName}", allowanceState, out var newVal) && allowanceState != newVal)
                UiService.SetUITask(async () => await PermissionHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, perm.ToPermAccessValue(), !allowanceState));
        CkGui.AttachToolTip(isActive ? "You are helpless to change this while active!" : allowanceState 
                ? $"Allowing {name} {data.ToggleTrueSuffixTT}." : $"Preventing {name} {data.ToggleFalseSuffixTT}.");

        // go back and draw inside the dummy.
        ImGui.SetCursorScreenPos(pos);

        using var _ = ImRaii.PushStyle(ImGuiStyleVar.Alpha, .5f);
        CkGui.FramedIconText(isActive ? data.IconT : data.IconF);
        if (isActive)
        {
            CkGui.ColorTextFrameAligned(isActive ? stateLocker.AsAnonKinkster() : "UNK KINKSTER", editCol);
            ImGui.SameLine(0, 0);
            CkGui.TextFrameAlignedInline(data.EnabledPreText);
            ImGui.SameLine(0, 0);
            ImUtf8.TextFrameAligned(".");
        }
        else
        {
            CkGui.TextFrameAlignedInline($"{data.DisabledText}.");
        }

    }

    public void DrawHcEmotePerm(Kinkster k, string name, float width, SPPID perm, string permName, string current, bool allowBasic, bool allowAll)
    {
        using var butt = ImRaii.PushColor(ImGuiCol.Button, 0);
        var data = ClientHcPermData[perm];
        var isActive = !string.IsNullOrEmpty(current);
        var isPairlocked = GlobalPermsEx.IsDevotional(current);
        var stateLocker = isActive ? GlobalPermsEx.PermEnactor(current) : string.Empty;
        var editCol = isActive ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();


        // change to ckgui for disabled?
        var pos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(width - ((ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X) * 2), ImGui.GetFrameHeight()));
        CkGui.AttachToolTip($"{permName}'s current locked emote status.");

        // draw out the checkboxessss
        ImUtf8.SameLineInner();
        using (ImRaii.Disabled(isActive))
            if (EditAccessCheckbox.Draw($"##EmBasic{permName}", allowBasic, out var newVal) && allowBasic != newVal)
                UiService.SetUITask(PermissionHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, SPPID.LockedEmoteState.ToPermAccessValue(), newVal));
        CkGui.AttachToolTip(isActive ? "Helpless to change this while performing a locked emote!" : allowBasic
                ? $"{name} can force you to Groundsit, Sit, or Cyclepose." : $"Preventing {name} from placing you in a locked emote state.");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled(isActive))
            if (EditAccessCheckbox.Draw($"##EmFull{permName}", allowAll, out var newVal2) && allowAll != newVal2)
                UiService.SetUITask(PermissionHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, SPPID.LockedEmoteState.ToPermAccessValue(true), newVal2));
        CkGui.AttachToolTip(isActive ? "Helpless to change this while performing a locked emote!" : allowBasic
                ? $"{name} can force you to perform any looping emote." : $"Preventing looped emotes from being forced by {name}.");

        // go back and draw inside the dummy.
        ImGui.SetCursorScreenPos(pos);
        using var _ = ImRaii.PushStyle(ImGuiStyleVar.Alpha, .5f);
        CkGui.FramedIconText(isActive ? data.IconT : data.IconF);
        if (isActive)
        {
            CkGui.ColorTextFrameAligned(isActive ? stateLocker.AsAnonKinkster() : "UNK KINKSTER", editCol);
            ImGui.SameLine(0, 0);
            CkGui.TextFrameAlignedInline(data.EnabledPreText);
            ImGui.SameLine(0, 0);
            ImUtf8.TextFrameAligned(".");
        }
        else
        {
            CkGui.TextFrameAlignedInline($"{data.DisabledText}.");
        }
    }

    public record PermDataClient(bool IsGlobal, FAI IconYes, FAI IconNo, string AllowedStr, string BlockedStr, string PermLabel, string JoinWord, string PairAllowedTT, string PairBlockedTT);

    /// <summary> The Cache of PermissionData for each permission in the Gear Setting Menu. </summary>
    private readonly ImmutableDictionary<SPPID, PermDataClient> ClientPermData = ImmutableDictionary<SPPID, PermDataClient>.Empty
        .Add(SPPID.ChatGarblerActive,     new PermDataClient(true, FAI.MicrophoneSlash,       FAI.Microphone,    "active",        "inactive",    "Chat Garbler", "is",       string.Empty, string.Empty))
        .Add(SPPID.ChatGarblerLocked,     new PermDataClient(true, FAI.Key,                   FAI.UnlockAlt,     "locked",        "unlocked",    "Chat Garbler", "is",       string.Empty, string.Empty))
        .Add(SPPID.GaggedNameplate,       new PermDataClient(true, FAI.IdCard,                FAI.Ban,           "enabled",       "disabled",    "GagPlates", "are",         string.Empty, string.Empty))

        .Add(SPPID.PermanentLocks,        new PermDataClient(false, FAI.Infinity,              FAI.Ban,           "allow",       "prevent",      "Permanent Locks", "are", "to use padlocks without timers.", "from using padlocks without timers."))
        .Add(SPPID.OwnerLocks,            new PermDataClient(false, FAI.UserLock,              FAI.Ban,           "allow",       "prevent",      "Owner Locks", "are", "to use owner padlocks.", "from using owner padlocks."))
        .Add(SPPID.DevotionalLocks,       new PermDataClient(false, FAI.UserLock,              FAI.Ban,           "allow",       "prevent",      "Devotional Locks", "are", "to use devotional padlocks.", "from using devotional padlocks."))

        .Add(SPPID.GagVisuals,            new PermDataClient(true, FAI.Surprise,              FAI.Ban,           "enabled",       "disabled",        "Gag Visuals", "are", string.Empty, string.Empty))
        .Add(SPPID.ApplyGags,             new PermDataClient(false, FAI.Mask,                  FAI.Ban,           "allow",       "prevent",      "applying Gags", "are", "to apply gags.", "from applying gags"))
        .Add(SPPID.LockGags,              new PermDataClient(false, FAI.Lock,                  FAI.Ban,           "allow",       "prevent",      "locking Gags", "are", "to lock gags",                     "from locking gags"))
        .Add(SPPID.MaxGagTime,            new PermDataClient(false, FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Gag Time", "are", string.Empty,                       string.Empty))
        .Add(SPPID.UnlockGags,            new PermDataClient(false, FAI.Key,                   FAI.Ban,           "allow",       "prevent",      "unlocking Gags", "are", "to unlock gags",                   "from unlocking gags"))
        .Add(SPPID.RemoveGags,            new PermDataClient(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",      "removing Gags", "are", "to remove gags",                   "from removing gags"))

        .Add(SPPID.RestrictionVisuals,    new PermDataClient(true,  FAI.Tshirt,                FAI.Ban,           "enabled",       "disabled",        "Restriction Visuals", "are", "to enable restriction visuals",    "from enabling restriction visuals"))
        .Add(SPPID.ApplyRestrictions,     new PermDataClient(false, FAI.Handcuffs,             FAI.Ban,           "allow",       "prevent",      "Applying Restrictions", "are", "to apply restrictions",            "from applying restrictions"))
        .Add(SPPID.LockRestrictions,      new PermDataClient(false, FAI.Lock,                  FAI.Ban,           "allow",       "prevent",      "Locking Restrictions", "are", "to lock restrictions",             "from locking restrictions"))
        .Add(SPPID.MaxRestrictionTime,    new PermDataClient(false, FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Restriction Time", string.Empty, string.Empty,                       string.Empty))
        .Add(SPPID.UnlockRestrictions,    new PermDataClient(false, FAI.Key,                   FAI.Ban,           "allow",       "prevent",      "Unlocking Restrictions", "are", "to unlock restrictions",           "from unlocking restrictions"))
        .Add(SPPID.RemoveRestrictions,    new PermDataClient(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",      "Removing Restrictions", "are", "to remove restrictions",           "from removing restrictions"))

        .Add(SPPID.RestraintSetVisuals,   new PermDataClient(true, FAI.Tshirt,                FAI.Ban,           "enabled",       "disabled",    "Restraint Visuals", "are", "to enable restraint visuals",      "from enabling restraint visuals"))
        .Add(SPPID.ApplyRestraintSets,    new PermDataClient(false, FAI.Handcuffs,             FAI.Ban,           "allow",       "prevent",      "applying restraints", "are", "to apply restraints",              "from applying restraints"))
        .Add(SPPID.ApplyLayers,           new PermDataClient(false, FAI.LayerGroup,            FAI.Ban,           "allow",       "prevent",      "adding layers", "are", "to apply layers",                  "from applying layers"))
        .Add(SPPID.ApplyLayersWhileLocked,new PermDataClient(false, FAI.LayerGroup,            FAI.Ban,           "allow",       "prevent",      "adding layers when locked", "are", "to apply layers while locked",   "from applying layers while locked"))
        .Add(SPPID.LockRestraintSets,     new PermDataClient(false, FAI.Lock,                  FAI.Ban,           "allow",       "prevent",      "locking restraints", "are", "to lock restraints",               "from locking restraints"))
        .Add(SPPID.MaxRestraintTime,      new PermDataClient(false, FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty, "Max Restraint Time", string.Empty,      string.Empty,                       string.Empty))
        .Add(SPPID.UnlockRestraintSets,   new PermDataClient(false, FAI.Key,                   FAI.Ban,           "allow",       "prevent",      "unlocking restraints", "are", "to unlock restraints",             "from unlocking restraints"))
        .Add(SPPID.RemoveLayers,          new PermDataClient(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",      "layer removal", "are", "to remove layers",                 "from removing layers"))
        .Add(SPPID.RemoveLayersWhileLocked,new PermDataClient(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",     "layer removal when locked", "are", "to remove layers while locked", "from removing layers while locked"))
        .Add(SPPID.RemoveRestraintSets,   new PermDataClient(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",      "removing restraints", "are", "to remove restraints",             "from removing restraints"))

        .Add(SPPID.PuppetPermSit,         new PermDataClient(false, FAI.Chair,                 FAI.Ban,           "allow",       "prevent",      "sit requests", "are", "to invoke sit requests",           "from invoking sit requests"))
        .Add(SPPID.PuppetPermEmote,       new PermDataClient(false, FAI.Walking,               FAI.Ban,           "allow",       "prevent",      "emote requests", "are", "to invoke emote requests",         "from invoking emote requests"))
        .Add(SPPID.PuppetPermAlias,       new PermDataClient(false, FAI.Scroll,                FAI.Ban,           "allow",       "prevent",      "alias requests", "are", "to invoke alias requests",         "from invoking alias requests"))
        .Add(SPPID.PuppetPermAll,         new PermDataClient(false, FAI.CheckDouble,           FAI.Ban,           "allow",       "prevent",      "all requests", "are", "to invoke all requests",           "from invoking all requests"))

        .Add(SPPID.ApplyPositive,         new PermDataClient(false, FAI.SmileBeam,             FAI.Ban,           "allow",       "prevent",      "positive Moodles", "are", "to apply positive moodles",        "from applying positive moodles"))
        .Add(SPPID.ApplyNegative,         new PermDataClient(false, FAI.FrownOpen,             FAI.Ban,           "allow",       "prevent",      "negative Moodles", "are", "to apply negative moodles",        "from applying negative moodles"))
        .Add(SPPID.ApplySpecial,          new PermDataClient(false, FAI.WandMagicSparkles,     FAI.Ban,           "allow",       "prevent",      "special Moodles", "are", "to apply special moodles",         "from applying special moodles"))
        .Add(SPPID.ApplyPairsMoodles,     new PermDataClient(false, FAI.PersonArrowUpFromLine, FAI.Ban,           "allow",       "prevent",      "applying your Moodles", "are", "to apply your moodles",            "from applying your moodles"))
        .Add(SPPID.ApplyOwnMoodles,       new PermDataClient(false, FAI.PersonArrowDownToLine, FAI.Ban,           "allow",       "prevent",      "applying their Moodles", "are", "to apply their moodles",           "from applying their moodles"))
        .Add(SPPID.MaxMoodleTime,         new PermDataClient(false, FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty, "Max Moodle Time", string.Empty,         string.Empty,                       string.Empty))
        .Add(SPPID.PermanentMoodles,      new PermDataClient(false, FAI.Infinity,              FAI.Ban,           "allow",       "prevent",      "permanent Moodles", "are", "to apply permanent moodles",       "from applying permanent moodles"))
        .Add(SPPID.RemoveMoodles,         new PermDataClient(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",      "removing Moodles", "are", "to remove moodles",                "from removing moodles"))

        .Add(SPPID.HypnosisMaxTime,       new PermDataClient(false, FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty, "Max Hypnosis Time", string.Empty, string.Empty,                       string.Empty))
        .Add(SPPID.HypnosisEffect,        new PermDataClient(false, FAI.CameraRotate,          FAI.Ban,           "allow",       "prevent",      "Hypnotic Effect Sending", "are", "to send hypnotic effects",         "from sending hypnotic effects"))

        .Add(SPPID.PatternStarting,       new PermDataClient(false, FAI.Play,                  FAI.Ban,           "allow",       "prevent",      "Pattern Starting", "is", "to start patterns",                "from starting patterns"))
        .Add(SPPID.PatternStopping,       new PermDataClient(false, FAI.Stop,                  FAI.Ban,           "allow",       "prevent",      "Pattern Stopping", "is", "to stop patterns",                 "from stopping patterns"))
        .Add(SPPID.AlarmToggling,         new PermDataClient(false, FAI.Bell,                  FAI.Ban,           "allow",       "prevent",      "Alarm Toggling", "is", "to toggle alarms",                 "from toggling alarms"))
        .Add(SPPID.TriggerToggling,       new PermDataClient(false, FAI.FileMedicalAlt,        FAI.Ban,           "allow",       "prevent",      "Trigger Toggling", "is", "to toggle triggers",               "from toggling triggers"))

        .Add(SPPID.HardcoreModeState,     new PermDataClient(false, FAI.AnchorLock,            FAI.Unlock,        "enabled",     "disabled",     "Hardcore Mode", "is",  string.Empty,                             string.Empty))
        .Add(SPPID.GarbleChannelEditing,  new PermDataClient(false, FAI.CommentDots,           FAI.Ban,           "allow",       "prevent",      "garble channel editing", "is", "to change your configured garbler channels", "from changing your configured garbler channels."))
        .Add(SPPID.HypnoticImage,         new PermDataClient(false, FAI.Images,                FAI.Ban,           "allow",       "prevent",      "hypnotic image sending", "is", "to send custom hypnosis BG's", "from sending custom hypnosis BG's"));

    public record HcPermClient(FAI IconT, FAI IconF, string PermLabel, string EnabledPreText, string DisabledText, string ToggleTrueSuffixTT, string ToggleFalseSuffixTT);

    private readonly ImmutableDictionary<SPPID, HcPermClient> ClientHcPermData = ImmutableDictionary<SPPID, HcPermClient>.Empty
        .Add(SPPID.LockedFollowing, new HcPermClient(FAI.Walking, FAI.Ban, "Forced Follow", "Actively following", "Not following anyone", "to make you follow them", "from triggering --COL--Forced Follow--COL-- on you"))
        .Add(SPPID.LockedEmoteState, new HcPermClient(FAI.PersonArrowDownToLine, FAI.Ban, "Locked Emote State", "Emote Locked for", "Not locked in an emote loop", string.Empty, string.Empty)) // Handle this seperately, it has it's own call.
        .Add(SPPID.IndoorConfinement, new HcPermClient(FAI.HouseLock, FAI.Ban, "Indoor Confinement", "Confined by", "Not confined by anyone", "to confine you indoors --COL--via the nearest housing node--COL----NL--If --COL--Lifestream--COL-- is installed, can be confined to --COL--any address--COL--.", "from confining you indoors"))
        .Add(SPPID.Imprisonment, new HcPermClient(FAI.Bars, FAI.Ban, "Imprisonment", "Imprisoned by", "Not imprisoned", "to imprison you at a desired location.--SEP----COL--They must be nearby when giving a location besides your current position.", "from imprisoning you at a desired location"))
        .Add(SPPID.ChatBoxesHidden, new HcPermClient(FAI.CommentSlash, FAI.Ban, "Chatbox Visibility", "Chatbox hidden by", "Chatbox is visible", "to hide your chatbox UI", "from hiding your chatbox"))
        .Add(SPPID.ChatInputHidden, new HcPermClient(FAI.CommentSlash, FAI.Ban, "ChatInput Visibility", "ChatInput hidden by", "ChatInput is visible", "to hide your chat input UI", "from hiding your chat input"))
        .Add(SPPID.ChatInputBlocked, new HcPermClient(FAI.CommentDots, FAI.Ban, "ChatInput Blocking", "ChatInput blocked by", "ChatInput is accessible", "to block your chat input", "from blocking your chat input"));
}