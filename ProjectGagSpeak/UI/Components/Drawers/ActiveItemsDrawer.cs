using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.CustomCombos.Padlock;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;
using Penumbra.GameData.Enums;

namespace GagSpeak.Gui.Components;

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
            _restrictionItems[i] = new RestrictionCombo(logger, mediator, favorites, () => [
                ..restrictions.Storage.OrderByDescending(p => favorites._favoriteRestrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
            ]);

        // Init Restriction Padlocks.
        _restrictionPadlocks = new PadlockRestrictionsClient[Constants.MaxRestrictionSlots];
        for (var i = 0; i < _restrictionPadlocks.Length; i++)
            _restrictionPadlocks[i] = new PadlockRestrictionsClient(logger, mediator, restrictions);

        // Init Restraint Combo & Padlock.
        _restraintItem = new RestraintCombo(logger, mediator, favorites, () => [
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
    public void ApplyItemGroup(int slotIdx, ActiveGagSlot data)
    {
        using var group = ImRaii.Group();

        var height = CkStyle.ThreeRowHeight();
        // Draw out the framed image first.
        DrawFramedImage(data.GagItem, height, 10f, uint.MaxValue);

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

    public void ApplyItemGroup(int slotIdx, ActiveRestriction data)
    {
        using var group = ImRaii.Group();

        var height = CkStyle.TwoRowHeight();
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
                    var updateType = combo.Current.Identifier == Guid.Empty ? DataUpdateType.Applied : DataUpdateType.Swapped;
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

    public void ApplyRestraintSetGroup(CharaActiveRestraint data)
    {
        using var group = ImRaii.Group();

        var width = ImGui.GetContentRegionAvail().X;
        var change = _restraintItem.Draw($"##RestraintSets", data.Identifier, width);
        if (change && _restraintItem.Current != null && !data.Identifier.Equals(_restraintItem.Current.Identifier))
        {
            // return if we are not allow to do the application.
            if (_restraints.CanApply(_restraintItem.Current.Identifier))
            {
                var updateType = data.Identifier == Guid.Empty ? DataUpdateType.Applied : DataUpdateType.Swapped;
                var newSlotData = new CharaActiveRestraint()
                {
                    Identifier = _restraintItem.Current.Identifier,
                    Enabler = MainHub.UID,
                };
                _mediator.Publish(new RestraintDataChangedMessage(updateType, newSlotData));
                _logger.LogTrace($"Requesting Server to change Restraint Set to {_restraintItem.Current.Identifier} from {data.Identifier}");
            }
        }
    }


    public void LockItemGroup(int slotIdx, ActiveGagSlot data)
    {
        using var group = ImRaii.Group();

        var height = CkStyle.ThreeRowHeight();
        // Draw out the framed image first.
        DrawFramedImage(data.GagItem, height, 10f, uint.MaxValue);
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

    public void LockItemGroup(int slotIdx, ActiveRestriction data, RestrictionItem dispData)
    {
        using var group = ImRaii.Group();

        var height = CkStyle.TwoRowHeight();
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

    public void UnlockItemGroup(int slotIdx, ActiveGagSlot data)
    {
        using var group = ImRaii.Group();

        var isTimer = data.Padlock.IsTimerLock();
        var size = new Vector2(CkStyle.ThreeRowHeight());
        var padlockSize = new Vector2(CkStyle.TwoRowHeight());
        var offsetV = ImGui.GetFrameHeight() / 2;
        // Draw out the framed image first.
        DrawFramedImage(data.GagItem, size.X, 10f);
        var gagDispPos = ImGui.GetItemRectMin();

        // Move over the distance of the framed image.
        ImGui.SameLine(0, padlockSize.X - (size.X * .2f));
        using (var c = CkRaii.Child($"UnlockGroup-{slotIdx}", new Vector2(ImGui.GetContentRegionAvail().X, CkStyle.TwoRowHeight())))
        {
            var centerWidth = c.InnerRegion.X - ImGui.GetFrameHeight();
            if (isTimer)
                CkGui.CenterColorTextAligned(data.Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink, centerWidth);
            else
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetFrameHeight());

            // Display the unlock row.
            _gagPadlocks[slotIdx].DrawUnlockCombo(c.InnerRegion.X, slotIdx, "Attempt to unlock this Padlock!");
        }

        // Go back and show the image.
        ImGui.SetCursorScreenPos(gagDispPos + new Vector2(size.X * .75f, offsetV));
        DrawFramedImage(data.Padlock, padlockSize.X, padlockSize.X / 2);
    }

    public void UnlockItemGroup(int slotIdx, ActiveRestriction data, RestrictionItem dispData)
    {
        using var group = ImRaii.Group();

        var isTimer = data.Padlock.IsTimerLock();
        var size = new Vector2(CkStyle.TwoRowHeight());
        var offsetV = ImGui.GetFrameHeight() * 0.5f;
        var padlockSize = new Vector2(size.Y - offsetV);

        // Draw out the framed image first.
        DrawRestrictionImage(dispData, size.X, 10f);
        var gagDispPos = ImGui.GetItemRectMin();

        // Move over the distance of the framed image.
        ImGui.SameLine(0, padlockSize.X - (size.X * .2f));
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetFrameHeight()/2);
        _restrictionPadlocks[slotIdx].DrawUnlockCombo(ImGui.GetContentRegionAvail().X, slotIdx, "Attempt to unlock this Padlock!");

        // Go back and show the image.
        ImGui.SetCursorScreenPos(gagDispPos + new Vector2(size.X * .75f, offsetV * .5f));
        DrawFramedImage(data.Padlock, padlockSize.X, padlockSize.X / 2);
    }

    public void DrawFramedImage(GagType gag, float size, float rounding, uint frameTint = 0)
    {
        var gagImage = gag is GagType.None ? null : TextureManagerEx.GagImage(gag);
        var gagFrame = _cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var frameImg) ? frameImg : null;
        DrawImageInternal(gagImage, gagFrame, size, rounding, frameTint);
    }

    public void DrawFramedImage(Padlocks padlock, float size, float rounding, uint frameTint = 0, uint bgCol = 0xFF000000)
    {
        var padlockImage = padlock is Padlocks.None ? null : TextureManagerEx.PadlockImage(padlock);
        var padlockFrame = _cosmetics.TryGetBorder(ProfileComponent.Padlock, ProfileStyleBorder.Default, out var frameImg) ? frameImg : null;
        DrawImageInternal(padlockImage, padlockFrame, size, rounding, frameTint, bgCol, size / 6);
    }

    public void DrawRestrictionImage(RestrictionItem? restriction, float size, float rounding, bool doFrame = true)
    {
        // Attempt custom thumbnail.
        if (restriction != null && TextureManagerEx.GetMetadataPath(ImageDataType.Restrictions, restriction.ThumbnailPath) is { } image)
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
        if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var frameWrap) && doFrame)
            ImGui.GetWindowDrawList().AddDalamudImageRounded(frameWrap, ImGui.GetItemRectMin(), new Vector2(size), rounding);
    }

    public void DrawRestraintImage(RestraintSet? rs, Vector2 size, float rounding, bool doFrame = true)
    {
        if(rs != null && TextureManagerEx.GetMetadataPath(ImageDataType.Restraints, rs.ThumbnailPath) is { } imageWrap)
            DrawImageInternal(imageWrap, null, size, rounding);
    }

    public void DrawImageInternal(IDalamudTextureWrap? img, IDalamudTextureWrap? frame, float size, float rounding, uint frameTint = 0, uint bgCol = 0, float padding = 0)
        => DrawImageInternal(img, frame, new Vector2(size), rounding, frameTint, bgCol, padding);

    public void DrawImageInternal(IDalamudTextureWrap? img, IDalamudTextureWrap? frame, Vector2 size, float rounding, uint frameTint = 0, uint bgCol = 0, float padding = 0)
    {
        // Fill the area with the dummy region.
        ImGui.Dummy(size);
        var pos = ImGui.GetItemRectMin();

        if (bgCol > 0)
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, bgCol, size.X);

        // Now draw out the image, if we should.
        if (img is { } imageWrap)
        {
            if (padding > 0)
                ImGui.GetWindowDrawList().AddDalamudImageRounded(imageWrap, pos + new Vector2(padding), size - new Vector2(padding * 2), rounding);
            else
                ImGui.GetWindowDrawList().AddDalamudImageRounded(imageWrap, pos, size, rounding);
        }

        // Fill out the frame.
        if (frameTint > 0 && frame is { } frameWrap)
            ImGui.GetWindowDrawList().AddDalamudImageRounded(frameWrap, pos, size, rounding, frameTint);
    }
}
