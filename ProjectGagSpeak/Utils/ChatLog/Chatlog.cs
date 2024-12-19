using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui.Text;
using System;
using System.Globalization;
using System.Numerics;
using System.Windows.Forms;

namespace GagSpeak.Utils.ChatLog;
// an instance of a chatlog.
public class ChatLog
{
    private readonly MainHub _apiHubMain;
    private readonly GagspeakMediator _mediator;

    public readonly ChatCircularBuffer<ChatMessage> Messages = new(1000);
    private int MessageCountSinceLastScroll = 0;
    private readonly Dictionary<string, Vector4> UserColors = new();
    private static Vector4 CKMistressColor = new Vector4(0.886f, 0.407f, 0.658f, 1f);
    private static Vector4 CkMistressText = new Vector4(1, 0.711f, 0.843f, 1f);
    public DateTime TimeCreated { get; set; }
    public bool AutoScroll = true;
    private string _lastAttachedMessage = string.Empty;

    // Define which users to ignore.
    public List<string> UidSilenceList = new List<string>();
    private ChatMessage _lastInteractedMsg = new ChatMessage();

    public ChatLog(MainHub mainHub, GagspeakMediator mediator)
    {
        _apiHubMain = mainHub;
        _mediator = mediator;
        TimeCreated = DateTime.Now;
    }

    public void AddMessage(ChatMessage message)
    {
        Messages.PushBack(message);
        MessageCountSinceLastScroll++;
    }

public void AddMessageRange(IEnumerable<ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            Messages.PushBack(message);
        }
        MessageCountSinceLastScroll += messages.Count();
    }

    public void ClearMessages()
        => Messages.Clear();

    public bool ShouldScrollToBottom = false;
    private Vector2 RectMin { get; set; } = Vector2.Zero;
    private Vector2 RectMax { get; set; } = Vector2.Zero;

    public void PrintChatLogHistory(bool showMessagePreview, string previewMessage, Vector2 region)
    {
        using (ImRaii.Child("##GlobalChatLog" + TimeCreated.ToString(), region, false, ImGuiWindowFlags.NoDecoration))
        {
            var drawList = ImGui.GetWindowDrawList();
            RectMin = drawList.GetClipRectMin();
            RectMax = drawList.GetClipRectMax();

            var ySpacing = ImGui.GetStyle().ItemInnerSpacing.Y;
            var messagesToDisplay = Messages.Skip(Math.Max(0, Messages.Size - 250)).Take(250);
            foreach (var x in messagesToDisplay)
            {
                if (UidSilenceList.Contains(x.UID)) continue;

                if (!UserColors.ContainsKey(x.UID))
                {
                    if (x.SupporterTier is CkSupporterTier.KinkporiumMistress)
                    {
                        UserColors[x.UID] = CKMistressColor;
                    }
                    else
                    {
                        // Generate a random color for the user (excluding dark colors)
                        Vector4 color;
                        float brightness;
                        do
                        {
                            float r = (float)new Random().NextDouble();
                            float g = (float)new Random().NextDouble();
                            float b = (float)new Random().NextDouble();

                            // Calculate brightness as the average of RGB values
                            brightness = (r + g + b) / 3.0f;
                            color = new Vector4(r, g, b, 1.0f);

                        } while (brightness < 0.55f || UserColors.ContainsValue(color)); // Adjust threshold as needed (e.g., 0.7 for lighter colors)
                        UserColors[x.UID] = color;
                    }
                }

                // grab cursor screen pos x
                var cursorPos = ImGui.GetCursorScreenPos();
                // Print the user name with color
                ImGui.TextColored(UserColors[x.UID], $"[{x.Name}]");
                // Attach popup if clicked.
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    _lastInteractedMsg = x;
                    _lastAttachedMessage = string.Empty;
                    ImGui.OpenPopup($"GlobalChatMessageActions_{x.UID}");
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Middle))
                {
                    _mediator.Publish(new KinkPlateOpenStandaloneLightMessage(x.UserData));
                }
                UiSharedService.AttachToolTip("Sent @ " + x.TimeStamp.ToString("T", CultureInfo.CurrentCulture) +
                    "--SEP--Right-Click to View Interactions" +
                    "--SEP--Middle-Click to open KinkPlate");
                ImUtf8.SameLineInner();

                // Get the remaining width available in the current row
                var remainingWidth = ImGui.GetContentRegionAvail().X;
                float msgWidth = ImGui.CalcTextSize(x.Message).X;
                // If the total width is less than available, print in one go
                if (msgWidth <= remainingWidth)
                {
                    if (x.SupporterTier is CkSupporterTier.KinkporiumMistress)
                        UiSharedService.ColorText(x.Message, CkMistressText);
                    else
                        ImGui.TextUnformatted(x.Message);
                }
                else
                {
                    // Calculate how much of the message fits in the available space
                    string fittingMessage = string.Empty;
                    string[] words = x.Message.Split(' ');
                    float currentWidth = 0;

                    // Build the fitting message
                    foreach (var word in words)
                    {
                        float wordWidth = ImGui.CalcTextSize(word + " ").X;

                        // Check if adding this word exceeds the available width
                        if (currentWidth + wordWidth > remainingWidth) break; // Stop if it doesn't fit
                        fittingMessage += word + " ";
                        currentWidth += wordWidth;
                    }

                    // Print the fitting part of the message
                    ImUtf8.SameLineInner();
                    if (x.SupporterTier is CkSupporterTier.KinkporiumMistress)
                        UiSharedService.ColorText(fittingMessage.TrimEnd(), CkMistressText);
                    else
                        ImGui.TextUnformatted(fittingMessage.TrimEnd());

                    // Draw the remaining part of the message wrapped
                    string wrappedMessage = x.Message.Substring(fittingMessage.Length).TrimStart();
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ySpacing);
                    if (x.SupporterTier is CkSupporterTier.KinkporiumMistress)
                        UiSharedService.ColorTextWrapped(wrappedMessage, CkMistressText);
                    else
                        UiSharedService.TextWrapped(wrappedMessage);
                }
            }

            // Always scroll to the bottom after rendering messages
            // Only scroll to the bottom if auto-scroll is enabled and a new message is received
            if (ShouldScrollToBottom || (AutoScroll && MessageCountSinceLastScroll > 0))
            {
                ShouldScrollToBottom = false;
                ImGui.SetScrollHereY(1.0f);
                MessageCountSinceLastScroll = 0;
            }

            // draw the text preview if we should.
            if (showMessagePreview && !string.IsNullOrWhiteSpace(previewMessage))
                DrawTextWrapBox(previewMessage, drawList);

            if(!_lastInteractedMsg.Equals(new ChatMessage()))
                HandlePopup();
        }
    }

    private void HandlePopup()
    {
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.PopupRounding, 4f);
        using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2f);
        using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        // Handle popup if opened.
        if (ImGui.BeginPopup($"GlobalChatMessageActions_{_lastInteractedMsg.UID}"))
        {
            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(_lastInteractedMsg.Name);
            ImGui.Separator();
            if (ImGui.Selectable("View Light KinkPlate"))
            {
                if (_lastInteractedMsg.UID != "System")
                {
                    _mediator.Publish(new KinkPlateOpenStandaloneLightMessage(_lastInteractedMsg.UserData));
                    _lastInteractedMsg = new ChatMessage();
                    ImGui.CloseCurrentPopup();
                }
            }
            UiSharedService.AttachToolTip("Opens " + _lastInteractedMsg.Name + "'s Light KinkPlate.");
            ImGui.Separator();

            using (ImRaii.Disabled(!KeyMonitor.ShiftPressed()))
            {
                // Display each action as a selectable
                if (ImGui.Selectable("Send Kinkster Request"))
                {
                    _ = _apiHubMain.UserSendPairRequest(new(new(_lastInteractedMsg.UID), _lastAttachedMessage));
                    _lastInteractedMsg = new ChatMessage();
                    ImGui.CloseCurrentPopup();
                }
                if (KeyMonitor.ShiftPressed()) UiSharedService.AttachToolTip("Sends a Kinkster Request to " + _lastInteractedMsg.Name + ".");
            }
            if (!KeyMonitor.ShiftPressed()) UiSharedService.AttachToolTip("Must be holding SHIFT to select.");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 20);
            ImGui.InputTextWithHint("##attachedPairMsg", "Attached Request Msg..", ref _lastAttachedMessage, 100);
            ImGui.Separator();
            
            using (ImRaii.Disabled(!KeyMonitor.CtrlPressed()))
            {
                if (ImGui.Selectable("Hide Messages from Kinkster"))
                {
                    // Prevent silencing System and Self.
                    if (_lastInteractedMsg.UID != "System" && _lastInteractedMsg.UID != MainHub.UID)
                    {
                        UidSilenceList.Add(_lastInteractedMsg.UID);
                        _lastInteractedMsg = new ChatMessage();
                        ImGui.CloseCurrentPopup();
                    }
                }
                if (KeyMonitor.CtrlPressed()) UiSharedService.AttachToolTip("Hides any other messages from this Kinkster until plugin reload/restart.");
            }
            if (!KeyMonitor.CtrlPressed()) UiSharedService.AttachToolTip("Must be holding CTRL to select.");

            ImGui.EndPopup();
        }
    }

    private void DrawTextWrapBox(string message, ImDrawListPtr drawList)
    {
        var padding = new Vector2(5, 5);

        // Set the wrap width based on the available region
        var wrapWidth = (RectMax.X - RectMin.X) - padding.X * 2;

        // Estimate text size with wrapping
        var textSize = ImGui.CalcTextSize(message, wrapWidth: wrapWidth);

        // Calculate the height of a single line for the given wrap width
        float singleLineHeight = ImGui.CalcTextSize("A").Y;
        int lineCount = (int)Math.Ceiling(textSize.Y / singleLineHeight);

        // Calculate the total box size based on line count
        var boxSize = new Vector2(RectMax.X, lineCount * singleLineHeight + padding.Y * 2);

        // Position the box above the input, offset by box height
        var boxPos = new Vector2(0, RectMax.Y - boxSize.Y);

        // Draw semi-transparent background
        drawList.AddRectFilled(boxPos, boxPos + boxSize, ImGui.GetColorU32(new Vector4(0.05f, 0.025f, 0.05f, .9f)), 5);

        var startPos = new Vector2(ImGui.GetCursorScreenPos().X + padding.X, RectMax.Y - boxSize.Y + padding.Y);
        ImGui.SetCursorScreenPos(startPos);
        ImGui.PushTextWrapPos(wrapWidth);
        ImGui.TextUnformatted(message);
        ImGui.PopTextWrapPos();
    }
}
