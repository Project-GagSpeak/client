using GagspeakAPI.Data.IPC;
using MessagePack;

namespace GagspeakAPI.Data;

/// <summary>
/// The content stored within a users profile that indicates their selected cosmetics and information.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record KinkPlateContent
{
    public bool PublicPlate { get; set; } = false;
    public bool Flagged { get; set; } = false;
    public bool Disabled { get; set; } = false;
    public string Description { get; set; } = string.Empty;
    public int CompletedAchievementsTotal { get; set; } = 0;
    public int ChosenTitleId { get; set; } = 0; // Identifier for the achievement they have chosen a title for. 0 is nothing.

    public ProfileStyleBG PlateBackground { get; set; } = ProfileStyleBG.Default;
    public ProfileStyleBorder PlateBorder { get; set; } = ProfileStyleBorder.Default;

    public ProfileStyleBG PlateLightBackground { get; set; } = ProfileStyleBG.Default;
    public ProfileStyleBorder PlateLightBorder { get; set; } = ProfileStyleBorder.Default;

    public ProfileStyleBorder ProfilePictureBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay ProfilePictureOverlay { get; set; } = ProfileStyleOverlay.Default;

    public ProfileStyleBG DescriptionBackground { get; set; } = ProfileStyleBG.Default;
    public ProfileStyleBorder DescriptionBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay DescriptionOverlay { get; set; } = ProfileStyleOverlay.Default;

    public ProfileStyleBG GagSlotBackground { get; set; } = ProfileStyleBG.Default;
    public ProfileStyleBorder GagSlotBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay GagSlotOverlay { get; set; } = ProfileStyleOverlay.Default;

    public ProfileStyleBG PadlockBackground { get; set; } = ProfileStyleBG.Default;
    public ProfileStyleBorder PadlockBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay PadlockOverlay { get; set; } = ProfileStyleOverlay.Default;

    public ProfileStyleBG BlockedSlotsBackground { get; set; } = ProfileStyleBG.Default;
    public ProfileStyleBorder BlockedSlotsBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay BlockedSlotsOverlay { get; set; } = ProfileStyleOverlay.Default;

    public ProfileStyleBorder BlockedSlotBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay BlockedSlotOverlay { get; set; } = ProfileStyleOverlay.Default;
}
