using CkCommons;
using CkCommons.Textures;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.GameInternals;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;

namespace GagSpeak.Services;

// MAINTAINERS NOTE:
// Modifying the ORIGIN_X and ORIGIN_Y values of an image allow you to perminantly altar their changed location.
// using SetPositionFloat and other methods will NOT change this. Calculate the offsets with the ORIGIN, for persisted changes.
/// <summary>
///     Monitors and controls the state of the nameplate.
/// </summary>
public sealed class NameplateService : DisposableMediatorSubscriberBase
{
    private enum DisplayMode { JobIcon, AboveName }

    private readonly MainConfig _config;
    private readonly ClientData _clientData;
    private readonly GagRestrictionManager _gags;
    private readonly GagspeakEventManager _events;
    private readonly KinksterManager _kinksters;
    private readonly OnFrameworkService _frameworkUtils;

    // The value is if the kinkster is currently speaking or not, this can be accessed asyncronously by async void timeouts for speech.
    private ConcurrentDictionary<string, bool> TrackedKinksters = new();
    private IDalamudTextureWrap GaggedIcon;
    private IDalamudTextureWrap GaggedSpeakingIcon;

    public NameplateService(ILogger<NameplateService> logger, GagspeakMediator mediator,
        MainConfig config, ClientData clientData, GagRestrictionManager gags, 
        GagspeakEventManager events, KinksterManager kinksters, OnFrameworkService frameworkUtils)
        : base(logger, mediator)
    {
        _config = config;
        _clientData = clientData;
        _gags = gags;
        _events = events;
        _kinksters = kinksters;
        _frameworkUtils = frameworkUtils;

        // Texture Aquisition.
        var gaggedPath = Path.Combine(TextureManager.AssetFolderPath, "RequiredImages", "status_gagged.png");
        var speakingPath = Path.Combine(TextureManager.AssetFolderPath, "RequiredImages", "status_gagged_speaking.png");
        GaggedIcon = Svc.Texture.GetFromFile(gaggedPath).RentAsync().Result;
        GaggedSpeakingIcon = Svc.Texture.GetFromFile(speakingPath).RentAsync().Result;

        // Events
        _events.Subscribe<int, GagType, bool, string>(UnlocksEvent.GagStateChange, UpdateClientGagState);
        _events.Subscribe<int, GagType, bool, string, Kinkster>(UnlocksEvent.PairGagStateChange, UpdateKinkster);

        Mediator.Subscribe<VisibleKinkstersChanged>(this, _ => UpdateGaggedKinksters());
        Mediator.Subscribe<ChatboxMessageFromSelf>(this, m => OnOwnMessage(m.channel, m.message));
        Mediator.Subscribe<ChatboxMessageFromKinkster>(this, m => OnKinksterMessage(m.kinkster, m.channel, m.message));
        Mediator.Subscribe<MainHubConnectedMessage>(this, _ => RefreshClientGagState());

        Svc.NamePlate.OnPostNamePlateUpdate += NamePlateOnPostUpdate;
        Logger.LogInformation("NameplateService initialized.");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.NamePlate.OnPostNamePlateUpdate -= NamePlateOnPostUpdate;

        // perform one final rescan after clearing the dictionary to remove all icons.
        TrackedKinksters.Clear();
        Svc.NamePlate.RequestRedraw();

        _events.Unsubscribe<int, GagType, bool, string>(UnlocksEvent.GagStateChange, UpdateClientGagState);
        _events.Unsubscribe<int, GagType, bool, string, Kinkster>(UnlocksEvent.PairGagStateChange, UpdateKinkster);

        GaggedIcon?.Dispose();
        GaggedSpeakingIcon?.Dispose();
    }

    public void RefreshClientGagState()
        => UpdateClientGagState(0, 0, _gags.ServerGagData?.IsGagged() ?? false, string.Empty);

    private void UpdateClientGagState(int _, GagType __, bool applied, string ___)
    {
        if (ClientData.Globals is not { } g)
            return;

        if (g.GaggedNameplate && applied)
        {
            Logger.LogDebug($"Adding {PlayerData.NameWithWorldInstanced} to tracked Nameplates", LoggerType.Gags);
            TrackedKinksters.TryAdd(PlayerData.NameWithWorldInstanced, false);
        }
        else if (_gags.ServerGagData is { } data && data.IsGagged())
        {
            // do nothing, they are still gagged.
            return;
        }
        else
        {
            Logger.LogDebug($"Removing {PlayerData.NameWithWorldInstanced} to tracked Nameplates", LoggerType.Gags);
            TrackedKinksters.Remove(PlayerData.NameWithWorldInstanced, out var ____);
        }
        Svc.NamePlate.RequestRedraw();
    }

    private void UpdateKinkster(int _, GagType __, bool applied, string ___, Kinkster k)
    {
        if (!k.PairGlobals.GaggedNameplate)
            return;

        Logger.LogDebug($"Updating kinkster gag state for {k.PlayerNameWithWorld} to {applied}", LoggerType.Gags);
        if (applied)
        {
            Logger.LogDebug($"Adding {k.PlayerNameWithWorld} to tracked Nameplates", LoggerType.Gags);
            TrackedKinksters.TryAdd(k.PlayerNameWithWorld, false);
        }
        else if (k.ActiveGags.IsGagged())
        {
            // do nothing, they are still gagged.
            return;
        }
        else
        {
            Logger.LogDebug($"Removing {k.PlayerNameWithWorld} to tracked Nameplates", LoggerType.Gags);
            TrackedKinksters.Remove(k.PlayerNameWithWorld, out var ____);
        }
        Svc.NamePlate.RequestRedraw();
    }

    private void UpdateGaggedKinksters()
    {
        // assume a local copy of the kinksters, in which the remaining kinksters are gagged with an active chat garbler.
        var visibleKinksters = _kinksters.DirectPairs
            .Where(k => k.VisiblePairGameObject is not null)
            .Where(k => k.ActiveGags.IsGagged() && k.PairGlobals.ChatGarblerActive);

        // assign them to the dictionary.
        var newTrackedKinksters = new ConcurrentDictionary<string, bool>();
        foreach (var kinkster in visibleKinksters)
        {
            // Make sure if they are still speaking that we keep the value the same.
            if (TrackedKinksters.TryGetValue(kinkster.PlayerNameWithWorld, out var isSpeaking))
                newTrackedKinksters.AddOrUpdate(kinkster.PlayerNameWithWorld, isSpeaking, (key, oldValue) => isSpeaking);
            else
                newTrackedKinksters.TryAdd(kinkster.PlayerNameWithWorld, false);
        }
        // Update the Tracked Kinksters here (we dont do it during recalculation because of concurrent access)
        TrackedKinksters = newTrackedKinksters;
        Svc.NamePlate.RequestRedraw();
    }

    private void OnOwnMessage(InputChannel c, string message)
    {
        if (_gags.ServerGagData is not { } data || ClientData.Globals is not { } g)
            return;

        // Discard if not a garbled message.
        if (!data.IsGagged() || !g.ChatGarblerActive || !g.AllowedGarblerChannels.IsActiveChannel((int)c))
            return;

        // Fire achievement if it was longer than 5 words and stuff.
        if (g.ChatGarblerActive && message.Split(' ').Length > 5)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.GaggedChatSent, c, message);

        DisplayGaggedSpeaking(PlayerData.NameWithWorldInstanced, (int)(650.0f * message.Length / 20.0f));
    }

    private void OnKinksterMessage(Kinkster k, InputChannel c, string message)
    {
        // Discard if not a garbled message.
        if (!k.ActiveGags.IsGagged() || !k.PairGlobals.ChatGarblerActive || !k.PairGlobals.AllowedGarblerChannels.IsActiveChannel((int)c))
            return;

        // Fire achievement if it was longer than 5 words and stuff.
        if (k.PairGlobals.ChatGarblerActive && message.Split(' ').Length > 5)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.KinksterGaggedChatSent, k, c, message);

        DisplayGaggedSpeaking(k.PlayerNameWithWorld, (int)(650.0f * message.Length / 20.0f));
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

    private unsafe void NamePlateOnPostUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        // Iterate through our handlers.
        foreach (var h in handlers)
        {
            // Skip if not player character, if if the player character is null.
            if (h.NamePlateKind is not NamePlateKind.PlayerCharacter || h.PlayerCharacter is not { } pc)
                continue;

            // Force the icon to be visible.
            var nmo = (AddonNamePlate.NamePlateObject*)h.NamePlateObjectAddress;
            if (!nmo->IsVisible)
                continue;

            // Otherwise, load the correct asset into the nameplate.
            var nameContainer = nmo->NameContainer;
            var nameIcon = nmo->NameIcon;

            // If they aint tracked, dont do nothin.
            var pnww = PlayerData.GetNameWithWorld(pc);
            if (!TrackedKinksters.TryGetValue(pnww, out var isSpeaking))
                continue;

            // Logger.LogTrace($"Nameplate for {pnww} is {(isSpeaking ? "speaking" : "not speaking")}");
            LoadTextureToAsset(nameIcon, nameContainer, isSpeaking);
            nameIcon->ToggleVisibility(true);
        }
    }

    private unsafe void LoadTextureToAsset(AtkImageNode* node, AtkResNode* parentNode, bool isSpeaking)
    {
        var texturePointer = (Texture*)Svc.Texture.ConvertToKernelTexture(isSpeaking ? GaggedSpeakingIcon: GaggedIcon, true);
        // Update the actual width to be reflected in resolution
        texturePointer->ActualWidth = 32;
        texturePointer->ActualHeight = 32;
        // If the parts list is less than 1 its a corruypted image so do nothing.
        if (node->PartsList->PartCount < 1)
        {
            Logger.LogTrace("Refusing to fetch image, part count is less than 1");
            return;
        }

        // Update the position.
        // (or not i guess because it likes to reload every frame you move your camera so this will be cancer to work with.)
        // node->OriginX += ((float)parentNode->Width / 2) - (32 / 2);

        // Release the old texture and replace it with the new one.
        node->PartsList->Parts[0].UldAsset->AtkTexture.ReleaseTexture();
        node->PartsList->Parts[0].UldAsset->AtkTexture.KernelTexture = texturePointer;
        node->PartsList->Parts[0].UldAsset->AtkTexture.TextureType = TextureType.KernelTexture;
    }
}

