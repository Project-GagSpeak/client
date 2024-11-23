using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Utils;
using System.Text.RegularExpressions;

namespace GagSpeak.Services.Migrations;

// handles the global chat and pattern discovery social features.
public class MigratePatterns
{
    private readonly ILogger<MigratePatterns> _logger;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly string _oldPatternStorageDirectory;
    public MigratePatterns(ILogger<MigratePatterns> logger,
        ClientConfigurationManager clientConfigs, string configDirectory)
    {
        _logger = logger;
        _clientConfigs = clientConfigs;
        _oldPatternStorageDirectory = Path.Combine(configDirectory, "..", "GagSpeak", "PatternStorage.json");
    }

    public bool OldPatternsLoaded { get; private set; } = false;
    public OldPatternStorage OldPatternStorage { get; private set; } = new OldPatternStorage();
    public int SelectedPatternIdx = 0;
    public OldPatternData TmpOldPatternData => OldPatternStorage.PatternList[SelectedPatternIdx];

    public void LoadOldPatterns()
    {
        var oldPatternStorage = new OldPatternStorage();

        if (!File.Exists(_oldPatternStorageDirectory))
        {
            _logger.LogWarning($"Old pattern file not found at {_oldPatternStorageDirectory}");
            OldPatternStorage = oldPatternStorage;
            return;
        }

        try
        {
            var text = File.ReadAllText(_oldPatternStorageDirectory);
            var jsonObject = JObject.Parse(text);
            var patternsArray = jsonObject["Pattern List"]?.Value<JArray>();

            if (patternsArray != null)
            {
                foreach (var item in patternsArray)
                {
                    var pattern = new OldPatternData
                    {
                        Name = item["Name"]?.Value<string>() ?? string.Empty,
                        Description = item["Description"]?.Value<string>() ?? string.Empty,
                        Duration = item["Duration"]?.Value<string>() ?? string.Empty,
                        Selected = item["Selected"]?.Value<bool>() ?? false,
                        IsActive = item["IsActive"]?.Value<bool>() ?? false,
                        Loop = item["Loop"]?.Value<bool>() ?? false,
                        PatternData = item["PatternData"]?.Value<string>()?.Split(',')
                            .Select(byte.Parse)
                            .ToList() ?? new List<byte> { 0 }
                    };
                    oldPatternStorage.PatternList.Add(pattern);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading old patterns: {ex}");
        }
        OldPatternsLoaded = true;
        OldPatternStorage = oldPatternStorage;
        return;
    }

    // append a new function that takes a old pattern at a spesified index, and constructs a new patternData object from it.
    public void AppendOldPatternToPatternStorage(int index)
    {
        // fetch the pattern from the old pattern storage at the indx.
        OldPatternData oldPattern = OldPatternStorage.PatternList[index];

        // check to see if the old pattern duration included d, h, m, or s in it.
        if (oldPattern.Duration.Contains("d") || oldPattern.Duration.Contains("h") ||
            oldPattern.Duration.Contains("m") || oldPattern.Duration.Contains("s"))
        {
            var regex = new Regex(@"(?:(\d+)d)?(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s)?");
            var match = regex.Match(oldPattern.Duration);

            if (match.Success)
            {
                int days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
                int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                int seconds = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
                var correctedTimespan = new TimeSpan(days, hours, minutes, seconds);


                oldPattern.Duration = hours > 0
                    ? correctedTimespan.ToString("hh\\:mm\\:ss")
                    : correctedTimespan.ToString("mm\\:ss");
            }
        }


        PatternData newPatternToAdd = new PatternData
        {
            Name = oldPattern.Name,
            Description = oldPattern.Description,
            Author = "(Migrated Pattern)",
            Tags = new List<string>(),
            Duration = oldPattern.Duration.GetTimespanFromTimespanString(),
            StartPoint = TimeSpan.Zero,
            PlaybackDuration = oldPattern.Duration.GetTimespanFromTimespanString(),
            IsActive = false,
            ShouldLoop = oldPattern.Loop,
            PatternByteData = oldPattern.PatternData
        };

        // adjust the name and author based on the name
        if (newPatternToAdd.Name.Contains("[C.K.]"))
        {
            newPatternToAdd.Name = newPatternToAdd.Name.Replace("[C.K.] ", "");
            newPatternToAdd.Author = "C.K.";
        }
        if (newPatternToAdd.Name.Contains("[CK]"))
        {
            newPatternToAdd.Name = newPatternToAdd.Name.Replace("[CK] ", "");
            newPatternToAdd.Author = "C.K.";
        }

        if (newPatternToAdd.Name.Contains("[Base Pattern]"))
        {
            newPatternToAdd.Name = newPatternToAdd.Name.Replace("[Base Pattern] ", "");
            newPatternToAdd.Author = "Base Pattern";
        }

        // append the new Pattern data
        _clientConfigs.AddNewPattern(newPatternToAdd);
    }

    public void AppendAllOldPatternsToPatternStorage()
    {
        var newPatternList = new List<PatternData>();
        foreach (var oldPattern in OldPatternStorage.PatternList)
        {
            if (oldPattern.Duration.Contains("d") || oldPattern.Duration.Contains("h") ||
                oldPattern.Duration.Contains("m") || oldPattern.Duration.Contains("s"))
            {
                var regex = new Regex(@"(?:(\d+)d)?(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s)?");
                var match = regex.Match(oldPattern.Duration);

                if (match.Success)
                {
                    int days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
                    int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                    int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                    int seconds = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
                    var correctedTimespan = new TimeSpan(days, hours, minutes, seconds);


                    oldPattern.Duration = hours > 0
                        ? correctedTimespan.ToString("hh\\:mm\\:ss")
                        : correctedTimespan.ToString("mm\\:ss");
                }
            }

            var newPatternToAdd = new PatternData
            {
                Name = oldPattern.Name,
                Description = oldPattern.Description,
                Author = "(Migrated Pattern)",
                Tags = new List<string>(),
                Duration = oldPattern.Duration.GetTimespanFromTimespanString(),
                StartPoint = TimeSpan.Zero,
                PlaybackDuration = oldPattern.Duration.GetTimespanFromTimespanString(),
                IsActive = false,
                ShouldLoop = oldPattern.Loop,
                PatternByteData = oldPattern.PatternData
            };

            // adjust the name and author based on the name
            if (newPatternToAdd.Name.Contains("[C.K.]"))
            {
                newPatternToAdd.Name = newPatternToAdd.Name.Replace("[C.K.] ", "");
                newPatternToAdd.Author = "C.K.";
            }
            if (newPatternToAdd.Name.Contains("[CK]"))
            {
                newPatternToAdd.Name = newPatternToAdd.Name.Replace("[CK] ", "");
                newPatternToAdd.Author = "C.K.";
            }

            if (newPatternToAdd.Name.Contains("[Base Pattern]"))
            {
                newPatternToAdd.Name = newPatternToAdd.Name.Replace("[Base Pattern] ", "");
                newPatternToAdd.Author = "Base Pattern";
            }

            newPatternList.Add(newPatternToAdd);
        }
        _clientConfigs.AddNewPatterns(newPatternList);
    }
}


// For storing the imported old data.
public class OldPatternStorage
{
    public List<OldPatternData> PatternList { get; set; } = new List<OldPatternData>();
}

public class OldPatternData
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Duration { get; set; }
    public bool Selected { get; set; }
    public bool IsActive { get; set; }
    public bool Loop { get; set; }
    public List<byte> PatternData { get; set; }
}

