using CkCommons;
using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Pairs;

public sealed class PairPatternCombo : CkFilterComboIconTextButton<KinksterPattern>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _ref;
    public PairPatternCombo(ILogger log, MainHub hub, Kinkster pair, Action postButtonPress)
        : base(log, FAI.PlayCircle, () => [ ..pair.LightCache.Patterns.Values.OrderBy(x => x.Label)])
    {
        _mainHub = hub;
        _ref = pair;
        PostButtonPress = postButtonPress;
    }

    private bool ValidForExecution(ToyBrandName Device1, ToyBrandName Device2)
        => _ref.ValidToys.Contains(Device1) && _ref.ValidToys.Contains(Device2);

    protected override bool DisableCondition()
        => Current is null || !_ref.PairPerms.ExecutePatterns || _ref.ActivePattern != Guid.Empty || !ValidForExecution(Current.Device1, Current.Device2);
    protected override string ToString(KinksterPattern obj)
        => obj.Label;

    public bool Draw(string label, float width, string tt)
        => Draw(label, width, "Execute", tt);

    protected override void DrawList(float width, float itemHeight, float filterHeight)
    {
        _iconInfoWidth = CkGui.IconSize(FAI.InfoCircle).X;
        _iconCheckWidth = CkGui.IconSize(FAI.Check).X;
        _iconLoopWidth = CkGui.IconSize(FAI.Sync).X;
        base.DrawList(width, itemHeight, filterHeight);
    }

    private float _iconInfoWidth;
    private float _iconLoopWidth;
    private float _iconCheckWidth;

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var pattern = Items[globalIdx];
        var ret = ImGui.Selectable(pattern.Label, selected);

        // Handle the shifted draw based on the loop state.
        var isValid = ValidForExecution(pattern.Device1, pattern.Device2);
        if (pattern.Loops)
        {
            var rightW = _iconLoopWidth + _iconCheckWidth + _iconInfoWidth + ImGui.GetStyle().ItemInnerSpacing.X * 2;
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - rightW);
            CkGui.IconText(FAI.Sync, ImGui.GetColorU32(ImGuiColors.ParsedPink));
            CkGui.AttachToolTip("This is a Looping Pattern.");

            ImUtf8.SameLineInner();
        }
        else
        {
            var rightW = _iconInfoWidth + _iconCheckWidth + ImGui.GetStyle().ItemInnerSpacing.X;
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - rightW);
        }

        CkGui.IconText(FAI.Check, isValid ? ImGuiColors.HealerGreen.ToUint() : ImGui.GetColorU32(ImGuiCol.TextDisabled));
        CkGui.AttachToolTip(isValid
            ? $"Can execute: A used device is active on {_ref.GetNickAliasOrUid()}."
            : $"Cannot execute: No used devices are active on {_ref.GetNickAliasOrUid()}.");

        ImUtf8.SameLineInner();
        CkGui.HoverIconText(FAI.InfoCircle, ImGuiColors.TankBlue.ToUint(), ImGui.GetColorU32(ImGuiCol.TextDisabled));
        DrawItemTooltip(pattern);
        return ret;
    }

    protected override void OnButtonPress()
    {
        // we need to go ahead and create a deep clone of our new appearanceData, and ensure it is valid.
        if (Current is null)
            return;

        // if the pattern cannot be executed to the pair, do not do so.
        if (!ValidForExecution(Current.Device1, Current.Device2))
        {
            Log.LogError($"Patter not valid for execution!");
            return;
        }

        var updateType = _ref.ActivePattern == Guid.Empty
            ? DataUpdateType.PatternExecuted : DataUpdateType.PatternSwitched;

        UiService.SetUITask(async () =>
        {
            var dto = new PushKinksterActivePattern(_ref.UserData, Current.Id, updateType);
            var result = await _mainHub.UserChangeKinksterActivePattern(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform Pattern with {Current.Label} on {_ref.GetNickAliasOrUid()}, Reason:{result.ErrorCode}", LoggerType.StickyUI);
                PostButtonPress?.Invoke();
            }
            else
            {
                Log.LogDebug($"Executing Pattern {Current.Label} on {_ref.GetNickAliasOrUid()}'s Toy", LoggerType.StickyUI);
                PostButtonPress?.Invoke();
            }
        });
    }

    private void DrawItemTooltip(KinksterPattern item)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
                .Push(ImGuiStyleVar.WindowRounding, 4f)
                .Push(ImGuiStyleVar.PopupBorderSize, 1f);
            using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            // begin the tooltip interface
            ImGui.BeginTooltip();

            CkGui.ColorText("Description:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            if (string.IsNullOrWhiteSpace(item.Description))
                ImGui.TextUnformatted("<None Provided>");
            else
                CkGui.WrappedTooltipText(item.Description, 35f, ImGuiColors.ParsedPink);
            
            ImGui.Separator();

            var durationStr = item.Duration.Hours > 0 ? item.Duration.ToString("hh\\:mm\\:ss") : item.Duration.ToString("mm\\:ss");
            CkGui.ColorText("Duration:", ImGuiColors.ParsedGold);
            CkGui.TextInline(durationStr);

            CkGui.ColorText("Loops?:", ImGuiColors.ParsedGold);
            CkGui.TextInline(item.Loops ? "Yes" : "No");

            CkGui.ColorText("Primary Device:", ImGuiColors.ParsedGold);
            CkGui.TextInline(item.Device1.ToName());

            if (item.Device2 is not ToyBrandName.Unknown)
            {
                CkGui.ColorText("Secondary Device:", ImGuiColors.ParsedGold);
                CkGui.TextInline(item.Device2.ToName());
            }

            CkGui.ColorText("Motors Used:", ImGuiColors.ParsedGold);
            CkGui.TextInline(item.Motors.ToString());

            ImGui.EndTooltip();
        }
    }
}

