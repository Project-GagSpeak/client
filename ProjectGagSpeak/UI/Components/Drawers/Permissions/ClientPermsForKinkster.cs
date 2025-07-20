using CkCommons;using CkCommons.Classes;using CkCommons.Gui;using CkCommons.Raii;using Dalamud.Interface.Colors;using Dalamud.Interface.Utility.Raii;using GagSpeak.Kinksters;using GagSpeak.PlayerClient;using GagSpeak.Services;using GagSpeak.Utils;using GagSpeak.WebAPI;using GagspeakAPI.Data.Permissions;using GagspeakAPI.Enums;using GagspeakAPI.Extensions;using ImGuiNET;using Lumina;using OtterGui;using OtterGui.Text;using System.Collections.Immutable;namespace GagSpeak.Gui.Components;
public class ClientPermsForKinkster{
    private readonly MainHub _hub;
    private readonly PresetLogicDrawer _presets;
    private readonly PiShockProvider _shockies;
    private static IconCheckboxEx EditAccessCheckbox = new(FAI.Pen, 0xFF00FF00, 0);

    public ClientPermsForKinkster(MainHub hub, PresetLogicDrawer presets, PiShockProvider shockies)
    {
        _hub = hub;
        _presets = presets;
        _shockies = shockies;
    }

    // internal storage.
    private Dictionary<SPPID, string> _timespanCache = new();
    public void DrawPermissions(Kinkster kinkster, string dispName, float width)    {
        ImGuiUtil.Center($"Your Permissions for {dispName}");
        _presets.DrawPresetList(kinkster, width);
        ImGui.Separator();

        // Child area for scrolling.
        using var _ = CkRaii.Child("ClientPermsForKinkster", ImGui.GetContentRegionAvail(), WFlags.NoScrollbar);
        if (OwnGlobals.Perms is not { } globals)            return;
        ImGui.TextUnformatted("Global Settings");        DrawPermRow(kinkster, dispName,   width, SPPID.ChatGarblerActive,     globals.ChatGarblerActive,          kinkster.OwnPermAccess.ChatGarblerActiveAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ChatGarblerLocked,     globals.ChatGarblerLocked,          kinkster.OwnPermAccess.ChatGarblerLockedAllowed );
        DrawPermRow(kinkster, dispName,   width, SPPID.GaggedNameplate,       globals.GaggedNameplate,              kinkster.OwnPermAccess.GaggedNameplateAllowed);
        ImGui.Separator();        ImGui.TextUnformatted("Padlock Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.PermanentLocks,        kinkster.OwnPerms.PermanentLocks,       kinkster.OwnPermAccess.PermanentLocksAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.OwnerLocks,            kinkster.OwnPerms.OwnerLocks,           kinkster.OwnPermAccess.OwnerLocksAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.DevotionalLocks,       kinkster.OwnPerms.DevotionalLocks,      kinkster.OwnPermAccess.DevotionalLocksAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Gag Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.GagVisuals,            globals.GagVisuals,                     kinkster.OwnPermAccess.GagVisualsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyGags,             kinkster.OwnPerms.ApplyGags,            kinkster.OwnPermAccess.ApplyGagsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.LockGags,              kinkster.OwnPerms.LockGags,             kinkster.OwnPermAccess.LockGagsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxGagTime,            kinkster.OwnPerms.MaxGagTime,           kinkster.OwnPermAccess.MaxGagTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.UnlockGags,            kinkster.OwnPerms.UnlockGags,           kinkster.OwnPermAccess.UnlockGagsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveGags,            kinkster.OwnPerms.RemoveGags,           kinkster.OwnPermAccess.RemoveGagsAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Restriction Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.RestrictionVisuals,    globals.RestrictionVisuals,             kinkster.OwnPermAccess.RestrictionVisualsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyRestrictions,     kinkster.OwnPerms.ApplyRestrictions,    kinkster.OwnPermAccess.ApplyRestrictionsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.LockRestrictions,      kinkster.OwnPerms.LockRestrictions,     kinkster.OwnPermAccess.LockRestrictionsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxRestrictionTime,    kinkster.OwnPerms.MaxRestrictionTime,   kinkster.OwnPermAccess.MaxRestrictionTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.UnlockRestrictions,    kinkster.OwnPerms.UnlockRestrictions,   kinkster.OwnPermAccess.UnlockRestrictionsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveRestrictions,    kinkster.OwnPerms.RemoveRestrictions,   kinkster.OwnPermAccess.RemoveRestrictionsAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Restraint Set Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.RestraintSetVisuals,   globals.RestraintSetVisuals,            kinkster.OwnPermAccess.RestraintSetVisualsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyRestraintSets,    kinkster.OwnPerms.ApplyRestraintSets,   kinkster.OwnPermAccess.ApplyRestraintSetsAllowed );
        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyLayers,           kinkster.OwnPerms.ApplyLayers,          kinkster.OwnPermAccess.ApplyLayersAllowed );
        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyLayersWhileLocked,kinkster.OwnPerms.ApplyLayersWhileLocked,kinkster.OwnPermAccess.ApplyLayersWhileLockedAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.LockRestraintSets,     kinkster.OwnPerms.LockRestraintSets,    kinkster.OwnPermAccess.LockRestraintSetsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxRestrictionTime,    kinkster.OwnPerms.MaxRestrictionTime,   kinkster.OwnPermAccess.MaxRestrictionTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.UnlockRestraintSets,   kinkster.OwnPerms.UnlockRestraintSets,  kinkster.OwnPermAccess.UnlockRestraintSetsAllowed );
        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveLayers,          kinkster.OwnPerms.RemoveLayers,          kinkster.OwnPermAccess.RemoveLayersAllowed );
        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveLayersWhileLocked,kinkster.OwnPerms.RemoveLayersWhileLocked,kinkster.OwnPermAccess.RemoveLayersWhileLockedAllowed);        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveRestraintSets,   kinkster.OwnPerms.RemoveRestraintSets,  kinkster.OwnPermAccess.RemoveRestraintSetsAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Puppeteer Permissions");
        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermSit,         kinkster.OwnPerms.PuppetPerms,          kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Sit );        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermEmote,       kinkster.OwnPerms.PuppetPerms,          kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Emotes );        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermAlias,       kinkster.OwnPerms.PuppetPerms,          kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Alias );        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermAll,         kinkster.OwnPerms.PuppetPerms,          kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.All );        ImGui.Separator();        ImGui.TextUnformatted("Moodles Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyPositive,         kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PositiveStatusTypes );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyNegative,         kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.NegativeStatusTypes );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplySpecial,          kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.SpecialStatusTypes );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyOwnMoodles,       kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PairCanApplyTheirMoodlesToYou );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyPairsMoodles,     kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PairCanApplyYourMoodlesToYou );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxMoodleTime,         kinkster.OwnPerms.MaxMoodleTime,        kinkster.OwnPermAccess.MaxMoodleTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.PermanentMoodles,      kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PermanentMoodles );        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveMoodles,         kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.RemovingMoodles );        ImGui.Separator();

        ImGui.TextUnformatted("Miscellaneous Permissions");
        DrawPermRow(kinkster, dispName,   width, SPPID.HypnoticEffect,        kinkster.OwnPerms.HypnoEffectSending,   kinkster.OwnPermAccess.HypnoEffectSendingAllowed );        ImGui.Separator();
        ImGui.TextUnformatted("Toybox Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.PatternStarting,       kinkster.OwnPerms.ExecutePatterns,      kinkster.OwnPermAccess.ExecutePatternsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.PatternStopping,       kinkster.OwnPerms.StopPatterns,         kinkster.OwnPermAccess.StopPatternsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.AlarmToggling,         kinkster.OwnPerms.ToggleAlarms,         kinkster.OwnPermAccess.ToggleAlarmsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.TriggerToggling,       kinkster.OwnPerms.ToggleTriggers,       kinkster.OwnPermAccess.ToggleTriggersAllowed );        ImGui.Separator();
        ImGui.TextUnformatted("Hardcore Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.HardcoreModeState,     kinkster.OwnPerms.InHardcore,                   kinkster.OwnPerms.InHardcore );        DrawPermRow(kinkster, dispName,   width, SPPID.PairLockedStates,      kinkster.OwnPerms.PairLockedStates,             true );        DrawHcPermRow(kinkster, dispName, width, SPPID.ForcedFollow,          globals.ForcedFollow,                           kinkster.OwnPerms.AllowForcedFollow );        DrawHcPermRow(kinkster, dispName, width, SPPID.ForcedEmoteState,      globals.ForcedEmoteState,                       kinkster.OwnPerms.AllowForcedEmote );        DrawHcPermRow(kinkster, dispName, width, SPPID.ForcedStay,            globals.ForcedStay,                             kinkster.OwnPerms.AllowForcedStay );        DrawHcPermRow(kinkster, dispName, width, SPPID.ChatBoxesHidden,       globals.ChatBoxesHidden,                        kinkster.OwnPerms.AllowHidingChatBoxes );        DrawHcPermRow(kinkster, dispName, width, SPPID.ChatInputHidden,       globals.ChatInputHidden,                        kinkster.OwnPerms.AllowHidingChatInput );        DrawHcPermRow(kinkster, dispName, width, SPPID.ChatInputBlocked,      globals.ChatInputBlocked,                       kinkster.OwnPerms.AllowChatInputBlocking );
        DrawHcPermRow(kinkster, dispName, width, SPPID.HypnoticImage,         globals.HypnosisCustomEffect,                   kinkster.OwnPerms.AllowHypnoImageSending );
        // draw garble channel editing here.


        // the "special child"        DrawPiShockPairPerms(kinkster, dispName, width, kinkster.OwnPerms, kinkster.OwnPermAccess);    }

    private void DrawPermRow(Kinkster kinkster, string dispName, float width, SPPID perm, bool current, bool canEdit)
        => DrawPermRowCommon(kinkster, dispName, width, perm, current, canEdit, () => !current);

    private void DrawPermRow(Kinkster kinkster, string dispName, float width, SPPID perm, PuppetPerms current, PuppetPerms canEdit, PuppetPerms editFlag)
    {
        var isFlagSet = (current & editFlag) == editFlag;
        DrawPermRowCommon(kinkster, dispName, width, perm, isFlagSet, canEdit.HasAny(editFlag), () => current ^ editFlag);
    }

    private void DrawPermRow(Kinkster kinkster, string dispName, float width, SPPID perm, MoodlePerms current, MoodlePerms canEdit, MoodlePerms editFlag)
    {
        var isFlagSet = (current & editFlag) == editFlag;
        DrawPermRowCommon(kinkster, dispName, width, perm, isFlagSet, canEdit.HasAny(editFlag), () => current ^ editFlag);
    }

    private void DrawPermRow(Kinkster kinkster, string dispName, float width, SPPID perm, TimeSpan current, bool canEdit)
    {
        using var disabled = ImRaii.Disabled(kinkster.OwnPerms.InHardcore);

        var inputTxtWidth = width * .4f;
        var str = _timespanCache.TryGetValue(perm, out var value) ? value : current.ToGsRemainingTime();
        var data = ClientPermData[perm];
        var editCol = canEdit ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();

        if (CkGui.IconInputText(data.IconYes, data.PermLabel, "0d0h0m0s", ref str, 32, inputTxtWidth, true))
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
        CkGui.AttachToolTip($"How long of a timer {dispName} can put on your padlocks.");

        ImGui.SameLine(width - ImGui.GetFrameHeight());
        if (EditAccessCheckbox.Draw($"##{perm}", canEdit, out var newVal) && canEdit != newVal)
            UiService.SetUITask(async () => await PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), newVal));
        CkGui.AttachToolTip($"{dispName} {(canEdit ? "can" : "can not")} change your {data.PermLabel} setting.");
        
    }

    // optimize later and stuff.
    private void DrawPermRowCommon<T>(Kinkster kinkster, string dispName, float width, SPPID perm, bool current, bool canEdit, Func<T> newStateFunc)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0);
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
            UiService.SetUITask(async () => await PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), newVal));
        CkGui.AttachToolTip($"{dispName} {(canEdit ? "can" : "can not")} change your {data.PermLabel} setting.");

        // draw inside of the button.
        ImGui.SetCursorScreenPos(pos);
        PrintButtonRichText(data, current);
        CkGui.AttachToolTip(data.IsGlobal
            ? $"Your {data.PermLabel} {data.JoinWord} {(current ? data.AllowedStr : data.BlockedStr)}. (Globally)"
            : $"You have {(current ? data.AllowedStr : data.BlockedStr)} {dispName} {(current ? data.PairAllowedTT : data.PairBlockedTT)}");
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

    private void DrawHcPermRow(Kinkster kinkster, string dispName, float width, SPPID perm, string current, bool canUse)
    {
        using var s = ImRaii.PushStyle(ImGuiStyleVar.Alpha, kinkster.OwnPerms.InHardcore ? 1f : 0.5f);
        using var c = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        
        var data = ClientPermData[perm];
        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();
        var isActive = !current.IsNullOrWhitespace();

        using (ImRaii.Disabled(kinkster.OwnPerms.InHardcore))
        {
            // Be better corby... after all your UI work, you can make a better design for this...
            var button = ImGui.Button("##client" + perm, new Vector2(buttonW, ImGui.GetFrameHeight()));
            ImGui.SetCursorScreenPos(pos);
            using (ImRaii.Group())
            {
                CkGui.FramedIconText(isActive ? data.IconYes : data.IconNo);
                CkGui.TextFrameAlignedInline(data.PermLabel);
                ImGui.SameLine();
                CkGui.ColorTextBool(isActive ? data.AllowedStr : data.BlockedStr, isActive);
                CkGui.TextFrameAlignedInline(".");
            }
            CkGui.AttachToolTip($"You have {(isActive ? data.AllowedStr : data.BlockedStr)} {dispName} {(isActive ? data.PairAllowedTT : data.PairBlockedTT)}.");

            if (button)
            {
                var res = perm.ToPermValue();
                if (res.name.IsNullOrEmpty())
                    return;

                UiService.SetUITask(async () =>
                {
                    switch (res.type)
                    {
                        case PermissionType.Global: await _hub.UserChangeOwnGlobalPerm(res.name, !canUse); break;
                        case PermissionType.PairPerm: await PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.PairPerms, res.name, !canUse); break;
                        case PermissionType.PairAccess: await PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, res.name, !canUse); break;
                        default: break;
                    }
                });
            }
        }

        ImGui.SameLine();
        var refVar = canUse;
        if (ImGui.Checkbox("##" + perm + "edit", ref refVar))
            UiService.SetUITask(PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), refVar));
        CkGui.AttachToolTip(
            $"{data.PermLabel} is {(isActive ? data.AllowedStr : data.BlockedStr)} for {dispName}." +
            $"--SEP-- You are helpless to disable this!");
    }

    public void DrawHcPermRow(Kinkster kinkster, string dispName, float width, string current, bool canUse, bool canUseFull)
    {
        using var s = ImRaii.PushStyle(ImGuiStyleVar.Alpha, kinkster.OwnPerms.InHardcore ? 1f : 0.5f);
        using var c = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));

        var data = ClientPermData[SPPID.ForcedEmoteState];
        var isActive = !current.IsNullOrWhitespace();
        var buttonW = width - (2 * ImGui.GetFrameHeightWithSpacing());
        using (ImRaii.Disabled(kinkster.OwnPerms.InHardcore))
        {
            using (ImRaii.Group())
            {
                CkGui.FramedIconText(isActive ? data.IconYes : data.IconNo);
                CkGui.TextFrameAlignedInline($"{data.PermLabel} ");
                ImGui.SameLine();
                CkGui.ColorTextBool(isActive ? data.AllowedStr : data.BlockedStr, isActive);
                CkGui.TextFrameAlignedInline(".");
            }
            CkGui.AttachToolTip($"You have {(isActive ? data.AllowedStr : data.BlockedStr)} {dispName} {(isActive ? data.PairAllowedTT : data.PairBlockedTT)}.");
        }

        ImGui.SameLine(buttonW);
        var refVar = canUse;
        if (ImGui.Checkbox("##" + SPPID.ForcedEmoteState + "edit", ref refVar))
        {
            var propertyName = SPPID.ForcedEmoteState.ToPermAccessValue();
            UiService.SetUITask(PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, propertyName, refVar));
        }
        CkGui.AttachToolTip($"Limit {dispName} to only force GroundSit, Sit, and CyclePose.");

        ImUtf8.SameLineInner();
        var refVar2 = canUseFull;
        if (ImGui.Checkbox("##" + SPPID.ForcedEmoteState + "edit2", ref refVar2))
        {
            var propertyName = SPPID.ForcedEmoteState.ToPermAccessValue(true);
            UiService.SetUITask(PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, propertyName, refVar));
        }
        CkGui.AttachToolTip($"Allow {dispName} to force you into any looped Emote.");
    }


    private DateTime _lastRefresh = DateTime.MinValue;

    /// <summary>
    ///     This function is messy because it is independant of everything else due to a bad conflict between pishock HTML and gagspeak signalR.
    /// </summary>
    public void DrawPiShockPairPerms(Kinkster kinkster, string dispName, float width, PairPerms pairPerms, PairPermAccess pairAccess)
    {
        // First row must be drawn.
        using (ImRaii.Group())
        {
            var length = width - CkGui.IconTextButtonSize(FAI.Sync, "Refresh") + ImGui.GetFrameHeight();
            var refCode = pairPerms.PiShockShareCode;

            // the bad way.
            CkGui.IconInputText(FAI.ShareAlt, string.Empty, "Unique Share Code", ref refCode, 40, width, true, false);
            if (ImGui.IsItemDeactivatedAfterEdit())
                UiService.SetUITask(PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, SPPID.PiShockShareCode.ToPermValue().name, refCode));

            CkGui.AttachToolTip($"Unique Share Code for {dispName}." +
                "--SEP--This should be a separate Share Code from your Global Share Code." +
                $"--SEP--A Unique Share Code can have permissions elevated higher than the Global Share Code that only {dispName} can use.");

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Sync, "Refresh", disabled: DateTime.Now - _lastRefresh < TimeSpan.FromSeconds(15) || refCode.IsNullOrWhitespace()))
            {
                _lastRefresh = DateTime.Now;
                UiService.SetUITask(async () =>
                {
                    var newPerms = await _shockies.GetPermissionsFromCode(pairPerms.PiShockShareCode);
                    pairPerms.AllowShocks = newPerms.AllowShocks;
                    pairPerms.AllowVibrations = newPerms.AllowVibrations;
                    pairPerms.AllowBeeps = newPerms.AllowBeeps;
                    pairPerms.MaxDuration = newPerms.MaxDuration;
                    pairPerms.MaxIntensity = newPerms.MaxIntensity;
                    await _hub.UserBulkChangeUnique(new(kinkster.UserData, pairPerms, pairAccess));                        
                });
            }
        }

        // special case for this.
        using (ImRaii.Group())
        {
            var seconds = (float)pairPerms.MaxVibrateDuration.TotalMilliseconds / 1000;
            if (CkGui.IconSliderFloat("##maxVibeTime" + kinkster.UserData.UID, FAI.Stopwatch, "Max Vibe Duration",
                ref seconds, 0.1f, 15f, width * .65f, true, pairPerms.HasValidShareCode()))
            {
                pairPerms.MaxVibrateDuration = TimeSpan.FromSeconds(seconds);
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                var timespanValue = TimeSpan.FromSeconds(seconds);
                var ticks = (ulong)timespanValue.Ticks;
                UiService.SetUITask(PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, SPPID.MaxVibrateDuration.ToPermValue().name, ticks));
            }
            CkGui.AttachToolTip("Max duration you allow this pair to vibrate your Shock Collar for");
        }
    }

    public record PermDataClient(bool IsGlobal, FAI IconYes, FAI IconNo, string AllowedStr, string BlockedStr, string PermLabel, string JoinWord, string PairAllowedTT, string PairBlockedTT);

    /// <summary> The Cache of PermissionData for each permission in the Gear Setting Menu. </summary>
    private readonly ImmutableDictionary<SPPID, PermDataClient> ClientPermData = ImmutableDictionary<SPPID, PermDataClient>.Empty
        .Add(SPPID.ChatGarblerActive,     new PermDataClient(true, FAI.MicrophoneSlash,       FAI.Microphone,    "active",        "inactive",        "Chat Garbler", "is",       string.Empty, string.Empty))
        .Add(SPPID.ChatGarblerLocked,     new PermDataClient(true, FAI.Key,                   FAI.UnlockAlt,     "locked",        "unlocked",        "Chat Garbler", "is",       string.Empty, string.Empty))
        .Add(SPPID.GaggedNameplate,       new PermDataClient(true, FAI.IdCard,                FAI.Ban,           "enabled",       "disabled",        "GagPlates", "are",         string.Empty, string.Empty))

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

        .Add(SPPID.HypnoticEffect,        new PermDataClient(false, FAI.CameraRotate,          FAI.Ban,           "allow",       "prevent",      "Hypnotic Effect", "are", "to send hypnotic effects",         "from sending hypnotic effects"))

        .Add(SPPID.PatternStarting,       new PermDataClient(false, FAI.Play,                  FAI.Ban,           "allow",       "prevent",      "Pattern Starting", "is", "to start patterns",                "from starting patterns"))
        .Add(SPPID.PatternStopping,       new PermDataClient(false, FAI.Stop,                  FAI.Ban,           "allow",       "prevent",      "Pattern Stopping", "is", "to stop patterns",                 "from stopping patterns"))
        .Add(SPPID.AlarmToggling,         new PermDataClient(false, FAI.Bell,                  FAI.Ban,           "allow",       "prevent",      "Alarm Toggling", "is", "to toggle alarms",                 "from toggling alarms"))
        .Add(SPPID.TriggerToggling,       new PermDataClient(false, FAI.FileMedicalAlt,        FAI.Ban,           "allow",       "prevent",      "Trigger Toggling", "is", "to toggle triggers",               "from toggling triggers"))

        .Add(SPPID.HardcoreModeState,     new PermDataClient(false, FAI.AnchorLock,            FAI.Unlock,        "enabled",       "disabled",        "Hardcore Mode",            "is",  string.Empty,                             string.Empty))
        .Add(SPPID.PairLockedStates,      new PermDataClient(false, FAI.AnchorLock,            FAI.Unlock,        "Devotional",    "not Devotional",  "Hardcore States",          "are", string.Empty, string.Empty))
        .Add(SPPID.ForcedFollow,          new PermDataClient(false, FAI.Walking,               FAI.Ban,           "active",        "inactive",        "Forced Follow",            "is", string.Empty, string.Empty))
        .Add(SPPID.ForcedEmoteState,      new PermDataClient(false, FAI.PersonArrowDownToLine, FAI.Ban,           "active",        "inactive",        "Forced Emote",             "is", string.Empty, string.Empty))
        .Add(SPPID.ForcedStay,            new PermDataClient(false, FAI.HouseLock,             FAI.Ban,           "active",        "inactive",        "Forced Stay",              "is", string.Empty, string.Empty))
        .Add(SPPID.ChatBoxesHidden,       new PermDataClient(false, FAI.CommentSlash,          FAI.Ban,           "visible",       "hidden",          "Chatboxes",                "are", string.Empty, string.Empty))
        .Add(SPPID.ChatInputHidden,       new PermDataClient(false, FAI.CommentSlash,          FAI.Ban,           "visible",       "hidden",          "Chat Input",               "is", string.Empty, string.Empty))
        .Add(SPPID.ChatInputBlocked,      new PermDataClient(false, FAI.CommentDots,           FAI.Ban,           "blocked",       "allow",         "Chat Input",               "is", string.Empty, string.Empty))
        .Add(SPPID.HypnoticImage,         new PermDataClient(false, FAI.Image,                 FAI.Ban,           "allow",       "prevent",      "Hypnotic Image", "is", "to send hypnotic images",         "from sending hypnotic images"));}