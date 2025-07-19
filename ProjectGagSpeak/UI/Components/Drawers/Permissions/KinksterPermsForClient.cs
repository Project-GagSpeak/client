using Dalamud.Interface.Utility.Raii;using CkCommons.Raii;using GagSpeak.Kinksters;using GagSpeak.Services;using GagSpeak.Utils;using GagSpeak.WebAPI;using GagspeakAPI.Extensions;using ImGuiNET;using OtterGui;using System.Collections.Immutable;using CkCommons.Gui;namespace GagSpeak.Gui.Components;

// The permissions a Kinkster has set for us (their kinkster pair)
// from our perspective, these will be the pairs 'PairPerms' [ permissions they have set for us. ]
public class KinksterPermsForClient{
    private readonly MainHub _hub;

    public KinksterPermsForClient(MainHub hub)
    {
        _hub = hub;
    }

    // internal storage.
    private Dictionary<SPPID, string> _timespanCache = new();

    // Blocking Tasks.
    public Task? UiTask { get; private set; }

    public void DrawPermissions(Kinkster kinkster, float width)    {
        var dispName = kinkster.GetNickAliasOrUid();
        ImGuiUtil.Center($"{dispName}'s Permissions for You");
        ImGui.Separator();
        using var _ = CkRaii.Child("KinksterPerms", new Vector2(0, ImGui.GetContentRegionAvail().Y), WFlags.NoScrollbar);
        
        ImGui.TextUnformatted("Global Settings");        DrawPermRow(kinkster, dispName,   width, SPPID.ChatGarblerActive,     kinkster.PairGlobals.ChatGarblerActive, kinkster.PairPermAccess.ChatGarblerActiveAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ChatGarblerLocked,     kinkster.PairGlobals.ChatGarblerLocked, kinkster.PairPermAccess.ChatGarblerLockedAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Padlock Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.PermanentLocks,        kinkster.PairPerms.PermanentLocks,       kinkster.PairPermAccess.PermanentLocksAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.OwnerLocks,            kinkster.PairPerms.OwnerLocks,           kinkster.PairPermAccess.OwnerLocksAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.DevotionalLocks,       kinkster.PairPerms.DevotionalLocks,      kinkster.PairPermAccess.DevotionalLocksAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Gag Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.GagVisuals,            kinkster.PairGlobals.GagVisuals,                     kinkster.PairPermAccess.GagVisualsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyGags,             kinkster.PairPerms.ApplyGags,            kinkster.PairPermAccess.ApplyGagsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.LockGags,              kinkster.PairPerms.LockGags,             kinkster.PairPermAccess.LockGagsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxGagTime,            kinkster.PairPerms.MaxGagTime,           kinkster.PairPermAccess.MaxGagTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.UnlockGags,            kinkster.PairPerms.UnlockGags,           kinkster.PairPermAccess.UnlockGagsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveGags,            kinkster.PairPerms.RemoveGags,           kinkster.PairPermAccess.RemoveGagsAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Restriction Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.RestrictionVisuals,    kinkster.PairGlobals.RestrictionVisuals,             kinkster.PairPermAccess.RestrictionVisualsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyRestrictions,     kinkster.PairPerms.ApplyRestrictions,    kinkster.PairPermAccess.ApplyRestrictionsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.LockRestrictions,      kinkster.PairPerms.LockRestrictions,     kinkster.PairPermAccess.LockRestrictionsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxRestrictionTime,    kinkster.PairPerms.MaxRestrictionTime,   kinkster.PairPermAccess.MaxRestrictionTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.UnlockRestrictions,    kinkster.PairPerms.UnlockRestrictions,   kinkster.PairPermAccess.UnlockRestrictionsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveRestrictions,    kinkster.PairPerms.RemoveRestrictions,   kinkster.PairPermAccess.RemoveRestrictionsAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Restraint Set Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.RestraintSetVisuals,   kinkster.PairGlobals.RestraintSetVisuals,            kinkster.PairPermAccess.RestraintSetVisualsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyRestraintSets,    kinkster.PairPerms.ApplyRestraintSets,   kinkster.PairPermAccess.ApplyRestraintSetsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.LockRestraintSets,     kinkster.PairPerms.LockRestraintSets,    kinkster.PairPermAccess.LockRestraintSetsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxRestrictionTime,    kinkster.PairPerms.MaxRestrictionTime,   kinkster.PairPermAccess.MaxRestrictionTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.UnlockRestraintSets,   kinkster.PairPerms.UnlockRestraintSets,  kinkster.PairPermAccess.UnlockRestraintSetsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveRestraintSets,   kinkster.PairPerms.RemoveRestraintSets,  kinkster.PairPermAccess.RemoveRestraintSetsAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Puppeteer Permissions");
        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermSit,         kinkster.PairPerms.PuppetPerms,          kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.Sit );        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermEmote,       kinkster.PairPerms.PuppetPerms,          kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.Emotes );        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermAlias,       kinkster.PairPerms.PuppetPerms,          kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.Alias );        DrawPermRow(kinkster, dispName,   width, SPPID.PuppetPermAll,         kinkster.PairPerms.PuppetPerms,          kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.All );        ImGui.Separator();        ImGui.TextUnformatted("Moodles Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyPositive,         kinkster.PairPerms.MoodlePerms,          kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.PositiveStatusTypes );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyNegative,         kinkster.PairPerms.MoodlePerms,          kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.NegativeStatusTypes );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplySpecial,          kinkster.PairPerms.MoodlePerms,          kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.SpecialStatusTypes );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyOwnMoodles,       kinkster.PairPerms.MoodlePerms,          kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.PairCanApplyTheirMoodlesToYou );        DrawPermRow(kinkster, dispName,   width, SPPID.ApplyPairsMoodles,     kinkster.PairPerms.MoodlePerms,          kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.PairCanApplyYourMoodlesToYou );        DrawPermRow(kinkster, dispName,   width, SPPID.MaxMoodleTime,         kinkster.PairPerms.MaxMoodleTime,        kinkster.PairPermAccess.MaxMoodleTimeAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.PermanentMoodles,      kinkster.PairPerms.MoodlePerms,          kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.PermanentMoodles );        DrawPermRow(kinkster, dispName,   width, SPPID.RemoveMoodles,         kinkster.PairPerms.MoodlePerms,          kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.RemovingMoodles );        ImGui.Separator();        ImGui.TextUnformatted("Toybox Permissions");        DrawPermRow(kinkster, dispName,   width, SPPID.ToyControl,            kinkster.PairPerms.RemoteControlAccess,  kinkster.PairPermAccess.RemoteControlAccessAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.PatternStarting,       kinkster.PairPerms.ExecutePatterns,      kinkster.PairPermAccess.ExecutePatternsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.PatternStopping,       kinkster.PairPerms.StopPatterns,         kinkster.PairPermAccess.StopPatternsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.AlarmToggling,         kinkster.PairPerms.ToggleAlarms,         kinkster.PairPermAccess.ToggleAlarmsAllowed );        DrawPermRow(kinkster, dispName,   width, SPPID.TriggerToggling,       kinkster.PairPerms.ToggleTriggers,       kinkster.PairPermAccess.ToggleTriggersAllowed );        ImGui.Separator();    }

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
        using var disabled = ImRaii.Disabled(kinkster.PairPerms.InHardcore);

        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var str = _timespanCache.TryGetValue(perm, out var value) ? value : current.ToGsRemainingTime();
        var data = PairPermData[perm];

        if (CkGui.IconInputText($"##{perm}", data.IconOn, data.Text, "0d0h0m0s", ref str, 32, buttonW, true, !canEdit))
        {
            if (str != current.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(str, out var newTime))
            {
                var ticks = (ulong)newTime.Ticks;
                var res = perm.ToPermValue();

                // Assign the blocking task if allowed.
                if (!res.name.IsNullOrEmpty() && res.type is PermissionType.PairPerm)
                    UiTask = Task.Run(async () => await PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.PairPerms, res.name, ticks));
            }
            _timespanCache.Remove(perm);
        }
        CkGui.AttachToolTip($"The Max Duration {dispName} can lock you for.");

        ImGui.SameLine(buttonW);
        var refVar = canEdit;
        if (ImGui.Checkbox("##" + perm + "edit", ref refVar))
            UiTask = PermissionHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.PairPermAccess, perm.ToPermAccessValue(), refVar);
        CkGui.AttachToolTip(canEdit ? $"{dispName} allows you to change this." : $"Only {dispName} can update this permission.");
    }

    private void DrawPermRowCommon<T>(Kinkster kinkster, string dispName, float width, SPPID perm, bool current, bool canEdit, Func<T> newStateFunc)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, canEdit ? 1f : 0.5f);
        using var butt = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        
        var data = PairPermData[perm];
        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();
        var button = ImGui.Button("##pair" + perm, new Vector2(buttonW, ImGui.GetFrameHeight()));
        ImGui.SetCursorScreenPos(pos);
        using (ImRaii.Group())
        {
            CkGui.FramedIconText(current ? data.IconOn : data.IconOff);
            CkGui.TextFrameAlignedInline(current ? $"{dispName} has their {data.Text}" : dispName);
            ImGui.SameLine();
            CkGui.ColorTextBool(current ? data.CondTrue : data.CondFalse, current);
            CkGui.TextFrameAlignedInline(current ? "." : data.Text + ".");
        }
        CkGui.AttachToolTip($"Toggle {dispName}'s permission.");

        CkGui.BooleanToColoredIcon(canEdit, true, FAI.Unlock, FAI.Lock);
        CkGui.AttachToolTip(dispName + (canEdit
            ? "allows you to change this permission at will."
            : "is preventing you from changing this permission. Only they can update it."));

        if (button)
        {
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;

            UiService.SetUITask(Task.Run(() =>
            {
                return res.type switch
                {
                    PermissionType.Global => PermissionHelper.ChangeOtherGlobal(_hub, kinkster.UserData, kinkster.PairGlobals, res.name, newState),
                    PermissionType.PairPerm => PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.PairPerms, res.name, newState),
                    _ => Task.CompletedTask
                };
            }));
        }
    }

    public record PermDataPair(FAI IconOn, FAI IconOff, string CondTrue, string CondFalse, string Text);

    /// <summary> The Cache of PermissionData for each permission in the Magnifying Glass Window. </summary>
    private readonly ImmutableDictionary<SPPID, PermDataPair> PairPermData = ImmutableDictionary<SPPID, PermDataPair>.Empty
        .Add(SPPID.ChatGarblerActive,     new PermDataPair(FAI.MicrophoneSlash,       FAI.Microphone, "enabled",       "disabled",     "Chat Garbler"))
        .Add(SPPID.ChatGarblerLocked,     new PermDataPair(FAI.Key,                   FAI.UnlockAlt,  "locked",        "unlocked",     "Chat Garbler"))
        .Add(SPPID.PermanentLocks,        new PermDataPair(FAI.Infinity,              FAI.Ban,        "allows",        "prevents",     "Permanent Locks"))
        .Add(SPPID.OwnerLocks,            new PermDataPair(FAI.UserLock,              FAI.Ban,        "allows",        "prevents",     "Owner Locks"))
        .Add(SPPID.DevotionalLocks,       new PermDataPair(FAI.UserLock,              FAI.Ban,        "allows",        "prevents",     "Devotional Locks"))
        .Add(SPPID.GagVisuals,            new PermDataPair(FAI.Surprise,              FAI.Ban,        "enabled",       "disabled",     "Gag Visuals"))
        .Add(SPPID.ApplyGags,             new PermDataPair(FAI.Mask,                  FAI.Ban,        "allows",        "prevents",     "applying Gags"))
        .Add(SPPID.LockGags,              new PermDataPair(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Gags"))
        .Add(SPPID.MaxGagTime,            new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Gag Time"))
        .Add(SPPID.UnlockGags,            new PermDataPair(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Gags"))
        .Add(SPPID.RemoveGags,            new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Gags"))
        .Add(SPPID.RestrictionVisuals,    new PermDataPair(FAI.Tshirt,                FAI.Ban,        "enabled",       "disabled",     "Restriction Visuals"))
        .Add(SPPID.ApplyRestrictions,     new PermDataPair(FAI.Handcuffs,             FAI.Ban,        "allows",        "prevents",     "applying Restrictions"))
        .Add(SPPID.LockRestrictions,      new PermDataPair(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Restrictions"))
        .Add(SPPID.MaxRestrictionTime,    new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Restriction Time"))
        .Add(SPPID.UnlockRestrictions,    new PermDataPair(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Restrictions"))
        .Add(SPPID.RemoveRestrictions,    new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Restrictions"))
        .Add(SPPID.RestraintSetVisuals,   new PermDataPair(FAI.Tshirt,                FAI.Ban,        "enabled",       "disabled",     "Restraint Visuals"))
        .Add(SPPID.ApplyRestraintSets,    new PermDataPair(FAI.Handcuffs,             FAI.Ban,        "allows",        "prevents",     "applying Restraints"))
        .Add(SPPID.LockRestraintSets,     new PermDataPair(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Restraints"))
        .Add(SPPID.MaxRestraintTime,      new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Restraint Time"))
        .Add(SPPID.UnlockRestraintSets,   new PermDataPair(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Restraints"))
        .Add(SPPID.RemoveRestraintSets,   new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Restraints"))
        .Add(SPPID.PuppetPermSit,         new PermDataPair(FAI.Chair,                 FAI.Ban,        "allows",        "prevents",     "Sit Requests"))
        .Add(SPPID.PuppetPermEmote,       new PermDataPair(FAI.Walking,               FAI.Ban,        "allows",        "prevents",     "Emote Requests"))
        .Add(SPPID.PuppetPermAlias,       new PermDataPair(FAI.Scroll,                FAI.Ban,        "allows",        "prevents",     "Alias Requests"))
        .Add(SPPID.PuppetPermAll,         new PermDataPair(FAI.CheckDouble,           FAI.Ban,        "allows",        "prevents",     "All Requests"))
        .Add(SPPID.ApplyPositive,         new PermDataPair(FAI.SmileBeam,             FAI.Ban,        "allows",        "prevents",     "Positive Moodles"))
        .Add(SPPID.ApplyNegative,         new PermDataPair(FAI.FrownOpen,             FAI.Ban,        "allows",        "prevents",     "Negative Moodles"))
        .Add(SPPID.ApplySpecial,          new PermDataPair(FAI.WandMagicSparkles,     FAI.Ban,        "allows",        "prevents",     "Special Moodles"))
        .Add(SPPID.ApplyPairsMoodles,     new PermDataPair(FAI.PersonArrowUpFromLine, FAI.Ban,        "allows",        "prevents",     "applying your Moodles"))
        .Add(SPPID.ApplyOwnMoodles,       new PermDataPair(FAI.PersonArrowDownToLine, FAI.Ban,        "allows",        "prevents",     "applying their Moodles"))
        .Add(SPPID.MaxMoodleTime,         new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Moodle Time"))
        .Add(SPPID.PermanentMoodles,      new PermDataPair(FAI.Infinity,              FAI.Ban,        "allows",        "prevents",     "permanent Moodles"))
        .Add(SPPID.RemoveMoodles,         new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Moodles"))
        .Add(SPPID.ToyControl,            new PermDataPair(FAI.Phone,                 FAI.Ban,        "allows",        "prevents",     "Remote Controlling"))
        .Add(SPPID.PatternStarting,       new PermDataPair(FAI.Play,                  FAI.Ban,        "allows",        "prevents",     "Pattern Starting"))
        .Add(SPPID.PatternStopping,       new PermDataPair(FAI.Stop,                  FAI.Ban,        "allows",        "prevents",     "Pattern Stopping"))
        .Add(SPPID.AlarmToggling,         new PermDataPair(FAI.Bell,                  FAI.Ban,        "allows",        "prevents",     "Alarm Toggling"))
        .Add(SPPID.TriggerToggling,       new PermDataPair(FAI.FileMedicalAlt,        FAI.Ban,        "allows",        "prevents",     "Trigger Toggling"))
        .Add(SPPID.HardcoreModeState,     new PermDataPair(FAI.Bolt,                  FAI.Ban,        "In",            "Not in",       "Hardcore Mode"));
}