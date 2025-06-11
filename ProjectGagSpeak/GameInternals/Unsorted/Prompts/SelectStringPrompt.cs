using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.GameInternals.Addons;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Services.Configs;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase.Delegates;

namespace GagSpeak.Hardcore.ForcedStay;
public class SelectStringPrompt : SetupSelectListPrompt
{
    public SelectStringPrompt(ILogger<SelectStringPrompt> logger, IAddonLifecycle addonLifecycle, 
        IGameInteropProvider gameInterop, ITargetManager targets) 
        : base(logger, addonLifecycle, gameInterop, targets) 
    { }

    public override void Enable()
    {
        if(!Enabled)
        {
            base.Enable();
            _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", AddonSetup);
            _addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "SelectString", SetEntry);
        }
    }

    private void SetEntry(AddonEvent type, AddonArgs args)
    {
        try
        {
            MainConfigService.LastSeenListSelection = (MainConfigService.LastSeenListIndex < MainConfigService.LastSeenListEntries.Length)
                ? MainConfigService.LastSeenListEntries?[MainConfigService.LastSeenListIndex].Text ?? string.Empty
                : string.Empty;

            // If this was a cutscene skip, we should fail the conditional for the Cutscne
            if (MainConfigService.LastSeenNodeLabel.Contains("Skip cutscene", StringComparison.OrdinalIgnoreCase) 
             && MainConfigService.LastSeenListSelection.Contains("Yes", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogTrace("Cutscene Skip Detected, Halting Achievement WarriorOfLewd", LoggerType.Achievements);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.CutsceneInturrupted);
            }

            _logger.LogTrace($"SelectString: LastSeenListSelection={MainConfigService.LastSeenListSelection}, LastSeenListTarget={MainConfigService.LastSeenNodeLabel}");
        }
        catch { }
    }

    public override void Disable()
    {
        if(Enabled)
        {
            base.Disable();
            _addonLifecycle.UnregisterListener(AddonSetup);
            _addonLifecycle.UnregisterListener(SetEntry);
            _logger.LogInformation("Disabling SelectString!", LoggerType.HardcorePrompt);
        }
    }

    protected unsafe void AddonSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        _logger.LogInformation("Setting up SelectString!", LoggerType.HardcorePrompt);
        // Fetch the addon
        var addon = (AddonSelectString*)addonInfo.Base();
        // store node name
        var target = _targets.Target;
        var targetName = target != null ? target.Name.ExtractText() : string.Empty;
        MainConfigService.LastSeenNodeName = targetName;
        _logger.LogDebug("Node Name: " + targetName);
        // Store the node label,
        MainConfigService.LastSeenNodeLabel = AddonBaseString.ToText(addon);
        _logger.LogTrace("SelectString Label: " + MainConfigService.LastSeenNodeLabel, LoggerType.HardcorePrompt);
        // Store the last seen entries
        MainConfigService.LastSeenListEntries = AddonBaseString.GetEntries(addon).Select(x => (x.Index, x.Text)).ToArray();
        // Log all the list entries to the logger, split by \n
        _logger.LogDebug("SelectString: " + string.Join("\n", MainConfigService.LastSeenListEntries.Select(x => x.Text)), LoggerType.HardcorePrompt);
        // Grab the index it is found in.
        var index = GetMatchingIndex(AddonBaseString.GetEntries(addon).Select(x => x.Text).ToArray(), MainConfigService.LastSeenNodeLabel);
        if (index != null)
        {
            var entryToSelect = AddonBaseString.GetEntries(addon)[(int)index];
            StaticDetours.CallbackFuncFire((AtkUnitBase*)addon, true, entryToSelect.Index);
        }
    }
}


