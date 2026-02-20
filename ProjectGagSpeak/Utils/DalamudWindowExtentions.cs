using CkCommons.Gui;
using Dalamud.Interface.Windowing;
using GagSpeak.Services.Tutorial;
using static Dalamud.Interface.Windowing.Window;

namespace GagSpeak.Utils;

/// <summary>
///     Reduce the boilerplate code of title bar buttons with a builder.
/// </summary>
public class TitleBarButtonBuilder
{
    // temporary 
    private readonly List<TitleBarButton> _buttons = new();

    public TitleBarButtonBuilder Add(FAI icon, string tooltip, Action onClick)
    {
        _buttons.Add(new TitleBarButton
        {
            Icon = icon,
            Click = _ => onClick(),
            IconOffset = new Vector2(2, 1),
            ShowTooltip = () => CkGui.AttachToolTip(tooltip),
        });
        return this;
    }

    public TitleBarButtonBuilder AddTutorial(TutorialService service, TutorialType type)
        => AddTutorial(service, () => type);

    public TitleBarButtonBuilder AddTutorial(TutorialService service, Func<TutorialType> func)
    {
        _buttons.Add(new TitleBarButton
        {
            Icon = FAI.QuestionCircle,
            Click = (msg) =>
            {
                var type = func.Invoke();
                if (service.IsTutorialActive(type))
                {
                    service.SkipTutorial(type);
                    Svc.Logger.Information($"Skipping {type.ToString()} Tutorial");
                }
                else
                {
                    service.StartTutorial(type);
                    Svc.Logger.Information($"Starting {type.ToString()} Tutorial");
                }
            },
            IconOffset = new(2, 1),
            ShowTooltip = () =>
            {
                var type = func.Invoke();
                CkGui.AttachToolTip(
                    service.IsTutorialActive(type) ? $"Stop {type} Tutorial" : $"Start {type} tutorial.");
            },
        });
        return this;
    }

    public List<TitleBarButton> Build() => _buttons;
}

/// <summary>
///     Extension methods that help simplify Dalamud window 
///     setup and operations, to reduce boilerplate code.
/// </summary>
public static class DalamudWindowExtentions
{
    public static void PinningClickthroughFalse(this Window window)
    {
        window.AllowClickthrough = false;
        window.AllowPinning = false;
    }

    public static void SetBoundaries(this Window window, Vector2 minAndMax)
    {
        window.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = minAndMax,
            MaximumSize = minAndMax
        };
    }

    public static void SetBoundaries(this Window window, Vector2 min, Vector2 max)
    {
        window.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = min,
            MaximumSize = max
        };
    }

    public static void SetCloseState(this Window window, bool allowClose)
    {
        window.ShowCloseButton = allowClose;
        window.RespectCloseHotkey = allowClose;
    }
}
