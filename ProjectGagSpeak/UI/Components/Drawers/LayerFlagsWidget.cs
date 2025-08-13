using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using OtterGui.Text;
using OtterGui.Text.Widget;

namespace GagSpeak.Gui.Components;
public enum FlagComboMode
{
    None,
    EnableOnly,
    DisableOnly,
}

public class LayerFlagsWidget
{
    private string _preview;
    private RestraintLayer _added = RestraintLayer.None;
    private RestraintLayer _removed = RestraintLayer.None;

    public readonly FAI Icon;
    public readonly string IdLabel;
    
    public LayerFlagsWidget(FAI icon, string id, string preview)
    {
        _preview = preview;
        Icon = icon;
        IdLabel = id;
    }

    private string GetIconButtonText(FlagComboMode mode)
        => mode switch
        {
            FlagComboMode.EnableOnly => $"Enable Layers",
            FlagComboMode.DisableOnly => $"Disable Layers",
            _ => "Update Layers",
        };

    private void ResetChanges()
    {
        _added = RestraintLayer.None;
        _removed = RestraintLayer.None;
    }

    /// <summary>
    ///     Draws a combo - button pair, that tracks what flags are enabled and which are removed. <para />
    ///     Changes are passed out as <c>added</c> and <c>removed</c> flags, which can be used to apply the changes.
    /// </summary> 
    /// <returns> True when the update button is pressed, false otherwise.</returns>
    public bool Draw(float width, RestraintLayer current, out RestraintLayer added, out RestraintLayer removed, IEnumerable<RestraintLayer> options,
        Func<RestraintLayer, string>? toName = null, CFlags flags = CFlags.None)
        => DrawInternal(FlagComboMode.None, width, current, out added, out removed, options, toName, flags);

    private bool DrawInternal(FlagComboMode mode, float width, RestraintLayer current, out RestraintLayer added, out RestraintLayer removed,
        IEnumerable<RestraintLayer> options, Func<RestraintLayer, string>? toName = null, CFlags flags = CFlags.None)
    {
        // push the ID so our combo will be valid.
        using var id = ImUtf8.PushId(IdLabel);
        var buttonTxt = GetIconButtonText(mode);
        var comboWidth = width - CkGui.IconTextButtonSize(Icon, buttonTxt) - ImGui.GetStyle().ItemInnerSpacing.X;
        ImGui.SetNextItemWidth(comboWidth);
        using (var combo = ImUtf8.Combo(""u8, _preview, flags))
        {
            if (combo)
                DrawLayerCheckboxes(current, options, toName, mode);
        }

        ImUtf8.SameLineInner();
        var hasChanges = _added != RestraintLayer.None || _removed != RestraintLayer.None;
        // update outs.
        added = _added;
        removed = _removed;
        if(CkGui.IconTextButton(Icon, buttonTxt, disabled: !hasChanges))
        {
            ResetChanges();
            return true;
        }
        return false;
    }

    public void DrawLayerCheckboxes(RestraintLayer current, IEnumerable<RestraintLayer> options, Func<RestraintLayer, string>? toName = null, FlagComboMode mode = FlagComboMode.None)
    {
        var blockedLayers = mode switch
        {
            FlagComboMode.EnableOnly => current,
            FlagComboMode.DisableOnly => ~current,
            _ => RestraintLayer.None,
        };

        var wdl = ImGui.GetWindowDrawList();
        foreach (var flag in options)
        {
            var wasSet = (_added | (current & ~_removed)) & flag;
            var isSet = wasSet != 0;

            using var disabled = ImRaii.Disabled(blockedLayers.HasAny(flag));
            var name = toName?.Invoke(flag) ?? flag.ToString();

            var changed = (_added.HasAny(flag) || _removed.HasAny(flag));
            using (ImRaii.PushColor(ImGuiCol.CheckMark, ImGuiColors.HealerGreen, changed)) // push only for checkbox.
            {
                if (ImGui.Checkbox($"##LayerFlag_{(int)flag}", ref isSet))
                {
                    if (isSet == current.HasAny(flag))
                    {
                        Svc.Logger.Information("Same as original: clear both added and removed flags for this flag.");
                        _added &= ~flag;
                        _removed &= ~flag;
                    }
                    else if (isSet)
                    {
                        Svc.Logger.Information("Checked on, but originally off → mark as added");
                        _added |= flag;
                        _removed &= ~flag;
                    }
                    else
                    {
                        Svc.Logger.Information("Unchecked off, but originally on → mark as removed");
                        _removed |= flag;
                        _added &= ~flag;
                    }
                }
                if (!isSet && changed)
                    wdl.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.CheckMark), ImGui.GetStyle().FrameRounding, ImDrawFlags.RoundCornersAll);
            }
            // add text.
            ImUtf8.SameLineInner();
            ImUtf8.TextFrameAligned(name);
        }
    }

    public bool DrawUpdateButton(FAI icon, string buttonTxt, out RestraintLayer added, out RestraintLayer removed, float? width = null)
    {
        // update outs.
        added = _added;
        removed = _removed;
        var hasChanges = _added != RestraintLayer.None || _removed != RestraintLayer.None;
        if (CkGui.IconTextButton(icon, buttonTxt, width, disabled: !hasChanges))
        {
            ResetChanges();
            return true;
        }
        return false;
    }

    /// <summary>
    ///     Combo-Button pair, that tracks the <seealso cref="_added"/> flags, not currently enabled in <paramref name="current"/>.<para />
    ///     Changes are passed out as <paramref name="updates"/>, which can be used to apply the changes.
    /// </summary> 
    /// <returns> True when the update button is pressed, false otherwise.</returns>
    public bool DrawApply(float width, RestraintLayer current, out RestraintLayer updates, 
        IEnumerable<RestraintLayer> options, Func<RestraintLayer, string>? toName = null, CFlags flags = CFlags.None)
        => DrawInternal(FlagComboMode.EnableOnly, width, current, out updates, out _, options, toName, flags);

    /// <summary>
    ///     Combo-Button pair, that tracks the <seealso cref="_removed"/> flags, not currently enabled in <paramref name="current"/>.<para />
    ///     Changes are passed out as <paramref name="updates"/>, which can be used to apply the changes.
    /// </summary> 
    /// <returns> True when the update button is pressed, false otherwise.</returns>
    public bool DrawRemove(float width, RestraintLayer current, out RestraintLayer updates, 
        IEnumerable<RestraintLayer> options, Func<RestraintLayer, string>? toName = null, CFlags flags = CFlags.None)
        => DrawInternal(FlagComboMode.DisableOnly, width, current, out _, out updates, options, toName, flags);

    /// <inheritdoc cref="DrawApply(float, RestraintLayer, out RestraintLayer, IEnumerable{RestraintLayer}, Func{RestraintLayer, string}?, ImGuiComboFlags)"/>
    public bool Draw(float width, RestraintLayer current, out RestraintLayer added, out RestraintLayer removed,
        Func<RestraintLayer, string>? toName = null, int skip = 0, int skipEnd = 0, CFlags flags = CFlags.None)
        => Draw(width, current, out added, out removed, Enum.GetValues<RestraintLayer>().Skip(skip).SkipLast(skipEnd), toName, flags);

    /// <inheritdoc cref="DrawApply(float, RestraintLayer, out RestraintLayer, IEnumerable{RestraintLayer}, Func{RestraintLayer, string}?, ImGuiComboFlags)"/>
    public bool DrawApply(float width, RestraintLayer current, out RestraintLayer added,
        Func<RestraintLayer, string>? toName = null, int skip = 0, int skipEnd = 0, CFlags flags = CFlags.None)
        => DrawApply(width, current, out added, Enum.GetValues<RestraintLayer>().Skip(skip).SkipLast(skipEnd), toName, flags);

    /// <inheritdoc cref="DrawRemove(float, RestraintLayer, out RestraintLayer, IEnumerable{RestraintLayer}, Func{RestraintLayer, string}?, ImGuiComboFlags)"/>
    public bool DrawRemove(float width, RestraintLayer current, out RestraintLayer removed,
        Func<RestraintLayer, string>? toName = null, int skip = 0, int skipEnd = 0, CFlags flags = CFlags.None)
        => DrawRemove(width, current, out removed, Enum.GetValues<RestraintLayer>().Skip(skip).SkipLast(skipEnd), toName, flags);


}

