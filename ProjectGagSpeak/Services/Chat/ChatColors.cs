using CkCommons.Gui;
using GagSpeak.PlayerClient;
using GagspeakAPI.User;

namespace GagSpeak.Services;

// Holds data about a users display colors so they stay the same across all chatlogs.
// Internal to prevent exploitation of modifying this data rapidly.
public class ChatColors : IReadOnlyDictionary<string, uint>
{
    private readonly ChatConfig _chatConfig;

    private readonly Dictionary<string, uint> _userColors = new(StringComparer.OrdinalIgnoreCase);
    public ChatColors(ChatConfig chatConfig)
    {
        _chatConfig = chatConfig;
    }

    public bool ContainsKey(string uid)
        => _userColors.ContainsKey(uid);

    public bool TryGetValue(string uid, out uint color)
        => _userColors.TryGetValue(uid, out color);

    public uint GetOrCreateValue(string uid)
        => GetOrCreateValue(new UserData(uid));

    public uint GetOrCreateValue(UserData sender)
    {
        if (_userColors.TryGetValue(sender.UID, out var col))
            return col;

        // If they have a defined color, store that.
        if (sender.Color.HasValue)
        {
            _userColors[sender.UID] = CkGui.ApplyAlpha(sender.Color.Value, 1.0f);
            return _userColors[sender.UID];
        }

        // Otherwise, generate the value.
        Vector4 color;
        float brightness;
        do
        {
            var r = (float)new Random().NextDouble();
            var g = (float)new Random().NextDouble();
            var b = (float)new Random().NextDouble();
            // Calculate brightness as the average of RGB values
            brightness = (r + g + b) / 3.0f;
            color = new Vector4(r, g, b, 1.0f);

        } while (brightness < 0.55f || _userColors.Values.Contains(color.ToUint())); // Adjust threshold as needed (e.g., 0.7 for lighter colors)
        _userColors[sender.UID] = color.ToUint();
        return _userColors[sender.UID];
    }

    public void UpdateValue(string uid, uint color)
        => _userColors[uid] = color;

    public uint this[string uid] => _userColors[uid];

    public IEnumerable<string> Keys => _userColors.Keys;
    public IEnumerable<uint> Values => _userColors.Values;

    public int Count => _userColors.Count;

    public IEnumerator<KeyValuePair<string, uint>> GetEnumerator()
        => _userColors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public void SetUserColor(UserData user, uint color)
        => _userColors[user.UID] = color;
}
