using GagSpeak.PlayerData.Pairs;

namespace GagSpeak.PlayerState.Visual;

/// <summary>
/// This class helps handle which traits are currently set to active, and who they were set active by.
/// Additionally helps manage who can have traits applied to what.
/// </summary>
public class TraitsManager
{
    private readonly ILogger<TraitsManager> _logger;
    private readonly GagRestrictionManager _gagManager;
    private readonly RestrictionManager _restrictionManager;
    private readonly RestraintManager _restraintManager;
    private readonly PairManager _pairs;

    public TraitsManager(ILogger<TraitsManager> logger, GagRestrictionManager gagManager, 
        RestrictionManager restrictionManager, RestraintManager restraintManager, 
        PairManager pairs)
    {
        _logger = logger;
        _gagManager = gagManager;
        _restrictionManager = restrictionManager;
        _restraintManager = restraintManager;
        _pairs = pairs;
    }
}
