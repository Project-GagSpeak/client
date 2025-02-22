using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;

namespace GagSpeak.Hardcore.ForcedStay;
public class SelectStringPrompt : SetupSelectListPrompt
{
    private readonly ForcedStayCallback _callback;
    public SelectStringPrompt(ILogger<SelectStringPrompt> logger, GagspeakConfigService config, 
        ForcedStayCallback callback, IAddonLifecycle addonLifecycle, IGameInteropProvider gameInterop,
        ITargetManager targets) : base(logger, config, addonLifecycle, gameInterop, targets) 
    {
        _callback = callback;    
    }

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
            _config.LastSeenListSelection = (_config.LastSeenListIndex < _config.LastSeenListEntries.Length)
                ? _config.LastSeenListEntries?[_config.LastSeenListIndex].Text ?? string.Empty
                : string.Empty;

            // If this was a cutscene skip, we should fail the conditional for the Cutscne
            if (_config.LastSeenNodeLabel.Contains("Skip cutscene", StringComparison.OrdinalIgnoreCase) 
             && _config.LastSeenListSelection.Contains("Yes", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogTrace("Cutscene Skip Detected, Halting Achievement WarriorOfLewd", LoggerType.Achievements);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.CutsceneInturrupted);
            }

            _logger.LogTrace($"SelectString: LastSeenListSelection={_config.LastSeenListSelection}, LastSeenListTarget={_config.LastSeenNodeLabel}");
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
        _config.LastSeenNodeName = targetName;
        _logger.LogDebug("Node Name: " + targetName);
        // Store the node label,
        _config.LastSeenNodeLabel = AddonBaseString.ToText(addon);
        _logger.LogTrace("SelectString Label: " + _config.LastSeenNodeLabel, LoggerType.HardcorePrompt);
        // Store the last seen entries
        _config.LastSeenListEntries = AddonBaseString.GetEntries(addon).Select(x => (x.Index, x.Text)).ToArray();
        // Log all the list entries to the logger, split by \n
        _logger.LogDebug("SelectString: " + string.Join("\n", _config.LastSeenListEntries.Select(x => x.Text)), LoggerType.HardcorePrompt);
        // Grab the index it is found in.
        var index = GetMatchingIndex(AddonBaseString.GetEntries(addon).Select(x => x.Text).ToArray(), _config.LastSeenNodeLabel);
        if (index != null)
        {
            var entryToSelect = AddonBaseString.GetEntries(addon)[(int)index];
            ForcedStayCallback.Fire((AtkUnitBase*)addon, true, entryToSelect.Index);
        }
    }
}


