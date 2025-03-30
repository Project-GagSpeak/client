using GagSpeak.PlayerState.Models;
using GagspeakAPI.Data.Character;

namespace GagSpeak.PlayerState.Components;

public interface ICursedItem
{
    /// <summary> The unique identifier for the cursed item. </summary>
    Guid Identifier { get; }

    /// <summary> The name of the cursed item. </summary>
    string Label { get; }

    /// <summary> If the cursed item is in the active item pool to pull from. </summary>
    bool InPool { get; }

    /// <summary> When the item was applied to the player. (Used to determine order of application for display) </summary>
    /// <remarks> While inactive, value is DateTimeOffset.MinValue </remarks>
    DateTimeOffset AppliedTime { get; }

    /// <summary> The time when the item is taken off from the player. </summary>
    /// <remarks> While inactive, value is DateTimeOffset.MinValue </remarks>
    DateTimeOffset ReleaseTime { get; }

    /// <summary> If this cursed item can override other cursed items in the same slot. </summary>
    /// <remarks> Requires precedence. </remarks>
    bool CanOverride { get; }

    /// <summary> Level of precedence an item has when marking comparison for overriding. </summary>
    /// <remarks> We use this to sort our cursed item list with higher precedence first, so we can efficiently recalculate our list. </remarks>
    Precedence Precedence { get; }

    /// <summary> Stores a reference to the restriction item being used. </summary>
    IRestriction RestrictionRef { get; }

    /// <summary> Method used for obtaining the light data value of the CursedItem! </summary>
    /// <returns> the object representing the LightCursedItem </returns>
    public LightCursedItem ToLightData();

    /// <summary> This is used for serialization purposes. </summary>
    /// <returns> the object representing the LightCursedItem </returns>
    public JObject Serialize();
}
