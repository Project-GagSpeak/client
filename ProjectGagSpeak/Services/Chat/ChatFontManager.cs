using Dalamud.Interface.ManagedFontAtlas;
using GagSpeak.PlayerClient;

// Took inspiration from XIMessangers font manager here to avoid redesigning my CkRichText to account for SetWindowFontSize scaling.
namespace GagSpeak.Services;

// Make hosted service? Idk.
public class ChatFontManager : IDisposable
{
    private readonly ILogger<ChatFontManager> _logger;
    private readonly ChatConfig _config;

    private IFontHandle? _fontHandle;
    private bool _fontPushed = false;

    public ChatFontManager(ILogger<ChatFontManager> logger, ChatConfig chatConfig)
    {
        _logger = logger;
        _config = chatConfig;
        BuildFontHandle();
    }

    public bool IsAvailable => _fontHandle?.Available ?? false;

    public void Dispose()
    {
        _fontHandle?.Dispose();
        _fontHandle = null;
        _config.Save();
    }

    /// <summary>
    ///   Safely reloads the font handle. Used when the FontSpec changes in the UI.
    ///   Avoids needing to recreate the entire FontManager instance.
    /// </summary>
    public void ReloadFont()
    {
        if (_fontPushed)
        {
            _logger.LogError("Cannot reload font while it is currently pushed to ImGui!");
            return;
        }

        _fontHandle?.Dispose();
        _fontHandle = null;

        BuildFontHandle();
    }

    private void BuildFontHandle()
    {
        if (!_config.Data.UseCustomChatFont || _config.Data.ChatFont is null)
            return;

        try
        {
            _fontHandle = _config.Data.ChatFont.CreateFontHandle(Svc.PluginInterface.UiBuilder.FontAtlas);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to create font handle: {ex}");
        }
    }

    /// <summary>
    ///   Pushes the font manually. <b>Must be paired with a Pop().</b>
    /// </summary>
    public void PushFont()
    {
        if (_fontPushed)
        {
            _logger.LogError($"Cannot push the font while already pushed!");
            throw new InvalidOperationException("Font is already pushed.");
        }

        if (_config.Data.UseCustomChatFont)
        {
            if (_fontHandle is not null && _fontHandle.Available)
            {
                _fontHandle.Push();
                _fontPushed = true;
            }
        }
    }

    public void PopFont()
    {
        if (_fontPushed)
        {
            _fontHandle?.Pop();
            _fontPushed = false;
        }
    }
}
