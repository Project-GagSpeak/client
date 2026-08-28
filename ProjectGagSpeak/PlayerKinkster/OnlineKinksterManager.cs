using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Connection;
using GagspeakAPI.Data.Comparer;
using GagspeakAPI.User;
using OtterGui.Text;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Pairs;

public sealed class OnlineKinksterManager : DisposableMediatorSubscriberBase
{
    private readonly Lock _lock = new();

    private readonly ConcurrentDictionary<UserData, string> _onlineUsers = new(UserDataComparer.Instance);
    private readonly Dictionary<string, UserData> _identMap = new(StringComparer.Ordinal);

    /// <summary> Allows us to track how many users are online within a joined sanction. </summary>
    public event Action<UserData, string>? UserWentOnline;
    public event Action<UserData, string>? UserWentOffline;

    public OnlineKinksterManager(ILogger<OnlineKinksterManager> logger, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        Mediator.Subscribe<DisconnectedMessage>(this, _ => ClearAll());
    }

    public int Count => _onlineUsers.Count;
    public IEnumerable<UserData> Users => _onlineUsers.Keys;
    public IReadOnlyDictionary<string, UserData> IdentMap => _identMap;

    public bool Contains(UserData user)
        => _onlineUsers.ContainsKey(user);

    public UserData? GetMappedUserData(UserData user)
        => _onlineUsers.TryGetValue(user, out var ident) ? _identMap[ident] : null;

    public string? GetIdent(UserData user)
        => _onlineUsers.TryGetValue(user, out var ident) ? ident : null;

    public bool TryGetIdent(UserData user, [NotNullWhen(true)] out string? ident)
        => _onlineUsers.TryGetValue(user, out ident);

    public void UserUpdateVanity(UserData newUserData)
    {
        if (!_onlineUsers.TryGetValue(newUserData, out var ident))
            return;

        lock (_lock)
            _identMap[ident] = newUserData;
    }

    #region Add / Remove
    public void AddOnline(List<OnlineKinkster> onlineUsers)
    {
        foreach (var onlineUser in onlineUsers)
            AddOnline(onlineUser.User, onlineUser.Ident);
    }

    public void AddOnline(UserData user, string ident)
    {
        // TryAdd returns true only if the user wasn't already in the dictionary
        if (_onlineUsers.TryAdd(user, ident))
        {
            lock (_lock)
                _identMap[ident] = user;

            Logger.LogDebug($"Added OnlineUser {user.AliasOrUID}", LoggerType.OnlinePairs);
            UserWentOnline?.Invoke(user, ident);
        }
    }

    public void RemoveOnline(IEnumerable<UserData> users)
    {
        foreach (var user in users)
            RemoveOnline(user);
    }

    public void RemoveOnline(UserData user)
    {
        // TryRemove returns true only if the user was actually removed
        if (_onlineUsers.TryRemove(user, out var ident))
        {
            RemoveIdent(ident, out var removed);
            Logger.LogDebug($"Removed OnlineUser {user.AliasOrUID}", LoggerType.OnlinePairs);
            UserWentOffline?.Invoke(user, ident);
        }
    }

    public bool RemoveIdent(string ident, [NotNullWhen(true)] out UserData? removed)
    {
        lock (_lock)
            return _identMap.Remove(ident, out removed);
    }

    public void ClearAll()
    {
        // Snapshot the current users so we can fire the offline events
        var offlineUsers = _onlineUsers.ToArray();

        _onlineUsers.Clear();
        lock (_lock)
            _identMap.Clear();

        foreach (var (user, ident) in offlineUsers)
            UserWentOffline?.Invoke(user, ident);
    }
    #endregion

    #region Debug
    public void DebugUsers()
    {
        ImUtf8.Text("Total Online:");
        CkGui.ColorTextInline($"{_onlineUsers.Count}", ImGuiColors.DalamudViolet);

        using var _ = ImRaii.Table("online-users-table", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!_) return;

        ImGui.TableSetupColumn("User");
        ImGui.TableSetupColumn("Ident");
        ImGui.TableHeadersRow();

        // Snapshot to array for thread-safe iteration
        foreach (var kvp in _onlineUsers.ToArray())
        {
            ImGui.TableNextColumn();
            CkGui.ColorText(kvp.Key.AliasOrUID, ImGuiColors.ParsedBlue);

            ImGui.TableNextColumn();
            ImGui.Text(kvp.Value);
            ImGui.TableNextRow();
        }
    }
    #endregion
}
