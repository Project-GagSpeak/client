namespace GagspeakAPI.Data.Struct;
public struct CustomizeProfile
{
    public Guid ProfileGuid { get; set; } = Guid.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public CustomizeProfile(Guid uniqueId, string profileName)
    {
        ProfileGuid = uniqueId;
        ProfileName = profileName;
    }
}
