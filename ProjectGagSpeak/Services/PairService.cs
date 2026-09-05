using GagSpeak.Kinksters;
using GagSpeak.Pairs;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Chat;
using GagspeakAPI.User;
using System.Text.RegularExpressions;

namespace GagSpeak.Services;

/// <summary>
///   Utility service to pull information about a pair from respective handlers and managers for UI.
/// </summary>
public class PairService : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly NicksConfig _nicks;
    private readonly ChatConfig _chatConfig;
    private readonly GlobalChatLog _globalChatlog;
    private readonly VisibilityWatcher _visibleUsers;
    private readonly RequestsManager _requests;
    private readonly KinksterManager _kinksters;
    private readonly OnlineKinksterManager _onlineUsers;

    public PairService(ILogger<PairService> logger, GagspeakMediator mediator,
        MainConfig config, NicksConfig nicks, ChatConfig chatConfig,
        GlobalChatLog globalChat, VisibilityWatcher visibleUsers,
        RequestsManager requests, KinksterManager kinksters,
        OnlineKinksterManager onlineUsers)
        : base(logger, mediator)
    {
        _config = config;
        _nicks = nicks;
        _chatConfig = chatConfig;
        _globalChatlog = globalChat;
        _visibleUsers = visibleUsers;
        _requests = requests;
        _kinksters = kinksters;
        _onlineUsers = onlineUsers;

        // Could track offline, but at the moment if the alias is updated mid connection it will be kept in here.
        _onlineUsers.UserWentOnline += OnUserOnline;
        _onlineUsers.UserWentOffline -= OnUserOffline;
        Mediator.Subscribe<DisconnectedMessage>(this, _ => _userByAlias.Clear());
    }

    private readonly Dictionary<string, UserData> _userByAlias = new(StringComparer.Ordinal);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _onlineUsers.UserWentOnline += OnUserOnline;
        _onlineUsers.UserWentOffline -= OnUserOffline;
        _userByAlias.Clear();
    }

    private void OnUserOnline(UserData user, string ident)
    {
        if (user.Alias is not null)
            _userByAlias.TryAdd(user.Alias, user);
    }

    private void OnUserOffline(UserData user, string ident)
    {
        if (user.Alias is not null)
            _userByAlias.Remove(user.Alias, out _);
    }

    public void UpdateVanityData(UserDto newUserDto)
    {
        // Update the userdata in the kinksters.
        _kinksters.UpdateUserVanity(newUserDto);
        // Then grab the existing data for that user.
        if (_onlineUsers.GetMappedUserData(newUserDto.User) is { } prevUser)
        {
            // We have a previous user so try and remove their mapping if valid.
            if (prevUser.Alias is not null)
                _userByAlias.Remove(prevUser.Alias, out _);
            // Set the new data.
            _onlineUsers.UserUpdateVanity(newUserDto.User);
        }
        // Also update it in the mapping.
        if (newUserDto.User.Alias is not null)
            _userByAlias.TryAdd(newUserDto.User.Alias, newUserDto.User);
        Mediator.Publish(new DDSUpdateKinkster());
        Mediator.Publish(new DDSUpdateNearby());
    }

    public bool RequestExistsFor(UserData user)
        => _requests.IsInRequests(user);

    public bool IsDirectPaired(UserData user)
        => _kinksters.Contains(user);

    public bool IsRendered(UserData user)
        => _kinksters.TryGetValue(user, out var k) && k.IsRendered;

    public bool IsRenderedOnline(UserData user)
        => _kinksters.TryGetValue(user, out var k) && k.IsRendered && k.IsOnline;

    public bool IsOnline(UserData user)
        => _onlineUsers.Contains(user);

    #region Value Helpers
    // Maybe make this use the alias mapping if we run into issues.
    public string GetNickAliasOrUid(UserData user)
        => _nicks.TryGetNickname(user.UID, out var n) ? n : user.AliasOrUID;

    public string? GetNickname(UserData user)
        => _nicks.GetNicknameForUid(user.UID);

    internal IntPtr GetAddress(UserData user)
        => _kinksters.GetValueOrDefault(user)?.PlayerAddress ?? IntPtr.Zero;

    public string GetPlayerName(UserData user)
        => _kinksters.GetValueOrDefault(user)?.PlayerName ?? string.Empty;

    // Process through iterations for display name.
    public string GetDisplayName(UserData user)
    {
        // If we want nicks over player names just return that regardless.
        if (_config.Data.UseNicksOverPlayerNames)
            return GetNickAliasOrUid(user);
        // Otherwise Attempt PlayerName
        if (_kinksters.TryGetValue(user, out var k) && k.PlayerName.Length > 0)
            return k.PlayerName;
        // Fallback
        return GetNickAliasOrUid(user);
    }

    public string GetNearbyDisplayName(UserData user)
    {
        // Only draw out display names for direct pairs.
        if (_kinksters.Contains(user))
            return GetDisplayName(user);
        // Otherwise resort to VanityOrAnonName
        return user.VanityOrAnonName;
    }

    public string GetProfileDisplayName(UserData user)
    {
        if (user.UID == MainHub.UID)
            return MainHub.OwnUserData.AliasOrUID;
        if (_kinksters.TryGetValue(user, out var s))
            return s.User.AliasOrUID;
        // Fallback to GlobalChat
        if (_globalChatlog.ChatUsers.TryGetValue(user, out var meta) && meta.Flags.HasAny(ChatFlags.UseDisplayName))
            return user.VanityOrAnonName;
        return user.AnonName;
    }
    #endregion

    #region Filter Matching

    public bool MatchesPairFilter(UserData user, string searchFilter)
        => searchFilter.Length is 0
        || user.AliasOrUID.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)
        || (GetNickname(user)?.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ?? false)
        || (_kinksters.GetValueOrDefault(user)?.PlayerName?.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ?? false);

    // Likely incorrect here.
    public bool MatchesMonoName(UserData user, string printedName)
        => user.AliasOrUID.Equals(printedName, StringComparison.OrdinalIgnoreCase);
    #endregion

    #region Advanced Helpers
    public bool IsValidDMChatUser(UserData user)
    {
        // Check direct pairs first.
        if (_kinksters.Contains(user))
            return true;
        // Fallback to GlobalChat, if we allow DM's and they are a global chat user, accept.
        return _chatConfig.Data.ChatPerms.HasAny(ChatFlags.AllowDirectMessages) && _globalChatlog.ChatUsers.ContainsKey(user);
    }

    public string GetChatNameLabel(UserData user)
    {
        if (_kinksters.TryGetValue(user, out var kinkster))
            return kinkster.GetNickAliasOrUid();
        // Fallback to GlobalChat
        if (_globalChatlog.ChatUsers.TryGetValue(user, out var meta) && meta.Flags.HasAny(ChatFlags.UseDisplayName))
            return user.VanityOrAnonName;
        // Default to Anon if fail.
        return user.AnonName;
    }

    /// <summary>
    ///   Resolves the name of a user for a sent chat command. <br/>
    ///   Can be a bit costly, but should correctly evaluate a name with 
    ///   UID, AnonUser, Vanity, or Alias accounted for, based on access / permissions.
    /// </summary>
    public UserData? ResolveChatName(string chatNameArg, StringComparison comparer = StringComparison.Ordinal)
    {
        // Matches kinkster or kinkster- followed by 3 to 4 characters
        var anonMatch = Regex.Match(chatNameArg, @"(?i)^kinkster-?(.{3,4})$");
        if (anonMatch.Success)
        {
            var anonTag = anonMatch.Groups[1].Value;
            // Search GlobalChat for the tag.
            if (_globalChatlog.ChatUsers.FirstOrDefault(u => string.Equals(u.Key.AnonTag, chatNameArg, comparer) && u.Value.Flags.HasAny(ChatFlags.AllowDirectMessages)) is { } rcUser)
            {
                if (!Equals(rcUser, default(KeyValuePair<UserData, ChatFlags>)))
                    return rcUser.Key;
            }
            return null;
        }

        // Otherwise, attempt to perform O(1) Lookup by UID.
        if (_kinksters.TryGetValue(new(chatNameArg.ToUpperInvariant()), out var directPair))
            return directPair.User;

        // GlobalChat iterates a Dictionary, so check against default after.
        if (_globalChatlog.ChatUsers.FirstOrDefault(u => string.Equals(u.Key.VanityName, chatNameArg, comparer) && u.Value.Flags.HasAll(ChatFlags.UseDisplayName | ChatFlags.AllowDirectMessages)) is { } rcMatch)
            if (!Equals(rcMatch, default(KeyValuePair<UserData, ChatFlags>)))
                return rcMatch.Key;

        // Final fallback, Alias check. Perform a lookup in the alias map, then find by uid.
        if (_userByAlias.TryGetValue(chatNameArg, out var found))
            if (_kinksters.GetValueOrDefault(new(found.UID)) is { } aliasMappedUser)
                return aliasMappedUser.User;
        // We failed all conditions, so abort.
        return null;
    }
    #endregion

}
