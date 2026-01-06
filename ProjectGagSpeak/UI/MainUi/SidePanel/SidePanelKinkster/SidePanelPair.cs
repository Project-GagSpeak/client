using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

public partial class SidePanelPair
{
    private readonly ILogger<SidePanelPair> _logger;
    private readonly MainHub _hub;
    private readonly PiShockProvider _shockies;

    private Dictionary<KPID, string> _timespanCache = new();
    private static IconCheckboxEx EditAccessCheckbox = new(FAI.Pen, 0xFF00FF00, 0);
    private static IconCheckboxEx HardcoreCheckbox = new(FAI.UserLock, 0xFF00FF00, 0xFF0000FF);

    public SidePanelPair(ILogger<SidePanelPair> logger, MainHub hub, PiShockProvider shockies)
    {
        _logger = logger;
        _hub = hub;
        _shockies = shockies;
    }

    public void DrawClientPermissions(KinksterInfoCache cache, Kinkster kinkster, string dispName, float width)
    {
        ImGuiUtil.Center($"Your Permissions for {dispName}");
        DrawPresetList(cache, kinkster, width);
        ImGui.Separator();

        // Child area for scrolling.
        using var _ = CkRaii.Child("ClientPermsForKinkster", ImGui.GetContentRegionAvail(), wFlags: WFlags.NoScrollbar);
        // Change this later to reside in an internal accessor, that is not static. (Or maybe make static but more centralized.)
        if (ClientData.Globals is not { } globals || ClientData.Hardcore is not { } hc)
            return;

        ImGui.TextUnformatted("Global Settings");
        using (ImRaii.Group())
        {
            ClientPermRow(kinkster, dispName, width, KPID.ChatGarblerActive, globals.ChatGarblerActive, kinkster.OwnPermAccess.ChatGarblerActiveAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.ChatGarblerLocked, globals.ChatGarblerLocked, kinkster.OwnPermAccess.ChatGarblerLockedAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.GaggedNameplate, globals.GaggedNameplate, kinkster.OwnPermAccess.GaggedNameplateAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();

        ImGui.TextUnformatted("Padlock Permissions");
        using (ImRaii.Group())
        {
            ClientPermRow(kinkster, dispName, width, KPID.PermanentLocks, kinkster.OwnPerms.PermanentLocks, kinkster.OwnPermAccess.PermanentLocksAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.OwnerLocks, kinkster.OwnPerms.OwnerLocks, kinkster.OwnPermAccess.OwnerLocksAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.DevotionalLocks, kinkster.OwnPerms.DevotionalLocks, kinkster.OwnPermAccess.DevotionalLocksAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();

        ImGui.TextUnformatted("Gag Permissions");
        using (ImRaii.Group())
        {
            ClientPermRow(kinkster, dispName, width, KPID.GagVisuals, globals.GagVisuals, kinkster.OwnPermAccess.GagVisualsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.ApplyGags, kinkster.OwnPerms.ApplyGags, kinkster.OwnPermAccess.ApplyGagsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.LockGags, kinkster.OwnPerms.LockGags, kinkster.OwnPermAccess.LockGagsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.MaxGagTime, kinkster.OwnPerms.MaxGagTime, kinkster.OwnPermAccess.MaxGagTimeAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.UnlockGags, kinkster.OwnPerms.UnlockGags, kinkster.OwnPermAccess.UnlockGagsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.RemoveGags, kinkster.OwnPerms.RemoveGags, kinkster.OwnPermAccess.RemoveGagsAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();

        ImGui.TextUnformatted("Restriction Permissions");
        using (ImRaii.Group())
        {
            ClientPermRow(kinkster, dispName, width, KPID.RestrictionVisuals, globals.RestrictionVisuals, kinkster.OwnPermAccess.RestrictionVisualsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.ApplyRestrictions, kinkster.OwnPerms.ApplyRestrictions, kinkster.OwnPermAccess.ApplyRestrictionsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.LockRestrictions, kinkster.OwnPerms.LockRestrictions, kinkster.OwnPermAccess.LockRestrictionsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.MaxRestrictionTime, kinkster.OwnPerms.MaxRestrictionTime, kinkster.OwnPermAccess.MaxRestrictionTimeAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.UnlockRestrictions, kinkster.OwnPerms.UnlockRestrictions, kinkster.OwnPermAccess.UnlockRestrictionsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.RemoveRestrictions, kinkster.OwnPerms.RemoveRestrictions, kinkster.OwnPermAccess.RemoveRestrictionsAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();

        ImGui.TextUnformatted("Restraint Set Permissions");
        using (ImRaii.Group())
        {
            ClientPermRow(kinkster, dispName, width, KPID.RestraintSetVisuals, globals.RestraintSetVisuals, kinkster.OwnPermAccess.RestraintSetVisualsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.ApplyRestraintSets, kinkster.OwnPerms.ApplyRestraintSets, kinkster.OwnPermAccess.ApplyRestraintSetsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.ApplyLayers, kinkster.OwnPerms.ApplyLayers, kinkster.OwnPermAccess.ApplyLayersAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.ApplyLayersWhileLocked, kinkster.OwnPerms.ApplyLayersWhileLocked, kinkster.OwnPermAccess.ApplyLayersWhileLockedAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.LockRestraintSets, kinkster.OwnPerms.LockRestraintSets, kinkster.OwnPermAccess.LockRestraintSetsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.MaxRestraintTime, kinkster.OwnPerms.MaxRestraintTime, kinkster.OwnPermAccess.MaxRestraintTimeAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.UnlockRestraintSets, kinkster.OwnPerms.UnlockRestraintSets, kinkster.OwnPermAccess.UnlockRestraintSetsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.RemoveLayers, kinkster.OwnPerms.RemoveLayers, kinkster.OwnPermAccess.RemoveLayersAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.RemoveLayersWhileLocked, kinkster.OwnPerms.RemoveLayersWhileLocked, kinkster.OwnPermAccess.RemoveLayersWhileLockedAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.RemoveRestraintSets, kinkster.OwnPerms.RemoveRestraintSets, kinkster.OwnPermAccess.RemoveRestraintSetsAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();

        ImGui.TextUnformatted("Puppeteer Permissions");
        using (ImRaii.Group())
        {
            ClientPermRow(kinkster, dispName, width, KPID.PuppetPermSit, kinkster.OwnPerms.PuppetPerms, kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Sit);
            ClientPermRow(kinkster, dispName, width, KPID.PuppetPermEmote, kinkster.OwnPerms.PuppetPerms, kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Emotes);
            ClientPermRow(kinkster, dispName, width, KPID.PuppetPermAlias, kinkster.OwnPerms.PuppetPerms, kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.Alias);
            ClientPermRow(kinkster, dispName, width, KPID.PuppetPermAll, kinkster.OwnPerms.PuppetPerms, kinkster.OwnPermAccess.PuppetPermsAllowed, PuppetPerms.All);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();

        ImGui.TextUnformatted("Moodles Permissions");
        using (ImRaii.Group())
        {
            ClientPermRow(kinkster, dispName, width, KPID.ApplyPositive, kinkster.OwnPerms.MoodleAccess, kinkster.OwnPermAccess.MoodleAccessAllowed, MoodleAccess.Positive);
            ClientPermRow(kinkster, dispName, width, KPID.ApplyNegative, kinkster.OwnPerms.MoodleAccess, kinkster.OwnPermAccess.MoodleAccessAllowed, MoodleAccess.Negative);
            ClientPermRow(kinkster, dispName, width, KPID.ApplySpecial, kinkster.OwnPerms.MoodleAccess, kinkster.OwnPermAccess.MoodleAccessAllowed, MoodleAccess.Special);
            ClientPermRow(kinkster, dispName, width, KPID.ApplyOwnMoodles, kinkster.OwnPerms.MoodleAccess, kinkster.OwnPermAccess.MoodleAccessAllowed, MoodleAccess.AllowOther);
            ClientPermRow(kinkster, dispName, width, KPID.ApplyPairsMoodles, kinkster.OwnPerms.MoodleAccess, kinkster.OwnPermAccess.MoodleAccessAllowed, MoodleAccess.AllowOwn);
            ClientPermRow(kinkster, dispName, width, KPID.MaxMoodleTime, kinkster.OwnPerms.MaxMoodleTime, kinkster.OwnPermAccess.MaxMoodleTimeAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.PermanentMoodles, kinkster.OwnPerms.MoodleAccess, kinkster.OwnPermAccess.MoodleAccessAllowed, MoodleAccess.Permanent);
            ClientPermRow(kinkster, dispName, width, KPID.RemoveAppliedMoodles, kinkster.OwnPerms.MoodleAccess, kinkster.OwnPermAccess.MoodleAccessAllowed, MoodleAccess.RemoveApplied);
            ClientPermRow(kinkster, dispName, width, KPID.RemoveAnyMoodles, kinkster.OwnPerms.MoodleAccess, kinkster.OwnPermAccess.MoodleAccessAllowed, MoodleAccess.RemoveAny);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();

        ImGui.TextUnformatted("Miscellaneous Permissions");
        using (ImRaii.Group())
        {
            ClientPermRow(kinkster, dispName, width, KPID.HypnosisMaxTime, kinkster.OwnPerms.MaxHypnosisTime, kinkster.OwnPermAccess.HypnosisMaxTimeAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.HypnosisEffect, kinkster.OwnPerms.HypnoEffectSending, kinkster.OwnPermAccess.HypnosisSendingAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();

        ImGui.TextUnformatted("Toybox Permissions");
        using (ImRaii.Group())
        {
            ClientPermRow(kinkster, dispName, width, KPID.PatternStarting, kinkster.OwnPerms.ExecutePatterns, kinkster.OwnPermAccess.ExecutePatternsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.PatternStopping, kinkster.OwnPerms.StopPatterns, kinkster.OwnPermAccess.StopPatternsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.AlarmToggling, kinkster.OwnPerms.ToggleAlarms, kinkster.OwnPermAccess.ToggleAlarmsAllowed);
            ClientPermRow(kinkster, dispName, width, KPID.TriggerToggling, kinkster.OwnPerms.ToggleTriggers, kinkster.OwnPermAccess.ToggleTriggersAllowed);
        }
        CkGui.AttachToolTip($"Cannot change perms for {dispName} in Hardcore mode!", !kinkster.OwnPerms.InHardcore);
        ImGui.Separator();

        // Probably a good idea to add a warning here on a popup or something idk.
        ImGui.TextUnformatted("Hardcore Permissions");
        ImUtf8.SameLineInner();
        CkGui.HoverIconText(FAI.QuestionCircle, ImGuiColors.TankBlue.ToUint(), ImGui.GetColorU32(ImGuiCol.TextDisabled));
        if (ImGui.IsItemHovered())
        {
            using (UiFontService.UidFont.Push())
                CkGui.AttachToolTip($"--COL--IMPORTANT:--COL-- Once in hardcore mode, you can only change EditAccess for {dispName}." +
                    $"--NL--{dispName} will have control over any permissions they have edit access to instead." +
                    "--NL--Be sure you are ok with this before enabling!", color: ImGuiColors.DalamudRed);
        }

        // True when they wish to enter hardcore mode.
        if (InHardcoreRow(kinkster, dispName, width))
            ImGui.OpenPopup("Confirm Hardcore");

        ClientHcRow(kinkster, dispName, width, KPID.GarbleChannelEditing, kinkster.OwnPerms.AllowGarbleChannelEditing);
        ClientHcRow(kinkster, dispName, width, KPID.HypnoticImage, kinkster.OwnPerms.AllowHypnoImageSending);
        ClientHcStateRow(kinkster, dispName, width, KPID.LockedFollowing, nameof(HardcoreStatus.LockedFollowing), hc.LockedFollowing, kinkster.OwnPerms.AllowLockedFollowing);
        ClientHcEmoteRow(kinkster, dispName, width, KPID.LockedEmoteState, nameof(HardcoreStatus.LockedEmoteState), hc.LockedEmoteState, kinkster.OwnPerms.AllowLockedSitting, kinkster.OwnPerms.AllowLockedEmoting);
        ClientHcStateRow(kinkster, dispName, width, KPID.IndoorConfinement, nameof(HardcoreStatus.IndoorConfinement), hc.IndoorConfinement, kinkster.OwnPerms.AllowIndoorConfinement);
        ClientHcStateRow(kinkster, dispName, width, KPID.Imprisonment, nameof(HardcoreStatus.Imprisonment), hc.Imprisonment, kinkster.OwnPerms.AllowImprisonment);
        ClientHcStateRow(kinkster, dispName, width, KPID.ChatBoxesHidden, nameof(HardcoreStatus.ChatBoxesHidden), hc.ChatBoxesHidden, kinkster.OwnPerms.AllowHidingChatBoxes);
        ClientHcStateRow(kinkster, dispName, width, KPID.ChatInputHidden, nameof(HardcoreStatus.ChatInputHidden), hc.ChatInputHidden, kinkster.OwnPerms.AllowHidingChatInput);
        ClientHcStateRow(kinkster, dispName, width, KPID.ChatInputBlocked, nameof(HardcoreStatus.ChatInputBlocked), hc.ChatInputBlocked, kinkster.OwnPerms.AllowChatInputBlocking);
        ImGui.Separator();

        // Hardcore confirm modal.
        PanelPairEx.HardcoreConfirmationPopup(_hub, kinkster, dispName);

        ImGui.TextUnformatted("Shock Collar Permissions");
        if (ClientData.Globals is not { } p || !p.HasValidShareCode())
            CkGui.ColorTextCentered("Must have a valid Global ShareCode first!", ImGuiColors.DalamudRed);
        else
        {
            UniqueShareCode(kinkster, dispName, width);
            MaxVibrateDuration(cache, kinkster, dispName, width);
        }
    }

    public void DrawKinksterPermissions(KinksterInfoCache cache, Kinkster kinkster, string dispName, float width)
    {
        ImGuiUtil.Center($"{dispName}'s Permissions for You");
        ImGui.Separator();

        // have to make child object below the preset selector for a scrollable interface.
        using var _ = CkRaii.Child("KinksterPerms", new Vector2(0, ImGui.GetContentRegionAvail().Y), wFlags: WFlags.NoScrollbar);

        ImGui.TextUnformatted("Global Settings");
        KinksterPermRow(kinkster, dispName, width, KPID.ChatGarblerActive,       kinkster.PairGlobals.ChatGarblerActive,       kinkster.PairPermAccess.ChatGarblerActiveAllowed );
        KinksterPermRow(kinkster, dispName, width, KPID.ChatGarblerLocked,       kinkster.PairGlobals.ChatGarblerLocked,       kinkster.PairPermAccess.ChatGarblerLockedAllowed );
        KinksterPermRow(kinkster, dispName, width, KPID.GaggedNameplate,         kinkster.PairGlobals.GaggedNameplate,         kinkster.PairPermAccess.GaggedNameplateAllowed );
        ImGui.Separator();

        ImGui.TextUnformatted("Padlock Permissions");
        KinksterPermRow(kinkster, dispName, width, KPID.PermanentLocks,          kinkster.PairPerms.PermanentLocks,            kinkster.PairPermAccess.PermanentLocksAllowed );
        KinksterPermRow(kinkster, dispName, width, KPID.OwnerLocks,              kinkster.PairPerms.OwnerLocks,                kinkster.PairPermAccess.OwnerLocksAllowed );
        KinksterPermRow(kinkster, dispName, width, KPID.DevotionalLocks,         kinkster.PairPerms.DevotionalLocks,           kinkster.PairPermAccess.DevotionalLocksAllowed );
        ImGui.Separator();

        ImGui.TextUnformatted("Gag Permissions");
        KinksterPermRow(kinkster, dispName, width, KPID.GagVisuals,              kinkster.PairGlobals.GagVisuals,              kinkster.PairPermAccess.GagVisualsAllowed );
        KinksterPermRow(kinkster, dispName, width, KPID.ApplyGags,               kinkster.PairPerms.ApplyGags,                 kinkster.PairPermAccess.ApplyGagsAllowed );
        KinksterPermRow(kinkster, dispName, width, KPID.LockGags,                kinkster.PairPerms.LockGags,                  kinkster.PairPermAccess.LockGagsAllowed );
        KinksterPermRow(kinkster, dispName, width, KPID.MaxGagTime,              kinkster.PairPerms.MaxGagTime,                kinkster.PairPermAccess.MaxGagTimeAllowed );
        KinksterPermRow(kinkster, dispName, width, KPID.UnlockGags,              kinkster.PairPerms.UnlockGags,                kinkster.PairPermAccess.UnlockGagsAllowed );
        KinksterPermRow(kinkster, dispName, width, KPID.RemoveGags,              kinkster.PairPerms.RemoveGags,                kinkster.PairPermAccess.RemoveGagsAllowed );
        ImGui.Separator();

        ImGui.TextUnformatted("Restriction Permissions");
        KinksterPermRow(kinkster, dispName, width, KPID.RestrictionVisuals,      kinkster.PairGlobals.RestrictionVisuals,      kinkster.PairPermAccess.RestrictionVisualsAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.ApplyRestrictions,       kinkster.PairPerms.ApplyRestrictions,         kinkster.PairPermAccess.ApplyRestrictionsAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.LockRestrictions,        kinkster.PairPerms.LockRestrictions,          kinkster.PairPermAccess.LockRestrictionsAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.MaxRestrictionTime,      kinkster.PairPerms.MaxRestrictionTime,        kinkster.PairPermAccess.MaxRestrictionTimeAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.UnlockRestrictions,      kinkster.PairPerms.UnlockRestrictions,        kinkster.PairPermAccess.UnlockRestrictionsAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.RemoveRestrictions,      kinkster.PairPerms.RemoveRestrictions,        kinkster.PairPermAccess.RemoveRestrictionsAllowed);
        ImGui.Separator();

        ImGui.TextUnformatted("Restraint Set Permissions");
        KinksterPermRow(kinkster, dispName, width, KPID.RestraintSetVisuals,     kinkster.PairGlobals.RestraintSetVisuals,     kinkster.PairPermAccess.RestraintSetVisualsAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.ApplyRestraintSets,      kinkster.PairPerms.ApplyRestraintSets,        kinkster.PairPermAccess.ApplyRestraintSetsAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.ApplyLayers,             kinkster.PairPerms.ApplyLayers,               kinkster.PairPermAccess.ApplyLayersAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.ApplyLayersWhileLocked,  kinkster.PairPerms.ApplyLayersWhileLocked,    kinkster.PairPermAccess.ApplyLayersWhileLockedAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.LockRestraintSets,       kinkster.PairPerms.LockRestraintSets,         kinkster.PairPermAccess.LockRestraintSetsAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.MaxRestraintTime,        kinkster.PairPerms.MaxRestraintTime,          kinkster.PairPermAccess.MaxRestraintTimeAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.UnlockRestraintSets,     kinkster.PairPerms.UnlockRestraintSets,       kinkster.PairPermAccess.UnlockRestraintSetsAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.RemoveLayers,            kinkster.PairPerms.RemoveLayers,              kinkster.PairPermAccess.RemoveLayersAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.RemoveLayersWhileLocked, kinkster.PairPerms.RemoveLayersWhileLocked,   kinkster.PairPermAccess.RemoveLayersWhileLockedAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.RemoveRestraintSets,     kinkster.PairPerms.RemoveRestraintSets,       kinkster.PairPermAccess.RemoveRestraintSetsAllowed);
        ImGui.Separator();

        ImGui.TextUnformatted("Puppeteer Permissions");
        KinksterPermRow(kinkster, dispName, width, KPID.PuppetPermSit,           kinkster.PairPerms.PuppetPerms,              kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.Sit);
        KinksterPermRow(kinkster, dispName, width, KPID.PuppetPermEmote,         kinkster.PairPerms.PuppetPerms,              kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.Emotes);
        KinksterPermRow(kinkster, dispName, width, KPID.PuppetPermAlias,         kinkster.PairPerms.PuppetPerms,              kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.Alias);
        KinksterPermRow(kinkster, dispName, width, KPID.PuppetPermAll,           kinkster.PairPerms.PuppetPerms,              kinkster.PairPermAccess.PuppetPermsAllowed, PuppetPerms.All);
        ImGui.Separator();

        ImGui.TextUnformatted("Moodles Permissions");
        KinksterPermRow(kinkster, dispName, width, KPID.ApplyPositive,           kinkster.PairPerms.MoodleAccess,              kinkster.PairPermAccess.MoodleAccessAllowed, MoodleAccess.Positive);
        KinksterPermRow(kinkster, dispName, width, KPID.ApplyNegative,           kinkster.PairPerms.MoodleAccess,              kinkster.PairPermAccess.MoodleAccessAllowed, MoodleAccess.Negative);
        KinksterPermRow(kinkster, dispName, width, KPID.ApplySpecial,            kinkster.PairPerms.MoodleAccess,              kinkster.PairPermAccess.MoodleAccessAllowed, MoodleAccess.Special);
        KinksterPermRow(kinkster, dispName, width, KPID.ApplyOwnMoodles,         kinkster.PairPerms.MoodleAccess,              kinkster.PairPermAccess.MoodleAccessAllowed, MoodleAccess.AllowOwn);
        KinksterPermRow(kinkster, dispName, width, KPID.ApplyPairsMoodles,       kinkster.PairPerms.MoodleAccess,              kinkster.PairPermAccess.MoodleAccessAllowed, MoodleAccess.AllowOther);
        KinksterPermRow(kinkster, dispName, width, KPID.MaxMoodleTime,           kinkster.PairPerms.MaxMoodleTime,             kinkster.PairPermAccess.MaxMoodleTimeAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.PermanentMoodles,        kinkster.PairPerms.MoodleAccess,              kinkster.PairPermAccess.MoodleAccessAllowed, MoodleAccess.Permanent);
        KinksterPermRow(kinkster, dispName, width, KPID.RemoveAppliedMoodles,    kinkster.PairPerms.MoodleAccess,              kinkster.PairPermAccess.MoodleAccessAllowed, MoodleAccess.RemoveApplied);
        KinksterPermRow(kinkster, dispName, width, KPID.RemoveAnyMoodles,        kinkster.PairPerms.MoodleAccess,              kinkster.PairPermAccess.MoodleAccessAllowed, MoodleAccess.RemoveAny);
        ImGui.Separator();

        ImGui.TextUnformatted("Misc. Permissions");
        KinksterPermRow(kinkster, dispName, width, KPID.HypnosisMaxTime,         kinkster.PairPerms.MaxHypnosisTime,          kinkster.PairPermAccess.HypnosisMaxTimeAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.HypnosisEffect,          kinkster.PairPerms.HypnoEffectSending,       kinkster.PairPermAccess.HypnosisSendingAllowed);
        ImGui.Separator();

        ImGui.TextUnformatted("Toybox Permissions");
        KinksterPermRow(kinkster, dispName, width, KPID.PatternStarting,         kinkster.PairPerms.ExecutePatterns,          kinkster.PairPermAccess.ExecutePatternsAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.PatternStopping,         kinkster.PairPerms.StopPatterns,             kinkster.PairPermAccess.StopPatternsAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.AlarmToggling,           kinkster.PairPerms.ToggleAlarms,             kinkster.PairPermAccess.ToggleAlarmsAllowed);
        KinksterPermRow(kinkster, dispName, width, KPID.TriggerToggling,         kinkster.PairPerms.ToggleTriggers,           kinkster.PairPermAccess.ToggleTriggersAllowed);
        ImGui.Separator();

        ImGui.TextUnformatted("Hardcore State");
        CkGui.ColorTextInline("(View-Only)", ImGuiColors.DalamudRed);

        KinksterHcRow(kinkster, dispName, width, KPID.LockedFollowing, kinkster.PairHardcore.LockedFollowing, kinkster.PairPerms.AllowLockedFollowing);
        KinksterHcRow(kinkster, dispName, width, KPID.LockedEmoteState, kinkster.PairHardcore.LockedEmoteState, kinkster.PairPerms.AllowLockedEmoting || kinkster.PairPerms.AllowLockedSitting);
        KinksterHcRow(kinkster, dispName, width, KPID.IndoorConfinement, kinkster.PairHardcore.IndoorConfinement, kinkster.PairPerms.AllowIndoorConfinement);
        KinksterHcRow(kinkster, dispName, width, KPID.Imprisonment, kinkster.PairHardcore.Imprisonment, kinkster.PairPerms.AllowImprisonment);
        KinksterHcRow(kinkster, dispName, width, KPID.ChatBoxesHidden, kinkster.PairHardcore.ChatBoxesHidden, kinkster.PairPerms.AllowHidingChatBoxes);
        KinksterHcRow(kinkster, dispName, width, KPID.ChatInputHidden, kinkster.PairHardcore.ChatInputHidden, kinkster.PairPerms.AllowHidingChatInput);
        KinksterHcRow(kinkster, dispName, width, KPID.ChatInputBlocked, kinkster.PairHardcore.ChatInputBlocked, kinkster.PairPerms.AllowChatInputBlocking);
    }


    public void DrawInteractions(KinksterInfoCache cache, Kinkster kinkster, string dispName, float width)
    {
        /* ----------- GLOBAL SETTINGS ----------- */
        ImGuiUtil.Center($"Interactions with {dispName}");
        ImGui.Separator();

        // have to make child object below the preset selector for a scrollable interface.
        using var _ = CkRaii.Child("KinksterInteractions", new Vector2(0, ImGui.GetContentRegionAvail().Y), wFlags: WFlags.NoScrollbar);

        if (kinkster.IsOnline)
        {
            DrawGagActions(cache, kinkster, width, dispName);
            ImGui.Separator();

            DrawRestrictionActions(cache, kinkster, width, dispName);
            ImGui.Separator();

            DrawRestraintActions(cache, kinkster, width, dispName);
            ImGui.Separator();

            DrawMoodlesActions(cache, kinkster, width, dispName);
            ImGui.Separator();

            DrawToyboxActions(cache, kinkster, width, dispName);
            ImGui.Separator();

            DrawMiscActions(cache, kinkster, width, dispName);
            ImGui.Separator();
        }
        if (kinkster.PairPerms.InHardcore)
        {
            DrawHardcoreActions(cache, kinkster, dispName, width);
            ImGui.Separator();
        }
        // if (kinster.PairPerms.HasValidShareCode() || kinster.PairGlobals.HasValidShareCode())
        // {
        //     DrawShockActions(width, kinster, dispName);
        //     ImGui.Separator();
        // }

        ImGui.TextUnformatted("Individual Pair Functions");
        if (CkGui.IconTextButton(FAI.Trash, "Unpair Permanently", width, true, !KeyMonitor.CtrlPressed() || !KeyMonitor.ShiftPressed()))
            _hub.UserRemoveKinkster(new(kinkster.UserData)).ConfigureAwait(false);
        CkGui.AttachToolTip($"--COL--CTRL + SHIFT + L-Click--COL-- to remove {dispName}", color: ImGuiColors.DalamudRed);
    }

}
