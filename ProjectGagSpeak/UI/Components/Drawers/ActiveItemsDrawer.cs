using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.CustomCombos.Padlockable;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Drawing;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.GroupPoseModule;

namespace GagSpeak.UI.Components;

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
        _gagItems = Enum.GetValues<GagLayer>()
            .Select(l => new RestrictionGagCombo(logger, favorites, () => [
                ..gags.Storage.Values.OrderByDescending(p => favorites._favoriteGags.Contains(p.GagType)).ThenBy(p => p.GagType)
            ]))
            .ToArray();

        _gagPadlocks = Enum.GetValues<GagLayer>()
            .Select(i => new PadlockGagsClient(logger, mediator, (layerIdx) =>
                gags.ActiveGagsData?.GagSlots[layerIdx] ?? new ActiveGagSlot()))
            .ToArray();

        // Init Restriction Combos.
        _restrictionItems = Enumerable.Range(0, 5)
            .Select(_ => new RestrictionCombo(logger, favorites, () => [
                ..restrictions.Storage.OrderByDescending(p => favorites._favoriteRestrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
                ]))
            .ToArray();
        _restrictionPadlocks = Enumerable.Range(0, 5)
            .Select(i => new PadlockRestrictionsClient(logger, mediator, restrictions, (slotIdx) =>
                restrictions.ActiveRestrictionsData?.Restrictions[slotIdx] ?? new ActiveRestriction()))
            .ToArray();

        // Init Restraint Combos.
        _restraintItem = new RestraintCombo(logger, favorites, () => [
            ..restraints.Storage.OrderByDescending(p => favorites._favoriteRestraints.Contains(p.Identifier)).ThenBy(p => p.Label) 
            ]);
        _restraintPadlocks = new PadlockRestraintsClient(logger, mediator, restraints, (_) => 
            restraints.ActiveRestraintData ?? new CharaActiveRestraint());
    }

    // Draw out all of the possible combos for active items.
    private RestrictionGagCombo[] _gagItems;
    private PadlockGagsClient[] _gagPadlocks;

    private RestrictionCombo[] _restrictionItems;
    private PadlockRestrictionsClient[] _restrictionPadlocks;

    private RestraintCombo _restraintItem;
    private PadlockRestraintsClient _restraintPadlocks;

    public void DisplayGagSlots(float width)
    {
        if (_gags.ActiveGagsData is not { } activeGagSlots)
            return;

        // get the current content height.
        var height = ImGui.GetContentRegionAvail().Y;
        // calculate the Y spacing for the items.
        var groupH = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2;
        var groupSpacing = (height - 3 * groupH) / 4;

        // Draw the Gag Slots.
        foreach (var (gagData, index) in activeGagSlots.GagSlots.WithIndex())
        {
            // Spacing.
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + groupSpacing);

            // Lock Display.
            if(gagData.GagItem is not GagType.None)
            {
                if (gagData.IsLocked())
                    GagSlotUnlockingUi(width, index, gagData);
                else
                    GagSlotLockingUi(width, index, gagData);
            }
            else
                GagSlotApplyOrRemoveUi(width, index, gagData);
        }
    }

    public void DisplayRestrictionSlots(float width)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var _ = ImRaii.Child("RestrictionSlotsChild", ImGui.GetContentRegionAvail(), false, WFlags.NoScrollbar);

        if (_restrictions.ActiveRestrictionsData is not { } activeRestrictionSlots)
            return;

        // get the current content height.
        var height = ImGui.GetContentRegionAvail().Y;
        // calculate the Y spacing for the items.
        var groupH = ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;
        var groupSpacing = (height - 5 * groupH) / 7;

        // Draw the Gag Slots.
        foreach (var (restrictionData, index) in activeRestrictionSlots.Restrictions.WithIndex())
        {
            // Spacing.
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + groupSpacing);

            // Lock Display. For here we want the thumbnail we provide for the restriction item, so find it.
            if (_restrictions.ActiveRestrictions.TryGetValue(index, out var item))
            {
                if (restrictionData.IsLocked())
                    RestrictionUnlockingUi(width, index, restrictionData, item);
                else
                    RestrictionLockingUi(width, index, restrictionData, item);
            }
            else
            {
                RestrictionApplyOrRemoveUi(width, index, restrictionData);
            }
        }
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
        if(_restraints.ActiveRestraintData is not { } activeRestraint)
            return;

        // unlike gags or restrictions, these only display the locking, or unlocking, interface.
        if (activeRestraint.IsLocked())
            _restraintPadlocks.DrawUnlockCombo(region.X, "Unlock this Restraint!");
        else
            _restraintPadlocks.DrawLockCombo(region.X, "Lock this Restraint!");

    }

    #region GagRestrictionDisplays
    public void GagSlotApplyOrRemoveUi(float width, int slotIdx, ActiveGagSlot gagData)
    {
        using var group = ImRaii.Group();
        var wdl = ImGui.GetWindowDrawList();
        var height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2;
        var gagCombo = _gagItems[slotIdx];
        var imgPos = ImGui.GetCursorScreenPos();
        var imgSize = new Vector2(height);
        // Draw out the gag image first, but only if it exists at the layer we want to draw at. (Otherwise draw nothing)
        DrawImage(gagData.GagItem, new Vector2(height), 10f, false, true);
        // Draw out the border frame next.
        if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var slotBG))
            wdl.AddDalamudImageRounded(slotBG, imgPos, imgSize, 10f);

        // Perform actions based on the itemRect. (In this case, clear the gag.)
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _gags.CanRemove((GagLayer)slotIdx))
        {
            _logger.LogTrace($"Gag Layer {slotIdx} was cleared. and is now Empty");
            _mediator.Publish(new GagDataChangedMessage(DataUpdateType.Removed, slotIdx, new ActiveGagSlot()));
        }

        // Item Combos and Interactions.
        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        using (ImRaii.Group())
        {
            ImGui.Dummy(new Vector2(rightWidth, ImGui.GetFrameHeight()));
            var change = gagCombo.Draw("##GagApplyRemove" + slotIdx, gagData.GagItem, rightWidth);
            if (change && gagCombo.CurrentSelection is not null && gagData.GagItem != gagCombo.CurrentSelection.GagType)
            {
                // return if we are not allow to do the application.
                if (_gags.CanApply((GagLayer)slotIdx, gagCombo.CurrentSelection.GagType))
                {
                    var updateType = (gagCombo.CurrentSelection.GagType is GagType.None) ? DataUpdateType.Applied : DataUpdateType.Swapped;
                    var newSlotData = new ActiveGagSlot()
                    {
                        GagItem = gagCombo.CurrentSelection.GagType,
                        Enabler = MainHub.UID,
                    };
                    _mediator.Publish(new GagDataChangedMessage(updateType, slotIdx, newSlotData));
                    _logger.LogTrace($"Requesting Server to change gag layer {(GagLayer)slotIdx} to {gagCombo.CurrentSelection.GagType} from {gagData.GagItem}");
                }
            }
        }
    }

    public void GagSlotLockingUi(float width, int slotIdx, ActiveGagSlot gagData)
    {
        using var group = ImRaii.Group();
        var wdl = ImGui.GetWindowDrawList();
        var height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2;
        var imgPos = ImGui.GetCursorScreenPos();
        var imgSize = new Vector2(height);
        DrawImage(gagData.GagItem, new Vector2(height), 10f, true, true);
        // Draw out the border frame next.
        if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var slotBG))
            wdl.AddDalamudImageRounded(slotBG, imgPos, imgSize, 10f);

        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        _gagPadlocks[slotIdx].DrawLockCombo(rightWidth, slotIdx, "Lock this Padlock!");
    }

    public void GagSlotUnlockingUi(float width, int slotIdx, ActiveGagSlot gagData)
    {
        using var group = ImRaii.Group();
        var wdl = ImGui.GetWindowDrawList();
        var height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2;
        var padlockSize = new Vector2(height / 2);
        var imgPos = ImGui.GetCursorScreenPos();
        var imgSize = new Vector2(height);
        DrawImage(gagData.GagItem, new Vector2(height), 10f, true, true);

        // Draw out the border frame next.
        if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var gagFrame))
            wdl.AddDalamudImageRounded(gagFrame, imgPos, imgSize, 10f);

        // now we must center for the lock and draw this as well.
        ImGui.SetCursorScreenPos(imgPos + new Vector2(height - padlockSize.X / 2, (height - padlockSize.Y) / 2));
        DrawPadlockImage(gagData, padlockSize);
        if (_cosmetics.TryGetBorder(ProfileComponent.Padlock, ProfileStyleBorder.Default, out var padlockFrame))
            wdl.AddDalamudImageRounded(padlockFrame, imgPos, imgSize, 10f);

        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        _gagPadlocks[slotIdx].DrawUnlockCombo(rightWidth, slotIdx, "Unlock this Padlock!");
    }
    #endregion GagRestrictionDisplays

    #region RestrictionDisplays
    public void RestrictionApplyOrRemoveUi(float width, int slotIdx, ActiveRestriction itemData)
    {
        using var group = ImRaii.Group();
        var wdl = ImGui.GetWindowDrawList();
        var height = ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;
        var imgPos = ImGui.GetCursorScreenPos();
        var imgSize = new Vector2(height);

        ImGui.Dummy(new Vector2(height));
        // Draw out the border frame next.
        if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var slotBG))
            wdl.AddDalamudImageRounded(slotBG, imgPos, imgSize, 10f);

        // Perform actions based on the itemRect. (In this case, clear the gag.)
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _restrictions.CanRemove(slotIdx))
        {
            _logger.LogTrace($"Restriction Layer {slotIdx} was cleared. and is now Empty");
            _mediator.Publish(new RestrictionDataChangedMessage(DataUpdateType.Removed, slotIdx, new ActiveRestriction()));
        }

        // Item Combos and Interactions.
        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        using (ImRaii.Group())
        {
            var combo = _restrictionItems[slotIdx];
            ImGui.Dummy(new Vector2(rightWidth, ImGui.GetFrameHeight()/2));
            var change = combo.Draw("##RestrictionApplyRemove" + slotIdx, itemData.Identifier, rightWidth);
            if (change && combo.CurrentSelection is not null && itemData.Identifier != combo.CurrentSelection.Identifier)
            {
                // return if we are not allow to do the application.
                if (_restrictions.CanApply(slotIdx))
                {
                    var updateType = combo.CurrentSelection.Identifier.IsEmptyGuid() 
                        ? DataUpdateType.Applied : DataUpdateType.Swapped;
                    var newSlotData = new ActiveRestriction()
                    {
                        Identifier = combo.CurrentSelection.Identifier,
                        Enabler = MainHub.UID,
                    };
                    _mediator.Publish(new RestrictionDataChangedMessage(updateType, slotIdx, newSlotData));
                    _logger.LogTrace($"Requesting Server to change restriction layer {slotIdx} to {combo.CurrentSelection.Identifier} from {itemData.Identifier}");
                }
            }
        }
    }

    public void RestrictionLockingUi(float width, int slotIdx, ActiveRestriction itemData, RestrictionItem internalData)
    {
        using var group = ImRaii.Group();
        var wdl = ImGui.GetWindowDrawList();
        var height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2;
        var imgPos = ImGui.GetCursorScreenPos();
        var imgSize = new Vector2(height);
        DrawImage(internalData, new Vector2(height), 10f, true);
        // Draw out the border frame next.
        if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var slotBG))
            wdl.AddDalamudImageRounded(slotBG, imgPos, imgSize, 10f);

        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        _restrictionPadlocks[slotIdx].DrawLockCombo(rightWidth, slotIdx, "Lock this Padlock!");
    }

    public void RestrictionUnlockingUi(float width, int slotIdx, ActiveRestriction itemData, RestrictionItem internalData)
    {
        using var group = ImRaii.Group();
        var wdl = ImGui.GetWindowDrawList();
        var height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2;
        var padlockSize = new Vector2(height / 2);
        var imgPos = ImGui.GetCursorScreenPos();
        var imgSize = new Vector2(height);
        DrawImage(internalData, new Vector2(height), 10f, true);

        // Draw out the border frame next.
        if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var gagFrame))
            wdl.AddDalamudImageRounded(gagFrame, imgPos, imgSize, 10f);

        // now we must center for the lock and draw this as well.
        ImGui.SetCursorScreenPos(imgPos + new Vector2(height - padlockSize.X / 2, (height - padlockSize.Y) / 2));
        DrawPadlockImage(itemData, padlockSize);
        if (_cosmetics.TryGetBorder(ProfileComponent.Padlock, ProfileStyleBorder.Default, out var padlockFrame))
            wdl.AddDalamudImageRounded(padlockFrame, imgPos, imgSize, 10f);

        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        _restrictionPadlocks[slotIdx].DrawUnlockCombo(rightWidth, slotIdx, "Attempt to unlock this Padlock!");
    }
    #endregion RestrictionDisplays

    public void DrawImage(GagType gag, Vector2 region, float rounding = 0f, bool drawBg = false, bool blockNone = true)
    {
        var pos = ImGui.GetCursorScreenPos();
        if(drawBg)
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + region, 0xFF000000, rounding);

        if (gag is GagType.None && blockNone)
        {
            ImGui.Dummy(region);
            return;
        }
        else if(_cosmetics.GagImageFromType(gag) is { } image)
        {
            ImGui.GetWindowDrawList().AddDalamudImageRounded(image, pos, region, rounding);
            ImGui.Dummy(region);
        }
    }

    public void DrawImage(RestrictionItem restrictionItem, Vector2 region, float rounding = 0f, bool drawBg = false)
    {
        var pos = ImGui.GetCursorScreenPos();
        if (drawBg)
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + region, 0xFF000000, rounding);

        if (_cosmetics.GetImageFromAssetsFolder(Path.Combine("Thumbnails", restrictionItem.ThumbnailPath)) is { } image)
        {
            ImGui.GetWindowDrawList().AddDalamudImageRounded(image, pos, region, rounding);
            ImGui.Dummy(region);
        }
        else
        {
            restrictionItem.Glamour.GameItem.DrawIcon(_textures, region, restrictionItem.Glamour.Slot);
        }
    }

    public void DrawImage(RestraintSet restraintItem, Vector2 region, float rounding = 0f, bool drawBg = false)
    {
        var pos = ImGui.GetCursorScreenPos();
        if (drawBg)
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + region, 0xFF000000, rounding);

        if (_cosmetics.GetImageFromAssetsFolder(Path.Combine("Thumbnails", restraintItem.ThumbnailPath)) is { } image)
            ImGui.GetWindowDrawList().AddDalamudImageRounded(image, pos, region, rounding);

        ImGui.Dummy(region);
    }


    public void DrawPadlockImage(IPadlockableRestriction gagData, Vector2 region, bool drawBg = false, bool blockNone = true)
    {
        var wdl = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();

        // Draw the backdrop.
        if (drawBg)
            wdl.AddRectFilled(pos, pos + region, 0xFF000000);

        // Draw the padlock.
        if (gagData.Padlock is Padlocks.None && blockNone)
        {
            ImGui.Dummy(region);
            return;
        }
        else if (_cosmetics.PadlockImageFromType(gagData.Padlock) is { } image)
        {
            ImGui.GetWindowDrawList().AddDalamudImageRounded(image, pos, region, region.X / 2);
            ImGui.Dummy(region);
        }
    }


}
