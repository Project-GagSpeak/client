using CkCommons.GarblerCore;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.DrawSystem;
using GagSpeak.DrawSystem;
using GagSpeak.GameInternals.Agents;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.Components;
using GagSpeak.Localization;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Permissions;
using OtterGui;
using OtterGui.Text;
using OtterGui.Text.Widget.Editors;

namespace GagSpeak.Gui.Settings;

public class SettingsModulesWardrobe
{
    private enum PluginUiTabs
    {
        Gags,
        Garbler,
        Restraints,
        Restrictions,
        CursedLoot,
    }

    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly WhitelistDrawSystem _whitelistDDS;

    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<PluginUiTabs> _tabs;

    public SettingsModulesWardrobe(GagspeakMediator mediator, MainHub hub,
        MainConfig config, WhitelistDrawSystem whitelistDDS)
    {
        _mediator = mediator;
        _hub = hub;
        _config = config;
        _whitelistDDS = whitelistDDS;

        _tabs = new StylizedTabbarBuilder<PluginUiTabs>()
            .AddTab(PluginUiTabs.Gags, "Gags")
            .AddTab(PluginUiTabs.Garbler, "Chat Garbler")
            .AddTab(PluginUiTabs.Restraints, "Restraints")
            .AddTab(PluginUiTabs.Restrictions, "Restrictions")
            .AddTab(PluginUiTabs.CursedLoot, "Cursed Loot")
            .Build();
    }

    public int NavbarIdx => (int)_tabs.TabSelection;

    public void SetNavbarIdx(int idx)
    {
        if (idx < 0 || idx >= Enum.GetValues<PluginUiTabs>().Length)
            return;
        _tabs.TabSelection = (PluginUiTabs)idx;
    }

    // All Content
    public void Draw(ImGuiWindowPtr winPtr)
    {
        // Draw out the tabs using the custom backgrounds and whatever else.
        _tabs.DrawTabs(TabBarFlags.MinimalGlow);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2f * ImGuiHelpers.GlobalScale);
        ImGui.Separator();
        ImGui.Spacing();
        using var _ = ImRaii.Child("main-notifs-inner");
        // Draw out the content based on the selected tab.
        switch (_tabs.TabSelection)
        {
            case PluginUiTabs.Gags:
                DrawGagOptions();
                break;
            case PluginUiTabs.Garbler:
                DrawGarblerOptions();
                break;
            case PluginUiTabs.Restraints:
                DrawRestraintOptions();
                break;
            case PluginUiTabs.Restrictions:
                DrawRestrictionOptions();
                break;
            case PluginUiTabs.CursedLoot:
                DrawCursedLootOptions();
                break;
        }
    }

    private void AssignGlobalPermChangeTask(GlobalPerms perms, string globalKey, object newValue)
        => UiService.SetUITask(async () => await PermHelper.ChangeOwnGlobal(_hub, perms, globalKey, newValue));

    private void DrawRestraintOptions()
    {
        CkGui.FontText("Restraints", Fonts.SubtitleFont);
        if (ClientData.Globals is not { } globals)
            return;

        var doWardrobe = globals.WardrobeEnabled;
        if (ImGui.Checkbox(GSLoc.Settings.Options.WardrobeActive, ref doWardrobe))
        {
            UiService.SetUITask(async () =>
            {
                var success = await PermHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.WardrobeEnabled), doWardrobe);
                // Otherwise, process the remaining permissions we should forcibly change if the new state is now false.
                if (success && !doWardrobe)
                {
                    // If wardrobe is disabled, we should also disable the visuals.
                    await PermHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.RestrictionVisuals), false);
                    await PermHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.RestraintSetVisuals), false);
                }
            });
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.WardrobeActiveTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

        var restraintSetVisuals = globals.RestraintSetVisuals;
        if (CkGui.Checkbox(GSLoc.Settings.Options.RestraintSetGlamour, ref restraintSetVisuals, !doWardrobe))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.RestraintSetVisuals), restraintSetVisuals);
        CkGui.HelpTextFramed(GSLoc.Settings.Options.RestraintSetGlamourTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

        var removeRestraintOnLockExpiration = _config.Data.RemoveRestraintOnTimerExpire;
        if (CkGui.Checkbox(GSLoc.Settings.Options.RestraintPadlockTimer, ref removeRestraintOnLockExpiration, !doWardrobe))
        {
            _config.Data.RemoveRestraintOnTimerExpire = removeRestraintOnLockExpiration;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.RestraintPadlockTimerTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);
    }

    private void DrawRestrictionOptions()
    {
        CkGui.FontText("Restrictions", Fonts.SubtitleFont);
        if (ClientData.Globals is not { } globals)
            return;

        var doWardrobe = globals.WardrobeEnabled;
        if (ImGui.Checkbox(GSLoc.Settings.Options.WardrobeActive, ref doWardrobe))
        {
            UiService.SetUITask(async () =>
            {
                var success = await PermHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.WardrobeEnabled), doWardrobe);
                // Otherwise, process the remaining permissions we should forcibly change if the new state is now false.
                if (success && !doWardrobe)
                {
                    // If wardrobe is disabled, we should also disable the visuals.
                    await PermHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.RestrictionVisuals), false);
                    await PermHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.RestraintSetVisuals), false);
                }
            });
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.WardrobeActiveTT, true);

        var restrictionVisuals = globals.RestrictionVisuals;
        if (CkGui.Checkbox(GSLoc.Settings.Options.RestrictionGlamours, ref restrictionVisuals, !doWardrobe))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.RestrictionVisuals), restrictionVisuals);
        CkGui.HelpTextFramed(GSLoc.Settings.Options.RestrictionGlamoursTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

        var removeRestrictionOnLockExpiration = _config.Data.RemoveRestrictionOnTimerExpire;
        if (CkGui.Checkbox(GSLoc.Settings.Options.RestrictionPadlockTimer, ref removeRestrictionOnLockExpiration, !doWardrobe))
        {
            _config.Data.RemoveRestrictionOnTimerExpire = removeRestrictionOnLockExpiration;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.RestrictionPadlockTimerTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

    }

    private void DrawGagOptions()
    {
        CkGui.FontText("Gags", Fonts.SubtitleFont);
        if (ClientData.Globals is not { } globals)
            return;

        var liveChatGarblerActive = globals.ChatGarblerActive;
        var gaggedNamePlates = globals.GaggedNameplate;
        var gagVisuals = globals.GagVisuals;
        var removeGagOnLockExpiration = _config.Data.RemoveGagOnTimerExpire;
        var garbleWordsNotInDictionary = _config.Data.GarbleWordsNotInDictionary;

        if (CkGui.Checkbox(GSLoc.Settings.Options.LiveChatGarbler, ref liveChatGarblerActive, globals.ChatGarblerLocked))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.ChatGarblerActive), liveChatGarblerActive);
        CkGui.HelpTextFramed(GSLoc.Settings.Options.LiveChatGarblerTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

        if (CkGui.Checkbox(GSLoc.Settings.Options.GaggedNameplates, ref gaggedNamePlates, globals.ChatGarblerLocked))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.GaggedNameplate), gaggedNamePlates);
        CkGui.HelpTextFramed(GSLoc.Settings.Options.GaggedNameplatesTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

        if (CkGui.Checkbox(GSLoc.Settings.Options.NotInDictionaryGarbling, ref garbleWordsNotInDictionary, globals.ChatGarblerLocked))
        {
            _config.Data.GarbleWordsNotInDictionary = garbleWordsNotInDictionary;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.NotInDictionaryGarblingTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

        if (ImGui.Checkbox(GSLoc.Settings.Options.GagGlamours, ref gagVisuals))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.GagVisuals), gagVisuals);
        CkGui.HelpTextFramed(GSLoc.Settings.Options.GagGlamoursTT);

        if (ImGui.Checkbox(GSLoc.Settings.Options.GagPadlockTimer, ref removeGagOnLockExpiration))
        {
            _config.Data.RemoveGagOnTimerExpire = removeGagOnLockExpiration;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.GagPadlockTimerTT, true);
    }

    private void DrawCursedLootOptions()
    {
        CkGui.FontText("Cursed Loot", Fonts.SubtitleFont);
        if (ClientData.Globals is not { } globals)
            return;

        var cursedLootEnabled = _config.Data.CursedLootUI;
        if (CkGui.Checkbox(GSLoc.Settings.Options.CursedLootActive, ref cursedLootEnabled, !globals.WardrobeEnabled))
        {
            _config.Data.CursedLootUI = cursedLootEnabled;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.CursedLootActiveTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

        var hcCursedLoot = _config.Data.CursedItemsApplyTraits;
        if (CkGui.Checkbox(GSLoc.Settings.Options.MimicsApplyTraits, ref hcCursedLoot, !globals.WardrobeEnabled))
        {
            _config.Data.CursedItemsApplyTraits = hcCursedLoot;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.MimicsApplyTraitsTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

        var hcCursedOverlays = _config.Data.CursedItemsApplyOverlays;
        if (CkGui.Checkbox(GSLoc.Settings.Options.MimicsApplyOverlays, ref hcCursedOverlays, !globals.WardrobeEnabled))
        {
            _config.Data.CursedItemsApplyOverlays = hcCursedOverlays;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.MimicsApplyOverlaysTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);

        var blindfoldMaxOpacity = _config.Data.OverlayMaxOpacity;
        var hardcoreEscape = _config.Data.HardcoreEscape;

        using (ImRaii.PushIndent())
        {
            // show a prettier value for the end user
            blindfoldMaxOpacity *= 100;
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat(GSLoc.Settings.Options.OverlayMaxOpacity, ref blindfoldMaxOpacity, 0f, 100f, "%.1f%%", ImGuiSliderFlags.AlwaysClamp))
            {
                _config.Data.OverlayMaxOpacity = blindfoldMaxOpacity / 100;
                _config.Save();
            }
            CkGui.HelpTextFramed(GSLoc.Settings.Options.OverlayMaxOpacityTT, true, hFlags: ImGuiHoveredFlags.AllowWhenDisabled);
        }
    }

    private void DrawGarblerOptions()
    {
        CkGui.FontText("Enabled Garbler Channels", Fonts.DefaultScaled);
        // do not draw the preferences if the globalpermissions are null.
        if (ClientData.Globals is not { } globals)
        {
            ImGui.Text("Globals is null! Returning early");
            return;
        }

        using var _ = ImRaii.Group();
        foreach (var (label, channels) in ChatLogAgent.SortedChannels)
        {
            ImGui.Text(label); // Show the group label

            for (var i = 0; i < channels.Length; i++)
            {
                var channel = channels[i];
                var enabled = globals.AllowedGarblerChannels.IsActiveChannel((int)channel);
                var checkboxLabel = channel.ToString();

                using (ImRaii.Disabled(globals.ChatGarblerLocked && enabled))
                {
                    if (ImGui.Checkbox(checkboxLabel, ref enabled))
                    {
                        var newBitfield = globals.AllowedGarblerChannels.SetChannelState((int)channel, enabled);
                        AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.AllowedGarblerChannels),
                                                    newBitfield);
                    }
                }

                // Only SameLine if not the third column
                if ((i + 1) % 4 != 0 && (i + 1) != channels.Length)
                    ImGui.SameLine();
            }
        }

        CkGui.TextFrameAligned(GSLoc.Settings.Options.LangDialectLabel);
        if (ImGuiUtil.GenericEnumCombo("##Language", 125, _config.Data.Language, out var newLang, i => i.ToName()))
        {
            if (newLang != _config.Data.Language)
                _config.Data.LanguageDialect = newLang.GetDialects().First();
            _config.Data.Language = newLang;
            _config.Save();
        }
        CkGui.AttachTooltip(GSLoc.Settings.Options.LangTT);

        ImGui.SameLine();
        if (ImGuiUtil.GenericEnumCombo("##Dialect", 125, _config.Data.LanguageDialect, out var newDialect,
            _config.Data.Language.GetDialects(), i => i.ToName()))
        {
            _config.Data.LanguageDialect = newDialect;
            _config.Save();
        }
        CkGui.AttachTooltip(GSLoc.Settings.Options.DialectTT);
    }
}
