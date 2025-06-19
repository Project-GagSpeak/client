using OtterGui.Classes;

namespace GagSpeak.CkCommons.Classes;

/// <summary> Lazy Comma Separated Value Cache
/// <para> Stores a comma separated string as a list of strings for ImGui performance efficiency. </para> 
/// </summary>
/// <remarks> Any successful changes invoke an action returning the new string after the change. </remarks>
public class LazyCSVCache : LazyList<string>
{
    private string _latestString = string.Empty;
    private Func<string> generator;

    public LazyCSVCache(Func<string> commaSepString)
        : base(() => commaSepString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray())
    {
        generator = commaSepString;
        _latestString = commaSepString();
    }

    public string CurrentCache => _latestString;

    /// <summary> Updates the lazy cache to latest values if the latestString is not up to date. </summary>
    public void Sync()
    {
        if (generator() != _latestString)
        {
            _latestString = generator();
            ClearList();
        }
    }


    /// <summary> Renames a phrase at a given index </summary>
    /// <param name="index"> The index of the phrase to rename </param>
    /// <param name="newValue"> The new value for the phrase </param>
    public void Rename(int index, string newValue)
    {
        if (0 <= index && index < Count)
        {
            Svc.Logger.Debug($"Renaming '{this[index]}' to '{newValue}' in LazySplitString");
            var trimmed = newValue.Trim();
            if (this[index] != trimmed)
            {
                var output = this.ToList();
                output[index] = trimmed;
                output.Sort();
                var newString = string.Join(", ", output);
                StringChanged?.Invoke(newString);
            }
        }
    }

    /// <summary> Removes a phrase at a given index </summary>
    /// <param name="index"> The index of the phrase to remove </param>
    public void Remove(int index)
    {
        if (0 <= index && index < Count)
        {
            Svc.Logger.Debug($"Removing '{this[index]}' from LazySplitString");
            var output = this.ToList();
            output.RemoveAt(index);
            output.Sort();
            var newString = string.Join(", ", output);
            StringChanged?.Invoke(newString);
        }
    }

    /// <summary> Adds a new phrase to the list </summary>
    /// <param name="value"> The new phrase to add </param>
    public void Add(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return;

        Svc.Logger.Debug($"Adding '{trimmed}' to LazySplitString");
        var output = this.ToList();
        output.Add(trimmed);
        output.Sort();
        var newString = string.Join(", ", output);
        StringChanged?.Invoke(newString);
    }

    public event Action<string>? StringChanged;
}
