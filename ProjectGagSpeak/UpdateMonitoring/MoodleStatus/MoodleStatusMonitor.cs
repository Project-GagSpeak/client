using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagspeakAPI.Data.Permissions;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace GagSpeak.UpdateMonitoring;

/// <summary>
/// Keeps tabs on the currently polled Moodle information. Contains an initial dictionary cache.
/// ID's can then be efficiently looked up and stored into the cache instead of loading everything all at once.
/// </summary>
public class MoodleStatusMonitor
{
    private readonly ILogger<MoodleStatusMonitor> _logger;
    private readonly IDataManager _data;
    private readonly ITextureProvider _textures;

    public MoodleStatusMonitor(ILogger<MoodleStatusMonitor> logger, IDataManager data, ITextureProvider textures)
    {
        _logger = logger;
        _data = data;
        _textures = textures;

        // Get the subset of moodle statuses that are valid and appropriate for status application.
        StatusDict = _data.GetExcelSheet<Status>()
            .Where(x => x.Icon != 0 && !x.Name.ExtractText().IsNullOrWhitespace() && x.Icon > 200000)
            .ToDictionary(x => x.RowId, x => new ParsedIconInfo(x));
    }

    public static readonly Vector2 DefaultSize = new(24, 32);
    public static IReadOnlyDictionary<uint, ParsedIconInfo> StatusDict { get; private set; }

    /// <summary> Draws the Moodle icon. This only draw a single image so you can use IsItemHovered() outside. </summary>
    /// <param name="iconId"> The icon ID of the moodle. </param>
    /// <param name="stacks"> The amount of stacks the moodle has. </param>
    /// <param name="size"> The size of the moodle icon. </param>
    public void DrawMoodleIcon(int iconId, int stacks, Vector2 size)
    {
        var icon = _textures.GetFromGameIcon(new GameIconLookup((uint)(iconId + stacks - 1))).GetWrapOrEmpty();
        if (icon is { } wrap)
        {
            // offset moodles draw with maybe?
            ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() - new Vector2(0, 5));
            ImGui.Image(icon.ImGuiHandle, size);
        }
    }
    // Move the above into UiShared later probably lol.

    /// <summary> Validates if the pair can apply the status to the user. </summary>
    /// <param name="pairPerms"> The permissions of the pair. </param>
    /// <param name="statuses"> The statuses to apply. </param>
    /// <returns> True if the statuses can be applied.
    public bool CanApplyPairStatus(UserPairPermissions pairPerms, IEnumerable<MoodlesStatusInfo> statuses)
    {
        if (!pairPerms.MoodlePerms.HasAny(MoodlePerms.PositiveStatusTypes) && statuses.Any(statuses => statuses.Type == StatusType.Positive))
        {
            _logger.LogWarning("Client Attempted to apply status(s) with at least one containing a positive status, but they are not allowed to.");
            return false;
        }
        if (!pairPerms.MoodlePerms.HasAny(MoodlePerms.NegativeStatusTypes) && statuses.Any(statuses => statuses.Type == StatusType.Negative))
        {
            _logger.LogWarning("Client Attempted to apply status(s) with at least one containing a negative status, but they are not allowed to.");
            return false;
        }
        if (!pairPerms.MoodlePerms.HasAny(MoodlePerms.SpecialStatusTypes) && statuses.Any(statuses => statuses.Type == StatusType.Special))
        {
            _logger.LogWarning("Client Attempted to apply status(s) with at least one containing a special status, but they are not allowed to.");
            return false;
        }

        if (!pairPerms.MoodlePerms.HasAny(MoodlePerms.PermanentMoodles) && statuses.Any(statuses => statuses.NoExpire))
        {
            _logger.LogWarning("Client Attempted to apply status(s) with at least one containing a permanent status, but they are not allowed to.");
            return false;
        }

        // check the max moodle time exceeding
        if (statuses.Any(status => status.NoExpire == false && // if the status is not permanent, and the time its set for is longer than max allowed time.
            new TimeSpan(status.Days, status.Hours, status.Minutes, status.Seconds) > pairPerms.MaxMoodleTime))
        {
            _logger.LogWarning("Client Attempted to apply status(s) with at least one containing a time exceeding the max allowed time.");
            return false;
        }
        // return true if reached here.
        return true;
    }
}
