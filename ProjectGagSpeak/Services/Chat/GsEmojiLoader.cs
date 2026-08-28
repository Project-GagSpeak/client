using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.RichText;
using CkCommons.RichText.Emoji;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Textures;

namespace GagSpeak.Services;

public class GsEmojiLoader : EmojiLoader
{
    private readonly ILogger<GsEmojiLoader> _logger;
    public GsEmojiLoader(ILogger<GsEmojiLoader> logger)
        : base(new SimpleThreadPool())
    {
        _logger = logger;
        // Load GagSpeak Emoji Pack.
        LoadGagSpeakEmojis();
        // Init it into the richtext system.
        NewRichText.ShowEmojis = true;
        NewRichText.EmojiLoader = this;
    }

    public override void DrawEmoji(string emojiName, Vector2 size)
    {
        var imageFile = GetEmojiOrDefault(emojiName);
        // See if the image exists in the cache and has successfully loaded.
        if (imageFile?.GetWrapOrDefault() is { } wrap)
        {
            ImGui.Image(wrap.Handle, size);
            if (ImGui.IsItemHovered())
            {
                using (ImRaii.Tooltip())
                {
                    ImGui.TextUnformatted($":{emojiName}:");
                    ImGui.Image(wrap.Handle, size * 1.75f);
                }
            }
            return;
        }

        // Otherwise determine if we are actively waiting for the image.
        var isLoading = (imageFile != null);
        if (isLoading)
        {
            // Draw the loading GIF while actively fetching or decoding.
            if (CosmeticService.Loading.GetWrapOrDefault() is { } loading)
                ImGui.Image(loading.Handle, size);
            else
                ImGui.Dummy(size);
            CkGui.AttachTooltip("Loading..");
        }
        else
        {
            if (CosmeticService.Error.GetWrapOrDefault() is { } error)
                ImGui.Image(error.Handle, size);
            else
                ImGui.Dummy(size);
            CkGui.AttachTooltip($"Error Loading :{emojiName}:");
        }
    }

    private void LoadGagSpeakEmojis()
    {
        _logger.LogInformation($"Loading default emojis", LoggerType.Textures);
        try
        {
            var defaultEmojiFolder = Path.Combine(GsFiles.AssemblyDirectory, "Assets", "Emotes");
            // Preferably balance these out.
            foreach(var file in Directory.GetFiles(defaultEmojiFolder))
            {
                // Only use when debugging.
                _logger.LogTrace($"Loading default emoji {file} exists={File.Exists(file)}", LoggerType.Textures);
                _cache[Path.GetFileNameWithoutExtension(file)] = new(_pool, file);
            }
        }
        catch(Exception e)
        {
            _logger.LogError($"Error loading default emoji: {e}");
        }
    }
}
