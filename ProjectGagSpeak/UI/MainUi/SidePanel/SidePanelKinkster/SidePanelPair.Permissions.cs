using CkCommons;
using CkCommons.DrawSystem;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using OtterGui.Text;
using TerraFX.Interop.Windows;

namespace GagSpeak.Gui.MainWindow;

// Permission Functions.
public partial class SidePanelPair
{

    #region Client Permissions
    // Common Bool
    private void ClientPermRow(Kinkster kinkster, string dispName, float width, KPID perm, bool current, bool canEdit, bool disabled = false)
        => ClientRowCommon(kinkster, dispName, width, perm, current, canEdit, () => !current, disabled);

    // Puppeteer Variant
    private void ClientPermRow(Kinkster kinkster, string dispName, float width, KPID perm, PuppetPerms current, PuppetPerms canEdit, PuppetPerms editFlag)
    {
        var isFlagSet = (current & editFlag) == editFlag;
        ClientRowEnum(kinkster, dispName, width, perm, isFlagSet, canEdit.HasAny(editFlag), () => current ^ editFlag, () => canEdit ^ editFlag);
    }

    // Moodle Variant
    private void ClientPermRow(Kinkster kinkster, string dispName, float width, KPID perm, MoodleAccess current, MoodleAccess canEdit, MoodleAccess editFlag)
    {
        var isFlagSet = (current & editFlag) == editFlag;
        ClientRowEnum(kinkster, dispName, width, perm, isFlagSet, canEdit.HasAny(editFlag), () => current ^ editFlag, () => canEdit ^ editFlag);
    }

    // Timestamp Variant
    private void ClientPermRow(Kinkster kinkster, string dispName, float width, KPID perm, TimeSpan current, bool canEdit)
    {
        using var disabled = ImRaii.Disabled(kinkster.OwnPerms.InHardcore);

        var inputTxtWidth = width * .4f;
        var str = _timespanCache.TryGetValue(perm, out var value) ? value : current.ToGsRemainingTime();
        var data = PanelPairEx.OwnRowInfo[perm];
        var editCol = canEdit ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();

        if (CkGui.IconInputText(data.IconYes, data.PermLabel, "0d0h0m0s", ref str, 32, inputTxtWidth, true, kinkster.OwnPerms.InHardcore))
        {
            if (str != current.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(str, out var newTime))
            {
                var ticks = (ulong)newTime.Ticks;
                var res = perm.ToPermValue();
                // Assign the blocking task if allowed.
                if (!res.name.IsNullOrEmpty() && res.type is PermissionType.PairPerm)
                    UiService.SetUITask(async () => await PermHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, res.name, ticks));
            }
            _timespanCache.Remove(perm);
        }
        CkGui.AttachToolTip($"How long of a timer {dispName} can put on your padlocks.", kinkster.OwnPerms.InHardcore);

        ImGui.SameLine(width - ImGui.GetFrameHeight());
        if (EditAccessCheckbox.Draw($"##{perm}", canEdit, out var newVal) && canEdit != newVal)
            UiService.SetUITask(async () => await PermHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), newVal));
        CkGui.AttachToolTip($"{dispName} {(canEdit ? "can" : "can not")} change your {data.PermLabel} setting.", kinkster.OwnPerms.InHardcore);
    }

    // optimize later and stuff.
    private void ClientRowCommon<T>(Kinkster kinkster, string dispName, float width, KPID perm, bool current, bool canEdit, Func<T> newStateFunc, bool disabled)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0);
        using var dis = ImRaii.Disabled(kinkster.OwnPerms.InHardcore || disabled);
        var data = PanelPairEx.OwnRowInfo[perm];
        var buttonW = width - (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X);
        var editCol = canEdit ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();
        var pos = ImGui.GetCursorScreenPos();

        // change to ckgui for disabled?
        if (ImGui.Button("##pair" + perm, new Vector2(buttonW, ImGui.GetFrameHeight())))
        {
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;
            UiService.SetUITask(async () =>
            {
                switch (res.type)
                {
                    case PermissionType.Global: await _hub.ChangeOwnGlobalPerm(res.name, newState); break;
                    case PermissionType.PairPerm: await PermHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, res.name, newState); break;
                    default: break;
                }
            });
        }

        ImGui.SameLine();
        if (EditAccessCheckbox.Draw($"##{perm}", canEdit, out var newVal) && canEdit != newVal)
            UiService.SetUITask(async () => await PermHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), newVal));
        CkGui.AttachToolTip($"{dispName} {(canEdit ? "can" : "can not")} change your {data.PermLabel} setting.", kinkster.OwnPerms.InHardcore);

        // draw inside of the button.
        ImGui.SetCursorScreenPos(pos);
        ClientRowText(data, current);
        CkGui.AttachToolTip(data.IsGlobal
            ? $"Your {data.PermLabel} {data.JoinWord} {(current ? data.AllowedStr : data.BlockedStr)}. (Globally)"
            : $"You {(current ? data.AllowedStr : data.BlockedStr)} {dispName} {(current ? data.PairAllowedTT : data.PairBlockedTT)}", kinkster.OwnPerms.InHardcore);
    }

    // client perm text only. Is same as ClientPermRow, but not a button.
    private void ClientRowTextOnly(Kinkster kinkster, string dispName, float width, KPID perm, bool current, bool canEdit, bool disabled = false)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0);
        using var dis = ImRaii.Disabled(kinkster.OwnPerms.InHardcore || disabled);
        var data = PanelPairEx.OwnRowInfo[perm];
        var buttonW = width - ImGui.GetFrameHeight();
        var editCol = canEdit ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();
        var pos = ImGui.GetCursorScreenPos();

        // draw where the button would have been
        ClientRowText(data, current);
        CkGui.AttachToolTip(data.IsGlobal
            ? $"Your {data.PermLabel} {data.JoinWord} {(current ? data.AllowedStr : data.BlockedStr)}. (Globally)"
            : $"You {(current ? data.AllowedStr : data.BlockedStr)} {dispName} {(current ? data.PairAllowedTT : data.PairBlockedTT)}", kinkster.OwnPerms.InHardcore);

        ImGui.SameLine(buttonW);
        if (EditAccessCheckbox.Draw($"##{perm}", canEdit, out var newVal) && canEdit != newVal)
            UiService.SetUITask(async () => await PermHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), newVal));
        CkGui.AttachToolTip($"{dispName} {(canEdit ? "can" : "can not")} change your {data.PermLabel} setting.", kinkster.OwnPerms.InHardcore);
    }

    private void ClientRowEnum<T>(Kinkster kinkster, string dispName, float width, KPID perm, bool current, bool canEdit, Func<T> newStateFunc, Func<T> newEditStateFunc)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0);
        using var dis = ImRaii.Disabled(kinkster.OwnPerms.InHardcore);
        var data = PanelPairEx.OwnRowInfo[perm];
        var buttonW = width - (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X);
        var editCol = canEdit ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();
        var pos = ImGui.GetCursorScreenPos();

        // change to ckgui for disabled?
        if (ImGui.Button("##pair" + perm, new Vector2(buttonW, ImGui.GetFrameHeight())))
        {
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;
            UiService.SetUITask(async () =>
            {
                switch (res.type)
                {
                    case PermissionType.Global: await _hub.ChangeOwnGlobalPerm(res.name, newState); break;
                    case PermissionType.PairPerm: await PermHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, res.name, newState); break;
                    case PermissionType.PairAccess: await PermHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, res.name, newState); break;
                    default: break;
                }
            });
        }

        ImGui.SameLine();
        if (EditAccessCheckbox.Draw($"##{perm}", canEdit, out var newVal) && canEdit != newVal)
        {
            var newEditVal = newEditStateFunc();
            UiService.SetUITask(async () => await PermHelper.ChangeOwnAccess(_hub, kinkster.UserData, kinkster.OwnPermAccess, perm.ToPermAccessValue(), newEditVal!));
        }
        CkGui.AttachToolTip($"{dispName} {(canEdit ? "can" : "can not")} change your {data.PermLabel} setting.", kinkster.OwnPerms.InHardcore);

        // draw inside of the button.
        ImGui.SetCursorScreenPos(pos);
        ClientRowText(data, current);
        CkGui.AttachToolTip(data.IsGlobal
            ? $"Your {data.PermLabel} {data.JoinWord} {(current ? data.AllowedStr : data.BlockedStr)}. (Globally)"
            : $"You {(current ? data.AllowedStr : data.BlockedStr)} {dispName} {(current ? data.PairAllowedTT : data.PairBlockedTT)}", kinkster.OwnPerms.InHardcore);
    }


    private void ClientRowText(PanelPairEx.OwnPermRowData pdp, bool current)
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

    private bool InHardcoreRow(Kinkster k, string name, float width)
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
                UiService.SetUITask(async () => await PermHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, nameof(PairPerms.InHardcore), false));
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
            UiService.SetUITask(async () => await PermHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, nameof(PairPerms.DevotionalLocks), !devotionalState));
        CkGui.AttachToolTip(devotionalState
            ? $"Any Hardcore action by {name} will be --COL--pairlocked--COL----NL--This means that only {name} can disable it."
            : $"Anyone you are in Hardcore for can undo Hardcore interactions from --COL--{name}--COL--", color: GsCol.VibrantPink.Vec4());
        return false;
    }

    private void ClientHcRow(Kinkster k, string name, float width, KPID perm, bool current)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0);
        var data = PanelPairEx.OwnRowInfo[perm];
        var editCol = current ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();
        var pos = ImGui.GetCursorScreenPos();

        // change to ckgui for disabled?
        if (ImGui.Button($"##pair-{perm}", new Vector2(width, ImGui.GetFrameHeight())))
        {
            var res = perm.ToPermValue();
            UiService.SetUITask(async () => await PermHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, res.name, !current));
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

    private void ClientHcStateRow(Kinkster k, string name, float width, KPID perm, string permName, string current, bool allowanceState)
    {
        using var butt = ImRaii.PushColor(ImGuiCol.Button, 0);
        var data = PanelPairEx.OwnHcRowInfo[perm];
        var isActive = !string.IsNullOrEmpty(current);
        var isPairlocked = current.EndsWith(Constants.DevotedString);
        var stateLocker = isActive ? current.Split('|')[0] : string.Empty;
        var editCol = isActive ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();
        var pos = ImGui.GetCursorScreenPos();

        ImGui.Dummy(new Vector2(width - (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X), ImGui.GetFrameHeight()));

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled(isActive))
            if (EditAccessCheckbox.Draw($"##{permName}", allowanceState, out var newVal) && allowanceState != newVal)
                UiService.SetUITask(async () => await PermHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, perm.ToPermAccessValue(), !allowanceState));
        CkGui.AttachToolTip(isActive ? "You are helpless to change this while active!" : allowanceState
                ? $"Allowing {name} {data.ToggleTrueSuffixTT}." : $"Preventing {name} {data.ToggleFalseSuffixTT}.");

        // go back and draw inside the dummy.
        ImGui.SetCursorScreenPos(pos);

        using var _ = ImRaii.PushStyle(ImGuiStyleVar.Alpha, .5f);
        CkGui.FramedIconText(isActive ? data.IconT : data.IconF);
        if (isActive)
        {
            CkGui.TextFrameAlignedInline(data.EnabledPreText);
            CkGui.ColorTextFrameAlignedInline(isActive ? stateLocker.AsAnonKinkster() : "UNK KINKSTER", editCol);
            ImGui.SameLine(0, 0);
            ImUtf8.TextFrameAligned(".");
        }
        else
        {
            CkGui.TextFrameAlignedInline($"{data.DisabledText}.");
        }

    }

    public void ClientHcEmoteRow(Kinkster k, string name, float width, KPID perm, string permName, string current, bool allowBasic, bool allowAll)
    {
        using var butt = ImRaii.PushColor(ImGuiCol.Button, 0);
        var data = PanelPairEx.OwnHcRowInfo[perm];
        var isActive = !string.IsNullOrEmpty(current);
        var isPairlocked = current.EndsWith(Constants.DevotedString);
        var stateLocker = isActive ? current.Split('|')[0] : string.Empty;
        var editCol = isActive ? ImGuiColors.HealerGreen.ToUint() : ImGuiColors.DalamudRed.ToUint();


        // change to ckgui for disabled?
        var pos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(width - ((ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X) * 2), ImGui.GetFrameHeight()));
        CkGui.AttachToolTip($"{permName}'s current locked emote status.");

        // draw out the checkboxessss
        ImUtf8.SameLineInner();
        using (ImRaii.Disabled(isActive))
            if (EditAccessCheckbox.Draw($"##EmBasic{permName}", allowBasic, out var newVal) && allowBasic != newVal)
                UiService.SetUITask(PermHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, KPID.LockedEmoteState.ToPermAccessValue(), newVal));
        CkGui.AttachToolTip(isActive ? "Helpless to change this while performing a locked emote!" : allowBasic
                ? $"{name} can force you to Groundsit, Sit, or Cyclepose." : $"Preventing {name} from placing you in a locked emote state.");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled(isActive))
            if (EditAccessCheckbox.Draw($"##EmFull{permName}", allowAll, out var newVal2) && allowAll != newVal2)
                UiService.SetUITask(PermHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, KPID.LockedEmoteState.ToPermAccessValue(true), newVal2));
        CkGui.AttachToolTip(isActive ? "Helpless to change this while performing a locked emote!" : allowBasic
                ? $"{name} can force you to perform any looping emote." : $"Preventing looped emotes from being forced by {name}.");

        // go back and draw inside the dummy.
        ImGui.SetCursorScreenPos(pos);
        using var _ = ImRaii.PushStyle(ImGuiStyleVar.Alpha, .5f);
        CkGui.FramedIconText(isActive ? data.IconT : data.IconF);
        if (isActive)
        {
            CkGui.TextFrameAlignedInline(data.EnabledPreText);
            ImGui.SameLine(0, 0);
            CkGui.ColorTextFrameAlignedInline(isActive ? stateLocker.AsAnonKinkster() : "UNK KINKSTER", editCol);
            ImGui.SameLine(0, 0);
            ImUtf8.TextFrameAligned(".");
        }
        else
        {
            CkGui.TextFrameAlignedInline($"{data.DisabledText}.");
        }
    }

    #endregion Client Permissions

    #region Kinkster Permissions
    private void KinksterPermRow(Kinkster kinkster, string dispName, float width, KPID perm, bool current, bool canEdit)
        => KinksterRowCommon(kinkster, dispName, width, perm, current, canEdit, () => !current);

    private void KinksterPermRow(Kinkster kinkster, string dispName, float width, KPID perm, PuppetPerms current, PuppetPerms canEdit, PuppetPerms editFlag)
    {
        var isFlagSet = (current & editFlag) == editFlag;
        KinksterRowCommon(kinkster, dispName, width, perm, isFlagSet, canEdit.HasAny(editFlag), () => current ^ editFlag);
    }

    private void KinksterPermRow(Kinkster kinkster, string dispName, float width, KPID perm, MoodleAccess current, MoodleAccess canEdit, MoodleAccess editFlag)
    {
        var isFlagSet = (current & editFlag) == editFlag;
        KinksterRowCommon(kinkster, dispName, width, perm, isFlagSet, canEdit.HasAny(editFlag), () => current ^ editFlag);
    }

    private void KinksterPermRow(Kinkster kinkster, string dispName, float width, KPID perm, TimeSpan current, bool canEdit)
    {
        var inputTxtWidth = width * .4f;
        var str = _timespanCache.TryGetValue(perm, out var value) ? value : current.ToGsRemainingTime();
        var data = PanelPairEx.OtherRowInfo[perm];

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
                    UiService.SetUITask(async () => await PermHelper.ChangeOtherUnique(_hub, kinkster.UserData, kinkster.PairPerms, res.name, ticks));
                }
            }
            _timespanCache.Remove(perm);
        }
        CkGui.AttachToolTip($"The Maximum Time {dispName} can be locked for.");

        ImGui.SameLine(width - ImGui.GetFrameHeight());
        CkGui.BooleanToColoredIcon(canEdit, false, FAI.Pen, FAI.Pen);
        CkGui.AttachToolTip(canEdit ? $"{dispName} allows you to change this." : $"Only {dispName} can update this permission.");
    }

    private void KinksterRowCommon<T>(Kinkster kinkster, string dispName, float width, KPID perm, bool current, bool canEdit, Func<T> newStateFunc)
    {
        using var butt = ImRaii.PushColor(ImGuiCol.Button, 0);

        var data = PanelPairEx.OtherRowInfo[perm];
        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();

        // change to ckgui for disabled?
        if (ImGui.Button("##pair" + perm, new Vector2(buttonW, ImGui.GetFrameHeight())) && canEdit)
        {
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;

            UiService.SetUITask(async () =>
            {
                switch (res.type)
                {
                    case PermissionType.Global:
                        await PermHelper.ChangeOtherGlobal(_hub, kinkster.UserData, kinkster.PairGlobals, res.name, newState);
                        break;
                    case PermissionType.PairPerm:
                        await PermHelper.ChangeOtherUnique(_hub, kinkster.UserData, kinkster.PairPerms, res.name, newState);
                        break;
                    default:
                        break;
                }
                ;
            });
        }
        ImUtf8.SameLineInner();
        CkGui.BooleanToColoredIcon(canEdit, false, FAI.Pen, FAI.Pen);
        CkGui.AttachToolTip(dispName + (canEdit
            ? " allows you to change this permission at will."
            : " is preventing you from changing this permission. Only they can update it."));

        ImGui.SetCursorScreenPos(pos);
        KinksterRowText(data, dispName, current, canEdit);
        if (canEdit)
            CkGui.AttachToolTip($"Toggle {dispName}'s permission.");
    }

    private void KinksterHcRow(Kinkster k, string name, float width, KPID perm, string current, bool grantedAllowance)
    {
        using var butt = ImRaii.PushColor(ImGuiCol.Button, 0);
        var data = PanelPairEx.OtherHcRowInfo[perm];
        var isActive = !string.IsNullOrEmpty(current);
        var isPairlocked = current.EndsWith(Constants.DevotedString);

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
            CkGui.TextFrameAlignedInline(data.ActionText);
            ImGui.SameLine(0, 0);
            CkGui.ColorTextFrameAlignedInline(isActive ? current.Split('|')[0].AsAnonKinkster() : "ANON.KINKSTER", GsCol.VibrantPink.Uint());
        }
        else
        {
            CkGui.TextFrameAlignedInline($"{name}{data.InactiveText}");
        }
    }

    private void KinksterRowText(PanelPairEx.OtherPermRowData pdp, string dispName, bool current, bool canEdit)
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
    #endregion Kinkster Permissions
}
