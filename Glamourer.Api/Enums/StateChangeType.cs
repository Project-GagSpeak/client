namespace Glamourer.Api.Enums;

/// <summary> What type of information changed in a state. </summary>
public enum StateChangeType
{
    /// <summary> A characters saved state had the model id changed. This means everything may have changed. </summary>
    Model = 0,

    /// <summary> A characters saved state had multiple customization values changed. </summary>
    EntireCustomize = 1,

    /// <summary> A characters saved state had a customization value changed. </summary>
    Customize = 2,

    /// <summary> A characters saved state had an equipment piece changed. </summary>
    Equip = 3,

    /// <summary> A characters saved state had its weapons changed. </summary>
    Weapon = 4,

    /// <summary> A characters saved state had a stain changed. </summary>
    Stains = 5,

    /// <summary> A characters saved state had a crest visibility changed. </summary>
    Crest = 6,

    /// <summary> A characters saved state had its customize parameter changed. </summary>
    Parameter = 7,

    /// <summary> A characters saved state had a material color table value changed. </summary>
    MaterialValue = 8,

    /// <summary> A characters saved state had a design applied. This means everything may have changed. </summary>
    Design = 9,

    /// <summary> A characters saved state had its state reset to its game values. </summary>
    Reset = 10,

    /// <summary> A characters saved state had a meta toggle changed. </summary>
    Other = 11,

    /// <summary> A characters state was reapplied. Data is null. </summary>
    Reapply = 12,

    /// <summary> A characters saved state had a bonus item changed. </summary>
    BonusItem = 13,
}
