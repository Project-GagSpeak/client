using KA = Penumbra.GameData.Enums.KnownAttribute;
using static Penumbra.GameData.Enums.EquipSlot;

namespace Penumbra.GameData.Enums;

public enum KnownAttribute
{
    HeadVariant,
    Gorget,

    BodyVariant,
    Wrist,
    Neck,
    Elbow,

    HandsVariant,
    Glove,

    LegsVariant,
    Knee,
    Waist,
    Shin,

    FeetVariant,
    Boot,
    KneePad,

    EarVariant,
    NeckVariant,
    WristsVariant,
    FingerVariant,

    OtherPart,
    CharacterPart,

    Arrow,
    QuiverArrow1,
    QuiverArrow2,
    QuiverArrow3,
    Attachment,

    Detail,
    HasNoTail,
    HasTail,
    MiqoteEars,
    AuRaFace,
    MiqoteHair,

    FaceVariant,
    FacialHair,
    Horns,
    Face,
    Ears,

    HairVariant,
    Scalp,

    TwoWeapons,
    ThreeBody,

    NeckConnector,
    WristConnector,
    AnkleConnector,
    WaistConnector,
}

public readonly record struct KnownAttributeData(
    KnownAttribute Attribute,
    EquipSlot Slot,
    byte Variant,
    string Name,
    string Value,
    string Description);

public static class KnownAttributeExtensions
{
    public static KnownAttributeData GetData(this KnownAttribute attribute, int variant = 0)
    {
        var a = attribute;
        var v = variant;
        return a switch
        {
            // @formatter:off
            KA.HeadVariant    => V(a, v, Head,    "Head Variant",      "mv",       "Separate part of a head piece hidden by IMC attributes.",                                                10),
            KA.Gorget         => V(a, v, Head,    "Gorget",            "inr",      "A part of a head piece denoted as a gorget, hidden by EQP attributes of the body.",                       0),
            KA.BodyVariant    => V(a, v, Body,    "Body Variant",      "tv",       "Separate part of a body piece hidden by IMC attributes.",                                                10),
            KA.Wrist          => V(a, v, Body,    "Wrist",             "hij",      "A part of a body piece denoted as being on the wrists, hidden by EQP attributes of the hands.",           0),
            KA.Neck           => V(a, v, Body,    "Neck",              "nek",      "A part of a body piece denoted as being on the neck, hidden by EQP attributes of the head.",              0), 
            KA.Elbow          => V(a, v, Body,    "Elbow",             "ude",      "A part of a body piece denoted as being on the elbow, hidden by EQP attributes of the hands.",            0),
            KA.HandsVariant   => V(a, v, Hands,   "Hands Variant",     "gv",       "Separate part of a hand piece hidden by IMC attributes.",                                                10),
            KA.Glove          => V(a, v, Hands,   "Glove",             "arm",      "A part of a hand piece denoted as reaching upwards on the elbow, hidden by EQP attributes of the body.",  0),
            KA.LegsVariant    => V(a, v, Legs,    "Legs Variant",      "dv",       "Separate part of a leg piece hidden by IMC attributes.",                                                 10),
            KA.Knee           => V(a, v, Legs,    "Knee",              "hiz",      "A part of a leg piece denoted as the knee, hidden by EQP attributes of the feet.",                        0),
            KA.Waist          => V(a, v, Legs,    "Waist",             "kod",      "A part of a leg piece denoted as the waist, hidden by EQP attributes of the body.",                       0),
            KA.Shin           => V(a, v, Legs,    "Shin",              "sne",      "A part of a leg piece denoted as the shin, hidden by EQP attributes of the feet.",                        0),
            KA.FeetVariant    => V(a, v, Feet,    "Feet Variant",      "sv",       "Separate part of a foot piece hidden by IMC attributes.",                                                10),
            KA.Boot           => V(a, v, Feet,    "Boot",              "leg",      "A part of a foot piece denoted as the upper boot, hidden by EQP attributes of the legs.",                 0),
            KA.KneePad        => V(a, v, Feet,    "Knee Pad",          "lpd",      "A part of a foot piece denoted as a knee pad, hidden by EQP attributes of the legs.",                     0),
            KA.EarVariant     => V(a, v, Ears,    "Ears Variant",      "ev",       "Separate part of a ear piece hidden by IMC attributes.",                                                  1),
            KA.NeckVariant    => V(a, v, Neck,    "Neck Variant",      "nv",       "Separate part of a neck piece hidden by IMC attributes.",                                                 1),
            KA.WristsVariant  => V(a, v, Wrists,  "Wrists Variant",    "wv",       "Separate part of a wrist piece hidden by IMC attributes.",                                                1),
            KA.FingerVariant  => V(a, v, RFinger, "Finger Variant",    "rv",       "Separate part of a finger piece hidden by IMC attributes.",                                               1),
            KA.OtherPart      => V(a, v, Unknown, "Other Variant",     "bv",       "Separate part of something hidden by IMC attributes.",                                                   10),
            KA.CharacterPart  => V(a, v, Unknown, "Body Variant",      "parts",    "Separate part of a weapon or monster hidden by IMC attributes.",                                         10),
            KA.Arrow          => V(a, v, Unknown, "Drawn Arrow",       "arrow",    "The drawn arrow on a bow weapon.",                                                                        0),
            KA.QuiverArrow1   => V(a, v, Unknown, "Quiver Arrow 1",    "ar1",      "The first arrow in a bow's quiver.",                                                                      0),
            KA.QuiverArrow2   => V(a, v, Unknown, "Quiver Arrow 2",    "ar2",      "The second arrow in a bow's quiver.",                                                                     0),
            KA.QuiverArrow3   => V(a, v, Unknown, "Quiver Arrow 3",    "ar3",      "The third arrow in a bow's quiver.",                                                                      0),
            KA.Attachment     => V(a, v, Unknown, "Attachment",        "attach",   "An additional weapon attachment like the Gauss Barrel.",                                                  0),
            KA.Detail         => V(a, v, Unknown, "Excess Detail",     "lod",      "A detailed part of the object that should be hidden with distance.",                                      0),
            KA.HasNoTail      => V(a, v, Unknown, "Non-Tailed Race",   "tlh",      "A part of the object that should only be shown for races with no tail.",                                  0),
            KA.HasTail        => V(a, v, Unknown, "Tailed Race",       "tls",      "A part of the object that should only be shown for races with a tail.",                                   0),
            KA.MiqoteEars     => V(a, v, Unknown, "Miqo'te Ears",      "top",      "Denotes the ears for Miqo'te.",                                                                           0),
            KA.AuRaFace       => V(a, v, Unknown, "Female Au Ra Face", "hair",     "Denotes hair on female Au Ra faces (?).",                                                                 0),
            KA.MiqoteHair     => V(a, v, Unknown, "Miqo'te Hair",      "sta",      "Denotes hair on Miqo'te only (?).",                                                                       0),
            KA.FaceVariant    => V(a, v, Unknown, "Face Variant",      "fv",       "Separate part of a face hidden by character customizations.",                                             8),
            KA.FacialHair     => V(a, v, Unknown, "Facial Hair",       "hig",      "Separate part of a weapon or monster hidden by IMC attributes.",                                          0),
            KA.Horns          => V(a, v, Unknown, "Horns",             "hrn",      "A part of a face denoted as horns.",                                                                      0),
            KA.Face           => V(a, v, Unknown, "Face",              "kao",      "A part of a face (?).",                                                                                   0),
            KA.Ears           => V(a, v, Unknown, "Ears",              "mim",      "A part of a face denoted as ears.",                                                                       0),
            KA.HairVariant    => V(a, v, Unknown, "Hair Variant",      "hv",       "Separate part of a hairstyle hidden by IMC attributes.",                                                 10),
            KA.Scalp          => V(a, v, Unknown, "Scalp",             "kam",      "A part of a hairstyle denoted as the scalp.",                                                             0),
            KA.TwoWeapons     => V(a, v, Unknown, "Two-Weapon Model",  "showhide", "Used on weapons that use two models (?).",                                                                0),
            KA.ThreeBody      => V(a, v, Unknown, "Three-Body Model",  "blt",      "Used on body pieces that use more than one slot (?).",                                                    0),
            KA.NeckConnector  => V(a, v, Unknown, "Neck Connector",    "cn_neck",  "Denotes the connector for the neck seam.",                                                                0),
            KA.WristConnector => V(a, v, Unknown, "Wrist Connector",   "cn_wrist", "Denotes the connector for the wrist seam.",                                                               0),
            KA.AnkleConnector => V(a, v, Unknown, "Ankle Connector",   "cn_ankle", "Denotes the connector for the ankle seam.",                                                               0),
            KA.WaistConnector => V(a, v, Unknown, "Waist Connector",   "cn_waist", "Denotes the connector for the waist seam.",                                                               0),
            // @formatter:on
            _ => throw new ArgumentOutOfRangeException(nameof(a), a, null),
        };
    }

    private static KnownAttributeData V(KA attribute, int variant, EquipSlot slot, string name, string baseValue,
        string description, int maxVariant)
    {
        if (_dict.TryGetValue((attribute, variant), out var ret))
            return ret;

        if (maxVariant == 0)
        {
            ret = new KnownAttributeData(attribute, slot, 0, name, baseValue, description);
        }
        else
        {
            if (variant < 0 || variant >= maxVariant)
                variant = 0;

            ret = new KnownAttributeData(attribute, slot, (byte)variant, $"{name} {(char)('A' + variant)}",
                $"atr_{baseValue}_{(char)('a' + variant)}", description);
        }

        _dict.Add((attribute, variant), ret);
        return ret;
    }

    private static Dictionary<(KA, int), KnownAttributeData> _dict = [];
}
