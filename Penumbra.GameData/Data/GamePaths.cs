using Lumina.Data.Files;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using MtrlFile = Penumbra.GameData.Files.MtrlFile;

namespace Penumbra.GameData.Data;

/// <summary> Functions to create or parse game paths for certain types of files. </summary>
public static partial class GamePaths
{
    /// <summary> We never want to capture anything not named. </summary>
    public const RegexOptions Flags1 = RegexOptions.ExplicitCapture;

    /// <summary> When possible, we want to use non-backtracking regex. Backreferences are not compatible with that though. </summary>
    public const RegexOptions Flags2 = Flags1 | RegexOptions.NonBacktracking;

    [GeneratedRegex(@"c(?'racecode'\d{4})", Flags2)]
    public static partial Regex RaceCodeParser();

    public static GenderRace ParseRaceCode(string path)
    {
        var match = RaceCodeParser().Match(path);
        return match.Success
            ? Names.GenderRaceFromCode(match.Groups["racecode"].Value)
            : GenderRace.Unknown;
    }

    public static partial class Monster
    {
        public static partial class Imc
        {
            [GeneratedRegex(@"chara/monster/m(?'monster'\d{4})/obj/body/b(?'id'\d{4})/b\k'id'\.imc", Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId monsterId, SecondaryId bodyId)
                => $"chara/monster/m{monsterId.Id:D4}/obj/body/b{bodyId.Id:D4}/b{bodyId.Id:D4}.imc";
        }

        public static partial class Mdl
        {
            [GeneratedRegex(@"chara/monster/m(?'monster'\d{4})/obj/body/b(?'id'\d{4})/model/m\k'monster'b\k'id'\.mdl", Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId monsterId, SecondaryId bodyId)
                => $"chara/monster/m{monsterId.Id:D4}/obj/body/b{bodyId.Id:D4}/model/m{monsterId.Id:D4}b{bodyId.Id:D4}.mdl";
        }

        public static partial class Mtrl
        {
            [GeneratedRegex(
                @"chara/monster/m(?'monster'\d{4})/obj/body/b(?'id'\d{4})/material/v(?'variant'\d{4})/mt_m\k'monster'b\k'id'_[a-z]+\.mtrl",
                Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId monsterId, SecondaryId bodyId, Variant variant, string suffix)
                => $"chara/monster/m{monsterId.Id:D4}/obj/body/b{bodyId.Id:D4}/material/v{variant.Id:D4}/mt_m{monsterId.Id:D4}b{bodyId.Id:D4}_{suffix}.mtrl";
        }

        public static partial class Tex
        {
            [GeneratedRegex(
                @"chara/monster/m(?'monster'\d{4})/obj/body/b(?'id'\d{4})/texture/v(?'variant'\d{2})_m\k'monster'b\k'id'(_[a-z])?_[a-z]\.tex",
                Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId monsterId, SecondaryId bodyId, Variant variant, char suffix1, char suffix2 = '\0')
                => $"chara/monster/m{monsterId.Id:D4}/obj/body/b{bodyId.Id:D4}/texture/v{variant.Id:D2}_m{monsterId.Id:D4}b{bodyId.Id:D4}{(suffix2 != '\0' ? $"_{suffix2}" : string.Empty)}_{suffix1}.tex";
        }

        public static partial class Sklb
        {
            public static string Path(PrimaryId monsterId)
                => $"chara/monster/m{monsterId.Id:D4}/skeleton/base/b0001/skl_m{monsterId.Id:D4}b0001.sklb";
        }

        public static partial class Skp
        {
            public static string Path(PrimaryId monsterId)
                => $"chara/monster/m{monsterId.Id:D4}/skeleton/base/b0001/skl_m{monsterId.Id:D4}b0001.skp";
        }

        public static partial class Eid
        {
            public static string Path(PrimaryId monsterId)
                => $"chara/monster/m{monsterId.Id:D4}/skeleton/base/b0001/eid_m{monsterId.Id:D4}b0001.eid";
        }
    }

    public static partial class Weapon
    {
        public static partial class Imc
        {
            [GeneratedRegex(@"chara/weapon/w(?'id'\d{4})/obj/body/b(?'weapon'\d{4})/b\k'weapon'\.imc", Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId weaponId, SecondaryId bodyId)
                => $"chara/weapon/w{weaponId.Id:D4}/obj/body/b{bodyId.Id:D4}/b{bodyId.Id:D4}.imc";
        }

        public static partial class Mdl
        {
            [GeneratedRegex(@"chara/weapon/w(?'id'\d{4})/obj/body/b(?'weapon'\d{4})/model/w\k'id'b\k'weapon'\.mdl", Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId weaponId, SecondaryId bodyId)
                => $"chara/weapon/w{weaponId.Id:D4}/obj/body/b{bodyId.Id:D4}/model/w{weaponId.Id:D4}b{bodyId.Id:D4}.mdl";
        }

        public static partial class Mtrl
        {
            [GeneratedRegex(
                @"chara/weapon/w(?'id'\d{4})/obj/body/b(?'weapon'\d{4})/material/v(?'variant'\d{4})/mt_w\k'id'b\k'weapon'_[a-z]+\.mtrl",
                Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId weaponId, SecondaryId bodyId, Variant variant, string suffix)
                => $"chara/weapon/w{weaponId.Id:D4}/obj/body/b{bodyId.Id:D4}/material/v{variant.Id:D4}/mt_w{weaponId.Id:D4}b{bodyId.Id:D4}_{suffix}.mtrl";
        }

        public static partial class Tex
        {
            [GeneratedRegex(
                @"chara/weapon/w(?'id'\d{4})/obj/body/b(?'weapon'\d{4})/texture/v(?'variant'\d{2})_w\k'id'b\k'weapon'(_[a-z])?_[a-z]\.tex",
                Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId weaponId, SecondaryId bodyId, Variant variant, char suffix1, char suffix2 = '\0')
                => $"chara/weapon/w{weaponId.Id:D4}/obj/body/b{bodyId.Id:D4}/texture/v{variant.Id:D2}_w{weaponId.Id:D4}b{bodyId.Id:D4}{(suffix2 != '\0' ? $"_{suffix2}" : string.Empty)}_{suffix1}.tex";
        }

        public static partial class Sklb
        {
            public static string Path(PrimaryId weaponId)
                => $"chara/weapon/w{weaponId.Id:D4}/skeleton/base/b0001/skl_w{weaponId.Id:D4}b0001.sklb";
        }
    }

    public static partial class DemiHuman
    {
        public static partial class Imc
        {
            [GeneratedRegex(@"chara/demihuman/d(?'id'\d{4})/obj/equipment/e(?'equip'\d{4})/e\k'equip'\.imc", Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId demiId, SecondaryId equipId)
                => $"chara/demihuman/d{demiId.Id:D4}/obj/equipment/e{equipId.Id:D4}/e{equipId.Id:D4}.imc";
        }

        public static partial class Mdl
        {
            [GeneratedRegex(@"chara/demihuman/d(?'id'\d{4})/obj/equipment/e(?'equip'\d{4})/model/d\k'id'e\k'equip'_(?'slot'[a-z]{3})\.mdl",
                Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId demiId, SecondaryId equipId, EquipSlot slot)
                => $"chara/demihuman/d{demiId.Id:D4}/obj/equipment/e{equipId.Id:D4}/model/d{demiId.Id:D4}e{equipId.Id:D4}_{slot.ToSuffix()}.mdl";
        }

        public static partial class Mtrl
        {
            [GeneratedRegex(
                @"chara/demihuman/d(?'id'\d{4})/obj/equipment/e(?'equip'\d{4})/material/v(?'variant'\d{4})/mt_d\k'id'e\k'equip'_(?'slot'[a-z]{3})_[a-z]+\.mtrl",
                Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId demiId, SecondaryId equipId, EquipSlot slot, Variant variant, string suffix)
                => $"chara/demihuman/d{demiId.Id:D4}/obj/equipment/e{equipId.Id:D4}/material/v{variant.Id:D4}/mt_d{demiId.Id:D4}e{equipId.Id:D4}_{slot.ToSuffix()}_{suffix}.mtrl";
        }

        public static partial class Tex
        {
            [GeneratedRegex(
                @"chara/demihuman/d(?'id'\d{4})/obj/equipment/e(?'equip'\d{4})/texture/v(?'variant'\d{2})_d\k'id'e\k'equip'_(?'slot'[a-z]{3})(_[a-z])?_[a-z]\.tex",
                Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId demiId, SecondaryId equipId, EquipSlot slot, Variant variant, char suffix1, char suffix2 = '\0')
                => $"chara/demihuman/d{demiId.Id:D4}/obj/equipment/e{equipId.Id:D4}/texture/v{variant.Id:D2}_d{demiId.Id:D4}e{equipId.Id:D4}_{slot.ToSuffix()}{(suffix2 != '\0' ? $"_{suffix2}" : string.Empty)}_{suffix1}.tex";
        }

        public static partial class Sklb
        {
            public static string Path(PrimaryId demiHumanId)
                => $"chara/demihuman/d{demiHumanId.Id:D4}/skeleton/base/b0001/skl_d{demiHumanId.Id:D4}b0001.sklb";
        }
    }


    public static partial class Human
    {
        public static partial class Decal
        {
            public const string LegacyDecalPath = "chara/common/texture/decal_equip/_stigma.tex";

            public static string FaceDecalPath(byte decalId)
                => $"chara/common/texture/decal_face/_decal_{decalId}.tex";
        }
    }

    public static partial class Equipment
    {
        public static partial class Imc
        {
            [GeneratedRegex(@"chara/equipment/e(?'id'\d{4})/e\k'id'\.imc", Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId equipId)
                => $"chara/equipment/e{equipId.Id:D4}/e{equipId.Id:D4}.imc";
        }

        public static partial class Mdl
        {
            [GeneratedRegex(@"chara/equipment/e(?'id'\d{4})/model/c(?'race'\d{4})e\k'id'_(?'slot'[a-z]{3})\.mdl", Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId equipId, GenderRace raceCode, EquipSlot slot)
                => $"chara/equipment/e{equipId.Id:D4}/model/c{raceCode.ToRaceCode()}e{equipId.Id:D4}_{slot.ToSuffix()}.mdl";
        }

        public static partial class Mtrl
        {
            [GeneratedRegex(
                @"chara/equipment/e(?'id'\d{4})/material/v(?'variant'\d{4})/mt_c(?'race'\d{4})e\k'id'_(?'slot'[a-z]{3})_[a-z]+\.mtrl", Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId equipId, GenderRace raceCode, EquipSlot slot, Variant variant, string suffix)
                => $"{FolderPath(equipId, variant)}/mt_c{raceCode.ToRaceCode()}e{equipId.Id:D4}_{slot.ToSuffix()}_{suffix}.mtrl";

            public static string FolderPath(PrimaryId equipId, Variant variant)
                => $"chara/equipment/e{equipId.Id:D4}/material/v{variant.Id:D4}";
        }

        public static partial class Tex
        {
            [GeneratedRegex(
                @"chara/equipment/e(?'id'\d{4})/texture/v(?'variant'\d{2})_c(?'race'\d{4})e\k'id'_(?'slot'[a-z]{3})(_[a-z])?_[a-z]\.tex",
                Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId equipId, GenderRace raceCode, EquipSlot slot, Variant variant, char suffix1,
                char suffix2 = '\0')
                => $"chara/equipment/e{equipId.Id:D4}/texture/v{variant.Id:D2}_c{raceCode.ToRaceCode()}e{equipId.Id:D4}_{slot.ToSuffix()}{(suffix2 != '\0' ? $"_{suffix2}" : string.Empty)}_{suffix1}.tex";
        }

        public static partial class Avfx
        {
            [GeneratedRegex(@"chara/equipment/e(?'id'\d{4})/vfx/eff/ve(?'variant'\d{4})\.avfx", Flags2)]
            public static partial Regex Regex();

            public static string Path(PrimaryId equipId, byte effectId)
                => $"chara/equipment/e{equipId.Id:D4}/vfx/eff/ve{effectId:D4}.avfx";
        }

        public static partial class Decal
        {
            [GeneratedRegex(@"chara/common/texture/decal_equip/-decal_(?'decalId'\d{3})\.tex", Flags2)]
            public static partial Regex Regex();

            public static string Path(byte decalId)
                => $"chara/common/texture/decal_equip/-decal_{decalId:D3}.tex";
        }
    }

    public static partial class Accessory
    {
        public static partial class Imc
        {
            [GeneratedRegex(@"chara/accessory/a(?'id'\d{4})/a\k'id'\.imc", Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId accessoryId)
                => $"chara/accessory/a{accessoryId.Id:D4}/a{accessoryId.Id:D4}.imc";
        }

        public static partial class Mdl
        {
            [GeneratedRegex(@"chara/accessory/a(?'id'\d{4})/model/c(?'race'\d{4})a\k'id'_(?'slot'[a-z]{3})\.mdl", Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId accessoryId, GenderRace raceCode, EquipSlot slot)
                => $"chara/accessory/a{accessoryId.Id:D4}/model/c{raceCode.ToRaceCode()}a{accessoryId.Id:D4}_{slot.ToSuffix()}.mdl";
        }

        public static partial class Mtrl
        {
            [GeneratedRegex(
                @"chara/accessory/a(?'id'\d{4})/material/v(?'variant'\d{4})/mt_c(?'race'\d{4})a\k'id'_(?'slot'[a-z]{3})_[a-z]+\.mtrl", Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId accessoryId, GenderRace raceCode, EquipSlot slot, Variant variant, string suffix)
                => $"{FolderPath(accessoryId, variant)}/mt_c{raceCode.ToRaceCode()}a{accessoryId.Id:D4}_{slot.ToSuffix()}_{suffix}.mtrl";

            public static string FolderPath(PrimaryId accessoryId, Variant variant)
                => $"chara/accessory/a{accessoryId.Id:D4}/material/v{variant.Id:D4}";
        }

        public static partial class Tex
        {
            [GeneratedRegex(
                @"chara/accessory/a(?'id'\d{4})/texture/v(?'variant'\d{2})_c(?'race'\d{4})a\k'id'_(?'slot'[a-z]{3})(_[a-z])?_[a-z]\.tex",
                Flags1)]
            public static partial Regex Regex();

            public static string Path(PrimaryId accessoryId, GenderRace raceCode, EquipSlot slot, Variant variant, char suffix1,
                char suffix2 = '\0')
                => $"chara/accessory/a{accessoryId.Id:D4}/texture/v{variant.Id:D2}_c{raceCode.ToRaceCode()}a{accessoryId.Id:D4}_{slot.ToSuffix()}{(suffix2 != '\0' ? $"_{suffix2}" : string.Empty)}_{suffix1}.tex";
        }
    }

    public static partial class Skeleton
    {
        public static partial class Phyb
        {
            public static string Path(GenderRace raceCode, string slot, PrimaryId slotId)
                => $"chara/human/c{raceCode.ToRaceCode()}/skeleton/{slot}/{slot[0]}{slotId.Id:D4}/phy_c{raceCode.ToRaceCode()}{slot[0]}{slotId.Id:D4}.phyb";
        }

        public static partial class Sklb
        {
            public static string Path(GenderRace raceCode, string slot, PrimaryId slotId)
                => $"chara/human/c{raceCode.ToRaceCode()}/skeleton/{slot}/{slot[0]}{slotId.Id:D4}/skl_c{raceCode.ToRaceCode()}{slot[0]}{slotId.Id:D4}.sklb";
        }

        public static partial class Skp
        {
            public static string Path(GenderRace raceCode, string slot, PrimaryId slotId)
                => $"chara/human/c{raceCode.ToRaceCode()}/skeleton/{slot}/{slot[0]}{slotId.Id:D4}/skl_c{raceCode.ToRaceCode()}{slot[0]}{slotId.Id:D4}.skp";
        }
    }

    public static partial class Character
    {
        public static partial class Mdl
        {
            [GeneratedRegex(
                @"chara/human/c(?'race'\d{4})/obj/(?'type'[a-z]+)/(?'typeabr'[a-z])(?'id'\d{4})/model/c\k'race'\k'typeabr'\k'id'_(?'slot'[a-z]{3})\.mdl",
                Flags1)]
            public static partial Regex Regex();

            public static string Path(GenderRace raceCode, BodySlot slot, PrimaryId slotId, CustomizationType type)
                => $"chara/human/c{raceCode.ToRaceCode()}/obj/{slot.ToSuffix()}/{slot.ToAbbreviation()}{slotId.Id:D4}/model/c{raceCode.ToRaceCode()}{slot.ToAbbreviation()}{slotId.Id:D4}_{type.ToSuffix()}.mdl";
        }

        public static partial class Mtrl
        {
            [GeneratedRegex(
                @"chara/human/c(?'race'\d{4})/obj/(?'type'[a-z]+)/(?'typeabr'[a-z])(?'id'\d{4})/material(/v(?'variant'\d{4}))?/mt_c\k'race'\k'typeabr'\k'id'(.*?_(?'slot'[a-z]{3}))?_[a-z]+(?:_[^.]*)?\.mtrl",
                Flags1)]
            public static partial Regex Regex();

            public static string FolderPath(GenderRace raceCode, BodySlot slot, PrimaryId slotId, Variant variant)
                => variant == Variant.None
                    ? FolderPath(raceCode, slot, slotId)
                    : $"chara/human/c{raceCode.ToRaceCode()}/obj/{slot.ToSuffix()}/{slot.ToAbbreviation()}{slotId.Id:D4}/material/v{variant.Id:D4}";

            public static string FolderPath(GenderRace raceCode, BodySlot slot, PrimaryId slotId)
                => $"chara/human/c{raceCode.ToRaceCode()}/obj/{slot.ToSuffix()}/{slot.ToAbbreviation()}{slotId.Id:D4}/material";

            public static string HairPath(GenderRace raceCode, PrimaryId slotId, string fileName, out GenderRace actualGr)
            {
                actualGr = MaterialHandling.GetGameGenderRace(raceCode, slotId);
                var folder = FolderPath(actualGr, BodySlot.Hair, slotId, 1);
                return actualGr == raceCode
                    ? $"{folder}{fileName}"
                    : $"{folder}/mt_c{actualGr.ToRaceCode()}{fileName[9..]}";
            }

            public static string TailPath(GenderRace raceCode, PrimaryId slotId, string fileName, Variant variant, out PrimaryId actualSlotId)
            {
                switch (raceCode)
                {
                    case GenderRace.HrothgarMale:
                    case GenderRace.HrothgarFemale:
                    case GenderRace.HrothgarMaleNpc:
                    case GenderRace.HrothgarFemaleNpc:
                        var folder = FolderPath(raceCode, BodySlot.Tail, 1, variant == Variant.None ? 1 : variant);
                        actualSlotId = 1;
                        return $"{folder}{fileName}";
                    default:
                        actualSlotId = slotId;
                        return $"{FolderPath(raceCode, BodySlot.Tail, slotId, variant)}{fileName}";
                }
            }

            public static string Path(GenderRace raceCode, BodySlot slot, PrimaryId slotId, string fileName,
                out GenderRace actualGr, out PrimaryId actualSlotId)
                => Path(raceCode, slot, slotId, fileName, out actualGr, out actualSlotId, Variant.None);

            public static string Path(GenderRace raceCode, BodySlot slot, PrimaryId slotId, string fileName,
                out GenderRace actualGr, out PrimaryId actualSlotId, Variant variant)
            {
                switch (slot)
                {
                    case BodySlot.Hair:
                        actualSlotId = slotId;
                        return HairPath(raceCode, slotId, fileName, out actualGr);
                    case BodySlot.Tail:
                        actualGr = raceCode;
                        return TailPath(raceCode, slotId, fileName, variant, out actualSlotId);
                    default:
                        actualSlotId = slotId;
                        actualGr     = raceCode;
                        return $"{FolderPath(raceCode, slot, slotId, variant)}{fileName}";
                }
            }
        }

        public static partial class Tex
        {
            [GeneratedRegex(
                @"chara/human/c(?'race'\d{4})/obj/(?'type'[a-z]+)/(?'typeabr'[a-z])(?'id'\d{4})/texture/(?'minus'(--)?)(v(?'variant'\d{2})_)?c\k'race'\k'typeabr'\k'id'(_(?'slot'[a-z]{3}))?(_[a-z])?_[a-z]\.tex",
                Flags1)]
            public static partial Regex Regex();

            public static string Path(GenderRace raceCode, BodySlot slot, PrimaryId slotId, char suffix1, bool minus = false,
                CustomizationType type = CustomizationType.Unknown, char suffix2 = '\0')
                => Path(raceCode, slot, slotId, suffix1, Variant.None, minus, type, suffix2);

            public static string Path(GenderRace raceCode, BodySlot slot, PrimaryId slotId, char suffix1, Variant variant, bool minus = false,
                CustomizationType type = CustomizationType.Unknown, char suffix2 = '\0')
                => $"chara/human/c{raceCode.ToRaceCode()}/obj/{slot.ToSuffix()}/{slot.ToAbbreviation()}{slotId.Id:D4}/texture/"
                  + (minus ? "--" : string.Empty)
                  + (variant != Variant.None ? $"v{variant.Id:D2}_" : string.Empty)
                  + $"c{raceCode.ToRaceCode()}{slot.ToAbbreviation()}{slotId.Id:D4}{(type != CustomizationType.Unknown ? $"_{type.ToSuffix()}" : string.Empty)}{(suffix2 != '\0' ? $"_{suffix2}" : string.Empty)}_{suffix1}.tex";


            [GeneratedRegex(@"chara/common/texture/(?'catchlight'catchlight)(.*)\.tex", Flags2)]
            public static partial Regex CatchlightRegex();

            [GeneratedRegex(@"chara/common/texture/skin(?'skin'.*)\.tex", Flags2)]
            public static partial Regex SkinRegex();

            [GeneratedRegex(@"chara/common/texture/decal_(?'location'[a-z]+)/[-_]?decal_(?'id'\d+).tex", Flags2)]
            public static partial Regex DecalRegex();

            [GeneratedRegex(@"chara/human/c(?'race'\d{4})/obj/(?'type'[a-z]+)/(?'typeabr'[a-z])(?'id'\d{4})/texture", Flags2)]
            public static partial Regex FolderRegex();
        }
    }

    public static partial class Icon
    {
        [GeneratedRegex(@"ui/icon/(?'group'\d*)(/(?'lang'[a-z]{2}))?(/(?'hq'hq))?/(?'id'\d*)(?'hr'_hr1)?\.tex", Flags2)]
        public static partial Regex Regex();
    }

    public static partial class Map
    {
        [GeneratedRegex(@"ui/map/(?'id'[a-z0-9]{4})/(?'variant'\d{2})/\k'id'\k'variant'(?'suffix'[a-z])?(_[a-z])?\.tex", Flags1)]
        public static partial Regex Regex();
    }

    public static partial class Font
    {
        [GeneratedRegex(@"common/font/(?'fontname'.*)_(?'id'\d\d)(_lobby)?\.fdt", Flags2)]
        public static partial Regex Regex();
    }

    public static partial class Vfx
    {
        [GeneratedRegex(@"chara[\/]action[\/](?'key'[^\s]+?)\.tmb", RegexOptions.IgnoreCase | Flags2)]
        public static partial Regex Tmb();

        public static string ActionTmb(string key)
            => $"chara/action/{key}.tmb";

        [GeneratedRegex(@"chara[\/]human[\/]c0101[\/]animation[\/]a0001[\/][^\s]+?[\/](?'key'[^\s]+?)\.pap", RegexOptions.IgnoreCase | Flags2)]
        public static partial Regex Pap();
    }

    public static partial class Shader
    {
        public static string ShpkPath(string name)
            => $"shader/sm5/shpk/{name}";
    }

    public static partial class Tex
    {
        public const string DummyPath = "common/graphics/texture/dummy.tex";

        public const string TransparentPath = "chara/common/texture/transparent.tex";

        /// <summary> DX11 specific textures get '--' prepended to the filename. Even if the filename already starts with '--'. </summary>
        /// <param name="texture"> The texture struct to check. </param>
        /// <param name="ret"> The correctly modified path. </param>
        /// <returns> Whether the return path was manipulated. </returns>
        public static bool HandleDx11Path(in MtrlFile.Texture texture, out string ret)
        {
            if (!texture.DX11)
            {
                ret = texture.Path;
                return false;
            }

            var fileName       = Path.GetFileName(texture.Path.AsSpan());
            var lastSlashIndex = texture.Path.LastIndexOf('/');
            ret = lastSlashIndex >= 0
                ? $"{texture.Path.AsSpan()[..lastSlashIndex]}/--{fileName}"
                : $"--{fileName}";
            return true;
        }
    }
}
