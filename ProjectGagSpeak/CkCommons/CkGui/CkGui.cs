using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Interop;
using GagSpeak.Localization;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using ImGuiNET;

// please dont change this namespace or you will mess up so many references i dont want to deal with fixing.
// unless you are willing to, then by all means please do.
namespace GagSpeak.CkCommons.Gui;

// Primary Partial Class
public static partial class CkGui
{
    private static readonly Dictionary<string, float> CenteredLineWidths = new();
    private static void ImGuiLineCentered(string id, Action func)
    {
        if (CenteredLineWidths.TryGetValue(id, out float dims))
        {
            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X / 2 - dims / 2);
        }
        float oldCur = ImGui.GetCursorPosX();
        func();
        ImGui.SameLine(0, 0);
        CenteredLineWidths[id] = ImGui.GetCursorPosX() - oldCur;
        ImGui.NewLine(); // Use NewLine to finalize the line instead of Dummy
    }

    /// <summary> A helper function for centering the next displayed window. </summary>
    /// <param name="width"> The width of the window. </param>
    /// <param name="height"> The height of the window. </param>
    /// <param name="cond"> The condition for the ImGuiWindow to be displayed . </param>
    public static void CenterNextWindow(float width, float height, ImGuiCond cond = ImGuiCond.None)
    {
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(new Vector2(center.X - width / 2, center.Y - height / 2), cond);
    }

    /// <summary> A helper function for retrieving the proper color value given RGBA. </summary>
    /// <returns> The color formatted as a uint </returns>
    public static uint Color(byte r, byte g, byte b, byte a)
    { uint ret = a; ret <<= 8; ret += b; ret <<= 8; ret += g; ret <<= 8; ret += r; return ret; }

    /// <summary> A helper function for retrieving the proper color value given a vector4. </summary>
    /// <returns> The color formatted as a uint </returns>
    public static uint Color(Vector4 color)
    {
        uint ret = (byte)(color.W * 255);
        ret <<= 8;
        ret += (byte)(color.Z * 255);
        ret <<= 8;
        ret += (byte)(color.Y * 255);
        ret <<= 8;
        ret += (byte)(color.X * 255);
        return ret;
    }

    public static float GetAlpha(uint color)
    {
        byte alpha = (byte)(color >> 24);
        return alpha / 255f;
    }

    public static uint InvertColor(uint color)
    {
        uint r = 0xFF - (color & 0xFF);
        uint g = 0xFF - ((color >> 8) & 0xFF);
        uint b = 0xFF - ((color >> 16) & 0xFF);
        uint a = (color >> 24) & 0xFF;

        return (a << 24) | (b << 16) | (g << 8) | r;
    }

    public static Vector4 InvertColor(Vector4 color)
        => new Vector4(1f - color.X, 1f - color.Y, 1f - color.Z, color.W);


    public static Vector4 GetBoolColor(bool input) => input ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;

/*    public float GetFontScalerFloat() => ImGuiHelpers.GlobalScale * (_pi.UiBuilder.DefaultFontSpec.SizePt / 12f); */
    public static float GetButtonSize(string text)
    {
        var vector2 = ImGui.CalcTextSize(text);
        return vector2.X + ImGui.GetStyle().FramePadding.X * 2f;
    }

    public static float IconTextButtonSize(FAI icon, string text)
    {
        Vector2 vector;
        using (UiFontService.IconFont.Push())
            vector = ImGui.CalcTextSize(icon.ToIconString());

        var vector2 = ImGui.CalcTextSize(text);
        var num = 3f * ImGuiHelpers.GlobalScale;
        return vector.X + vector2.X + ImGui.GetStyle().FramePadding.X * 2f + num;
    }

    public static Vector2 IconButtonSize(FAI icon)
    {
        using var font = UiFontService.IconFont.Push();
        return ImGuiHelpers.GetButtonSize(icon.ToIconString());
    }

    public static Vector2 IconSize(FAI icon)
    {
        using var font = UiFontService.IconFont.Push();
        return ImGui.CalcTextSize(icon.ToIconString());
    }

    public static float CalcCheckboxWidth(string? label = null)
        => label is null ? ImGui.GetFrameHeight() : ImGui.CalcTextSize(label).X + ImGui.GetStyle().ItemInnerSpacing.X + ImGui.GetFrameHeight();

    public static float GetWindowContentRegionWidth() => ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;

    public static bool DrawScaledCenterButtonImage(string ID, Vector2 buttonSize, Vector4 buttonColor,
        Vector2 imageSize, IDalamudTextureWrap image)
    {
        // push ID for the function
        ImGui.PushID(ID);
        // grab the current cursor position
        var InitialPos = ImGui.GetCursorPos();
        // calculate the difference in height between the button and the image
        var heightDiff = buttonSize.Y - imageSize.Y;
        // draw out the button centered
        if (CenteredLineWidths.TryGetValue(ID, out var dims))
        {
            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X / 2 - dims / 2);
        }
        var oldCur = ImGui.GetCursorPosX();
        var result = ImGui.Button(string.Empty, buttonSize);
        //_logger.LogTrace("Result of button: {result}", result);
        ImGui.SameLine(0, 0);
        CenteredLineWidths[ID] = ImGui.GetCursorPosX() - oldCur;
        ImGui.Dummy(Vector2.Zero);
        // now go back up to the initial position, then step down by the height difference/2
        ImGui.SetCursorPosY(InitialPos.Y + heightDiff / 2);
        ImGuiLineCentered($"###CenterImage{ID}", () =>
        {
            ImGui.Image(image.ImGuiHandle, imageSize, Vector2.Zero, Vector2.One, buttonColor);
        });
        ImGui.PopID();
        // return the result
        return result;
    }

    public static float GetSeparatorHeight(float? height = null)
        => height + ImGui.GetStyle().ItemSpacing.Y * 2 ?? ImGui.GetStyle().ItemSpacing.Y * 3;

    public static float GetSeparatorSpacedHeight(float? height = null)
    => height + ImGui.GetStyle().ItemSpacing.Y * 4 ?? ImGui.GetStyle().ItemSpacing.Y * 5;

    public static void Separator(float? height = null, float? width = null, uint? col = null)
    {
        ImGui.Dummy(new Vector2(width ?? ImGui.GetContentRegionAvail().X, height ?? ImGui.GetStyle().ItemSpacing.Y));
        if(col is not null)
            ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), col ?? CkColor.FancyHeaderContrast.Uint());
    }

    public static void SeparatorSpaced(float? height = null, float? width = null, uint? col = null)
    {
        ImGui.Spacing();
        ImGui.Dummy(new Vector2(width ?? ImGui.GetContentRegionAvail().X, height ?? ImGui.GetStyle().ItemSpacing.Y));
        if (col is not null)
            ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), col.Value);
        ImGui.Spacing();
    }

    /// <summary> The additional param for an ID is optional. if not provided, the id will be the text. </summary>
    public static bool IconButton(FAI icon, float? height = null, string? id = null, bool disabled = false, bool inPopup = false)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, disabled ? 0.5f : 1f);
        var num = 0;
        if (inPopup)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
            num++;
        }

        var text = icon.ToIconString();

        ImGui.PushID((id == null) ? icon.ToIconString() : id + icon.ToIconString());
        Vector2 vector;
        using (UiFontService.IconFont.Push())
            vector = ImGui.CalcTextSize(text);
        var windowDrawList = ImGui.GetWindowDrawList();
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var x = vector.X + ImGui.GetStyle().FramePadding.X * 2f;
        var frameHeight = height ?? ImGui.GetFrameHeight();
        var result = ImGui.Button(string.Empty, new Vector2(x, frameHeight));
        var pos = new Vector2(cursorScreenPos.X + ImGui.GetStyle().FramePadding.X,
            cursorScreenPos.Y + (height ?? ImGui.GetFrameHeight()) / 2f - (vector.Y / 2f));
        using (UiFontService.IconFont.Push())
            windowDrawList.AddText(pos, ImGui.GetColorU32(ImGuiCol.Text), text);
        ImGui.PopID();

        if (num > 0)
        {
            ImGui.PopStyleColor(num);
        }
        return result && !disabled;
    }

    private static bool IconTextButtonInternal(FAI icon, string text, Vector4? defaultColor = null, float? width = null, bool disabled = false, string id = "")
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, disabled ? 0.5f : 1f);
        var num = 0;
        if (defaultColor.HasValue)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, defaultColor.Value);
            num++;
        }

        ImGui.PushID(text + "##" + id);
        Vector2 vector;
        using (UiFontService.IconFont.Push())
            vector = ImGui.CalcTextSize(icon.ToIconString());
        var vector2 = ImGui.CalcTextSize(text);
        var windowDrawList = ImGui.GetWindowDrawList();
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var num2 = 3f * ImGuiHelpers.GlobalScale;
        var x = width ?? vector.X + vector2.X + ImGui.GetStyle().FramePadding.X * 2f + num2;
        var frameHeight = ImGui.GetFrameHeight();
        var result = ImGui.Button(string.Empty, new Vector2(x, frameHeight));
        var pos = new Vector2(cursorScreenPos.X + ImGui.GetStyle().FramePadding.X, cursorScreenPos.Y + ImGui.GetStyle().FramePadding.Y);
        using (UiFontService.IconFont.Push())
            windowDrawList.AddText(pos, ImGui.GetColorU32(ImGuiCol.Text), icon.ToIconString());
        var pos2 = new Vector2(pos.X + vector.X + num2, cursorScreenPos.Y + ImGui.GetStyle().FramePadding.Y);
        windowDrawList.AddText(pos2, ImGui.GetColorU32(ImGuiCol.Text), text);
        ImGui.PopID();
        if (num > 0)
        {
            ImGui.PopStyleColor(num);
        }
        dis.Pop();

        return result && !disabled;
    }

    public static bool IconTextButton(FAI icon, string text, float? width = null, bool isInPopup = false, bool disabled = false, string id = "Identifier")
    {
        return IconTextButtonInternal(icon, text,
            isInPopup ? new Vector4(1.0f, 1.0f, 1.0f, 0.0f) : null,
            width <= 0 ? null : width,
            disabled, id);
    }

    private static bool IconSliderFloatInternal(string id, FAI icon, string label, ref float valueRef, float min,
        float max, Vector4? defaultColor = null, float? width = null, bool disabled = false, string format = "%.1f")
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, disabled ? 0.5f : 1f);
        var num = 0;
        // Disable if issues, tends to be culpret
        if (defaultColor.HasValue)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, defaultColor.Value);
            num++;
        }

        ImGui.PushID(id);
        Vector2 vector;
        using (UiFontService.IconFont.Push())
            vector = ImGui.CalcTextSize(icon.ToIconString());
        var vector2 = ImGui.CalcTextSize(label);
        var windowDrawList = ImGui.GetWindowDrawList();
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var num2 = 3f * ImGuiHelpers.GlobalScale;
        var x = width ?? vector.X + vector2.X + ImGui.GetStyle().FramePadding.X * 2f + num2;
        var frameHeight = ImGui.GetFrameHeight();
        ImGui.SetCursorPosX(vector.X + ImGui.GetStyle().FramePadding.X * 2f);
        ImGui.SetNextItemWidth(x - vector.X - num2 * 4); // idk why this works, it probably doesnt on different scaling. Idfk. Look into later.
        var result = ImGui.SliderFloat(label + "##" + id, ref valueRef, min, max, format);

        var pos = new Vector2(cursorScreenPos.X + ImGui.GetStyle().FramePadding.X, cursorScreenPos.Y + ImGui.GetStyle().FramePadding.Y);
        using (UiFontService.IconFont.Push())
            windowDrawList.AddText(pos, ImGui.GetColorU32(ImGuiCol.Text), icon.ToIconString());
        ImGui.PopID();
        if (num > 0)
        {
            ImGui.PopStyleColor(num);
        }
        dis.Pop();

        return result && !disabled;
    }

    public static bool IconSliderFloat(string id, FAI icon, string label, ref float valueRef,
        float min, float max, float? width = null, bool isInPopup = false, bool disabled = false)
    {
        return IconSliderFloatInternal(id, icon, label, ref valueRef, min, max,
            isInPopup ? new Vector4(1.0f, 1.0f, 1.0f, 0.1f) : null,
            width <= 0 ? null : width,
            disabled);
    }

    private static bool IconInputTextInternal(string id, FAI icon, string label, string hint, ref string inputStr,
        uint maxLength, Vector4? defaultColor = null, float? width = null, bool disabled = false)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, disabled ? 0.5f : 1f);
        var num = 0;
        // Disable if issues, tends to be culpret
        if (defaultColor.HasValue)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, defaultColor.Value);
            num++;
        }

        ImGui.PushID(id);
        Vector2 vector;
        using (UiFontService.IconFont.Push())
            vector = ImGui.CalcTextSize(icon.ToIconString());
        var vector2 = ImGui.CalcTextSize(label);
        var windowDrawList = ImGui.GetWindowDrawList();
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var num2 = 3f * ImGuiHelpers.GlobalScale;
        var x = width ?? vector.X + vector2.X + ImGui.GetStyle().FramePadding.X * 2f + num2;
        var frameHeight = ImGui.GetFrameHeight();
        ImGui.SetCursorPosX(vector.X + ImGui.GetStyle().FramePadding.X * 2f);
        ImGui.SetNextItemWidth(x - vector.X - num2 * 4); // idk why this works, it probably doesnt on different scaling. Idfk. Look into later.
        var result = ImGui.InputTextWithHint(label, hint, ref inputStr, maxLength, ImGuiInputTextFlags.EnterReturnsTrue);

        var pos = new Vector2(cursorScreenPos.X + ImGui.GetStyle().FramePadding.X, cursorScreenPos.Y + ImGui.GetStyle().FramePadding.Y);
        using (UiFontService.IconFont.Push())
            windowDrawList.AddText(pos, ImGui.GetColorU32(ImGuiCol.Text), icon.ToIconString());
        ImGui.PopID();
        if (num > 0)
        {
            ImGui.PopStyleColor(num);
        }
        dis.Pop();

        return result && !disabled;
    }

    public static bool IconInputText(string id, FAI icon, string label, string hint, ref string inputStr,
        uint maxLength, float? width = null, bool isInPopup = false, bool disabled = false)
    {
        return IconInputTextInternal(id, icon, label, hint, ref inputStr, maxLength,
            isInPopup ? new Vector4(1.0f, 1.0f, 1.0f, 0.1f) : null,
            width <= 0 ? null : width,
            disabled);
    }

    public static void SetScaledWindowSize(float width, bool centerWindow = true)
    {
        var newLineHeight = ImGui.GetCursorPosY();
        ImGui.NewLine();
        newLineHeight = ImGui.GetCursorPosY() - newLineHeight;
        var y = ImGui.GetCursorPos().Y + ImGui.GetWindowContentRegionMin().Y - newLineHeight * 2 - ImGui.GetStyle().ItemSpacing.Y;

        SetScaledWindowSize(width, y, centerWindow, scaledHeight: true);
    }

    public static void SetScaledWindowSize(float width, float height, bool centerWindow = true, bool scaledHeight = false)
    {
        ImGui.SameLine();
        var x = width * ImGuiHelpers.GlobalScale;
        var y = scaledHeight ? height : height * ImGuiHelpers.GlobalScale;

        if (centerWindow)
        {
            CenterWindow(x, y);
        }

        ImGui.SetWindowSize(new Vector2(x, y));
    }

    public static void SetCursorXtoCenter(float width)
        => ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) / 2 - width / 2);

    public static void BooleanToColoredIcon(bool value, bool inline = true, FAI trueIcon = FAI.Check, FAI falseIcon = FAI.Times, Vector4 colorTrue = default, Vector4 colorFalse = default)
    {
        if (inline)
            ImGui.SameLine();

        if (value)
            using (ImRaii.PushColor(ImGuiCol.Text, (colorTrue == default) ? ImGuiColors.HealerGreen : colorTrue)) FramedIconText(trueIcon);
        else
            using (ImRaii.PushColor(ImGuiCol.Text, (colorFalse == default) ? ImGuiColors.DalamudRed : colorFalse)) FramedIconText(falseIcon);
    }

    public static void DrawOptionalPlugins()
    {
        var check = FAI.Check;
        var cross = FAI.SquareXmark;
        ImGui.TextUnformatted(GSLoc.Settings.OptionalPlugins);

        ImGui.SameLine();
        ImGui.TextUnformatted("Penumbra");
        ImGui.SameLine();
        IconText(IpcCallerPenumbra.APIAvailable ? check : cross, GetBoolColor(IpcCallerPenumbra.APIAvailable));
        ImGui.SameLine();
        AttachToolTip(IpcCallerPenumbra.APIAvailable ? GSLoc.Settings.PluginValid : GSLoc.Settings.PluginInvalid);
        ImGui.Spacing();

        ImGui.SameLine();
        ImGui.TextUnformatted("Glamourer");
        ImGui.SameLine();
        IconText(IpcCallerGlamourer.APIAvailable ? check : cross, GetBoolColor(IpcCallerGlamourer.APIAvailable));
        ImGui.SameLine();
        AttachToolTip(IpcCallerGlamourer.APIAvailable ? GSLoc.Settings.PluginValid : GSLoc.Settings.PluginInvalid);
        ImGui.Spacing();

        ImGui.SameLine();
        ImGui.TextUnformatted("Customize+");
        ImGui.SameLine();
        IconText(IpcCallerCustomize.APIAvailable ? check : cross, GetBoolColor(IpcCallerCustomize.APIAvailable));
        ImGui.SameLine();
        AttachToolTip(IpcCallerCustomize.APIAvailable ? GSLoc.Settings.PluginValid : GSLoc.Settings.PluginInvalid);
        ImGui.Spacing();

        ImGui.SameLine();
        ImGui.TextUnformatted("Moodles");
        ImGui.SameLine();
        IconText(IpcCallerMoodles.APIAvailable ? check : cross, GetBoolColor(IpcCallerMoodles.APIAvailable));
        ImGui.SameLine();
        AttachToolTip(IpcCallerMoodles.APIAvailable ? GSLoc.Settings.PluginValid : GSLoc.Settings.PluginInvalid);
        ImGui.Spacing();
    }

    private static void CenterWindow(float width, float height, ImGuiCond cond = ImGuiCond.None)
    {
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetWindowPos(new Vector2(center.X - width / 2, center.Y - height / 2), cond);
    }

    /// <summary> Retrieves the various UID text color based on the current server state. </summary>
    /// <returns> The color of the UID text in Vector4 format .</returns>
    public static Vector4 UidColor()
    {
        return MainHub.ServerStatus switch
        {
            ServerState.Connecting => ImGuiColors.DalamudYellow,
            ServerState.Reconnecting => ImGuiColors.DalamudRed,
            ServerState.Connected => ImGuiColors.ParsedPink,
            ServerState.ConnectedDataSynced => ImGuiColors.ParsedPink,
            ServerState.Disconnected => ImGuiColors.DalamudYellow,
            ServerState.Disconnecting => ImGuiColors.DalamudYellow,
            ServerState.Unauthorized => ImGuiColors.DalamudRed,
            ServerState.VersionMisMatch => ImGuiColors.DalamudRed,
            ServerState.Offline => ImGuiColors.DalamudRed,
            ServerState.NoSecretKey => ImGuiColors.DalamudYellow,
            _ => ImGuiColors.DalamudRed
        };
    }

    public static Vector4 ServerStateColor()
    {
        return MainHub.ServerStatus switch
        {
            ServerState.Connecting => ImGuiColors.DalamudYellow,
            ServerState.Reconnecting => ImGuiColors.DalamudYellow,
            ServerState.Connected => ImGuiColors.HealerGreen,
            ServerState.ConnectedDataSynced => ImGuiColors.HealerGreen,
            ServerState.Disconnected => ImGuiColors.DalamudRed,
            ServerState.Disconnecting => ImGuiColors.DalamudYellow,
            ServerState.Unauthorized => ImGuiColors.ParsedOrange,
            ServerState.VersionMisMatch => ImGuiColors.ParsedOrange,
            ServerState.Offline => ImGuiColors.DPSRed,
            ServerState.NoSecretKey => ImGuiColors.ParsedOrange,
            _ => ImGuiColors.ParsedOrange
        };
    }

    public static FAI ServerStateIcon(ServerState state)
    {
        return state switch
        {
            ServerState.Connecting => FAI.SatelliteDish,
            ServerState.Reconnecting => FAI.SatelliteDish,
            ServerState.Connected => FAI.Link,
            ServerState.ConnectedDataSynced => FAI.Link,
            ServerState.Disconnected => FAI.Unlink,
            ServerState.Disconnecting => FAI.SatelliteDish,
            ServerState.Unauthorized => FAI.Shield,
            ServerState.VersionMisMatch => FAI.Unlink,
            ServerState.Offline => FAI.Signal,
            ServerState.NoSecretKey => FAI.Key,
            _ => FAI.ExclamationTriangle
        };
    }

    /// <summary> Retrieves the various UID text based on the current server state. </summary>
    /// <returns> The text of the UID.</returns>
    public static string GetUidText()
    {
        return MainHub.ServerStatus switch
        {
            ServerState.Reconnecting => "Reconnecting",
            ServerState.Connecting => "Connecting",
            ServerState.Disconnected => "Disconnected",
            ServerState.Disconnecting => "Disconnecting",
            ServerState.Unauthorized => "Unauthorized",
            ServerState.VersionMisMatch => "Version mismatch",
            ServerState.Offline => "Unavailable",
            ServerState.NoSecretKey => "No Secret Key",
            ServerState.Connected => MainHub.DisplayName,
            ServerState.ConnectedDataSynced => MainHub.DisplayName,
            _ => string.Empty
        };
    }
}
