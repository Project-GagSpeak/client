namespace GagSpeak.GameInternals;

/// <summary> 
///     A Full Representation of the actual ChatChannel enum from AgentChatLog. 
/// </summary>
public enum InputChannel
{
    Tell_In = 0,
    Say = 1,
    Party = 2,
    Alliance = 3,
    Yell = 4,
    Shout = 5,
    FreeCompany = 6,
    // PvpTeam = 7, <-- Probably not worth risking allowing this.
    NoviceNetwork = 8,

    CWL1 = 9,
    CWL2 = 10,
    CWL3 = 11,
    CWL4 = 12,
    CWL5 = 13,
    CWL6 = 14,
    CWL7 = 15,
    CWL8 = 16,

    Tell = 17, // Special channel for recieved tells and such

    LS1 = 19,
    LS2 = 20,
    LS3 = 21,
    LS4 = 22,
    LS5 = 23,
    LS6 = 24,
    LS7 = 25,
    LS8 = 26,

    Echo = 56,
}
