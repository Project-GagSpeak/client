using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.CustomCombos.Padlock;
using GagSpeak.Gui.Wardrobe;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using OtterGui.Text;
using OtterGui.Text.EndObjects;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Text.RegularExpressions;
using TerraFX.Interop.Windows;
using static Lumina.Data.Parsing.Layer.LayerCommon;
namespace GagSpeak.Gui.Components;

public class ActiveItemsDrawer
{
    private readonly ILogger<ActiveItemsDrawer> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly FavoritesManager _favorites;
    private readonly VisualStateListener _visuals;
    private readonly DistributorService _dds;
    private readonly TextureService _textures;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    private readonly KinksterManager _kinksters;
    private RestrictionGagCombo[] _gagItems;
    private PadlockGagsClient[] _gagPadlocks;

    private RestrictionCombo[] _restrictionItems;
    private PadlockRestrictionsClient[] _restrictionPadlocks;

    private RestraintCombo _restraintItem;
    private PadlockRestraintsClient _restraintPadlocks;

    private LayerFlagsWidget _layerFlagsWidget;

    public ActiveItemsDrawer(
        ILogger<ActiveItemsDrawer> logger,
        GagspeakMediator mediator,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        FavoritesManager favorites,
        VisualStateListener visuals,
        DistributorService dds,
        TextureService textures,
        CosmeticService cosmetics,
        KinksterManager kinksters,
        TutorialService guides)
    {
        _logger = logger;
        _mediator = mediator;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _favorites = favorites;
        _visuals = visuals;
        _dds = dds;
        _textures = textures;
        _cosmetics = cosmetics;
        _guides = guides;
        _kinksters = kinksters;

        // Initialize the GagCombos.
        _gagItems = new RestrictionGagCombo[Constants.MaxGagSlots];
        for (var i = 0; i < _gagItems.Length; i++)
            _gagItems[i] = new RestrictionGagCombo(logger, favorites, () => [
                ..gags.Storage.Values.OrderByDescending(p => favorites._favoriteGags.Contains(p.GagType)).ThenBy(p => p.GagType)
            ]);

        // Init Gag Padlocks.
        _gagPadlocks = new PadlockGagsClient[Constants.MaxGagSlots];
        for (var i = 0; i < _gagPadlocks.Length; i++)
            _gagPadlocks[i] = new PadlockGagsClient(logger, dds, visuals, gags);

        // Init Restriction Combos.
        _restrictionItems = new RestrictionCombo[Constants.MaxRestrictionSlots];
        for (var i = 0; i < _restrictionItems.Length; i++)
            _restrictionItems[i] = new RestrictionCombo(logger, mediator, favorites, () => [
                ..restrictions.Storage.OrderByDescending(p => favorites._favoriteRestrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
            ]);

        // Init Restriction Padlocks.
        _restrictionPadlocks = new PadlockRestrictionsClient[Constants.MaxRestrictionSlots];
        for (var i = 0; i < _restrictionPadlocks.Length; i++)
            _restrictionPadlocks[i] = new PadlockRestrictionsClient(logger, dds, visuals, restrictions);

        // Init Restraint Combo & Padlock.
        _restraintItem = new RestraintCombo(logger, mediator, favorites, () => [
            ..restraints.Storage.OrderByDescending(p => favorites._favoriteRestraints.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        _restraintPadlocks = new PadlockRestraintsClient(logger, dds, visuals, restraints);

        // Init Layer Editor Client.
        _layerFlagsWidget = new(FAI.LayerGroup, "ClientRestraintLayers", string.Empty);
    }

    private void GagComboChanged(RestrictionGagCombo combo, int slotIdx, GagType curr)
    {
        if (combo.Current is null || curr.Equals(combo.Current.GagType))
            return;
        if (!_gags.CanApply(slotIdx))
            return;

        var type = (curr is GagType.None) ? DataUpdateType.Applied : DataUpdateType.Swapped;
        var newDat = _gags.ServerGagData!.GagSlots[slotIdx] with { GagItem = combo.Current.GagType, Enabler = MainHub.UID };
        SelfBondageHelper.GagUpdateTask(slotIdx, newDat, type, _dds, _visuals);
    }

    private void RestrictionComboChanged(RestrictionCombo combo, int slotIdx, Guid curr)
    {
        if (combo.Current is null || curr.Equals(combo.Current.Identifier))
            return;
        if (!_restrictions.CanApply(slotIdx))
            return;

        var updateType = (curr == Guid.Empty) ? DataUpdateType.Applied : DataUpdateType.Swapped;
        var newData = _restrictions.ServerRestrictionData!.Restrictions[slotIdx] with
        {
            Identifier = combo.Current.Identifier,
            Enabler = MainHub.UID,
        };
        SelfBondageHelper.RestrictionUpdateTask(slotIdx, newData, updateType, _dds, _visuals);
    }

    private void RestraintComboChanged(Guid curr)
    {
        if (_restraintItem.Current is null || curr.Equals(_restraintItem.Current.Identifier))
            return;
        if (!_restraints.CanApply())
            return;

        var updateType = (curr == Guid.Empty) ? DataUpdateType.Applied : DataUpdateType.Swapped;
        var newData = _restraints.ServerData! with
        {
            Identifier = _restraintItem.Current.Identifier,
            Enabler = MainHub.UID,
        };
        SelfBondageHelper.RestraintUpdateTask(newData, updateType, _dds, _visuals);
    }

    public void ApplyItemGroup(int slotIdx, ActiveGagSlot data)
    {
        using var group = ImRaii.Group();

        var height = CkStyle.ThreeRowHeight();
        DrawFramedImage(data.GagItem, height, 10f, uint.MaxValue);

        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            // Center vertically the combo.
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((height - ImGui.GetFrameHeight()) / 2));
            var width = ImGui.GetContentRegionAvail().X;
            var combo = _gagItems[slotIdx];
            if (combo.Draw($"##GagSelector-{slotIdx}", data.GagItem, width))
                GagComboChanged(combo, slotIdx, data.GagItem);
        }
    }

    public void ApplyItemGroup(int slotIdx, ActiveRestriction data)
    {
        using var group = ImRaii.Group();

        var height = CkStyle.TwoRowHeight();
        DrawRestrictionImage(null, height, 10f);

        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            // Center vertically the combo.
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((height - ImGui.GetFrameHeight()) / 2));
            var combo = _restrictionItems[slotIdx];
            if (combo.Draw($"##Restrictions-{slotIdx}", data.Identifier, ImGui.GetContentRegionAvail().X))
                RestrictionComboChanged(combo, slotIdx, data.Identifier);
        }
    }

    public void ApplyItemGroup(CharaActiveRestraint data)
    {
        using var group = ImRaii.Group();

        if (_restraintItem.Draw($"##RestraintSetSelector", data.Identifier, ImGui.GetContentRegionAvail().X))
            RestraintComboChanged(data.Identifier);
    }

    public void LockItemGroup(int slotIdx, ActiveGagSlot data)
    {
        using var group = ImRaii.Group();

        var height = CkStyle.ThreeRowHeight();
        DrawFramedImage(data.GagItem, height, 10f, uint.MaxValue);
        var drawPos = ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X, 0);
        CkGui.AttachToolTip($"{data.GagItem.GagName()}--SEP--" +
            "--NL----COL--Left-Click--COL-- ⇒ Select another Gag." +
            "--NL----COL--Right-Click--COL-- ⇒ Clear active Gag.", color: ImGuiColors.ParsedGold);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            ImGui.OpenPopup($"##GagSelector-{slotIdx}");
        else if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _gags.CanRemove(slotIdx))
            SelfBondageHelper.GagUpdateTask(slotIdx, new ActiveGagSlot(), DataUpdateType.Removed, _dds, _visuals);

        // Draw out padlocks selections.
        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        _gagPadlocks[slotIdx].DrawLockCombo(rightWidth, slotIdx, "Lock this Padlock!");

        // Draw out the potential popup if we should.
        var applyCombo = _gagItems[slotIdx];
        if (_gagItems[slotIdx].DrawPopup($"##GagSelector-{slotIdx}", data.GagItem, rightWidth * .9f, drawPos))
            GagComboChanged(applyCombo, slotIdx, data.GagItem);
    }

    public void LockItemGroup(int slotIdx, ActiveRestriction data, RestrictionItem? dispData)
    {
        using var group = ImRaii.Group();

        var height = CkStyle.TwoRowHeight();
        // Draw out the framed image first.
        DrawRestrictionImage(dispData, height, 10f);
        var drawPos = ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X, 0);
        string? nickEnabler; _kinksters.TryGetNickAliasOrUid(data.Enabler, out nickEnabler);
        CkGui.AttachToolTip($"{StripFormattingCodes(dispData?.Label) ?? "Unknown Restriction"} applied by {nickEnabler}" +
            "--SEP----COL--Left-Click--COL-- ⇒ Select another Restriction Item." +
            "--NL----COL--Right-Click--COL-- ⇒ Clear active Restriction Item.", color: ImGuiColors.ParsedGold);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            ImGui.OpenPopup($"##Restrictions-{slotIdx}");
        else if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _restrictions.CanRemove(slotIdx))
            SelfBondageHelper.RestrictionUpdateTask(slotIdx, new ActiveRestriction(), DataUpdateType.Removed, _dds, _visuals);

        ImUtf8.SameLineInner();
        var rightWidth = ImGui.GetContentRegionAvail().X;
        _restrictionPadlocks[slotIdx].DrawLockCombo(rightWidth, slotIdx, "Lock this Padlock!");

        // Draw the potential popup if we should.
        var applyCombo = _restrictionItems[slotIdx];
        if (applyCombo.DrawPopup($"##Restrictions-{slotIdx}", data.Identifier, rightWidth * .75f, drawPos))
            RestrictionComboChanged(applyCombo, slotIdx, data.Identifier);
    }

    public void LockItemGroup(CharaActiveRestraint data, RestraintSet dispData)
    {
        using var group = ImRaii.Group();
        _restraintPadlocks.DrawLockCombo(ImGui.GetContentRegionAvail().X, "Lock this Padlock!");
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.LockingRestraint, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
            () =>
            {
                /* select metal padlock and lock the restraint set. 
                probably easier to just tell the server to put a metal padlock on instead of trying to configure gui nonsense*/
            });

        var height = ImGui.GetFrameHeightWithSpacing() * 5 + ImGui.GetFrameHeight();
        DrawRestraintImage(dispData, new Vector2(height / 1.2f, height), CkStyle.ChildRoundingLarge(), CkColor.FancyHeaderContrast.Uint());
        var drawPos = ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X, 0);
        CkGui.AttachToolTip("--NL----COL--Left-Click--COL-- ⇒ Select another Restraint Set." +
                      "--NL----COL--Right-Click--COL-- ⇒ Clear active Restraint Set.", color: ImGuiColors.ParsedGold);
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.RemovingRestraints, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            ImGui.OpenPopup($"##RestraintSetSelector");
        else if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _restraints.CanRemove())
            SelfBondageHelper.RestraintUpdateTask(new CharaActiveRestraint(), DataUpdateType.Removed, _dds, _visuals);

        ImUtf8.SameLineInner();
        using var s = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, CkStyle.ChildRoundingLarge());
        using (ImRaii.Group())
        {
            // draw sync button, and call layer update if pressed.
            if (_layerFlagsWidget.DrawUpdateButton(FAI.Sync, "Update Layers", out var added, out var removed, ImGui.GetContentRegionAvail().X))
            {
                var newData = new CharaActiveRestraint() { ActiveLayers = (data.ActiveLayers | added) & ~removed };
                SelfBondageHelper.RestraintUpdateTask(newData, DataUpdateType.LayersChanged, _dds, _visuals);
            }

            // Below draw out the layers.
            var options = Enum.GetValues<RestraintLayer>().Skip(1).SkipLast(5 - dispData.Layers.Count + 1);
            _layerFlagsWidget.DrawLayerCheckboxes(data.ActiveLayers, options, _ =>
            {
                var idx = BitOperations.TrailingZeroCount((int)_); return (idx < dispData.Layers.Count) && (!dispData.Layers[idx].Label.IsNullOrWhitespace()) ? dispData.Layers[idx].Label : $"Layer {idx + 1}";
            });
        }
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.EditingLayers, ImGui.GetWindowPos(), ImGui.GetWindowSize());
    }

    public void UnlockItemGroup(int slotIdx, ActiveGagSlot data)
    {
        using var group = ImRaii.Group();

        var isTimer = data.Padlock.IsTimerLock();
        var size = new Vector2(CkStyle.ThreeRowHeight());
        var padlockSize = new Vector2(CkStyle.TwoRowHeight());
        var offsetV = ImGui.GetFrameHeight() / 2;

        using (ImRaii.Group())
        {
            // Draw out the framed image first.
            DrawFramedImage(data.GagItem, size.X, 10f);
            var gagDispPos = ImGui.GetItemRectMin();
            // Go back and show the image.
            ImGui.SetCursorScreenPos(gagDispPos + new Vector2(size.X * .75f, offsetV));
            DrawFramedImage(data.Padlock, padlockSize.X, padlockSize.X / 2);
        }
        CkGui.AttachToolTip($"{data.GagItem.GagName()} applied by {data.Enabler}" +
            $"--NL--Locked with {(data.Padlock == Padlocks.Owner || data.Padlock == Padlocks.OwnerTimer ? "an " : "a ")}--COL--{data.Padlock.ToName()}--COL-- by {data.PadlockAssigner}",
            color: ImGuiColors.ParsedPink);

        // Move over the distance of the framed image.
        ImGui.SameLine();
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
    }

    public void UnlockItemGroup(int slotIdx, ActiveRestriction data, RestrictionItem? dispData)
    {
        using var group = ImRaii.Group();

        var isTimer = data.Padlock.IsTimerLock();
        var size = new Vector2(CkStyle.TwoRowHeight());
        var offsetV = ImGui.GetFrameHeight() * 0.5f;
        var padlockSize = new Vector2(size.Y - offsetV);
        _kinksters.TryGetNickAliasOrUid(data.Enabler, out var nickEnabler);
        _kinksters.TryGetNickAliasOrUid(data.PadlockAssigner, out var lockEnabler);
        

        using (ImRaii.Group())
        {
            // Draw out the framed image first.
            DrawRestrictionImage(dispData, size.X, 10f);
            var gagDispPos = ImGui.GetItemRectMin();
            // Go back and show the image.
            ImGui.SetCursorScreenPos(gagDispPos + new Vector2(size.X * .75f, offsetV * .5f));
            DrawFramedImage(data.Padlock, padlockSize.X, padlockSize.X / 2);
        }
        // TODO: See if we can't find a way to properly cache the kinkster nicks speed up display
        CkGui.AttachToolTip($"{StripFormattingCodes(dispData?.Label) ?? "--COL----COL--Unknown Restriction"} applied by {nickEnabler ?? "yourself"}" +
            $"--NL--Locked with {(data.Padlock == Padlocks.Owner || data.Padlock == Padlocks.OwnerTimer ? "an " : "a ")}--COL--{data.Padlock.ToName()}--COL-- by {lockEnabler ?? "yourself"}",
            color: ImGuiColors.ParsedPink);
        if (dispData is null)
            CkGui.AttachToolTip("--SEP----COL--The item that was here couldn't be found.--NL--It may have been deleted or the data is corrupted.--COL--", color: ImGuiColors.DalamudRed);

        // Move over the distance of the framed image.
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetFrameHeight() / 2);
        _restrictionPadlocks[slotIdx].DrawUnlockCombo(ImGui.GetContentRegionAvail().X, slotIdx, "Attempt to unlock this Padlock!");
    }

    public void UnlockItemGroup(CharaActiveRestraint data, RestraintSet? dispData)
    {
        using var group = ImRaii.Group();
        var isTimer = data.Padlock.IsTimerLock();
        var size = new Vector2(CkStyle.TwoRowHeight());
        var offsetV = ImGui.GetFrameHeightWithSpacing() * 0.5f;

        // Go back and show the image.
        DrawFramedImage(data.Padlock, size.X, size.X / 2);
        CkGui.AttachToolTip($"Applied by {data.Enabler}" +
            $"--NL--Locked with {(data.Padlock == Padlocks.Owner || data.Padlock == Padlocks.OwnerTimer ? "an " : "a ")}--COL--{data.Padlock.ToName()}--COL-- by {data.PadlockAssigner}", color: ImGuiColors.ParsedPink);

        // Move over the distance of the framed image.
        ImGui.SameLine();
        using (ImRaii.Group())
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + offsetV);
            _restraintPadlocks.DrawUnlockCombo(ImGui.GetContentRegionAvail().X, "Attempt to unlock this Padlock!");
        }

        var height = ImGui.GetFrameHeightWithSpacing() * 5 + ImGui.GetFrameHeight();
        DrawRestraintImage(dispData, new Vector2(height / 1.2f, height), CkStyle.HeaderRounding(), CkColor.FancyHeaderContrast.Uint());

        ImUtf8.SameLineInner();
        //ImGui.SameLine(0,0);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, CkStyle.ChildRoundingLarge());
        using var _ = ImRaii.Disabled(data.PadlockAssigner != MainHub.UID);
        using (ImRaii.Group())
        {
            // draw sync button, and call layer update if pressed.
            if (_layerFlagsWidget.DrawUpdateButton(FAI.Sync, "Update Layers", out var added, out var removed, ImGui.GetContentRegionAvail().X))
            {
                // calculate the new layers by blending the current with the applied and removed layers.
                var newData = new CharaActiveRestraint() { ActiveLayers = (data.ActiveLayers | added) & ~removed };
                SelfBondageHelper.RestraintUpdateTask(newData, DataUpdateType.LayersChanged, _dds, _visuals);
            }
            // dont draw if display data is null.
            if (dispData is null)
                return;
            // Below draw out the layers.
            var options = Enum.GetValues<RestraintLayer>().Skip(1).SkipLast(1).Take(dispData.Layers.Count);
            _layerFlagsWidget.DrawLayerCheckboxes(data.ActiveLayers, options, _ =>
            {
                var idx = BitOperations.TrailingZeroCount((int)_); return (idx < dispData.Layers.Count) && (!dispData.Layers[idx].Label.IsNullOrWhitespace()) ? dispData.Layers[idx].Label : $"Layer {idx + 1}";
            });
        }
    }
    private readonly Regex FormattingCodesRegex = new($"({CkGui.TipSep}|{CkGui.TipNL}|{CkGui.TipCol})", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private string? StripFormattingCodes(string? input)
    {
        if (input is null) return null; // don't process null input, just pass it back.
        return FormattingCodesRegex.Replace(input, string.Empty);
    }

    public void DrawFramedImage(GagType gag, float size, float rounding, uint frameTint = uint.MaxValue)
    {
        var gagImage = gag is GagType.None ? null : TextureManagerEx.GagImage(gag);
        var gagFrame = _cosmetics.TryGetBorder(ProfileComponent.GagSlot, ProfileStyleBorder.Default, out var frameImg) ? frameImg : null;
        DrawImageInternal(gagImage, gagFrame, size, rounding, frameTint);
    }

    public void DrawFramedImage(Padlocks padlock, float size, float rounding, uint frameTint = uint.MaxValue, uint bgCol = 0xFF000000)
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

    // maybe condense these into a shared method at some point or something.
    public static void DrawRestraintImage(RestraintSet? rs, Vector2 size, float rounding, uint thumbnailBg = 0, bool doFrame = true)
    {
        if (rs != null && TextureManagerEx.GetMetadataPath(ImageDataType.Restraints, rs.ThumbnailPath) is { } imageWrap)
            DrawImageInternal(imageWrap, null, size, rounding, 0, thumbnailBg);
        else
        {
            ImGui.Dummy(size);
            ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), thumbnailBg, rounding);
        }
    }

    // These should be moved to static...
    public static void DrawCollarImage(GagSpeakCollar collar, Vector2 size, float rounding, uint thumbnailBg = 0, bool doFrame = true)
    {
        if (TextureManagerEx.GetMetadataPath(ImageDataType.Collar, collar.ThumbnailPath) is { } imageWrap)
            DrawImageInternal(imageWrap, null, size, rounding, 0, thumbnailBg);
        else
        {
            ImGui.Dummy(size);
            ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), thumbnailBg, rounding);
        }
    }

    public static void DrawImageInternal(IDalamudTextureWrap? img, IDalamudTextureWrap? frame, float size, float rounding, uint frameTint = 0, uint bgCol = 0, float padding = 0)
        => DrawImageInternal(img, frame, new Vector2(size), rounding, frameTint, bgCol, padding);

    public static void DrawImageInternal(IDalamudTextureWrap? img, IDalamudTextureWrap? frame, Vector2 size, float rounding, uint frameTint = 0, uint bgCol = 0, float padding = 0)
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

    public static void DrawImagePadded(IDalamudTextureWrap? img, Vector2 size, float rounding, float padding)
    {
        ImGui.Dummy(size);
        var pos = ImGui.GetItemRectMin();
        if (img is { } imageWrap)
            ImGui.GetWindowDrawList().AddDalamudImageRounded(imageWrap, pos + new Vector2(padding), size - new Vector2(padding * 2), rounding);
    }
}
