using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Util;

namespace GagSpeak.PlayerClient;

// Possibly cleanup and merge with kinkster requests once we get stuff with the hub sorted out and the rest cleaned up.
public sealed class OwnGlobals : IDisposable
{
    private readonly ILogger<OwnGlobals> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly KinksterManager _kinksters;

    // Static permissions for globals so it can be accessed from anywhere.
    // However, to modify this, it must be changed via methods.
    private static GlobalPerms? _perms = null;

    public OwnGlobals(ILogger<OwnGlobals> logger, GagspeakMediator mediator, KinksterManager kinkster) 
    {
        _logger = logger;
        _mediator = mediator;
        _kinksters = kinkster;

        Svc.ClientState.Logout += OnLogout;
    }

    public static IReadOnlyGlobalPerms? Perms => _perms;
    public static EmoteState LockedEmoteState => EmoteState.FromString(_perms?.LockedEmoteState ?? string.Empty);

    public void Dispose()
    {
        Svc.ClientState.Logout -= OnLogout;
    }

    /// <summary> Create a mutable clone of the current globals, that is not readonly. </summary>
    /// <remarks> Since it's a record, <c>_perms with {}</c> makes a shallow copy, so original is unaffected. </remarks>
    public static GlobalPerms CurrentPermsWith(Action<GlobalPerms> configure)
    {
        // Shallow copy.
        var copy = (_perms ?? new GlobalPerms()) with { };
        // apply with changes.
        configure(copy);
        // return copy.
        return copy;
    }

    private void OnLogout(int type, int code)
    {
        _logger.LogInformation("Clearing Global Permissions on Logout.");
        ApplyBulkChange(new GlobalPerms());
        _perms = null;
    }

    /// <summary>
    ///     Assumes change was validated through <c>OwnGlobalsListener</c> first. <para />
    ///     Update should only be enacted by the client. <para />
    ///     Updates all values in <see cref="_perms"/> to their new ones.
    /// </summary>
    public void ApplyBulkChange(GlobalPerms newGlobals)
    {
        var prevGlobals = _perms;
        _perms = newGlobals;
        _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.BulkUpdate, "Global Permissions Updated in Bulk")));
    }

    /// <summary>
    ///     Assumes change was validated through <c>OwnGlobalsListener</c> first. <para />
    ///     Updates both changes in <see cref="_perms"/> to their new ones.
    /// </summary>
    public void DoubleGlobalPermChange(UserData enactor, KeyValuePair<string, object> newPerm1, KeyValuePair<string, object> newPerm2)
    {
        UpdatePermissionValue(enactor, newPerm1.Key, newPerm1.Value);
        UpdatePermissionValue(enactor, newPerm2.Key, newPerm2.Value);
    }

    public void UpdatePermissionValue(UserData enactor, string permName, object newValue)
    {
        if (string.Equals(enactor.UID, MainHub.UID))
            PerformPermissionChange(enactor, permName, newValue);
        else if (_kinksters.TryGetKinkster(enactor, out var kinkster))
            PerformPermissionChange(enactor, permName, newValue, kinkster);
        else
            throw new Exception($"Change not from self, and [{enactor.AliasOrUID}] is not a Kinkster Pair. Invalid change for [{permName}]!");
    }

    private void PerformPermissionChange(UserData enactor, string permName, object newValue, Kinkster? pair = null)
    {
        // Attempt to set the property. if this fails, which it never should if validated previously, throw an exception.
        if (!PropertyChanger.TrySetProperty(_perms, permName, newValue, out var _))
            throw new InvalidOperationException($"Failed to set property [{permName}] to [{newValue}] on Global Permissions.");
        // Then perform the log.
        SendActionEventMessage(pair?.GetNickAliasOrUid() ?? "Self-Update", enactor.UID, $"[{permName}] changed to [{newValue}]");
    }

    private void SendActionEventMessage(string applierNick, string applierUid, string message)
        => _mediator.Publish(new EventMessage(new(applierNick, applierUid, InteractionType.ForcedPermChange, message)));
}
