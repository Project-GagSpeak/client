using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.State.Listeners;
using GagSpeak.UpdateMonitoring;

namespace GagSpeak.CustomCombos;

public abstract class CkMoodleComboBase<T> : CkFilterComboCache<T>
{
    protected readonly MoodleIcons _displayer;
    protected float _iconScale;

    protected CkMoodleComboBase(float iconScale, MoodleIcons displayer, ILogger log,
        Func<IReadOnlyList<T>> generator) : base(generator, log)
    {
        _displayer = displayer;
        _iconScale = iconScale;
    }

    protected virtual Vector2 IconSize => MoodleDrawer.IconSize * _iconScale;

    protected void DrawItemTooltip(MoodlesStatusInfo item)
        => _displayer.DrawMoodleStatusTooltip(item, MoodleCache.IpcData.StatusList);
}
