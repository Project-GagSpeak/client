using System.Reflection;

namespace GagSpeak.CkCommons.Helpers;

// Ref from: https://github.com/NightmareXIV/ECommons/blob/a2d8748a68d8038a989b42f18c6100e015edb886/ECommons/ChatMethods/UIColor.cs

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum XlDataUiColor : short
{
    WhiteNormal = 0,
    White = 1,
    Grey1 = 2,
    Grey2 = 3,
    Grey3 = 4,
    Grey4 = 5,
    Grey5 = 6,
    Black = 7,
    LightYellow = 8,

    Red = 17,

    DarkRed = 19,

    Green = 45,

    DarkGreen = 47,

    WarmSeaBlue = 52,

    Orange = 500,

    LightBlue = 502,

    Yellow = 514,

    Gold = 540,

    DarkBlue = 543,

    LightGreen = 551,

    Pink = 561,
}
