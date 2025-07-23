using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Pairs;

public sealed class PairPatternCombo : CkFilterComboIconButton<KinksterPattern>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _kinksterRef;
    public PairPatternCombo(ILogger log, MainHub hub, Kinkster pair, Action postButtonPress)
        : base(log, FAI.PlayCircle, "Execute", () => [ ..pair.LightCache.Patterns.Values.OrderBy(x => x.Label)])
    {
        _mainHub = hub;
        _kinksterRef = pair;
        PostButtonPress = postButtonPress;
    }

    protected override bool DisableCondition()
        => !_kinksterRef.PairPerms.ExecutePatterns;

    // we need to override the drawSelectable method here for a custom draw display.
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var pattern = Items[globalIdx];
        // we want to start by drawing the selectable first.
        var ret = ImGui.Selectable(pattern.Label, selected);

        // shift over and draw an info circle, and a loop circle if any.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 2 * ImGui.GetTextLineHeight() - ImGui.GetStyle().ItemSpacing.X);

        // draw the shouldLoop icon.
        CkGui.IconText(FAI.Sync, ImGui.GetColorU32(pattern.Loops ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey));
        if (pattern.Loops) CkGui.AttachToolTip("This is a Looping Pattern.");

        // draw the info icon.
        ImGui.SameLine();
        CkGui.IconText(FAI.InfoCircle, ImGuiColors.TankBlue);
        DrawItemTooltip(pattern);
        return ret;
    }

    protected override async Task<bool> OnButtonPress()
    {
        // we need to go ahead and create a deep clone of our new appearanceData, and ensure it is valid.
        if (Current is null)
            return false;

        var updateType = _kinksterRef.ActivePattern == Guid.Empty
            ? DataUpdateType.PatternExecuted : DataUpdateType.PatternSwitched;

        // construct the dto to send.
        var dto = new PushKinksterActivePattern(_kinksterRef.UserData, Current.Id, updateType);
        var result = await _mainHub.UserChangeKinksterActivePattern(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform Pattern with {Current.Label} on {_kinksterRef.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
            PostButtonPress?.Invoke();
            return false;
        }
        else
        {
            Log.LogDebug($"Executing Pattern {Current.Label} on {_kinksterRef.GetNickAliasOrUid()}'s Toy", LoggerType.StickyUI);
            PostButtonPress?.Invoke();
            return true;
        }
    }

    private void DrawItemTooltip(KinksterPattern item)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            // begin the tooltip interface
            ImGui.BeginTooltip();

            if (!item.Description.IsNullOrWhitespace())
            {
                // push the text wrap position to the font size times 35
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                // we will then check to see if the text contains a tooltip
                if (item.Description.Contains(CkGui.TipSep, StringComparison.Ordinal))
                {
                    // if it does, we will split the text by the tooltip
                    var splitText = item.Description.Split(CkGui.TipSep, StringSplitOptions.None);
                    // for each of the split text, we will display the text unformatted
                    for (var i = 0; i < splitText.Length; i++)
                    {
                        ImGui.TextUnformatted(splitText[i]);
                        if (i != splitText.Length - 1) ImGui.Separator();
                    }
                }
                else
                {
                    ImGui.TextUnformatted(item.Description);
                }
                // finally, pop the text wrap position
                ImGui.PopTextWrapPos();
                ImGui.Separator();
            }

            CkGui.ColorText("Duration:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            var durationStr = item.Duration.Hours > 0 ? item.Duration.ToString("hh\\:mm\\:ss") : item.Duration.ToString("mm\\:ss");
            ImGui.Text(durationStr);

            CkGui.ColorText("Loops?:", ImGuiColors.ParsedGold);
            ImUtf8.SameLineInner();
            ImGui.Text(item.Loops ? "Yes" : "No");

            ImGui.EndTooltip();
        }
    }
}

