using MessagePack;

namespace GagspeakAPI.Data;

/// <summary>
/// Stores a list of alias triggers. This is intended to be applied once for each player in your whitelist.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record AliasTrigger
{
    /// <summary> If this alias trigger is enabled </summary>
    public bool Enabled { get; set; } = false;

    /// <summary> The input command that triggers the output command </summary>
    public string InputCommand { get; set; } = string.Empty;

    /// <summary> The output command that is triggered by the input command </summary>
    public string OutputCommand { get; set; } = string.Empty;
}
