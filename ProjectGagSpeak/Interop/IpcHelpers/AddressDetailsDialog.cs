using CkCommons;
using GagspeakAPI.Data.Permissions;

namespace GagSpeak.Interop.Helpers;

// These entries we assign when sending off to others, then they are kept until the effect is turned off.
public class AddressBookEntry
{
    public string Name = string.Empty;
    public ushort World = ushort.MaxValue;
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
            World = (ushort)tuple.World,
            City = (ResidentialAetheryteKind)tuple.City,
            Ward = tuple.Ward,
            PropertyType = (PropertyType)tuple.PropertyType,
            Plot = tuple.Plot,
            Apartment = tuple.Apartment,
            ApartmentSubdivision = tuple.ApartmentSubdivision
        };
    }

    public static AddressBookEntry FromHardcoreStatus(IReadOnlyHardcoreState hcState)
        => new AddressBookEntry
        {
            Name = "HardcoreStatus-Assigned-Address",
            World = (ushort)hcState.ConfinedWorld,
            City = (ResidentialAetheryteKind)hcState.ConfinedCity,
            Ward = hcState.ConfinedWard,
            PropertyType = hcState.ConfinedInApartment ? PropertyType.Apartment : PropertyType.House,
            Plot = hcState.ConfinedInApartment ? 0 : hcState.ConfinedPlaceId,
            Apartment = hcState.ConfinedInApartment ? hcState.ConfinedPlaceId : 0,
            ApartmentSubdivision = hcState.ConfinedInSubdivision
        };
}
