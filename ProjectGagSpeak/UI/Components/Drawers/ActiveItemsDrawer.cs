using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.CustomCombos.Padlock;
using GagSpeak.Kinksters.Data;
using GagSpeak.State;
using GagSpeak.State.Listeners;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using System.Drawing;
using static Penumbra.GameData.Data.GamePaths;

namespace GagSpeak.CkCommons.Gui.Components;

public class ActiveItemsDrawer
{
    private readonly ILogger<ActiveItemsDrawer> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly FavoritesManager _favorites;
    private readonly TextureService _textures;
    private readonly CosmeticService _cosmetics;

    private RestrictionGagCombo[] _gagItems;
    private PadlockGagsClient[] _gagPadlocks;

    private RestrictionCombo[] _restrictionItems;
    private PadlockRestrictionsClient[] _restrictionPadlocks;

    private RestraintCombo _restraintItem;
    private PadlockRestraintsClient _restraintPadlocks;

    public ActiveItemsDrawer(
        ILogger<ActiveItemsDrawer> logger,
        GagspeakMediator mediator,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        FavoritesManager favorites,
        TextureService textures,
        CosmeticService cosmetics)
    {
        _logger = logger;
        _mediator = mediator;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _favorites = favorites;
        _textures = textures;
        _cosmetics = cosmetics;

        // Initialize the GagCombos.
        _gagItems = new RestrictionGagCombo[Constants.MaxGagSlots];
        for (var i = 0; i < _gagItems.Length; i++)
            _gagItems[i] = new RestrictionGagCombo(logger, favorites, () => [
                ..gags.Storage.Values.OrderByDescending(p => favorites._favoriteGags.Contains(p.GagType)).ThenBy(p => p.GagType)
            ]);

        // Init Gag Padlocks.
        _gagPadlocks = new PadlockGagsClient[Constants.MaxGagSlots];
        for (var i = 0; i < _gagPadlocks.Length; i++)
            _gagPadlocks[i] = new PadlockGagsClient(logger, mediator, gags);

        // Init Restriction Combos.
        _restrictionItems = new RestrictionCombo[Constants.MaxRestrictionSlots];
        for (var i = 0; i < _restrictionItems.Length; i++)
            _restrictionItems[i] = new RestrictionCombo(logger, favorites, () => [
                ..restrictions.Storage.OrderByDescending(p => favorites._favoriteRestrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
            ]);

        // Init Restriction Padlocks.
        _restrictionPadlocks = new PadlockRestrictionsClient[Constants.MaxRestrictionSlots];
        for (var i = 0; i < _restrictionPadlocks.Length; i++)
            _restrictionPadlocks[i] = new PadlockRestrictionsClient(logger, mediator, restrictions);

        // Init Restraint Combo & Padlock.
        _restraintItem = new RestraintCombo(logger, favorites, () => [
            ..restraints.Storage.OrderByDescending(p => favorites._favoriteRestraints.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        _restraintPadlocks = new PadlockRestraintsClient(logger, mediator, restraints);
    }

    public void DrawRestraintSlots(RestraintSet set, Vector2 iconFrameSize)
    {
        // Equip Group
        using (ImRaii.Group())
        {
            foreach (var slot in EquipSlotExtensions.EquipmentSlots)
            {
                // Draw out the item image first
                set.RestraintSlots[slot].EquipItem.DrawIcon(_textures, iconFrameSize, slot);
                // Draw out the border frame next.
                if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var slotBG))
                    ImGui.GetWindowDrawList().AddDalamudImageRounded(slotBG, ImGui.GetCursorScreenPos(), iconFrameSize, 10f);
            }
        }
        ImGui.SameLine(0, 1);
        // Accessory Group
        using (ImRaii.Group())
        {
            foreach (var slot in EquipSlotExtensions.AccessorySlots)
            {
                // Draw out the item image first
                set.RestraintSlots[slot].EquipItem.DrawIcon(_textures, iconFrameSize, slot);
                // Draw out the border frame next.
                if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var slotBG))
                    ImGui.GetWindowDrawList().AddDalamudImageRounded(slotBG, ImGui.GetCursorScreenPos(), iconFrameSize, 10f);
            }
        }
    }

    public void DisplayRestraintPadlock(Vector2 region)
    {
        if(_restraints.ServerRestraintData is not { } activeRestraint)
            return;

        // unlike gags or restrictions, these only display the locking, or unlocking, interface.
        if (activeRestraint.IsLocked())
            _restraintPadlocks.DrawUnlockCombo(region.X, "Unlock this Restraint!");
        else
            _restraintPadlocks.DrawLockCombo(region.X, "Lock this Restraint!");

    }

    // New Revised and Optimized displays:
    public void ApplyItemGroup(float height, int slotIdx, ActiveGagSlot data)
    {
        using var group = ImRaii.Group();

        // Draw out the framed image first.
        DrawFramedImage(data.GagItem, height, 10f);

        // Beside it begin a secondary group.
        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            // Center vertically the combo.
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((height - ImGui.GetFrameHeight()) / 2));
            var width = ImGui.GetContentRegionAvail().X;
            var combo = _gagItems[slotIdx];

            var change = combo.Draw($"##GagSelector-{slotIdx}", data.GagItem, width);
            if (change && combo.Current != null && data.GagItem != combo.Current.GagType)
            {
                // return if we are not allow to do the application.
                if (_gags.CanApply(slotIdx, combo.Current.GagType))
                {
                    var updateType = (data.GagItem is GagType.None) ? DataUpdateType.Applied : DataUpdateType.Swapped;
                    var newSlotData = new ActiveGagSlot()
                    {
                        GagItem = combo.Current.GagType,
                        Enabler = MainHub.UID,
                    };
                    _mediator.Publish(new GagDataChangedMessage(updateType, slotIdx, newSlotData));
                    _logger.LogTrace($"Requesting Server to update layer {slotIdx}'s Gag to {combo.Current.GagType} from {data.GagItem}");
                }
            }
        }
    }

    public void ApplyItemGroup(float height, int slotIdx, ActiveRestriction data)
    {
        using var group = ImRaii.Group();

        // Draw out the framed image first.
        DrawRestrictionImage(null, height, 10f);
        // Perform actions based on the itemRect. (In this case, clear the gag.)
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _restrictions.CanRemove(slotIdx))
        {
            _logger.LogTrace($"Restriction Layer {slotIdx} was cleared. and is now Empty");
            _mediator.Publish(new RestrictionDataChangedMessage(DataUpdateType.Removed, slotIdx, new ActiveRestriction()));
        }

        // Beside it begin a secondary group.
        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            // Center vertically the combo.
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((height - ImGui.GetFrameHeight()) / 2));
            var width = ImGui.GetContentRegionAvail().X;
            var combo = _restrictionItems[slotIdx];

            var change = combo.Draw($"##Restrictions-{slotIdx}", data.Identifier, width);
            if (change && combo.Current != null && data.Identifier != combo.Current.Identifier)
            {
                // return if we are not allow to do the application.
                if (_restrictions.CanApply(slotIdx))
                {
                    var updateType = combo.Current.Identifier.IsEmptyGuid() ? DataUpdateType.Applied : DataUpdateType.Swapped;
                    var newSlotData = new ActiveRestriction()
                    {
                        Identifier = combo.Current.Identifier,
                        Enabler = MainHub.UID,
                    };
                    _mediator.Publish(new RestrictionDataChangedMessage(updateType, slotIdx, newSlotData));
                    _logger.LogTrace($"Requesting Server to change restriction layer {slotIdx} to {combo.Current.Identifier} from {data.Identifier}");
                }
            }
        }
    }

    public void LockItemGroup(float height, int slotIdx, ActiveGagSlot data)
    {
        using var group = ImRaii.Group();

        // Draw out the framed image first.
        DrawFramedImage(data.GagItem, height, 10f);
        // We can remove if we are not yet locked (unlocked)
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _gags.CanRemove(slotIdx))
        {
            _logger.LogTrace($"Gag Layer {slotIdx} was cleared. and is now Empty");
            _mediator.Publish(new GagDataChangedMessage(DataUpdateType.Removed, slotIdx, new ActiveGagSlot()));
        }

        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        _gagPadlocks[slotIdx].DrawLockCombo(rightWidth, slotIdx, "Lock this Padlock!");
    }

    public void LockItemGroup(float height, int slotIdx, ActiveRestriction data, RestrictionItem dispData)
    {
        using var group = ImRaii.Group();
        // Draw out the framed image first.
        DrawRestrictionImage(dispData, height, 10f);
        // We can remove if we are not yet locked (unlocked)
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _restrictions.CanRemove(slotIdx))
        {
            _logger.LogTrace($"Restriction Layer {slotIdx} was cleared. and is now Empty");
            _mediator.Publish(new RestrictionDataChangedMessage(DataUpdateType.Removed, slotIdx, new ActiveRestriction()));
        }

        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        _restrictionPadlocks[slotIdx].DrawLockCombo(rightWidth, slotIdx, "Lock this Padlock!");
    }

    public void UnlockItemGroup(float height, int slotIdx, ActiveGagSlot data)
    {
        using var group = ImRaii.Group();
        var size = new Vector2(height);
        var padlockSize = size / 2;
        // Draw out the framed image first.
        DrawFramedImage(data.GagItem, height, 10f);
        ImGui.SetCursorScreenPos(ImGui.GetItemRectMin() + (size - padlockSize) / 2);
        DrawFramedImage(data.Padlock, padlockSize.X, padlockSize.X / 2);

        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        _gagPadlocks[slotIdx].DrawUnlockCombo(rightWidth, slotIdx, "Attempt to unlock this Padlock!");
    }

    public void UnlockItemGroup(float height, int slotIdx, ActiveRestriction data, RestrictionItem dispData)
    {
        using var group = ImRaii.Group();
        var size = new Vector2(height);
        var padlockSize = size / 2;
        // Draw out the framed image first.
        DrawRestrictionImage(dispData, height, 10f);
        ImGui.SetCursorScreenPos(ImGui.GetItemRectMin() + (size - padlockSize) / 2);
        DrawFramedImage(data.Padlock, padlockSize.X, padlockSize.X / 2);

        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        _restrictionPadlocks[slotIdx].DrawUnlockCombo(rightWidth, slotIdx, "Attempt to unlock this Padlock!");
    }

    public void DrawFramedImage(GagType gag, float size, float rounding, bool excludeFrame = false)
    {
        var gagImage = gag is GagType.None ? null : _cosmetics.GagImageFromType(gag);
        var gagFrame = _cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var frame) ? frame : null;
        DrawImageInternal(gagImage, gagFrame, size, rounding, excludeFrame);
    }

    public void DrawFramedImage(Padlocks padlock, float size, float rounding, bool excludeFrame = false)
    {
        var padlockImage = padlock is Padlocks.None ? null : _cosmetics.PadlockImageFromType(padlock);
        var padlockFrame = _cosmetics.TryGetBorder(ProfileComponent.Padlock, ProfileStyleBorder.Default, out var frame) ? frame : null;
        DrawImageInternal(padlockImage, padlockFrame, size, rounding, excludeFrame);
    }

    public void DrawRestrictionImage(RestrictionItem? restriction, float size, float rounding, bool excludeFrame = false)
    {
        // Attempt custom thumbnail.
        if (restriction != null && _cosmetics.GetImageMetadataPath(ImageDataType.Restrictions, restriction.ThumbnailPath) is { } image)
        {
            ImGuiHelpers.ScaledDummy(size);
            ImGui.GetWindowDrawList().AddDalamudImageRounded(image, ImGui.GetItemRectMin(), new Vector2(size), rounding);
        }
        else if (restriction != null && restriction.Glamour.Slot is not EquipSlot.Nothing)
        {
            restriction.Glamour.GameItem.DrawIcon(_textures, new Vector2(size), restriction.Glamour.Slot, rounding);
        }
        else
        {
            ImGuiHelpers.ScaledDummy(size);
        }

        // Fill out the frame.
        if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var frameWrap) && !excludeFrame)
            ImGui.GetWindowDrawList().AddDalamudImageRounded(frameWrap, ImGui.GetItemRectMin(), new Vector2(size), rounding);
    }

    public void DrawFramedImage(RestraintSet? rs, Vector2 size, float rounding, bool excludeFrame = false)
    {
        var image = rs is null ? null : _cosmetics.GetImageMetadataPath(ImageDataType.Restraints, rs.ThumbnailPath);
        DrawImageInternal(image, null, size, rounding);
    }

    public void DrawImageInternal(IDalamudTextureWrap? img, IDalamudTextureWrap? frame, float size, float rounding, bool excludeFrame = false)
        => DrawImageInternal(img, frame, new Vector2(size), rounding, excludeFrame);

    public void DrawImageInternal(IDalamudTextureWrap? img, IDalamudTextureWrap? frame, Vector2 size, float rounding, bool excludeFrame = false)
    {
        // Fill the area with the dummy region.
        ImGui.Dummy(size);
        var pos = ImGui.GetItemRectMin();

        // Now draw out the image, if we should.
        if(img is { } imageWrap)
            ImGui.GetWindowDrawList().AddDalamudImageRounded(imageWrap, pos, size, rounding);

        // Fill out the frame.
        if (frame is { } frameWrap && !excludeFrame)
            ImGui.GetWindowDrawList().AddDalamudImageRounded(frameWrap, pos, size, rounding);
    }
}
