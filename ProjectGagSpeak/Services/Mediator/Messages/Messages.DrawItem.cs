using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;

namespace GagSpeak.Services.Mediator;

// Draw Systems
public record FolderUpdateKinkster : MessageBase;
public record FolderUpdateRequests : MessageBase;
public record FolderUpdatePuppeteers : MessageBase;
public record FolderUpdateKinksterAliases(Kinkster Kinkster) : MessageBase;
public record FolderUpdateMarionettes : MessageBase;

public record FavoritesChanged(FavoriteIdContainer Container) : MessageBase;


// Unsure should maybe remove? Idk
public record SelectedModContainerChanged : MessageBase;
