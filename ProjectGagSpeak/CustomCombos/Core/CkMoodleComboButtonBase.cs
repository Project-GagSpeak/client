using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CustomCombos;

public abstract class CkMoodleComboButtonBase<T> : CkFilterComboCache<T>
{
    protected readonly MoodleIcons _statuses;
    protected readonly MainHub _mainHub;
    protected readonly Pair _pairRef;
    protected float _iconScale;

    protected CkMoodleComboButtonBase(float iconScale, MoodleIcons monitor, Pair pair, MainHub hub,
        ILogger log, Func<IReadOnlyList<T>> itemSource)
        : base(itemSource, log)
    {
        _mainHub = hub;
        _iconScale = iconScale;
        Current = default;
    }

    protected virtual Vector2 IconSize => MoodleDrawer.IconSize * _iconScale;

    /// <summary> The condition that when met, prevents the combo from being interacted. </summary>
    protected abstract bool DisableCondition();
    protected abstract bool CanDoAction(T item);
    protected abstract Task<bool> OnApplyButton(T item);
    protected virtual Task<bool> OnRemoveButton(T item) => Task.FromResult(true);

    protected override void DrawList(float width, float itemHeight)
    {
        try
        {
            ImGui.SetWindowFontScale(_iconScale);
            base.DrawList(width, itemHeight);
        }
        finally
        {
            ImGui.SetWindowFontScale(1.0f);
        }
    }

    /// <summary> The virtual function for all filter combo buttons. </summary>
    /// <returns> True if anything was selected, false otherwise. </returns>
    /// <remarks> The action passed in will be invoked if the button interaction was successful. </remarks>
    public bool DrawComboButton(string label, float width, bool isApply, string tt, Action? onButtonSuccess = null)
    {
        // we need to first extract the width of the button.
        var buttonText = isApply ? "Apply" : "Remove";
        var comboWidth = width - ImGui.GetStyle().ItemInnerSpacing.X - CkGui.IconTextButtonSize(FAI.PersonRays, buttonText);
        InnerWidth = width;

        // if we have a new item selected we need to update some conditionals.

        var previewLabel = Current?.ToString() ?? "Select an Item...";
        var ret = Draw(label, previewLabel, string.Empty, comboWidth, ImGui.GetTextLineHeightWithSpacing(), ImGuiComboFlags.None);
        
        // move just beside it to draw the button.
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.PersonRays, buttonText, disabled: DisableCondition()))
        {
            if (Current is { } item)
            {
                _ = Task.Run(async () =>
                {
                    if (isApply)
                    {
                        if (await OnApplyButton(item))
                            onButtonSuccess?.Invoke();
                    }
                    else
                    {
                        if (await OnRemoveButton(item))
                            onButtonSuccess?.Invoke();
                    }
                });
            }
        }
        CkGui.AttachToolTip(tt);

        return ret;
    }

    protected void DrawItemTooltip(MoodlesStatusInfo item, string dispellMoodleTitle)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            ImGui.BeginTooltip();

            if (!item.Description.IsNullOrWhitespace())
            {
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                ImGui.TextUnformatted(item.Description);
                ImGui.PopTextWrapPos();
            }

            ImGui.Separator();
            CkGui.ColorText("Stacks:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Stacks.ToString());
            if (item.StackOnReapply)
            {
                ImGui.SameLine();
                CkGui.ColorText(" (inc by " + item.StacksIncOnReapply + ")", ImGuiColors.ParsedGold);
            }

            CkGui.ColorText("Duration:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text($"{item.Days}d {item.Hours}h {item.Minutes}m {item.Seconds}");

            CkGui.ColorText("Category:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Type.ToString());

            CkGui.ColorText("Dispellable:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Dispelable ? "Yes" : "No");

            if (item.StatusOnDispell != Guid.Empty)
            {
                CkGui.ColorText("StatusOnDispell:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                ImGui.Text(dispellMoodleTitle);
            }

            ImGui.EndTooltip();
        }
    }
}
