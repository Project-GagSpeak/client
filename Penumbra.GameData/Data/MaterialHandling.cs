using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Data;

/// <summary> Helper functions for hair material handling. </summary>
public static class MaterialHandling
{
    /// <summary> Obtain the gender race code for a specific gender, race and hair combination. </summary>
    /// <param name="actualGr"> The actors gender and race. </param>
    /// <param name="hairId"> The actors hair ID. </param>
    /// <returns> The gender and race combination to use for the material. </returns>
    public static GenderRace GetGameGenderRace(GenderRace actualGr, PrimaryId hairId)
    {
        // Hrothgar do not share hairstyles.
        if (actualGr is GenderRace.HrothgarFemale or GenderRace.HrothgarMale)
            return actualGr;

        // Some hairstyles are miqo'te specific but otherwise shared.
        if (hairId.Id is >= 101 and <= 115)
        {
            if (actualGr is GenderRace.MiqoteFemale or GenderRace.MiqoteMale)
                return actualGr;

            return actualGr.Split().Item1 == Gender.Female ? GenderRace.MidlanderFemale : GenderRace.MidlanderMale;
        }

        // All hairstyles above 116 are shared except for Hrothgar
        if (hairId.Id is >= 116 and <= 200)
            return actualGr.Split().Item1 == Gender.Female ? GenderRace.MidlanderFemale : GenderRace.MidlanderMale;

        return actualGr;
    }

    /// <summary> Whether the hair is already shared globally. </summary>
    public static bool IsSpecialCase(GenderRace gr, PrimaryId hairId)
        => gr is GenderRace.MidlanderMale or GenderRace.MidlanderFemale && hairId.Id is >= 101 and <= 200;
}
