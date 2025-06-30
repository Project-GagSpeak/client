using Dalamud.Game.Gui.NamePlate;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.GameInternals;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using static PInvoke.User32;

namespace GagSpeak.Services;

/// <summary>
///     Monitors and controls the state of the nameplate.
/// </summary>
public sealed class NameplateService : DisposableMediatorSubscriberBase
{
    private readonly GlobalPermissions _globals;
    private readonly GagRestrictionManager _gags;
    private readonly MainConfig _mainConfig;
    private readonly KinksterManager _kinksters;
    private readonly OnFrameworkService _frameworkUtils;

    // The value is if the kinkster is currently speaking or not, this can be accessed asyncronously by async void timeouts for speech.
    private ConcurrentDictionary<string, bool> TrackedKinksters = new();
    private IDalamudTextureWrap GaggedIcon;
    private IDalamudTextureWrap GaggedSpeakingIcon;
    private string GaggedIconPath = Path.Combine(ConfigFileProvider.AssemblyDirectory, "Assets", "status_gagged.png");
    private string GaggedSpeakingIconPath = Path.Combine(ConfigFileProvider.AssemblyDirectory, "Assets", "status_gagged_speaking.png");

    public NameplateService(ILogger<NameplateService> logger, GagspeakMediator mediator,
        GlobalPermissions globals, GagRestrictionManager gags, MainConfig mainConfig,
        KinksterManager kinksters, OnFrameworkService frameworkUtils)
        : base(logger, mediator)
    {
        _globals = globals;
        _gags = gags;
        _mainConfig = mainConfig;
        _kinksters = kinksters;
        _frameworkUtils = frameworkUtils;

        GaggedIcon = Svc.Texture.GetFromFile(GaggedIconPath).RentAsync().Result;
        GaggedSpeakingIcon = Svc.Texture.GetFromFile(GaggedSpeakingIconPath).RentAsync().Result;

        // Subscribers to update the list and gagstates.
        Mediator.Subscribe<VisibleKinkstersChanged>(this, _ => UpdateGaggedKinksters());

        // Mediator Subscriptions to handle chat messages that were sent.
        Mediator.Subscribe<ChatboxMessageFromSelf>(this, m => OnOwnMessage(m.channel, m.message));
        Mediator.Subscribe<ChatboxMessageFromKinkster>(this, m => OnKinksterMessage(m.kinkster, m.channel, m.message));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        GaggedIcon?.Dispose();
        GaggedSpeakingIcon?.Dispose();
    }

    private void UpdateGaggedKinksters()
    {
        // assume a local copy of the kinksters, in which the remaining kinksters are gagged with an active chat garbler.
        var visibleKinksters = _kinksters.DirectPairs
            .Where(k => k.VisiblePairGameObject is not null)
            .Where(k => k.LastGagData.IsGagged() && k.PairGlobals.ChatGarblerActive);

        // assign them to the dictionary.
        var newTrackedKinksters = new ConcurrentDictionary<string, bool>();
        foreach (var kinkster in visibleKinksters)
        {
            // Make sure if they are still speaking that we keep the value the same.
            if (TrackedKinksters.TryGetValue(kinkster.PlayerNameWithWorld, out var isSpeaking))
                newTrackedKinksters[kinkster.PlayerNameWithWorld] = isSpeaking;
            else
                newTrackedKinksters.TryAdd(kinkster.PlayerNameWithWorld, false);
        }
        // Update the Tracked Kinksters here (we dont do it during recalculation because of concurrent access)
        TrackedKinksters = newTrackedKinksters;
    }

    private void OnOwnMessage(InputChannel c, string message)
    {
        if (_gags.ServerGagData is not { } data || _globals.Current is not { } perms)
            return;

        // Discard if not a garbled message.
        if (!data.IsGagged() 
            || !perms.ChatGarblerActive 
            || !perms.AllowedGarblerChannels.IsActiveChannel((int)c))
            return;

        // Fire achievement if it was longer than 5 words and stuff.
        if (perms.ChatGarblerActive && message.Split(' ').Length > 5)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.GaggedChatSent, c, message);

        DisplayGaggedSpeaking(PlayerData.NameWithWorldInstanced, (message.Length / 30) * 250);
    }

    private void OnKinksterMessage(Kinkster k, InputChannel c, string message)
    {
        // Discard if not a garbled message.
        if (!k.LastGagData.IsGagged() || !k.PairGlobals.ChatGarblerActive || !k.PairGlobals.AllowedGarblerChannels.IsActiveChannel((int)c))
            return;

        // Fire achievement if it was longer than 5 words and stuff.
        if (k.PairGlobals.ChatGarblerActive && message.Split(' ').Length > 5)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.KinksterGaggedChatSent, k, c, message);

        DisplayGaggedSpeaking(k.PlayerNameWithWorld, (message.Length / 30) * 250);
    }

    private async void DisplayGaggedSpeaking(string playerNameWorld, int milliseconds)
    {
        // Add the key or update the value to true.
        TrackedKinksters.AddOrUpdate(playerNameWorld, true, (key, oldValue) => true);
        // request a nameplate update.
        Svc.NamePlate.RequestRedraw();
        // wait the desired milliseconds.
        await Task.Delay(milliseconds).ConfigureAwait(false);
        // Update the value to false (do not remove, we are still gagged)
        TrackedKinksters.TryUpdate(playerNameWorld, false, true);
        // request a nameplate update.
        Svc.NamePlate.RequestRedraw();
    }

    private unsafe void NamePlateOnUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        Logger.LogDebug("NamePlateGuiOnOnPostNamePlateUpdate");
        // Iterate through our handlers.
        foreach (var h in handlers)
        {
            // Skip if not player character, if if the player character is null.
            if (h.NamePlateKind is not NamePlateKind.PlayerCharacter || h.PlayerCharacter is not { } pc)
                continue;

            var nmo = (AddonNamePlate.NamePlateObject*)h.NamePlateObjectAddress;
            if (!nmo->IsVisible)
                continue;

            // If they aint tracked, dont do nothin.
            if (!TrackedKinksters.TryGetValue(PlayerData.GetNameWithWorld(pc), out var isSpeaking))
                return;

            // Otherwise, load the correct asset into the nameplate.
            var nameContainer = nmo->NameContainer;
            var nameIcon = nmo->NameIcon;
            LoadTextureToAsset(nameIcon, nameContainer, isSpeaking);
        }

        // Request a redraw.
        Svc.NamePlate.RequestRedraw();
    }

    private unsafe void LoadTextureToAsset(AtkImageNode* node, AtkResNode* parentNode, bool isSpeaking)
    {
        var texturePointer = (Texture*)Svc.Texture.ConvertToKernelTexture(isSpeaking ? GaggedSpeakingIcon : GaggedIcon, true);
        // Update the actual width to be reflected in resolution
        texturePointer->ActualWidth = 32;
        texturePointer->ActualHeight = 32;
        // If the parts list is less than 1 its a corruypted image so do nothing.
        if (node->PartsList->PartCount < 1)
            return;

        // Update the position.
        node->SetPositionFloat(((float)parentNode->Width / 2) - (32 / 2), 0f);
        // Release the old texture and replace it with the new one.
        node->PartsList->Parts[0].UldAsset->AtkTexture.ReleaseTexture();
        node->PartsList->Parts[0].UldAsset->AtkTexture.KernelTexture = texturePointer;
        node->PartsList->Parts[0].UldAsset->AtkTexture.TextureType = TextureType.KernelTexture;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("NameplateService started.");
        Svc.NamePlate.OnPostNamePlateUpdate += NamePlateOnUpdate;
        //Svc.NamePlate.OnNamePlateUpdate += NamePlateOnUpdate;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("NameplateService stopped.");
        Svc.NamePlate.OnPostNamePlateUpdate -= NamePlateOnUpdate;
        //Svc.NamePlate.OnNamePlateUpdate -= NamePlateOnUpdate;
        return Task.CompletedTask;
    }
}

