using GagSpeak.Kinksters;

namespace GagSpeak.Services.Mediator;

// Draw Systems
public record FolderUpdateKinkster : MessageBase;
public record FolderUpdateRequests : MessageBase;
public record FolderUpdatePuppeteers : MessageBase;
public record FolderUpdateKinksterAliases(Kinkster Kinkster) : MessageBase;
public record FolderUpdateMarionettes : MessageBase;


// Unsure should maybe remove? Idk
public record SelectedModContainerChanged : MessageBase;
