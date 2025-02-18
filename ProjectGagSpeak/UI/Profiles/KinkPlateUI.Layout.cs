using Dalamud.Interface.Utility;
using GagSpeak.Services.Mediator;
using System.Numerics;

namespace GagSpeak.UI.Profile;

/// <summary>
/// The UI Design for the KinkPlates.
/// </summary>
public partial class KinkPlateUI : WindowMediatorSubscriberBase
{
    private Vector2 RectMin { get; set; } = Vector2.Zero;
    private Vector2 RectMax { get; set; } = Vector2.Zero;

    // plate properties.
    private Vector2 PlateSize => ImGuiHelpers.ScaledVector2(750, 450);

    // Left Side
    private Vector2 CloseButtonPos => RectMin + ImGuiHelpers.ScaledVector2(16f);
    private Vector2 CloseButtonSize => ImGuiHelpers.ScaledVector2(24f);

    private Vector2 ProfilePictureBorderPos => RectMin + ImGuiHelpers.ScaledVector2(12f);
    private Vector2 ProfilePictureBorderSize => ImGuiHelpers.ScaledVector2(226f);

    private Vector2 ProfilePicturePos => RectMin + ImGuiHelpers.ScaledVector2(18f);
    private Vector2 ProfilePictureSize => ImGuiHelpers.ScaledVector2(214f);

    private Vector2 SupporterIconBorderPos => RectMin + ImGuiHelpers.ScaledVector2(182, 16);
    private Vector2 SupporterIconBorderSize => ImGuiHelpers.ScaledVector2(52f);

    private Vector2 SupporterIconPos => RectMin + ImGuiHelpers.ScaledVector2(184, 18);
    private Vector2 SupporterIconSize => ImGuiHelpers.ScaledVector2(48f);

    private Vector2 IconOverviewListPos => RectMin + ImGuiHelpers.ScaledVector2(12, 290);
    private Vector2 IconOverviewListSize => ImGuiHelpers.ScaledVector2(224, 34);

    private Vector2 DescriptionBorderPos => RectMin + ImGuiHelpers.ScaledVector2(12, 332);
    private Vector2 DescriptionBorderSize => ImGuiHelpers.ScaledVector2(550, 105);

    // Center Middle
    private Vector2 TitleLineStartPos => RectMin + ImGuiHelpers.ScaledVector2(247, 76);
    private Vector2 TitleLineSize => ImGuiHelpers.ScaledVector2(316, 6);

    private Vector2 GagSlotOneBorderPos => RectMin + ImGuiHelpers.ScaledVector2(260, 94);
    private Vector2 GagSlotTwoBorderPos => RectMin + ImGuiHelpers.ScaledVector2(363, 94);
    private Vector2 GagSlotThreeBorderPos => RectMin + ImGuiHelpers.ScaledVector2(467, 94);
    private Vector2 GagSlotBorderSize => ImGuiHelpers.ScaledVector2(85f);

    private Vector2 GagSlotOnePos => RectMin + ImGuiHelpers.ScaledVector2(264, 98);
    private Vector2 GagSlotTwoPos => RectMin + ImGuiHelpers.ScaledVector2(367, 98);
    private Vector2 GagSlotThreePos => RectMin + ImGuiHelpers.ScaledVector2(471, 98);
    private Vector2 GagSlotSize => ImGuiHelpers.ScaledVector2(77f);

    private Vector2 GagLockOneBorderPos => RectMin + ImGuiHelpers.ScaledVector2(273, 166);
    private Vector2 GagLockTwoBorderPos => RectMin + ImGuiHelpers.ScaledVector2(376, 166);
    private Vector2 GagLockThreeBorderPos => RectMin + ImGuiHelpers.ScaledVector2(480, 166);
    private Vector2 GagLockBorderSize => ImGuiHelpers.ScaledVector2(59f);

    private Vector2 GagLockOnePos => RectMin + ImGuiHelpers.ScaledVector2(277, 170);
    private Vector2 GagLockTwoPos => RectMin + ImGuiHelpers.ScaledVector2(380, 170);
    private Vector2 GagLockThreePos => RectMin + ImGuiHelpers.ScaledVector2(484, 170);
    private Vector2 GagLockSize => ImGuiHelpers.ScaledVector2(55f);

    private Vector2 StatsPos => RectMin + ImGuiHelpers.ScaledVector2(385, 305);

    // Right Side.
    private Vector2 LockedSlotsPanelBorderPos => RectMin + ImGuiHelpers.ScaledVector2(573, 14);
    private Vector2 LockedSlotsPanelBorderSize => ImGuiHelpers.ScaledVector2(163, 423);

    private Vector2 LockedSlotsPanelPos => RectMin + ImGuiHelpers.ScaledVector2(576, 17);
    private Vector2 LockedSlotsPanelSize => ImGuiHelpers.ScaledVector2(155, 415);

    private Vector2 LockAffectersRowPos => RectMin + ImGuiHelpers.ScaledVector2(599, 26);
    private Vector2 LockAffecterIconSize => ImGuiHelpers.ScaledVector2(28);
    private Vector2 LockAffecterSpacing => ImGuiHelpers.ScaledVector2(13);


    private Vector2 LockedSlotsGroupPos => RectMin + ImGuiHelpers.ScaledVector2(590, 60);
    private Vector2 LockedSlotSize => ImGuiHelpers.ScaledVector2(58, 58);
    private Vector2 LockedSlotSpacing = ImGuiHelpers.ScaledVector2(12, 12);

    private Vector2 HardcoreTraitsRowPos => RectMin + ImGuiHelpers.ScaledVector2(586, 405);
    private Vector2 HardcoreTraitIconSize => ImGuiHelpers.ScaledVector2(20);
    private Vector2 HardcoreTraitSpacing => ImGuiHelpers.ScaledVector2(9);
}
