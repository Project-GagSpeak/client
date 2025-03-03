using Dalamud.Interface.Colors;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Tutorial;
using GagSpeak.Services;
using GagSpeak.UI.Wardrobe;
using GagspeakAPI.Data.Character;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace GagSpeak.UI.Components;
public class MoodleDrawer
{


    public MoodleDrawer(CkGui uiShared)
    {

    }

    public void thing()
    {
        /*try
        {
            using var table = ImRaii.Table("MoodlesSelections", 2, ImGuiTableFlags.BordersInnerV);
            if (!table) return;

            ImGui.TableSetupColumn("MoodleSelection", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("FinalizedPreviewList", ImGuiTableColumnFlags.WidthFixed, 200f);

            ImGui.TableNextRow(); ImGui.TableNextColumn();

            using (var child = ImRaii.Child("##RestraintMoodleStatusSelection", new(ImGui.GetContentRegionAvail().X - 1f, ImGui.GetContentRegionAvail().Y / 2), false))
            {
                if (!child) return;
                _relatedMoodles.DrawMoodlesStatusesListForItem(refRestraintSet, LastCreatedCharacterData, cellPaddingY, false);
            }
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.MoodlesStatuses, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);


            ImGui.Separator();
            using (var child2 = ImRaii.Child("##RestraintMoodlePresetSelection", -Vector2.One, false))
            {
                if (!child2) return;
                _relatedMoodles.DrawMoodlesStatusesListForItem(refRestraintSet, LastCreatedCharacterData, cellPaddingY, true);
            }
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.MoodlesPresets, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

            ImGui.TableNextColumn();
            // Filter the MoodlesStatuses list to get only the moodles that are in AssociatedMoodles
            var associatedMoodles = LastCreatedCharacterData.MoodlesStatuses
                .Where(moodle => refRestraintSet.AssociatedMoodles.Contains(moodle.GUID))
                .ToList();
            // draw out all the active associated moodles in the restraint set with thier icon beside them.
            using (ImRaii.Group())
            {
                CkGui.ColorText("Moodles Applied with Set:", ImGuiColors.ParsedPink);
                ImGui.Separator();

                foreach (var moodle in associatedMoodles)
                {
                    using (ImRaii.Group())
                    {

                        var currentPos = ImGui.GetCursorPos();
                        if (moodle.IconID != 0 && currentPos != Vector2.Zero)
                        {
                            var statusIcon = CkGui.GetGameStatusIcon((uint)((uint)moodle.IconID + moodle.Stacks - 1));

                            if (statusIcon is { } wrap)
                            {
                                ImGui.SetCursorPos(currentPos);
                                ImGui.Image(statusIcon.ImGuiHandle, MoodlesService.StatusSize);
                            }
                        }
                        ImGui.SameLine();
                        var shiftAmmount = (MoodlesService.StatusSize.Y - ImGui.GetTextLineHeight()) / 2;
                        ImGui.SetCursorPosY(currentPos.Y + shiftAmmount);
                        ImGui.Text(moodle.Title);
                    }
                }
            }
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.AppendedMoodles, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error Drawing Moodles Options for Restraint Set.");
        }*/
    }

}
