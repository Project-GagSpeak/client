using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.UI;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.CustomCombos;

public abstract class CkMoodleComboBase<T> : CkFilterComboCache<T>
{
    protected readonly CharaIPCData _moodleData;
    protected readonly MoodlesDisplayer _displayer;
    protected float _iconScale;

    protected CkMoodleComboBase(float iconScale, CharaIPCData data, MoodlesDisplayer displayer, ILogger log,
        Func<IReadOnlyList<T>> generator) : base(generator, log)
    {
        _displayer = displayer;
        _iconScale = iconScale;
        _moodleData = data;
    }

    protected virtual Vector2 IconSize => MoodlesDisplayer.DefaultSize * _iconScale;

    protected void DrawItemTooltip(MoodlesStatusInfo item)
        => _displayer.DrawMoodleStatusTooltip(item, _moodleData.MoodlesStatuses);
}
