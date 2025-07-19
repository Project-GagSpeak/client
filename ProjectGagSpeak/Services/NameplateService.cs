using CkCommons;
using CkCommons.Textures;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.GameInternals;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using Lumina.Excel.Sheets;
using NAudio.CoreAudioApi;

namespace GagSpeak.Services;

// MAINTAINERS NOTE:
// Modifying the ORIGIN_X and ORIGIN_Y values of an image allow you to perminantly altar their changed location.
// using SetPositionFloat and other methods will NOT change this. Calculate the offsets with the ORIGIN, for persisted changes.

public sealed class PlayerPortaitManager(ulong id) : IDisposable
{
    private readonly Dictionary<string, IDalamudTextureWrap> _portaitsPerJob = new();
    private readonly IDalamudTextureWrap? _adventurePlateWrap = null;

    public readonly ulong ContentID = id;
    public IReadOnlyDictionary<string, IDalamudTextureWrap> Portraits => _portaitsPerJob;
    public IDalamudTextureWrap? AdventurePlateTexture { get; private set; } = null;

    public IDalamudTextureWrap? GetWrapForJob(string jobName)
    {
        if (_portaitsPerJob.TryGetValue(jobName, out var wrap))
            return wrap;
        Svc.Logger.Warning($"No portrait found for job {jobName} in PlayerPortaitManager.");
        return null;
    }

    public bool TrySetAdventurePlateWrap(string localPath)
    {
        var texture = Svc.Texture.GetFromFile(Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, localPath)).GetWrapOrDefault();
        if (texture is null)
        {
            Svc.Logger.Error($"Failed to load adventure plate texture from {localPath}");
            return false;
        }
        AdventurePlateTexture = texture;
        return true;
    }

    public bool SetWrapForJob(string jobName, IDalamudTextureWrap wrap)
    {
        if (_portaitsPerJob.ContainsKey(jobName))
        {
            _portaitsPerJob[jobName].Dispose();
            _portaitsPerJob[jobName] = wrap;
            return true;
        }
        _portaitsPerJob.Add(jobName, wrap);
        return false;
    }

    public void Dispose()
    {
        foreach (var wrap in _portaitsPerJob.Values)
        {
            wrap.Dispose();
        }
        _portaitsPerJob.Clear();
    }
}


public sealed class PortaitService : IDisposable
{
    private readonly ILogger<PortaitService> _logger;
    private Dictionary<ulong, PlayerPortaitManager> _playerPortaits = new();

    public PortaitService(ILogger<PortaitService> logger)
    {
        _logger = logger;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "CharaCard", AdventurePlatePostSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "CharaCard", AdventurePlatePreDraw);
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "CharaCard", AdventurePlatePostSetup);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, "CharaCard", AdventurePlatePreDraw);
        // Cleanup
        foreach (var portait in _playerPortaits.Values)
            portait.Dispose();

        _playerPortaits.Clear();
    }

    private void AdventurePlatePostSetup(AddonEvent type, AddonArgs args)
    {
        // grab info here & download the image
    }

    private unsafe void AdventurePlatePreDraw(AddonEvent type, AddonArgs args)
    {
        var charaCardStruct = (AgentCharaCard.Storage*)args.Addon;
        var cid = charaCardStruct->ContentId;

        // if the content ID is not present, return.
        if (!_playerPortaits.ContainsKey(cid))
            return;

        // /xldata -> Addon Inspector -> Depth Layer 5 -> CharaCard
        var charaCard = (AtkUnitBase*)args.Addon;

        // sample only covers ownID as a usecase for now.
        var ownId = Svc.ClientState.LocalContentId;

        // if the image isnt set, dont change it.
        if (!_playerPortaits.TryGetValue(ownId, out var portaitManager) || portaitManager.AdventurePlateTexture is not { } texture)
            return;

        // swap the texture.
        var portraitNode = (AtkComponentNode*)charaCard->GetNodeById(19);
        var portrait = (AtkImageNode*)portraitNode->Component->UldManager.SearchNodeById(2);
        // inject the texture we found.
        InjectJobTextureToAsset(portrait, texture);
    }

    private unsafe void InjectJobTextureToAsset(AtkImageNode* node, IDalamudTextureWrap wrap)
    {
        // If the parts list is less than 1 its a corruypted image so do nothing.
        if (node->PartsList->PartCount < 1)
            return;

        // get the original width and height of the texture.
        var width = node->PartsList->Parts[0].UldAsset->AtkTexture.KernelTexture->ActualWidth;
        var height = node->PartsList->Parts[0].UldAsset->AtkTexture.KernelTexture->ActualHeight;

        // Convert the texture to kernal, and make sure it keeps original dimentions.
        var texturePointer = (Texture*)Svc.Texture.ConvertToKernelTexture(wrap);
        // Update the actual width to be reflected in resolution
        texturePointer->ActualWidth = width;
        texturePointer->ActualHeight = height;

        // Release the original texture.
        node->PartsList->Parts[0].UldAsset->AtkTexture.ReleaseTexture();
        // Replace it with the new one and update the texture type.
        node->PartsList->Parts[0].UldAsset->AtkTexture.KernelTexture = texturePointer;
        node->PartsList->Parts[0].UldAsset->AtkTexture.TextureType = TextureType.KernelTexture;
    }
}



/// <summary>
///     Monitors and controls the state of the nameplate.
/// </summary>
public sealed class NameplateService : DisposableMediatorSubscriberBase
{
    private enum DisplayMode { JobIcon, AboveName }

    private readonly GagRestrictionManager _gags;
    private readonly MainConfig _mainConfig;
    private readonly GagspeakEventManager _events;
    private readonly KinksterManager _kinksters;
    private readonly OnFrameworkService _frameworkUtils;

    // The value is if the kinkster is currently speaking or not, this can be accessed asyncronously by async void timeouts for speech.
    private ConcurrentDictionary<string, bool> TrackedKinksters = new();
    private IDalamudTextureWrap GaggedIcon;
    private IDalamudTextureWrap GaggedSpeakingIcon;

    public NameplateService(ILogger<NameplateService> logger, GagspeakMediator mediator,
        GagRestrictionManager gags, MainConfig mainConfig, GagspeakEventManager events, 
        KinksterManager kinksters, OnFrameworkService frameworkUtils)
        : base(logger, mediator)
    {
        _gags = gags;
        _mainConfig = mainConfig;
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
        if (OwnGlobals.Perms is not { } globals)
            return;

        if (globals.GaggedNameplate && applied)
        {
            Logger.LogDebug($"Adding {PlayerData.NameWithWorldInstanced} to tracked Nameplates", LoggerType.Gags);
            TrackedKinksters.TryAdd(PlayerData.NameWithWorldInstanced, false);
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
            .Where(k => k.LastGagData.IsGagged() && k.PairGlobals.ChatGarblerActive);

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
        if (_gags.ServerGagData is not { } data || OwnGlobals.Perms is not { } perms)
            return;

        // Discard if not a garbled message.
        if (!data.IsGagged() 
            || !perms.ChatGarblerActive 
            || !perms.AllowedGarblerChannels.IsActiveChannel((int)c))
            return;

        // Fire achievement if it was longer than 5 words and stuff.
        if (perms.ChatGarblerActive && message.Split(' ').Length > 5)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.GaggedChatSent, c, message);

        DisplayGaggedSpeaking(PlayerData.NameWithWorldInstanced, (int)(650.0f * message.Length / 20.0f));
    }

    private void OnKinksterMessage(Kinkster k, InputChannel c, string message)
    {
        // Discard if not a garbled message.
        if (!k.LastGagData.IsGagged() || !k.PairGlobals.ChatGarblerActive || !k.PairGlobals.AllowedGarblerChannels.IsActiveChannel((int)c))
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

            Logger.LogTrace($"Nameplate for {pnww} is {(isSpeaking ? "speaking" : "not speaking")}");
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

