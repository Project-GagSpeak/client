using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using ImGuiNET;
using OtterGui;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.CkCommons.Helpers;

public static class Icons
{

    /// <summary> Draw a game icon display (not icon button or anything) </summary>
    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size, EquipSlot slot, bool doHover = true)
    {
        try
        {
            var isEmpty = item.PrimaryId.Id == 0;
            var (ptr, textureSize, empty) = textures.GetIcon(item, slot);
            if (empty)
            {
                var (bgColor, tint) = isEmpty
                    ? (ImGui.GetColorU32(ImGuiCol.FrameBg), Vector4.One)
                    : (ImGui.GetColorU32(ImGuiCol.FrameBgActive), new Vector4(0.3f, 0.3f, 0.3f, 1f));
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, bgColor, 5 * ImGuiHelpers.GlobalScale);
                if (ptr != nint.Zero)
                    ImGui.Image(ptr, size, Vector2.Zero, Vector2.One, tint);
                else
                    ImGui.Dummy(size);
            }
            else
            {
                ImGui.Image(ptr, size);
                if (doHover) ImGuiUtil.HoverIconTooltip(ptr, size, textureSize);
            }
        }
        catch (Exception e)
        {
            StaticLogger.Logger.LogError(e, "Error drawing icon");
        }
    }

    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size, BonusItemFlag slot)
    {
        var isEmpty = item.PrimaryId.Id == 0;
        var (ptr, textureSize, empty) = textures.GetIcon(item, slot);
        if (empty)
        {
            var (bgColor, tint) = isEmpty
                ? (ImGui.GetColorU32(ImGuiCol.FrameBg), Vector4.One)
                : (ImGui.GetColorU32(ImGuiCol.FrameBgActive), new Vector4(0.3f, 0.3f, 0.3f, 1f));
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, bgColor, 5 * ImGuiHelpers.GlobalScale);
            if (ptr != nint.Zero)
                ImGui.Image(ptr, size, Vector2.Zero, Vector2.One, tint);
            else
                ImGui.Dummy(size);
        }
        else
        {
            ImGuiUtil.HoverIcon(ptr, textureSize, size);
        }
    }

    public static bool DrawFavoriteStar(FavoritesManager favorites, FavoriteIdContainer type, Guid id)
    {
        var isFavorite = type switch
        {
            FavoriteIdContainer.Restraint => favorites._favoriteRestraints.Contains(id),
            FavoriteIdContainer.Restriction => favorites._favoriteRestrictions.Contains(id),
            FavoriteIdContainer.CursedLoot => favorites._favoriteCursedLoot.Contains(id),
            FavoriteIdContainer.Pattern => favorites._favoritePatterns.Contains(id),
            FavoriteIdContainer.Alarm => favorites._favoriteAlarms.Contains(id),
            FavoriteIdContainer.Trigger => favorites._favoriteTriggers.Contains(id),
            _ => false
        };
        var hovering = ImGui.IsMouseHoveringRect(
            ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()));

        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        using var c = ImRaii.PushColor(ImGuiCol.Text, hovering 
            ? ImGuiColors.DalamudWhite2 : isFavorite ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3);

        ImGui.TextUnformatted(FontAwesomeIcon.Star.ToIconString());
        if (!ImGui.IsItemClicked())
            return false;

        if (isFavorite)
            favorites.RemoveRestriction(type, id);
        else
            favorites.TryAddRestriction(type, id);
        return true;
    }

    public static bool DrawFavoriteStar(FavoritesManager favorites, GagType gag)
    {
        var isFavorite = favorites._favoriteGags.Contains(gag);
        var hovering = ImGui.IsMouseHoveringRect(
            ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()));

        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        using var c = ImRaii.PushColor(ImGuiCol.Text, hovering 
            ? ImGuiColors.DalamudWhite2 : isFavorite ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3);

        ImGui.TextUnformatted(FontAwesomeIcon.Star.ToIconString());
        if (!ImGui.IsItemClicked())
            return false;

        if (isFavorite)
            favorites._favoriteGags.Remove(gag);
        else
            favorites.TryAddGag(gag);
        return true;
    }

    public static bool DrawFavoriteStar(FavoritesManager favorites, string kinksterUid)
    {
        var isFavorite = favorites._favoriteKinksters.Contains(kinksterUid);
        var hovering = ImGui.IsMouseHoveringRect(
            ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()));

        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        using var c = ImRaii.PushColor(ImGuiCol.Text, hovering 
            ? ImGuiColors.DalamudWhite2 : isFavorite ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3);

        ImGui.TextUnformatted(FontAwesomeIcon.Star.ToIconString());
        if (!ImGui.IsItemClicked())
            return false;

        if (isFavorite)
            favorites._favoriteKinksters.Remove(kinksterUid);
        else
            favorites.TryAddKinkster(kinksterUid);
        return true;
    }
}

