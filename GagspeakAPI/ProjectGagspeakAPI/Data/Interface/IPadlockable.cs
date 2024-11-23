namespace GagspeakAPI.Data.Interfaces;
public interface IPadlockable
{
    string Padlock { get; set; }
    string Password { get; set; }
    DateTimeOffset Timer { get; set; }
    string Assigner { get; set; }
}
