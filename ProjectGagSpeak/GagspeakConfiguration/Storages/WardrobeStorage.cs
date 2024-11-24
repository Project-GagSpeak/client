using GagSpeak.Utils;

namespace GagSpeak.GagspeakConfiguration.Models;

[Serializable]
public class WardrobeStorage
{
    /// <summary> The list of restraint sets in the wardrobe </summary>
    public List<RestraintSet> RestraintSets { get; set; } = new List<RestraintSet>();

    /// <summary> The DrawData for the Hardcore Blindfold Item </summary>
    public BlindfoldModel BlindfoldInfo { get; set; } = new BlindfoldModel();
}
