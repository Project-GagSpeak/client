using GagSpeak.Services.Mediator;
using System.Numerics;

namespace GagSpeak.UI.Profile;

/// <summary>
/// The UI Design for the KinkPlates.
/// </summary>
public partial class KinkPlatePreviewUI : WindowMediatorSubscriberBase
{
    private Vector2 RectMin { get; set; } = Vector2.Zero;
    private Vector2 RectMax { get; set; } = Vector2.Zero;

    // plate properties.
    private Vector2 PlateSize => new Vector2(750, 450);

    // Left Side
    private Vector2 CloseButtonPos => RectMin + Vector2.One * 16f;
    private Vector2 CloseButtonSize => Vector2.One * 24f;

    private Vector2 ProfilePictureBorderPos => RectMin + Vector2.One * 12f;
    private Vector2 ProfilePictureBorderSize => Vector2.One * 226f;

    private Vector2 ProfilePicturePos => RectMin + Vector2.One * 18f;
    private Vector2 ProfilePictureSize => Vector2.One * 214f;

    private Vector2 SupporterIconBorderPos => RectMin + new Vector2(182, 16);
    private Vector2 SupporterIconBorderSize => Vector2.One * 52f;

    private Vector2 SupporterIconPos => RectMin + new Vector2(184, 18);
    private Vector2 SupporterIconSize => Vector2.One * 48f;

    private Vector2 IconOverviewListPos => RectMin + new Vector2(12, 290);
    private Vector2 IconOverviewListSize => new Vector2(224, 34);

    private Vector2 DescriptionBorderPos => RectMin + new Vector2(12, 332);
    private Vector2 DescriptionBorderSize => new Vector2(550, 105);

    // Center Middle
    private Vector2 TitleLineStartPos => RectMin + new Vector2(247, 76);
    private Vector2 TitleLineSize => new Vector2(316, 6);

    private Vector2 GagSlotOneBorderPos => RectMin + new Vector2(260, 94);
    private Vector2 GagSlotTwoBorderPos => RectMin + new Vector2(363, 94);
    private Vector2 GagSlotThreeBorderPos => RectMin + new Vector2(467, 94);
    private Vector2 GagSlotBorderSize => Vector2.One * 85f;

    private Vector2 GagSlotOnePos => RectMin + new Vector2(264, 98);
    private Vector2 GagSlotTwoPos => RectMin + new Vector2(367, 98);
    private Vector2 GagSlotThreePos => RectMin + new Vector2(471, 98);
    private Vector2 GagSlotSize => Vector2.One * 77f;

    private Vector2 GagLockOneBorderPos => RectMin + new Vector2(273, 166);
    private Vector2 GagLockTwoBorderPos => RectMin + new Vector2(376, 166);
    private Vector2 GagLockThreeBorderPos => RectMin + new Vector2(480, 166);
    private Vector2 GagLockBorderSize => Vector2.One * 59f;

    private Vector2 GagLockOnePos => RectMin + new Vector2(277, 170);
    private Vector2 GagLockTwoPos => RectMin + new Vector2(380, 170);
    private Vector2 GagLockThreePos => RectMin + new Vector2(484, 170);
    private Vector2 GagLockSize => Vector2.One * 55f;

    private Vector2 StatsPos => RectMin + new Vector2(385, 305);

    // Right Side.
    private Vector2 LockedSlotsPanelBorderPos => RectMin + new Vector2(573, 14);
    private Vector2 LockedSlotsPanelBorderSize => new Vector2(163, 423);

    private Vector2 LockedSlotsPanelPos => RectMin + new Vector2(576, 17);
    private Vector2 LockedSlotsPanelSize => new Vector2(155, 415);

    private Vector2 LockAffectersRowPos => RectMin + new Vector2(599, 26);
    private Vector2 LockAffecterIconSize => Vector2.One * 28;
    private Vector2 LockAffecterSpacing => Vector2.One * 13;


    private Vector2 LockedSlotsGroupPos => RectMin + new Vector2(590, 60);
    private Vector2 LockedSlotSize => new Vector2(58, 58);
    private Vector2 LockedSlotSpacing = new Vector2(12, 12);

    private Vector2 HardcoreTraitsRowPos => RectMin + new Vector2(586, 405);
    private Vector2 HardcoreTraitIconSize => Vector2.One * 20;
    private Vector2 HardcoreTraitSpacing => Vector2.One * 9;
}
