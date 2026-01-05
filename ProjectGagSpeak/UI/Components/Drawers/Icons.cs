using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using OtterGui;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace CkCommons.Widgets;

public static class Icons
{
    /// <summary> Draw a game icon display (not icon button or anything) </summary>
    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size, EquipSlot slot, bool doHover = true)
        => DrawIcon(item, textures, size, slot, 5 * ImGuiHelpers.GlobalScale, doHover);

    /// <summary> Draw a game icon display (not icon button or anything) </summary>
    public static void DrawIcon(this EquipItem item, TextureService texture, Vector2 size, EquipSlot slot, float rounding, bool doHover = true)
    {
        try
        {
            var isEmpty = item.PrimaryId.Id == 0;
            var (ptr, textureSize, empty) = texture.GetIcon(item, slot);
            if (empty)
            {
                var (bgColor, tint) = isEmpty
                    ? (ImGui.GetColorU32(ImGuiCol.FrameBg), Vector4.One)
                    : (ImGui.GetColorU32(ImGuiCol.FrameBgActive), new Vector4(0.3f, 0.3f, 0.3f, 1f));
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, bgColor, rounding);
                if (!ptr.IsNull)
                    ImGui.GetWindowDrawList().AddImageRounded(ptr, pos, pos + size, Vector2.Zero, Vector2.One, ColorHelpers.RgbaVector4ToUint(tint), rounding);
            }
            else
            {
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddImageRounded(ptr, pos, pos + size, Vector2.Zero, Vector2.One, 0xFFFFFFFF, rounding);
            }

            ImGui.Dummy(size);
            if (doHover && !empty)
                ImGuiUtil.HoverIconTooltip(ptr, textureSize, size);


        }
        catch (Bagagwa e)
        {
            Svc.Log.Error(e, "Error drawing icon");
        }
    }

    /// <summary> Draw a game icon to display for a Bonus Slot </summary>
    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size, BonusItemFlag slot, bool doHover = true)
        => DrawIcon(item, textures, size, slot, 5 * ImGuiHelpers.GlobalScale, doHover);

    /// <summary> Draw a game icon to display for a Bonus Slot </summary>
    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size, BonusItemFlag slot, float rounding, bool doHover = true)
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
            if (!ptr.IsNull)
                ImGui.GetWindowDrawList().AddImageRounded(ptr, pos, pos + size, Vector2.Zero, Vector2.One, ColorHelpers.RgbaVector4ToUint(tint), rounding);
        }
        else
        {
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddImageRounded(ptr, pos, pos + size, Vector2.Zero, Vector2.One, 0xFFFFFFFF, rounding);
        }

        ImGui.Dummy(size);
        if (doHover && !empty)
            ImGuiUtil.HoverIconTooltip(ptr, textureSize, size);
    }

    public static bool DrawFavoriteStar(FavoritesConfig favorites, FavoriteIdContainer type, Guid id, bool framed = true)
    {
        var isFavorite = type switch
        {
            FavoriteIdContainer.Restraint => favorites.Restraints.Contains(id),
            FavoriteIdContainer.Restriction => favorites.Restrictions.Contains(id),
            FavoriteIdContainer.CursedLoot => favorites.CursedLoot.Contains(id),
            FavoriteIdContainer.Pattern => favorites.Patterns.Contains(id),
            FavoriteIdContainer.Alarm => favorites.Alarms.Contains(id),
            FavoriteIdContainer.Trigger => favorites.Triggers.Contains(id),
            _ => false
        };
        var hovering = ImGui.IsMouseHoveringRect(
            ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()));

        var col = hovering ? ImGuiColors.DalamudGrey : isFavorite ? ImGuiColors.ParsedGold : ImGuiColors.ParsedGrey;
        
        if (framed)
            CkGui.FramedIconText(FAI.Star, col);
        else
            CkGui.IconText(FAI.Star, col);
        CkGui.AttachToolTip((isFavorite ? "Remove" : "Add") + " from Favorites.");
        
        if (hovering && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            if (isFavorite) favorites.RemoveRestriction(type, id);
            else favorites.TryAddRestriction(type, id);
            return true;
        }
        return false;
    }

    public static bool DrawFavoriteStar(FavoritesConfig favorites, GagType gag, bool framed = true)
    {
        var isFavorite = favorites.Gags.Contains(gag);
        var hovering = ImGui.IsMouseHoveringRect(
            ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()));

        var col = hovering ? ImGuiColors.DalamudGrey : isFavorite ? ImGuiColors.ParsedGold : ImGuiColors.ParsedGrey;
        
        if (framed)
            CkGui.FramedIconText(FAI.Star, col);
        else 
            CkGui.IconText(FAI.Star, col);
        CkGui.AttachToolTip((isFavorite ? "Remove" : "Add") + " from Favorites.");
        
        if (hovering && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            if (isFavorite) favorites.RemoveGag(gag);
            else favorites.TryAddGag(gag);
            return true;
        }
        return false;
    }

    public static bool DrawFavoriteStar(FavoritesConfig favorites, string kinksterUid, bool framed = true)
    {
        var isFavorite = favorites.Kinksters.Contains(kinksterUid); 
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(ImGui.GetTextLineHeight()));
        var col = hovering ? ImGuiColors.DalamudGrey2 : isFavorite ? ImGuiColors.ParsedGold : ImGuiColors.ParsedGrey;
        
        if (framed)
            CkGui.FramedIconText(FAI.Star, col);
        else 
            CkGui.IconText(FAI.Star, col);
        CkGui.AttachToolTip((isFavorite ? "Remove" : "Add") + " from Favorites.");
        
        if (hovering && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            if (isFavorite) favorites.RemoveKinkster(kinksterUid);
            else favorites.TryAddKinkster(kinksterUid);
            return true;
        }
        return false;
    }
}

