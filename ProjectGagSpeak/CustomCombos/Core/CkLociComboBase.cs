using GagSpeak.Services;
using Dalamud.Bindings.ImGui;
using CkCommons.Textures;

namespace GagSpeak.CustomCombos;

public abstract class CkLociComboBase<T> : CkFilterComboCache<T>
{
    protected float IconScale { get; }
    protected CkLociComboBase(ILogger log, float iconScale, Func<IReadOnlyList<T>> generator)
        : base(generator, log)
    {
        IconScale = iconScale;
    }

    protected unsafe virtual float SelectableTextHeight => Fonts.Default150PercentPtr.IsLoaded()
        ? Fonts.Default150PercentPtr.FontSize : ImGui.GetTextLineHeight();
    protected virtual Vector2 IconSize => LociIcon.Size * IconScale;
}
