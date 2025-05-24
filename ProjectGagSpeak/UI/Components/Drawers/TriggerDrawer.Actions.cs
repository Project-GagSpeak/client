using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Raii;
using GagSpeak.PlayerState.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Components;

// Scoped, sealed class to draw the editor and display components of aliasItems.
public sealed partial class TriggerDrawer
{
    public void DrawActionInfo(Trigger trigger, bool isEditorItem)
    {
        // What we draw, should be based on what triggerkind it is.
        switch (trigger.InvokableAction)
        {
            case TextAction textAct:
                DrawTextAction(textAct);
                break;

            case GagAction gagAct:
                DrawGagAction(gagAct);
                break;

            case RestraintAction restraintAct:
                DrawRestraintAction(restraintAct);
                break;

            case RestrictionAction restrictionAct:
                DrawRestrictionAction(restrictionAct);
                break;

            case MoodleAction moodleAct:
                DrawMoodleAction(moodleAct);
                break;

            case PiShockAction shockAct:
                DrawPiShockAction(shockAct);
                break;

            case SexToyAction sexToyAct:
                DrawToyAction(sexToyAct);
                break;
        }
    }

    private void DrawTextAction(TextAction textAct)
    {
        // stuff for text action.
    }

    private void DrawGagAction(GagAction gagAct)
    {
        // stuff for gag action.
    }

    private void DrawRestraintAction(RestraintAction restraintAct)
    {
        // stuff for restraint action.
    }

    private void DrawRestrictionAction(RestrictionAction restrictionAct)
    {
        // stuff for restriction action.
    }

    private void DrawMoodleAction(MoodleAction moodleAct)
    {
        // stuff for moodle action.
    }

    private void DrawPiShockAction(PiShockAction shockAct)
    {
        // stuff for shock collar action.
    }

    private void DrawToyAction(SexToyAction sexToyAct)
    {
        // stuff for toys.
    }
}
