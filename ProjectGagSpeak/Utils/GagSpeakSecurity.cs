using CkCommons;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System.Security.Cryptography;

namespace GagSpeak.Utils;

public static class GagSpeakSecurity
{
    // i think they are just using the player hash to make it more less likely to have the same identifier occur? If so, all of this is useless.
    private static readonly Dictionary<(string, ushort), string> _hashListPlayersSHA256 = new();
    private static readonly Dictionary<string, string> _hashListSHA256 = new(StringComparer.Ordinal);
    private static readonly SHA256 _sha256CryptoProvider = SHA256.Create();

    /// <summary>
    ///     Only call this when the ptr is visible.
    /// </summary>
    public unsafe static string GetIdentHashByCharacterPtr(nint address)
        => ((Character*)address)->ContentId.ToString().GetHash256();

    /// <summary>
    ///     Only call this when you are visible.
    /// </summary>
    public unsafe static string GetClientIdentHashThreadSafe()
        => Control.Instance()->LocalPlayer->ContentId.ToString().GetHash256();

    /// <summary>
    ///     Only call this when you are visible.
    /// </summary>
    public static async Task<string> GetClientIdentHash()
        => await Svc.Framework.RunOnFrameworkThread(() => PlayerData.CID.ToString().GetHash256());

    /// <summary>
    ///     for right now i wont be encrypting in and out the secret keys but later on i can keep this here and just add it back in. 
    /// </summary>
    public static string GetHash256(this string stringToHash)
        => GetOrComputeHashSHA256(stringToHash);

    private static string GetOrComputeHashSHA256(string stringToCompute)
    {
        if (_hashListSHA256.TryGetValue(stringToCompute, out var hash))
            return hash;

        return _hashListSHA256[stringToCompute] =
            BitConverter.ToString(_sha256CryptoProvider.ComputeHash(Encoding.UTF8.GetBytes(stringToCompute))).Replace("-", "", StringComparison.Ordinal);
    }
}
#pragma warning restore SYSLIB0021 // Type or member is obsolete

