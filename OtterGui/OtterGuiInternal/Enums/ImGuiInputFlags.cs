namespace OtterGui.OtterGuiInternal.Enums;

[Flags]
public enum ImGuiInputFlags : ulong
{
    None                             = 0x00000000,
    Repeat                           = 0x00000001,
    RepeatRateDefault                = 0x00000002,
    RepeatRateNavMove                = 0x00000004,
    RepeatRateNavTweak               = 0x00000008,
    RepeatUntilRelease               = 0x00000010,
    RepeatUntilKeyModsChange         = 0x00000020,
    RepeatUntilKeyModsChangeFromNone = 0x00000040,
    RepeatUntilOtherKeyPress         = 0x00000080,
    CondHovered                      = 0x00000100,
    CondActive                       = 0x00000200,
    LockThisFrame                    = 0x00000400,
    LockUntilRelease                 = 0x00000800,
    RouteFocused                     = 0x00001000,
    RouteGlobalLow                   = 0x00002000,
    RouteGlobal                      = 0x00004000,
    RouteGlobalHigh                  = 0x00008000,
    RouteAlways                      = 0x00010000,
    RouteUnlessBgFocused             = 0x00020000,

    CondDefault_               = CondHovered | CondActive,
    CondMask_                  = CondHovered | CondActive,
    RouteMask_                 = RouteFocused | RouteGlobal | RouteGlobalLow | RouteGlobalHigh,
    RouteExtraMask_            = RouteAlways | RouteUnlessBgFocused,
    RepeatRateMask_            = RepeatRateDefault | RepeatRateNavMove | RepeatRateNavTweak,
    RepeatUntilMask_           = RepeatUntilRelease | RepeatUntilKeyModsChange | RepeatUntilKeyModsChangeFromNone | RepeatUntilOtherKeyPress,
    RepeatMask_                = Repeat | RepeatRateMask_ | RepeatUntilMask_,
    SupportedByIsKeyPressed    = RepeatMask_,
    SupportedByIsMouseClicked  = Repeat,
    SupportedByShortcut        = RepeatMask_ | RouteMask_ | RouteExtraMask_,
    SupportedBySetKeyOwner     = LockThisFrame | LockUntilRelease,
    SupportedBySetItemKeyOwner = SupportedBySetKeyOwner | CondMask_,
}
