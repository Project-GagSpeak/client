using GagSpeak.Utils.Enums;

namespace GagSpeak.Hardcore;
public enum AcReqProps // "Action Required Properties"
{
    None,           // if the actions has no properties attached
    Movement,       // if the action requires any movement of any kind
    LegMovement,    // if the action requires dramatic movement from legs (flips ext.) 
    ArmMovement,    // if the action requires dramatic movement from arms (punches ext.)
    Speech,         // if the action requires a verbal scream or use of their mouth
    Sight,          // if the action requires direct sight / contact to point at something with, such as ordering a pet to attack something
    Weighted,       // if the action requires a heavy object to be lifted or moved
}

// class for identifying which action is being used and the properties associated with it.
public class GagspeakActionData
{
    public static void GetJobActionProperties(JobType job, out Dictionary<uint, AcReqProps[]> bannedActions)
    {
        // return the correct dictionary from our core data.
        switch (job)
        {
            case JobType.ADV: { bannedActions = ActionDataCore.Adventurer; return; }
            case JobType.GLA: { bannedActions = ActionDataCore.Gladiator; return; }
            case JobType.PGL: { bannedActions = ActionDataCore.Pugilist; return; }
            case JobType.MRD: { bannedActions = ActionDataCore.Marauder; return; }
            case JobType.LNC: { bannedActions = ActionDataCore.Lancer; return; }
            case JobType.ARC: { bannedActions = ActionDataCore.Archer; return; }
            case JobType.CNJ: { bannedActions = ActionDataCore.Conjurer; return; }
            case JobType.THM: { bannedActions = ActionDataCore.Thaumaturge; return; }
            case JobType.CRP: { bannedActions = ActionDataCore.Carpenter; return; }
            case JobType.BSM: { bannedActions = ActionDataCore.Blacksmith; return; }
            case JobType.ARM: { bannedActions = ActionDataCore.Armorer; return; }
            case JobType.GSM: { bannedActions = ActionDataCore.Goldsmith; return; }
            case JobType.LTW: { bannedActions = ActionDataCore.Leatherworker; return; }
            case JobType.WVR: { bannedActions = ActionDataCore.Weaver; return; }
            case JobType.ALC: { bannedActions = ActionDataCore.Alchemist; return; }
            case JobType.CUL: { bannedActions = ActionDataCore.Culinarian; return; }
            case JobType.MIN: { bannedActions = ActionDataCore.Miner; return; }
            case JobType.BTN: { bannedActions = ActionDataCore.Botanist; return; }
            case JobType.FSH: { bannedActions = ActionDataCore.Fisher; return; }
            case JobType.PLD: { bannedActions = ActionDataCore.Paladin; return; }
            case JobType.MNK: { bannedActions = ActionDataCore.Monk; return; }
            case JobType.WAR: { bannedActions = ActionDataCore.Warrior; return; }
            case JobType.DRG: { bannedActions = ActionDataCore.Dragoon; return; }
            case JobType.BRD: { bannedActions = ActionDataCore.Bard; return; }
            case JobType.WHM: { bannedActions = ActionDataCore.WhiteMage; return; }
            case JobType.BLM: { bannedActions = ActionDataCore.BlackMage; return; }
            case JobType.ACN: { bannedActions = ActionDataCore.Arcanist; return; }
            case JobType.SMN: { bannedActions = ActionDataCore.Summoner; return; }
            case JobType.SCH: { bannedActions = ActionDataCore.Scholar; return; }
            case JobType.ROG: { bannedActions = ActionDataCore.Rogue; return; }
            case JobType.NIN: { bannedActions = ActionDataCore.Ninja; return; }
            case JobType.MCH: { bannedActions = ActionDataCore.Machinist; return; }
            case JobType.DRK: { bannedActions = ActionDataCore.DarkKnight; return; }
            case JobType.AST: { bannedActions = ActionDataCore.Astrologian; return; }
            case JobType.SAM: { bannedActions = ActionDataCore.Samurai; return; }
            case JobType.RDM: { bannedActions = ActionDataCore.RedMage; return; }
            case JobType.BLU: { bannedActions = ActionDataCore.BlueMage; return; }
            case JobType.GNB: { bannedActions = ActionDataCore.Gunbreaker; return; }
            case JobType.DNC: { bannedActions = ActionDataCore.Dancer; return; }
            case JobType.RPR: { bannedActions = ActionDataCore.Reaper; return; }
            case JobType.SGE: { bannedActions = ActionDataCore.Sage; return; }
            case JobType.VPR: { bannedActions = ActionDataCore.Viper; return; }
            case JobType.PCT: { bannedActions = ActionDataCore.Pictomancer; return; }
            default: { bannedActions = new Dictionary<uint, AcReqProps[]>(); return; } // return an empty list if job does not exist
        }
    }
}
