using GagSpeak.Kinksters;

namespace GagSpeak.Services.Mediator;

// Draw Systems
public record DDSUpdateRequests : MessageBase;
public record DDSUpdateKinkster : MessageBase;
public record DDSUpdateNearby : MessageBase;

public record FolderUpdatePuppeteers : MessageBase;
public record FolderUpdateKinksterAliases(Kinkster Kinkster) : MessageBase;
public record FolderUpdateMarionettes : MessageBase;

public record DTRRefreshMessage : MessageBase;

// Unsure should maybe remove? Idk
public record SelectedModContainerChanged : MessageBase;
