using CkCommons;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Component.Shell;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagspeakAPI.Chat;
using GagspeakAPI.User;
using InteropGenerator.Runtime;

// Some of the below signatures were referenced by:
// https://git.anna.lgbt/anna/ChatTwo and https://git.anna.lgbt/anna/ExtraChat

namespace GagSpeak.Services;

public unsafe class ChatHooks : DisposableMediatorSubscriberBase
{
    // Detours the internal message sending.
    // Different than the sig from ShellCommandModule.ExecuteCommandInner, but could be the same destination?
    private delegate void SendMessageDelegate(ShellCommandModule* shell, Utf8String* message, UIModule* uiModule);
    [Signature("E8 ?? ?? ?? ?? FE 87 ?? ?? ?? ?? C7 87", DetourName = nameof(SendMessageDetour))]
    private Hook<SendMessageDelegate> SendMessageHook = null!;

    // Called when we select a chat channel from the bubble icon in the ChatboxUI.
    private delegate void SetChatChannelDelegate(RaptureShellModule* module, uint channel);
    [Signature("E8 ?? ?? ?? ?? 33 C0 EB ?? 85 D2", DetourName = nameof(SetChatChannelDetour))]
    private Hook<SetChatChannelDelegate> SetChatChannelHook = null!;

    // Used to change the label of a chat channel in the chatbox.
    // Can replace with AgentChatLog::Delegates::ChangeChannelName when comfortable.
    private delegate CStringPointer ChangeChannelNameDelegate(AgentChatLog* agent);
    [Signature("E8 ?? ?? ?? ?? BA ?? ?? ?? ?? 48 8D 4D B0 48 8B F8 E8 ?? ?? ?? ?? 41 8B D6", DetourName = nameof(ChangeChannelNameDetour))] // Almost the same sig as the one in clientstructs, without the extra ?? ?? ?? ?? ??
    private Hook<ChangeChannelNameDelegate> ChangeChannelNameHook = null!;

    // Called when manually selecting the chat channel from the bubble icon in the ChatboxUI, or via Hotkeys.
    // Like the existing counterpart in ClientStructs but with a bool return type so setChatType is handled properly.
    private delegate bool ChangeChatChannelDelegate(RaptureShellModule* shell, int channel, uint linkshellIndex, Utf8String* target, bool setChatType);
    [Signature("E8 ?? ?? ?? ?? 0F B7 44 37 ??", DetourName = nameof(ChangeChatChannelDetour))]
    private Hook<ChangeChatChannelDelegate> ChangeChatChannelHook = null!;

    // Experimental Tell Methods.
    private readonly Hook<RaptureShellModule.Delegates.ReplyInSelectedChatMode> ReplyInSelectedChatModeHook;
    private readonly Hook<RaptureShellModule.Delegates.SetContextTellTarget> SetChatLogTellTargetHook;

    private readonly GlobalChatLog _globalChat;
    private readonly MainConfig _config;
    private readonly ChatConfig _chatConfig;
    private readonly CommandManager _commands;
    private readonly ChatService _chatService;

    // Store locally only.
    private static AtkTextNode* _inputTxtNode = null;
    private static uint _cachedAddonLoadCount = 0;
    public RollingList<string> _sentChatHistory = new(200);

    public ChatHooks(ILogger<ChatService> logger, GagspeakMediator mediator,
        GlobalChatLog radarChatLog, MainConfig config, ChatConfig chatConfig,
        CommandManager commands, ChatService chatService)
        : base(logger, mediator)
    {
        _config = config;
        _globalChat = radarChatLog;
        _chatConfig = chatConfig;
        _commands = commands;
        _chatService = chatService;

        Svc.Hook.InitializeFromAttributes(this);

        // ExtraChat Hooks.
        SendMessageHook.SafeEnable();
        SetChatChannelHook.SafeEnable();
        ChangeChannelNameHook.SafeEnable();
        ChangeChatChannelHook.SafeEnable();

        ReplyInSelectedChatModeHook = Svc.Hook.HookFromAddress<RaptureShellModule.Delegates.ReplyInSelectedChatMode>(RaptureShellModule.MemberFunctionPointers.ReplyInSelectedChatMode, ReplyInSelectedChatModeDetour);
        ReplyInSelectedChatModeHook.SafeEnable();

        SetChatLogTellTargetHook = Svc.Hook.HookFromAddress<RaptureShellModule.Delegates.SetContextTellTarget>(RaptureShellModule.MemberFunctionPointers.SetContextTellTarget, SetContextTellTarget);
        SetChatLogTellTargetHook.SafeEnable();

        Svc.Framework.Update += OnTick;
        Svc.ClientState.Logout += OnLogout;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.Framework.Update -= OnTick;
        Svc.ClientState.Logout -= OnLogout;
        // Dispose Manual hooks.
        SendMessageHook.SafeDispose();
        SetChatChannelHook.SafeDispose();
        ChangeChannelNameHook.SafeDispose();
        ChangeChatChannelHook.SafeDispose();
        // Dispose CS Hooks
        SetChatLogTellTargetHook.SafeDispose();
        ReplyInSelectedChatModeHook.SafeDispose();
    }

    private void OnTick(IFramework _)
    {
        if (_chatService.ChatlogOverride.Equals(ChatlogId.Invalid))
            return;

        if (_chatService.OverrideColor == default)
            return;

        if (HasTempChannelDiffThanCurrent())
            return;

        // Apply the input color to the chat.
        var node = FindChatInputTextNode();
        if (node is null)
            return;

        var color = _chatService.OverrideColor;
        node->TextColor = color.TextByteColor();
        node->EdgeColor = color.EdgeByteColor();
    }

    // Clear out cached data.
    private void OnLogout(int type, int code)
    {
        _inputTxtNode = null;
        _cachedAddonLoadCount = 0;
    }

    internal static void SetChatInputFocus()
    {
        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName("ChatLog");
        if (addon is null || !addon->IsReady)
            return;

        var node = FindChatInputTextNode();
        if (node is null)
            return;

        AtkStage.Instance()->AtkInputManager->SetFocus((AtkResNode*)node, addon, 0);
    }

    private static AtkTextNode* FindChatInputTextNode()
    {
        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName("ChatLog");
        if (addon is null || !addon->IsReady)
            return null;

        if (addon->Id != _cachedAddonLoadCount)
        {
            _inputTxtNode = null;
            _cachedAddonLoadCount = addon->Id;
        }
        // Quick-Return from cache storage
        if (_inputTxtNode != null)
            return _inputTxtNode;
        // Walk node list looking for the TextInput component
        for (var i = 0; i < addon->UldManager.NodeListCount; i++)
        {
            var node = addon->UldManager.NodeList[i];
            if (node is null || node->GetNodeType() is not NodeType.Component)
                continue;

            var compNode = node->GetAsAtkComponentNode();
            if (compNode is null || compNode->Component is null) continue;
            if (compNode->Component->GetComponentType() is not ComponentType.TextInput)
                continue;

            var textInput = (AtkComponentInputBase*)compNode->Component;
            _inputTxtNode = textInput->AtkTextNode;
            break;
        }
        return _inputTxtNode;
    }

    /// <summary>
    ///   Detours the internal message sending. Different sig than 
    ///   ShellCommandModule.ExecuteCommandInner, but could same dest? <para />
    ///   Messages for GagSpeak ChatLogIds are not sent and instead 
    ///   sent to the server. On success the result it printed via
    ///   Dalamud's chat printer.
    /// </summary>
    /// <remarks> Passed from the chatbox, before being sent to the servers / game. </remarks>
    private void SendMessageDetour(ShellCommandModule* shell, Utf8String* message, UIModule* uiModule)
    {
        try
        {
            if (SendMessageInternal(message))
                SendMessageHook.Original(shell, message, uiModule);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in message detour: {ex}");
        }
    }

    // This method could be optimized but dear god I hate how much time i spent on this.
    private bool SendMessageInternal(Utf8String* message)
    {
        var sendTo = _chatService.ChatlogOverride;
        var blockCustomNative = false;

        byte[]? msgContents = null;
        if (message->StringPtr.Value[0] == 2)
        {
            // check for autotranslate commands
            Logger.LogTrace("SendMessageInternal starts with 0x02, checking for autotranslate command...", LoggerType.ChatHooks);
            var payload = Payload.Decode(new BinaryReader(new UnmanagedMemoryStream(message->StringPtr, message->BufSize)));
            // Custom commands dont have Auto-Translate, so valid.
            if (payload is AutoTranslatePayload at && at.Text[2..].StartsWith('/'))
            {
                Logger.LogTrace("SendMessageInternal was AutoTranslatePayload with command, allowing.", LoggerType.ChatHooks);
                return true;
            }
        }

        // Assume Block Native sends if temp & current channels are different and tmp is not none.
        // This means we are doing ALT+R or something while the current chat was a ChatlogId channel.
        // In this case, we should process it like normal.
        blockCustomNative = HasTempChannelDiffThanCurrent();

        // Otherwise attempt to aquire the chatlog shortcut, if we didnt have the channel hard-set.
        if (message->StringPtr.Value[0] == '/')
        {
            sendTo = ChatlogId.Invalid;
            var command = "";
            int i;
            for (i = 0; i < message->BufSize; i++)
            {
                var c = message->StringPtr.Value[i];
                if (c is 0 || char.IsWhiteSpace((char)c))
                    break;

                command += (char)c;
            }

            // Clean this up so the command manager can handle errored return values.
            if (_commands.IsGlobalChatCommand(command))
            {
                Logger.LogTrace($"Intercepted GlobalChat message [{command}]", LoggerType.ChatHooks);
                var radarLogId = new ChatlogId(GsChatKind.Global, "GlobalChat");
                Logger.LogDebug($"Intercepted GlobalChatLog: [{command}]", LoggerType.ChatHooks);
                // Extract the message.
                var entireMessage = MemoryHelper.ReadRawNullTerminated((nint)message->StringPtr.Value);
                // Update the channel we are sending it to.
                sendTo = radarLogId;
                // Skip past the command and any whitespace to get to the actual message.
                if (entireMessage.Length - 1 >= i && char.IsWhiteSpace((char)entireMessage[i]))
                    i += 1; // i++?..

                // The message bytes to send.
                msgContents = entireMessage[i..];
                // If the message is blank, we should instead override the current channel to reflect this channel by default!
                if (msgContents.Length is 0 || msgContents.All(c => char.IsWhiteSpace((char)c)))
                {
                    _chatService.ChatlogOverride = radarLogId;
                    return false;
                }
                // The ALT+R or whatever we used to make temp channels was also used to
                // manually type out a custom chat message, so revert block.
                blockCustomNative = false;
            }

            if (_commands.IsTellCommand(command))
            {
                Logger.LogTrace($"Intercepted GsTellChatLog message [{command}]", LoggerType.ChatHooks);
                if (_commands.CommandToChatKind(command) is not { } chatKind)
                {
                    Mediator.Publish(new ChatCmdFailureMessage(null, command, string.Empty, ChatFailType.TargetResolutionFailed));
                    return false;
                }

                var targetArg = string.Empty;
                // Skip the spaces between the command and the argument
                for (; i < message->BufSize; i++)
                {
                    var c = message->StringPtr.Value[i];
                    if (c is 0 || !char.IsWhiteSpace((char)c))
                        break;
                }
                // Extract the target argument
                for (; i < message->BufSize; i++)
                {
                    var c = message->StringPtr.Value[i];
                    if (c is 0 || char.IsWhiteSpace((char)c))
                        break;

                    targetArg += (char)c;
                }

                if (string.IsNullOrEmpty(targetArg))
                {
                    Mediator.Publish(new ChatCmdFailureMessage(chatKind, command, targetArg, ChatFailType.MissingArgument));
                    return false;
                }
                var resolved = _chatService.ResolveAlias(chatKind, targetArg);
                if (resolved.Equals(ChatlogId.Invalid))
                {
                    Mediator.Publish(new ChatCmdFailureMessage(chatKind, command, targetArg, ChatFailType.TargetResolutionFailed, targetArg));
                    return false;
                }

                Logger.LogDebug($"Intercepted CkChatLog [{command} {targetArg}] -> (Kind: {resolved.Kind} - ID: {resolved.ChatId})", LoggerType.ChatHooks);
                // Extract the message.
                var entireMessage = MemoryHelper.ReadRawNullTerminated((nint)message->StringPtr.Value);
                // Update the channel we are sending it to.
                sendTo = resolved;
                // Skip past the command and any whitespace to get to the actual message.
                if (entireMessage.Length - 1 >= i && char.IsWhiteSpace((char)entireMessage[i]))
                    i += 1; // i++?..
                // The message bytes to send.
                msgContents = entireMessage[i..];
                // If the message is blank, we should instead override the current channel to reflect this channel by default!
                if (msgContents.Length is 0 || msgContents.All(c => char.IsWhiteSpace((char)c)))
                {
                    _chatService.ChatlogOverride = resolved;
                    return false;
                }

                // The ALT+R or whatever we used to make temp channels was also used to
                // manually type out a custom chat message, so revert block.
                blockCustomNative = false;
            }
        }

        // Allow the message if it was an invalid ChatLogId
        if (sendTo.Equals(ChatlogId.Invalid))
        {
            Logger.LogTrace($"SendMessageInternal: No valid chatlog override found.", LoggerType.ChatHooks);
            return true;
        }

        if (blockCustomNative)
        {
            Logger.LogTrace($"SendMessageInternal: blockCustomNative was true.", LoggerType.ChatHooks);
            return true;
        }

        // Update the message to send, if still null.
        msgContents ??= MemoryHelper.ReadRawNullTerminated((nint)message->StringPtr.Value);
        // don't send blank messages even to the original handler
        if (msgContents.Length is 0 || msgContents.All(c => char.IsWhiteSpace((char)c)))
        {
            Logger.LogTrace($"SendMessageInternal: Message is blank after processing, not sending.", LoggerType.ChatHooks);
            return false;
        }

        // Internally send off the message, and perhaps also off to others via chat distribution.
        Logger.LogTrace($"SendMessageInternal: Sending off message to chatlog override (Kind: {sendTo.Kind} - ID: {sendTo.ChatId}), with contents: '{Encoding.UTF8.GetString(msgContents)}'", LoggerType.ChatHooks);
        _chatService.SendMessageNative(sendTo, msgContents, message);
        // Prevent it from being sent off to the game.
        return false;
    }

    /// <summary>
    ///   Called when we select a chat channel from the bubble icon in the ChatboxUI.
    /// </summary>
    /// <param name="channel"> The <see cref="NativeInputChannel"/> to set the chat to. </param>
    /// <remarks> The channel set is NOT a tempChatType, it changes it internally. </remarks>
    private void SetChatChannelDetour(RaptureShellModule* module, uint channel)
    {
        // avoid potential stack overflow from recursion
        if (_chatService.ChatlogOverride != ChatlogId.Invalid)
        {
            Logger.LogDebug($"SetChatChannel called on channel={(NativeInputChannel)channel}, with TmpChannel={(NativeInputChannel)RaptureShellModule.Instance()->TempChatType}", LoggerType.ChatHooks);
            // If the TempChatType is -2, it means that we should restore the temp channel to its original state.
            if (RaptureShellModule.Instance()->TempChatType == -2)
            {
                Logger.LogTrace($"Restoring original temp chat channel. Setting override to Invalid.", LoggerType.ChatHooks);
                _chatService.ChatlogOverride = ChatlogId.Invalid;
            }
        }

        Logger.LogTrace($"SetChatChannel called with channel={(NativeInputChannel)channel}.", LoggerType.ChatHooks);
        SetChatChannelHook.Original(module, channel);
    }

    private bool HasTempChannelDiffThanCurrent()
    {
        var shell = RaptureShellModule.Instance();
        var chatType = (NativeInputChannel)shell->ChatType;
        var tmpChatType = (NativeInputChannel)shell->TempChatType;
        return tmpChatType is not NativeInputChannel.None && tmpChatType != chatType;
    }

    /// <summary>
    ///   Used to change the label of a chat channel in the chatbox.
    /// </summary>
    /// <remarks> Can replace with <see cref="AgentChatLog.Delegates.ChangeChannelName"/> when comfortable. </remarks>
    private CStringPointer ChangeChannelNameDetour(AgentChatLog* agent)
    {
        var ret = ChangeChannelNameHook!.Original(agent);
        Logger.LogTrace($"ChangeChannelName At the time of this call:" +
            $"\n\tChannel      : {(NativeInputChannel)agent->CurrentChannel}" +
            $"\n\tTmpChannel   : {(NativeInputChannel)RaptureShellModule.Instance()->TempChatType}" +
            $"\n\tLabel        : {agent->ChannelLabel.ToString()}" +
            $"\n\tReplyChannel : {(NativeInputChannel)agent->ReplyChannel}" +
            $"\n\tTellName     : {agent->TellPlayerName.ToString()} @ {agent->TellWorldId}" +
            $"\n\tChatLogId    : [{_chatService.ChatlogOverride.Kind}_{_chatService.ChatlogOverride.ChatId}]", LoggerType.ChatHooks);

        // Nothing to override, so return.
        if (_chatService.ChatlogOverride == ChatlogId.Invalid)
            return ret;

        // If a tempchannel is set, let that apply over the custom chatlog override.
        if (RaptureShellModule.Instance()->TempChatType != -2)
            return ret;

        // Extract the chat channel, and the name to override with.
        var chatboxChatType = (uint)RaptureShellModule.Instance()->ChatType;
        var chatChannel = agent->ChannelLabel;
        var overrideName = _chatService.ResolveOverrideName();
        // If not a temp channel update the name, otherwise, allow it to function as normal.
        Logger.LogDebug($"ChangeChannelName overriding current chatChannel ({overrideName}) ({chatChannel.ToString()})", LoggerType.ChatHooks);
        fixed (byte* bytesPtr = Encoding.UTF8.GetBytes("\u3000 " + overrideName + "\0"))
        {
            chatChannel.SetString(bytesPtr);
        }

        return chatChannel.StringPtr;
    }

    /// <summary>
    ///   This is from FFXIVClientStructs, but has return type of void. The actual type should be bool. <br/>
    ///   We must do this so that the return value for <paramref name="setChatType"/> will be handled 
    ///   following the detour, otherwise it will always be the same value, and is why Chat2 is a bit
    ///   buggy with channel setting.
    /// </summary>
    private bool ChangeChatChannelDetour(RaptureShellModule* shell, int channel, uint linkshellIndex, Utf8String* target, bool setChatType)
    {
        var ret = ChangeChatChannelHook!.Original(shell, channel, linkshellIndex, target, setChatType);
        Logger.LogDebug($"ChangeChatChannelDetour: to {(NativeInputChannel)channel} with linkshellIndex {linkshellIndex} and target '{(target != null ? target->ToString() : "null")}', setChatType={setChatType}", LoggerType.ChatHooks);
        Logger.LogDebug($"TempChatType: {(NativeInputChannel)shell->TempChatType}, CurrChatType: {(NativeInputChannel)shell->ChatType}, RetValue={ret}", LoggerType.ChatHooks);
        return ret;
    }

    // Unsure exactly of purpose yet.
    private void ReplyInSelectedChatModeDetour(RaptureShellModule* agent)
    {
        var replyMode = AgentChatLog.Instance()->ReplyChannel;
        if (replyMode == -2)
        {
            Logger.LogTrace($"ReplyInSelectedChatMode called with replyMode -2, using original function without setting channel", LoggerType.ChatHooks);
            ReplyInSelectedChatModeHook!.Original(agent);
            return;
        }
        
        Logger.LogDebug($"ReplyInSelectedChatMode called with replyMode {replyMode}, setting channel to {(XivChatType)replyMode} before calling original function", LoggerType.ChatHooks);
        SetChannelInternal((NativeInputChannel)replyMode);
        ReplyInSelectedChatModeHook!.Original(agent);
    }
    
    // Could have some purpose, will need to see though.
    private bool SetContextTellTarget(RaptureShellModule* a1, Utf8String* playerName, Utf8String* worldName, ushort worldId, ulong accountId, ulong contentId, ushort reason, bool setChatType)
    {
        if (playerName != null)
        {
            try
            {
                Logger.LogTrace($"SetContextTellTarget called with playerName='{playerName->ToString()}', worldName='{(worldName != null ? worldName->ToString() : "null")}', " +
                    $"worldId={worldId}, accountId={accountId}, contentId={contentId}, reason={reason}, setChatType={setChatType}", LoggerType.ChatHooks);
                // Can maybe do something here idk.
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in SetContextTellTarget");
            }
        }

        // Perform the original invocation regardless.
        return SetChatLogTellTargetHook!.Original(a1, playerName, worldName, worldId, accountId, contentId, reason, setChatType);
    }

    // I dont think this will be called by anything we need yet.
    // If no telltarget is provided we can assume this is not a tell channel being set.
    internal void SetChannelInternal(NativeInputChannel channel, UserData? tellTarget = null)
    {
        // Custom GS channels are not supported in-game, so we dont want to call it with them.
        // ExtraChat linkshells aren't supported in game so we never want to
        // call the ChangeChatChannel function with them.
        //
        // Callers should call ChatLogWindow.SetChannel() which handles ExtraChat channels
        Logger.LogTrace($"SetChannelInternal called with channel {channel} and tellTarget '{tellTarget?.VanityOrAnonName ?? "UNK"}'", LoggerType.ChatHooks);
        if (channel is NativeInputChannel.Invalid)
        {
            Logger.LogTrace("SetChannelInternal was a GSChat channel, ignoring.", LoggerType.ChatHooks);
            return;
        }

        var target = Utf8String.FromString(tellTarget?.VanityOrAnonName ?? "");
        var idx = channel.LinkshellIdx();
        if (idx is uint.MaxValue)
            idx = 0; // Set to default.

        // As a fallback if not valid for any linkshell, abort.
        if (!channel.ValidAnyLinkshell())
        {
            Logger.LogWarning($"Attempted to set chat channel to {channel} which is not valid for any linkshell, aborting.", LoggerType.ChatHooks);
            return;
        }

        Logger.LogDebug($"Setting chat channel to {channel} with idx {idx} and target, Nullable: '{target is null}', for tellTarget '{tellTarget?.VanityOrAnonName}'", LoggerType.ChatHooks);
        RaptureShellModule.Instance()->ChangeChatChannel(tellTarget != null ? 17 : (int)channel, idx, target, true);
        target->Dtor(true);
    }
}
