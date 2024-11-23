namespace Penumbra.Api.Enums;

/// <summary>
/// Describes known types of changed items that could provide special care.
/// </summary>
public enum ChangedItemType
{
    None          = 0,
    Item          = 1,
    Action        = 2,
    Customization = 3,
    ItemOffhand   = 4,
    Unknown       = 5,
    Emote         = 6,
    Model         = 7,
    CustomArmor   = 8,
}
