using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using GagSpeak.Services;
using ImGuiNET;
using OtterGui.Text;
using static CkCommons.GameDataHelp;

namespace GagSpeak.Interop.Helpers;

// These entries we assign when sending off to others, then they are kept until the effect is turned off.
public class AddressBookEntry
{
    public string Name = string.Empty;
    public int World = 21;
    public ResidentialAetheryteKind City = ResidentialAetheryteKind.Uldah;
    public int Ward = 1;
    public PropertyType PropertyType;
    public int Plot = 1;
    public int Apartment = 1;
    public bool ApartmentSubdivision = false;

    public AddressBookEntryTuple AsTuple()
    {
        return (Name, (int)World, (int)City, Ward, (int)PropertyType, Plot, Apartment, ApartmentSubdivision, false, string.Empty);
    }

    public static AddressBookEntry FromTuple(AddressBookEntryTuple tuple)
    {
        return new AddressBookEntry
        {
            Name = tuple.Name,
            World = tuple.World,
            City = (ResidentialAetheryteKind)tuple.City,
            Ward = tuple.Ward,
            PropertyType = (PropertyType)tuple.PropertyType,
            Plot = tuple.Plot,
            Apartment = tuple.Apartment,
            ApartmentSubdivision = tuple.ApartmentSubdivision
        };
    }
}

// Allows us to decide where to lock a kinkster away at
public static class AddressDetailsDialog
{
    public static AddressBookEntry? Entry = null;
    public static bool Open = false;
    public static void Draw()
    {
        if (Entry is not null)
        {
            if (!ImGui.IsPopupOpen($"###ABEEditModal"))
            {
                Open = true;
                ImGui.OpenPopup($"###ABEEditModal");
            }
            if (ImGui.BeginPopupModal($"Editing {Entry.Name}###ABEEditModal", ref Open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (ImGui.BeginTable($"ABEEditTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Edit1", ImGuiTableColumnFlags.WidthFixed, 150);
                    ImGui.TableSetupColumn("Edit2", ImGuiTableColumnFlags.WidthFixed, 250);

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImUtf8.TextFrameAligned("Name:");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    ImGui.InputTextWithHint($"##name", "Random Name with No Purpose", ref Entry.Name, 150);

                    ImGui.TableNextColumn();
                    ImUtf8.TextFrameAligned("World:");
                    ImGui.TableNextColumn();
                    if(OnFrameworkService.WorldCombo.Draw((ushort)Entry.World, ImGui.GetContentRegionAvail().X))
                        Entry.World = OnFrameworkService.WorldCombo.Current.Key;

                    ImGui.TableNextColumn();
                    ImUtf8.TextFrameAligned("Residential District:");
                    ImGui.TableNextColumn();
                    CkGuiUtils.ResidentialAetheryteCombo($"##resdis", ImGui.GetContentRegionAvail().X, ref Entry.City);

                    ImGui.TableNextColumn();
                    ImUtf8.TextFrameAligned("Ward:");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    ImGui.DragInt($"##ward", ref Entry.Ward, 1, 1, 30, "Ward %d");

                    ImGui.TableNextColumn();
                    ImUtf8.TextFrameAligned("Property Type:");
                    ImGui.TableNextColumn();
                    var isHouse = Entry.PropertyType == PropertyType.House;
                    var isApartment = Entry.PropertyType == PropertyType.Apartment;
                    if (ImGui.RadioButton("House", isHouse))
                        Entry.PropertyType = PropertyType.House;
                    ImGui.SameLine();
                    if (ImGui.RadioButton("Apartment", isApartment))
                        Entry.PropertyType = PropertyType.Apartment;

                    // Draw the rest based on the selection.
                    if (Entry.PropertyType is PropertyType.Apartment)
                    {
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Checkbox("Subdivision", ref Entry.ApartmentSubdivision);

                        ImGui.TableNextColumn();
                        ImUtf8.TextFrameAligned("Room:");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        ImGui.SliderInt($"##room", ref Entry.Apartment, 1, 999);
                    }

                    if (Entry.PropertyType is PropertyType.House)
                    {
                        ImGui.TableNextColumn();
                        ImUtf8.TextFrameAligned("Plot:");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        ImGui.SliderInt($"##plot", ref Entry.Plot, 1, 60);
                    }

                    ImGui.EndTable();
                }
                CkGui.SeparatorSpacedColored(col: CkColor.LushPinkLine.Uint());

                CkGui.SetCursorXtoCenter(CkGui.IconTextButtonSize(FAI.Save, "Save and Close"));
                if (CkGui.IconTextButton(FAI.Save, "Save and Close"))
                {
                    Open = false;
                }
                ImGui.EndPopup();
            }
        }

        // Set the entry back to null if the popup is no longer open.
        if (!Open)
            Entry = null;
    }
}
