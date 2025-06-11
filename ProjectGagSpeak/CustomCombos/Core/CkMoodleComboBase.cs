using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.PlayerState.Visual;
using GagSpeak.UpdateMonitoring;

namespace GagSpeak.CustomCombos;

public abstract class CkMoodleComboBase<T> : CkFilterComboCache<T>
{
    protected readonly IconDisplayer _displayer;
    protected float _iconScale;

    protected CkMoodleComboBase(float iconScale, IconDisplayer displayer, ILogger log,
        Func<IReadOnlyList<T>> generator) : base(generator, log)
    {
        _displayer = displayer;
        _iconScale = iconScale;
    }

    protected virtual Vector2 IconSize => MoodleDrawer.IconSize * _iconScale;

    protected void DrawItemTooltip(MoodlesStatusInfo item)
        => _displayer.DrawMoodleStatusTooltip(item, MoodleHandler.IpcData.StatusList);
}
