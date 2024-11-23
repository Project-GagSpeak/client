namespace GagspeakAPI.Routes;

public class GagspeakAuth
{
    public const string Auth = "/auth"; // the authentication path.
    public const string Auth_CreateIdent = "createWithIdent"; // creating a new identity for the user authentication
    public const string Auth_RenewToken = "renewToken"; // renewing the token for the user authentication
    public const string Auth_TempToken = "createTemporaryToken"; // creating a temporary token for the user authentication
    public static Uri AuthFullPath(Uri baseUri) => new Uri(baseUri, Auth + "/" + Auth_CreateIdent);
    public static Uri RenewTokenFullPath(Uri baseUri) => new Uri(baseUri, Auth + "/" + Auth_RenewToken);
    public static Uri TempTokenFullPath(Uri baseUri) => new Uri(baseUri, Auth + "/" + Auth_TempToken);
}
