using CkCommons;
using CkCommons.Chat;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Raii;
using CkCommons.RichText;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;
using System.Globalization;
using GagSpeak.Gui.MainWindow;

namespace GagSpeak.Utils;

// Revise later to make more instanced or common chatlog sources to draw from.
public class GlobalChatLog : CkChatlog<GagSpeakChatMessage>, IMediatorSubscriber, IDisposable
{
    private static string RecentFile => Path.Combine(ConfigFileProvider.GagSpeakDirectory, "global-chat-recent.log");
    private static RichTextFilter AllowedTypes = RichTextFilter.Emotes;

    private readonly MainHub _hub;
    private readonly MainMenuTabs _tabMenu;
    private readonly MainConfig _config;
    private readonly GagRestrictionManager _gags;
    private readonly KinksterManager _kinksters;
    private readonly MufflerService _garbler;
    private readonly TutorialService _guides;
    // load the popout to sync our message sending.

    private static bool _newMsgFromDev = false;
    private static int _newMsgCount = 0;

    private string _requestNickPref = string.Empty;
    private string _requestMessage = string.Empty;

    private bool _showEmotes = false;

    public GlobalChatLog(GagspeakMediator mediator, MainHub hub, MainMenuTabs tabs, 
        MainConfig config, GagRestrictionManager gags, KinksterManager kinksters, 
        MufflerService garbler, TutorialService guides)
        : base(0, "Global Chat", 1000)
    {
        Mediator = mediator;
        _hub = hub;
        _tabMenu = tabs;
        _config = config;
        _gags = gags;
        _kinksters = kinksters;
        _garbler = garbler;
        _guides = guides;

        // Load the chat log from most recent session, if any.
        LoadChatLog();

        Mediator.Subscribe<GlobalChatMessage>(this, AddNetworkMessage);
        Mediator.Subscribe<MainWindowTabChangeMessage>(this, (msg) =>
        {
            if (msg.NewTab is MainMenuTabs.SelectedTab.GlobalChat)
            {
                ShouldScrollToBottom = true;
                unreadSinceScroll = 0;
                _newMsgFromDev = false;
                _newMsgCount = 0;
            }
        });
    }

    public GagspeakMediator Mediator { get; }

    public static bool AccessBlocked => ChatBlocked || NotVerified;
    public static bool ChatBlocked => !MainHub.Reputation.ChatUsage;
    public static bool NotVerified => !MainHub.Reputation.IsVerified;
    public static int NewMsgCount => _newMsgCount;
    public static bool NewMsgFromDev => _newMsgFromDev;

    void IDisposable.Dispose()
    {
        Mediator.UnsubscribeAll(this);
        // Save the chat log prior to disposal.
        SaveChatLog();

        GC.SuppressFinalize(this);
    }

    public void SetDisabledStates(bool content, bool input)
    {
        disableContent = content;
        disableInput = input;
    }

    public void SetAutoScroll (bool newState)
        => DoAutoScroll = newState;

    protected override string ToTooltip(GagSpeakChatMessage message)
        => $"Sent @ {message.Timestamp.ToString("T", CultureInfo.CurrentCulture)}" +
        "--NL----COL--[Right-Click]--COL-- View Interactions" +
        "--NL----COL--[Middle-Click]--COL-- Open KinkPlate";

    private void AddNetworkMessage(GlobalChatMessage networkChat)
    {
        if (_tabMenu.TabSelection is not MainMenuTabs.SelectedTab.GlobalChat)
            _newMsgCount++;
        else
        {
            _newMsgCount = 0;
            _newMsgFromDev = false;
        }
        
        // Default sender name and tag, respects 3-4 character string.
        var userTagCode = networkChat.Message.UserTagCode;
        var SenderName = "Kinkster-" + userTagCode;
        // get the UserData from the GlobalChatMessage (may not be a pair)
        var userData = networkChat.Message.Sender;
        // Set the SenderName according to conditions.
        if (userData.Tier is CkSupporterTier.KinkporiumMistress)
            SenderName = $"Mistress Cordy";
        else if (_kinksters.DirectPairs.FirstOrDefault(p => p.UserData.UID == userData.UID) is { } match)
            SenderName = match.GetNickAliasOrUid() + " (" + userTagCode + ")";
        else if (networkChat.FromSelf)
            SenderName = userData.AliasOrUID + " (" + userTagCode + ")";
        // construct the chat message struct to add, and append it.
        AddMessage(new GagSpeakChatMessage(userData, SenderName, networkChat.Message.Message));
    }

    protected override void AddMessage(GagSpeakChatMessage newMsg)
    {
        // Cordy is special girl :3
        if (newMsg.Tier is CkSupporterTier.KinkporiumMistress)
        {
            // Force set the uid color to her favorite color.
            UserColors[newMsg.UID] = GsCol.ShopKeeperColor.Vec4();
            // allow any rich text tags, as she is a special case.
            var prefix = $"[img=RequiredImages\\Tier4Icon][rawcolor={GsCol.ShopKeeperColor.Uint()}]{newMsg.Name}[/rawcolor]: ";
            Messages.PushBack(newMsg with { Message = prefix + newMsg.Message });
            unreadSinceScroll++;
            _newMsgFromDev = true;
        }
        else if (newMsg.UID == "System")
        {
            // System messages are special, they are not colored.
            var prefix = $"[rawcolor=0xFF0000FF]{newMsg.Name}[/rawcolor]: ";
            Messages.PushBack(newMsg with { Message = prefix + newMsg.Message });
            unreadSinceScroll++;
        }
        else
        {
            // Assign the sender color
            var col = ColorHelpers.RgbaVector4ToUint(AssignSenderColor(newMsg));
            // strip out the modifiers that are not allowed to prevent chaos in global chat.
            var sanitizedMsg = CkRichText.StripDisallowedRichTags(newMsg.Message, AllowedTypes);
            // append special formatting to the start of the message based on supporter type.
            var prefix = newMsg.Tier switch
            {
                CkSupporterTier.DistinguishedConnoisseur => $"[img=RequiredImages\\Tier3Icon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                CkSupporterTier.EsteemedPatron => $"[img=RequiredImages\\Tier2Icon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                CkSupporterTier.ServerBooster => $"[img=RequiredImages\\TierBoosterIcon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                CkSupporterTier.IllustriousSupporter => $"[img=RequiredImages\\Tier1Icon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                _ => $"[rawcolor={col}]{newMsg.Name}[/rawcolor]: "
            };
            Messages.PushBack(newMsg with { Message = prefix + sanitizedMsg });
            unreadSinceScroll++;
        }
    }

    private void AddExistingMessage(GagSpeakChatMessage newMsg)
    {
        // Cordy is special girl :3
        if (newMsg.Tier is CkSupporterTier.KinkporiumMistress)
        {
            // Force set the uid color to her favorite color.
            UserColors[newMsg.UID] = GsCol.ShopKeeperColor.Vec4();
            Messages.PushBack(newMsg);
            unreadSinceScroll++;
        }
        else
        {
            // Assign the sender color
            AssignSenderColor(newMsg);
            Messages.PushBack(newMsg);
            unreadSinceScroll++;
        }
    }

    public override void DrawChatInputRow()
    {
        using var _ = ImRaii.Group();

        var scrollIcon = DoAutoScroll ? FAI.ArrowDownUpLock : FAI.ArrowDownUpAcrossLine;
        var width = ImGui.GetContentRegionAvail().X;

        // Set keyboard focus to the chat input box if needed
        if (shouldFocusChatInput && ImGui.IsWindowFocused())
        {
            Svc.Logger.Information("Setting keyboard focus to chat input box.", LoggerType.GlobalChat);
            ImGui.SetKeyboardFocusHere(0);
            shouldFocusChatInput = false;
        }
        

        ImGui.SetNextItemWidth(width - (CkGui.IconButtonSize(scrollIcon).X + ImGui.GetStyle().ItemInnerSpacing.X) * 3);
        ImGui.InputTextWithHint($"##ChatInput{Label}{ID}", "type here...", ref previewMessage, 300);
        // Process submission Prevent losing chat focus after pressing the Enter key.
        if (ImGui.IsItemFocused() && ImGui.IsKeyPressed(ImGuiKey.Enter))
        {
            shouldFocusChatInput = true;
            _showEmotes = false;
            OnSendMessage(previewMessage);
        }

        // toggle emote viewing.
        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, GsCol.VibrantPink.Uint(), _showEmotes))
        {
            if (CkGui.IconButton(FAI.Heart))
                _showEmotes = !_showEmotes;
        }
        CkGui.AttachToolTip($"Toggles Quick-Emote selection.");
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ChatEmotes, MainUI.LastPos, MainUI.LastSize);

        // Toggle AutoScroll functionality
        ImUtf8.SameLineInner();
        if (CkGui.IconButton(scrollIcon))
            DoAutoScroll = !DoAutoScroll;
        CkGui.AttachToolTip($"Toggles AutoScroll (Current: {(DoAutoScroll ? "Enabled" : "Disabled")})");
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ChatScroll, MainUI.LastPos, MainUI.LastSize);

        // draw the popout button
        ImUtf8.SameLineInner();
        if (CkGui.IconButton(FAI.Expand, disabled: !KeyMonitor.ShiftPressed()))
            Mediator.Publish(new UiToggleMessage(typeof(GlobalChatPopoutUI)));
        CkGui.AttachToolTip("Open the Global Chat in a Popout Window--SEP--Hold SHIFT to activate!");
    }

    protected override void DrawPostChatLog(Vector2 inputPosMin)
    {
        // Preview Text padding area
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5));
        var drawTextPreview = !string.IsNullOrWhiteSpace(previewMessage);
        // if we should show the preview, do so.
        if (drawTextPreview)
            DrawTextPreview(previewMessage, inputPosMin);

        // Afterwards, we need to make sure that we can create a new window for the emotes at the correct space if so.
        if (_showEmotes)
        {
            var drawPos = drawTextPreview ? ImGui.GetItemRectMin() : inputPosMin;
            DrawQuickEmoteWindow(drawPos);
        }
    }

    private void DrawQuickEmoteWindow(Vector2 drawPos)
    {
        var totalWidth = ImGui.GetContentRegionAvail().X;
        var spacing = ImGui.GetStyle().ItemInnerSpacing;
        var emoteCache = CosmeticService.EmoteTextures.Cache;
        var totalEmotes = emoteCache.Count;
        var emoteSize = new Vector2(ImGui.GetFrameHeightWithSpacing());
        var emotesPerRow = Math.Max(1, (int)((totalWidth.RemoveWinPadX() + spacing.X) / (emoteSize.X + spacing.X)));
        var rows = (int)Math.Ceiling((float)totalEmotes / emotesPerRow);
        var winHeight = (emoteSize.Y + spacing.Y) * Math.Clamp(rows, 1, 2) + spacing.Y;


        var winPos = drawPos - new Vector2(0, winHeight.AddWinPadY() + spacing.Y);
        ImGui.SetNextWindowPos(winPos);
        using var c = CkRaii.ChildPaddedW("Quick-Emote-View", totalWidth, winHeight, wFlags: WFlags.AlwaysVerticalScrollbar);

        var wdl = ImGui.GetWindowDrawList();
        wdl.PushClipRect(winPos, winPos + c.InnerRegion.WithWinPadding(), false);
        wdl.AddRectFilled(winPos, winPos + c.InnerRegion.WithWinPadding(), 0xCC000000, 5, ImDrawFlags.RoundCornersAll);
        wdl.AddRect(winPos, winPos + c.InnerRegion.WithWinPadding(), ImGuiColors.ParsedGold.ToUint(), 5, ImDrawFlags.RoundCornersAll);
        wdl.PopClipRect();

        var count = 0;
        foreach (var (key, wrap) in CosmeticService.EmoteTextures.Cache)
        {
            ImGui.Dummy(emoteSize);
            var min = ImGui.GetItemRectMin();
            wdl.AddDalamudImageRounded(wrap, min, emoteSize, 5, key.ToRichTextString());
            // if clicked, append the string to our message.
            if (ImGui.IsItemClicked())
            {
                previewMessage += $"{key.ToRichTextString()} ";
                shouldFocusChatInput = true;
            }

            count++;
            if (count % emotesPerRow != 0)
                ImUtf8.SameLineInner();
        }
    }

    protected override void OnMiddleClick(GagSpeakChatMessage message)
        => Mediator.Publish(new KinkPlateLightCreateOpenMessage(message.UserData));

    protected override void OnSendMessage(string message)
    {
        shouldFocusChatInput = true;
        if (string.IsNullOrWhiteSpace(previewMessage))
            return;

        // Process message if gagged
        if ((_gags.ServerGagData?.IsGagged() ?? true) && (ClientData.Globals?.ChatGarblerActive ?? false))
            previewMessage = _garbler.ProcessMessage(previewMessage);

        // truncate the string if it ends up longer than the character limit.
        if (previewMessage.Length > 400)
            previewMessage = previewMessage[..400];

        // Send message to the server
        _hub.UserSendGlobalChat(new(MainHub.OwnUserData, previewMessage, _config.Current.PreferThreeCharaAnonName)).ConfigureAwait(false);

        // Clear message and trigger achievement event
        previewMessage = string.Empty;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.GlobalSent);
    }

    protected override void DrawPopupInternal()
    {
        var shiftHeld = KeyMonitor.ShiftPressed();
        var ctrlHeld = KeyMonitor.CtrlPressed();

        if(LastInteractedMsg is null)
            return;

        CkGui.FontText(LastInteractedMsg.Name, Svc.PluginInterface.UiBuilder.MonoFontHandle);
        ImGui.Separator();
        if (ImGui.Selectable("View Light KinkPlate") && LastInteractedMsg.UID != "System")
        {
            Mediator.Publish(new KinkPlateLightCreateOpenMessage(LastInteractedMsg.UserData));
            ClosePopupAndResetMsg();
        }
        CkGui.AttachToolTip($"Opens {LastInteractedMsg.Name}'s Light KinkPlate.");

        ImGui.Separator();
        using (ImRaii.Disabled(!shiftHeld || string.IsNullOrWhiteSpace(_requestMessage)))
            if (ImGui.Selectable("Send Kinkster Request"))
            {
                _hub.UserSendKinksterRequest(new(new(LastInteractedMsg.UID), false, _requestNickPref, _requestMessage)).ConfigureAwait(false);
                ClosePopupAndResetMsg();
            }
        CkGui.AttachToolTip(!shiftHeld ? "Must be holding SHIFT to select."
            : string.IsNullOrWhiteSpace(_requestMessage)
            ? "Must attach a message to the request!"
                : $"Sends a Kinkster Request to {LastInteractedMsg.Name}.");

        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 20);
        ImGui.InputTextWithHint("##attachedPairMsg", "Attached Request Msg..", ref _requestMessage, 150);
        ImGui.Separator();

        using (ImRaii.Disabled(!ctrlHeld))
            if (ImGui.Selectable("Hide Messages from Kinkster") && LastInteractedMsg.UID != "System" && LastInteractedMsg.UID != MainHub.UID)
            {
                SilenceList.Add(LastInteractedMsg.UID);
                ClosePopupAndResetMsg();
            }
        CkGui.AttachToolTip(ctrlHeld
            ? $"Hides all messages from {LastInteractedMsg.Name} until plugin reload/restart."
            : "Must be holding CTRL to select.");
    }

    private void ClosePopupAndResetMsg()
    {
        LastInteractedMsg = new(new("System"), string.Empty, string.Empty);
        _requestMessage = string.Empty;
        _requestNickPref = string.Empty;
        ImGui.CloseCurrentPopup();
    }

    private void SaveChatLog()
    {
        // Capture up to the last 500 messages
        var messagesToSave = Messages.TakeLast(500).ToList();
        var logToSave = new SerializableChatLog(TimeCreated, messagesToSave);

        // Serialize the item to JSON
        var json = JsonConvert.SerializeObject(logToSave);
        var compressed = json.Compress(6);
        var base64ChatLogData = Convert.ToBase64String(compressed);
        File.WriteAllText(RecentFile, base64ChatLogData);
    }

    public void LoadChatLog()
    {
        // if the file does not exist, return
        if (!File.Exists(RecentFile))
        {
            // Add the basic welcome message and return.
            Svc.Logger.Information("Chat log file does not exist. Adding welcome message.", LoggerType.GlobalChat);
            AddMessage(new(new("System"), "System",
                "Welcome to the GagSpeak Global Chat![para]" +
                "Your Name in here is Anonymous to anyone you have not yet added. Feel free to say hi![line]"));
            return;
        }

        // Attempt Deserialization.
        var savedChatlog = new SerializableChatLog();
        try
        {
            var base64logFile = File.ReadAllText(RecentFile);
            var bytes = Convert.FromBase64String(base64logFile);
            var version = bytes[0];
            version = bytes.DecompressToString(out var decompressed);
            savedChatlog = JsonConvert.DeserializeObject<SerializableChatLog>(decompressed);

            // if any user datas are null, throw an exception.
            if (savedChatlog.Messages.Any(m => m.UserData is null))
                throw new Exception("One or more user datas are null in the chat log.");
        }
        catch (Bagagwa ex)
        {
            Svc.Logger.Error(ex, "Failed to load chat log.");
            AddMessage(new(new("System"), "System",
                "Welcome to the GagSpeak Global Chat![para]" +
                "Your Name in here is Anonymous to anyone you have not yet added. Feel free to say hi![line]"));
            return;
        }

        // If the de-serialized date is not the same date as our current date, do not restore the data.
        if (savedChatlog.DateStarted.DayOfYear != DateTime.Now.DayOfYear)
        {
            Svc.Logger.Information("Chat log is from a different day. Not restoring.", LoggerType.GlobalChat);
            AddMessage(new(new("System"), "System",
                "Welcome to the GagSpeak Global Chat![para]" +
                "Your Name in here is Anonymous to anyone you have not yet added. Feel free to say hi![line]"));
            return;
        }

        // print out all messages:
        foreach (var msg in savedChatlog.Messages)
            AddExistingMessage(msg);

        Svc.Logger.Information($"Loaded {savedChatlog.Messages.Count} messages from the chat log.", LoggerType.GlobalChat);
    }
}
