using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.PlayerClient;
using GagSpeak.GameInternals.Addons;
using GagSpeak.GameInternals.Detours;

using GagSpeak.Utils;

namespace GagSpeak.Game.Readers;
public class SelectStringPrompt : SetupSelectListPrompt
{
    public SelectStringPrompt(ILogger<SelectStringPrompt> logger, IAddonLifecycle addonLifecycle, 
        ITargetManager targets) : base(logger, addonLifecycle, targets) 
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
            MainConfig.LastSeenListSelection = (MainConfig.LastSeenListIndex < MainConfig.LastSeenListEntries.Length)
                ? MainConfig.LastSeenListEntries?[MainConfig.LastSeenListIndex].Text ?? string.Empty
                : string.Empty;

            // If this was a cutscene skip, we should fail the conditional for the Cutscne
            if (MainConfig.LastSeenNodeLabel.Contains("Skip cutscene", StringComparison.OrdinalIgnoreCase) 
             && MainConfig.LastSeenListSelection.Contains("Yes", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogTrace("Cutscene Skip Detected, Halting Achievement WarriorOfLewd", LoggerType.Achievements);
                GagspeakEventManager.AchievementEvent(UnlocksEvent.CutsceneInturrupted);
            }

            _logger.LogTrace($"SelectString: LastSeenListSelection={MainConfig.LastSeenListSelection}, LastSeenListTarget={MainConfig.LastSeenNodeLabel}");
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
        MainConfig.LastSeenNodeName = targetName;
        _logger.LogDebug("Node Name: " + targetName);
        // Store the node label,
        MainConfig.LastSeenNodeLabel = AddonBaseString.ToText(addon);
        _logger.LogTrace("SelectString Label: " + MainConfig.LastSeenNodeLabel, LoggerType.HardcorePrompt);
        // Store the last seen entries
        MainConfig.LastSeenListEntries = AddonBaseString.GetEntries(addon).Select(x => (x.Index, x.Text)).ToArray();
        // Log all the list entries to the logger, split by \n
        _logger.LogDebug("SelectString: " + string.Join("\n", MainConfig.LastSeenListEntries.Select(x => x.Text)), LoggerType.HardcorePrompt);
        // Grab the index it is found in.
        var index = GetMatchingIndex(AddonBaseString.GetEntries(addon).Select(x => x.Text).ToArray(), MainConfig.LastSeenNodeLabel);
        if (index != null)
        {
            var entryToSelect = AddonBaseString.GetEntries(addon)[(int)index];
            StaticDetours.CallbackFuncFire((AtkUnitBase*)addon, true, entryToSelect.Index);
        }
    }
}


