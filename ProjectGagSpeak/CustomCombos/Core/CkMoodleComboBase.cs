using GagSpeak.Gui.Components;
using GagSpeak.Services;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using Dalamud.Bindings.ImGui;

namespace GagSpeak.CustomCombos;

public abstract class CkMoodleComboBase<T> : CkFilterComboCache<T>
{
    protected float IconScale { get; }
    protected CkMoodleComboBase(ILogger log, float iconScale, Func<IReadOnlyList<T>> generator)
        : base(generator, log)
    {
        IconScale = iconScale;
    }

    protected unsafe virtual float SelectableTextHeight => UiFontService.Default150PercentPtr.IsLoaded()
        ? UiFontService.Default150PercentPtr.FontSize : ImGui.GetTextLineHeight();
    protected virtual Vector2 IconSize => MoodleDrawer.IconSize * IconScale;

    protected void DrawItemTooltip(MoodlesStatusInfo item)
        => GagspeakEx.DrawMoodleStatusTooltip(item, MoodleCache.IpcData.StatusList);
}
