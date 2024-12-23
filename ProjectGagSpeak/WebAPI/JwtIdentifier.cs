namespace GagSpeak.WebAPI;

/// <summary> The record for a JWT Identifier
/// <para>The ToString method will conjoin the three inputs together with identifiers, separated by commas</para>
/// </summary>
/// <param name="ApiUrl">The API Url of the server connected</param>
/// <param name="CharaHash">The hash for the current character</param>
public abstract record JwtIdentifier(string ApiUrl, string CharaHash)
{
    public override string ToString()
    {
        return "{JwtIdentifier; Url: " + ApiUrl + ", Chara: " + CharaHash + "}";
    }
}

/// <summary> The record for a JWT Identifier with a secret key </summary>
/// <param name="ApiUrl"> The API Url of the server connected </param>
/// <param name="CharaHash"> The hash for the current character </param>
/// <param name="SecretKey"> The secret key for the JWT </param>
/// <param name="IsPrimary"> Whether this is the primary Auth account </param>
public record SecretKeyJwtIdentifier(string ApiUrl, string CharaHash, string SecretKey, bool ExpectPrimary) : JwtIdentifier(ApiUrl, CharaHash)
{
    public override string ToString()
    {
        return base.ToString() + ", HasSecretKey: " + !string.IsNullOrEmpty(SecretKey) + ", ExpectPrimary: " + ExpectPrimary + "}";
    }
}

/// <summary> The record for a JWT Identifier with a LocalContentID </summary>
/// <param name="ApiUrl"> The API Url of the server connected </param>
/// <param name="CharaHash"> The hash for the current character </param>
/// <param name="LocalContentID"> The LocalContentID for the current character </param>
public record LocalContentIDJwtIdentifier(string ApiUrl, string CharaHash, string LocalContentID) : JwtIdentifier(ApiUrl, CharaHash)
{
    public override string ToString()
    {
        return base.ToString() + ", LocalContentID: " + LocalContentID + "}";
    }
}
