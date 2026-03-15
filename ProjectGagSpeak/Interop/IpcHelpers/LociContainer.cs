using GagspeakAPI.Data;
using MessagePack;

namespace GagSpeak.Interop.Helpers;


/// <summary>
///   A container for data using LociAPI enums and tuples. Has helpers for converting.
/// </summary>
public class LociContainer
{
    public Dictionary<Guid, LociStatusInfo> DataInfo { get; set; } = [];
    public Dictionary<Guid, LociStatusInfo> Statuses { get; set; } = [];
    public Dictionary<Guid, LociPresetInfo> Presets { get; set; } = [];

    // Convenience access to collections.
    [IgnoreMember] public IEnumerable<LociStatusInfo> DataInfoList => DataInfo.Values;
    [IgnoreMember] public IEnumerable<LociStatusInfo> StatusList => Statuses.Values;
    [IgnoreMember] public IEnumerable<LociPresetInfo> PresetList => Presets.Values;

    public LociContainer()
    { }

    public LociContainer(LociContainer other)
    {
        DataInfo = new Dictionary<Guid, LociStatusInfo>(other.DataInfo);
        Statuses = new Dictionary<Guid, LociStatusInfo>(other.Statuses);
        Presets = new Dictionary<Guid, LociPresetInfo>(other.Presets);
    }

    public LociContainer(LociContainerData dto)
    {
        DataInfo = dto.SMInfo.ToDictionary(x => x.GUID, x => x.ToTuple());
        Statuses = dto.Statuses.ToDictionary(x => x.GUID, x => x.ToTuple());
        Presets = dto.Presets.ToDictionary(x => x.GUID, x => x.ToTuple());
    }

    public void SetDataInfo(IEnumerable<LociStatusInfo> statuses)
        => DataInfo = statuses.ToDictionary(x => x.GUID, x => x);

    public void SetStatuses(IEnumerable<LociStatusInfo> statuses)
        => Statuses = statuses.ToDictionary(x => x.GUID, x => x);
    public void SetStatuses(IEnumerable<LociStatusStruct> statuses)
        => Statuses = statuses.ToDictionary(x => x.GUID, x => x.ToTuple());

    public void SetPresets(IEnumerable<LociPresetInfo> presets)
        => Presets = presets.ToDictionary(x => x.GUID, x => x);
    public void SetPresets(IEnumerable<LociPresetStruct> presets)
        => Presets = presets.ToDictionary(x => x.GUID, x => x.ToTuple());

    public LociContainerData ToDto()
    {
        var info = DataInfo.Values.Select(x => x.ToStruct()).ToList();
        var statuses = Statuses.Values.Select(x => x.ToStruct()).ToList();
        var presets = Presets.Values.Select(x => x.ToStruct()).ToList();
        return new(info, statuses, presets);
    }
}
