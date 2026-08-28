using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using GagSpeak.Services.Tutorial;

namespace GagSpeak.Utils;

/// <summary>
///   Reduce the boilerplate code of title bar buttons with a builder.
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
            ShowTooltip = () => CkGui.AttachTooltip(tooltip),
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
                if (!service.Exists(type))
                {
                    // do nothing, this tutorial doesn't exist just print an error for the devs.
                    Svc.Logger.Error($"!!!NO TUTORIAL OF {type} ADDED TO DICTIONARY!!!");
                    return;
                }

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
                if (!service.Exists(type))
                {
                    // do nothing, this tutorial doesn't exist just print an error for the devs.
                    Svc.Logger.Error($"!!!NO TUTORIAL OF {type} ADDED TO DICTIONARY!!!");
                    return;
                }

                CkGui.AttachTooltip(service.IsTutorialActive(type) ? $"Stop {type} Tutorial" : $"Start {type} tutorial.");
            },
        });
        return this;
    }

    public List<TitleBarButton> Build() => _buttons;
}

/// <summary>
///   Extension methods that help simplify Dalamud window
///   setup and operations, to reduce boilerplate code.
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


    // Code yoinked directly from native ImGui for the custom ResizeGrip rendering.
    public static void RenderCustomResizeGrips(this ImGuiWindowPtr winPtr)
    {
        if ((winPtr.Flags & ImGuiWindowFlags.NoResize) != 0)
            return;

        var border = winPtr.WindowBorderSize;
        var rounding = winPtr.WindowRounding;
        var fontSize = winPtr.CalcFontSize();
        var gripSize = MathF.Floor(MathF.Max(fontSize * 1.1f, rounding + 1f + fontSize * 0.2f));
        var hoveredId = ImGuiP.GetHoveredID();
        var activeId = ImGuiP.GetActiveID();

        for (int i = 0; i < Grips.Length; i++)
        {
            var g = Grips[i];
            var corner = Vector2.Lerp(winPtr.Pos, winPtr.Pos + winPtr.Size, g.CornerPosN);
            var id = ImGuiP.GetWindowResizeCornerID(winPtr, i);
            var hovered = hoveredId == id;
            var active = activeId == id;

            if (!((i == 0) || hovered || active))
                continue;

            var col = hovered
                ? (active
                    ? ImGui.GetColorU32(ImGuiCol.ResizeGripActive)
                    : ImGui.GetColorU32(ImGuiCol.ResizeGripHovered))
                : ImGui.GetColorU32(ImGuiCol.ResizeGrip);

            var flip = (i & 1) != 0;
            var sideA = corner + g.InnerDir * (flip ? new Vector2(border, gripSize) : new Vector2(gripSize, border));
            var sideB = corner + g.InnerDir * (flip ? new Vector2(gripSize, border) : new Vector2(border, gripSize));

            winPtr.DrawList.PathLineTo(sideA);
            winPtr.DrawList.PathLineTo(sideB);
            winPtr.DrawList.PathArcToFast(corner + g.InnerDir * (rounding + border), rounding, g.AngleMin12, g.AngleMax12);
            winPtr.DrawList.PathFillConvex(col);
        }
    }

    // Grips as defined by native ImGui.
    private static readonly ResizeGripDef[] Grips =
    {
        new() { CornerPosN = new(1,1), InnerDir = new(-1,-1), AngleMin12 = 0,  AngleMax12 = 3  }, // BR
        new() { CornerPosN = new(0,1), InnerDir = new(1,-1),  AngleMin12 = 3,  AngleMax12 = 6  }, // BL
        new() { CornerPosN = new(0,0), InnerDir = new(1,1),   AngleMin12 = 6,  AngleMax12 = 9  }, // TL
        new() { CornerPosN = new(1,0), InnerDir = new(-1,1),  AngleMin12 = 9,  AngleMax12 = 12 }  // TR
    };

    private struct ResizeGripDef
    {
        public Vector2 CornerPosN; // (0,0) (1,0) etc
        public Vector2 InnerDir;   // direction into window
        public int AngleMin12;
        public int AngleMax12;
    }
}
