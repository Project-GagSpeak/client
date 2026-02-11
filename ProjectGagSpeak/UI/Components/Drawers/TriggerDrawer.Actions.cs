using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using CkCommons.Raii;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;

// Scoped, sealed class to draw the editor and display components of aliasItems.
public sealed partial class TriggerDrawer
{
    public void DrawActionInfo(Trigger trigger, bool isEditorItem, uint searchBg)
    {
        // What we draw, should be based on what triggerkind it is.
        switch (trigger.InvokableAction)
        {
            case TextAction textAct:
                DrawTextAction(textAct, isEditorItem);
                break;

            case GagAction gagAct:
                DrawGagAction(gagAct, isEditorItem, searchBg);
                break;

            case RestrictionAction restrictionAction:
                DrawRestrictionAction(restrictionAction, isEditorItem, searchBg);
                break;

            case RestraintAction restraintAct:
                DrawRestraintAction(restraintAct, isEditorItem, searchBg);
                break;

            case MoodleAction moodleAct:
                DrawMoodleAction(moodleAct, isEditorItem, searchBg);
                break;

            case PiShockAction shockAct:
                DrawPiShockAction(shockAct, isEditorItem, searchBg);
                break;

            case SexToyAction sexToyAct:
                DrawToyAction(sexToyAct, isEditorItem, searchBg);
                break;
        }
    }

    private string GetStateName(NewState state)
        => state switch { NewState.Enabled => "Apply", NewState.Locked => "Lock", NewState.Disabled => "Remove", _ => "" };

    private IEnumerable<NewState> StatesNoUnlock => [NewState.Enabled, NewState.Locked, NewState.Disabled];

    private void DrawTextAction(TextAction textAct, bool isEditorItem)
    {
        // Player to Track.
        CkGui.FramedIconText(FAI.Comment);
        CkGui.TextFrameAlignedInline("Respond with the command:");

        // Draw the text action command.
        using (ImRaii.Group())
        {
            CkGui.FramedIconText(FAI.Font);
            CkGui.TextFrameAlignedInline("/");
            ImUtf8.SameLineInner();
            var textCommand = textAct.OutputCommand;
            CkGuiUtils.FramedEditDisplay("##TextAction", ImGui.GetContentRegionAvail().X, true, textCommand, _ =>
            {
                if (ImGui.InputTextMultiline("##TextAction", ref textCommand, 256, ImGui.GetContentRegionAvail()))
                    textAct.OutputCommand = textCommand;

                // Draw a hint if no text is present.
                if (textAct.OutputCommand.IsNullOrWhitespace())
                    ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, 0xFFBBBBBB, "yell Hello World!");

            }, height: ImGui.GetTextLineHeightWithSpacing() * 3);
        }
    }

    private void DrawGagAction(GagAction gagAct, bool isEditorItem, uint searchBg)
    {
        // stuff for gag action.
        using (CkRaii.InfoRow(FAI.ListOl, "On", "What Layer this trigger will perform the action on."))
        {
            var width = ImGui.CalcTextSize("Any Layerm").X;
            CkGuiUtils.FramedEditDisplay("##LayerSelect", width, isEditorItem, CkGuiUtils.LayerIdxName(gagAct.LayerIdx), _ =>
            {
                if(CkGuiUtils.LayerIdxCombo("##LayerIdx", width, gagAct.LayerIdx, out var newIdx, 3, true, CFlags.NoArrowButton))
                    gagAct.LayerIdx = newIdx;
            });

            CkGui.TextFrameAlignedInline("try to");

            ImUtf8.SameLineInner();
            var stateW = ImGui.CalcTextSize("removem").X;
            CkGuiUtils.FramedEditDisplay("##GagState", stateW, isEditorItem, GetStateName(gagAct.NewState), _ =>
            {
                if (CkGuiUtils.EnumCombo("##GagState", stateW, gagAct.NewState, out var newVal, StatesNoUnlock,
                    i => GetStateName(i), flags: CFlags.NoArrowButton))
                {
                    gagAct.NewState = newVal;
                }
            });

            CkGui.TextFrameAlignedInline(gagAct.NewState switch
            {
                NewState.Enabled => "a Gag.",
                NewState.Locked => "the Gag.",
                _ => "the Gag.",
            });
        }

        // Return early if disabled.
        if (gagAct.NewState is NewState.Disabled)
            return;

        // If Apply or Lock:
        var icon = gagAct.NewState is NewState.Locked ? FAI.Lock : FAI.MehBlank;
        var iconTT = gagAct.NewState is NewState.Locked ? "The Padlock that is locked onto the Gag." : "The Gag to be applied.";
        using (CkRaii.InfoRow(icon, "Attempt with a", iconTT))
        {
            var width = ImGui.GetContentRegionAvail().X;
            if (gagAct.NewState is NewState.Enabled)
            {
                CkGuiUtils.FramedEditDisplay("##Gag", width, isEditorItem, gagAct.GagType.GagName(), _ =>
                {
                    if (CkGuiUtils.EnumCombo("##Gag", width, gagAct.GagType, out var newGag,
                        i => i.GagName(), "Randomly chosen Gag", skip: 1))
                    {
                        gagAct.GagType = newGag;
                    }
                });
            }
            else
            {
                CkGuiUtils.FramedEditDisplay("##Padlock", width, isEditorItem, gagAct.Padlock.ToName(), _ =>
                {
                    var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
                    if (CkGuiUtils.EnumCombo("##Padlock", width, gagAct.Padlock, out var newPadlock,
                        options, i => i.ToName(), flags: CFlags.NoArrowButton))
                    {
                        gagAct.Padlock = newPadlock;
                    }
                });
            }
        }

        // For Timer padlocks.
        if(gagAct.Padlock.IsTimerLock() && gagAct.NewState is NewState.Locked)
        {
            using (CkRaii.InfoRow(FAI.HourglassStart, "Between Min. Time", "the minimum randomly chosen time for the lock."))
            {
                CkGuiUtils.FramedEditDisplay("##TimeMin", ImGui.GetContentRegionAvail().X, isEditorItem, "LowerBound", _ =>
                {
                    ImGui.Text("Timer Ranges included later!");
                });
            }

            using (CkRaii.InfoRow(FAI.HourglassStart, "And a Max. Time", "the maximum randomly chosen time for the lock."))
            {
                CkGuiUtils.FramedEditDisplay("##TimeMax", ImGui.GetContentRegionAvail().X, isEditorItem, "UpperBound", _ =>
                {
                    ImGui.Text("Timer Ranges included later!");
                });
            }
        }
    }

    private void DrawRestrictionAction(RestrictionAction rsAct, bool isEditorItem, uint searchBg)
    {
        // stuff for gag action.
        using (CkRaii.InfoRow(FAI.ListOl, "On", "What Layer this trigger will perform the action on."))
        {
            var width = ImGui.CalcTextSize("Any Layerm").X;
            CkGuiUtils.FramedEditDisplay("##LayerSelect", width, isEditorItem, CkGuiUtils.LayerIdxName(rsAct.LayerIdx), _ =>
            {
                if (CkGuiUtils.LayerIdxCombo("##LayerIdx", width, rsAct.LayerIdx, out var newIdx, 5, true, CFlags.NoArrowButton))
                    rsAct.LayerIdx = newIdx;
            });

            CkGui.TextFrameAlignedInline("try to");

            ImUtf8.SameLineInner();
            var stateW = ImGui.CalcTextSize("removem").X;
            CkGuiUtils.FramedEditDisplay("##RestrictionState", stateW, isEditorItem, GetStateName(rsAct.NewState), _ =>
            {
                if (CkGuiUtils.EnumCombo("##RestrictionState", stateW, rsAct.NewState, out var newVal, StatesNoUnlock,
                    i => GetStateName(i), flags: CFlags.NoArrowButton))
                {
                    rsAct.NewState = newVal;
                }
            });

            CkGui.TextFrameAlignedInline("a Restriction.");
        }

        // Return early if disabled.
        if (rsAct.NewState is NewState.Disabled)
            return;

        // If Apply or Lock:
        var icon = rsAct.NewState is NewState.Locked ? FAI.Lock : FAI.Handcuffs;
        var iconTT = rsAct.NewState is NewState.Locked ? "The Padlock that is locked onto the Restriction Item." : "The Restriction Item to be applied.";
        using (CkRaii.InfoRow(icon, "Attempt with a", iconTT))
        {
            var width = ImGui.GetContentRegionAvail().X;
            if (rsAct.NewState is NewState.Enabled)
            {
                var label = _restrictionCombo.Current?.Label ?? "<No Restriction Selected>";
                CkGuiUtils.FramedEditDisplay("##Restriction", ImGui.GetContentRegionAvail().X, isEditorItem, label, _ =>
                {
                    var change = _restrictionCombo.Draw("##Restriction", rsAct.RestrictionId, ImGui.GetContentRegionAvail().X, searchBg);
                    if (change && rsAct.RestrictionId != _restrictionCombo.Current?.Identifier)
                        rsAct.RestrictionId = _restrictionCombo.Current?.Identifier ?? Guid.Empty;
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        rsAct.RestrictionId = Guid.Empty;
                });
            }
            else
            {
                CkGuiUtils.FramedEditDisplay("##Padlock", width, isEditorItem, rsAct.Padlock.ToName(), _ =>
                {
                    var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
                    if (CkGuiUtils.EnumCombo("##Padlock", width, rsAct.Padlock, out var newPadlock,
                        options, i => i.ToName(), flags: CFlags.NoArrowButton))
                    {
                        rsAct.Padlock = newPadlock;
                    }
                });
            }
        }

        // For Timer padlocks.
        if (rsAct.Padlock.IsTimerLock() && rsAct.NewState is NewState.Locked)
        {
            using (CkRaii.InfoRow(FAI.HourglassStart, "Between Min. Time", "the minimum randomly chosen time for the lock."))
            {
                CkGuiUtils.FramedEditDisplay("##TimeMin", ImGui.GetContentRegionAvail().X, isEditorItem, "LowerBound", _ =>
                {
                    ImGui.Text("Timer Ranges included later!");
                });
            }

            using (CkRaii.InfoRow(FAI.HourglassStart, "And a Max. Time", "the maximum randomly chosen time for the lock."))
            {
                CkGuiUtils.FramedEditDisplay("##TimeMax", ImGui.GetContentRegionAvail().X, isEditorItem, "UpperBound", _ =>
                {
                    ImGui.Text("Timer Ranges included later!");
                });
            }
        }
    }

    private void DrawRestraintAction(RestraintAction rsAct, bool isEditorItem, uint searchBg)
    {
        // StateType and GagType/Padlock
        using (CkRaii.InfoRow(FAI.Lock, "Try to", "The state change that must occur for detection.", string.Empty))
        {
            var width = ImGui.CalcTextSize("removem").X;
            CkGuiUtils.FramedEditDisplay("##RS_State", width, isEditorItem, GetStateName(rsAct.NewState), _ =>
            {
                if (CkGuiUtils.EnumCombo("##RS_State", width, rsAct.NewState, out var newVal, StatesNoUnlock,
                    i => GetStateName(i), flags: CFlags.NoArrowButton))
                {
                    rsAct.NewState = newVal;
                }
            });

            CkGui.TextFrameAlignedInline(rsAct.NewState switch
            {
                NewState.Enabled => "a Restraint Set.",
                NewState.Locked => "the applied Restraint Set.",
                _ => "the Restraint Set.",
            });
        }

        var icon = rsAct.NewState is NewState.Locked ? FAI.Lock : FAI.Handcuffs;
        var iconTT = rsAct.NewState is NewState.Locked ? "The Padlock that's locked on the Restraint Set." : "The Restraint Set to be applied.";
        using (CkRaii.InfoRow(icon, "Attempt with a", iconTT))
        {
            var width = ImGui.GetContentRegionAvail().X;
            if (rsAct.NewState is NewState.Enabled)
            {
                var label = _restraintCombo.Current?.Label ?? "<No Restraint Selected>";
                CkGuiUtils.FramedEditDisplay("##RestraintSet", ImGui.GetContentRegionAvail().X, isEditorItem, label, _ =>
                {
                    var change = _restraintCombo.Draw("##RestraintSet", rsAct.RestrictionId, ImGui.GetContentRegionAvail().X, searchBg);
                    if (change && rsAct.RestrictionId != _restraintCombo.Current?.Identifier)
                        rsAct.RestrictionId = _restraintCombo.Current?.Identifier ?? Guid.Empty;
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        rsAct.RestrictionId = Guid.Empty;
                });
            }
            else
            {
                CkGuiUtils.FramedEditDisplay("##Padlock", width, isEditorItem, rsAct.Padlock.ToName(), _ =>
                {
                    var options = PadlockEx.ClientLocks.Except(PadlockEx.PasswordPadlocks);
                    if (CkGuiUtils.EnumCombo("##Padlock", width, rsAct.Padlock, out var newPadlock,
                        options, i => i.ToName(), flags: CFlags.NoArrowButton))
                    {
                        rsAct.Padlock = newPadlock;
                    }
                });
            }
        }

        // For Timer padlocks.
        if (rsAct.Padlock.IsTimerLock() && rsAct.NewState is NewState.Locked)
        {
            using (CkRaii.InfoRow(FAI.HourglassStart, "Between Min. Time", "the minimum randomly chosen time for the lock."))
            {
                CkGuiUtils.FramedEditDisplay("##TimeMin", ImGui.GetContentRegionAvail().X, isEditorItem, "LowerBound", _ =>
                {
                    ImGui.Text("Timer Ranges included later!");
                });
            }

            using (CkRaii.InfoRow(FAI.HourglassStart, "And a Max. Time", "the maximum randomly chosen time for the lock."))
            {
                CkGuiUtils.FramedEditDisplay("##TimeMax", ImGui.GetContentRegionAvail().X, isEditorItem, "UpperBound", _ =>
                {
                    ImGui.Text("Timer Ranges included later!");
                });
            }
        }
    }

    private void DrawMoodleAction(MoodleAction mAct, bool isEditorItem, uint searchBg)
    {
        var curType = mAct.MoodleItem.GetType() == typeof(MoodlePreset) ? MoodleType.Preset : MoodleType.Status;
        using (CkRaii.InfoRow(FAI.WandMagicSparkles, "Applies a Moodle", "The type of Moodle being applied (Status/Preset)"))
        {
            var width = ImGui.CalcTextSize("Statusm").X;
            CkGuiUtils.FramedEditDisplay("##MoodleType", width, isEditorItem, curType.ToString(), _ =>
            {
                if (CkGuiUtils.EnumCombo("##M_Type", width, curType, out var newVal) && newVal != curType)
                    mAct.MoodleItem = newVal is MoodleType.Preset ? new MoodlePreset() : new Moodle();
            });

            CkGui.TextFrameAlignedInline("to the Player.");
        }
        
        using (CkRaii.InfoRow(FAI.TheaterMasks, "Attempt applying", "The Moodle Status/Preset to apply."))
        {
            var width = ImGui.GetContentRegionAvail().X;
            if (mAct.MoodleItem is MoodlePreset preset)
            {
                var label = _presetCombo.Current.GUID== Guid.Empty ? "<No Preset Selected>" : _presetCombo.Current.Title.StripColorTags();
                CkGuiUtils.FramedEditDisplay("##MoodleItem", width, isEditorItem, label, _ =>
                {
                    // update the preset if different.
                    if (_presetCombo.Draw("##M_Preset", preset.Id, width, searchBg))
                        preset.UpdatePreset(_presetCombo.Current.GUID, _presetCombo.Current.Statuses);

                    // Reset the preset if the item is clicked.
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        mAct.MoodleItem = new MoodlePreset();
                });
            }
            else
            {
                var label = _statusCombo.Current.GUID== Guid.Empty ? "<No Status Selected>" : _statusCombo.Current.Title.StripColorTags();
                CkGuiUtils.FramedEditDisplay("##MoodleItem", width, isEditorItem, label, _ =>
                {
                    // update the status if different.
                    if (_statusCombo.Draw("##M_Status", mAct.MoodleItem.Id, width, searchBg))
                        mAct.MoodleItem.UpdateId(_statusCombo.Current.GUID);
                    // Reset the status if the item is clicked.
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        mAct.MoodleItem = new Moodle();
                });
            }
        }

        // Draw Framed Icons
        _moodleDrawer.ShowStatusIconsFramed("MoodleAct", [mAct.MoodleItem], ImGui.GetContentRegionAvail().X,
            CkStyle.ChildRounding(), MoodleDrawer.IconSize, 2);
    }

    private void DrawPiShockAction(PiShockAction shockAct, bool isEditorItem, uint searchBg)
    {
        var instructionTT = "What type of instruction to send to the Shock Collar. ";
        var durTooltip = $"How long to {shockAct.ShockInstruction.OpCode.ToString()} for.";

        using (CkRaii.InfoRow(FAI.Bolt, "Sends a", instructionTT, instructionTT, "instruction to the Collar"))
        {
            var width = ImGui.CalcTextSize("Vibratem").X;
            CkGuiUtils.FramedEditDisplay("##OpCode", width, isEditorItem, shockAct.ShockInstruction.OpCode.ToString(), _ =>
            {
                if (CkGuiUtils.EnumCombo("##OpCode", width, shockAct.ShockInstruction.OpCode, out var mode))
                    shockAct.ShockInstruction.OpCode = mode;
            });
        }

        using (CkRaii.InfoRow(FAI.Stopwatch, "Lasting for", durTooltip, string.Empty))
        {
            var width = ImGui.GetContentRegionAvail().X;
            var durationFloat = shockAct.ShockInstruction.GetDurationFloat();
            CkGuiUtils.FramedEditDisplay("##Duration", width, isEditorItem, $"{durationFloat}s", _ =>
            {
                ImGui.SetNextItemWidth(width);
                if (ImGui.SliderFloat("##Duration", ref durationFloat, 0.016f, 15f, "%.3fs"))
                    shockAct.ShockInstruction.SetDuration(durationFloat);
            });
        }

        if (shockAct.ShockInstruction.OpCode is not ShockMode.Beep)
        {
            using (CkRaii.InfoRow(FAI.Signal, "With an intensity of", "The intensity to apply for the duration."))
            {
                var width = ImGui.GetContentRegionAvail().X;
                var intensityRef = shockAct.ShockInstruction.Intensity;
                CkGuiUtils.FramedEditDisplay("##Intensity", width, isEditorItem, intensityRef.ToString(), _ =>
                {
                    ImGui.SetNextItemWidth(width);
                    if (ImGui.SliderInt("##ShockIntensity", ref intensityRef, 0, 100))
                        shockAct.ShockInstruction.Intensity = intensityRef;
                });
            }
        }
    }

    private void DrawToyAction(SexToyAction toyAct, bool isEditorItem, uint searchBg)
    {
        // using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);

        // Split things up into 2 columns.
        var columnWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;
        var height = CkGuiUtils.GetTimeDisplayHeight(UiFontService.UidFont) + ImGui.GetFrameHeightWithSpacing();
        // Enter the first column.
        using (ImRaii.Group())
        {
            var timeLimit = TimeSpan.FromMilliseconds(59999);
            var refStart = toyAct.StartAfter;
            using (var c = CkRaii.ChildPaddedW("ToyActStart", columnWidth, height, CkCol.CurvedHeaderFade.Uint(),
                CkStyle.ChildRoundingLarge(), ImDrawFlags.RoundCornersAll))
            {
                ImGuiUtil.Center("Start After");
                if (isEditorItem)
                {
                    CkGuiUtils.TimeSpanEditor("ToyActStart", TimeSpan.FromMilliseconds(59999), ref refStart, "ss\\:fff", UiFontService.UidFont, c.InnerRegion.X);
                    toyAct.StartAfter = refStart;
                }
                else
                {
                    CkGuiUtils.TimeSpanPreview("ToyActStart", TimeSpan.FromMilliseconds(59999), refStart, "ss\\:fff", UiFontService.UidFont, c.InnerRegion.X);
                }
            }
        }

        // Shift to next column and display the pattern playback child.
        ImGui.SameLine();
        using (ImRaii.Group())
        {
            using (var c = CkRaii.ChildPaddedW("ToyActEnd", columnWidth, height, CkCol.CurvedHeaderFade.Uint(),
                CkStyle.ChildRoundingLarge(), ImDrawFlags.RoundCornersAll))
            {
                ImGuiUtil.Center("End After");
                var refEnd = toyAct.EndAfter;
                var format = toyAct.EndAfter.Minutes > 0 ? "mm\\:ss" : "ss\\:fff";
                if (isEditorItem)
                {
                    CkGuiUtils.TimeSpanEditor("ToyActEnd", TimeSpan.FromMinutes(30), ref refEnd, format, UiFontService.UidFont, c.InnerRegion.X);
                    toyAct.EndAfter = refEnd;
                }
                else
                {
                    CkGuiUtils.TimeSpanPreview("ToyActEnd", TimeSpan.FromMinutes(30), refEnd, format, UiFontService.UidFont, c.InnerRegion.X);
                }
            }
        }

        // What kind of action for it to be.
        using (CkRaii.InfoRow(FAI.Filter, "Execute a", "The action to perform on the toy.", "The action to perform on the toy.", "action to the toys."))
        {
            var width = ImGui.CalcTextSize("Vibrationm").X;
            CkGuiUtils.FramedEditDisplay("##ToyActionType", width, isEditorItem, toyAct.ActionKind.ToString(), _ =>
            {
                if (CkGuiUtils.EnumCombo("##ToyActionType", width, toyAct.ActionKind, out var newVal))
                    toyAct.ActionKind = newVal;
            });
        }

        if (toyAct.ActionKind is ToyActionType.Pattern)
        {
            using (CkRaii.InfoRow(FAI.Play, "Play the Pattern", "The pattern to play on the toy."))
            {
                var width = ImGui.GetContentRegionAvail().X;
                var label = _patternCombo.Current?.Label ?? "<No Pattern Selected>";
                CkGuiUtils.FramedEditDisplay("##ToyPattern", width, isEditorItem, label, _ =>
                {
                    // update the pattern if different.
                    if (_patternCombo.Draw("##ToyPattern", toyAct.PatternId, width, searchBg))
                        toyAct.PatternId = _patternCombo.Current?.Identifier ?? Guid.Empty;
                    // Reset the pattern if the item is clicked.
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        toyAct.PatternId = Guid.Empty;
                });
            }
        }
        else
        {
            using (CkRaii.InfoRow(FAI.Signal, "With an intensity of", "The intensity to apply for the duration."))
            {
                var width = ImGui.GetContentRegionAvail().X;
                var intensityRef = toyAct.Intensity;
                CkGuiUtils.FramedEditDisplay("##VibeIntensity", width, isEditorItem, intensityRef.ToString(), _ =>
                {
                    ImGui.SetNextItemWidth(width);
                    if (ImGui.SliderInt("##VibeIntensity", ref intensityRef, 0, 100))
                        toyAct.Intensity = intensityRef;
                });
            }
        }


        // stuff for toys.
    }
}
