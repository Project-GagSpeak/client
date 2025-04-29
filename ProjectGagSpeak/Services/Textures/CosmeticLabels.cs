namespace GagSpeak.Services.Textures;

public static class CosmeticLabels
{
    public static readonly Dictionary<CoreEmoteTexture, string> ChatEmoteTextures = new()
    {
        { CoreEmoteTexture._2bgasm, "RequiredImages\\Emotes\\2bgasm.png" },
        { CoreEmoteTexture.BallGag, "RequiredImages\\Emotes\\ballgag.png" },
        { CoreEmoteTexture.Cappie, "RequiredImages\\Emotes\\cappie.png" },
        { CoreEmoteTexture.CatDrool, "RequiredImages\\Emotes\\catdrool.png" },
        { CoreEmoteTexture.CatFlappyLeft, "RequiredImages\\Emotes\\catflappyleft.png" },
        { CoreEmoteTexture.CatFlappyRight, "RequiredImages\\Emotes\\catflappyright.png" },
        { CoreEmoteTexture.CatPat, "RequiredImages\\Emotes\\catpat.png" },
        { CoreEmoteTexture.CatScream, "RequiredImages\\Emotes\\catscream.png" },
        { CoreEmoteTexture.CatSit, "RequiredImages\\Emotes\\catsit.png" },
        { CoreEmoteTexture.CatSnuggle, "RequiredImages\\Emotes\\catsnuggle.png" },
        { CoreEmoteTexture.Collar, "RequiredImages\\Emotes\\collar.png" },
        { CoreEmoteTexture.Crop, "RequiredImages\\Emotes\\crop.png" },
        { CoreEmoteTexture.Cuffs, "RequiredImages\\Emotes\\cuffs.png" },
        { CoreEmoteTexture.Cute, "RequiredImages\\Emotes\\cute.png" },
        { CoreEmoteTexture.Gag, "RequiredImages\\Emotes\\gag.png" },
        { CoreEmoteTexture.Heart, "RequiredImages\\Emotes\\heart.png" },
        { CoreEmoteTexture.Horny, "RequiredImages\\Emotes\\horny.png" },
        { CoreEmoteTexture.Hyperwah, "RequiredImages\\Emotes\\hyperwah.png" },
        { CoreEmoteTexture.Rope, "RequiredImages\\Emotes\\rope.png" },
        { CoreEmoteTexture.Straitjacket, "RequiredImages\\Emotes\\straitjacket.png" },
        { CoreEmoteTexture.Tape, "RequiredImages\\Emotes\\tape.png" },
        { CoreEmoteTexture.Vibe, "RequiredImages\\Emotes\\vibe.png" },
    };

    public static readonly Dictionary<CoreTexture, string> NecessaryImages = new()
    {
        { CoreTexture.Achievement, "RequiredImages\\achievement.png" },
        { CoreTexture.AchievementLineSplit, "RequiredImages\\achievementlinesplit.png" },
        { CoreTexture.ArrowSpin, "RequiredImages\\arrowspin.png" },
        { CoreTexture.Blindfolded, "RequiredImages\\blindfolded.png" },
        { CoreTexture.ChatBlocked, "RequiredImages\\chatblocked.png" },
        { CoreTexture.CircleDot, "RequiredImages\\circledot.png" },
        { CoreTexture.Clock, "RequiredImages\\clock.png" },
        { CoreTexture.Collar, "RequiredImages\\collar.png" },
        { CoreTexture.CursedLoot, "RequiredImages\\cursedloot.png" },
        { CoreTexture.ForcedEmote, "RequiredImages\\forcedemote.png" },
        { CoreTexture.ForcedStay, "RequiredImages\\forcedstay.png" },
        { CoreTexture.Gagged, "RequiredImages\\gagged.png" },
        { CoreTexture.Icon256, "RequiredImages\\icon256.png" },
        { CoreTexture.Icon256Bg, "RequiredImages\\icon256bg.png" },
        { CoreTexture.Immobilize, "RequiredImages\\immobilize.png" },
        { CoreTexture.Leash, "RequiredImages\\leash.png" },
        { CoreTexture.Play, "RequiredImages\\play.png" },
        { CoreTexture.Power, "RequiredImages\\power.png" },
        { CoreTexture.PuppetMaster, "RequiredImages\\puppetmaster.png" },
        { CoreTexture.PuppetVictimGlobal, "RequiredImages\\puppetvictimglobal.png" },
        { CoreTexture.PuppetVictimUnique, "RequiredImages\\puppetvictimunique.png" },
        { CoreTexture.Restrained, "RequiredImages\\restrained.png" },
        { CoreTexture.RestrainedArmsLegs, "RequiredImages\\restrainedarmslegs.png" },
        { CoreTexture.ShockCollar, "RequiredImages\\shockcollar.png" },
        { CoreTexture.SightLoss, "RequiredImages\\sightloss.png" },
        { CoreTexture.StatusActiveGag, "RequiredImages\\statusActiveGag.png" },
        { CoreTexture.StatusActiveGagSpeaking, "RequiredImages\\statusActiveGagSpeaking.png" },
        { CoreTexture.Stimulated, "RequiredImages\\stimulated.png" },
        { CoreTexture.Stop, "RequiredImages\\stop.png" },
        { CoreTexture.Tier1Icon, "RequiredImages\\Tier1Icon.png" },
        { CoreTexture.Tier2Icon, "RequiredImages\\Tier2Icon.png" },
        { CoreTexture.Tier3Icon, "RequiredImages\\Tier3Icon.png" },
        { CoreTexture.Tier4Icon, "RequiredImages\\Tier4Icon.png" },
        { CoreTexture.TierBoosterIcon, "RequiredImages\\TierBoosterIcon.png" },
        { CoreTexture.Vibrator, "RequiredImages\\vibrator.png" },
        { CoreTexture.Weighty, "RequiredImages\\weighty.png" },
    };

    public static readonly Dictionary<string, string> CosmeticTextures = InitializeCosmeticTextures();

    private static Dictionary<string, string> InitializeCosmeticTextures()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "DummyTest", "RequiredImages\\icon256bg.png" } // Dummy File

        };

        AddEntriesForComponent(dictionary, ProfileComponent.Plate, hasBackground: true, hasBorder: true, hasOverlay: false);
        AddEntriesForComponent(dictionary, ProfileComponent.PlateLight, hasBackground: true, hasBorder: true, hasOverlay: false);
        AddEntriesForComponent(dictionary, ProfileComponent.ProfilePicture, hasBackground: false, hasBorder: true, hasOverlay: true);
        AddEntriesForComponent(dictionary, ProfileComponent.Description, hasBackground: true, hasBorder: true, hasOverlay: true);
        AddEntriesForComponent(dictionary, ProfileComponent.DescriptionLight, hasBackground: true, hasBorder: true, hasOverlay: true);
        AddEntriesForComponent(dictionary, ProfileComponent.GagSlot, hasBackground: true, hasBorder: true, hasOverlay: true);
        AddEntriesForComponent(dictionary, ProfileComponent.Padlock, hasBackground: true, hasBorder: true, hasOverlay: true);
        AddEntriesForComponent(dictionary, ProfileComponent.BlockedSlots, hasBackground: true, hasBorder: true, hasOverlay: true);
        AddEntriesForComponent(dictionary, ProfileComponent.BlockedSlot, hasBackground: false, hasBorder: true, hasOverlay: true);

        return dictionary;
    }

    private static void AddEntriesForComponent(Dictionary<string, string> dictionary, ProfileComponent component, bool hasBackground, bool hasBorder, bool hasOverlay)
    {
        if (hasBackground)
        {
            foreach (var styleBG in Enum.GetValues<ProfileStyleBG>())
            {
                var key = component.ToString() + "_Background_" + styleBG.ToString();
                var value = $"CosmeticImages\\{component}\\Background_{styleBG}.png";
                dictionary[key] = value;
            }
        }

        if (hasBorder)
        {
            foreach (var styleBorder in Enum.GetValues<ProfileStyleBorder>())
            {
                var key = component.ToString() + "_Border_" + styleBorder.ToString();
                var value = $"CosmeticImages\\{component}\\Border_{styleBorder}.png";
                dictionary[key] = value;
            }
        }

        if (hasOverlay)
        {
            foreach (var styleOverlay in Enum.GetValues<ProfileStyleOverlay>())
            {
                var key = component.ToString() + "_Overlay_" + styleOverlay.ToString();
                var value = $"CosmeticImages\\{component}\\Overlay_{styleOverlay}.png";
                dictionary[key] = value;
            }
        }
    }
}
