using GagSpeak.PlayerState.Visual;
using GagSpeak.UI.Components;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data.Character;

namespace GagSpeak.CustomCombos;

public abstract class CkMoodleComboBase<T> : CkFilterComboCache<T>
{
    protected readonly MoodlesDisplayer _displayer;
    protected float _iconScale;

    protected CkMoodleComboBase(float iconScale, MoodlesDisplayer displayer, ILogger log,
        Func<IReadOnlyList<T>> generator) : base(generator, log)
    {
        _displayer = displayer;
        _iconScale = iconScale;
    }

    protected virtual Vector2 IconSize => MoodleDrawer.IconSize * _iconScale;

    protected void DrawItemTooltip(MoodlesStatusInfo item)
        => _displayer.DrawMoodleStatusTooltip(item, VisualApplierMoodles.LatestIpcData.MoodlesStatuses);
}
