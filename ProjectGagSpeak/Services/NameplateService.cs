using CkCommons;
using CkCommons.Textures;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.GameInternals;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
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
    private readonly GagRestrictionManager _gags;
    private readonly GagspeakEventManager _events;
    private readonly KinksterManager _kinksters;

    // The value is if the kinkster is currently speaking or not, this can be accessed asyncronously by async void timeouts for speech.
    private ConcurrentDictionary<string, bool> TrackedKinksters = new();
    private IDalamudTextureWrap GaggedIcon;
    private IDalamudTextureWrap GaggedSpeakingIcon;

    public NameplateService(ILogger<NameplateService> logger, GagspeakMediator mediator, MainConfig config,
        GagRestrictionManager gags, GagspeakEventManager events, KinksterManager kinksters)
        : base(logger, mediator)
    {
        _config = config;
        _gags = gags;
        _events = events;
        _kinksters = kinksters;

        // Texture Aquisition.
        var gaggedPath = Path.Combine(TextureManager.AssetFolderPath, "RequiredImages", "status_gagged.png");
        var speakingPath = Path.Combine(TextureManager.AssetFolderPath, "RequiredImages", "status_gagged_speaking.png");
        GaggedIcon = Svc.Texture.GetFromFile(gaggedPath).RentAsync().Result;
        GaggedSpeakingIcon = Svc.Texture.GetFromFile(speakingPath).RentAsync().Result;

        // All of these methods should perform an update to a persons Gagged State.
        // This accounts for both client and pair, on or off. (Supposedly) and should fix the issues we have with removals.
        Mediator.Subscribe<GagStateChanged>(this, _ => OnGagStateChange(_.State, _.Enactor, _.Target));

        // Whenever someone unrenders or renders, we should pull a refresh on the nameplates
        // Handle this in a way where we dont need to worry about keying by nameworld anymore or something.
        Mediator.Subscribe<KinksterPlayerRendered>(this, _ => RefreshGagPlates());
        Mediator.Subscribe<KinksterPlayerUnrendered>(this, _ => RefreshGagPlates());
        Mediator.Subscribe<ConnectedMessage>(this, _ => RefreshClientGagState());

        // Temporarily change the GagPlate state if talking while gagged! (Works for both client and pair)
        Mediator.Subscribe<GameChatMessage>(this, _ => OnGameChatMessage(_.Channel, _.SenderNameWorld, _.Msg.TextValue));

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
        GaggedIcon?.Dispose();
        GaggedSpeakingIcon?.Dispose();
    }

    public void RefreshClientGagState()
        => CheckUpdateClient(NewState.Disabled, string.Empty, MainHub.UID);

    private void OnGagStateChange(NewState newState, string enactor, string target)
    {
        if (target == MainHub.UID)
            CheckUpdateClient(newState, enactor, target);
        else
            CheckUpdateKinkster(newState, enactor, target);
    }

    private void CheckUpdateClient(NewState newState, string enactor, string target)
    {
        // Fail if globals not valid (not connected)
        if (ClientData.Globals is not { } g)
            return;

        // If we have gaggedNameplate settings on and the change was apply, we should attempt to add them to tracked.
        if (g.GaggedNameplate && newState is NewState.Enabled)
        {
            Logger.LogDebug($"Adding {PlayerData.NameWithWorld} to tracked Nameplates", LoggerType.Gags);
            TrackedKinksters.TryAdd(PlayerData.NameWithWorld, false);
        }
        // If we had gagged data and are gagged, we should proceed as normal
        else if (_gags.ServerGagData is { } data && data.IsGagged())
        {
            // Add it if we are not already added.
            TrackedKinksters.TryAdd(PlayerData.NameWithWorld, false);
        }
        // Otherwise, remove it
        else
        {
            Logger.LogDebug($"Removing {PlayerData.NameWithWorld} to tracked Nameplates", LoggerType.Gags);
            TrackedKinksters.Remove(PlayerData.NameWithWorld, out var _);
        }
        // Request a redraw afterwards to the latest state is reflected.
        Svc.NamePlate.RequestRedraw();
    }

    private void CheckUpdateKinkster(NewState newState, string enactor, string target)
    {
        // Attempt to grab the Kinkster associated with this targetted message
        if (!_kinksters.TryGetKinkster(new(target), out var kinkster))
            return;

        // If the kinkster is not rendered, we do not care (I think?)
        if (!kinkster.IsRendered)
            return;

        var hasNameplates = kinkster.PairGlobals.GaggedNameplate;
        if (hasNameplates && newState is NewState.Enabled)
        {
            Logger.LogDebug($"Adding {kinkster.PlayerNameWorld} to tracked Nameplates", LoggerType.Gags);
            TrackedKinksters.TryAdd(kinkster.PlayerNameWorld, false);
        }
        // Otherwise if they are gagged we shouldnt do anything.
        else if (hasNameplates && kinkster.ActiveGags.IsGagged())
        {
            // Add it if we are not already added.
            TrackedKinksters.TryAdd(kinkster.PlayerNameWorld, false);
        }
        // Otherwise we should remove the tracked nameplate.
        else
        {
            Logger.LogDebug($"Removing {kinkster.PlayerNameWorld} to tracked Nameplates", LoggerType.Gags);
            TrackedKinksters.Remove(kinkster.PlayerNameWorld, out var _);
        }

        // Request a redraw afterwards to the latest state is reflected.
        Svc.NamePlate.RequestRedraw();
    }

    // Do this by pointer now since we are cool like that. (TODO)
    private void RefreshGagPlates()
    {
        // assume a local copy of the kinksters, in which the remaining kinksters are gagged with an active chat garbler.
        var visibleGaggedKinksters = _kinksters.DirectPairs
            .Where(k => k.IsRendered && k.ActiveGags.IsGagged() && k.PairGlobals.ChatGarblerActive);

        // assign them to the dictionary.
        var newTrackedKinksters = new ConcurrentDictionary<string, bool>();
        foreach (var kinkster in visibleGaggedKinksters)
        {
            // Make sure if they are still speaking that we keep the value the same.
            if (TrackedKinksters.TryGetValue(kinkster.PlayerNameWorld, out var isSpeaking))
                newTrackedKinksters.AddOrUpdate(kinkster.PlayerNameWorld, isSpeaking, (key, oldValue) => isSpeaking);
            else
                newTrackedKinksters.TryAdd(kinkster.PlayerNameWorld, false);
        }
        // Update the Tracked Kinksters here (we dont do it during recalculation because of concurrent access)
        TrackedKinksters = newTrackedKinksters;
        Svc.NamePlate.RequestRedraw();
    }

    private void OnGameChatMessage(InputChannel channel, string senderNameWorld, string message)
    {
        if (PlayerData.NameWithWorld == senderNameWorld)
        {
            // Handle a client chat message
            if (_gags.ServerGagData is not { } data || ClientData.Globals is not { } g)
                return;

            // Discard if not a garbled message.
            if (!data.IsGagged() || !g.ChatGarblerActive || !g.AllowedGarblerChannels.IsActiveChannel((int)channel))
                return;

            // Fire achievement if it was longer than 5 words and stuff.
            if (g.ChatGarblerActive && message.Split(' ').Length > 5)
                GagspeakEventManager.AchievementEvent(UnlocksEvent.GaggedChatSent, channel, message);

            DisplayGaggedSpeaking(PlayerData.NameWithWorld, (int)(650.0f * message.Length / 20.0f));
        }
        else if (_kinksters.DirectPairs.FirstOrDefault(k => k.PlayerNameWorld == senderNameWorld) is { } match)
        {
            // If not rendered, ignore.
            if (!match.IsRendered)
                return;
            // Handle a Kinkster chat message.
            if (!match.ActiveGags.IsGagged() || !match.PairGlobals.ChatGarblerActive || !match.PairGlobals.AllowedGarblerChannels.IsActiveChannel((int)channel))
                return;

            // Fire achievement if it was longer than 5 words and stuff.
            if (message.Split(' ').Length > 5)
                GagspeakEventManager.AchievementEvent(UnlocksEvent.KinksterGaggedChatSent, match, channel, message);

            DisplayGaggedSpeaking(match.PlayerNameWorld, (int)(650.0f * message.Length / 20.0f));
        }
    }

    private void OnKinksterMessage(Kinkster k, InputChannel c, string message)
    {
        // Discard if not a garbled message.
        if (!k.ActiveGags.IsGagged() || !k.PairGlobals.ChatGarblerActive || !k.PairGlobals.AllowedGarblerChannels.IsActiveChannel((int)c))
            return;

        // Fire achievement if it was longer than 5 words and stuff.
        if (k.PairGlobals.ChatGarblerActive && message.Split(' ').Length > 5)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.KinksterGaggedChatSent, k, c, message);

        DisplayGaggedSpeaking(k.PlayerNameWorld, (int)(650.0f * message.Length / 20.0f));
    }

    /// <summary>
    ///     Temporarily sets a tracked Kinkster's GagPlate to the speaking variant momentarily, and then sets it back.
    /// </summary>
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
            if (h.NamePlateKind is not NamePlateKind.PlayerCharacter)
                continue;

            if (h.PlayerCharacter is not { } pc)
                continue;

            Character* handlerChara = (Character*)pc.Address;

            // Force the icon to be visible.
            var nmo = (AddonNamePlate.NamePlateObject*)h.NamePlateObjectAddress;
            if (!nmo->IsVisible)
                continue;

            // Otherwise, load the correct asset into the nameplate.
            var nameContainer = nmo->NameContainer;
            var nameIcon = nmo->NameIcon;

            // If they aint tracked, dont do nothin.
            var pnww = handlerChara->GetNameWithWorld();
            if (!TrackedKinksters.TryGetValue(pnww, out var isSpeaking))
                continue;

            // Logger.LogTrace($"Nameplate for {pnww} is {(isSpeaking ? "speaking" : "not speaking")}");
            LoadTextureToAsset(nameIcon, nameContainer, isSpeaking);
            nameIcon->ToggleVisibility(true);
        }
    }

    private unsafe void LoadTextureToAsset(AtkImageNode* node, AtkResNode* parentNode, bool isSpeaking)
    {
        var texturePointer = (Texture*)Svc.Texture.ConvertToKernelTexture(isSpeaking ? GaggedSpeakingIcon : GaggedIcon, true);
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

