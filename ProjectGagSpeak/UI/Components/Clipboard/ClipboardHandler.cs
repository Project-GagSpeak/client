using GagSpeak.Utils;
using ImGuiNET;

namespace GagSpeak.UI.Components;

public class ClipboardHandler<T> : IClipboardHandler<T>
{
    private readonly ILogger<ClipboardHandler<T>> _logger;

    public ClipboardHandler(ILogger<ClipboardHandler<T>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Copies a object to the clipboard in a compressed base64 encoded format to hold large data in small quantities.
    /// </summary>
    public void Copy(T item)
    {
        try
        {
            // Serialize the item to JSON
            string json = JsonConvert.SerializeObject(item);
            // Compress the JSON string
            var compressed = json.Compress(6);
            // Encode the compressed string to base64
            string base64Pattern = Convert.ToBase64String(compressed);
            // Copy the base64 string to the clipboard
            ImGui.SetClipboardText(base64Pattern);
            _logger.LogInformation("Data copied to clipboard.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to copy data to clipboard: {ex.Message}");
        }
    }

    /// <summary>
    /// De-serializes copied content to clipboard.
    /// <para>
    /// THE RESULT DOES NOT ENSURE UNIQUENESS. YOU MUST DO THIS IN THE ON_ADD_ACTION!!
    /// </para>
    /// </summary>
    public void Import(Action<T> onAddAction)
    {
        try
        {
            // Get the base64 string from the clipboard
            string base64 = ImGui.GetClipboardText();
            // Decode the base64 string to bytes
            var bytes = Convert.FromBase64String(base64);
            // Decompress the bytes to a JSON string
            var version = bytes[0];
            version = bytes.DecompressToString(out var decompressed);
            // Deserialize the JSON string back to the object
            T item = JsonConvert.DeserializeObject<T>(decompressed) ?? throw new Exception("Failed to deserialize data from clipboard.");

            // Perform the custom action with the de-serialized item
            onAddAction(item);

            _logger.LogInformation("Data imported from clipboard.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not import data from clipboard: {ex.Message}");
            throw;
        }
    }
}
