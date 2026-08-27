using CkCommons;
using Dalamud.Bindings.ImGui;
using Penumbra.GameData.Structs;

namespace GagSpeak.CustomCombos;

/// <summary> A combo for selecting an entry from the Lifestream address book. </summary>
public sealed class AddressBookCombo : CkFilterComboCache<AddressBookEntryTuple>
{
    public AddressBookCombo(ILogger log, Func<IReadOnlyList<AddressBookEntryTuple>> generator)
        : base(() => generator().OrderBy(DisplayString, StringComparer.OrdinalIgnoreCase).ToList(), log)
    { }

    private static string Label(AddressBookEntryTuple obj)
        => obj.AliasEnabled && !string.IsNullOrEmpty(obj.Alias) ? obj.Alias
        : !string.IsNullOrEmpty(obj.Name) ? obj.Name : string.Empty;

    public static string DisplayString(AddressBookEntryTuple obj)
    {
        var label = Label(obj);
        return string.IsNullOrEmpty(label) ? AddressString(obj) : $"{label}  |  {AddressString(obj)}";
    }

    protected override string ToString(AddressBookEntryTuple obj)
        => DisplayString(obj);

    /// <summary> Builds the readable address: house = world, city, ward, plot | apartment = world, city, ward, apartment. </summary>
    public static string AddressString(AddressBookEntryTuple obj)
    {
        var world = ItemSvc.WorldData.TryGetValue(new WorldId((ushort)obj.World), out var w) ? w : "Unknown World";
        var city = GameDataHelp.ResidentialNames.TryGetValue((ResidentialAetheryteKind)obj.City, out var c) ? c : "Unknown City";
        var unit = (PropertyType)obj.PropertyType is PropertyType.Apartment
            ? $"Apt {obj.Apartment}" : $"Plot {obj.Plot}";
        return $"{world}, {city}, Ward {obj.Ward}, {unit}";
    }

    /// <summary> Simple draw invoke. </summary>
    public bool Draw(string preview, float width, CFlags flags = CFlags.None)
    {
        InnerWidth = width * 1.3f;
        return Draw("##addressBookCombo", preview, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags);
    }
}
