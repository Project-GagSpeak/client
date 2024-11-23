using GagspeakAPI.Enums;

namespace GagspeakAPI.Extensions;

public static partial class EnumToName
{
    public const string None = "None";
    public const string BallGag = "Ball Gag";
    public const string BallGagMask = "Ball Gag Mask";
    public const string BambooGag = "Bamboo Gag";
    public const string BeltStrapGag = "Belt Strap Gag";
    public const string BitGag = "Bit Gag";
    public const string BitGagPadded = "Bit Gag Padded";
    public const string BoneGag = "Bone Gag";
    public const string BoneGagXL = "Bone Gag (XL)";
    public const string CandleGag = "Candle Gag";
    public const string CageMuzzle = "Cage Muzzle";
    public const string CleaveGag = "Cleave Gag";
    public const string ChloroformGag = "Chloroform Gag";
    public const string ChopStickGag = "Chopstick Gag";
    public const string ClothWrapGag = "Cloth Wrap Gag";
    public const string ClothStuffingGag = "Cloth Stuffing Gag";
    public const string CropGag = "Crop Gag";
    public const string CupHolderGag = "Cup Holder Gag";
    public const string DeepthroatPenisGag = "Deepthroat Penis Gag";
    public const string DentalGag = "Dental Gag";
    public const string DildoGag = "Dildo Gag";
    public const string DuctTapeGag = "Duct Tape Gag";
    public const string DusterGag = "Duster Gag";
    public const string FunnelGag = "Funnel Gag";
    public const string FuturisticHarnessBallGag = "Futuristic Harness Ball Gag";
    public const string FuturisticHarnessPanelGag = "Futuristic Harness Panel Gag";
    public const string GasMask = "Gas Mask";
    public const string HarnessBallGag = "Harness Ball Gag";
    public const string HarnessBallGagXL = "Harness Ball Gag XL";
    public const string HarnessPanelGag = "Harness Panel Gag";
    public const string HookGagMask = "Hook Gag Mask";
    public const string InflatableHood = "Inflatable Hood";
    public const string LargeDildoGag = "Large Dildo Gag";
    public const string LatexHood = "Latex Hood";
    public const string LatexBallMuzzleGag = "Latex Ball Muzzle Gag";
    public const string LatexPostureCollarGag = "Latex Posture Collar Gag";
    public const string LeatherCorsetCollarGag = "Leather Corset Collar Gag";
    public const string LeatherHood = "Leather Hood";
    public const string LipGag = "Lip Gag";
    public const string MedicalMask = "Medical Mask";
    public const string MuzzleGag = "Muzzle Gag";
    public const string PantyStuffingGag = "Panty Stuffing Gag";
    public const string PlasticWrapGag = "Plastic Wrap Gag";
    public const string PlugGag = "Plug Gag";
    public const string PonyGag = "Pony Gag";
    public const string PumpGagLv1 = "Pump Gag Lv.1";
    public const string PumpGagLv2 = "Pump Gag Lv.2";
    public const string PumpGagLv3 = "Pump Gag Lv.3";
    public const string PumpGagLv4 = "Pump Gag Lv.4";
    public const string RibbonGag = "Ribbon Gag";
    public const string RingGag = "Ring Gag";
    public const string RopeGag = "Rope Gag";
    public const string ScarfGag = "Scarf Gag";
    public const string SensoryDeprivationHood = "Sensory Deprivation Hood";
    public const string SockStuffingGag = "Sock Stuffing Gag";
    public const string SpiderGag = "Spider Gag";
    public const string TentacleGag = "Tentacle Gag";
    public const string WebGag = "Web Gag";
    public const string WhiffleGag = "Whiffle Gag";

    public static GagType ToGagType(this string alias) => alias switch
    {
        BallGag => GagType.BallGag,
        BallGagMask => GagType.BallGagMask,
        BambooGag => GagType.BambooGag,
        BeltStrapGag => GagType.BeltStrapGag,
        BitGag => GagType.BitGag,
        BitGagPadded => GagType.BitGagPadded,
        BoneGag => GagType.BoneGag,
        BoneGagXL => GagType.BoneGagXL,
        CandleGag => GagType.CandleGag,
        CageMuzzle => GagType.CageMuzzle,
        CleaveGag => GagType.CleaveGag,
        ChloroformGag => GagType.ChloroformGag,
        ChopStickGag => GagType.ChopStickGag,
        ClothWrapGag => GagType.ClothWrapGag,
        ClothStuffingGag => GagType.ClothStuffingGag,
        CropGag => GagType.CropGag,
        CupHolderGag => GagType.CupHolderGag,
        DeepthroatPenisGag => GagType.DeepthroatPenisGag,
        DentalGag => GagType.DentalGag,
        DildoGag => GagType.DildoGag,
        DuctTapeGag => GagType.DuctTapeGag,
        DusterGag => GagType.DusterGag,
        FunnelGag => GagType.FunnelGag,
        FuturisticHarnessBallGag => GagType.FuturisticHarnessBallGag,
        FuturisticHarnessPanelGag => GagType.FuturisticHarnessPanelGag,
        GasMask => GagType.GasMask,
        HarnessBallGag => GagType.HarnessBallGag,
        HarnessBallGagXL => GagType.HarnessBallGagXL,
        HarnessPanelGag => GagType.HarnessPanelGag,
        HookGagMask => GagType.HookGagMask,
        InflatableHood => GagType.InflatableHood,
        LargeDildoGag => GagType.LargeDildoGag,
        LatexHood => GagType.LatexHood,
        LatexBallMuzzleGag => GagType.LatexBallMuzzleGag,
        LatexPostureCollarGag => GagType.LatexPostureCollarGag,
        LeatherCorsetCollarGag => GagType.LeatherCorsetCollarGag,
        LeatherHood => GagType.LeatherHood,
        LipGag => GagType.LipGag,
        MedicalMask => GagType.MedicalMask,
        MuzzleGag => GagType.MuzzleGag,
        PantyStuffingGag => GagType.PantyStuffingGag,
        PlasticWrapGag => GagType.PlasticWrapGag,
        PlugGag => GagType.PlugGag,
        PonyGag => GagType.PonyGag,
        PumpGagLv1 => GagType.PumpGaglv1,
        PumpGagLv2 => GagType.PumpGaglv2,
        PumpGagLv3 => GagType.PumpGaglv3,
        PumpGagLv4 => GagType.PumpGaglv4,
        RibbonGag => GagType.RibbonGag,
        RingGag => GagType.RingGag,
        RopeGag => GagType.RopeGag,
        ScarfGag => GagType.ScarfGag,
        SensoryDeprivationHood => GagType.SensoryDeprivationHood,
        SockStuffingGag => GagType.SockStuffingGag,
        SpiderGag => GagType.SpiderGag,
        TentacleGag => GagType.TentacleGag,
        WebGag => GagType.WebGag,
        WhiffleGag => GagType.WhiffleGag,
        None => GagType.None,
        _ => GagType.None
    };

    // now create a static helper to go from the type back to the string.
    public static string GagName(this GagType gagType) => gagType switch
    {
        GagType.BallGag => BallGag,
        GagType.BallGagMask => BallGagMask,
        GagType.BambooGag => BambooGag,
        GagType.BeltStrapGag => BeltStrapGag,
        GagType.BitGag => BitGag,
        GagType.BitGagPadded => BitGagPadded,
        GagType.BoneGag => BoneGag,
        GagType.BoneGagXL => BoneGagXL,
        GagType.CandleGag => CandleGag,
        GagType.CageMuzzle => CageMuzzle,
        GagType.CleaveGag => CleaveGag,
        GagType.ChloroformGag => ChloroformGag,
        GagType.ChopStickGag => ChopStickGag,
        GagType.ClothWrapGag => ClothWrapGag,
        GagType.ClothStuffingGag => ClothStuffingGag,
        GagType.CropGag => CropGag,
        GagType.CupHolderGag => CupHolderGag,
        GagType.DeepthroatPenisGag => DeepthroatPenisGag,
        GagType.DentalGag => DentalGag,
        GagType.DildoGag => DildoGag,
        GagType.DuctTapeGag => DuctTapeGag,
        GagType.DusterGag => DusterGag,
        GagType.FunnelGag => FunnelGag,
        GagType.FuturisticHarnessBallGag => FuturisticHarnessBallGag,
        GagType.FuturisticHarnessPanelGag => FuturisticHarnessPanelGag,
        GagType.GasMask => GasMask,
        GagType.HarnessBallGag => HarnessBallGag,
        GagType.HarnessBallGagXL => HarnessBallGagXL,
        GagType.HarnessPanelGag => HarnessPanelGag,
        GagType.HookGagMask => HookGagMask,
        GagType.InflatableHood => InflatableHood,
        GagType.LargeDildoGag => LargeDildoGag,
        GagType.LatexHood => LatexHood,
        GagType.LatexBallMuzzleGag => LatexBallMuzzleGag,
        GagType.LatexPostureCollarGag => LatexPostureCollarGag,
        GagType.LeatherCorsetCollarGag => LeatherCorsetCollarGag,
        GagType.LeatherHood => LeatherHood,
        GagType.LipGag => LipGag,
        GagType.MedicalMask => MedicalMask,
        GagType.MuzzleGag => MuzzleGag,
        GagType.PantyStuffingGag => PantyStuffingGag,
        GagType.PlasticWrapGag => PlasticWrapGag,
        GagType.PlugGag => PlugGag,
        GagType.PonyGag => PonyGag,
        GagType.PumpGaglv1 => PumpGagLv1,
        GagType.PumpGaglv2 => PumpGagLv2,
        GagType.PumpGaglv3 => PumpGagLv3,
        GagType.PumpGaglv4 => PumpGagLv4,
        GagType.RibbonGag => RibbonGag,
        GagType.RingGag => RingGag,
        GagType.RopeGag => RopeGag,
        GagType.ScarfGag => ScarfGag,
        GagType.SensoryDeprivationHood => SensoryDeprivationHood,
        GagType.SockStuffingGag => SockStuffingGag,
        GagType.SpiderGag => SpiderGag,
        GagType.TentacleGag => TentacleGag,
        GagType.WebGag => WebGag,
        GagType.WhiffleGag => WhiffleGag,
        GagType.None => None,
        _ => None
    };

    // Create a HashSet of valid gag names for quick lookup
    private static readonly HashSet<string> ValidGagNames = new HashSet<string>
    {
        BallGag,
        BallGagMask,
        BambooGag,
        BeltStrapGag,
        BitGag,
        BitGagPadded,
        BoneGag,
        BoneGagXL,
        CandleGag,
        CageMuzzle,
        CleaveGag,
        ChloroformGag,
        ChopStickGag,
        ClothWrapGag,
        ClothStuffingGag,
        CropGag,
        CupHolderGag,
        DeepthroatPenisGag,
        DentalGag,
        DildoGag,
        DuctTapeGag,
        DusterGag,
        FunnelGag,
        FuturisticHarnessBallGag,
        FuturisticHarnessPanelGag,
        GasMask,
        HarnessBallGag,
        HarnessBallGagXL,
        HarnessPanelGag,
        HookGagMask,
        InflatableHood,
        LargeDildoGag,
        LatexHood,
        LatexBallMuzzleGag,
        LatexPostureCollarGag,
        LeatherCorsetCollarGag,
        LeatherHood,
        LipGag,
        MedicalMask,
        MuzzleGag,
        PantyStuffingGag,
        PlasticWrapGag,
        PlugGag,
        PonyGag,
        PumpGagLv1,
        PumpGagLv2,
        PumpGagLv3,
        PumpGagLv4,
        RibbonGag,
        RingGag,
        RopeGag,
        ScarfGag,
        SensoryDeprivationHood,
        SockStuffingGag,
        SpiderGag,
        TentacleGag,
        WebGag,
        WhiffleGag,
        None
    };

    // Method to check if a string is a valid gag name
    public static bool IsValidGagName(this string gagName)
    {
        return ValidGagNames.Contains(gagName);
    }



}
