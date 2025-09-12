using GagSpeak.Gui;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Services;

/// <summary> 
///     Tracks the current state that the Thumbnail's UI should reflect.
/// </summary>
public sealed class UiThumbnailService : IDisposable
{
    private readonly ILogger<UiThumbnailService> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;

    private ThumbnailFolder _folder; // nullable.
    public UiThumbnailService(ILogger<UiThumbnailService> logger, GagspeakMediator mediator,
        MainConfig config)
    {
        _logger = logger;
        _mediator = mediator;
        _config = config;
    }
    public string SearchString = string.Empty;
    public ThumbnailFile? Selected = null;

    public Guid SourceId { get; private set; } = Guid.Empty;
    public Vector2 BaseSize { get; private set; } = Vector2.Zero;
    public ImageDataType Kind { get; private set; } = ImageDataType.None;
    public ThumbnailFolder Folder => _folder;

    public Vector2 ItemSize => BaseSize * _config.Current.FileIconScale;
    public Vector2 DispSize => Kind switch
    {
        ImageDataType.Blindfolds or ImageDataType.Hypnosis or ImageDataType.Collar => BaseSize,
        ImageDataType.Restraints or ImageDataType.Restrictions => BaseSize * 2,
        _ => new Vector2(100),
    };

    public void Dispose()
    {
        _folder?.Dispose();
        _folder = null!;
        Selected = null;
    }

    public bool SetThumbnailSource(Guid sourceId, Vector2 baseSize, ImageDataType kind)
    {
        // update the source ID and base size.
        SourceId = sourceId;
        BaseSize = baseSize;
        // if the kinds are different.
        if (Kind != kind)
        {
            // Swap folder types.
            if (_folder is not null)
                _folder.Dispose();

            _folder = new ThumbnailFolder(_logger, kind);
            Selected = null;
            SearchString = string.Empty;
        }
        // update the kind.
        Kind = kind;
        _mediator.Publish(new UiToggleMessage(typeof(ThumbnailUI), ToggleType.Show));
        return true;
    }

    public void ScanFolderFiles()
    {
        if (Folder is null)
            return;
        Folder.ScanFiles();
    }

    public void PickSelectedThumbnail()
    {
        if (Selected is null)
            return;
        _logger.LogInformation($"Selecting thumbnail: {Selected.FileName} in folder: {_folder.FolderName}");
        _mediator.Publish(new ThumbnailImageSelected(SourceId, BaseSize, Kind, Selected.FileName));
        // close the window.
        ClearThumbnailSource();
    }

    public void ClearThumbnailSource()
    {
        _mediator.Publish(new UiToggleMessage(typeof(ThumbnailUI), ToggleType.Hide));
        // clear the source ID, base size, and kind.
        SourceId = Guid.Empty;
        BaseSize = Vector2.Zero;
        Kind = ImageDataType.None;
        // dispose the folder if it exists.
        _folder?.Dispose();
        _folder = null!;
        Selected = null;
        SearchString = string.Empty;
    }
}
