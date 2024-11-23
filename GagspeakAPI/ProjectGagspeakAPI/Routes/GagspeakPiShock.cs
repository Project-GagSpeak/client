namespace GagspeakAPI.Routes;

public class GagspeakPiShock
{
    public const string BaseUri = "https://do.pishock.com";
    public const string Api = "api";
    public const string PiShock_GetInfo = "GetShockerInfo"; 
    public const string PiShock_Execute = "apioperate";
    public static Uri GetInfoPath() => new Uri(new Uri(BaseUri), Api + "/" + PiShock_GetInfo);
    public static Uri ExecuteOperationPath() => new Uri(new Uri(BaseUri), Api + "/" + PiShock_Execute);
}
