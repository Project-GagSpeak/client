using Dalamud.Interface.Utility.Raii;using CkCommons.Gui;using CkCommons.Raii;using GagSpeak.Gui;using GagSpeak.Kinksters;using GagSpeak.PlayerClient;using GagSpeak.Services;using GagSpeak.Utils;using GagSpeak.WebAPI;using GagspeakAPI.Data.Permissions;using GagspeakAPI.Extensions;using GagspeakAPI.Hub;using ImGuiNET;using NAudio.SoundFont;using OtterGui;using OtterGui.Text;using System.Collections.Immutable;namespace GagSpeak.Gui.Components;
public class ClientPermsForKinkster{
    private readonly GlobalPermissions _globals;
    private readonly MainHub _hub;
    private readonly PresetLogicDrawer _presets;
    private readonly PiShockProvider _shockies;

    public ClientPermsForKinkster(GlobalPermissions globals, MainHub hub,
        PresetLogicDrawer presets, PiShockProvider shockies)
    {
        _globals = globals;
        _hub = hub;
        _presets = presets;
        _shockies = shockies;
    }

    // internal storage.
    private Dictionary<SPPID, string> _timespanCache = new();
    public void DrawPermissions(Kinkster kinkster, float width)    {
        var dispName = kinkster.GetNickAliasOrUid();
        ImGuiUtil.Center($"Your Permissions for {dispName}");
        _presets.DrawPresetList(kinkster, width);
        ImGui.Separator();

        // Child area for scrolling.
        using var _ = CkRaii.Child("ClientPermsForKinkster", new Vector2(0, ImGui.GetContentRegionAvail().Y), WFlags.NoScrollbar);        if (_globals.Current is not { } globals)            return;
        ImGui.TextUnformatted("Global Settings");        DrawPermRow(kinkster, dispName,   width, SPPID.ChatGarblerActive,     globals.ChatGarblerActive,          kinkster.OwnPermAccess.ChatGarblerActiveAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ChatGarblerLocked,     globals.ChatGarblerLocked,          kinkster.OwnPermAccess.ChatGarblerLockedAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.LockToyboxUI,          globals.LockToyboxUI,               kinkster.OwnPermAccess.LockToyboxUIAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Padlock Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.PermanentLocks,        kinkster.OwnPerms.PermanentLocks,       kinkster.OwnPermAccess.PermanentLocksAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.OwnerLocks,            kinkster.OwnPerms.OwnerLocks,           kinkster.OwnPermAccess.OwnerLocksAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.DevotionalLocks,       kinkster.OwnPerms.DevotionalLocks,      kinkster.OwnPermAccess.DevotionalLocksAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Gag Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.GagVisuals,            globals.GagVisuals,                     kinkster.OwnPermAccess.GagVisualsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyGags,             kinkster.OwnPerms.ApplyGags,            kinkster.OwnPermAccess.ApplyGagsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.LockGags,              kinkster.OwnPerms.LockGags,             kinkster.OwnPermAccess.LockGagsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxGagTime,            kinkster.OwnPerms.MaxGagTime,           kinkster.OwnPermAccess.MaxGagTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.UnlockGags,            kinkster.OwnPerms.UnlockGags,           kinkster.OwnPermAccess.UnlockGagsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveGags,            kinkster.OwnPerms.RemoveGags,           kinkster.OwnPermAccess.RemoveGagsAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Restriction Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.RestrictionVisuals,    globals.RestrictionVisuals,             kinkster.OwnPermAccess.RestrictionVisualsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyRestrictions,     kinkster.OwnPerms.ApplyRestrictions,    kinkster.OwnPermAccess.ApplyRestrictionsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.LockRestrictions,      kinkster.OwnPerms.LockRestrictions,     kinkster.OwnPermAccess.LockRestrictionsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxRestrictionTime,    kinkster.OwnPerms.MaxRestrictionTime,   kinkster.OwnPermAccess.MaxRestrictionTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.UnlockRestrictions,    kinkster.OwnPerms.UnlockRestrictions,   kinkster.OwnPermAccess.UnlockRestrictionsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveRestrictions,    kinkster.OwnPerms.RemoveRestrictions,   kinkster.OwnPermAccess.RemoveRestrictionsAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Restraint Set Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.RestraintSetVisuals,   globals.RestraintSetVisuals,            kinkster.OwnPermAccess.RestraintSetVisualsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyRestraintSets,    kinkster.OwnPerms.ApplyRestraintSets,   kinkster.OwnPermAccess.ApplyRestraintSetsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.LockRestraintSets,     kinkster.OwnPerms.LockRestraintSets,    kinkster.OwnPermAccess.LockRestraintSetsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxRestrictionTime,    kinkster.OwnPerms.MaxRestrictionTime,   kinkster.OwnPermAccess.MaxRestrictionTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.UnlockRestraintSets,   kinkster.OwnPerms.UnlockRestraintSets,  kinkster.OwnPermAccess.UnlockRestraintSetsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveRestraintSets,   kinkster.OwnPerms.RemoveRestraintSets,  kinkster.OwnPermAccess.RemoveRestraintSetsAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Puppeteer Permissions");
        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermSit,         kinkster.OwnPerms.PuppetPerms,          kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Sit );        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermEmote,       kinkster.OwnPerms.PuppetPerms,          kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Emotes );        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermAlias,       kinkster.OwnPerms.PuppetPerms,          kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Alias );        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermAll,         kinkster.OwnPerms.PuppetPerms,          kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.All );        ImGui.Separator();        ImGui.TextUnformatted("Moodles Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyPositive,         kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PositiveStatusTypes );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyNegative,         kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.NegativeStatusTypes );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplySpecial,          kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.SpecialStatusTypes );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyOwnMoodles,       kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PairCanApplyTheirMoodlesToYou );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyPairsMoodles,     kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PairCanApplyYourMoodlesToYou );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxMoodleTime,         kinkster.OwnPerms.MaxMoodleTime,        kinkster.OwnPermAccess.MaxMoodleTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.PermanentMoodles,      kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.PermanentMoodles );        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveMoodles,         kinkster.OwnPerms.MoodlePerms,          kinkster.OwnPermAccess.MoodlePermsAllowed, MoodlePerms.RemovingMoodles );        ImGui.Separator();        ImGui.TextUnformatted("Toybox Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.ToyControl,            kinkster.OwnPerms.RemoteControlAccess,  kinkster.OwnPermAccess.RemoteControlAccessAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.PatternStarting,       kinkster.OwnPerms.ExecutePatterns,      kinkster.OwnPermAccess.ExecutePatternsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.PatternStopping,       kinkster.OwnPerms.StopPatterns,         kinkster.OwnPermAccess.StopPatternsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.AlarmToggling,         kinkster.OwnPerms.ToggleAlarms,         kinkster.OwnPermAccess.ToggleAlarmsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.TriggerToggling,       kinkster.OwnPerms.ToggleTriggers,       kinkster.OwnPermAccess.ToggleTriggersAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Hardcore Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.HardcoreModeState,     kinkster.OwnPerms.InHardcore,                   kinkster.OwnPerms.InHardcore );        DrawPermRow(kinkster, dispName,   width, SPPID.PairLockedStates,      kinkster.OwnPerms.PairLockedStates,             true );        DrawHcPermRow(kinkster, dispName, width, SPPID.ForcedFollow,          globals.ForcedFollow,                           kinkster.OwnPerms.AllowForcedFollow );        DrawHcPermRow(kinkster, dispName, width, SPPID.ForcedEmoteState,      globals.ForcedEmoteState,                       kinkster.OwnPerms.AllowForcedEmote );        DrawHcPermRow(kinkster, dispName, width, SPPID.ForcedStay,            globals.ForcedStay,                             kinkster.OwnPerms.AllowForcedStay );        DrawHcPermRow(kinkster, dispName, width, SPPID.GarbleChannelEditing,  globals.ChatGarblerChannelsBitfield.ToString(), kinkster.OwnPerms.AllowGarbleChannelEditing);
        DrawHcPermRow(kinkster, dispName, width, SPPID.ChatBoxesHidden,       globals.ChatBoxesHidden,                        kinkster.OwnPerms.AllowHidingChatBoxes );        DrawHcPermRow(kinkster, dispName, width, SPPID.ChatInputHidden,       globals.ChatInputHidden,                        kinkster.OwnPerms.AllowHidingChatInput );        DrawHcPermRow(kinkster, dispName, width, SPPID.ChatInputBlocked,      globals.ChatInputBlocked,                       kinkster.OwnPerms.AllowChatInputBlocking );
        DrawHcPermRow(kinkster, dispName, width, SPPID.HypnoticEffect,        globals.HypnosisCustomEffect,                   kinkster.OwnPerms.AllowHypnoEffectSending );
        DrawHcPermRow(kinkster, dispName, width, SPPID.HypnoticImage,         globals.HypnosisCustomEffect,                   kinkster.OwnPerms.AllowHypnoImageSending );

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

        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var str = _timespanCache.TryGetValue(perm, out var value) ? value : current.ToGsRemainingTime();
        var data = ClientPermData[perm];

        if (CkGui.IconInputText($"##{perm}", data.IconOn, data.Text, "0d0h0m0s", ref str, 32, buttonW, true, !canEdit))
        {
            if (str != current.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(str, out var newTime))
            {
                var ticks = (ulong)newTime.Ticks;
                var res = perm.ToPermValue();

                // Assign the blocking task if allowed.
                if (!res.name.IsNullOrEmpty() && res.type is PermissionType.PairPerm)
                    UiService.SetUITask(async () => await PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, res.name, ticks));
            }
            _timespanCache.Remove(perm);
        }
        CkGui.AttachToolTip($"The Max Duration {dispName} can Lock for.");

        ImGui.SameLine(buttonW);
        var refVar = canEdit;
        if (ImGui.Checkbox("##" + perm + "edit", ref refVar))
            UiService.SetUITask(PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), refVar));
        CkGui.AttachToolTip(canEdit
            ? $"Grant {dispName} control over this permission, allowing them to change the permission at any time"
            : $"Revoke {dispName} control over this permission, preventing them from changing the permission at any time");
    }

    // optimize later and stuff.
    private void DrawPermRowCommon<T>(Kinkster kinkster, string dispName, float width, SPPID perm, bool current, bool canEdit, Func<T> newStateFunc)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, canEdit ? 1f : 0.5f);
        using var butt = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        using var disabled = ImRaii.Disabled(kinkster.OwnPerms.InHardcore);

        var data = ClientPermData[perm];

        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();
        var button = ImGui.Button("##client" + perm, new Vector2(buttonW, ImGui.GetFrameHeight()));
        ImGui.SetCursorScreenPos(pos);
        using (ImRaii.Group())
        {
            CkGui.FramedIconText(current ? data.IconOn : data.IconOff);
            CkGui.TextFrameAlignedInline(data.Text);
            ImGui.SameLine();
            CkGui.ColorTextBool(current ? data.CondTrue : data.CondFalse, current);
            CkGui.TextFrameAlignedInline(".");
        }
        CkGui.AttachToolTip(current
            ? $"{dispName} is currently {data.CondTrue} {data.CondTrueTT}.--SEP--Clicking this will change it to {data.CondFalse}."
            : $"{dispName} is currently {data.CondFalse} {data.CondFalseTT}.--SEP--Clicking this will change it to {data.CondTrue}.");

        if (button)
        {
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;

            UiService.SetUITask(res.type switch
            {
                PermissionType.PairAccess
                    => PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, res.name, newState),
                PermissionType.PairPerm
                    => PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, res.name, newState),
                _ when _globals.Current != null
                    => PermissionHelper.ChangeOwnGlobal(_hub, _globals.Current, res.name, newState),
                _ => Task.CompletedTask

            });
        }

        ImGui.SameLine(buttonW);
        var refVar = canEdit;
        if (ImGui.Checkbox("##" + perm + "edit", ref refVar))
            UiService.SetUITask(PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), refVar));
        CkGui.AttachToolTip(canEdit
            ? $"Grant {dispName} control over this permission, allowing them to change the permission at any time"
            : $"Revoke {dispName} control over this permission, preventing them from changing the permission at any time");
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
                CkGui.FramedIconText(isActive ? data.IconOn : data.IconOff);
                CkGui.TextFrameAlignedInline(data.Text);
                ImGui.SameLine();
                CkGui.ColorTextBool(isActive ? data.CondTrue : data.CondFalse, isActive);
                CkGui.TextFrameAlignedInline(".");
            }
            CkGui.AttachToolTip(isActive
                ? $"{dispName} is currently {data.CondTrue} {data.CondTrueTT}.--SEP--Clicking this will change it to {data.CondFalse}."
                : $"{dispName} is currently {data.CondFalse} {data.CondFalseTT}.--SEP--Clicking this will change it to {data.CondTrue}.");

            if (button)
            {
                var res = perm.ToPermValue();
                if (res.name.IsNullOrEmpty())
                    return;

                UiService.SetUITask(res.type switch
                {
                    PermissionType.PairAccess => PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, res.name, !canUse),
                    PermissionType.PairPerm => PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, res.name, !canUse),
                    _ when _globals.Current is { } perms => PermissionHelper.ChangeOwnGlobal(_hub, perms, res.name, !canUse),
                    _ => Task.CompletedTask
                });
            }
        }

        ImGui.SameLine(buttonW);
        var refVar = canUse;
        if (ImGui.Checkbox("##" + perm + "edit", ref refVar))
            UiService.SetUITask(PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), refVar));
        CkGui.AttachToolTip(canUse
            ? $"{data.Text} {data.CondTrueTT} {data.CondTrue} for {dispName}.--SEP-- You are helpless to disable this!"
            : $"{data.Text} {data.CondFalseTT} {data.CondFalse} for {dispName}.");
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
                CkGui.FramedIconText(isActive ? data.IconOn : data.IconOff);
                CkGui.TextFrameAlignedInline($"{data.Text} ");
                ImGui.SameLine();
                CkGui.ColorTextBool(isActive ? data.CondTrue : data.CondFalse, isActive);
                CkGui.TextFrameAlignedInline(".");
            }
            CkGui.AttachToolTip(isActive
                ? $"{dispName} is currently {data.CondTrue} {data.CondTrueTT}.--SEP--Clicking this will change it to {data.CondFalse}."
                : $"{dispName} is currently {data.CondFalse} {data.CondFalseTT}.--SEP--Clicking this will change it to {data.CondTrue}.");
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
            CkGui.IconInputText($"Code {dispName}", FAI.ShareAlt, string.Empty, "Unique Share Code", ref refCode, 40, width, true, false);
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

    public record PermDataClient(FAI IconOn, FAI IconOff, string CondTrue, string CondFalse, string Text, string CondTrueTT, string CondFalseTT);

    /// <summary> The Cache of PermissionData for each permission in the Gear Setting Menu. </summary>
    private readonly ImmutableDictionary<SPPID, PermDataClient> ClientPermData = ImmutableDictionary<SPPID, PermDataClient>.Empty
        .Add(SPPID.ChatGarblerActive,     new PermDataClient(FAI.MicrophoneSlash,       FAI.Microphone,    "active",        "inactive",        "Chat Garbler",             string.Empty,                       string.Empty))
        .Add(SPPID.ChatGarblerLocked,     new PermDataClient(FAI.Key,                   FAI.UnlockAlt,     "locked",        "unlocked",        "Chat Garbler",             string.Empty,                       string.Empty))
        .Add(SPPID.LockToyboxUI,          new PermDataClient(FAI.Box,                   FAI.BoxOpen,       "allowed",       "restricted",      "Toybox UI locking",        "to lock your Toybox UI",           "from locking your ToyboxUI"))
        .Add(SPPID.PermanentLocks,        new PermDataClient(FAI.Infinity,              FAI.Ban,           "allowed",       "restricted",      "Permanent Locks",          "to use permanent locks.",          "from using permanent locks."))
        .Add(SPPID.OwnerLocks,            new PermDataClient(FAI.UserLock,              FAI.Ban,           "allowed",       "restricted",      "Owner Locks",              "to use owner locks",               "from using owner locks"))
        .Add(SPPID.DevotionalLocks,       new PermDataClient(FAI.UserLock,              FAI.Ban,           "allowed",       "restricted",      "Devotional Locks",         "to use devotional locks",          "from using devotional locks"))
        .Add(SPPID.GagVisuals,            new PermDataClient(FAI.Surprise,              FAI.Ban,           "enabled",       "disabled",        "Gag Visuals",              "to enable gag visuals",            "from enabling gag visuals"))
        .Add(SPPID.ApplyGags,             new PermDataClient(FAI.Mask,                  FAI.Ban,           "allowed",       "restricted",      "Applying Gags",            "to apply gags",                    "from applying gags"))
        .Add(SPPID.LockGags,              new PermDataClient(FAI.Lock,                  FAI.Ban,           "allowed",       "restricted",      "Locking Gags",             "to lock gags",                     "from locking gags"))
        .Add(SPPID.MaxGagTime,            new PermDataClient(FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Gag Time",             string.Empty,                       string.Empty))
        .Add(SPPID.UnlockGags,            new PermDataClient(FAI.Key,                   FAI.Ban,           "allowed",       "restricted",      "Unlocking Gags",           "to unlock gags",                   "from unlocking gags"))
        .Add(SPPID.RemoveGags,            new PermDataClient(FAI.Eraser,                FAI.Ban,           "allowed",       "restricted",      "Removing Gags",            "to remove gags",                   "from removing gags"))
        .Add(SPPID.RestrictionVisuals,    new PermDataClient(FAI.Tshirt,                FAI.Ban,           "enabled",       "disabled",        "Restriction Visuals",      "to enable restriction visuals",    "from enabling restriction visuals"))
        .Add(SPPID.ApplyRestrictions,     new PermDataClient(FAI.Handcuffs,             FAI.Ban,           "allowed",       "restricted",      "Applying Restrictions",    "to apply restrictions",            "from applying restrictions"))
        .Add(SPPID.LockRestrictions,      new PermDataClient(FAI.Lock,                  FAI.Ban,           "allowed",       "restricted",      "Locking Restrictions",     "to lock restrictions",             "from locking restrictions"))
        .Add(SPPID.MaxRestrictionTime,    new PermDataClient(FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Restriction Time",     string.Empty,                       string.Empty))
        .Add(SPPID.UnlockRestrictions,    new PermDataClient(FAI.Key,                   FAI.Ban,           "allowed",       "restricted",      "Unlocking Restrictions",   "to unlock restrictions",           "from unlocking restrictions"))
        .Add(SPPID.RemoveRestrictions,    new PermDataClient(FAI.Eraser,                FAI.Ban,           "allowed",       "restricted",      "Removing Restrictions",    "to remove restrictions",           "from removing restrictions"))
        .Add(SPPID.RestraintSetVisuals,   new PermDataClient(FAI.Tshirt,                FAI.Ban,           "enabled",       "disabled",        "Restraint Visuals",        "to enable restraint visuals",      "from enabling restraint visuals"))
        .Add(SPPID.ApplyRestraintSets,    new PermDataClient(FAI.Handcuffs,             FAI.Ban,           "allowed",       "restricted",      "Applying Restraints",      "to apply restraints",              "from applying restraints"))
        .Add(SPPID.LockRestraintSets,     new PermDataClient(FAI.Lock,                  FAI.Ban,           "allowed",       "restricted",      "Locking Restraints",       "to lock restraints",               "from locking restraints"))
        .Add(SPPID.MaxRestraintTime,      new PermDataClient(FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Restraint Time",       string.Empty,                       string.Empty))
        .Add(SPPID.UnlockRestraintSets,   new PermDataClient(FAI.Key,                   FAI.Ban,           "allowed",       "restricted",      "Unlocking Restraints",     "to unlock restraints",             "from unlocking restraints"))
        .Add(SPPID.RemoveRestraintSets,   new PermDataClient(FAI.Eraser,                FAI.Ban,           "allowed",       "restricted",      "Removing Restraints",      "to remove restraints",             "from removing restraints"))
        .Add(SPPID.PuppetPermSit,         new PermDataClient(FAI.Chair,                 FAI.Ban,           "allowed",       "restricted",      "Sit Requests",             "to invoke sit requests",           "from invoking sit requests"))
        .Add(SPPID.PuppetPermEmote,       new PermDataClient(FAI.Walking,               FAI.Ban,           "allowed",       "restricted",      "Emote Requests",           "to invoke emote requests",         "from invoking emote requests"))
        .Add(SPPID.PuppetPermAlias,       new PermDataClient(FAI.Scroll,                FAI.Ban,           "allowed",       "restricted",      "Alias Requests",           "to invoke alias requests",         "from invoking alias requests"))
        .Add(SPPID.PuppetPermAll,         new PermDataClient(FAI.CheckDouble,           FAI.Ban,           "allowed",       "restricted",      "All Requests",             "to invoke all requests",           "from invoking all requests"))
        .Add(SPPID.ApplyPositive,         new PermDataClient(FAI.SmileBeam,             FAI.Ban,           "allowed",       "restricted",      "Positive Moodles",         "to apply positive moodles",        "from applying positive moodles"))
        .Add(SPPID.ApplyNegative,         new PermDataClient(FAI.FrownOpen,             FAI.Ban,           "allowed",       "restricted",      "Negative Moodles",         "to apply negative moodles",        "from applying negative moodles"))
        .Add(SPPID.ApplySpecial,          new PermDataClient(FAI.WandMagicSparkles,     FAI.Ban,           "allowed",       "restricted",      "Special Moodles",          "to apply special moodles",         "from applying special moodles"))
        .Add(SPPID.ApplyPairsMoodles,     new PermDataClient(FAI.PersonArrowUpFromLine, FAI.Ban,           "allowed",       "restricted",      "applying your Moodles",    "to apply your moodles",            "from applying your moodles"))
        .Add(SPPID.ApplyOwnMoodles,       new PermDataClient(FAI.PersonArrowDownToLine, FAI.Ban,           "allowed",       "restricted",      "applying their Moodles",   "to apply their moodles",           "from applying their moodles"))
        .Add(SPPID.MaxMoodleTime,         new PermDataClient(FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Moodle Time",          string.Empty,                       string.Empty))
        .Add(SPPID.PermanentMoodles,      new PermDataClient(FAI.Infinity,              FAI.Ban,           "allowed",       "restricted",      "permanent Moodles",        "to apply permanent moodles",       "from applying permanent moodles"))
        .Add(SPPID.RemoveMoodles,         new PermDataClient(FAI.Eraser,                FAI.Ban,           "allowed",       "restricted",      "removing Moodles",         "to remove moodles",                "from removing moodles"))
        .Add(SPPID.ToyControl,            new PermDataClient(FAI.Phone,                 FAI.Ban,           "allowed",       "restricted",      "Remote Controlling",       "to control toys",                  "from controlling toys"))
        .Add(SPPID.PatternStarting,       new PermDataClient(FAI.Play,                  FAI.Ban,           "allowed",       "restricted",      "Pattern Starting",         "to start patterns",                "from starting patterns"))
        .Add(SPPID.PatternStopping,       new PermDataClient(FAI.Stop,                  FAI.Ban,           "allowed",       "restricted",      "Pattern Stopping",         "to stop patterns",                 "from stopping patterns"))
        .Add(SPPID.AlarmToggling,         new PermDataClient(FAI.Bell,                  FAI.Ban,           "allowed",       "restricted",      "Alarm Toggling",           "to toggle alarms",                 "from toggling alarms"))
        .Add(SPPID.TriggerToggling,       new PermDataClient(FAI.FileMedicalAlt,        FAI.Ban,           "allowed",       "restricted",      "Trigger Toggling",         "to toggle triggers",               "from toggling triggers"))
        .Add(SPPID.HardcoreModeState,     new PermDataClient(FAI.AnchorLock,            FAI.Unlock,        "enabled",       "disabled",        "Hardcore Mode",            "is",                               string.Empty))
        .Add(SPPID.PairLockedStates,      new PermDataClient(FAI.AnchorLock,            FAI.Unlock,        "Devotional",    "not Devotional",  "Hardcore States",          "are",                              string.Empty))
        .Add(SPPID.ForcedFollow,          new PermDataClient(FAI.Walking,               FAI.Ban,           "active",        "inactive",        "Forced Follow",            "is",                               string.Empty))
        .Add(SPPID.ForcedEmoteState,      new PermDataClient(FAI.PersonArrowDownToLine, FAI.Ban,           "active",        "inactive",        "Forced Emote",             "is",                               string.Empty))
        .Add(SPPID.ForcedStay,            new PermDataClient(FAI.HouseLock,             FAI.Ban,           "active",        "inactive",        "Forced Stay",              "is",                               string.Empty))
        .Add(SPPID.ChatBoxesHidden,       new PermDataClient(FAI.CommentSlash,          FAI.Ban,           "visible",       "hidden",          "Chatboxes",                "are",                              string.Empty))
        .Add(SPPID.ChatInputHidden,       new PermDataClient(FAI.CommentSlash,          FAI.Ban,           "visible",       "hidden",          "Chat Input",               "is",                               string.Empty))
        .Add(SPPID.ChatInputBlocked,      new PermDataClient(FAI.CommentDots,           FAI.Ban,           "blocked",       "allowed",         "Chat Input",               "is",                               string.Empty));}