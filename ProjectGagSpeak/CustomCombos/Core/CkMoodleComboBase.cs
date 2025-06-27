using CkCommons.Textures;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.Utils;

namespace GagSpeak.CustomCombos;

public abstract class CkMoodleComboBase<T> : CkFilterComboCache<T>
{
    protected float _iconScale;

    protected CkMoodleComboBase(ILogger log, float iconScale, Func<IReadOnlyList<T>> generator) : base(generator, log)
    {
        _iconScale = iconScale;
    }

    protected virtual Vector2 IconSize => MoodleDrawer.IconSize * _iconScale;

    protected void DrawItemTooltip(MoodlesStatusInfo item)
        => GsExtensions.DrawMoodleStatusTooltip(item, MoodleCache.IpcData.StatusList);
}
