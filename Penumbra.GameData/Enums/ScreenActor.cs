namespace Penumbra.GameData.Enums;

/// <summary> Special game object indices that represent specific boundaries in the object table or certain actors. </summary>
public enum ScreenActor : ushort
{
    CutsceneStart   = 200,
    GPosePlayer     = 201,
    CutsceneEnd     = 440,
    CharacterScreen = 440,
    ExamineScreen   = 441,
    FittingRoom     = 442,
    DyePreview      = 443,
    Portrait        = 444,
    Card6           = 445,
    Card7           = 446,
    Card8           = 447,
    ScreenEnd       = Card8 + 1,
}
