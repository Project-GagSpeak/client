using CkCommons;using CkCommons.Gui;using CkCommons.Raii;using Dalamud.Interface.Colors;using Dalamud.Interface.Utility.Raii;using GagSpeak.Kinksters;using GagSpeak.Services;using GagSpeak.Utils;using GagSpeak.WebAPI;using GagspeakAPI.Extensions;using ImGuiNET;using OtterGui;using OtterGui.Text;using System.Collections.Immutable;namespace GagSpeak.Gui.Components;

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

    public void DrawPermissions(Kinkster kinkster, string dispName, float width)    {
        ImGuiUtil.Center($"{dispName}'s Permissions for You");
        ImGui.Separator();
        
        // have to make child object below the preset selector for a scrollable interface.
        using var _ = CkRaii.Child("KinksterPerms", new Vector2(0, ImGui.GetContentRegionAvail().Y), wFlags: WFlags.NoScrollbar);
        
        ImGui.TextUnformatted("Global Settings");        DrawPermRow(kinkster, dispName, width, SPPID.ChatGarblerActive,       kinkster.PairGlobals.ChatGarblerActive,       kinkster.PairPermAccess.ChatGarblerActiveAllowed );        DrawPermRow(kinkster, dispName, width, SPPID.ChatGarblerLocked,       kinkster.PairGlobals.ChatGarblerLocked,       kinkster.PairPermAccess.ChatGarblerLockedAllowed );
        DrawPermRow(kinkster, dispName, width, SPPID.GaggedNameplate,         kinkster.PairGlobals.GaggedNameplate,         kinkster.PairPermAccess.GaggedNameplateAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Padlock Permissions");        DrawPermRow(kinkster, dispName, width, SPPID.PermanentLocks,          kinkster.PairPerms.PermanentLocks,            kinkster.PairPermAccess.PermanentLocksAllowed );        DrawPermRow(kinkster, dispName, width, SPPID.OwnerLocks,              kinkster.PairPerms.OwnerLocks,                kinkster.PairPermAccess.OwnerLocksAllowed );        DrawPermRow(kinkster, dispName, width, SPPID.DevotionalLocks,         kinkster.PairPerms.DevotionalLocks,           kinkster.PairPermAccess.DevotionalLocksAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Gag Permissions");        DrawPermRow(kinkster, dispName, width, SPPID.GagVisuals,              kinkster.PairGlobals.GagVisuals,              kinkster.PairPermAccess.GagVisualsAllowed );        DrawPermRow(kinkster, dispName, width, SPPID.ApplyGags,               kinkster.PairPerms.ApplyGags,                 kinkster.PairPermAccess.ApplyGagsAllowed );        DrawPermRow(kinkster, dispName, width, SPPID.LockGags,                kinkster.PairPerms.LockGags,                  kinkster.PairPermAccess.LockGagsAllowed );        DrawPermRow(kinkster, dispName, width, SPPID.MaxGagTime,              kinkster.PairPerms.MaxGagTime,                kinkster.PairPermAccess.MaxGagTimeAllowed );        DrawPermRow(kinkster, dispName, width, SPPID.UnlockGags,              kinkster.PairPerms.UnlockGags,                kinkster.PairPermAccess.UnlockGagsAllowed );        DrawPermRow(kinkster, dispName, width, SPPID.RemoveGags,              kinkster.PairPerms.RemoveGags,                kinkster.PairPermAccess.RemoveGagsAllowed );        ImGui.Separator();        ImGui.TextUnformatted("Restriction Permissions");        DrawPermRow(kinkster, dispName, width, SPPID.RestrictionVisuals,      kinkster.PairGlobals.RestrictionVisuals,      kinkster.PairPermAccess.RestrictionVisualsAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.ApplyRestrictions,       kinkster.PairPerms.ApplyRestrictions,         kinkster.PairPermAccess.ApplyRestrictionsAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.LockRestrictions,        kinkster.PairPerms.LockRestrictions,          kinkster.PairPermAccess.LockRestrictionsAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.MaxRestrictionTime,      kinkster.PairPerms.MaxRestrictionTime,        kinkster.PairPermAccess.MaxRestrictionTimeAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.UnlockRestrictions,      kinkster.PairPerms.UnlockRestrictions,        kinkster.PairPermAccess.UnlockRestrictionsAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.RemoveRestrictions,      kinkster.PairPerms.RemoveRestrictions,        kinkster.PairPermAccess.RemoveRestrictionsAllowed);
        ImGui.Separator();

        ImGui.TextUnformatted("Restraint Set Permissions");
        DrawPermRow(kinkster, dispName, width, SPPID.RestraintSetVisuals,     kinkster.PairGlobals.RestraintSetVisuals,     kinkster.PairPermAccess.RestraintSetVisualsAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.ApplyRestraintSets,      kinkster.PairPerms.ApplyRestraintSets,        kinkster.PairPermAccess.ApplyRestraintSetsAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.ApplyLayers,             kinkster.PairPerms.ApplyLayers,               kinkster.PairPermAccess.ApplyLayersAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.ApplyLayersWhileLocked,  kinkster.PairPerms.ApplyLayersWhileLocked,    kinkster.PairPermAccess.ApplyLayersWhileLockedAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.LockRestraintSets,       kinkster.PairPerms.LockRestraintSets,         kinkster.PairPermAccess.LockRestraintSetsAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.MaxRestraintTime,        kinkster.PairPerms.MaxRestraintTime,          kinkster.PairPermAccess.MaxRestraintTimeAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.UnlockRestraintSets,     kinkster.PairPerms.UnlockRestraintSets,       kinkster.PairPermAccess.UnlockRestraintSetsAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.RemoveLayers,            kinkster.PairPerms.RemoveLayers,              kinkster.PairPermAccess.RemoveLayersAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.RemoveLayersWhileLocked, kinkster.PairPerms.RemoveLayersWhileLocked,   kinkster.PairPermAccess.RemoveLayersWhileLockedAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.RemoveRestraintSets,     kinkster.PairPerms.RemoveRestraintSets,       kinkster.PairPermAccess.RemoveRestraintSetsAllowed);
        ImGui.Separator();

        ImGui.TextUnformatted("Puppeteer Permissions");
        DrawPermRow(kinkster, dispName, width, SPPID.PuppetPermSit,           kinkster.PairPerms.PuppetPerms,              kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.Sit);
        DrawPermRow(kinkster, dispName, width, SPPID.PuppetPermEmote,         kinkster.PairPerms.PuppetPerms,              kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.Emotes);
        DrawPermRow(kinkster, dispName, width, SPPID.PuppetPermAlias,         kinkster.PairPerms.PuppetPerms,              kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.Alias);
        DrawPermRow(kinkster, dispName, width, SPPID.PuppetPermAll,           kinkster.PairPerms.PuppetPerms,              kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.All);
        ImGui.Separator();

        ImGui.TextUnformatted("Moodles Permissions");
        DrawPermRow(kinkster, dispName, width, SPPID.ApplyPositive,           kinkster.PairPerms.MoodlePerms,              kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.PositiveStatusTypes);
        DrawPermRow(kinkster, dispName, width, SPPID.ApplyNegative,           kinkster.PairPerms.MoodlePerms,              kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.NegativeStatusTypes);
        DrawPermRow(kinkster, dispName, width, SPPID.ApplySpecial,            kinkster.PairPerms.MoodlePerms,              kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.SpecialStatusTypes);
        DrawPermRow(kinkster, dispName, width, SPPID.ApplyOwnMoodles,         kinkster.PairPerms.MoodlePerms,              kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.PairCanApplyTheirMoodlesToYou);
        DrawPermRow(kinkster, dispName, width, SPPID.ApplyPairsMoodles,       kinkster.PairPerms.MoodlePerms,              kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.PairCanApplyYourMoodlesToYou);
        DrawPermRow(kinkster, dispName, width, SPPID.MaxMoodleTime,           kinkster.PairPerms.MaxMoodleTime,            kinkster.PairPermAccess.MaxMoodleTimeAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.PermanentMoodles,        kinkster.PairPerms.MoodlePerms,              kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.PermanentMoodles);
        DrawPermRow(kinkster, dispName, width, SPPID.RemoveMoodles,           kinkster.PairPerms.MoodlePerms,              kinkster.PairPermAccess.MoodlePermsAllowed, MoodlePerms.RemovingMoodles);
        ImGui.Separator();

        ImGui.TextUnformatted("Misc. Permissions");
        DrawPermRow(kinkster, dispName, width, SPPID.HypnosisMaxTime,         kinkster.PairPerms.MaxHypnosisTime,          kinkster.PairPermAccess.HypnosisMaxTimeAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.HypnosisEffect,          kinkster.PairPerms.HypnoEffectSending,       kinkster.PairPermAccess.HypnosisSendingAllowed);
        ImGui.Separator();

        ImGui.TextUnformatted("Toybox Permissions");
        DrawPermRow(kinkster, dispName, width, SPPID.PatternStarting,         kinkster.PairPerms.ExecutePatterns,          kinkster.PairPermAccess.ExecutePatternsAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.PatternStopping,         kinkster.PairPerms.StopPatterns,             kinkster.PairPermAccess.StopPatternsAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.AlarmToggling,           kinkster.PairPerms.ToggleAlarms,             kinkster.PairPermAccess.ToggleAlarmsAllowed);
        DrawPermRow(kinkster, dispName, width, SPPID.TriggerToggling,         kinkster.PairPerms.ToggleTriggers,           kinkster.PairPermAccess.ToggleTriggersAllowed);
        ImGui.Separator();

        ImGui.TextUnformatted("Hardcore State");
        CkGui.ColorTextInline("(View-Only)", ImGuiColors.DalamudRed);

        DrawHcRow(kinkster, dispName, width, SPPID.LockedFollowing, kinkster.PairGlobals.LockedFollowing, kinkster.PairPerms.AllowLockedFollowing);
        DrawHcRow(kinkster, dispName, width, SPPID.LockedEmoteState, kinkster.PairGlobals.LockedEmoteState, kinkster.PairPerms.AllowLockedEmoting || kinkster.PairPerms.AllowLockedSitting);
        DrawHcRow(kinkster, dispName, width, SPPID.IndoorConfinement, kinkster.PairGlobals.IndoorConfinement, kinkster.PairPerms.AllowIndoorConfinement);
        DrawHcRow(kinkster, dispName, width, SPPID.Imprisonment, kinkster.PairGlobals.Imprisonment, kinkster.PairPerms.AllowImprisonment);
        DrawHcRow(kinkster, dispName, width, SPPID.ChatBoxesHidden, kinkster.PairGlobals.ChatBoxesHidden, kinkster.PairPerms.AllowHidingChatBoxes);
        DrawHcRow(kinkster, dispName, width, SPPID.ChatInputHidden, kinkster.PairGlobals.ChatInputHidden, kinkster.PairPerms.AllowHidingChatInput);
        DrawHcRow(kinkster, dispName, width, SPPID.ChatInputBlocked, kinkster.PairGlobals.ChatInputBlocked, kinkster.PairPerms.AllowChatInputBlocking);    }

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
        var inputTxtWidth = width * .4f;
        var str = _timespanCache.TryGetValue(perm, out var value) ? value : current.ToGsRemainingTime();
        var data = PairPermData[perm];

        if (CkGui.IconInputText(data.IconYes, data.Label, "0d0h0m0s", ref str, 32, inputTxtWidth, true, !canEdit))
        {
            if (str != current.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(str, out var newTime))
            {
                var ticks = (ulong)newTime.Ticks;
                var res = perm.ToPermValue();

                // Assign the blocking task if allowed.
                if (!res.name.IsNullOrEmpty() && res.type is PermissionType.PairPerm)
                {
                    Svc.Logger.Information($"Attempting to change {dispName}'s {res.name} to {ticks} ticks.", LoggerType.UI);
                    UiService.SetUITask(async () => await PermissionHelper.ChangeOtherUnique(_hub, kinkster.UserData, kinkster.PairPerms, res.name, ticks));
                }
            }
            _timespanCache.Remove(perm);
        }
        CkGui.AttachToolTip($"The Maximum Time {dispName} can be locked for.");

        ImGui.SameLine(width - ImGui.GetFrameHeight());
        CkGui.BooleanToColoredIcon(canEdit, false, FAI.Pen, FAI.Pen);
        CkGui.AttachToolTip(canEdit ? $"{dispName} allows you to change this." : $"Only {dispName} can update this permission.");
    }

    private void DrawPermRowCommon<T>(Kinkster kinkster, string dispName, float width, SPPID perm, bool current, bool canEdit, Func<T> newStateFunc)
    {
        using var butt = ImRaii.PushColor(ImGuiCol.Button, 0);
        
        var data = PairPermData[perm];
        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();

        // change to ckgui for disabled?
        if(ImGui.Button("##pair" + perm, new Vector2(buttonW, ImGui.GetFrameHeight())) && canEdit)
        {
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;

            UiService.SetUITask(async () =>
            {
                switch(res.type)
                {
                    case PermissionType.Global:
                        await PermissionHelper.ChangeOtherGlobal(_hub, kinkster.UserData, kinkster.PairGlobals, res.name, newState);
                        break;
                    case PermissionType.PairPerm:
                        await PermissionHelper.ChangeOtherUnique(_hub, kinkster.UserData, kinkster.PairPerms, res.name, newState);
                        break;
                    default:
                        break;
                };
            });
        }
        ImUtf8.SameLineInner();
        CkGui.BooleanToColoredIcon(canEdit, false, FAI.Pen, FAI.Pen);
        CkGui.AttachToolTip(dispName + (canEdit
            ? " allows you to change this permission at will."
            : " is preventing you from changing this permission. Only they can update it."));

        ImGui.SetCursorScreenPos(pos);
        PrintButtonRichText(data, dispName, current, canEdit);
        if (canEdit)
            CkGui.AttachToolTip($"Toggle {dispName}'s permission.");
    }

    private void DrawHcRow(Kinkster k, string name, float width, SPPID perm, string current, bool grantedAllowance)
    {
        using var butt = ImRaii.PushColor(ImGuiCol.Button, 0);
        var data = ClientHcPermData[perm];
        var isActive = !string.IsNullOrEmpty(current);
        var isPairlocked = GlobalPermsEx.IsDevotional(current);

        var pos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(width - (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X), ImGui.GetFrameHeight()));
        if (grantedAllowance)
        {
            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.UserLock, ImGuiColors.HealerGreen);
            CkGui.AttachToolTip($"{name} {data.AllowedTT}");
        }

        // go back to the dummy and draw out the text stuff.
        // go back and draw inside the dummy.
        ImGui.SetCursorScreenPos(pos);
        var iconCol = isPairlocked ? ImGuiColors.DalamudRed.ToUint() : uint.MaxValue;
        CkGui.FramedIconText(isActive ? data.IconActive : data.IconInactive, iconCol);
        if (isPairlocked)
            CkGui.AttachToolTip("This status was Devotion Locked, and can only be changed by the assigner!");

        // print the text row.
        using var _ = ImRaii.PushStyle(ImGuiStyleVar.Alpha, .5f);
        if (isActive)
        {
            CkGui.TextFrameAlignedInline($"{name}{data.ActionText}");
            ImGui.SameLine(0, 0);
            CkGui.ColorTextFrameAlignedInline(isActive ? GlobalPermsEx.PermEnactor(current).AsAnonKinkster() : "ANON.KINKSTER", CkColor.VibrantPink.Uint());
        }
        else
        {
            CkGui.TextFrameAlignedInline($"{name}{data.InactiveText}");
        }
    }

    private void PrintButtonRichText(PermDataPair pdp, string dispName, bool current, bool canEdit)
    {
        using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, canEdit ? 1f : 0.5f);
        using var _ = ImRaii.Group();

        CkGui.FramedIconText(current ? pdp.IconYes : pdp.IconNo);
        CkGui.TextFrameAlignedInline($"{dispName}");
        ImGui.SameLine(0, 0);
        if (pdp.CondAfterLabel)
        {
            ImGui.Text($"'s {pdp.Label} {pdp.suffix} ");
            ImGui.SameLine(0, 0);
            CkGui.ColorTextFrameAligned(current ? pdp.CondTrue : pdp.CondFalse, current ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
            ImGui.SameLine(0, 0);
            ImGui.Text(".");
        }
        else
        {
            CkGui.ColorTextFrameAligned($" {(current ? pdp.CondTrue : pdp.CondFalse)}", current ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
            ImGui.SameLine(0, 0);
            ImGui.Text($" {pdp.Label}.");
        }
    }

    public record HcStatusKinkster(FAI IconActive, FAI IconInactive, string PermLabel, string ActionText, string InactiveText, string AllowedTT);

    private readonly ImmutableDictionary<SPPID, HcStatusKinkster> ClientHcPermData = ImmutableDictionary<SPPID, HcStatusKinkster>.Empty
        .Add(SPPID.LockedFollowing,    new HcStatusKinkster(FAI.Walking,           FAI.Ban, "Forced Follow",         " is following",           " is not following anyone.",    "has allowed you to enact forced follow on them."))
        .Add(SPPID.LockedEmoteState,   new HcStatusKinkster(FAI.PersonArrowDownToLine, FAI.Ban, "Forced Emote Lock", " is in emote lock for",   " is not emote locked.",        "has allowed you to lock them in an emote loop.")) // Handle this separately, it has its own call.
        .Add(SPPID.IndoorConfinement,  new HcStatusKinkster(FAI.HouseLock,         FAI.Ban, "Indoor Confinement",    " is confined by",         " is not confined.",            "has allowed you to confine them indoors."))
        .Add(SPPID.Imprisonment,       new HcStatusKinkster(FAI.Bars,              FAI.Ban, "Imprisonment",          " is imprisoned by",       " is not imprisoned.",          "has allowed you to imprison them at a desired location.--SEP--They must be nearby when giving a location besides your current position."))
        .Add(SPPID.ChatBoxesHidden,    new HcStatusKinkster(FAI.CommentSlash,      FAI.Ban, "Chatbox Visibility",    "'s chatbox hidden by",    "'s chat is visible.",          "has allowed you to hide their chat--NL--Note: This will prevent them from seeing your messages, or anyone else's."))
        .Add(SPPID.ChatInputHidden,    new HcStatusKinkster(FAI.CommentSlash,      FAI.Ban, "ChatInput Visibility",  "'s input hidden by",      "'s chat input is visible.",    "has allowed you to hide their chat input.--NL--Note: They will still be able to type, but can't see what they type."))
        .Add(SPPID.ChatInputBlocked,   new HcStatusKinkster(FAI.CommentDots,       FAI.Ban, "ChatInput Blocking",    "'s input blocked by",     "'s chat input is accessible.", "has allowed you to block their chat input.--SEP--THEIR SAFEWORD IN THIS CASE IS --COL--CTRL + ALT + BACKSPACE (FUCK GO BACK)--COL--"));

    // adjust for prefixes and suffixs?
    public record PermDataPair(FAI IconYes, FAI IconNo, string CondTrue, string CondFalse, string Label, bool CondAfterLabel, string suffix = "");

    /// <summary> The Cache of PermissionData for each permission in the Magnifying Glass Window. </summary>
    private readonly ImmutableDictionary<SPPID, PermDataPair> PairPermData = ImmutableDictionary<SPPID, PermDataPair>.Empty
        .Add(SPPID.ChatGarblerActive,     new PermDataPair(FAI.MicrophoneSlash,       FAI.Microphone, "enabled",       "disabled",     "Chat Garbler",           true , "is"))
        .Add(SPPID.ChatGarblerLocked,     new PermDataPair(FAI.Key,                   FAI.UnlockAlt,  "locked",        "unlocked",     "Chat Garbler",           true , "is"))
        .Add(SPPID.GaggedNameplate,       new PermDataPair(FAI.IdCard,                FAI.Ban,        "enabled",       "disabled",     "GagPlates",              true , "are"))

        .Add(SPPID.PermanentLocks,        new PermDataPair(FAI.Infinity,              FAI.Ban,        "allows",        "prevents",     "permanent locks",        false))
        .Add(SPPID.OwnerLocks,            new PermDataPair(FAI.UserLock,              FAI.Ban,        "allows",        "prevents",     "owner locks",            false))
        .Add(SPPID.DevotionalLocks,       new PermDataPair(FAI.UserLock,              FAI.Ban,        "allows",        "prevents",     "devotional locks",       false))

        .Add(SPPID.GagVisuals,            new PermDataPair(FAI.Surprise,              FAI.Ban,        "enabled",       "disabled",     "Gag Visuals",            true , "are"))
        .Add(SPPID.ApplyGags,             new PermDataPair(FAI.Mask,                  FAI.Ban,        "allows",        "prevents",     "applying Gags",          false))
        .Add(SPPID.LockGags,              new PermDataPair(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Gags",           false))
        .Add(SPPID.MaxGagTime,            new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Gag time",           false))
        .Add(SPPID.UnlockGags,            new PermDataPair(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Gags",         false))
        .Add(SPPID.RemoveGags,            new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Gags",          false))

        .Add(SPPID.RestrictionVisuals,    new PermDataPair(FAI.Tshirt,                FAI.Ban,        "enabled",       "disabled",     "Restriction visuals",    true , "are"))
        .Add(SPPID.ApplyRestrictions,     new PermDataPair(FAI.Handcuffs,             FAI.Ban,        "allows",        "prevents",     "applying Restrictions",  false))
        .Add(SPPID.LockRestrictions,      new PermDataPair(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Restrictions",   false))
        .Add(SPPID.MaxRestrictionTime,    new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Restriction time",   false))
        .Add(SPPID.UnlockRestrictions,    new PermDataPair(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Restrictions", false))
        .Add(SPPID.RemoveRestrictions,    new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Restrictions",  false))

        .Add(SPPID.RestraintSetVisuals,   new PermDataPair(FAI.Tshirt,                FAI.Ban,        "enabled",       "disabled",     "Restraint visuals",      true , "are"))
        .Add(SPPID.ApplyRestraintSets,    new PermDataPair(FAI.Handcuffs,             FAI.Ban,        "allows",        "prevents",     "applying Restraints",    false))
        .Add(SPPID.ApplyLayers,           new PermDataPair(FAI.LayerGroup,            FAI.Ban,        "allows",        "prevents",     "applying Layers",        false))
        .Add(SPPID.ApplyLayersWhileLocked,new PermDataPair(FAI.LayerGroup,            FAI.Ban,        "allows",        "prevents",     "applying locked Layers", false))
        .Add(SPPID.LockRestraintSets,     new PermDataPair(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Restraints",     false))
        .Add(SPPID.MaxRestraintTime,      new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Restraint time",     false))
        .Add(SPPID.UnlockRestraintSets,   new PermDataPair(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Restraints",   false))
        .Add(SPPID.RemoveLayers,          new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Layers",        false))
        .Add(SPPID.RemoveLayersWhileLocked,new PermDataPair(FAI.Eraser,               FAI.Ban,        "allows",        "prevents",     "removing locked Layers", false))
        .Add(SPPID.RemoveRestraintSets,   new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Restraints",    false))

        .Add(SPPID.PuppetPermSit,         new PermDataPair(FAI.Chair,                 FAI.Ban,        "allows",        "prevents",     "sit Requests",           false))
        .Add(SPPID.PuppetPermEmote,       new PermDataPair(FAI.Walking,               FAI.Ban,        "allows",        "prevents",     "emote Requests",         false))
        .Add(SPPID.PuppetPermAlias,       new PermDataPair(FAI.Scroll,                FAI.Ban,        "allows",        "prevents",     "alias Requests",         false))
        .Add(SPPID.PuppetPermAll,         new PermDataPair(FAI.CheckDouble,           FAI.Ban,        "allows",        "prevents",     "all Requests",           false))

        .Add(SPPID.ApplyPositive,         new PermDataPair(FAI.SmileBeam,             FAI.Ban,        "allows",        "prevents",     "positive Moodles",       false))
        .Add(SPPID.ApplyNegative,         new PermDataPair(FAI.FrownOpen,             FAI.Ban,        "allows",        "prevents",     "negative Moodles",       false))
        .Add(SPPID.ApplySpecial,          new PermDataPair(FAI.WandMagicSparkles,     FAI.Ban,        "allows",        "prevents",     "special Moodles",        false))
        .Add(SPPID.ApplyPairsMoodles,     new PermDataPair(FAI.PersonArrowUpFromLine, FAI.Ban,        "allows",        "prevents",     "applying your Moodles",  false))
        .Add(SPPID.ApplyOwnMoodles,       new PermDataPair(FAI.PersonArrowDownToLine, FAI.Ban,        "allows",        "prevents",     "applying their Moodles", false))
        .Add(SPPID.MaxMoodleTime,         new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Moodle time",        false))
        .Add(SPPID.PermanentMoodles,      new PermDataPair(FAI.Infinity,              FAI.Ban,        "allows",        "prevents",     "permanent Moodles",      false))
        .Add(SPPID.RemoveMoodles,         new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Moodles",       false))

        .Add(SPPID.HypnosisMaxTime,       new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max hypnosis time",      false))
        .Add(SPPID.HypnosisEffect,        new PermDataPair(FAI.CameraRotate,          FAI.Ban,        "allows",        "prevents",     "Hypno effect sending",   false))

        .Add(SPPID.PatternStarting,       new PermDataPair(FAI.Play,                  FAI.Ban,        "allows",        "prevents",     "Pattern starting",       false))
        .Add(SPPID.PatternStopping,       new PermDataPair(FAI.Stop,                  FAI.Ban,        "allows",        "prevents",     "Pattern stopping",       false))
        .Add(SPPID.AlarmToggling,         new PermDataPair(FAI.Bell,                  FAI.Ban,        "allows",        "prevents",     "Alarm toggling",         false))
        .Add(SPPID.TriggerToggling,       new PermDataPair(FAI.FileMedicalAlt,        FAI.Ban,        "allows",        "prevents",     "Trigger toggling",       false));}