using System.Security.Cryptography;

namespace GagSpeak.WebAPI.Utils;
#pragma warning disable SYSLIB0021 // Type or member is obsolete

/// <summary>
/// Yoinked once again with little to no modification. Maybe tinker and see how well functions work without this?
/// </summary>
public static class ComputeHashed
{
    // i think they are just using the player hash to make it more less likely to have the same identifier occur? If so, all of this is useless.
    private static readonly Dictionary<(string, ushort), string> _hashListPlayersSHA256 = new();
    private static readonly Dictionary<string, string> _hashListSHA256 = new(StringComparer.Ordinal);
    private static readonly SHA256CryptoServiceProvider _sha256CryptoProvider = new();

    // dont even ask me what any of these do, its just black magic encryption bullshit.
    public static string GetHash256(this (string, ushort) playerToHash)
    {
        if (_hashListPlayersSHA256.TryGetValue(playerToHash, out var hash))
            return hash;

        return _hashListPlayersSHA256[playerToHash] =
            BitConverter.ToString(_sha256CryptoProvider.ComputeHash(Encoding.UTF8.GetBytes(playerToHash.Item1 + playerToHash.Item2.ToString()))).Replace("-", "", StringComparison.Ordinal);
    }

    // for right now i wont be encrypting in and out the secret keys but later on i can keep this here and just add it back in. 
    public static string GetHash256(this string stringToHash)
    {
        return GetOrComputeHashSHA256(stringToHash);
    }

    private static string GetOrComputeHashSHA256(string stringToCompute)
    {
        if (_hashListSHA256.TryGetValue(stringToCompute, out var hash))
            return hash;

        return _hashListSHA256[stringToCompute] =
            BitConverter.ToString(_sha256CryptoProvider.ComputeHash(Encoding.UTF8.GetBytes(stringToCompute))).Replace("-", "", StringComparison.Ordinal);
    }
}
#pragma warning restore SYSLIB0021 // Type or member is obsolete

