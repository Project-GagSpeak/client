using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Text;
using System.Runtime.CompilerServices;
using static OtterGui.Raii.ImRaii;

// A Modified take on OtterGui.Widgets.Tutorial.
// This iteration removes redundant buttons, adds detailed text, and sections.
namespace GagSpeak.Services.Tutorial;

public record struct Step(string Name, string Info, string MoreInfo, bool Enabled);

public class Tutorial
{
    private readonly List<Step> _steps = new();

    private static Vector2 _closeSize = new Vector2(20f * ImGuiHelpers.GlobalScale);
    private string  _popupLabel;
    private int     _waitFrames     = 0;
    private bool    _showDetails    = false;
    private bool    _hoveringExit   = false;

    public Tutorial(string label)
    {
        _popupLabel = label;
    }

    public GuideCache Cache { get; private set; } = new GuideCache();

    public int EndStep => _steps.Count;
    public IReadOnlyList<Step> Steps => _steps;

    public Tutorial WithCache(GuideCache cache)
    {
        Cache = cache;
        return this;
    }

    public Tutorial AddStep(string name, string info, string moreInfo)
    {
        _steps.Add(new Step(name, info, moreInfo, true));
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Open(int step, Vector2 parentWinPos, Vector2 parentWinSize, Action<GuideCache>? onNext = null)
    {
        if (step != Cache.CurrentStep)
            return;

        OpenWhenMatch(parentWinPos, parentWinSize, onNext);
        --_waitFrames;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Skip()
        => Cache.CurrentStep = NextId();

    private void OpenWhenMatch(Vector2 parentWinPos, Vector2 parentWinSize, Action<GuideCache>? onNext = null)
    {
        var step = Steps[Cache.CurrentStep];

        // Skip disabled tutorials.
        if (!step.Enabled)
        {
            Cache.CurrentStep = NextId();
            return;
        }

        if (_waitFrames > 0)
            --_waitFrames;
        else if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsPopupOpen(_popupLabel))
            ImGui.OpenPopup(_popupLabel);
        
        HighlightObject();
        var popupPos = new Vector2(parentWinPos.X + parentWinSize.X, parentWinPos.Y); // Position the popup to the right of the window
        DrawPopup(popupPos, step, NextId(), onNext);
    }


    /// <summary>
    /// Appends to the DrawList foreground a rectangle surrounding the last drawn item.
    /// </summary>
    private Vector2 HighlightObject()
    {
        ImGui.SetScrollHereY();
        var offset = ImGuiHelpers.ScaledVector2(5, 4);
        var min = ImGui.GetItemRectMin() - offset;
        var max = ImGui.GetItemRectMax() + offset;
        ImGui.GetForegroundDrawList().PushClipRect(ImGui.GetWindowPos() - offset, ImGui.GetWindowPos() + ImGui.GetWindowSize() + offset);
        ImGui.GetForegroundDrawList().AddRect(min, max, Cache.HighlightColor, 5 * ImGuiHelpers.GlobalScale, ImDrawFlags.RoundCornersAll,
            2 * ImGuiHelpers.GlobalScale);
        ImGui.GetForegroundDrawList().PopClipRect();
        return max + new Vector2(ImGuiHelpers.GlobalScale);
    }

    /// <summary>
    /// The tutorial display.
    /// </summary>
    private void DrawPopup(Vector2 pos, Step step, int nextStepVal, Action<GuideCache>? onNext = null)
    {
        using var style = DefaultStyle()
            .Push(ImGuiStyleVar.WindowPadding, Vector2.One*8 * ImGuiHelpers.GlobalScale)
            .Push(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale)
            .Push(ImGuiStyleVar.PopupRounding, 5 * ImGuiHelpers.GlobalScale);
        using var color = DefaultColors()
            .Push(ImGuiCol.Border, Cache.BorderColor)
            .Push(ImGuiCol.PopupBg, 0xFF000000)
            .Push(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0)));
        using var font = DefaultFont();
        // Prevent the window from opening outside of the screen.
        var size = ImGuiHelpers.ScaledVector2(350, 0);
        var diff = ImGui.GetWindowSize().X - size.X;

        ImGui.SetNextWindowPos(pos);
        ImGui.SetNextWindowSize(size);
        ImGui.SetNextWindowFocus();
        using var popup = Popup(_popupLabel, WFlags.AlwaysAutoResize);
        if (!popup)
            return;

        // grab the window position and size.
        var windowPos = ImGui.GetCursorScreenPos();
        var windowSize = ImGui.GetContentRegionAvail();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5f);
        CkGui.FontText(step.Name, Fonts.GagspeakLabelFont);
        int? nextValue = null;

        ImGui.Separator();
        ImGui.PushTextWrapPos();
        foreach (var text in step.Info.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (text.Length == 0)
                ImGui.Spacing();
            else
                ImGui.Text(text);
        }
        ImGui.PopTextWrapPos();

        if (_showDetails)
        {
            ImGui.Spacing();
            ImGui.PushTextWrapPos();
            foreach (var text in step.MoreInfo.Split('\n', StringSplitOptions.TrimEntries))
            {
                if (text.Length == 0)
                    ImGui.Spacing();
                else
                    CkGui.ColorText(text, ImGuiColors.DalamudGrey);
            }
            ImGui.PopTextWrapPos();
        }

        // Draw out the button row.
        ImGui.Spacing();
        using (ImRaii.Group())
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(_showDetails ? ImGuiColors.DalamudViolet : ImGuiColors.DalamudWhite)))
            {
                if(CkGui.IconButton(FAI.InfoCircle))
                    _showDetails = !_showDetails;
                ImGuiUtil.HoverTooltip("Shows additional details about each step for anyone curious.");
            }

            ImUtf8.SameLineInner();
            if(CkGui.IconButton(FAI.ArrowAltCircleRight))
                nextValue = nextStepVal;
            ImGuiUtil.HoverTooltip("Go to the next Step.");

            CkGui.ColorTextFrameAlignedInline(_popupLabel + " ("+(Cache.CurrentStep + 1)+"/"+EndStep+")", ImGuiColors.ParsedPink);
        }

        var buttonPos = windowPos + new Vector2(windowSize.X - _closeSize.X - ImGui.GetStyle().ItemInnerSpacing.X, ImGui.GetStyle().ItemSpacing.Y);
        if (CloseButton(buttonPos))
            nextValue = -1;

        if (nextValue != null)
        {
            var prevStep = Cache.CurrentStep;
            Cache.CurrentStep = nextValue.Value;
            onNext?.Invoke(Cache);
            _waitFrames = 2;
            // If the next value is -1, but was not -1 before, then fire onExit
            if (prevStep != -1 && nextValue.Value == -1)
                Cache.OnExit();
            // Regardless, close this popup
            ImGui.CloseCurrentPopup();
        }
    }

    private int NextId()
    {
        for (var i = Cache.CurrentStep + 1; i < EndStep; ++i)
            if (Steps[i].Enabled)
                return i;

        return -1;
    }

    // Obtain the current ID if it is enabled, otherwise the first enabled ID after it.
    public int CurrentEnabledId()
    {
        if (Cache.CurrentStep < 0)
            return -1;

        for (var i = Cache.CurrentStep; i < EndStep; ++i)
        {
            if (Steps[i].Enabled)
                return i;
        }

        return -1;
    }

    // Make sure you have as many tutorials registered as you intend to.
    public Tutorial EnsureSize(int size)
    {
        try
        {
            if (_steps.Count != size)
            {
                throw new Exception($"Size of [{_popupLabel}] is invalid. Size is {_steps.Count}, and expected is {size}");
            }
        }
        catch (Bagagwa e)
        {
            Svc.Logger.Error("EnsureSize Exception:" + e.Message);
        }
        return this;
    }

    private bool CloseButton(Vector2 btnPos)
    {
        var drawList = ImGui.GetWindowDrawList();

        var closeButtonColor = _hoveringExit ? ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)) : Cache.BorderColor;

        drawList.AddLine(btnPos, btnPos + _closeSize, closeButtonColor, 3);
        drawList.AddLine(new Vector2(btnPos.X + _closeSize.X, btnPos.Y), new Vector2(btnPos.X, btnPos.Y + _closeSize.Y), closeButtonColor, 3);

        ImGui.SetCursorScreenPos(btnPos);
        if (ImGui.InvisibleButton("##CloseTutorial-" + _popupLabel, _closeSize))
            return true;

        _hoveringExit = ImGui.IsItemHovered();
        return false;
    }
}
