using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using GagspeakAPI.User;

namespace GagSpeak.Services;

public class ProfileFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;

    public ProfileFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
    }

    // For placeholder profiles.
    public UserKinkPlate CreateKinkplate(UserData user)
        => new(user, _loggerFactory.CreateLogger<UserKinkPlate>(), _mediator);

    // For real profiles.
    public UserKinkPlate CreateKinkplate(UserData user, KinkPlateContent info, string images)
        => new(user, info, images, _loggerFactory.CreateLogger<UserKinkPlate>(), _mediator);
}
